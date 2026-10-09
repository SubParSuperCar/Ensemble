using EnsembleRoot.Sessions.Api;
using Godot;

namespace EnsembleRoot.Sessions;

public partial class SessionManager
{
	[Signal]
	public delegate void PortMappingChangedEventHandler();

	public PortMappingState PortMappingState => PortMapping?.PortMappingState ?? PortMappingState.Disabled;

	/// <inheritdoc cref="IPortMappingSession.ExternalAddress" />
	public string ExternalAddress => PortMapping?.ExternalAddress ?? string.Empty;

	public string PortMappingError => PortMapping?.PortMappingError ?? string.Empty;

	private IPortMappingSession? PortMapping => _session as IPortMappingSession;

	private void AttachPortMapping(ISession session)
	{
		if (session is IPortMappingSession mapping)
			mapping.PortMappingChanged += OnPortMappingChanged;
	}

	private void DetachPortMapping(ISession session)
	{
		if (session is not IPortMappingSession mapping)
			return;

		mapping.PortMappingChanged -= OnPortMappingChanged;
		OnPortMappingChanged();
	}

	private void OnPortMappingChanged() => EmitSignal(SignalName.PortMappingChanged);
}
