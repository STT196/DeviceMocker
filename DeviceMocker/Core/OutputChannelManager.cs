using System.Collections.Generic;
using System.Linq;
using DeviceMocker.Interfaces;
using DeviceMocker.Models;

namespace DeviceMocker.Core
{
    public class OutputChannelManager
    {
        private readonly Dictionary<OutputChannelType, IOutputChannel> _channels = new();

        public void Register(IOutputChannel channel)
        {
            _channels[channel.ChannelType] = channel;
        }

        public IOutputChannel? GetChannel(OutputChannelType type)
        {
            _channels.TryGetValue(type, out var channel);
            return channel;
        }

        public IReadOnlyList<IOutputChannel> GetAllChannels()
        {
            return _channels.Values.ToList().AsReadOnly();
        }
    }
}
