using System.Globalization;
using System.Runtime.InteropServices;
using EnsembleRoot.Common.Networking;

// ReSharper disable MemberCanBePrivate.Global

namespace EnsembleRoot.Scripts.Creations.Packets;

[StructLayout(LayoutKind.Auto)]
public readonly record struct CreationPacketHeader(int PlotId, ushort Epoch, ushort Sequence);

/// <summary>One plot's streamed assembly states: a header, a count, then the states, bit-packed.</summary>
public static class CreationPackets
{
	public const int HeaderBits = PlotIdBits + EpochBits + SequenceBits + CountBits;
	public const int MaxPlotId = (1 << PlotIdBits) - 1;
	public const int MaxStateCount = (1 << CountBits) - 1;

	private const int PlotIdBits = 8;
	private const int EpochBits = 16;
	private const int SequenceBits = 16;
	private const int CountBits = 16;

	public static byte[] Write(BitWriter writer, CreationPacketHeader header, ReadOnlySpan<AssemblyState> states)
	{
		if ((uint)header.PlotId > MaxPlotId)
			throw new ArgumentOutOfRangeException(
				nameof(header),
				string.Create(CultureInfo.InvariantCulture, $"Plot ID {header.PlotId} exceeds {MaxPlotId}."));

		if (states.Length > MaxStateCount)
			throw new ArgumentOutOfRangeException(
				nameof(states),
				string.Create(CultureInfo.InvariantCulture, $"{states.Length} states exceed {MaxStateCount}."));

		writer.Clear();
		writer.Write((ulong)header.PlotId, PlotIdBits);
		writer.Write(header.Epoch, EpochBits);
		writer.Write(header.Sequence, SequenceBits);
		writer.Write((ulong)states.Length, CountBits);

		foreach (var state in states)
			state.Write(writer);

		return writer.ToArray();
	}

	/// <summary>Decodes a packet, rejecting truncated or inconsistent ones without throwing.</summary>
	public static bool TryRead(
		ReadOnlySpan<byte> data, out CreationPacketHeader header, ICollection<AssemblyState> states)
	{
		var reader = new BitReader(data);

		header = new CreationPacketHeader(
			(int)reader.Read(PlotIdBits), (ushort)reader.Read(EpochBits), (ushort)reader.Read(SequenceBits));

		var count = (int)reader.Read(CountBits);
		states.Clear();

		if (reader.IsOverrun || (long)count * AssemblyState.Bits > reader.RemainingBits)
			return false;

		for (var i = 0; i < count; i++)
			states.Add(AssemblyState.Read(ref reader));

		return !reader.IsOverrun;
	}
}
