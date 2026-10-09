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

	public void Update(string key, Variant value) => Source.Update(key, value.FromGodot());
	public void UpdateAll(Dictionary properties) => Source.UpdateAll(Converter.FromGodotProperties(properties));

	public override string ToString() => Source.ToString()!;
}
