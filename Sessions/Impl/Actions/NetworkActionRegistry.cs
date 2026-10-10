using System.Globalization;
using Godot;
using Godot.Collections;
using Serilog;

namespace EnsembleRoot.Sessions.Actions;

public static class NetworkActionRegistry
{
	public const int DefaultTokenCost = 1;

	private static readonly System.Collections.Generic.Dictionary<string, Entry> EntriesByActionId =
		new(StringComparer.Ordinal);

	public static void Register<TAction>() where TAction : INetworkAction<TAction>
	{
		var actionId = TAction.Id;
		var tokenCost = TAction.TokenCost;

		if (tokenCost <= 0)
			throw new InvalidOperationException(string.Create(
				CultureInfo.InvariantCulture,
				$"Token cost of action with id {actionId} must be positive, got {tokenCost}."));

		if (!EntriesByActionId.TryAdd(actionId, new Entry(tokenCost, Execute<TAction>)))
			throw new InvalidOperationException($"Action with id {actionId} is already registered.");
	}

	internal static int GetTokenCost(string actionId) =>
		EntriesByActionId.TryGetValue(actionId, out var entry) ? entry.TokenCost : DefaultTokenCost;

	/// <summary>
	///     Validates and applies an action. As host, rewrites it first, replacing <paramref name="payload" /> with the
	///     rewritten action's.
	/// </summary>
	internal static ActionValidation Execute(
		string actionId, ref Array<Variant> payload, ActionSource source, bool isHost)
	{
		if (!EntriesByActionId.TryGetValue(actionId, out var entry))
			return ActionValidation.Reject($"Action with id {actionId} not found.");

		try
		{
			return entry.Execute(ref payload, source, isHost);
		}
		catch (Exception exception)
		{
			Log.Error(exception, "Failed to execute action {ActionId} from peer {PeerId}", actionId, source.PeerId);
			return ActionValidation.Reject($"Action with id {actionId} failed to execute.");
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
			action = action.Rewrite();
			payload = action.ToPayload();
		}

		action.Apply(source);
		return validation;
	}

	extension<TAction>(TAction action) where TAction : INetworkAction<TAction>
	{
		public void Submit() => GSessionManager.Submit(action);
	}

	private delegate ActionValidation ExecuteHandler(ref Array<Variant> payload, ActionSource source, bool isHost);

	// ReSharper disable once MemberHidesStaticFromOuterClass
	private readonly record struct Entry(int TokenCost, ExecuteHandler Execute);
}
