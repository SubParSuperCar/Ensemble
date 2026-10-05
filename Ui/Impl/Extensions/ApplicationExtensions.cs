using Avalonia;
using Avalonia.Controls;

namespace EnsembleRoot.Ui.Impl.Extensions;

public static class ApplicationExtensions
{
	extension(Application application)
	{
		public TimeSpan? TransitionDuration => application.FindResource("TransitionDuration") as TimeSpan?;
	}
}
