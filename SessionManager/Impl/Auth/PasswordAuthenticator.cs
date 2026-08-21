using System.Security.Cryptography;
using System.Text;
using Godot;
using Root.SessionManager.Api;
using RandomNumberGenerator = System.Security.Cryptography.RandomNumberGenerator;

namespace Root.SessionManager.Auth;

public sealed class PasswordAuthenticator(string password) : IPeerAuthenticator
{
	private const int NonceSize = 16;

	private readonly Dictionary<long, byte[]> _pendingNonces = [];
	private bool _isServer;

	private SceneMultiplayer? _multiplayer;

	// ReSharper disable once AutoPropertyCanBeMadeGetOnly.Global
	public TimeSpan Timeout { get; init; } = TimeSpan.FromSeconds(5);

	public event Action<long, string>? AuthenticationFailed;

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

		_pendingNonces.Clear();
		_multiplayer = null;
	}

	private void OnPeerAuthenticating(long peerId)
	{
		if (!_isServer)
			return;

		var nonce = RandomNumberGenerator.GetBytes(NonceSize);
		_pendingNonces[peerId] = nonce;

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
		if (!_pendingNonces.TryGetValue(peerId, out var nonce))
			return;

		var expected = Hmac(nonce);
		_pendingNonces.Remove(peerId);

		if (data.Length != expected.Length || !CryptographicOperations.FixedTimeEquals(data, expected))
		{
			_multiplayer!.DisconnectPeer((int)peerId);
			return;
		}

		_multiplayer!.CompleteAuth((int)peerId);
	}

	private void HandleClientMessage(long peerId, byte[] data)
	{
		var response = Hmac(data);
		_multiplayer!.SendAuth((int)peerId, response);
		_multiplayer.CompleteAuth((int)peerId);
	}

	private void OnPeerAuthenticationFailed(long peerId)
	{
		_pendingNonces.Remove(peerId);
		AuthenticationFailed?.Invoke(peerId, "Authentication timed out or was rejected.");
	}

	private byte[] Hmac(byte[] nonce) => HMACSHA256.HashData(Encoding.UTF8.GetBytes(password), nonce);
}
