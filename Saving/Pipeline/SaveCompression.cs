using System.IO.Compression;
using ZstdSharp;

namespace Root.Saving.Pipeline;

internal static class SaveCompression
{
	public static Stream? CreateCompressorOrNull(Stream target, CompressionType type, int? level) =>
		type switch
		{
			CompressionType.None => null,
			CompressionType.ZStandard => level is { } value
				? new CompressionStream(target, value, leaveOpen: true)
				: new CompressionStream(target, leaveOpen: true),
			CompressionType.Brotli => new BrotliStream(target, ToBrotliLevel(level), true),
			_ => throw new ArgumentOutOfRangeException(nameof(type))
		};

	public static Stream? CreateDecompressorOrNull(Stream source, CompressionType type) =>
		type switch
		{
			CompressionType.None => null,
			CompressionType.ZStandard => new DecompressionStream(source, leaveOpen: true),
			CompressionType.Brotli => new BrotliStream(source, CompressionMode.Decompress, true),
			_ => throw new ArgumentOutOfRangeException(nameof(type))
		};

	private static CompressionLevel ToBrotliLevel(int? level) =>
		level switch
		{
			null or 2 => CompressionLevel.Optimal,
			<= 0 => CompressionLevel.NoCompression,
			1 => CompressionLevel.Fastest,
			_ => CompressionLevel.SmallestSize
		};
}
