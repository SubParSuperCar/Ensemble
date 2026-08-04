using DiscordRPC;
using Godot;

namespace Root.Scripts.DiscordRpc;

public partial class DiscordRichPresence : Node
{
	private const string AppId = "1534319171079504002";
	private DiscordRpcClient _client = null!;

	public override void _EnterTree()
	{
		_client = new DiscordRpcClient(AppId);

		_client.SetPresence(new RichPresence
		{
			Timestamps = Timestamps.Now
		});

		_client.Initialize();
	}

	public override void _ExitTree() => _client.Dispose();
}
