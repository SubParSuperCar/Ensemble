using DiscordRPC;
using Godot;
using Root.Autoloading;
using Serilog;

namespace Root.Scripts.DiscordRichPresence;

[GlobalClass]
[Autoload(Scope = AutoloadScope.Client, Order = 1, FailurePolicy = AutoloadFailurePolicy.LogAndContinue)]
public partial class DiscordRpc : Node, IAutoload
{
	private const string AppId = "1534319171079504002";
	private DiscordRpcClient? _client;

	public void Initialize()
	{
		Log.Debug("App ID: {AppId}", AppId);
		_client = new DiscordRpcClient(AppId);

		_client.SetPresence(new RichPresence
		{
			Timestamps = Timestamps.Now
		});

		_client.Initialize();
	}

	public override void _ExitTree()
	{
		Log.Debug("Terminating {Client}...", _client);
		_client?.Dispose();
	}
}
