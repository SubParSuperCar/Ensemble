namespace EnsembleRoot.Common.Utils;

/// <summary>
///     The game's one easing curve, quadratic, for Avalonia transitions and Godot tweens alike: each maps linear
///     progress from 0 to 1 onto eased progress.
/// </summary>
public static class Ease
{
	public static double In(double progress) => progress * progress;

	public static double Out(double progress) => progress * (2 - progress);

	public static double InOut(double progress) =>
		progress < 1 / 2d ? 2 * progress * progress : 1 - 2 * (1 - progress) * (1 - progress);
}
