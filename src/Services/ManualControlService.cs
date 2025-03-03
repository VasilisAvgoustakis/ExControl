using System;
using System.Collections.Generic;
using ExControl.Models;

namespace ExControl.Services
{
    /// <summary>
    /// Provides manual on/off commands for a given device without altering schedules.
    /// </summary>
    public class ManualControlService
    {

        // We'll maintain a dictionary of which devices are "manually turned on" at what time,
        // so we can figure out delays if multiple calls happen in a short window.
        private readonly Dictionary<string, DateTime> _manualOnTimes = new Dictionary<string, DateTime>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Turns a device on (stub). In a real system, you might call Wake-on-LAN or send
        /// a power command over the network. Here, we log or print a stub message.
        /// </summary>
        /// <param name="device">The device to turn on.</param>
        /// <returns>True if command succeeded; false if it failed.</returns>
        public bool TurnDeviceOn(Device device)
        {
            if (device == null) 
                throw new ArgumentNullException(nameof(device));

            // If the device has a specific "on" command, we could attempt it
            // For now, we'll just do a stub or console log
            if (device.Commands.TryGetValue("on", out var onCommand))
            {
                // In real code, you might do: return SendWakeOnLan(device.MAC);
                Console.WriteLine($"[ManualControlService] TURN ON: {device.Name} using command '{onCommand}'");
                return true; 
            }
            else
            {
                // If no "on" command is defined, we log a warning
                Console.WriteLine($"[ManualControlService] No 'on' command found for device {device.Name}.");
                return false;
            }
        }

        /// <summary>
        /// Turns a device off (stub). In a real system, you might call "shutdown -s" or
        /// send a projector off command. Here, we log or print a stub message.
        /// </summary>
        /// <param name="device">The device to turn off.</param>
        /// <returns>True if command succeeded; false if it failed.</returns>
        public bool TurnDeviceOff(Device device)
        {
            if (device == null) 
                throw new ArgumentNullException(nameof(device));

            // If the device has a specific "off" command, we could attempt it
            // For now, we just log a message
            if (device.Commands.TryGetValue("off", out var offCommand))
            {
                // In real code, you might do: return SendShutdownCommand(device.IP);
                Console.WriteLine($"[ManualControlService] TURN OFF: {device.Name} using command '{offCommand}'");
                return true; 
            }
            else
            {
                Console.WriteLine($"[ManualControlService] No 'off' command found for device {device.Name}.");
                return false;
            }
        }

        // ----------------------------------------------
        // PRIVATE method to compute the "final on time"
        // if dependencies are currently scheduled or manually turned on
        // ----------------------------------------------
        private DateTime _computeDelayForDependencies(Device device, DateTime now)
        {
            if (device.Dependencies == null || !device.Dependencies.Any())
                return now; // no dependencies => no delay

            DateTime finalTime = now;

            foreach (var dep in device.Dependencies)
            {
                // If the dependency device never turned on or does not exist => skip
                if (!_manualOnTimes.TryGetValue(dep.DependsOn, out var depOnTime))
                {
                    // "If the dependency is offline or removed... the device eventually turns on anyway."
                    // So we just ignore
                    continue;
                }

                // If found, candidate time is depOnTime + delay
                var candidate = depOnTime.AddMinutes(dep.DelayMinutes);
                if (candidate > finalTime)
                {
                    finalTime = candidate;
                }
            }

            return finalTime;
        }
    }
}
