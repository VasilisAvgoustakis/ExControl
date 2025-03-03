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
        private readonly ManualControlService _manualControlService;

        /// <summary>
        /// Loads all devices from JSON on construction, using a default ManualControlService.
        /// </summary>
        public DeviceManager()
            : this(new ManualControlService())
        {
        }

        /// <summary>
        /// Internal or test constructor allowing injection of a ManualControlService.
        /// </summary>
        public DeviceManager(ManualControlService manualControlService)
        {
            _manualControlService = manualControlService ?? throw new ArgumentNullException(nameof(manualControlService));
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
            existing.Schedule = new List<Models.ScheduleEntry>(updatedDevice.Schedule);

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

        // ---------------------------------------------------------------------------------------
        // NEW GROUPING METHODS
        // ---------------------------------------------------------------------------------------

        /// <summary>
        /// Iterates over all devices that have 'groupName' in their SchedulerGroups
        /// and calls TurnDeviceOn(...) on each, via the ManualControlService.
        /// </summary>
        public void TurnGroupOn(string groupName)
        {
            if (string.IsNullOrWhiteSpace(groupName))
                throw new ArgumentNullException(nameof(groupName), "Group name cannot be null or empty.");

            // Find devices that contain this group
            var matchingDevices = _devices
                .Where(d => d.SchedulerGroups.Any(g => g.Equals(groupName, StringComparison.OrdinalIgnoreCase)))
                .ToList();

            foreach (var device in matchingDevices)
            {
                _manualControlService.TurnDeviceOn(device);
            }
        }

        /// <summary>
        /// Iterates over all devices that have 'groupName' in their SchedulerGroups
        /// and calls TurnDeviceOff(...) on each, via the ManualControlService.
        /// </summary>
        public void TurnGroupOff(string groupName)
        {
            if (string.IsNullOrWhiteSpace(groupName))
                throw new ArgumentNullException(nameof(groupName), "Group name cannot be null or empty.");

            // Find devices that contain this group
            var matchingDevices = _devices
                .Where(d => d.SchedulerGroups.Any(g => g.Equals(groupName, StringComparison.OrdinalIgnoreCase)))
                .ToList();

            foreach (var device in matchingDevices)
            {
                _manualControlService.TurnDeviceOff(device);
            }
        }

        /// <summary>
        /// Removes the specified group from all devices' SchedulerGroups list.
        /// For example, if the group is being deleted from the system,
        /// each device that references it will have it removed.
        /// </summary>
        public void RemoveGroup(string groupName)
        {
            if (string.IsNullOrWhiteSpace(groupName))
                throw new ArgumentNullException(nameof(groupName), "Group name cannot be null or empty.");

            bool modifiedAny = false;

            foreach (var device in _devices)
            {
                // Remove all matches of groupName, ignoring case
                int removed = device.SchedulerGroups.RemoveAll(
                    g => g.Equals(groupName, StringComparison.OrdinalIgnoreCase)
                );
                if (removed > 0)
                {
                    modifiedAny = true;
                }
            }

            if (modifiedAny)
            {
                // Save to JSON only if something actually changed
                JsonStorage.SaveDevices(_devices);
            }
        }
    }
}
