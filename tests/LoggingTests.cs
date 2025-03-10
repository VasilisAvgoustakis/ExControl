using System;
using System.IO;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ExControl.Models;
using ExControl.Services;

namespace ExControl.Tests
{
    [TestClass]
    public class LoggingTests
    {
        // Get the same log file path as used by Logger.
        private readonly string logFilePath = Path.Combine(AppContext.BaseDirectory, "debug.log");

        [TestInitialize]
        public void Setup()
        {
            // Clear any existing log entries.
            if (File.Exists(logFilePath))
            {
                File.Delete(logFilePath);
            }
        }

        [TestMethod]
        public void CommandFailure_LogsError()
        {
            // Arrange: Create a device with an "on" command that will simulate failure.
            var device = new Device
            {
                Name = "TestDevice",
                Commands = new Dictionary<string, string>
                {
                    { "on", "fail" }  // "fail" causes TryExecuteCommand to return false.
                }
            };
            var service = new ManualControlService();

            // Act: Attempt to turn on the device.
            bool result = service.TurnDeviceOn(device);

            // Assert: The command should fail.
            Assert.IsFalse(result, "Expected the command to fail.");

            // Read the log file and verify it contains the expected error entry.
            string logContent = File.Exists(logFilePath) ? File.ReadAllText(logFilePath) : string.Empty;
            Assert.IsTrue(logContent.Contains($"Command 'fail' failed on device '{device.Name}' after retry."),
                "Log entry for command failure was not found.");
        }

        [TestMethod]
        public void OfflineDevice_SkipsScheduledAction_LogsSkipMessage()
        {
            // Arrange: Create an offline device with a scheduled "turn_on" action.
            var device = new Device
            {
                Name = "OfflineDevice",
                IsOnline = false, // Device is offline.
                Schedule = new List<Models.ScheduleEntry>
                {
                    new Models.ScheduleEntry 
                    { 
                        Action = "turn_on", 
                        Time = "09:00", 
                        Days = new List<string> { "Monday" } 
                    }
                }
            };

            var scheduler = new Scheduler();
            // Use a test date corresponding to Monday 09:00 UTC.
            var testDate = new DateTime(2025, 3, 3, 9, 0, 0, DateTimeKind.Utc);

            // Act: Run the scheduler.
            scheduler.RunSchedules(new List<Device> { device }, testDate, (d, action) => { /* No action */ });

            // Assert: Verify that a skip log entry was created.
            string logContent = File.Exists(logFilePath) ? File.ReadAllText(logFilePath) : string.Empty;
            Assert.IsTrue(logContent.Contains($"Scheduled action 'turn_on' skipped for offline device '{device.Name}'"),
                "Log entry for skipping offline device was not found.");
        }
    }
}
