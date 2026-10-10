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

	/// <summary>
	///     Lets the host rewrite a validated action (e.g., to filter its text) before applying it. Peers receive and
	///     apply the rewritten action, which must still pass <see cref="Validate" />.
	/// </summary>
	TSelf Rewrite() => (TSelf)this;

	void Apply(ActionSource source);
}
