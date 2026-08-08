using System.Security.Cryptography;
using Godot;
using Serilog;
using Environment = System.Environment;
using FileAccess = System.IO.FileAccess;

namespace Root.Scripts.Hasher;

[GlobalClass]
public partial class ExeHasher : Node
{
	public override void _Ready() => _ = OnReady();

	private async Task OnReady()
	{
		try
		{
			var exePath = Environment.ProcessPath;
			Log.Debug("Process executable path: {Path}", exePath);

			if (!File.Exists(exePath))
			{
				Log.Warning("Process executable not found; skipping hash verification");
				return;
			}

			await Task.Delay((int)TimeSpan.MillisecondsPerSecond).ConfigureAwait(false);

			Log.Debug("Hashing process executable...");

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

				Log.Debug("Process executable SHA-256 hash: {Digest}", hashHex);
			}
		}
		finally
		{
			QueueFree();
		}
	}
}
