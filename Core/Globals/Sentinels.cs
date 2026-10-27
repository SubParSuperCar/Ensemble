using System.Runtime.CompilerServices;

namespace EnsembleCoreRoot.Globals;

public static class Sentinels
{
	public const int Unlimited = -1;
	public const int Default = 0;
	public const int None = -1;

	public static bool IsLimitReached(int count, int maxCount) => maxCount is not Unlimited && count >= maxCount;

	internal static void ThrowIfInvalidMaxCount(
		int? maxCount, [CallerArgumentExpression(nameof(maxCount))] string? paramName = null)
	{
		if (maxCount is { } count and not Unlimited)
			ArgumentOutOfRangeException.ThrowIfNegative(count, paramName);
	}
}
