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
        private readonly IDialogService _dialogService; // 팝업창 담당 (나중에 구현)

        private DateTime _lastFocusNagTime = DateTime.MinValue; // 경고 스팸 방지용

        private readonly Stopwatch _stopwatch;
        private AppSettings _settings;

        private bool _isInGracePeriod = false;
        private DateTime _gracePeriodStartTime;
        private const int GracePeriodSeconds = 120; // 2분 유예

        private TaskItem _currentWorkingTask;
        private DateTime _sessionStartTime;

        // 일일 합계 저장용
        private Dictionary<string, TimeSpan> _dailyTaskTotals = new Dictionary<string, TimeSpan>();
        private TimeSpan _totalTimeTodayFromLogs;

        #endregion

        #region --- UI와 연결될 속성들 ---

        // 메인 타이머 시간 (00:00:00)
        private string _mainTimeDisplayText = "00:00:00";
        public string MainTimeDisplayText
        {
            get => _mainTimeDisplayText;
            set => SetProperty(ref _mainTimeDisplayText, value);
        }

        // 오늘의 총 작업 시간
        private string _totalTimeTodayDisplayText = "총 작업 시간 | 00:00:00";
        public string TotalTimeTodayDisplayText
        {
            get => _totalTimeTodayDisplayText;
            set => SetProperty(ref _totalTimeTodayDisplayText, value);
        }

        // 현재 작업 중인 과목 이름
        private string _currentTaskDisplayText = "없음";
        public string CurrentTaskDisplayText
        {
            get => _currentTaskDisplayText;
            set => SetProperty(ref _currentTaskDisplayText, value);
        }

        // 과목 목록
        public ObservableCollection<TaskItem> TaskItems { get; private set; }

        // 로그 목록
        public ObservableCollection<TimeLogEntry> TimeLogEntries { get; private set; }

        // 선택된 과목
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

        // 생성자 (조립하는 곳)
        public DashboardViewModel()
        {
            // 1. 서비스들 준비 (나중에는 의존성 주입으로 더 깔끔하게 할 수 있어요)
            _taskService = new TaskService();
            _settingsService = new SettingsService();
            _timerService = new TimerService(); // 우리가 만든 Avalonia용 타이머
            _timeLogService = new TimeLogService();
            // _dialogService = new DialogService(); // 아직 없으므로 일단 주석 or null

            _stopwatch = new Stopwatch();
            TaskItems = new ObservableCollection<TaskItem>();
            TimeLogEntries = new ObservableCollection<TimeLogEntry>();

            // 2. 타이머 심장 박동 연결
            _timerService.Tick += OnTimerTick;

            // 3. 설정 변경 감지 연결
            DataManager.SettingsUpdated += OnSettingsUpdated;

            // 4. 데이터 불러오기 시작
            LoadInitialDataAsync();
        }

        private void OnSettingsUpdated()
        {
            _settings = _settingsService.LoadSettings();
        }

        private async void LoadInitialDataAsync()
        {
            _settings = _settingsService.LoadSettings();

            // 과목 불러오기
            var loadedTasks = await _taskService.LoadTasksAsync();
            foreach (var task in loadedTasks) TaskItems.Add(task);

            // 로그 불러오기
            var loadedLogs = await _timeLogService.LoadTimeLogsAsync();
            foreach (var log in loadedLogs) TimeLogEntries.Add(log);

            RecalculateDailyTotals();
            UpdateLiveTimeDisplays();

            // 타이머 가동!
            _timerService.Start();
        }

        // --- 핵심 로직들 ---

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

            // 1. 현재 활성 창 정보 가져오기 (지금은 임시 ActiveWindowHelper가 작동함)
            string activeProcess = ActiveWindowHelper.GetActiveProcessName().ToLower();
            string activeUrl = ActiveWindowHelper.GetActiveBrowserTabUrl()?.ToLower() ?? string.Empty;
            string activeTitle = ActiveWindowHelper.GetActiveWindowTitle()?.ToLower() ?? string.Empty;

            // 2. 일하는 중인지 판단 (Settings에 목록이 있어야 함)
            bool isWorkApp = _settings.WorkProcesses.Any(p => activeProcess.Contains(p));

            // 3. 딴짓 중인지 판단
            bool isDistraction = _settings.DistractionProcesses.Any(p => activeProcess.Contains(p));

            // 로직 간소화: 일하는 중이면 타이머 GO, 아니면 PAUSE (유예 시간 로직 포함)
            if (isWorkApp)
            {
                if (_isInGracePeriod)
                {
                    _isInGracePeriod = false;
                    _stopwatch.Start();
                }
                else if (!_stopwatch.IsRunning)
                {
                    // 작업 시작!
                    _currentWorkingTask = SelectedTaskItem ?? TaskItems.FirstOrDefault();
                    if (_currentWorkingTask != null)
                    {
                        _sessionStartTime = DateTime.Now;
                        _stopwatch.Start();
                        CurrentTaskDisplayText = _currentWorkingTask.Text;
                    }
                }
            }
            else // 일 안 하는 중
            {
                if (_stopwatch.IsRunning)
                {
                    _stopwatch.Stop();
                    _isInGracePeriod = true;
                    _gracePeriodStartTime = DateTime.Now;
                }
                else if (_isInGracePeriod)
                {
                    // 유예 시간 초과 체크
                    if ((DateTime.Now - _gracePeriodStartTime).TotalSeconds > GracePeriodSeconds)
                    {
                        LogWorkSession(); // 기록 저장하고 초기화
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
            // 화면에 표시할 시간 계산
            var totalTimeToday = _totalTimeTodayFromLogs;
            if (_stopwatch.IsRunning || _isInGracePeriod)
            {
                totalTimeToday += _stopwatch.Elapsed;
            }
            TotalTimeTodayDisplayText = $"총 작업 시간 | {totalTimeToday:hh\\:mm\\:ss}";

            // 각 과목별 시간 업데이트
            foreach (var task in TaskItems)
            {
                if (_dailyTaskTotals.TryGetValue(task.Text, out var storedTime))
                    task.TotalTime = storedTime;
                else
                    task.TotalTime = TimeSpan.Zero;
            }

            // 현재 작업 중인 과목 시간 실시간 증가
            if ((_stopwatch.IsRunning || _isInGracePeriod) && _currentWorkingTask != null)
            {
                var liveTask = TaskItems.FirstOrDefault(t => t.Text == _currentWorkingTask.Text);
                if (liveTask != null)
                {
                    liveTask.TotalTime = liveTask.TotalTime.Add(_stopwatch.Elapsed);
                }
            }

            // 메인 타이머 숫자 업데이트
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

        // --- INotifyPropertyChanged 구현 (화면 갱신용) ---
        public event PropertyChangedEventHandler PropertyChanged;
        protected bool SetProperty<T>(ref T field, T newValue, [CallerMemberName] string propertyName = null)
        {
            if (Equals(field, newValue)) return false;
            field = newValue;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            return true;
        }
    }
}