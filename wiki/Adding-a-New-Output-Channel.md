# Adding a New Output Channel

## Steps

### 1. Add the OutputChannelType enum value
In `Models/OutputChannelType.cs`:
```csharp
public enum OutputChannelType
{
    // ... existing types
    MyChannel
}
```

### 2. Create the service class
```csharp
public class MyChannelOutputService : IOutputChannel
{
    public string Id => "my-channel";
    public string Name => "My Channel";
    public OutputChannelType ChannelType => OutputChannelType.MyChannel;

    public async Task<OutputResult> SendAsync(DeviceAction action, CancellationToken ct)
    {
        try
        {
            var payload = $"{action.Prefix}{action.Payload}{action.Suffix}";
            // Send payload to your target
            return OutputResult.Ok();
        }
        catch (Exception ex)
        {
            return OutputResult.Fail($"Error: {ex.Message}");
        }
    }
}
```

### 3. Register in ServiceLocator
```csharp
MyChannelOutput = new MyChannelOutputService();
ChannelManager.Register(MyChannelOutput);
```

That's it. Any device can now use `OutputChannelType.MyChannel` and the `InputRouter` will route to your new channel automatically.
