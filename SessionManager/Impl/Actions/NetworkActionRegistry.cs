using System.Globalization;
using GDictionary = Godot.Collections.Dictionary;

namespace Root.SessionManager.Actions;

public sealed class NetworkActionRegistry
{
	public const int DefaultTokenCost = 1;

	private readonly Dictionary<string, Entry> _entriesByActionId = [];

	public void Register<TAction>(INetworkActionHandler<TAction> handler) where TAction : INetworkAction<TAction>
	{
		var actionId = TAction.ActionId;
		var tokenCost = TAction.TokenCost;

		if (tokenCost <= 0)
			throw new InvalidOperationException(string.Create(
				CultureInfo.InvariantCulture,
				$"Token cost of action id {actionId} must be positive, got {tokenCost}."));

		if (
			!_entriesByActionId.TryAdd(actionId, new Entry(
				tokenCost,
				(payload, senderId) => handler.Validate(TAction.FromPayload(payload), senderId),
				(payload, senderId) => handler.Apply(TAction.FromPayload(payload), senderId))))
			throw new InvalidOperationException($"A handler for action id {actionId} already exists.");
	}

	public int GetTokenCost(string actionId) =>
		_entriesByActionId.TryGetValue(actionId, out var entry) ? entry.TokenCost : DefaultTokenCost;

	public ActionValidation ValidateRaw(string actionId, GDictionary payload, int sourcePeerId) =>
		_entriesByActionId.TryGetValue(actionId, out var entry)
			? entry.Validate(payload, sourcePeerId)
			: ActionValidation.Reject($"Action with id {actionId} not found.");

	public void ApplyRaw(string actionId, GDictionary payload, int sourcePeerId)
	{
		if (_entriesByActionId.TryGetValue(actionId, out var entry))
			entry.Apply(payload, sourcePeerId);
	}

	private readonly record struct Entry(
		int TokenCost,
		Func<GDictionary, int, ActionValidation> Validate,
		Action<GDictionary, int> Apply);
}
