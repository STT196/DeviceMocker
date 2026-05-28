namespace DeviceMocker.Models
{
    public enum OutputChannelType
    {
        Keyboard,
        Serial,
        // Future
        TcpClient,
        TcpServer,
        Udp,
        HttpWebhook,
        WebSocket,
        File,
        NamedPipe,
        Mqtt,
        Plugin
    }
}
