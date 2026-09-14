using Avalonia.Controls;
using Avalonia.Controls.Embedding;
using Avalonia.Input;

namespace Estragonia;

/// <summary>
///     A <see cref="TopLevel" /> used with Godot.
///     This is implicitly created by <see cref="AvaloniaControl" />.
/// </summary>
public sealed class GodotTopLevel : EmbeddableControlRoot
{
	static GodotTopLevel()
	{
		// TopLevel uses Cycle navigation mode, but the focus should be able to leave Avalonia
		// and return to Godot, so use Continue instead
		KeyboardNavigation.TabNavigationProperty.OverrideDefaultValue<GodotTopLevel>(KeyboardNavigationMode.Continue);
	}

	internal GodotTopLevel(GodotTopLevelImpl impl)
		: base(impl)
	{
		Impl = impl;
	}

	internal GodotTopLevelImpl Impl { get; }
}
