using CoreRoot;
using Godot;
using Root.Autoloading;
using Root.GdCore.Assets;
using Root.GdCore.Players;
using Root.GdCore.Plots;
using Serilog;

namespace Root.GdCore;

/// <inheritdoc cref="CoreRoot.Api.ICore" />
[GlobalClass]
[Autoload(Order = sbyte.MinValue + 1, FailurePolicy = AutoloadFailurePolicy.FailFast)]
public partial class GdCore : Node, IAutoload
{
	public Core Core { get; private set; } = null!;

	public static GdCore? Instance
	{
		get;
		private set
		{
			field = value;

			Log.Debug("{Class}.{Member} set. (Hash={Hash})",
				nameof(GdCore),
				nameof(Instance),
				value?.GetHashCode());
		}
	}

	public GdPlayers Players { get; private set; } = null!;
	public GdAssets Assets { get; private set; } = null!;
	public GdPlots Plots { get; private set; } = null!;

	public void Initialize()
	{
		Core = new Core(timeProvider: GTimeProvider);

		Players = GdPlayers.From(Core.Players);
		Assets = GdAssets.From(Core.Assets);
		Plots = GdPlots.From(Core.Plots);
	}

	public override void _EnterTree() => Instance = this;

	public override void _ExitTree()
	{
		if (ReferenceEquals(Instance, this))
			Instance = null;
	}

	public void Reset() => Core.Reset();
}
