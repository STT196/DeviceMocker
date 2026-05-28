using System;
using System.Collections.Generic;
using System.Linq;
using DeviceMocker.Interfaces;
using DeviceMocker.Models;

namespace DeviceMocker.Services
{
    public class LoggerService : ILoggerService
    {
        private readonly List<DeviceLog> _logs = new();
        private readonly object _lock = new();
        private const int MaxLogs = 1000;

        public event Action? LogsUpdated;

        public void Log(DeviceLog log)
        {
            lock (_lock)
            {
                _logs.Insert(0, log);
                if (_logs.Count > MaxLogs)
                    _logs.RemoveAt(_logs.Count - 1);
            }
            LogsUpdated?.Invoke();
        }

        public IReadOnlyList<DeviceLog> GetLogs()
        {
            lock (_lock)
            {
                return _logs.ToList().AsReadOnly();
            }
        }

        public void Clear()
        {
            lock (_lock)
            {
                _logs.Clear();
            }
            LogsUpdated?.Invoke();
        }
    }
}
