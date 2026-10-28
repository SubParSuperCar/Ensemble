using Avalonia.Animation.Easings;

namespace EnsembleRoot.Ui.Easings;

public sealed class ZeroEasing : Easing
{
	public override double Ease(double progress) => 0;
}
