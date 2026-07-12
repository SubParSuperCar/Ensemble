using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Root.Ui.Impl.Abstractions;

namespace Root.Ui.Impl.Services;

// Your typical run-of-the-mill ViewLocator, but DI compatible
// ReSharper disable once ClassNeverInstantiated.Global
public class ViewLocatorService : ISingletonObject, IServiceBase, IDataTemplate
{
	public Control? Build(object? data)
	{
		if (data is null)
			return null;

		var name = data.GetType().FullName!.Replace("ViewModel", "View", StringComparison.OrdinalIgnoreCase);
		var type = Type.GetType(name);

		if (type is null)
			return new TextBlock { Text = $"View with name {name} not found" };

		// Since it's a view, we don't have to worry about dependencies
		var view = (Control?)Activator.CreateInstance(type);
		view?.DataContext = data;

		return view;
	}

	public bool Match(object? data) => data is ViewModelBase;
}
