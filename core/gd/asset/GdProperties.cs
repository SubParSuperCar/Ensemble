using System.Runtime.CompilerServices;
using Godot;
using Godot.Collections;
using Root.Core.Api.Asset;
using Root.Core.Gd.Util;
using Variant = Godot.Variant;

namespace Root.Core.Gd.Asset;

public partial class GdProperties : RefCounted
{
	[Signal]
	public delegate void ChangedEventHandler(string key, Variant value);

	private static readonly ConditionalWeakTable<IProperties, GdProperties> Wrappers = [];
	private IProperties _source = null!;

	public static GdProperties From(IProperties properties) =>
		Wrappers.GetValue(properties,
			static source =>
			{
				var wrapper = new GdProperties { _source = source };

				source.Changed += (key, value)
					=> wrapper.EmitSignal(SignalName.Changed, key, value.ToGodot());

				return wrapper;
			});

	public Variant GetValue(string key) => _source.All.TryGetValue(key, out var value) ? value.ToGodot() : default;
	public Dictionary GetAll() => Converter.ToGodotProperties(_source.All);

	public void Update(string key, Variant value) => _source.Update(key, value.FromGodot());
	public void UpdateAll(Dictionary properties) => _source.UpdateAll(Converter.FromGodotProperties(properties));

	public override string ToString() => _source.ToString()!;
}
