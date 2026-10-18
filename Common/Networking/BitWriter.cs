namespace EnsembleRoot.Common.Networking;

/// <summary>
///     Packs values into bytes at bit granularity, least significant bits first. Reusable via <see cref="Clear" />.
/// </summary>
public sealed class BitWriter(int capacity = 256)
{
	private int _bitCount;
	private byte[] _buffer = new byte[capacity];

	private int ByteCount => (_bitCount + 7) / 8;

	public void Clear()
	{
		Array.Clear(_buffer, 0, ByteCount);
		_bitCount = 0;
	}

	public void Write(ulong value, int bits)
	{
		ArgumentOutOfRangeException.ThrowIfNegativeOrZero(bits);
		ArgumentOutOfRangeException.ThrowIfGreaterThan(bits, 64);

		EnsureCapacity(_bitCount + bits);

		for (var written = 0; written < bits;)
		{
			var offset = _bitCount & 7;
			var count = Math.Min(8 - offset, bits - written);

			_buffer[_bitCount >> 3] |= (byte)(((value >> written) & BitReader.GetMask(count)) << offset);
			written += count;
			_bitCount += count;
		}
	}

	public void WriteSigned(long value, int bits) => Write((ulong)value & BitReader.GetMask(bits), bits);

	public byte[] ToArray() => _buffer[..ByteCount];

	private void EnsureCapacity(int bitCount)
	{
		var byteCount = (bitCount + 7) / 8;

		if (byteCount > _buffer.Length)
			Array.Resize(ref _buffer, Math.Max(byteCount, _buffer.Length * 2));
	}
}
