using System.Security.Cryptography;
using System.Text;
using EnsembleRoot.SessionManager.Api;
using Godot;
using Serilog;
using RandomNumberGenerator = System.Security.Cryptography.RandomNumberGenerator;

namespace EnsembleRoot.SessionManager.Auth;

/// <summary>
///     Authenticates every peer, locked or not, so both sides always agree on the handshake.
/// </summary>
/// <remarks>
///     The challenge is [IsLocked][Nonce (if locked)][Version (UTF-8)]; clients reject version mismatches.
/// </remarks>
public sealed class HandshakeAuthenticator(string version, string? password) : IPeerAuthenticator
{
	private const byte OpenFlag = 0;
	private const byte LockedFlag = 1;

	private const int NonceSize = 16;

	private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(5);

	private readonly byte[]? _key = string.IsNullOrEmpty(password) ? null : Encoding.UTF8.GetBytes(password);
	private readonly Dictionary<long, byte[]> _pendingNoncesByPeerId = [];
	private readonly byte[] _version = Encoding.UTF8.GetBytes(version);

	private bool _isServer;
	private SceneMultiplayer? _multiplayer;

	public event Action<string>? Failed;

	public void StartAuth(SceneMultiplayer multiplayer, bool isServer)
	{
		_multiplayer = multiplayer;
		_isServer = isServer;

		multiplayer.AuthTimeout = Timeout.TotalSeconds;
		multiplayer.AuthCallback = Callable.From<long, byte[]>(OnAuthMessage);

		multiplayer.PeerAuthenticating += OnPeerAuthenticating;
		multiplayer.PeerAuthenticationFailed += OnPeerAuthenticationFailed;
	}

	public void StopAuth(SceneMultiplayer multiplayer)
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
			_multiplayer!.SendAuth((int)peerId, [OpenFlag, .. _version]);
			_multiplayer.CompleteAuth((int)peerId);

			return;
		}

		var nonce = RandomNumberGenerator.GetBytes(NonceSize);
		_pendingNoncesByPeerId[peerId] = nonce;

		_multiplayer!.SendAuth((int)peerId, [LockedFlag, .. nonce, .. _version]);
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
		var isLocked = data is [LockedFlag, ..];
		var headerSize = isLocked ? 1 + NonceSize : 1;

		if (data.Length < headerSize)
		{
			Failed?.Invoke("Handshake malformed.");
			return;
		}

		if (!data.AsSpan(headerSize).SequenceEqual(_version))
		{
			Failed?.Invoke(
				$"Version mismatch (host {Encoding.UTF8.GetString(data.AsSpan(headerSize))}, local {version}).");
			return;
		}

		if (isLocked)
		{
			if (_key is null)
			{
				Failed?.Invoke("Password required.");
				return;
			}

			_multiplayer!.SendAuth((int)peerId, HMACSHA256.HashData(_key, data.AsSpan(1, NonceSize)));
		}

		_multiplayer!.CompleteAuth((int)peerId);
	}

	private void OnPeerAuthenticationFailed(long peerId)
	{
		_pendingNoncesByPeerId.Remove(peerId);

		if (_isServer)
			Log.Warning("Peer {PeerId} timed out or was rejected during authentication", peerId);
		else
			Failed?.Invoke("Authentication failed.");
	}
}
