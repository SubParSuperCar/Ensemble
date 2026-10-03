using EnsembleRoot.SessionManager.Api;
using EnsembleRoot.SessionManager.Nat;
using Godot;

namespace EnsembleRoot.SessionManager;

public partial class SessionManager
{
	[Signal]
	public delegate void PortMappingChangedEventHandler();

	private static readonly TimeSpan UnmapTimeout = TimeSpan.FromSeconds(1);

	private UpnpPortMapping? _portMapping;
	private Task _unmapping = Task.CompletedTask;

	public PortMappingState PortMappingState { get; private set; }

	/// <remarks>The router's public address, if reported, while <see cref="PortMappingState" /> is open.</remarks>
	public string ExternalAddress { get; private set; } = string.Empty;

	public string PortMappingError { get; private set; } = string.Empty;

	private void OpenPortMapping()
	{
		if (Config is not HostConfig { IsUpnpEnabled: true } config)
			return;

		var mapping = new UpnpPortMapping(config.Port);
		_portMapping = mapping;

		mapping.Opened += address => SetPortMapping(PortMappingState.Open, address, string.Empty);
		mapping.Failed += error => SetPortMapping(PortMappingState.Failed, string.Empty, error);

		SetPortMapping(PortMappingState.Pending, string.Empty, string.Empty);
		mapping.Open();
	}

	private void ClosePortMapping()
	{
		if (_portMapping is not { } mapping)
			return;

		_portMapping = null;
		_unmapping = mapping.DisposeAsync().AsTask();

		SetPortMapping(PortMappingState.Disabled, string.Empty, string.Empty);
	}

	private void SetPortMapping(PortMappingState state, string externalAddress, string error)
	{
		PortMappingState = state;
		ExternalAddress = externalAddress;
		PortMappingError = error;

		EmitSignal(SignalName.PortMappingChanged);
	}
}
