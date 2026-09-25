using EnsembleCoreRoot;
using EnsembleRoot.Autoloading;
using EnsembleRoot.GdCore.Assets;
using EnsembleRoot.GdCore.Players;
using EnsembleRoot.GdCore.Plots;
using Godot;
using Serilog;

namespace EnsembleRoot.GdCore;

/// <inheritdoc cref="EnsembleCoreRoot.Core" />
[GlobalClass]
[Autoload(Order = AutoloadOrder.Early, FailurePolicy = AutoloadFailurePolicy.FailFast)]
public partial class GdCore : Node, IAutoload
{
	public static GdCore? Instance
	{
		get;
		private set
		{
			field = value;

			Log.Debug(
				"{Class}.{Member} set. (Hash={Hash})",
				nameof(GdCore),
				nameof(Instance),
				value?.GetHashCode());
		}
	}

	// TODO: Expose the backing Core component for each GdCore class?
	// Makes performance-critical code easier instead of walking GdCore from here
	// E.g., in ConstructTool, where it accesses backing Core to optimize quota operations and bypass interop
	/// <inheritdoc cref="EnsembleCoreRoot.Core" />
	public Core Core { get; private set; } = null!;

	/// <inheritdoc cref="GdPlayers" />
	public GdPlayers Players { get; private set; } = null!;

	/// <inheritdoc cref="GdAssets" />
	public GdAssets Assets { get; private set; } = null!;

	/// <inheritdoc cref="GdPlots" />
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
