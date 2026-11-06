using Avalonia.Animation.Easings;

namespace EnsembleRoot.Ui.Easings;

public sealed class EaseInOutEasing : Easing
{
	public override double Ease(double progress) => Common.Utils.Ease.InOut(progress);
}
