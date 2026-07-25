using System;

namespace NAudio.CoreAudioApi;

/// <summary>
/// Base event data for an endpoint device notification raised by
/// <see cref="MMDeviceNotificationClient"/>. Carries the endpoint ID string that identifies the
/// device the notification is about. Treat the ID as opaque — pass it to <see cref="MMDeviceEnumerator.GetDevice"/>
/// or compare it against <see cref="MMDevice.ID"/>, but do not parse it.
/// </summary>
public class DeviceNotificationEventArgs : EventArgs
{
    /// <summary>
    /// The endpoint ID string of the device the notification concerns. May be <c>null</c> for a
    /// <see cref="MMDeviceNotificationClient.DefaultDeviceChanged"/> notification when there is no
    /// longer a default device for the reported flow/role.
    /// </summary>
    public string DeviceId { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="DeviceNotificationEventArgs"/> class.
    /// </summary>
    /// <param name="deviceId">The endpoint ID string of the affected device.</param>
    public DeviceNotificationEventArgs(string deviceId)
    {
        DeviceId = deviceId;
    }
}

/// <summary>
/// Event data for <see cref="MMDeviceNotificationClient.DeviceStateChanged"/>.
/// </summary>
public sealed class DeviceStateChangedEventArgs : DeviceNotificationEventArgs
{
    /// <summary>
    /// The new <see cref="DeviceState"/> of the device.
    /// </summary>
    public DeviceState NewState { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="DeviceStateChangedEventArgs"/> class.
    /// </summary>
    /// <param name="deviceId">The endpoint ID string of the affected device.</param>
    /// <param name="newState">The device's new state.</param>
    public DeviceStateChangedEventArgs(string deviceId, DeviceState newState)
        : base(deviceId)
    {
        NewState = newState;
    }
}

/// <summary>
/// Event data for <see cref="MMDeviceNotificationClient.DefaultDeviceChanged"/>.
/// </summary>
public sealed class DefaultDeviceChangedEventArgs : DeviceNotificationEventArgs
{
    /// <summary>
    /// The data-flow direction (render or capture) whose default device changed.
    /// </summary>
    public DataFlow Flow { get; }

    /// <summary>
    /// The device role whose default assignment changed.
    /// </summary>
    public Role Role { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultDeviceChangedEventArgs"/> class.
    /// </summary>
    /// <param name="flow">The data-flow direction that changed.</param>
    /// <param name="role">The device role that changed.</param>
    /// <param name="deviceId">The new default endpoint ID, or <c>null</c> if there is no longer a default.</param>
    public DefaultDeviceChangedEventArgs(DataFlow flow, Role role, string deviceId)
        : base(deviceId)
    {
        Flow = flow;
        Role = role;
    }
}

/// <summary>
/// Event data for <see cref="MMDeviceNotificationClient.PropertyValueChanged"/>.
/// </summary>
public sealed class DevicePropertyChangedEventArgs : DeviceNotificationEventArgs
{
    /// <summary>
    /// The property whose value changed.
    /// </summary>
    public PropertyKey PropertyKey { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="DevicePropertyChangedEventArgs"/> class.
    /// </summary>
    /// <param name="deviceId">The endpoint ID string of the affected device.</param>
    /// <param name="propertyKey">The property key that changed.</param>
    public DevicePropertyChangedEventArgs(string deviceId, PropertyKey propertyKey)
        : base(deviceId)
    {
        PropertyKey = propertyKey;
    }
}
