using System;
using ExControl.Models;

namespace ExControl.Services
{
    public class DefaultPingService : IPingService
    {
        public bool PingDevice(Device device)
        {
            // TODO: In real code, do actual ICMP ping or similar approach.
            // For now, we can do a stub that always returns true:
            return true;
        }
    }
}
