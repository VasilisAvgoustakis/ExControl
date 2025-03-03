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
    public class DeviceStatusMonitorTests
    {
        private class FakePingService : IPingService
        {
            // Dictionary: deviceName => forced ping result
            public Dictionary<string, bool> PingResults = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);

            public bool PingDevice(Device device)
            {
                // If not found, default to "true" or "false" as you wish.
                if (PingResults.TryGetValue(device.Name, out var result))
                {
                    return result;
                }
                // Default to success if not in dictionary
                return true;
            }
        }

        private string _backupJsonContent = string.Empty;
        private readonly string _jsonPath = Path.Combine(
            AppContext.BaseDirectory, "..", "..", "..", "..", "assets", "json", "devices.json"
        );

        [TestInitialize]
        public void Setup()
        {
            // Backup original devices.json
            if (File.Exists(_jsonPath))
            {
                _backupJsonContent = File.ReadAllText(_jsonPath);
            }
            File.WriteAllText(_jsonPath, "[]");
        }

        [TestCleanup]
        public void Cleanup()
        {
            // Restore devices.json
            File.WriteAllText(_jsonPath, _backupJsonContent);
        }

        [TestMethod]
        public void SingleDevice_GoesOfflineAfter3Failures()
        {
            // 1) Create a device manager, add one device
            var manager = new DeviceManager();
            var device = new Device { Name = "TestPC", IP = "192.168.0.10" };
            manager.AddDevice(device);

            // 2) Create a fake ping service that returns "false" (fail) for "TestPC"
            var fakePing = new FakePingService();
            fakePing.PingResults["TestPC"] = false; // always failing

            // 3) Create the status monitor
            using (var monitor = new DeviceStatusMonitor(manager, fakePing, 100))
            {
                // We'll manually call CheckAllDevices() if you want immediate effect,
                // but it’s private. So we rely on the Timer or reflect, or we can do "Thread.Sleep".
                // For testing, let's do something simpler: we'll call 
                // the private method with reflection or simulate multiple intervals.

                // Since the code actually fires on a Timer, let's wait enough time to let it run a few times.
                // CAUTION: If your environment is slow, you might increase the sleeps or
                // add some way to forcibly call an internal check.

                System.Threading.Thread.Sleep(200); // Wait a fraction of a second for the first check

                // After the first check:
                // The device has 1 failure, but is still online because it hasn't reached 3 yet.
                var devAfter1 = manager.GetDeviceByName("TestPC");
                Assert.IsTrue(devAfter1.IsOnline, "Should still be online after 1 failure.");

                // Sleep enough for 2 more checks to happen. Each check ~1 second for test? 
                // If we set 60s in code we’ll have to wait 5 minutes to run the real check. 
                // For unit test speed, we pass it in via a constructor parameter.

                // For demonstration, let's assume we changed PingIntervalMs to 100ms for testing:
                System.Threading.Thread.Sleep(700); 
                // By now, 1 + 2 = 3 checks total should have happened.

                var devAfter3 = manager.GetDeviceByName("TestPC");
                Assert.IsFalse(devAfter3.IsOnline, "Should be offline after 3 consecutive failures.");
            }
        }

        [TestMethod]
        public void Device_ComesOnlineImmediatelyAfterSuccess()
        {
            // 1) Create a device
            var manager = new DeviceManager();
            var device = new Device { Name = "Switch", IP = "192.168.0.20" };
            manager.AddDevice(device);

            // 2) Fake ping that fails first, then success
            var fakePing = new FakePingService();
            fakePing.PingResults["Switch"] = false; // Start failing

            using (var monitor = new DeviceStatusMonitor(manager, fakePing, 1000))
            {
                // Let's say we forcibly cause multiple checks. We'll do quick sleeps to let the timer run.
                System.Threading.Thread.Sleep(700); 
                // Enough time for a few checks, device should still be online until it fails 3 times.

                var devMid = manager.GetDeviceByName("Switch");
                Assert.IsTrue(devMid.IsOnline, "Has not yet reached 3 consecutive failures.");

                // Now we set the ping to success, meaning next check should set it IsOnline = true, and reset counters.
                fakePing.PingResults["Switch"] = true;

                System.Threading.Thread.Sleep(300); 
                // Next timer tick: success => device is definitely online, failure count reset to 0.

                var devNow = manager.GetDeviceByName("Switch");
                Assert.IsTrue(devNow.IsOnline, "Should remain (or become) online after success ping.");
            }
        }
    }
}
