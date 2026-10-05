using System.Text.RegularExpressions;
using Avalonia;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EnsembleRoot.Ui.Impl.Abstractions;

namespace EnsembleRoot.Ui.Impl.Services;

/// <summary>A widget type registered with a <see cref="WidgetManagerService" />, whether open or not.</summary>
public sealed partial class WidgetEntry : ObservableObject
{
	private const string ViewModelSuffix = "ViewModel";

	private readonly Func<ViewModelBase> _createContent;
	private readonly WidgetManagerService _manager;

	internal WidgetEntry(
		WidgetManagerService manager,
		Type type,
		WidgetDescriptor descriptor,
		Func<ViewModelBase> createContent)
	{
		_manager = manager;
		_createContent = createContent;

		Type = type;
		Descriptor = descriptor;
		Title = descriptor.Title ?? GetTitle(type);
	}

	public Type Type { get; }
	public WidgetDescriptor Descriptor { get; }
	public string Title { get; }

	[ObservableProperty] public partial bool IsOpen { get; internal set; }

	internal Rect? LastBounds { get; set; }

	[GeneratedRegex("(?<=[a-z0-9])(?=[A-Z])", RegexOptions.None, 100)]
	private static partial Regex WordBoundaryRegex { get; }

	internal ViewModelBase CreateContent() => _createContent();

	[RelayCommand]
	private void Toggle() => _manager.Toggle(this);

	private static string GetTitle(Type type)
	{
		var name = type.Name.EndsWith(ViewModelSuffix, StringComparison.Ordinal)
			? type.Name[..^ViewModelSuffix.Length]
			: type.Name;

		return WordBoundaryRegex.Replace(name, " ");
	}
}
