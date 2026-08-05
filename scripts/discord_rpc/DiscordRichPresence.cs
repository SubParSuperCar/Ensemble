using DiscordRPC;
using Godot;
using Serilog;

namespace Root.Scripts.DiscordRpc;

public partial class DiscordRichPresence : Node
{
	private const string AppId = "1534319171079504002";
	private DiscordRpcClient _client = null!;

	public override void _EnterTree()
	{
		Log.Debug("Initializing {Class} (App ID: {AppId})", nameof(DiscordRichPresence), AppId);
		_client = new DiscordRpcClient(AppId);

		_client.SetPresence(new RichPresence
		{
			Timestamps = Timestamps.Now
		});

		_client.Initialize();
		Log.Debug("Initialized {Class}", nameof(DiscordRichPresence));
	}

	public override void _ExitTree()
	{
		_client.Dispose();
		Log.Debug("Terminated {Class}", nameof(DiscordRichPresence));
	}
}
