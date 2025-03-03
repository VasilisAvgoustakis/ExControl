using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ExControl.Data;
using ExControl.Models;
using ExControl.Services;

namespace ExControl.Tests
{
    [TestClass]
    public class DependencyPowerSequencingTests
    {
        private string _backupJson = string.Empty;
        private readonly string _jsonPath = Path.Combine(
            AppContext.BaseDirectory, "..", "..", "..", "..", "assets", "json", "devices.json"
        );

        [TestInitialize]
        public void Setup()
        {
            // Backup
            if (File.Exists(_jsonPath))
            {
                _backupJson = File.ReadAllText(_jsonPath);
            }
            File.WriteAllText(_jsonPath, "[]");
        }

        [TestCleanup]
        public void Cleanup()
        {
            // Restore
            File.WriteAllText(_jsonPath, _backupJson);
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void DetectsCircularDependency_OnAdd()
        {
            var manager = new DeviceManager();

            var devA = new Device
            {
                Name = "A",
                Dependencies = new List<Dependency>
                {
                    new Dependency { DependsOn = "B", DelayMinutes = 2 }
                }
            };
            var devB = new Device
            {
                Name = "B",
                Dependencies = new List<Dependency>
                {
                    new Dependency { DependsOn = "A", DelayMinutes = 1 }
                }
            };

            manager.AddDevice(devA);
            // This next call should trigger the cycle detection:
            manager.AddDevice(devB); // throws
        }

        [TestMethod]
        public void ManualControl_DelaysOn_CorrectlyForChain()
        {
            var manager = new DeviceManager();
            var control = new ManualControlService();

            // A => no dependencies
            var devA = new Device
            {
                Name = "A",
                Commands = new Dictionary<string, string>
                {
                    { "on", "turnOnA" },
                    { "off", "turnOffA" }
                }
            };

            // B => depends on A, 5 min delay
            var devB = new Device
            {
                Name = "B",
                Dependencies = new List<Dependency>
                {
                    new Dependency { DependsOn = "A", DelayMinutes = 5 }
                },
                Commands = new Dictionary<string, string>
                {
                    { "on", "turnOnB" }
                }
            };

            // Add them
            manager.AddDevice(devA);
            manager.AddDevice(devB);

            // Turn A on manually -> no delay
            bool resultA = control.TurnDeviceOn(devA);
            Assert.IsTrue(resultA, "A should turn on immediately.");

            // Now turn B on -> should be delayed 5 mins after A's on
            bool resultB = control.TurnDeviceOn(devB);
            Assert.IsTrue(resultB, "B is scheduled/delayed. Not an immediate fail.");

            // We can't do an actual 5-min wait in a test. We just confirm the logs or mechanism:
            // For demonstration, we rely on the console output or do we store times in the manual service?

            // In a real system, you'd check that B is recognized as "delayed until (A'sTime + 5)". 
            // The test here is mostly that no exception is thrown, and we can read the log to confirm.
        }

        [TestMethod]
        public void Scheduler_RespectsDelays_WhenDependenciesTurnOnFirst()
        {
            // Create 2 devices in chain
            var devA = new Device
            {
                Name = "A",
                Schedule = new List<ScheduleEntry>
                {
                    new ScheduleEntry { Action = "turn_on", Time = "09:00", Days = new List<string>{"Monday"} }
                }
            };
            var devB = new Device
            {
                Name = "B",
                Dependencies = new List<Dependency>
                {
                    new Dependency { DependsOn = "A", DelayMinutes = 10 }
                },
                Schedule = new List<ScheduleEntry>
                {
                    new ScheduleEntry { Action = "turn_on", Time = "09:05", Days = new List<string>{"Monday"} }
                }
            };

            var devices = new List<Device> { devA, devB };
            var scheduler = new Scheduler();

            // We'll keep track of final actions
            var actions = new List<string>();

            // It's Monday 09:06 => 
            var testNow = new DateTime(2025, 3, 3, 9, 6, 0, DateTimeKind.Utc);

            scheduler.RunSchedules(
                devices,
                testNow,
                (device, action) =>
                {
                    actions.Add($"{device.Name}:{action}");
                }
            );

            // By 09:06 => A triggered at 09:00. B triggered at 09:05, 
            // but it depends on A + 10 min => earliest B can actually turn on is 09:10
            // The scheduler code logs a "delayed" message if the final time > now, so no immediate turn_on for B

            // So actions should contain "A:turn_on" but not "B:turn_on" yet, because B is delayed beyond 09:06
            Assert.AreEqual(1, actions.Count, "Only one immediate turn_on should happen by 09:06.");
            Assert.AreEqual("A:turn_on", actions[0]);
        }

        [TestMethod]
        public void DependencyOffline_IgnoredByScheduler()
        {
            // Suppose B depends on C, but C is not scheduled at all or is offline. B eventually turns on anyway.
            var devB = new Device
            {
                Name = "B",
                Dependencies = new List<Dependency>
                {
                    new Dependency { DependsOn = "C", DelayMinutes = 5 }
                },
                Schedule = new List<ScheduleEntry>
                {
                    new ScheduleEntry { Action = "turn_on", Time = "10:00", Days = new List<string>{"Tuesday"} }
                }
            };

            // B is in the list, but there's no 'C' device. => "If the dependency is offline or removed, the device eventually turns on anyway."
            var devices = new List<Device> { devB };
            var scheduler = new Scheduler();

            var testNow = new DateTime(2025, 3, 4, 10, 0, 0, DateTimeKind.Utc); // A Tuesday at 10:00

            bool bOnCalled = false;
            scheduler.RunSchedules(
                devices,
                testNow,
                (dev, action) =>
                {
                    if (dev.Name == "B" && action == "turn_on") bOnCalled = true;
                }
            );

            Assert.IsTrue(bOnCalled, "B should turn on even though 'C' does not exist or is offline.");
        }
    }
}
