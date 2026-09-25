using Godot.Collections;

namespace EnsembleRoot.SessionManager.Actions;

public interface INetworkAction<out TSelf> where TSelf : INetworkAction<TSelf>
{
	static abstract string ActionId { get; }
	static virtual int TokenCost => NetworkActionRegistry.DefaultTokenCost;

	Dictionary ToPayload();
	static abstract TSelf FromPayload(Dictionary payload);
}
