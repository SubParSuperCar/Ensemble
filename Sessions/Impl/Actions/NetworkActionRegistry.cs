using Godot;
using Godot.Collections;
using Serilog;

namespace EnsembleRoot.Sessions.Actions;

public static class NetworkActionRegistry
{
	public const int DefaultTokenCost = 1;

	/// <summary>A peer's whole token bucket, which refills in 5 s.</summary>
	public const int MaxTokenCost = 100;

	private static readonly System.Collections.Generic.Dictionary<string, Entry> EntriesByActionId =
		new(StringComparer.Ordinal);

	public static void Register<TAction>() where TAction : INetworkAction<TAction>
	{
		if (!EntriesByActionId.TryAdd(TAction.Id, new Entry(TAction.GetTokenCost, Execute<TAction>)))
			throw new InvalidOperationException($"Action with ID {TAction.Id} is already registered.");
	}

	public static void Submit<TAction>(this TAction action) where TAction : INetworkAction<TAction> =>
		GSessionManager.Submit(action);

	internal static int GetTokenCost(string actionId, Array<Variant> payload, ActionSource source) =>
		EntriesByActionId.TryGetValue(actionId, out var entry)
			? Math.Clamp(entry.GetCost(payload, source), DefaultTokenCost, MaxTokenCost)
			: DefaultTokenCost;

	/// <summary>
	///     Validates and applies an action. On the server, rewrites it first, replacing <paramref name="payload" />
	///     with the rewritten action's.
	/// </summary>
	internal static ActionValidation Execute(
		string actionId, ref Array<Variant> payload, ActionSource source, bool isServer)
	{
		if (!EntriesByActionId.TryGetValue(actionId, out var entry))
			return ActionValidation.Reject($"Action with ID {actionId} not found.");

		try
		{
			return entry.Run(ref payload, source, isServer);
		}
		catch (Exception exception)
		{
			Log.Error(exception, "Failed to execute action {ActionId} from peer {PeerId}", actionId, source.PeerId);
			return ActionValidation.Reject($"Action with ID {actionId} failed to execute.");
		}
	}

	private static ActionValidation Execute<TAction>(ref Array<Variant> payload, ActionSource source, bool isServer)
		where TAction : INetworkAction<TAction>
	{
		var action = TAction.FromPayload(payload);
		var validation = action.Validate(source);

		if (!validation.IsValid)
			return validation;

		if (isServer)
		{
			action = action.Rewrite(source);
			payload = action.ToPayload();
		}

		action.Apply(source);
		return validation;
	}

	private delegate ActionValidation ExecuteHandler(ref Array<Variant> payload, ActionSource source, bool isServer);

	private readonly record struct Entry(Func<Array<Variant>, ActionSource, int> GetCost, ExecuteHandler Run);
}
