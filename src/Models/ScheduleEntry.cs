using System;
using System.Collections.Generic;

namespace ExControl.Models
{
    public class ScheduleEntry
    {
        // e.g. "turn_on" or "turn_off"
        public string Action { get; set; } = string.Empty;
        
        // Could be "09:00" or a full DateTime string for one-time scheduling.
        public string Time { get; set; } = string.Empty;
        
        // For weekly schedules, which days apply? e.g. ["Monday","Tuesday"]
        public List<string> Days { get; set; } = new List<string>();
        
        // Optionally store a specific date for one-time events, or combine date+time in "Time".
        // (Implementation can vary depending on your approach.)
    }
}
