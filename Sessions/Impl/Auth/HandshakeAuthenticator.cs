using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using Godot;
using Konscious.Security.Cryptography;
using Serilog;
using RandomNumberGenerator = System.Security.Cryptography.RandomNumberGenerator;

namespace EnsembleRoot.Sessions.Auth;

/// <summary>Authenticates every peer, locked or not, so both sides always agree on the handshake.</summary>
/// <remarks>
///     The challenge is [IsLocked][Salt and nonce (if locked)][Version (UTF-8)]; clients reject version mismatches.
///     Clients of a locked host answer with the nonce's HMAC-SHA256, keyed with the password's Argon2id hash under the
///     host's per-session salt, so a sniffed exchange costs an attacker one Argon2id run per guessed password.
/// </remarks>
public sealed class HandshakeAuthenticator(SceneMultiplayer multiplayer, string version, string? password)
{
	private const byte OpenFlag = 0;
	private const byte LockedFlag = 1;

	private const int SaltSize = 16;
	private const int NonceSize = 16;
	private const int KeySize = 32;
	private const int MaxShownVersionLength = 32;
	private const int MaxPendingPerAddress = 4;

	// About 0.1 s on a desktop CPU
	private const int Argon2MemoryKiB = 32 * 1024;
	private const int Argon2Iterations = 2;

	private static readonly TimeSpan AuthTimeout = TimeSpan.FromSeconds(5);

	// Slows online password guessing to a round of tries per address per cooldown, a round being the challenges an
	// address may have pending at once (a few, for players sharing a network)
	private static readonly TimeSpan FailureCooldown = TimeSpan.FromSeconds(3);

	private readonly Dictionary<string, long> _lastFailureTimestampsByAddress = new(StringComparer.Ordinal);

	// NFC, so the same password typed on any OS matches
	private readonly byte[]? _password =
		string.IsNullOrEmpty(password) ? null : Encoding.UTF8.GetBytes(password.Normalize(NormalizationForm.FormC));

	private readonly Dictionary<long, (byte[] Nonce, string? Address)> _pendingChallengesByPeerId = [];
	private readonly byte[] _version = Encoding.UTF8.GetBytes(version);

	private bool _hasAnswered;
	private bool _hasSentPassword;
	private bool _isServer;
	private bool _isStarted;
	private byte[]? _key;
	private byte[] _salt = [];

	public event Action<string>? Failed;

	public void StartAuth(bool isServer)
	{
		_isServer = isServer;
		_isStarted = true;
		_hasAnswered = false;
		_hasSentPassword = false;

		if (isServer && _password is not null)
		{
			_salt = RandomNumberGenerator.GetBytes(SaltSize);
			_key = DeriveKey(_password, _salt);
		}

		multiplayer.AuthTimeout = AuthTimeout.TotalSeconds;
		multiplayer.AuthCallback = Callable.From<long, byte[]>(OnAuthMessage);

		multiplayer.PeerAuthenticating += OnPeerAuthenticating;
		multiplayer.PeerAuthenticationFailed += OnPeerAuthenticationFailed;
	}

	public void StopAuth()
	{
		multiplayer.PeerAuthenticating -= OnPeerAuthenticating;
		multiplayer.PeerAuthenticationFailed -= OnPeerAuthenticationFailed;
		multiplayer.AuthCallback = default;

		_isStarted = false;
		_lastFailureTimestampsByAddress.Clear();
		_pendingChallengesByPeerId.Clear();
	}

	private void OnPeerAuthenticating(long peerId)
	{
		if (!_isServer)
			return;

		if (_key is null)
		{
			multiplayer.SendAuth((int)peerId, [OpenFlag, .. _version]);
			multiplayer.CompleteAuth((int)peerId);

			return;
		}

		var address = GetAddressKey(peerId);

		if (
			address is not null &&
			_lastFailureTimestampsByAddress.TryGetValue(address, out var lastFailure) &&
			Stopwatch.GetElapsedTime(lastFailure) < FailureCooldown)
		{
			Log.Warning("Peer {PeerId} retried a password too soon", peerId);
			multiplayer.DisconnectPeer((int)peerId);

			return;
		}

		if (address is not null && CountPendingChallenges(address) >= MaxPendingPerAddress)
		{
			Log.Warning("Peer {PeerId} joined while its address had too many pending challenges", peerId);
			multiplayer.DisconnectPeer((int)peerId);

			return;
		}

		var nonce = RandomNumberGenerator.GetBytes(NonceSize);
		_pendingChallengesByPeerId[peerId] = (nonce, address);

		multiplayer.SendAuth((int)peerId, [LockedFlag, .. _salt, .. nonce, .. _version]);
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
		if (!_pendingChallengesByPeerId.Remove(peerId, out var pending))
			return;

		var expected = HMACSHA256.HashData(_key!, pending.Nonce);

		if (data.Length != expected.Length || !CryptographicOperations.FixedTimeEquals(data, expected))
		{
			Log.Warning("Peer {PeerId} failed password authentication", peerId);
			RecordFailure(peerId);
			multiplayer.DisconnectPeer((int)peerId);

			return;
		}

		multiplayer.CompleteAuth((int)peerId);
	}

