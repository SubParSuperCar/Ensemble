using System.Runtime.CompilerServices;
using EnsembleRoot.GdCore.Assets;
using EnsembleRoot.Sessions.Actions;
using Godot;
using Godot.Collections;
using static EnsembleRoot.Sessions.Actions.ActionValidation;

namespace EnsembleRoot.Replication.Actions;

/// <remarks>Only existing properties can be set, and only to values of the same type.</remarks>
public readonly record struct SetPropertiesAction(IReadOnlyList<PropertyEdit> Edits)
	: INetworkAction<SetPropertiesAction>
{
	public const int MaxCount = 256;

	// As cheap as removals, so a marquee selection can be edited at once
	private const int CountPerToken = 16;

	public static int GetTokenCost(Array<Variant> payload, ActionSource source) =>
		(payload.Count + CountPerToken - 1) / CountPerToken;

	public static SetPropertiesAction FromPayload(Array<Variant> payload) =>
		new([.. payload.Select(static edit => PropertyEdit.FromPayload(edit.AsGodotArray<Variant>()))]);

	public Array<Variant> ToPayload() => [.. Edits.Select(static edit => (Variant)edit.ToPayload())];

	public ActionValidation Validate(ActionSource source)
	{
		if (source.Plot is not { IsSpawned: false } plot)
			return Reject("Plot not editable.");

		if (Edits.Count is 0 or > MaxCount)
			return Reject("Edit count invalid.");

		var isAnyFound = false;

		foreach (var edit in Edits)
		{
			if (edit.Properties.Count is 0)
				return Reject("No properties.");

			// Others may remove some first; the rest still change
			if (edit.Instance.Resolve(plot) is not { } instance)
				continue;

			if (GdProperties.Check(instance.Properties.Source.All, edit.Properties) is { } reason)
				return Reject(reason);

			isAnyFound = true;
		}

		return isAnyFound ? Accept : Reject("Instances not found.");
	}

	public void Apply(ActionSource source)
	{
		var plot = source.Plot!;

		foreach (var edit in Edits)
			edit.Instance.Resolve(plot)?.Properties.UpdateAll(edit.Properties);
	}

	[ModuleInitializer]
	internal static void Register() => NetworkActionRegistry.Register<SetPropertiesAction>();
}
