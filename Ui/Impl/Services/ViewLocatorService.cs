using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Root.Ui.Impl.Abstractions;

namespace Root.Ui.Impl.Services;

// ReSharper disable once ClassNeverInstantiated.Global
public class ViewLocatorService(IServiceProvider services) : ISingletonObject, IServiceBase, IDataTemplate
{
	public Control? Build(object? data)
	{
		if (data is not ViewModelBase viewModel)
			return null;

#pragma warning disable IL3050
		var type = viewModel.GetType();
		var viewInterface = typeof(IViewFor<>).MakeGenericType(type);
#pragma warning restore IL3050
		var view = (Control?)services.GetService(viewInterface);

		if (view is null)
			return new TextBlock { Text = $"View for {type.Name} not found." };

		view.DataContext = data;
		return view;
	}

	public bool Match(object? data) => data is ViewModelBase;
}
