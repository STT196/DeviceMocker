using System;
using System.Threading;
using System.Threading.Tasks;
using DeviceMocker.Interfaces;
using DeviceMocker.Models;

namespace DeviceMocker.Core
{
    public class InputRouter
    {
        private readonly OutputChannelManager _channelManager;
        private readonly ILoggerService _logger;

        public InputRouter(OutputChannelManager channelManager, ILoggerService logger)
        {
            _channelManager = channelManager;
            _logger = logger;
        }

        public async Task<OutputResult> RouteAsync(DeviceAction action, CancellationToken cancellationToken = default)
        {
            var channel = _channelManager.GetChannel(action.OutputChannelType);
            if (channel == null)
            {
                var error = $"Output channel '{action.OutputChannelType}' is not available.";
                LogAction(action, false, error);
                return OutputResult.Fail(error);
            }

            try
            {
                var result = await channel.SendAsync(action, cancellationToken);
                LogAction(action, result.Success, result.ErrorMessage);
                return result;
            }
            catch (OperationCanceledException)
            {
                LogAction(action, false, "Operation was cancelled.");
                return OutputResult.Fail("Operation was cancelled.");
            }
            catch (Exception ex)
            {
                var error = $"Error sending via {action.OutputChannelType}: {ex.Message}";
                LogAction(action, false, error);
                return OutputResult.Fail(error);
            }
        }

        private void LogAction(DeviceAction action, bool success, string errorMessage)
        {
            _logger.Log(new DeviceLog
            {
                DeviceName = action.DeviceName,
                DeviceType = action.DeviceType,
                OutputChannelType = action.OutputChannelType,
                Payload = $"{action.Prefix}{action.Payload}{action.Suffix}",
                Success = success,
                ErrorMessage = errorMessage
            });
        }
    }
}
