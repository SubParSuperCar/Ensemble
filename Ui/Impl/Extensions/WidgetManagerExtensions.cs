using EnsembleRoot.Ui.Abstractions;
using EnsembleRoot.Ui.Services;
using ServiceScan.SourceGenerator;

namespace EnsembleRoot.Ui.Extensions;

public static partial class WidgetManagerExtensions
{
	[ScanForTypes(AssignableTo = typeof(IWidget), HandlerTemplate = "manager.Register<T>()")]
	public static partial void RegisterAll(this WidgetManagerService manager);
}
