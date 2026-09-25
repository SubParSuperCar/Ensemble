using Avalonia.Animation.Easings;

namespace EnsembleRoot.Ui.Impl.Easings;

public sealed class ZeroEasing : Easing
{
	public override double Ease(double progress) => 0;
}
