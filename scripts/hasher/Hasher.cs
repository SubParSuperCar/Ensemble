using System.Security.Cryptography;
using Godot;
using Serilog;
using Environment = System.Environment;
using FileAccess = System.IO.FileAccess;

namespace Root.Scripts.Hasher;

public partial class Hasher : Node
{
	public override void _Ready() => _ = OnReady();

	private async Task OnReady()
	{
		if (Environment.ProcessPath is not { } exePath || !File.Exists(exePath))
			return;

		await Task.Delay((int)TimeSpan.MillisecondsPerSecond).ConfigureAwait(false);

		var stream = new FileStream(
			exePath,
			FileMode.Open,
			FileAccess.Read,
			FileShare.Read,
			1 << 12,
			FileOptions.Asynchronous);

		await using (stream.ConfigureAwait(false))
		{
			var hashBytes = await SHA256.HashDataAsync(stream).ConfigureAwait(false);
			var hashHex = Convert.ToHexString(hashBytes);

			Log.Debug("Process executable path & SHA-256 hash, respectively: {Path}, {Hash}", exePath, hashHex);
		}

		QueueFree();
	}
}
