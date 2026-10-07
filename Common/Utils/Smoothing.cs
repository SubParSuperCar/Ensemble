namespace EnsembleRoot.Common.Utils;

public static class Smoothing
{
	/// <summary>Gets a lerp weight that closes the same fraction of the gap per second at any frame rate.</summary>
	public static float GetWeight(float rate, double delta) => 1 - MathF.Exp(-rate * (float)delta);
}
