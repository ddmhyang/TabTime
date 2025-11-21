using System;
using System.Diagnostics;
using System.Linq;
using Avalonia.Threading; // ✨ WPF 대신 Avalonia 스레딩 사용!

namespace TabTime
{
    public class TimerService : ITimerService
    {
        // ✨ DispatcherTimer는 이제 Avalonia의 것입니다.
        private readonly DispatcherTimer _timer;
        private readonly Stopwatch _stopwatch;
        private AppSettings _settings;

        public event Action<TimeSpan> Tick;

        public TimerService()
        {
            _stopwatch = new Stopwatch();

            // ✨ Avalonia 타이머 생성 방식
            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            _timer.Tick += Timer_Tick;

            LoadSettings();
            DataManager.SettingsUpdated += LoadSettings;
        }

        private void LoadSettings()
        {
            _settings = DataManager.LoadSettings();
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            Tick?.Invoke(_stopwatch.Elapsed);
            CheckActiveWindow();
        }

        private void CheckActiveWindow()
        {
            if (_settings == null) return;

            // 아까 만든 임시 ActiveWindowHelper가 여기서 호출됩니다.
            string activeProcessName = ActiveWindowHelper.GetActiveProcessName()?.ToLower();
            if (string.IsNullOrEmpty(activeProcessName))
            {
                // Pause(); // (일단 테스트를 위해 주석 처리 해둘게요)
                return;
            }

            // --- 원래 있던 로직들 (나중에 ActiveWindowHelper 완성하면 작동함) ---
            /*
            if (_settings.DistractionProcesses.Any(p => activeProcessName == p))
            {
                Pause();
                return;
            }
            // ... (나머지 로직 생략)
            */
        }

        public bool IsRunning => _timer.IsEnabled;
        public bool IsPaused { get; private set; }

        public void Start()
        {
            if (!IsRunning)
            {
                _stopwatch.Start();
                _timer.Start();
                IsPaused = false;
            }
        }

        public void Stop()
        {
            _stopwatch.Reset();
            _timer.Stop();
            IsPaused = false;
            Tick?.Invoke(TimeSpan.Zero);
        }

        public void Pause()
        {
            if (IsRunning && !IsPaused)
            {
                _stopwatch.Stop();
                IsPaused = true;
            }
        }

        public void Resume()
        {
            if (IsRunning && IsPaused)
            {
                _stopwatch.Start();
                IsPaused = false;
            }
        }
    }
}