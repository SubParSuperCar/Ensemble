using DiscordRPC;
using DiscordRPC.Logging;
using DiscordRPC.Message;
using Godot;
using Root.Autoloading;
using Serilog;

namespace Root.Scripts.DiscordRichPresence;

// TODO: Fix benign errors in AOT export builds caused by IPC named pipe socket exceptions
[GlobalClass]
[Autoload(
	Scope = AutoloadScope.RegularClient,
	Order = AutoloadOrder.Standard + 1,
	FailurePolicy = AutoloadFailurePolicy.LogAndContinue)]
public partial class DiscordRpc : Node, IAutoload
{
	private const string AppId = "1534319171079504002";

	private DiscordRpcClient? _client;

	public void Initialize()
	{
		Log.Debug("Discord RPC app ID: {AppId}", AppId);

		_client = new DiscordRpcClient(AppId)
		{
			Logger = new ConsoleLogger(LogLevel.Info, true)
		};
		_client.OnReady += OnReady;

		_client.SetPresence(new RichPresence
		{
			Timestamps = Timestamps.Now,
			Details = "By SubParSuperCar on GitHub",
			DetailsUrl = "https://github.com/SubParSuperCar/Ensemble"
		});

		_client.Initialize();
	}

	public override void _ExitTree()
	{
		Log.Debug("Terminating {$Client}...", _client);
		_client?.Dispose();
	}

	private static void OnReady(object? sender, ReadyMessage e) =>
		Log.Debug("Connected to Discord with user: {UserName} ({SnowflakeId})", e.User.Username, e.User.ID);
}
