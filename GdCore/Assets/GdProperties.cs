using System.Runtime.CompilerServices;
using EnsembleCoreRoot.Api.Assets;
using EnsembleRoot.GdCore.Utils;
using Godot;
using Godot.Collections;

namespace EnsembleRoot.GdCore.Assets;

/// <inheritdoc cref="IProperties" />
public partial class GdProperties : RefCounted
{
	[Signal]
	public delegate void ChangedEventHandler(string key, Variant value);

	private static readonly ConditionalWeakTable<IProperties, GdProperties> Wrappers = [];

	/// <inheritdoc cref="IProperties" />
	public IProperties Source { get; private init; } = null!;

	public static GdProperties From(IProperties properties) =>
		Wrappers.GetValue(properties,
			static source =>
			{
				var wrapper = new GdProperties { Source = source };

				source.Changed += (key, value) => wrapper.EmitSignal(SignalName.Changed, key, value.ToGodot());

				return wrapper;
			});

	public Variant GetValue(string key) => Source.All.TryGetValue(key, out var value) ? value.ToGodot() : default;
	public Dictionary GetAll() => Converter.ToGodotProperties(Source.All);

	public void Update(string key, Variant value)
	{
		Validate(key, value);
		Source.Update(key, value.FromGodot());
	}

	public void UpdateAll(Dictionary properties)
	{
		foreach (var (key, value) in properties)
			Validate(key.AsString(), value);

		Source.UpdateAll(Converter.FromGodotProperties(properties));
	}

	/// <returns>Whether the property exists and the value is valid for it, and so was set.</returns>
	public bool TryUpdate(string key, Variant value)
	{
		if (!Source.All.TryGetValue(key, out var current) || Check(key, value, current) is not null)
			return false;

		Source.Update(key, value.FromGodot());
		return true;
	}

	public override string ToString() => Source.ToString()!;

	/// <summary>
	///     Gets why <paramref name="properties" /> can't update properties holding the <paramref name="current" />
	///     values, or null if they can.
	/// </summary>
	public static string? Check(IReadOnlyDictionary<string, CoreVariant> current, Dictionary properties)
	{
		foreach (var (key, value) in properties)
		{
			var name = key.VariantType is Variant.Type.String or Variant.Type.StringName ? key.AsString() : null;

			if (name is null || !current.TryGetValue(name, out var existing))
				return "Property not found.";

			if (Check(name, value, existing) is { } reason)
				return reason;
		}

		return null;
	}

	// Unsupported Godot types convert to Null, so they would otherwise pass for a Null property's value
	private static string? Check(string key, Variant value, CoreVariant current) =>
		value.IsConvertible()
			? PropertyRules.Check(key, value.FromGodot(), current)
			: $"Property {key} type not supported.";

	private void Validate(string key, Variant value)
	{
		if (Source.All.TryGetValue(key, out var current) && Check(key, value, current) is { } reason)
			throw new ArgumentException(reason, nameof(value));
	}
}
