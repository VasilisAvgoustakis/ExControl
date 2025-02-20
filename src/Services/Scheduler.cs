using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using ExControl.Models;

namespace ExControl.Services
{
    /// <summary>
    /// The Scheduler inspects each device's schedule to determine if any 
    /// turn_on or turn_off actions apply at the given 'now' time.
    /// 
    /// "Last action wins" is enforced by taking all qualifying schedules 
    /// up to the current time, sorting by their triggered time, and applying 
    /// the final one.
    /// </summary>
    public class Scheduler
    {
        /// <summary>
        /// Examines each device’s schedule to see if an action (turn_on/turn_off) 
        /// should occur at or before the specified 'now'.
        /// 
        /// The last triggered schedule for a device determines its final action 
        /// ("last action wins"). If no schedule is triggered, no action is invoked.
        /// 
        /// For each triggered action, we call 'deviceAction(device, action)'.
        /// 
        /// Typically, you'd call this method periodically (e.g., once per minute) 
        /// to keep devices in sync with their schedules.
        /// </summary>
        /// <param name="devices">All devices to process.</param>
        /// <param name="now">Current time (UTC recommended).</param>
        /// <param name="deviceAction">A callback that will be invoked with (device, "turn_on"/"turn_off").</param>
        public void RunSchedules(
            List<Device> devices, 
            DateTime now, 
            Action<Device, string> deviceAction)
        {
            if (devices == null) return;

            foreach (var device in devices)
            {
                // 1) Collect all schedule entries that are "valid" as of 'now'.
                var triggeredSchedules = GetTriggeredSchedules(device, now);

                // 2) If none are triggered, do nothing.
                if (!triggeredSchedules.Any()) continue;

                // 3) "Last action wins": sort by the time they actually triggered, 
                //    pick the final one.
                var lastOne = triggeredSchedules
                    .OrderBy(s => s.TriggeredDateTime)
                    .Last();

                // 4) Perform the device action (turn_on or turn_off).
                deviceAction(device, lastOne.Entry.Action);

                // 5) If any of these are one-time schedules, set HasTriggered = true.
                MarkOneTimeSchedulesAsTriggered(triggeredSchedules);
            }
        }

        /// <summary>
        /// Examines a single device’s schedule and returns all schedules 
        /// that have triggered on or before 'now'.
        /// 
        /// Each returned item has 'TriggeredDateTime' to indicate the 
        /// effective trigger time for "last action wins" sorting.
        /// </summary>
        private List<ScheduleTrigger> GetTriggeredSchedules(Device device, DateTime now)
        {
            var results = new List<ScheduleTrigger>();

            foreach (var entry in device.Schedule)
            {
                // Check if this entry is weekly or one-time.
                if (entry.Days.Count > 0)
                {
                    // Weekly schedule
                    // e.g. entry.Days = ["Monday", "Wednesday"], entry.Time = "09:00"
                    var triggeredAt = GetWeeklyTriggerTime(entry, now);
                    if (triggeredAt != null)
                    {
                        results.Add(new ScheduleTrigger(entry, triggeredAt.Value));
                    }
                }
                else if (!string.IsNullOrEmpty(entry.OneTimeUtc))
                {
                    // One-time schedule
                    var triggeredAt = GetOneTimeTriggerTime(entry, now);
                    if (triggeredAt != null)
                    {
                        results.Add(new ScheduleTrigger(entry, triggeredAt.Value));
                    }
                }
            }

            return results;
        }

        /// <summary>
        /// For a weekly schedule entry, returns the actual DateTime it triggered 
        /// if it's on or before 'now'. Otherwise, null if it hasn't triggered yet 
        /// (or not valid for this day).
        /// </summary>
        private DateTime? GetWeeklyTriggerTime(ScheduleEntry entry, DateTime now)
        {
            // 1) Check if 'now.DayOfWeek' is in entry.Days (case-insensitive)
            var dayName = now.DayOfWeek.ToString(); // "Monday", "Tuesday", etc.
            bool dayMatches = entry.Days.Any(
                d => d.Equals(dayName, StringComparison.OrdinalIgnoreCase)
            );
            if (!dayMatches) return null;

            // 2) Parse entry.Time as HH:mm or HH:mm:ss
            if (!TimeSpan.TryParseExact(entry.Time, 
                                        new[] { "hh\\:mm", "HH\\:mm", "hh\\:mm\\:ss", "HH\\:mm\\:ss" }, 
                                        CultureInfo.InvariantCulture, 
                                        out var scheduleTime))
            {
                // If parsing fails, skip this schedule as invalid
                return null;
            }

            // 3) Construct a DateTime for "today" at scheduleTime
            var scheduledToday = new DateTime(
                now.Year, now.Month, now.Day,
                scheduleTime.Hours, 
                scheduleTime.Minutes, 
                scheduleTime.Seconds,
                now.Kind // maintain UTC or local
            );

            // 4) If scheduledToday <= now, it means the schedule has triggered
            //    for today's date.
            if (scheduledToday <= now)
            {
                return scheduledToday;
            }

            return null;
        }

        /// <summary>
        /// For a one-time schedule entry, returns the actual DateTime it triggered 
        /// if it's on or before 'now'. Returns null if it hasn't triggered or 
        /// if it already triggered in the past (HasTriggered == true).
        /// </summary>
        private DateTime? GetOneTimeTriggerTime(ScheduleEntry entry, DateTime now)
        {
            // If it has already triggered once, do not trigger again.
            if (entry.HasTriggered) return null;

            // Handle multiple possible formats, if you wish. For now, we only need "yyyy-MM-dd HH:mm".
            var formats = new[] { "yyyy-MM-dd HH:mm", "yyyy-MM-dd'T'HH:mm" };

            if (!DateTime.TryParseExact(entry.OneTimeUtc,
                                        formats,
                                        CultureInfo.InvariantCulture,
                                        DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                                        out var oneTimeDate))
            {
                // If parsing fails or the format is different, skip
                return null;
            }

            // If oneTimeDate <= now => it triggered, else not yet
            if (oneTimeDate <= now)
            {
                return oneTimeDate;
            }

            return null;
        }


        /// <summary>
        /// Marks any one-time schedule entries as triggered (HasTriggered = true) 
        /// so they won't fire again.
        /// </summary>
        private void MarkOneTimeSchedulesAsTriggered(List<ScheduleTrigger> triggered)
        {
            foreach (var item in triggered)
            {
                // Only for one-time entries
                if (item.Entry.Days.Count == 0 && !string.IsNullOrEmpty(item.Entry.OneTimeUtc))
                {
                    item.Entry.HasTriggered = true;
                }
            }
        }
    }

    /// <summary>
    /// Internal helper struct to keep track of a triggered schedule entry 
    /// and the exact date/time it triggered.
    /// </summary>
    internal struct ScheduleTrigger
    {
        public ScheduleEntry Entry { get; }
        public DateTime TriggeredDateTime { get; }

        public ScheduleTrigger(ScheduleEntry entry, DateTime triggeredDateTime)
        {
            Entry = entry;
            TriggeredDateTime = triggeredDateTime;
        }
    }
}
