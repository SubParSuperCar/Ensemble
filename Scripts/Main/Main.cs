using Godot;
using Root.Globals;

// ReSharper disable MemberCanBePrivate.Global

namespace Root.Scripts.Main;

public partial class Main : Node
{
	private Node? _gameScene;

	[Export] public PackedScene GameScene { get; set; } = null!;

	public override void _EnterTree()
	{
		GdGlobals.Mp.SessionStarted += OnSessionStarted;
		GdGlobals.Mp.SessionStopped += OnSessionStopped;
	}

	public override void _ExitTree()
	{
		GdGlobals.Mp.SessionStarted -= OnSessionStarted;
		GdGlobals.Mp.SessionStopped -= OnSessionStopped;
	}

	public void OnSessionStarted()
	{
		if (_gameScene is not null)
			return;

		_gameScene = GameScene.Instantiate();
		GetTree().Root.AddChild(_gameScene);
	}

	public void OnSessionStopped()
	{
		if (_gameScene is null)
			return;

		_gameScene.QueueFree();
		_gameScene = null;
	}
}
