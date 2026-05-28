# Output Channels

DeviceMocker separates **devices** from **output channels**. The same barcode data can be sent via keyboard, serial, TCP, UDP, or HTTP.

## Available Channels

### Keyboard Wedge (Default)
- Types keystrokes into the active Windows application
- Uses Windows `SendInput` API for reliability
- Supports: plain text, special keys, shortcuts, function keys
- Best for: POS apps, text fields, any app that accepts keyboard input

### Serial (COM Port)
- Sends text data to a physical or virtual COM port
- Configurable: baud rate, parity, data bits, stop bits, line ending
- Best for: Scale software, serial barcode readers, Arduino/microcontrollers

### TCP Client
- Connects to a TCP server and sends the payload
- Configurable: host and port (default: 127.0.0.1:9000)
- Best for: Network-based device integration, socket servers

### UDP
- Sends a UDP datagram to the target
- Configurable: host and port (default: 127.0.0.1:9001)
- Best for: Lightweight network protocols, IoT devices

### HTTP Webhook
- POSTs a JSON payload to any URL
- JSON body includes: deviceId, deviceName, deviceType, payload, timestamp
- Configurable: URL and HTTP method (POST/PUT)
- Best for: REST APIs, backend testing, webhook integrations

## Architecture

```
Device Module → DeviceAction → InputRouter → OutputChannelManager → IOutputChannel → Target
```

The `InputRouter` looks at the `OutputChannelType` in the `DeviceAction` and routes it to the correct channel.
