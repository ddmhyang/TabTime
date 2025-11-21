using System;

namespace TabTime
{
    public interface ITimerService
    {
        bool IsRunning { get; }
        event Action<TimeSpan> Tick;
        void Start();
        void Stop();
        void Pause();
        void Resume();
    }
}