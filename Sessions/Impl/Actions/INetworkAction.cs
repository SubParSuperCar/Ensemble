using Godot;
using Godot.Collections;

namespace EnsembleRoot.Sessions.Actions;

public interface INetworkAction<out TSelf> where TSelf : INetworkAction<TSelf>
{
	static virtual string Id => typeof(TSelf).Name;
	static virtual int TokenCost => NetworkActionRegistry.DefaultTokenCost;

	static abstract TSelf FromPayload(Array<Variant> payload);
	Array<Variant> ToPayload();

	ActionValidation Validate(ActionSource source);
	void Apply(ActionSource source);
}
