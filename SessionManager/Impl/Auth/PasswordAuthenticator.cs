using System.Security.Cryptography;
using System.Text;
using EnsembleRoot.SessionManager.Api;
using Godot;
using Serilog;
using RandomNumberGenerator = System.Security.Cryptography.RandomNumberGenerator;

namespace EnsembleRoot.SessionManager.Auth;

// Always installed so both sides agree on the handshake; an open server sends a 1-byte challenge, a locked one a nonce
public sealed class PasswordAuthenticator(string? password) : IPeerAuthenticator
{
	private const int NonceSize = 16;

	private static readonly byte[] OpenChallenge = [0];

	private readonly byte[]? _key = string.IsNullOrEmpty(password) ? null : Encoding.UTF8.GetBytes(password);
	private readonly Dictionary<long, byte[]> _pendingNoncesByPeerId = [];

	private bool _isServer;
	private SceneMultiplayer? _multiplayer;

	public TimeSpan Timeout { get; init; } = TimeSpan.FromSeconds(5);

	public event Action<string>? Failed;

	public void Start(SceneMultiplayer multiplayer, bool isServer)
	{
		_multiplayer = multiplayer;
		_isServer = isServer;

		multiplayer.AuthTimeout = Timeout.TotalSeconds;
		multiplayer.AuthCallback = Callable.From<long, byte[]>(OnAuthMessage);

		multiplayer.PeerAuthenticating += OnPeerAuthenticating;
		multiplayer.PeerAuthenticationFailed += OnPeerAuthenticationFailed;
	}

	public void Stop(SceneMultiplayer multiplayer)
	{
		multiplayer.PeerAuthenticating -= OnPeerAuthenticating;
		multiplayer.PeerAuthenticationFailed -= OnPeerAuthenticationFailed;
		multiplayer.AuthCallback = default;

		_pendingNoncesByPeerId.Clear();
		_multiplayer = null;
	}

	private void OnPeerAuthenticating(long peerId)
	{
		if (!_isServer)
			return;

		if (_key is null)
		{
			_multiplayer!.SendAuth((int)peerId, OpenChallenge);
			_multiplayer.CompleteAuth((int)peerId);

			return;
		}

		var nonce = RandomNumberGenerator.GetBytes(NonceSize);
		_pendingNoncesByPeerId[peerId] = nonce;

		_multiplayer!.SendAuth((int)peerId, nonce);
	}

	private void OnAuthMessage(long peerId, byte[] data)
	{
		if (_isServer)
			HandleServerMessage(peerId, data);
		else
			HandleClientMessage(peerId, data);
	}

	private void HandleServerMessage(long peerId, byte[] data)
	{
		if (!_pendingNoncesByPeerId.Remove(peerId, out var nonce))
			return;

		var expected = HMACSHA256.HashData(_key!, nonce);

		if (data.Length != expected.Length || !CryptographicOperations.FixedTimeEquals(data, expected))
		{
			Log.Warning("Peer {PeerId} failed password authentication", peerId);
			_multiplayer!.DisconnectPeer((int)peerId);

			return;
		}

		_multiplayer!.CompleteAuth((int)peerId);
	}

	private void HandleClientMessage(long peerId, byte[] data)
	{
		if (data.Length is NonceSize)
		{
			if (_key is null)
			{
				Failed?.Invoke("The server requires a password.");
				return;
			}

			_multiplayer!.SendAuth((int)peerId, HMACSHA256.HashData(_key, data));
		}

		_multiplayer!.CompleteAuth((int)peerId);
	}

	private void OnPeerAuthenticationFailed(long peerId)
	{
		_pendingNoncesByPeerId.Remove(peerId);

		if (_isServer)
			Log.Warning("Peer {PeerId} timed out or was rejected during authentication", peerId);
		else
			Failed?.Invoke("Authentication timed out or was rejected (incorrect password?).");
	}
}
