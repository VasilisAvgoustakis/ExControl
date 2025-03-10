using System;
using System.Collections.Generic;

namespace ExControl.Models
{
    public class Device
    {
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;  // e.g. "pc", "projector"
        public string IP { get; set; } = string.Empty;
        public string MAC { get; set; } = string.Empty;
        public string Area { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        
        // For assigning a device to multiple scheduler groups (presets).
        public List<string> SchedulerGroups { get; set; } = new List<string>();
        // In Device.cs, inside the Device class:
        public List<Outlet> Outlets { get; set; } = new List<Outlet>();


        // E.g., {"on": "...", "off": "..."}
        public Dictionary<string, string> Commands { get; set; } = new Dictionary<string, string>();

        // For advanced power sequencing or turn-on dependencies.
        public List<Dependency> Dependencies { get; set; } = new List<Dependency>();

        // Holds scheduled actions (weekly or one-time).
        public List<ScheduleEntry> Schedule { get; set; } = new List<ScheduleEntry>();
        public bool IsOnline { get; set; } = true;
    }
}
