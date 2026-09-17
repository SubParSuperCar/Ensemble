using CoreRoot.Api.Assets;
using CoreRoot.Api.Players;
using CoreRoot.Api.Plots;

// ReSharper disable UnusedMemberInSuper.Global

namespace CoreRoot.Api;

/// <summary>
///     The Godot-agnostic data model for Ensemble.
///     Provides resources for managing
///     <see cref="IPlayer" />,
///     <see cref="IAsset" />, [Asset] <see cref="IInstance" />,
///     <see cref="IPlot" />, and [Plot] <see cref="IOccupant" /> objects.
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
