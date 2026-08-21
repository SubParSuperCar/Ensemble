using GDictionary = Godot.Collections.Dictionary;

namespace Root.SessionManager.Actions;

public sealed class NetworkActionRegistry
{
	private readonly Dictionary<string, Entry> _entries = [];

	// ReSharper disable once UnusedMember.Global
	public void Register<TAction>(INetworkActionHandler<TAction> handler) where TAction : INetworkAction<TAction>
	{
		var actionId = TAction.ActionId;

		if (!_entries.TryAdd(actionId, new Entry(
				(payload, senderId) => handler.Validate(TAction.FromPayload(payload), senderId),
				(payload, senderId) => handler.Apply(TAction.FromPayload(payload), senderId))))
			throw new InvalidOperationException($"A handler is already registered for the '{actionId}' action.");
	}

	public ActionValidation ValidateRaw(string actionId, GDictionary payload, int sourcePeerId) =>
		_entries.TryGetValue(actionId, out var entry)
			? entry.Validate(payload, sourcePeerId)
			: ActionValidation.Reject("Unknown action.");

	public void ApplyRaw(string actionId, GDictionary payload, int sourcePeerId)
	{
		if (_entries.TryGetValue(actionId, out var entry))
			entry.Apply(payload, sourcePeerId);
	}

	private readonly record struct Entry(
		Func<GDictionary, int, ActionValidation> Validate,
		Action<GDictionary, int> Apply);
}
