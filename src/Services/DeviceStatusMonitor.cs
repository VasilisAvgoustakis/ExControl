using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using ExControl.Models;

namespace ExControl.Services
{
    public class DeviceStatusMonitor : IDisposable
    {
        private readonly DeviceManager _deviceManager;
        private readonly IPingService _pingService;
        private readonly Dictionary<string, int> _failureCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        private readonly Timer _timer;

        // How often (in milliseconds) to run the ping loop. Default: 60000 ms = 60 seconds.
        // Commented out. Better to pass the time interval at the constructor to allow for custom times i.e. in the case of testing
        // private const int PingIntervalMs = 60000; 

        public DeviceStatusMonitor(DeviceManager deviceManager, IPingService pingService, int PingIntervalMs)
        {
            _deviceManager = deviceManager ?? throw new ArgumentNullException(nameof(deviceManager));
            _pingService = pingService ?? throw new ArgumentNullException(nameof(pingService));

            // Initialize the timer. The timer callback is the CheckAllDevices method.
            // DueTime=0 => start immediately, Period=PingIntervalMs => repeat every 'PingIntervalMS' seconds.
            _timer = new Timer(_ => CheckAllDevices(), null, 0, PingIntervalMs);
        }

        /// <summary>
        /// Called by the timer every 'PingIntervalMS' seconds (whatever PingIntervalMs is).
        /// </summary>
        private void CheckAllDevices()
        {
            var devices = _deviceManager.GetAllDevices();
            foreach (var device in devices)
            {
                bool success = _pingService.PingDevice(device);
                ProcessPingResult(device, success);
            }
        }

        /// <summary>
        /// Updates failure counters and sets device.IsOnline based on ping results.
        /// </summary>
        private void ProcessPingResult(Device device, bool pingSuccess)
        {
            var deviceName = device.Name;

            if (!_failureCounts.ContainsKey(deviceName))
                _failureCounts[deviceName] = 0;

            if (pingSuccess)
            {
                // If the device responded, reset failure count and mark online immediately.
                _failureCounts[deviceName] = 0;
                device.IsOnline = true;
            }
            else
            {
                // Increment the consecutive failure count.
                _failureCounts[deviceName]++;
                // If it's 3 or more consecutive failures, mark device offline.
                if (_failureCounts[deviceName] >= 3)
                {
                    device.IsOnline = false;
                }
            }
        }

        /// <summary>
        /// Dispose pattern to clean up the timer when this service is no longer needed.
        /// </summary>
        public void Dispose()
        {
            _timer?.Dispose();
        }
    }
}
