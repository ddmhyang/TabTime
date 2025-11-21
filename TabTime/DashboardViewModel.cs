using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace TabTime
{
    public class DashboardViewModel : INotifyPropertyChanged
    {
        #region --- 서비스 및 멤버 변수 ---

        private readonly ITimerService _timerService;
        private readonly ISettingsService _settingsService;
        private readonly ITaskService _taskService;
        private readonly ITimeLogService _timeLogService;
        private readonly IDialogService _dialogService;

        private DateTime _lastFocusNagTime = DateTime.MinValue;

        private readonly Stopwatch _stopwatch;
        private AppSettings _settings;

        private bool _isInGracePeriod = false;
        private DateTime _gracePeriodStartTime;
        private const int GracePeriodSeconds = 120;

        private TaskItem _currentWorkingTask;
        private DateTime _sessionStartTime;

        private Dictionary<string, TimeSpan> _dailyTaskTotals = new Dictionary<string, TimeSpan>();
        private TimeSpan _totalTimeTodayFromLogs;

        #endregion

        #region --- UI와 연결될 속성들 ---

        private string _mainTimeDisplayText = "00:00:00";
        public string MainTimeDisplayText
        {
            get => _mainTimeDisplayText;
            set => SetProperty(ref _mainTimeDisplayText, value);
        }

        private string _totalTimeTodayDisplayText = "총 작업 시간 | 00:00:00";
        public string TotalTimeTodayDisplayText
        {
            get => _totalTimeTodayDisplayText;
            set => SetProperty(ref _totalTimeTodayDisplayText, value);
        }

        private string _currentTaskDisplayText = "없음";
        public string CurrentTaskDisplayText
        {
            get => _currentTaskDisplayText;
            set => SetProperty(ref _currentTaskDisplayText, value);
        }

        public ObservableCollection<TaskItem> TaskItems { get; private set; }

        public ObservableCollection<TimeLogEntry> TimeLogEntries { get; private set; }

        private TaskItem _selectedTaskItem;
        public TaskItem SelectedTaskItem
        {
            get => _selectedTaskItem;
            set
            {
                if (SetProperty(ref _selectedTaskItem, value))
                {
                    OnSelectedTaskChanged(value);
                }
            }
        }

        #endregion

        public DashboardViewModel()
        {
            _taskService = new TaskService();
            _settingsService = new SettingsService();
            _timerService = new TimerService();
            _timeLogService = new TimeLogService();

            _stopwatch = new Stopwatch();
            TaskItems = new ObservableCollection<TaskItem>();
            TimeLogEntries = new ObservableCollection<TimeLogEntry>();

            _timerService.Tick += OnTimerTick;
            DataManager.SettingsUpdated += OnSettingsUpdated;

            LoadInitialDataAsync();
        }

        private void OnSettingsUpdated()
        {
            _settings = _settingsService.LoadSettings();
        }

        private async void LoadInitialDataAsync()
        {
            _settings = _settingsService.LoadSettings();

            var loadedTasks = await _taskService.LoadTasksAsync();
            foreach (var task in loadedTasks) TaskItems.Add(task);

            var loadedLogs = await _timeLogService.LoadTimeLogsAsync();
            foreach (var log in loadedLogs) TimeLogEntries.Add(log);

            RecalculateDailyTotals();
            UpdateLiveTimeDisplays();

            _timerService.Start();
        }

        private void RecalculateDailyTotals()
        {
            _dailyTaskTotals.Clear();
            var todayLogs = TimeLogEntries.Where(log => log.StartTime.Date == DateTime.Today);

            _totalTimeTodayFromLogs = new TimeSpan(todayLogs.Sum(log => log.Duration.Ticks));

            _dailyTaskTotals = todayLogs
                .GroupBy(log => log.TaskText)
                .ToDictionary(g => g.Key, g => new TimeSpan(g.Sum(l => l.Duration.Ticks)));
        }

        private void OnTimerTick(TimeSpan ignored)
        {
            HandleStopwatchMode();
            UpdateLiveTimeDisplays();
        }

        private void HandleStopwatchMode()
        {
            if (_settings == null) return;

            string activeProcess = ActiveWindowHelper.GetActiveProcessName().ToLower();
            // string activeUrl = ActiveWindowHelper.GetActiveBrowserTabUrl()?.ToLower() ?? string.Empty;
            // string activeTitle = ActiveWindowHelper.GetActiveWindowTitle()?.ToLower() ?? string.Empty;

            bool isWorkApp = _settings.WorkProcesses.Any(p => activeProcess.Contains(p));
            bool isDistraction = _settings.DistractionProcesses.Any(p => activeProcess.Contains(p));

            if (isWorkApp)
            {
                if (_isInGracePeriod)
                {
                    _isInGracePeriod = false;
                    _stopwatch.Start();
                }
                else if (!_stopwatch.IsRunning)
                {
                    _currentWorkingTask = SelectedTaskItem ?? TaskItems.FirstOrDefault();
                    if (_currentWorkingTask != null)
                    {
                        _sessionStartTime = DateTime.Now;
                        _stopwatch.Start();
                        CurrentTaskDisplayText = _currentWorkingTask.Text;
                    }
                }
            }
            else
            {
                if (_stopwatch.IsRunning)
                {
                    _stopwatch.Stop();
                    _isInGracePeriod = true;
                    _gracePeriodStartTime = DateTime.Now;
                }
                else if (_isInGracePeriod)
                {
                    if ((DateTime.Now - _gracePeriodStartTime).TotalSeconds > GracePeriodSeconds)
                    {
                        LogWorkSession();
                        _isInGracePeriod = false;
                    }
                }
            }
        }

        private void LogWorkSession()
        {
            if (_currentWorkingTask == null || _stopwatch.Elapsed.TotalSeconds < 1)
            {
                _stopwatch.Reset();
                return;
            }

            var entry = new TimeLogEntry
            {
                StartTime = _sessionStartTime,
                EndTime = DateTime.Now,
                TaskText = _currentWorkingTask.Text
            };

            TimeLogEntries.Insert(0, entry);
            _timeLogService.SaveTimeLogsAsync(TimeLogEntries);

            RecalculateDailyTotals();
            _stopwatch.Reset();
        }

        private void UpdateLiveTimeDisplays()
        {
            var totalTimeToday = _totalTimeTodayFromLogs;
            if (_stopwatch.IsRunning || _isInGracePeriod)
            {
                totalTimeToday += _stopwatch.Elapsed;
            }
            TotalTimeTodayDisplayText = $"총 작업 시간 | {totalTimeToday:hh\\:mm\\:ss}";

            foreach (var task in TaskItems)
            {
                if (_dailyTaskTotals.TryGetValue(task.Text, out var storedTime))
                    task.TotalTime = storedTime;
                else
                    task.TotalTime = TimeSpan.Zero;
            }

            if ((_stopwatch.IsRunning || _isInGracePeriod) && _currentWorkingTask != null)
            {
                var liveTask = TaskItems.FirstOrDefault(t => t.Text == _currentWorkingTask.Text);
                if (liveTask != null)
                {
                    liveTask.TotalTime = liveTask.TotalTime.Add(_stopwatch.Elapsed);
                }
            }

            // 메인 디스플레이 갱신
            if (SelectedTaskItem != null)
            {
                MainTimeDisplayText = SelectedTaskItem.TotalTimeFormatted;
            }
        }

        private void OnSelectedTaskChanged(TaskItem newSelectedTask)
        {
            CurrentTaskDisplayText = newSelectedTask?.Text ?? "없음";
            UpdateLiveTimeDisplays();

            if (_currentWorkingTask != newSelectedTask)
            {
                if (_stopwatch.IsRunning)
                {
                    LogWorkSession();
                }
                _currentWorkingTask = newSelectedTask;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected bool SetProperty<T>(ref T field, T newValue, [CallerMemberName] string propertyName = null)
        {
            if (Equals(field, newValue)) return false;
            field = newValue;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            return true;
        }

        // ✨ [추가됨] 종료 시 호출될 메서드
        public void Shutdown()
        {
            if ((_stopwatch.IsRunning || _isInGracePeriod) && _currentWorkingTask != null)
            {
                LogWorkSession();
            }
            _settingsService?.SaveSettings(_settings);
        }
    }
}