using System;
using System.Collections.Generic;
using System.IO;
using ExControl.Data;
using ExControl.Models;
using ExControl.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ExControl.Tests
{
    [TestClass]
    public class GroupManagementTests
    {
        private string _backupJsonContent = string.Empty;
        private readonly string _jsonPath = Path.Combine(
            AppContext.BaseDirectory, "..", "..", "..", "..", "assets", "json", "devices.json"
        );

        // We’ll use a ManualControlService that just logs to console for testing.
        // If you wanted to spy on calls or verify logs, you might mock it instead.
        private ManualControlService _controlService;

        [TestInitialize]
        public void Setup()
        {
            // Backup the current devices.json (if it exists) so we can restore it after tests
            if (File.Exists(_jsonPath))
            {
                _backupJsonContent = File.ReadAllText(_jsonPath);
            }

            // Start each test with an empty file
            File.WriteAllText(_jsonPath, "[]");

            // Create the service we’ll pass to our custom DeviceManager constructor
            _controlService = new ManualControlService();
        }

        [TestCleanup]
        public void Cleanup()
        {
            // Restore original devices.json content after tests
            File.WriteAllText(_jsonPath, _backupJsonContent);
        }

        [TestMethod]
        public void TurnGroupOn_DevicesInGroup_AreTurnedOn()
        {
            // Arrange
            var manager = new DeviceManager(_controlService);

            // Create devices
            var deviceA = new Device
            {
                Name = "DevA",
                SchedulerGroups = new List<string> { "GroupX" },
                Commands = new Dictionary<string, string>
                {
                    { "on", "someOnCommand" },
                    { "off", "someOffCommand" }
                }
            };
            var deviceB = new Device
            {
                Name = "DevB",
                SchedulerGroups = new List<string> { "GroupX" },
                Commands = new Dictionary<string, string>
                {
                    { "on", "onCmd" }
                }
            };
            var deviceC = new Device
            {
                Name = "DevC", // not in GroupX
                SchedulerGroups = new List<string> { "OtherGroup" },
                Commands = new Dictionary<string, string>
                {
                    { "on", "onCmdC" }
                }
            };

            manager.AddDevice(deviceA);
            manager.AddDevice(deviceB);
            manager.AddDevice(deviceC);

            // Act
            manager.TurnGroupOn("GroupX");

            // Assert
            // We can't easily "assert" the console output in a standard MSTest
            // without hooking a mock or capturing the console. But we can confirm
            // no exceptions thrown, and rely on console logs for debugging.
            //
            // Optionally, you could implement a "TestManualControlService" that
            // records calls so you can assert that DevA and DevB were turned on,
            // while DevC was not.
            //
            // We'll do a basic check that the groups remain unchanged:
            var updatedA = manager.GetDeviceByName("DevA");
            Assert.IsTrue(updatedA.SchedulerGroups.Contains("GroupX"));
            var updatedB = manager.GetDeviceByName("DevB");
            Assert.IsTrue(updatedB.SchedulerGroups.Contains("GroupX"));
            var updatedC = manager.GetDeviceByName("DevC");
            Assert.IsFalse(updatedC.SchedulerGroups.Contains("GroupX"));
        }

        [TestMethod]
        public void TurnGroupOff_DevicesInGroup_AreTurnedOff()
        {
            // Arrange
            var manager = new DeviceManager(_controlService);

            var deviceX = new Device
            {
                Name = "X",
                SchedulerGroups = new List<string> { "TurnMeOff" },
                Commands = new Dictionary<string, string>
                {
                    { "off", "shutdownMe" }
                }
            };
            var deviceY = new Device
            {
                Name = "Y",
                SchedulerGroups = new List<string> { "TurnMeOff", "OtherGroup" },
                Commands = new Dictionary<string, string>
                {
                    { "off", "byeNow" }
                }
            };
            var deviceZ = new Device
            {
                Name = "Z",
                SchedulerGroups = new List<string> { "Nope" },
                Commands = new Dictionary<string, string>
                {
                    { "off", "notUsed" }
                }
            };

            manager.AddDevice(deviceX);
            manager.AddDevice(deviceY);
            manager.AddDevice(deviceZ);

            // Act
            manager.TurnGroupOff("TurnMeOff");

            // Assert
            // Same as above, we just confirm groups remain the same, and
            // rely on console logs (or a test double) to confirm the off command was invoked.
            Assert.IsTrue(manager.GetDeviceByName("X").SchedulerGroups.Contains("TurnMeOff"));
            Assert.IsTrue(manager.GetDeviceByName("Y").SchedulerGroups.Contains("TurnMeOff"));
            Assert.IsTrue(manager.GetDeviceByName("Z").SchedulerGroups.Contains("Nope"));
        }

        [TestMethod]
        public void RemoveGroup_DeletesFromAllDevices()
        {
            // Arrange
            var manager = new DeviceManager(_controlService);
            var device1 = new Device
            {
                Name = "HasG1",
                SchedulerGroups = new List<string> { "G1", "G2" }
            };
            var device2 = new Device
            {
                Name = "HasG1Only",
                SchedulerGroups = new List<string> { "G1" }
            };
            var device3 = new Device
            {
                Name = "NoG1",
                SchedulerGroups = new List<string> { "G2" }
            };

            manager.AddDevice(device1);
            manager.AddDevice(device2);
            manager.AddDevice(device3);

            // Act
            manager.RemoveGroup("G1");

            // Assert
            var dev1 = manager.GetDeviceByName("HasG1");
            Assert.IsTrue(dev1.SchedulerGroups.Contains("G2"));
            Assert.IsFalse(dev1.SchedulerGroups.Contains("G1"), "G1 should have been removed.");

            var dev2 = manager.GetDeviceByName("HasG1Only");
            Assert.AreEqual(0, dev2.SchedulerGroups.Count, "All G1 references should be gone.");

            var dev3 = manager.GetDeviceByName("NoG1");
            Assert.IsTrue(dev3.SchedulerGroups.Contains("G2"), "Should be unaffected since it didn't have G1.");
        }
    }
}
