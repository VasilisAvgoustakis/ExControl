namespace ExControl.Models
{
    public class Dependency
    {
        public string DependsOn { get; set; } = string.Empty;   // e.g. "Projector-1"
        public int DelayMinutes { get; set; } = 0;             // e.g. 3
    }
}