	private void HandleClientMessage(long peerId, byte[] data)
	{
		var isLocked = data is [LockedFlag, ..];
		var headerSize = isLocked ? 1 + SaltSize + NonceSize : 1;

		if (data.Length < headerSize)
		{
			Failed?.Invoke("Handshake malformed.");
			return;
		}

		if (!data.AsSpan(headerSize).SequenceEqual(_version))
		{
			Failed?.Invoke($"Version mismatch (host {FormatVersion(data.AsSpan(headerSize))}, local {version}).");
			return;
		}

		if (!isLocked)
		{
			multiplayer.CompleteAuth((int)peerId);
			return;
		}

		if (_password is null)
		{
			Failed?.Invoke("Password required.");
			return;
		}

		// Only the first challenge is answered, so a hostile host can't queue Argon2id runs
		if (_hasAnswered)
			return;

		_hasAnswered = true;
		_ = AnswerAsync(peerId, data[1..(1 + SaltSize)], data[(1 + SaltSize)..headerSize]);
	}

	// Argon2id takes about 0.1 s, so it runs off the main thread
	private async Task AnswerAsync(long peerId, byte[] salt, byte[] nonce)
	{
		byte[] key;

		try
		{
			key = await Task.Run(() => DeriveKey(_password!, salt)).ConfigureAwait(true);
		}
		catch (Exception exception)
		{
			Log.Error(exception, "Failed to derive the password key");
			Failed?.Invoke("Authentication failed.");

			return;
		}

		if (!_isStarted)
			return;

		multiplayer.SendAuth((int)peerId, HMACSHA256.HashData(key, nonce));
		_hasSentPassword = true;

		multiplayer.CompleteAuth((int)peerId);
	}

	private void OnPeerAuthenticationFailed(long peerId)
	{
		_pendingChallengesByPeerId.Remove(peerId);

		if (_isServer)
			Log.Warning("Peer {PeerId} timed out or was rejected during authentication", peerId);
		// Hosts only drop clients that answered the challenge when the password is wrong
		else
			Failed?.Invoke(_hasSentPassword ? "Incorrect password." : "Authentication failed.");
	}

	private string? GetAddressKey(long peerId) =>
		(multiplayer.MultiplayerPeer as ENetMultiplayerPeer)?.GetPeer((int)peerId)?.GetRemoteAddress() is { } address &&
		AddressKey.TryCreate(address, out var key)
			? key
			: null;

	private int CountPendingChallenges(string address) =>
		_pendingChallengesByPeerId.Values.Count(pending =>
			string.Equals(pending.Address, address, StringComparison.Ordinal));

	private void RecordFailure(long peerId)
	{
		if (GetAddressKey(peerId) is not { } address)
			return;

		foreach (var (stale, timestamp) in _lastFailureTimestampsByAddress)
			if (Stopwatch.GetElapsedTime(timestamp) >= FailureCooldown)
				_lastFailureTimestampsByAddress.Remove(stale);

		_lastFailureTimestampsByAddress[address] = Stopwatch.GetTimestamp();
	}

	private static byte[] DeriveKey(byte[] password, byte[] salt)
	{
		using var argon2 = new Argon2id(password);
		argon2.Salt = salt;
		argon2.MemorySize = Argon2MemoryKiB;
		argon2.Iterations = Argon2Iterations;
		argon2.DegreeOfParallelism = 1;

		return argon2.GetBytes(KeySize);
	}

	// The host's version is shown to the user, so it must be short, printable text
	private static string FormatVersion(ReadOnlySpan<byte> bytes) =>
		bytes.Length is > 0 and <= MaxShownVersionLength && !bytes.ContainsAnyExceptInRange((byte)' ', (byte)'~')
			? Encoding.ASCII.GetString(bytes)
			: "unknown";
}
