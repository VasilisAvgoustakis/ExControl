using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using ExControl.Models;

namespace ExControl.Services
{
    public class Scheduler
    {
        /// <summary>
        /// Examines each device’s schedule to see if an action (turn_on/turn_off) 
        /// should occur at or before the specified 'now' time.
        ///
        /// The last triggered schedule for a device determines its final action 
        /// ("last action wins"). If no schedule is triggered, no action is invoked.
        ///
        /// For each triggered action, we call 'deviceAction(device, action)'.
        /// 
        /// Now, we also factor in any dependency delays: if a device depends on others
        /// being on for X minutes, we skip its immediate "turn_on" if that final
        /// delayed time is still in the future.
        /// </summary>
        public void RunSchedules(
            List<Device> devices,
            DateTime now,
            Action<Device, string> deviceAction)
        {
            if (devices == null) return;

            // We'll keep track of each device's actual "on time" to factor in dependency delays.
            // Key = device.Name, Value = the actual time we turned it on in this scheduler pass.
            var actualOnTimes = new Dictionary<string, DateTime>(StringComparer.OrdinalIgnoreCase);

            foreach (var device in devices)
            {
                // 1) Collect all schedule entries that are "valid" as of 'now'.
                var triggeredSchedules = GetTriggeredSchedules(device, now);
                if (!triggeredSchedules.Any()) continue;

                // 2) "Last action wins": pick the final triggered schedule
                var lastOne = triggeredSchedules
                    .OrderBy(s => s.TriggeredDateTime)
                    .Last();

                // 3) If it's turn_on, we check dependency delays
                if (lastOne.Entry.Action.Equals("turn_on", StringComparison.OrdinalIgnoreCase))
                {
                    // The normal "trigger time" is lastOne.TriggeredDateTime.
                    var nominalOnTime = lastOne.TriggeredDateTime;

                    // Adjust that time based on dependencies
                    var finalOnTime = AdjustForDependencies(device, nominalOnTime, actualOnTimes);

                    // If the finalOnTime is still in the future, skip turning on now
                    if (finalOnTime <= now)
                    {
                        deviceAction(device, "turn_on");
                        // Record that we turned it on right now
                        actualOnTimes[device.Name] = now;
                    }
                    // else: we do nothing if it's not "time" yet
                }
                else if (lastOne.Entry.Action.Equals("turn_off", StringComparison.OrdinalIgnoreCase))
                {
                    // Turn off immediately, ignoring dependencies
                    deviceAction(device, "turn_off");
                    // If we had an on-time recorded, remove it
                    if (actualOnTimes.ContainsKey(device.Name))
                    {
                        actualOnTimes.Remove(device.Name);
                    }
                }

                // 4) Mark one-time schedules as triggered
                MarkOneTimeSchedulesAsTriggered(triggeredSchedules);
            }
        }

        /// <summary>
        /// Computes the final time the device can turn on, given its dependencies'
        /// actual on-times plus their respective delays. If a dependency has no
        /// known on-time, we ignore it (the device eventually turns on anyway).
        /// </summary>
        private DateTime AdjustForDependencies(
            Device device,
            DateTime scheduledOnTime,
            Dictionary<string, DateTime> actualOnTimes)
        {
            DateTime finalTime = scheduledOnTime;

            if (device.Dependencies != null)
            {
                foreach (var dep in device.Dependencies)
                {
                    if (actualOnTimes.TryGetValue(dep.DependsOn, out var depOnTime))
                    {
                        var candidate = depOnTime.AddMinutes(dep.DelayMinutes);
                        if (candidate > finalTime)
                        {
                            finalTime = candidate;
                        }
                    }
                }
            }

            return finalTime;
        }

        // -----------------------------------------
        // (No changes in the methods below)
        // -----------------------------------------
        
        private List<ScheduleTrigger> GetTriggeredSchedules(Device device, DateTime now)
        {
            var results = new List<ScheduleTrigger>();

            foreach (var entry in device.Schedule)
            {
                if (entry.Days.Count > 0)
                {
                    var triggeredAt = GetWeeklyTriggerTime(entry, now);
                    if (triggeredAt != null)
                    {
                        results.Add(new ScheduleTrigger(entry, triggeredAt.Value));
                    }
                }
                else if (!string.IsNullOrEmpty(entry.OneTimeUtc))
                {
                    var triggeredAt = GetOneTimeTriggerTime(entry, now);
                    if (triggeredAt != null)
                    {
                        results.Add(new ScheduleTrigger(entry, triggeredAt.Value));
                    }
                }
            }

            return results;
        }

        private DateTime? GetWeeklyTriggerTime(ScheduleEntry entry, DateTime now)
        {
            var dayName = now.DayOfWeek.ToString();
            bool dayMatches = entry.Days.Any(d => d.Equals(dayName, StringComparison.OrdinalIgnoreCase));
            if (!dayMatches) return null;

            if (!TimeSpan.TryParseExact(
                    entry.Time,
                    new[] { "hh\\:mm", "HH\\:mm", "hh\\:mm\\:ss", "HH\\:mm\\:ss" },
                    CultureInfo.InvariantCulture,
                    out var scheduleTime))
            {
                return null;
            }

            var scheduledToday = new DateTime(now.Year, now.Month, now.Day,
                                              scheduleTime.Hours,
                                              scheduleTime.Minutes,
                                              scheduleTime.Seconds,
                                              now.Kind);

            return (scheduledToday <= now) ? scheduledToday : (DateTime?)null;
        }

        private DateTime? GetOneTimeTriggerTime(ScheduleEntry entry, DateTime now)
        {
            if (entry.HasTriggered) return null;

            var formats = new[] { "yyyy-MM-dd HH:mm", "yyyy-MM-dd'T'HH:mm" };
            if (!DateTime.TryParseExact(
                    entry.OneTimeUtc,
                    formats,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                    out var oneTimeDate))
            {
                return null;
            }

            return (oneTimeDate <= now) ? oneTimeDate : (DateTime?)null;
        }

        private void MarkOneTimeSchedulesAsTriggered(List<ScheduleTrigger> triggered)
        {
            foreach (var item in triggered)
            {
                if (item.Entry.Days.Count == 0 && !string.IsNullOrEmpty(item.Entry.OneTimeUtc))
                {
                    item.Entry.HasTriggered = true;
                }
            }
        }

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
}
