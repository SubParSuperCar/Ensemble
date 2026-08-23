using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Styling;

namespace Root.Ui.Impl;

public class App : Application
{
	public override void Initialize() => AvaloniaXamlLoader.Load(this);

	public override void OnFrameworkInitializationCompleted()
	{
		base.OnFrameworkInitializationCompleted();

		var registry = AvaloniaPropertyRegistry.Instance;
		var tooltipStyle = new Style(x => x.OfType<ToolTip>());

		if (registry.FindRegistered(typeof(TextOptions), "TextRenderingMode") is { } property)
			tooltipStyle.Setters.Add(new Setter(property, TextRenderingMode.Antialias));

		Current?.Styles.Add(tooltipStyle);
	}
}
