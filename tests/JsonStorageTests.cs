using System.Collections.Generic;
using System.IO;
using ExControl.Data;
using ExControl.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ExControl.Tests
{
    [TestClass]
    public class JsonStorageTests
    {
        [TestMethod]
        public void SaveAndLoadDevices_PreservesAllProperties()
        {
            // 1. Create a few devices in memory
            var device1 = new Device
            {
                Name = "PC-1",
                Type = "pc",
                IP = "192.168.0.10",
                MAC = "00:11:22:33:44:55",
                Area = "MainHall",
                Category = "PCs",
                SchedulerGroups = new List<string> { "GroupA", "GroupB" },
                Commands = new Dictionary<string, string> { { "on", "wolCommand" }, { "off", "shutdown -s -t 0" } },
                Dependencies = new List<Dependency>
                {
                    new Dependency { DependsOn = "Projector-1", DelayMinutes = 5 }
                },
                Schedule = new List<ScheduleEntry>
                {
                    new ScheduleEntry { Action = "turn_on", Time = "08:00", Days = new List<string>{"Monday", "Tuesday"} },
                    new ScheduleEntry { Action = "turn_off", Time = "18:00", Days = new List<string>{"Monday", "Tuesday"} }
                }
            };

            var device2 = new Device
            {
                Name = "Projector-1",
                Type = "projector",
                IP = "192.168.0.20",
                MAC = "AA:BB:CC:DD:EE:FF",
                Area = "MainHall",
                Category = "Projectors",
                SchedulerGroups = new List<string> { "GroupA" },
                Commands = new Dictionary<string, string> { { "on", "proj_on" }, { "off", "proj_off" } },
            };

            var originalList = new List<Device> { device1, device2 };

            // 2. Save them to JSON
            JsonStorage.SaveDevices(originalList);

            // 3. Load them back
            var loadedList = JsonStorage.LoadDevices();

            // 4. Compare
            Assert.AreEqual(originalList.Count, loadedList.Count, "Device count should match after round-trip.");

            // We'll just check the first device for demonstration
            var loaded1 = loadedList[0];
            Assert.AreEqual(device1.Name, loaded1.Name, "Name mismatch");
            Assert.AreEqual(device1.Type, loaded1.Type, "Type mismatch");
            Assert.AreEqual(device1.IP, loaded1.IP, "IP mismatch");
            Assert.AreEqual(device1.MAC, loaded1.MAC, "MAC mismatch");
            Assert.AreEqual(device1.Area, loaded1.Area, "Area mismatch");
            Assert.AreEqual(device1.Category, loaded1.Category, "Category mismatch");
            Assert.AreEqual(device1.SchedulerGroups.Count, loaded1.SchedulerGroups.Count, "Scheduler groups mismatch");
            Assert.AreEqual(device1.Commands["on"], loaded1.Commands["on"], "Commands mismatch (on)");
            Assert.AreEqual(device1.Commands["off"], loaded1.Commands["off"], "Commands mismatch (off)");
            Assert.AreEqual(device1.Dependencies[0].DependsOn, loaded1.Dependencies[0].DependsOn, "Dependency mismatch");
            Assert.AreEqual(device1.Schedule[0].Action, loaded1.Schedule[0].Action, "Schedule mismatch (action)");
            Assert.AreEqual(device1.Schedule[0].Time, loaded1.Schedule[0].Time, "Schedule mismatch (time)");
            Assert.AreEqual(device1.Schedule[0].Days[0], loaded1.Schedule[0].Days[0], "Schedule mismatch (days)");

            // Optional: Clean up the test file or leave it for manual inspection
            // File.Delete("...devices.json");
        }
    }
}
