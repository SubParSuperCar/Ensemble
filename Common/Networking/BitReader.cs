namespace EnsembleRoot.Common.Networking;

/// <summary>
///     Unpacks what a <see cref="BitWriter" /> packed. Reading past the end yields zeros and sets
///     <see cref="IsOverrun" /> instead of throwing, so hostile input is checked once after decoding.
/// </summary>
public ref struct BitReader(ReadOnlySpan<byte> data)
{
	private readonly ReadOnlySpan<byte> _data = data;
	private int _bitCount;

	public readonly int RemainingBits => _data.Length * 8 - _bitCount;
	public bool IsOverrun { get; private set; }

	public ulong Read(int bits)
	{
		ArgumentOutOfRangeException.ThrowIfNegativeOrZero(bits);
		ArgumentOutOfRangeException.ThrowIfGreaterThan(bits, 64);

		if (bits > RemainingBits)
		{
			IsOverrun = true;
			_bitCount = _data.Length * 8;
			return 0;
		}

		var value = 0UL;

		for (var read = 0; read < bits;)
		{
			var offset = _bitCount & 7;
			var count = Math.Min(8 - offset, bits - read);

			value |= (ulong)((_data[_bitCount >> 3] >> offset) & (int)GetMask(count)) << read;
			read += count;
			_bitCount += count;
		}

		return value;
	}

	public long ReadSigned(int bits)
	{
		var shift = 64 - bits;
		return (long)(Read(bits) << shift) >> shift;
	}

	internal static ulong GetMask(int bits) => bits is 64 ? ulong.MaxValue : (1UL << bits) - 1;
}
