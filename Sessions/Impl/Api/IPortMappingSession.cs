namespace EnsembleRoot.Sessions.Api;

/// <summary>A session whose host may forward its port on the local network's router.</summary>
public interface IPortMappingSession
{
	PortMappingState PortMappingState { get; }

	/// <summary>The router's public address, if reported, while the mapping is open.</summary>
	string ExternalAddress { get; }

	string PortMappingError { get; }

	event Action PortMappingChanged;
}
