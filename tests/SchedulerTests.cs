using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ExControl.Models;
using ExControl.Services;

namespace ExControl.Tests
{
    [TestClass]
    public class SchedulerTests
    {
        [TestMethod]
        public void WeeklySchedule_SimpleScenario()
        {
            var device = new Device
            {
                Name = "TestDevice",
                Schedule = new List<ScheduleEntry>
                {
                    // Every Monday at 09:00 => turn_on
                    new ScheduleEntry 
                    {
                        Action = "turn_on",
                        Time = "09:00",
                        Days = new List<string> {"Monday"}
                    },
                    // Every Monday at 18:00 => turn_off
                    new ScheduleEntry 
                    {
                        Action = "turn_off",
                        Time = "18:00",
                        Days = new List<string> {"Monday"}
                    }
                }
            };

            var scheduler = new Scheduler();
            var testDate = new DateTime(2025, 3, 3, 9, 0, 0, DateTimeKind.Utc); 
            // This is a Monday at 09:00 (assuming 3/3/2025 is Monday)

            string finalAction = string.Empty;

            // We'll pass a callback that sets 'finalAction' whenever a schedule is triggered
            scheduler.RunSchedules(new List<Device> { device }, testDate, (dev, action) =>
            {
                finalAction = action;
            });

            // At 09:00 Monday, "turn_on" is exactly triggered
            Assert.AreEqual("turn_on", finalAction, "Expected turn_on at 09:00 Monday.");
        }

        [TestMethod]
        public void WeeklySchedule_OverlappingLastActionWins()
        {
            var device = new Device
            {
                Name = "TestDevice",
                Schedule = new List<ScheduleEntry>
                {
                    // Monday 09:00 => turn_on
                    new ScheduleEntry
                    {
                        Action = "turn_on",
                        Time = "09:00",
                        Days = new List<string> {"Monday"}
                    },
                    // Monday 09:05 => turn_off
                    new ScheduleEntry
                    {
                        Action = "turn_off",
                        Time = "09:05",
                        Days = new List<string> {"Monday"}
                    },
                    // Monday 09:05 => turn_on (overlapping, but appears after turn_off in the list)
                    new ScheduleEntry
                    {
                        Action = "turn_on",
                        Time = "09:05",
                        Days = new List<string> {"Monday"}
                    }
                }
            };

            var scheduler = new Scheduler();
            // It's Monday 09:06 => everything at 09:00 or 09:05 has triggered.
            // The last schedule in chronological order is the second 09:05 entry => "turn_on".
            var testDate = new DateTime(2025, 3, 3, 9, 6, 0, DateTimeKind.Utc);

            string finalAction = string.Empty;
            scheduler.RunSchedules(new List<Device> { device }, testDate, (dev, action) =>
            {
                finalAction = action;
            });

            // We had 09:00 => turn_on, 09:05 => turn_off, 09:05 => turn_on
            // The last to trigger was the second 09:05 => turn_on
            Assert.AreEqual("turn_on", finalAction, 
                "The final (last) action at 09:05 must override the previous turn_off at the same time.");
        }

        [TestMethod]
        public void OneTimeSchedule_TriggersExactlyOnce()
        {
            var device = new Device
            {
                Name = "OneTimeDevice",
                Schedule = new List<ScheduleEntry>
                {
                    new ScheduleEntry
                    {
                        Action = "turn_off",
                        OneTimeUtc = "2025-03-05 09:00",  // one-time
                    }
                }
            };

            var scheduler = new Scheduler();

            // 1) At 2025-03-05 08:59, hasn't triggered yet
            var testDate1 = new DateTime(2025, 3, 5, 8, 59, 0, DateTimeKind.Utc);
            bool calledAction = false;
            scheduler.RunSchedules(new List<Device> { device }, testDate1, (dev, action) =>
            {
                calledAction = true;
            });
            Assert.IsFalse(calledAction, "No action before 09:00 for a one-time schedule.");

            // 2) At 2025-03-05 09:00, it triggers "turn_off"
            var testDate2 = new DateTime(2025, 3, 5, 9, 0, 0, DateTimeKind.Utc);
            string finalAction = string.Empty;
            scheduler.RunSchedules(new List<Device> { device }, testDate2, (dev, action) =>
            {
                finalAction = action;
            });
            Assert.AreEqual("turn_off", finalAction, "One-time schedule should fire at 09:00.");

            // 3) At 2025-03-05 09:01, the schedule is now used up (HasTriggered = true).
            var testDate3 = new DateTime(2025, 3, 5, 9, 1, 0, DateTimeKind.Utc);
            bool calledAgain = false;
            scheduler.RunSchedules(new List<Device> { device }, testDate3, (dev, action) =>
            {
                calledAgain = true;
            });
            Assert.IsFalse(calledAgain, "Should not trigger again after the one-time event fired.");
        }
    }
}
