using System.Diagnostics;
using Godot;
using Serilog;
using FileAccess = Godot.FileAccess;

namespace EnsembleRoot.Sessions.Auth;

/// <summary>The host's self-signed DTLS certificate, created once and kept, so clients could pin it later.</summary>
public static class HostCertificate
{
	private const int KeySize = 2048;
	private const string IssuerName = "CN=Ensemble Host";

	// UTC (YYYYMMDDhhmmss), open-ended so a pinned certificate never needs renewing
	private const string NotBefore = "20000101000000";
	private const string NotAfter = "99991231235959";

	public static TlsOptions ServerOptions => field ??= LoadOrCreate();

	private static TlsOptions LoadOrCreate()
	{
		var key = new CryptoKey();
		var certificate = new X509Certificate();

		if (
			FileAccess.FileExists(HostKeyPath) && FileAccess.FileExists(HostCertificatePath) &&
			key.Load(HostKeyPath) is Error.Ok && certificate.Load(HostCertificatePath) is Error.Ok)
			return TlsOptions.Server(key, certificate);

		Log.Debug("Creating host certificate...");
		var stopwatch = Stopwatch.StartNew();

		var crypto = new Crypto();
		key = crypto.GenerateRsa(KeySize);
		certificate = crypto.GenerateSelfSignedCertificate(key, IssuerName, NotBefore, NotAfter);

		stopwatch.Stop();
		Log.Debug("Created host certificate in {ElapsedMs:F3} ms", stopwatch.Elapsed.TotalMilliseconds);

		// A new certificate each session still encrypts, so failing to keep it isn't fatal
		if (key.Save(HostKeyPath) is not Error.Ok || certificate.Save(HostCertificatePath) is not Error.Ok)
			Log.Warning("Failed to save host certificate to {Path}", HostCertificatePath);

		return TlsOptions.Server(key, certificate);
	}
}
