using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EnsembleRoot.Common.Networking;
using EnsembleRoot.GdCore.Players;
using EnsembleRoot.SessionManager.Api;
using EnsembleRoot.Ui.Impl.Abstractions;
using Godot;

namespace EnsembleRoot.Ui.Impl.ViewModels;

public partial class PlayerListViewModel : ViewModelBase
{
	private const int MaxPingMs = 999;

	private readonly Dictionary<string, PlayerItem> _playersById = [];
	private readonly Dictionary<string, Action> _unsubscribeByPlayerId = [];

	public PlayerListViewModel()
	{
		foreach (var player in GPlayers.GetAll())
			OnPlayerAdded(player);

		GPlayers.Added += OnPlayerAdded;
		GPlayers.Removed += OnPlayerRemoved;

		OnPortMappingChanged();
		GSessionManager.PortMappingChanged += OnPortMappingChanged;
	}

	public ObservableCollection<PlayerItem> Players { get; } = [];

	public bool IsMultiPlayer { get; } = GSessionManager.Mode is SessionMode.MultiPlayer;

	[ObservableProperty] public partial PlayerItem? SelectedPlayer { get; set; }

	[ObservableProperty] public partial string? PortMappingStatus { get; set; }
	[ObservableProperty] public partial string? PortMappingDetail { get; set; }

	[ObservableProperty]
	[NotifyCanExecuteChangedFor(nameof(CopyJoinCodeCommand))]
	public partial string? JoinCode { get; set; }

	protected override void OnDispose()
	{
		GPlayers.Added -= OnPlayerAdded;
		GPlayers.Removed -= OnPlayerRemoved;
		GSessionManager.PortMappingChanged -= OnPortMappingChanged;

		foreach (var unsubscribe in _unsubscribeByPlayerId.Values)
			unsubscribe();
	}

	[RelayCommand(CanExecute = nameof(CanCopyJoinCode))]
	private void CopyJoinCode() => DisplayServer.ClipboardSet(JoinCode!);

	private bool CanCopyJoinCode() => JoinCode is not null;

	private void OnPortMappingChanged()
	{
		var manager = GSessionManager;
		var port = manager.Port;

		JoinCode = manager is { PortMappingState: PortMappingState.Open, ExternalAddress: not "" }
			? new HostEndPoint(manager.ExternalAddress, port).ToString()
			: null;

		PortMappingStatus = manager.PortMappingState switch
		{
			PortMappingState.Pending => "UPnP\u2026",
			PortMappingState.Open => JoinCode is null ? "UPnP OK" : $"Join Code: {JoinCode}",
			PortMappingState.Failed => "UPnP Failed",
			_ => null
		};

		PortMappingDetail = manager.PortMappingState switch
		{
			PortMappingState.Pending => "Asking your router to forward the port\u2026",
			PortMappingState.Open => JoinCode is null
				? string.Create(
					CultureInfo.InvariantCulture,
					$"UDP port {port} forwarded, but the router did not report its address.")
				: "Share this with players outside your network.",
			PortMappingState.Failed => string.Create(
				CultureInfo.InvariantCulture,
				$"{manager.PortMappingError}\nForward UDP port {port} manually to host over the internet."),
			_ => null
		};
	}

	private void OnPlayerAdded(GdPlayer gdPlayer)
	{
		var peer = GSessionManager.GetPeerByPlayerId(gdPlayer.Id);
		var player = new PlayerItem { Name = gdPlayer.Name, Id = gdPlayer.Id, PeerId = peer?.Id ?? None };

		var index = Players
			.TakeWhile(other => ComparePlayers(other, player) < 0)
			.Count();

		Players.Insert(index, player);
		_playersById.Add(gdPlayer.Id, player);

		if (ReferenceEquals(gdPlayer, GPlayers.Local))
			SelectedPlayer = player;

		if (peer is null)
			return;

		OnPingUpdated(peer.PingMs);
		peer.PingUpdated += OnPingUpdated;

		_unsubscribeByPlayerId.Add(gdPlayer.Id, Unsubscribe);

		return;

		void OnPingUpdated(int pingMs)
		{
			player.PingMs = Math.Min(pingMs, MaxPingMs);
		}

		void Unsubscribe()
		{
			peer.PingUpdated -= OnPingUpdated;
		}
	}

	private void OnPlayerRemoved(GdPlayer gdPlayer)
	{
		if (ReferenceEquals(gdPlayer, GPlayers.Local))
			SelectedPlayer = null;

		if (_playersById.Remove(gdPlayer.Id, out var player))
			Players.Remove(player);

		if (_unsubscribeByPlayerId.Remove(gdPlayer.Id, out var unsubscribe))
			unsubscribe();
	}

	private static int ComparePlayers(PlayerItem a, PlayerItem b)
	{
		var nameComparison = string.Compare(a.Name, b.Name, StringComparison.Ordinal);
		return nameComparison is 0 ? a.PeerId.CompareTo(b.PeerId) : nameComparison;
	}
}

public partial class PlayerItem : ObservableObject
{
	public required string Name { get; init; }
	public required string Id { get; init; }
	public int PeerId { get; init; }

	[ObservableProperty] public partial int PingMs { get; set; }
}
