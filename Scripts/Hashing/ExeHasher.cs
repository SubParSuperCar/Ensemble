using System.Diagnostics;
using System.Security.Cryptography;
using Godot;
using Root.Autoloading;
using Root.Common.Utils;
using Serilog;
using Environment = System.Environment;
using FileAccess = System.IO.FileAccess;

namespace Root.Scripts.Hashing;

[GlobalClass]
[Autoload(FailurePolicy = AutoloadFailurePolicy.AskUser)]
public partial class ExeHasher : Node, IAutoload
{
	private readonly CancellationTokenSource _cts = new();

	public void Initialize() => _ = HashProcessExecutableAsync();

	// This lets us cancel the operation if the node is somehow removed right after being created.
	public override void _ExitTree() => _cts.Cancel();

	private async Task HashProcessExecutableAsync()
	{
		try
		{
			var exePath = Environment.ProcessPath;
			Log.Information("Process executable path: {Path}", exePath);

			if (!File.Exists(exePath))
			{
				Log.Warning("Process executable not found.");
				return;
			}

			var info = new FileInfo(exePath);
			Log.Information("Process executable size: {Size}", Formatter.FormatBytes((ulong)info.Length));

			Log.Debug("Hashing process executable...");
			var stopwatch = Stopwatch.StartNew();

			var stream = new FileStream(
				exePath,
				FileMode.Open,
				FileAccess.Read,
				FileShare.Read,
				1 << 12,
				FileOptions.Asynchronous);

			await using (stream.ConfigureAwait(false))
			{
				var hashBytes = await SHA256.HashDataAsync(stream, _cts.Token).ConfigureAwait(false);

				stopwatch.Stop();
				Log.Debug("Hashed process executable in {ElapsedMs:F3} ms.", stopwatch.Elapsed.TotalMilliseconds);

				var hashHex = Convert.ToHexString(hashBytes).ToLowerInvariant();
				Log.Information("Process executable SHA-256 digest: {Digest}", hashHex);
			}
		}
		finally
		{
			// Dispose the node to save memory. No reason to keep it alive after it's served its purpose.
			QueueFree();
		}
	}
}
