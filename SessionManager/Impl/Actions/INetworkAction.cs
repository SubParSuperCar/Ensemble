using GArray = Godot.Collections.Array;

namespace EnsembleRoot.SessionManager.Actions;

public interface INetworkAction<out TSelf> where TSelf : INetworkAction<TSelf>
{
	static virtual string Id => typeof(TSelf).Name;
	static virtual int TokenCost => NetworkActionRegistry.DefaultTokenCost;

	static abstract TSelf FromPayload(GArray payload);
	GArray ToPayload();

	ActionValidation Validate(ActionSource source);
	void Apply(ActionSource source);
}
