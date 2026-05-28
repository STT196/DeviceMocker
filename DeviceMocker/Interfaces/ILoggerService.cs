using System;
using System.Collections.Generic;
using DeviceMocker.Models;

namespace DeviceMocker.Interfaces
{
    public interface ILoggerService
    {
        event Action? LogsUpdated;
        void Log(DeviceLog log);
        IReadOnlyList<DeviceLog> GetLogs();
        void Clear();
    }
}
