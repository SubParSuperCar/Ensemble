using System.Runtime.CompilerServices;
using Godot;
using Godot.Collections;
using Root.Core.Api.Asset;
using Root.Core.Gd.Util;
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
		static p =>
		{
			var gdProperties = new GdProperties { _properties = p };
			p.Changed += (key, value) => gdProperties.EmitSignal(SignalName.Changed, key, value.ToGodot());

			return gdProperties;
		});

	public Variant Get(string key)
		=> _properties.All.TryGetValue(key, out var value) ? value.ToGodot() : default;

	public Dictionary GetAll()
		=> GdConvert.ToGodotProperties(_properties.All);

	public void Update(string key, Variant value)
		=> _properties.Update(key, value.FromGodot());

	public void UpdateAll(Dictionary properties)
		=> _properties.UpdateAll(GdConvert.FromGodotProperties(properties));

	public override string ToString() => _properties.ToString()!;
}
