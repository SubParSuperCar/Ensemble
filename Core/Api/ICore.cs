using EnsembleCoreRoot.Api.Assets;
using EnsembleCoreRoot.Api.Players;
using EnsembleCoreRoot.Api.Plots;

// ReSharper disable UnusedMemberInSuper.Global

namespace EnsembleCoreRoot.Api;

/// <summary>
///     The Godot-agnostic data model for Ensemble. Manages <see cref="IPlayer" />, <see cref="IAsset" />, and
///     <see cref="IPlot" /> objects, plus each plot's <see cref="IInstance" /> and <see cref="IOccupant" /> objects.
/// </summary>
public interface ICore
{
	/// <inheritdoc cref="IPlayers" />
	IPlayers Players { get; }

	/// <inheritdoc cref="IAssets" />
	IAssets Assets { get; }

	/// <inheritdoc cref="IPlots" />
	IPlots Plots { get; }

	void Reset();
}
