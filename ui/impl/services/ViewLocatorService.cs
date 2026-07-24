using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Root.Ui.Impl.Abstractions;

namespace Root.Ui.Impl.Services;

// ReSharper disable once ClassNeverInstantiated.Global
public class ViewLocatorService : ISingletonObject, IServiceBase, IDataTemplate
{
	public Control? Build(object? data)
	{
		if (data is null)
			return null;

		var name = data.GetType().FullName!.Replace("ViewModel", "View", StringComparison.OrdinalIgnoreCase);
#pragma warning disable IL2026
		var type = typeof(ViewLocatorService)
			.Assembly
			.GetType(name);
#pragma warning restore IL2026

		if (type is null)
			return new TextBlock { Text = $"View with name {name} not found" };

#pragma warning disable IL2072
		var view = (Control?)Activator.CreateInstance(type);
#pragma warning restore IL2072
		view?.DataContext = data;

		return view;
	}

	public bool Match(object? data) => data is ViewModelBase;
}
