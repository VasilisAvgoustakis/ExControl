using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ExControl.Models;
using ExControl.Services;

namespace ExControl.Tests
{
    [TestClass]
    public class ManualControlServiceTests
    {
        [TestMethod]
        public void TurnDeviceOn_WithValidCommand_SucceedsAndDoesNotAlterSchedule()
        {
            // Arrange
            var device = new Device
            {
                Name = "TestPC",
                Commands = new Dictionary<string, string> 
                {
                    { "on", "wolCommand" },
                    { "off", "shutdown -s -t 0" }
                },
                Schedule = new List<ScheduleEntry>
                {
                    new ScheduleEntry
                    {
                        Action = "turn_off",
                        Time = "17:00",
                        Days = new List<string>{"Friday"}
                    }
                }
            };
            var service = new ManualControlService();

            // Act
            bool result = service.TurnDeviceOn(device);

            // Assert
            Assert.IsTrue(result, "Turning on should succeed if an 'on' command is present.");
            Assert.AreEqual(1, device.Schedule.Count, "Schedule count must remain unchanged.");
            Assert.AreEqual("turn_off", device.Schedule[0].Action, "Schedule action must remain the same.");
        }

        [TestMethod]
        public void TurnDeviceOn_NoOnCommand_ReturnsFalseAndScheduleRemains()
        {
            // Arrange
            var device = new Device
            {
                Name = "TestPC2",
                // No "on" command
                Commands = new Dictionary<string, string> 
                {
                    { "off", "shutdown -s" }
                },
                Schedule = new List<ScheduleEntry>
                {
                    new ScheduleEntry
                    {
                        Action = "turn_off",
                        Time = "20:00",
                        Days = new List<string>{"Saturday"}
                    }
                }
            };
            var service = new ManualControlService();

            // Act
            bool result = service.TurnDeviceOn(device);

            // Assert
            Assert.IsFalse(result, "Should return false if no 'on' command is found.");
            Assert.AreEqual(1, device.Schedule.Count, "Schedules remain untouched.");
        }

        [TestMethod]
        public void TurnDeviceOff_WithValidCommand_SucceedsAndDoesNotAlterSchedule()
        {
            // Arrange
            var device = new Device
            {
                Name = "Projector-1",
                Commands = new Dictionary<string, string> 
                {
                    { "on", "proj_on" },
                    { "off", "proj_off" }
                },
                Schedule = new List<ScheduleEntry>
                {
                    new ScheduleEntry
                    {
                        Action = "turn_on",
                        Time = "08:00",
                        Days = new List<string>{"Monday"}
                    }
                }
            };
            var service = new ManualControlService();

            // Act
            bool result = service.TurnDeviceOff(device);

            // Assert
            Assert.IsTrue(result, "Turning off should succeed if an 'off' command is present.");
            Assert.AreEqual(1, device.Schedule.Count, "Schedules remain the same.");
            Assert.AreEqual("turn_on", device.Schedule[0].Action, "No changes to the schedule.");
        }

        [TestMethod]
        public void TurnDeviceOff_NoOffCommand_ReturnsFalseAndScheduleRemains()
        {
            // Arrange
            var device = new Device
            {
                Name = "Projector-2",
                Commands = new Dictionary<string, string>
                {
                    { "on", "proj_on" }
                    // missing "off"
                },
                Schedule = new List<ScheduleEntry>
                {
                    new ScheduleEntry
                    {
                        Action = "turn_on",
                        Time = "09:00",
                        Days = new List<string>{"Sunday"}
                    }
                }
            };
            var service = new ManualControlService();

            // Act
            bool result = service.TurnDeviceOff(device);

            // Assert
            Assert.IsFalse(result, "Should return false if no 'off' command is found.");
            Assert.AreEqual(1, device.Schedule.Count, "Schedules remain untouched.");
        }
    }
}
