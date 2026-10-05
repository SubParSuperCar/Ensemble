namespace EnsembleCoreRoot.Globals;

public static class Sentinels
{
	public const int Unlimited = -1;
	public const int Default = 0;
	public const int None = -1;

	public static bool IsLimitReached(int count, int maxCount) => maxCount is not Unlimited && count >= maxCount;
}
