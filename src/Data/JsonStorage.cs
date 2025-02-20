using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using ExControl.Models;

namespace ExControl.Data
{
    public static class JsonStorage
    {
        private static readonly string DevicesJsonPath =
    Path.Combine(
        AppContext.BaseDirectory,
        "..",  // up from net7.0
        "..",  // up from Debug
        "..",  // up from bin
        "..",  // up from tests  <-- Add this extra one
        "assets",
        "json",
        "devices.json"
    );
        // ^ Adjust path logic if needed. For production, you might store an absolute path
        //   like "C:\\MuseumControl\\devices.json". Or read from config.

        public static List<Device> LoadDevices()
        {
            try
            {
                if (!File.Exists(DevicesJsonPath))
                {
                    // Return empty list if file doesn't exist yet.
                    return new List<Device>();
                }

                string json = File.ReadAllText(DevicesJsonPath);
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };
                List<Device>? devices = JsonSerializer.Deserialize<List<Device>>(json, options);
                return devices ?? new List<Device>();
            }
            catch
            {
                // Handle errors or log them as needed
                return new List<Device>();
            }
        }

        public static void SaveDevices(List<Device> devices)
        {
            try
            {
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true
                };
                string json = JsonSerializer.Serialize(devices, options);
                File.WriteAllText(DevicesJsonPath, json);
            }
            catch (Exception ex)
            {
                // Handle or log error
                Console.WriteLine($"Failed to save devices.json: {ex.Message}");
            }
        }
    }
}
