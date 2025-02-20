namespace ExControl.Models
{
    public class ScheduleEntry
    {
        // e.g. "turn_on" or "turn_off"
        public string Action { get; set; } = string.Empty;
        
        // If this is a weekly schedule, store time in "HH:mm" format, and use 'Days'.
        // Example: "09:00"
        //
        // If this is a one-time schedule, store a full date/time in 'OneTimeUtc' instead.
        public string Time { get; set; } = string.Empty;
        
        // For weekly schedules, which days apply? e.g. ["Monday","Tuesday"]
        // If this is empty, we interpret it as a one-time schedule.
        public List<string> Days { get; set; } = new List<string>();
        
        // If present, indicates a one-time event in UTC. 
        // Example: "2025-03-05 09:00" => March 5, 2025 at 09:00 UTC
        public string? OneTimeUtc { get; set; }

        // If this is a one-time schedule, set HasTriggered = true after it fires, so it won’t repeat.
        public bool HasTriggered { get; set; } = false;
    }
}
