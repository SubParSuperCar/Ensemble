using Avalonia.Animation.Easings;

namespace EnsembleRoot.Ui.Easings;

public sealed class EaseInEasing : Easing
{
	public override double Ease(double progress) => Common.Utils.Ease.In(progress);
}
