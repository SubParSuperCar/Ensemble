using Avalonia;
using Avalonia.Markup.Xaml;

namespace Root.Ui.Impl;

public class App : Application
{
	public override void Initialize() => AvaloniaXamlLoader.Load(this);
}
