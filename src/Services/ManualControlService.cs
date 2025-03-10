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
        /// Attempts to execute a command with one retry.
        /// If the initial attempt fails, waits for 5 seconds and retries.
        /// Logs an error if the command still fails.
        /// </summary>
        /// <param name="device">The target device.</param>
        /// <param name="command">The command string to execute.</param>
        /// <returns>True if the command ultimately succeeded; false otherwise.</returns>
        private bool ExecuteDeviceCommand(Device device, string command)
        {
            bool success = TryExecuteCommand(device, command);
            if (!success)
            {
                // Retry after a short delay (5 seconds)
                Thread.Sleep(5000);
                success = TryExecuteCommand(device, command);
                if (!success)
                {
                    Logger.Log($"Command '{command}' failed on device '{device.Name}' after retry.");
                }
            }
            return success;
        }

        /// <summary>
        /// Simulates the attempt to execute a command.
        /// In production, this would actually send the command over the network.
        /// For simulation, any command equal to "fail" will simulate a failure.
        /// </summary>
        protected virtual bool TryExecuteCommand(Device device, string command)
        {
            // For demonstration, if the command is "fail", simulate a failure.
            return command != "fail";
        }

        public bool TurnDeviceOn(Device device)
        {
            if (device == null) 
                throw new ArgumentNullException(nameof(device));

            if (device.Commands.TryGetValue("on", out var onCommand))
            {
                bool success = ExecuteDeviceCommand(device, onCommand);
                if (!success)
                {
                    Console.WriteLine($"[ManualControlService] Failed to execute 'on' command for device '{device.Name}'.");
                    return false;
                }
                Console.WriteLine($"[ManualControlService] TURN ON: {device.Name} using command '{onCommand}'");
                return true;
            }
            else
            {
                Console.WriteLine($"[ManualControlService] No 'on' command found for device {device.Name}.");
                return false;
            }
        }

        public bool TurnDeviceOff(Device device)
        {
            if (device == null) 
                throw new ArgumentNullException(nameof(device));

            if (device.Commands.TryGetValue("off", out var offCommand))
            {
                bool success = ExecuteDeviceCommand(device, offCommand);
                if (!success)
                {
                    Console.WriteLine($"[ManualControlService] Failed to execute 'off' command for device '{device.Name}'.");
                    return false;
                }
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


        /// <summary>
        /// Attempts to turn a specific outlet on a power strip on.
        /// If the device.Type != "power_strip", or if it's offline, or if the outlet index is invalid,
        /// we log an error and return false.
        /// </summary>
        public bool TurnOutletOn(Device device, int outletIndex)
        {
            if (device == null) throw new ArgumentNullException(nameof(device));
            if (!device.IsOnline)
            {
                Console.WriteLine($"[ManualControlService] Cannot turn outlet ON because '{device.Name}' is offline.");
                return false;
            }

            if (!device.Type.Equals("power_strip", StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine($"[ManualControlService] Device '{device.Name}' is not a power strip.");
                return false;
            }

            if (outletIndex < 0 || outletIndex >= device.Outlets.Count)
            {
                Console.WriteLine($"[ManualControlService] Invalid outlet index '{outletIndex}' for device '{device.Name}'.");
                return false;
            }

            var outlet = device.Outlets[outletIndex];
            // If outlet has specific commands
            if (outlet.Commands.TryGetValue("on", out var onCmd))
            {
                // Actually run command or stub
                Console.WriteLine($"[ManualControlService] TURN ON outlet '{outlet.Name}' of '{device.Name}' using cmd '{onCmd}'");
            }
            else
            {
                // fallback or log
                Console.WriteLine($"[ManualControlService] No 'on' command found for outlet '{outlet.Name}' on '{device.Name}'.");
            }

            // Mark outlet as on
            outlet.IsOn = true;
            return true;
        }

        /// <summary>
        /// Attempts to turn a specific outlet on a power strip off.
        /// Similar checks to TurnOutletOn.
        /// </summary>
        public bool TurnOutletOff(Device device, int outletIndex)
        {
            if (device == null) throw new ArgumentNullException(nameof(device));
            if (!device.IsOnline)
            {
                Console.WriteLine($"[ManualControlService] Cannot turn outlet OFF because '{device.Name}' is offline.");
                return false;
            }

            if (!device.Type.Equals("power_strip", StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine($"[ManualControlService] Device '{device.Name}' is not a power strip.");
                return false;
            }

            if (outletIndex < 0 || outletIndex >= device.Outlets.Count)
            {
                Console.WriteLine($"[ManualControlService] Invalid outlet index '{outletIndex}' for device '{device.Name}'.");
                return false;
            }

            var outlet = device.Outlets[outletIndex];
            if (outlet.Commands.TryGetValue("off", out var offCmd))
            {
                Console.WriteLine($"[ManualControlService] TURN OFF outlet '{outlet.Name}' of '{device.Name}' using cmd '{offCmd}'");
            }
            else
            {
                Console.WriteLine($"[ManualControlService] No 'off' command found for outlet '{outlet.Name}' on '{device.Name}'.");
            }

            outlet.IsOn = false;
            return true;
        }

    }
}
