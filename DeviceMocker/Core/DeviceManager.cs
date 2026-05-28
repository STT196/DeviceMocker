using System.Collections.Generic;
using System.Linq;
using DeviceMocker.Interfaces;
using DeviceMocker.Models;

namespace DeviceMocker.Core
{
    public class DeviceManager
    {
        private readonly Dictionary<string, IDeviceModule> _devices = new();

        public void Register(IDeviceModule device)
        {
            _devices[device.Id] = device;
        }

        public IDeviceModule? GetDevice(string id)
        {
            _devices.TryGetValue(id, out var device);
            return device;
        }

        public IReadOnlyList<IDeviceModule> GetAllDevices()
        {
            return _devices.Values.ToList().AsReadOnly();
        }

        public IReadOnlyList<IDeviceModule> GetDevicesByType(DeviceType type)
        {
            return _devices.Values.Where(d => d.DeviceType == type).ToList().AsReadOnly();
        }
    }
}
