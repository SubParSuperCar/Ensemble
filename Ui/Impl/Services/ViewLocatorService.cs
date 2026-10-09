using Avalonia.Controls;
using Avalonia.Controls.Templates;
using EnsembleRoot.Ui.Impl.Abstractions;

namespace EnsembleRoot.Ui.Impl.Services;

public sealed class ViewLocatorService(IServiceProvider services) : ISingletonObject, IServiceBase, IDataTemplate
{
	public Control? Build(object? data)
	{
		if (data is not ViewModelBase viewModel)
			return null;

		var type = viewModel.GetType();
#pragma warning disable IL3050
		var viewType = typeof(IViewFor<>).MakeGenericType(type);
#pragma warning restore IL3050

		if (services.GetService(viewType) is not Control view)
			return new TextBlock { Text = $"View for {type.Name} not found." };

		view.DataContext = viewModel;
		return view;
	}

	public bool Match(object? data) => data is ViewModelBase;
}
