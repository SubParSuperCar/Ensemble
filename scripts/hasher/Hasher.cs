using System.Security.Cryptography;
using Godot;
using Serilog;
using Environment = System.Environment;
using FileAccess = System.IO.FileAccess;

namespace Root.scripts.hasher;

public partial class Hasher : Node
{
	public override void _Ready() => _ = OnReady();

	private static async Task OnReady()
	{
		var exePath = Environment.ProcessPath;
		if (!File.Exists(exePath))
			return;

		await Task.Delay(1000).ConfigureAwait(false);

		using var sha256 = SHA256.Create();
		var stream = new FileStream(
			exePath,
			FileMode.Open,
			FileAccess.Read,
			FileShare.Read,
			1 << 12,
			FileOptions.Asynchronous);

		await using (stream.ConfigureAwait(false))
		{
			var hashBytes = await sha256.ComputeHashAsync(stream).ConfigureAwait(false);
			var hashHex = Convert.ToHexString(hashBytes);

			Log.Debug("Exe SHA-256: {Hash}", hashHex);
		}
	}
}
