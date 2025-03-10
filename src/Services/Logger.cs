using System;
using System.IO;

namespace ExControl.Services
{
    public static class Logger
    {
        // In production this path might be configurable.
        private static readonly string LogFilePath = Path.Combine(AppContext.BaseDirectory, "debug.log");

        public static void Log(string message)
        {
            try
            {
                string logEntry = $"{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} - {message}{Environment.NewLine}";
                File.AppendAllText(LogFilePath, logEntry);
            }
            catch (Exception ex)
            {
                // Fallback: output to error stream if logging fails.
                Console.Error.WriteLine("Failed to log message: " + ex.Message);
            }
        }
    }
}
