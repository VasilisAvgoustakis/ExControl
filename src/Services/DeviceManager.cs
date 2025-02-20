using System;
using System.Collections.Generic;
using System.Linq;
using ExControl.Data;
using ExControl.Models;

namespace ExControl.Services
{
    public class DeviceManager
    {
        private readonly List<Device> _devices;

        /// <summary>
        /// Loads all devices from JSON on construction.
        /// </summary>
        public DeviceManager()
        {
            _devices = JsonStorage.LoadDevices();
        }

        /// <summary>
        /// Returns a snapshot of all devices in memory.
        /// </summary>
        public IReadOnlyList<Device> GetAllDevices()
        {
            return _devices.AsReadOnly();
        }

        /// <summary>
        /// Retrieves a single device by name. Throws if not found.
        /// </summary>
        public Device GetDeviceByName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentNullException(nameof(name), "Device name cannot be null or empty.");
            }

            var device = _devices.FirstOrDefault(d => d.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
            if (device == null)
            {
                throw new KeyNotFoundException($"No device found with name '{name}'.");
            }

            return device;
        }

        /// <summary>
        /// Adds a new device. Fails if a device with the same name already exists.
        /// </summary>
        public void AddDevice(Device device)
        {
            if (device == null)
                throw new ArgumentNullException(nameof(device), "Cannot add a null device.");

            if (string.IsNullOrWhiteSpace(device.Name))
                throw new ArgumentException("Device.Name cannot be null or empty.");

            // Check if a device with this name already exists
            bool alreadyExists = _devices.Any(d => d.Name.Equals(device.Name, StringComparison.OrdinalIgnoreCase));
            if (alreadyExists)
            {
                throw new InvalidOperationException(
                    $"A device with the name '{device.Name}' already exists."
                );
            }

            _devices.Add(device);
            JsonStorage.SaveDevices(_devices);
        }

        /// <summary>
        /// Edits an existing device by matching on 'Name' as the unique key.
        /// Throws if no matching device is found.
        /// </summary>
        public void EditDevice(Device updatedDevice)
        {
            if (updatedDevice == null)
                throw new ArgumentNullException(nameof(updatedDevice), "Cannot edit a null device.");

            if (string.IsNullOrWhiteSpace(updatedDevice.Name))
                throw new ArgumentException("updatedDevice.Name cannot be null or empty.");

            // Find existing device by Name (case-insensitive)
            var existing = _devices.FirstOrDefault(d =>
                d.Name.Equals(updatedDevice.Name, StringComparison.OrdinalIgnoreCase)
            );
            if (existing == null)
            {
                throw new KeyNotFoundException(
                    $"No device found with name '{updatedDevice.Name}' to edit."
                );
            }

            // Update fields
            existing.Type = updatedDevice.Type;
            existing.IP = updatedDevice.IP;
            existing.MAC = updatedDevice.MAC;
            existing.Area = updatedDevice.Area;
            existing.Category = updatedDevice.Category;
            existing.SchedulerGroups = new List<string>(updatedDevice.SchedulerGroups);
            existing.Commands = new Dictionary<string, string>(updatedDevice.Commands);
            existing.Dependencies = new List<Dependency>(updatedDevice.Dependencies);
            existing.Schedule = new List<ScheduleEntry>(updatedDevice.Schedule);

            JsonStorage.SaveDevices(_devices);
        }

        /// <summary>
        /// Removes a device by matching 'deviceName'. Throws if not found.
        /// </summary>
        public void RemoveDevice(string deviceName)
        {
            if (string.IsNullOrWhiteSpace(deviceName))
                throw new ArgumentNullException(nameof(deviceName), "Device name cannot be null or empty.");

            // Remove devices matching the given name (case-insensitive).
            int removedCount = _devices.RemoveAll(d =>
                d.Name.Equals(deviceName, StringComparison.OrdinalIgnoreCase)
            );

            if (removedCount == 0)
            {
                throw new KeyNotFoundException(
                    $"No device found with name '{deviceName}' to remove."
                );
            }

            JsonStorage.SaveDevices(_devices);
        }

        /// <summary>
        ///  Explicitly save the current in-memory device list to JSON.
        /// </summary>
        public void SaveChanges()
        {
            JsonStorage.SaveDevices(_devices);
        }
    }
}
