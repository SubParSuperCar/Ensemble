using System.Runtime.CompilerServices;
using Godot;
using Godot.Collections;
using Root.Core.Api.Asset;
using Root.Core.Gd.Util;
using Convert = Root.Core.Gd.Util.Convert;
using Variant = Godot.Variant;

namespace Root.Core.Gd.Asset;

[GlobalClass]
public partial class GdProperties : RefCounted
{
	[Signal]
	public delegate void ChangedEventHandler(string key, Variant value);

	private static readonly ConditionalWeakTable<IProperties, GdProperties> Cache = [];
	private IProperties _properties = null!;

	public static GdProperties From(IProperties properties) => Cache.GetValue(properties,
		static value =>
		{
			var wrapper = new GdProperties { _properties = value };

			value.Changed += (key, propertyValue)
				=> wrapper.EmitSignal(SignalName.Changed, key, propertyValue.ToGodot());

			return wrapper;
		});

	public Variant Get(string key)
		=> _properties.All.TryGetValue(key, out var value) ? value.ToGodot() : default;

	public Dictionary GetAll() => Convert.ToGodotProperties(_properties.All);

	public void Update(string key, Variant value)
		=> _properties.Update(key, value.FromGodot());

	public void UpdateAll(Dictionary properties)
		=> _properties.UpdateAll(Convert.FromGodotProperties(properties));

	public override string ToString() => _properties.ToString()!;
}
