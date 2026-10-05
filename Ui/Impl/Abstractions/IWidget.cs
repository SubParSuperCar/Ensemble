namespace EnsembleRoot.Ui.Impl.Abstractions;

/// <remarks><see cref="Descriptor" /> is virtual, not abstract, so implementers stay registrable for DI.</remarks>
public interface IWidget
{
	static virtual WidgetDescriptor Descriptor => WidgetDescriptor.Default;
}
