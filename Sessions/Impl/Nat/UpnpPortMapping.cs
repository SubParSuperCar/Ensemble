using System.Net;
using System.Net.Sockets;
using Godot;
using Serilog;

namespace EnsembleRoot.Sessions.Nat;

/// <summary>
///     Forwards a UDP port on the local network's UPnP gateway, so peers outside it can connect without manual port
///     forwarding.
/// </summary>
/// <remarks>
///     UPnP calls block, so they run off the main thread, one at a time across all instances (a closing mapping's
///     removal can't race a new one for the same port); events are raised on the opener's context. The mapping is
///     leased and renewed while open, so it expires on its own if the process dies before disposing it.
/// </remarks>
public sealed class UpnpPortMapping(int port) : IAsyncDisposable
{
	private const string Description = "Ensemble";

	private static readonly TimeSpan Lease = TimeSpan.FromHours(1);
	private static readonly TimeSpan RenewInterval = TimeSpan.FromMinutes(20);

	private static readonly Lock QueueLock = new();
	private static Task _queueTail = Task.CompletedTask;

	private readonly CancellationTokenSource _cts = new();
	private readonly Upnp _upnp = new();

	private bool _isMapped;
	private int _leaseSeconds = (int)Lease.TotalSeconds;

	public async ValueTask DisposeAsync()
	{
		await _cts.CancelAsync().ConfigureAwait(false);
		await Enqueue(Unmap, CancellationToken.None).ConfigureAwait(false);

		_cts.Dispose();
		_upnp.Dispose();
	}

	public event Action<string>? Opened;
	public event Action<string>? Failed;

	public void Open() => _ = OpenAsync(_cts.Token);

	private static bool IsPublic(IPAddress address) =>
		address.AddressFamily is not AddressFamily.InterNetwork ||
		address.GetAddressBytes() is not (
			[0, ..] or [10, ..] or [127, ..] or [169, 254, ..] or [192, 168, ..] or
			[100, >= 64 and < 128, ..] or [172, >= 16 and < 32, ..]);

	private static Task<T> Enqueue<T>(Func<T> work, CancellationToken token)
	{
		lock (QueueLock)
		{
			var task = _queueTail.ContinueWith(
				_ => work(), token, TaskContinuationOptions.LazyCancellation, TaskScheduler.Default);

			_queueTail = task;
			return task;
		}
	}

	private async Task OpenAsync(CancellationToken token)
	{
		try
		{
			var address = await Enqueue(Map, token).ConfigureAwait(true);
			token.ThrowIfCancellationRequested();

			Log.Information(
				"Forwarded UDP port {Port} via UPnP (ExternalAddress={ExternalAddress}, LeaseSeconds={LeaseSeconds})",
				port, address, _leaseSeconds);

			Opened?.Invoke(address);

			while (_leaseSeconds is not 0)
			{
				await Task.Delay(RenewInterval, GTimeProvider, token).ConfigureAwait(true);
				token.ThrowIfCancellationRequested();

				if (await Enqueue(AddMapping, token).ConfigureAwait(true) is not Upnp.UpnpResult.Success and var result)
					Log.Warning("Failed to renew UPnP port mapping (Port={Port}, Result={Result})", port, result);
			}
		}
		catch (OperationCanceledException) when (token.IsCancellationRequested) { }
		catch (InvalidOperationException exception) when (!token.IsCancellationRequested)
		{
			Log.Warning(exception, "Failed to forward UDP port {Port} via UPnP", port);
			Failed?.Invoke(exception.Message);
		}
	}

	private string Map()
	{
		if ((Upnp.UpnpResult)_upnp.Discover() is not Upnp.UpnpResult.Success ||
			_upnp.GetGateway()?.IsValidGateway() is not true)
			throw new InvalidOperationException("No UPnP-enabled router found.");

		var result = AddMapping();

		if (result is Upnp.UpnpResult.OnlyPermanentLeaseSupported)
		{
			_leaseSeconds = 0;
			result = AddMapping();
		}

		if (result is not Upnp.UpnpResult.Success)
			throw new InvalidOperationException($"The router refused to forward the port ({result}).");

		var address = _upnp.QueryExternalAddress();

		if (IPAddress.TryParse(address, out var ip) && !IsPublic(ip))
			throw new InvalidOperationException($"The router is behind another NAT ({address}).");

		return address;
	}

	private Upnp.UpnpResult AddMapping()
	{
		var result = (Upnp.UpnpResult)_upnp.AddPortMapping(port, desc: Description, duration: _leaseSeconds);
		_isMapped |= result is Upnp.UpnpResult.Success;

		return result;
	}

	private Upnp.UpnpResult Unmap()
	{
		if (!_isMapped)
			return Upnp.UpnpResult.Success;

		var result = (Upnp.UpnpResult)_upnp.DeletePortMapping(port);
		_isMapped = false;

		Log.Debug("Removed UPnP port mapping (Port={Port}, Result={Result})", port, result);
		return result;
	}
}
