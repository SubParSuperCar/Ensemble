using Avalonia.Animation.Easings;

namespace EnsembleRoot.Ui.Easings;

public sealed class EaseOutEasing : Easing
{
	public override double Ease(double progress) => Common.Utils.Ease.Out(progress);
}
