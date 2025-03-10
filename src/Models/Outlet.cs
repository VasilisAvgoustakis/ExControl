using System.Collections.Generic;

namespace ExControl.Models
{
    /// <summary>
    /// Represents a single outlet on a power strip.
    /// </summary>
    public class Outlet
    {
        public string Name { get; set; } = string.Empty; // e.g. "Outlet #1"
        public bool IsOn { get; set; } = false;          // tracks on/off state
        
        // Optional: If each outlet has separate commands, you can store them here.
        // If all outlets share the same commands, you can skip this dictionary.
        public Dictionary<string, string> Commands { get; set; } = new Dictionary<string, string>();
    }
}
