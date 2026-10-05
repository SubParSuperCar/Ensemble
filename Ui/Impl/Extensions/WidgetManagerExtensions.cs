using EnsembleRoot.Ui.Impl.Abstractions;
using EnsembleRoot.Ui.Impl.Services;
using ServiceScan.SourceGenerator;

namespace EnsembleRoot.Ui.Impl.Extensions;

public static partial class WidgetManagerExtensions
{
	/// <summary>Registers every <see cref="IWidget" /> view model, so implementing it is all a widget needs.</summary>
	[ScanForTypes(AssignableTo = typeof(IWidget), HandlerTemplate = "manager.Register<T>()")]
	public static partial void RegisterAll(this WidgetManagerService manager);
}
