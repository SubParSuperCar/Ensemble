using System.Diagnostics;
using System.Security.Cryptography;
using Godot;
using Root.Autoloading;
using Serilog;
using Environment = System.Environment;
using FileAccess = System.IO.FileAccess;

namespace Root.Scripts.Hashing;

[GlobalClass]
[Autoload(FailurePolicy = AutoloadFailurePolicyEnum.LogAndContinue)]
public partial class ExeHasher : Node, IAutoload
{
	private readonly CancellationTokenSource _cts = new();

	public void Initialize() => _ = InitializeAsync();

	public override void _ExitTree() => _cts.Cancel();

	private async Task InitializeAsync()
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

				var hashHex = Convert.ToHexString(hashBytes);
				Log.Information("Process executable SHA-256 digest: {Digest}", hashHex);
			}
		}
		finally
		{
			QueueFree();
		}
	}
}
