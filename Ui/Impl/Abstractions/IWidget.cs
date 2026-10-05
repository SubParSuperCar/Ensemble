namespace EnsembleRoot.Ui.Impl.Abstractions;

/// <summary>A view model that can be shown as a widget, at most once per widget manager.</summary>
/// <remarks>
///     <see cref="Descriptor" /> is virtual rather than abstract, so view models can still be registered for DI by
///     their interfaces.
/// </remarks>
public interface IWidget
{
	static virtual WidgetDescriptor Descriptor => WidgetDescriptor.Default;
}
