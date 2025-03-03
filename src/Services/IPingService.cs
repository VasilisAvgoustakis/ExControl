namespace ExControl.Services
{
    public interface IPingService
    {
        /// <summary>
        /// Returns true if the device responds to a network ping, false if not.
        /// </summary>
        bool PingDevice(Models.Device device);
    }
}
