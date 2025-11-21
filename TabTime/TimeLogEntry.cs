using System;
using System.Collections.Generic;

namespace TabTime
{
    public class TimeLogEntry
    {
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public string TaskText { get; set; }
        public int FocusScore { get; set; }
        public List<string> BreakActivities { get; set; }
        public TimeSpan Duration => EndTime - StartTime;

        public TimeLogEntry()
        {
            BreakActivities = new List<string>();
        }

        public override string ToString()
        {
            return $"{StartTime:HH:mm} - {EndTime:HH:mm} ({TaskText})";
        }
    }
}