# Adding a New Device

## Steps

### 1. Create the device folder
```
Devices/MyDevice/
├── MyDeviceDevice.cs
├── MyDeviceViewModel.cs
├── MyDeviceView.xaml
└── MyDeviceView.xaml.cs
```

### 2. Add the DeviceType enum value
In `Models/DeviceType.cs`:
```csharp
public enum DeviceType
{
    // ... existing types
    MyDevice
}
```

### 3. Create the Device class
```csharp
public class MyDeviceDevice : IDeviceModule
{
    private readonly InputRouter _router;
    public string Id => "my-device";
    public string Name => "My Device";
    public DeviceType DeviceType => DeviceType.MyDevice;

    public MyDeviceDevice(InputRouter router) => _router = router;

    public async Task<OutputResult> SendAsync(DeviceAction action, CancellationToken ct)
    {
        action.DeviceId = Id;
        action.DeviceName = Name;
        action.DeviceType = DeviceType;
        return await _router.RouteAsync(action, ct);
    }
}
```

### 4. Create the ViewModel
Extend `ViewModelBase`, add properties and commands for your device's UI.

### 5. Create the View
Create a XAML UserControl with your device's UI, bound to the ViewModel.

### 6. Register in ServiceLocator
```csharp
MyDeviceDevice = new MyDeviceDevice(Router);
DeviceManager.Register(MyDeviceDevice);
```

### 7. Add navigation in DevicesViewModel
```csharp
case "my-device":
    _mainVm.NavigateToDevice(new MyDeviceViewModel(), "My Device");
    break;
```

### 8. Add DataTemplate in MainWindow.xaml
```xml
<DataTemplate DataType="{x:Type mydev:MyDeviceViewModel}">
    <mydev:MyDeviceView/>
</DataTemplate>
```

### 9. Add a card in DevicesView.xaml
Add a button card with `CommandParameter="my-device"`.
