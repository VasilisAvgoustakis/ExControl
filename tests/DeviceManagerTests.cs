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
    public class DeviceManagerTests
    {
        private string _backupJsonContent = string.Empty;
        private readonly string _jsonPath = Path.Combine(
            AppContext.BaseDirectory, "..", "..", "..", "..", "assets", "json", "devices.json"
        );

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
        }

        [TestCleanup]
        public void Cleanup()
        {
            // Restore original devices.json content after tests
            File.WriteAllText(_jsonPath, _backupJsonContent);
        }

        [TestMethod]
        public void AddDevice_NewDevice_SavesToJson()
        {
            var manager = new DeviceManager();
            var newDevice = new Device
            {
                Name = "TestDevice1",
                Type = "pc",
                IP = "192.168.0.99"
            };

            manager.AddDevice(newDevice);

            // Check in-memory
            var all = manager.GetAllDevices();
            Assert.AreEqual(1, all.Count);
            Assert.AreEqual("TestDevice1", all[0].Name);

            // Check on disk
            var fromDisk = JsonStorage.LoadDevices();
            Assert.AreEqual(1, fromDisk.Count);
            Assert.AreEqual("TestDevice1", fromDisk[0].Name);
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void AddDevice_DuplicateName_ThrowsException()
        {
            var manager = new DeviceManager();
            var device1 = new Device { Name = "Duplicate" };
            var device2 = new Device { Name = "duplicate" }; // same name, different case

            manager.AddDevice(device1);
            // This should throw because we use case-insensitive matching
            manager.AddDevice(device2);

            // MSTest will fail if the exception is not thrown
        }

        [TestMethod]
        public void EditDevice_ExistingDevice_UpdatesFields()
        {
            var manager = new DeviceManager();
            manager.AddDevice(new Device { Name = "OriginalName", IP = "192.168.0.10" });

            var updated = new Device
            {
                Name = "OriginalName", // must match existing device
                IP = "192.168.1.222",
                MAC = "AA:BB:CC:DD:EE:77",
                Area = "UpdatedArea",
                Category = "UpdatedCategory"
            };

            manager.EditDevice(updated);

            // Check in-memory
            var all = manager.GetAllDevices();
            Assert.AreEqual(1, all.Count);
            Assert.AreEqual("OriginalName", all[0].Name);
            Assert.AreEqual("192.168.1.222", all[0].IP);
            Assert.AreEqual("AA:BB:CC:DD:EE:77", all[0].MAC);
            Assert.AreEqual("UpdatedArea", all[0].Area);
            Assert.AreEqual("UpdatedCategory", all[0].Category);

            // Check on disk
            var fromDisk = JsonStorage.LoadDevices();
            Assert.AreEqual("192.168.1.222", fromDisk[0].IP);
            Assert.AreEqual("AA:BB:CC:DD:EE:77", fromDisk[0].MAC);
            Assert.AreEqual("UpdatedArea", fromDisk[0].Area);
            Assert.AreEqual("UpdatedCategory", fromDisk[0].Category);
        }

        [TestMethod]
        [ExpectedException(typeof(KeyNotFoundException))]
        public void EditDevice_UnknownName_ThrowsException()
        {
            var manager = new DeviceManager();
            var updated = new Device { Name = "NoSuchDevice", IP = "192.168.1.12" };
            manager.EditDevice(updated);
        }

        [TestMethod]
        public void RemoveDevice_ExistingDevice_RemovesIt()
        {
            var manager = new DeviceManager();
            manager.AddDevice(new Device { Name = "D1" });
            manager.AddDevice(new Device { Name = "D2" });

            manager.RemoveDevice("D1");

            // Check in-memory
            var all = manager.GetAllDevices();
            Assert.AreEqual(1, all.Count);
            Assert.AreEqual("D2", all[0].Name);

            // Check on disk
            var fromDisk = JsonStorage.LoadDevices();
            Assert.AreEqual(1, fromDisk.Count);
            Assert.AreEqual("D2", fromDisk[0].Name);
        }

        [TestMethod]
        [ExpectedException(typeof(KeyNotFoundException))]
        public void RemoveDevice_NonExistent_ThrowsException()
        {
            var manager = new DeviceManager();
            manager.AddDevice(new Device { Name = "ExistingDevice" });

            // Should throw
            manager.RemoveDevice("NonExistent");
        }

        [TestMethod]
        public void GetDeviceByName_ValidName_ReturnsDevice()
        {
            var manager = new DeviceManager();
            manager.AddDevice(new Device { Name = "MyDevice", IP = "192.168.10.5" });

            var found = manager.GetDeviceByName("MyDevice");
            Assert.IsNotNull(found);
            Assert.AreEqual("192.168.10.5", found.IP);
        }

        [TestMethod]
        [ExpectedException(typeof(KeyNotFoundException))]
        public void GetDeviceByName_InvalidName_ThrowsException()
        {
            var manager = new DeviceManager();
            manager.AddDevice(new Device { Name = "RealDevice" });

            // Should throw
            manager.GetDeviceByName("FakeDevice");
        }
    }
}
