using System;
using System.Collections.Generic;
using System.Threading;
using ExControl.Data;
using ExControl.Models;
using ExControl.Services;

namespace ExControl
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Console.WriteLine("Welcome to ExControl!");

            // 1. Initialize the DeviceManager and load devices from JSON
            var deviceManager = new DeviceManager();
            var devices = deviceManager.GetAllDevices().ToList();
            Console.WriteLine($"Loaded {devices.Count} devices from JSON.");

            // 2. Create the Scheduler
            var scheduler = new Scheduler();

            // 3. For demonstration, run the scheduler *once* right now
            var now = DateTime.UtcNow;
            Console.WriteLine($"Running scheduler at: {now} (UTC)");

            

            scheduler.RunSchedules(
                devices,
                now,
                (device, action) => 
                {
                    // This is a stub for the actual "device command" logic.
                    // For now, just print to console.
                    Console.WriteLine($"[Scheduler] Device '{device.Name}' => {action}");

                    // If you wanted to do real commands, e.g.:
                    // if (action == "turn_on") { ExecuteWakeOnLan(device); }
                    // else if (action == "turn_off") { ExecuteShutdown(device); }
                }
            );

            // 4. Because one-time schedules might have changed (HasTriggered = true),
            //    re-save any changes back to JSON if you want them persisted.
            deviceManager.SaveChanges();

            // 5. Possibly do a wait or keep the program running
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }

        // Optional: real device commands
        // private static void ExecuteWakeOnLan(Device device) { ... }
        // private static void ExecuteShutdown(Device device) { ... }
    }
}
