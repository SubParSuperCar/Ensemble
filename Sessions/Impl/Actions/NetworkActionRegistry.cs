using System.Globalization;
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
		var actionId = TAction.Id;
		var tokenCost = TAction.TokenCost;

		if (tokenCost <= 0)
			throw new InvalidOperationException(string.Create(
				CultureInfo.InvariantCulture,
				$"Token cost of action with ID {actionId} must be positive, got {tokenCost}."));

		if (!EntriesByActionId.TryAdd(actionId, new Entry(TAction.GetTokenCost, Execute<TAction>)))
			throw new InvalidOperationException($"Action with ID {actionId} is already registered.");
	}

	public static void Submit<TAction>(this TAction action) where TAction : INetworkAction<TAction> =>
		GSessionManager.Submit(action);

	internal static int GetTokenCost(string actionId, Array<Variant> payload, ActionSource source) =>
		EntriesByActionId.TryGetValue(actionId, out var entry)
			? Math.Clamp(entry.GetCost(payload, source), DefaultTokenCost, MaxTokenCost)
			: DefaultTokenCost;

	/// <summary>
	///     Validates and applies an action. As host, rewrites it first, replacing <paramref name="payload" /> with the
	///     rewritten action's.
	/// </summary>
	internal static ActionValidation Execute(
		string actionId, ref Array<Variant> payload, ActionSource source, bool isHost)
	{
		if (!EntriesByActionId.TryGetValue(actionId, out var entry))
			return ActionValidation.Reject($"Action with ID {actionId} not found.");

		try
		{
			return entry.Run(ref payload, source, isHost);
		}
		catch (Exception exception)
		{
			Log.Error(exception, "Failed to execute action {ActionId} from peer {PeerId}", actionId, source.PeerId);
			return ActionValidation.Reject($"Action with ID {actionId} failed to execute.");
		}
	}

	private static ActionValidation Execute<TAction>(ref Array<Variant> payload, ActionSource source, bool isHost)
		where TAction : INetworkAction<TAction>
	{
		var action = TAction.FromPayload(payload);
		var validation = action.Validate(source);

		if (!validation.IsValid)
			return validation;

		if (isHost)
		{
			action = action.Rewrite(source);
			payload = action.ToPayload();
		}

		action.Apply(source);
		return validation;
	}

	private delegate ActionValidation ExecuteHandler(ref Array<Variant> payload, ActionSource source, bool isHost);

	private readonly record struct Entry(Func<Array<Variant>, ActionSource, int> GetCost, ExecuteHandler Run);
}
