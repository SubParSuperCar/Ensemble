using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Net.NetworkInformation;
using EnsembleRoot.Common.Networking;
using EnsembleRoot.Scripts.Sessions;
using EnsembleRoot.Sessions.Api;
using Godot;
using Lua;
using Serilog;
using static EnsembleRoot.Scripts.Sessions.CommandLineSession;
using static EnsembleRoot.Sessions.SessionManager;

namespace EnsembleRoot.Execution;

// Sessions started here go through the same SessionManager calls as the menu, which follows them
public static partial class LuaExecutor
{
	private const string NameKey = "name";
	private const string PortKey = "port";
	private const string PasswordKey = "password";
	private const string MaxClientsKey = "max_clients";
	private const string UpnpKey = "upnp";
	private const string ProfanityFilterKey = "profanity_filter";

	// The headless server needs a few seconds to load the world
	private static readonly TimeSpan ServerStartTimeout = TimeSpan.FromSeconds(30);
	private static readonly TimeSpan ServerPollInterval = TimeSpan.FromSeconds(1 / 4d);

	private static readonly string[] HostKeys = [NameKey, PortKey, PasswordKey, MaxClientsKey, UpnpKey];
	private static readonly string[] JoinKeys = [NameKey, PasswordKey];
	private static readonly string[] ServerKeys = [PortKey, PasswordKey, MaxClientsKey, UpnpKey, ProfanityFilterKey];

	private static readonly List<Process> Servers = [];

	private static bool _isServerCleanupHooked;

	private static ValueTask<int> host_mp(LuaFunctionExecutionContext context, CancellationToken cancellationToken)
	{
		var table = context.IsArgumentOmitted(0) ? null : context.GetArgument<LuaTable>(0);

		if (!TryReadSessionOptions(table, HostKeys, out var options) || GetDisplayName(options) is not { } name)
			return context.ReturnNothing();

		var config = new HostConfig(
			options.Port ?? SessionPreferences.HostPort ?? DefaultPort, options.Password, options.MaxClients,
			IsUpnpEnabled: options.IsUpnpEnabled ?? SessionPreferences.IsUpnpEnabled);

		GSessionManager.HostMultiPlayer(config, name);

		return context.ReturnNothing();
	}

	/// <summary>
	///     Starts a headless dedicated server on this machine and returns its process ID once it listens, so it can be
	///     joined right away. Servers stop with <c>stop_servers()</c> or when the game quits.
	/// </summary>
	private static async ValueTask<int> host_server(
		LuaFunctionExecutionContext context, CancellationToken cancellationToken)
	{
		var table = context.IsArgumentOmitted(0) ? null : context.GetArgument<LuaTable>(0);

		if (!TryReadSessionOptions(table, ServerKeys, out var options))
			return context.Return();

		var port = options.Port ?? DefaultPort;

		if (IsUdpPortBound(port) is true)
		{
			Log.Error("UDP port {Port} is already in use", port);
			return context.Return();
		}

		var process = StartServer(port, options);
		var start = Stopwatch.GetTimestamp();

		Log.Information("Starting a headless server on port {Port} (PID {ProcessId})...", port, process.Id);

		for (bool? isBound; (isBound = IsUdpPortBound(port)) is not true;)
		{
			if (isBound is null)
			{
				Log.Warning("Can't tell when the server listens; give it a few seconds before joining");
				return context.Return(process.Id);
			}

			if (process.HasExited)
			{
				Log.Error("The server exited with code {ExitCode} before listening (see its log)", process.ExitCode);
				return context.Return();
			}

			if (Stopwatch.GetElapsedTime(start) > ServerStartTimeout)
			{
				Log.Error("The server didn't listen within {Seconds} s", ServerStartTimeout.TotalSeconds);
				return context.Return(process.Id);
			}

			await DelayAsync(ServerPollInterval, cancellationToken).ConfigureAwait(true);
		}

		Log.Information(
			"Headless server listening on port {Port} (PID {ProcessId}); join it with {Function}(\"{Address}\")",
			port, process.Id, nameof(join_mp), string.Create(CultureInfo.InvariantCulture, $"127.0.0.1:{port}"));

		return context.Return(process.Id);
	}

	private static ValueTask<int> join_mp(LuaFunctionExecutionContext context, CancellationToken cancellationToken)
	{
		var address = context.GetArgument<string>(0);
		var table = context.IsArgumentOmitted(1) ? null : context.GetArgument<LuaTable>(1);

		if (!TryReadSessionOptions(table, JoinKeys, out var options) || GetDisplayName(options) is not { } name)
			return context.ReturnNothing();

		// Without a port, the default one
		if (
			!HostEndPoint.TryParse(address, out var endPoint) &&
			!HostEndPoint.TryParse(
				string.Create(CultureInfo.InvariantCulture, $"{address}:{DefaultPort}"), out endPoint))
		{
			Log.Error("Invalid address {Address}: use host or host:port", address);
			return context.ReturnNothing();
		}

		GSessionManager.JoinMultiPlayer(new JoinConfig(endPoint.Host, endPoint.Port, options.Password), name);

		return context.ReturnNothing();
	}

	private static ValueTask<int> start_sp(LuaFunctionExecutionContext context, CancellationToken cancellationToken)
	{
		var name = context.GetArgumentOrDefault(0, SessionPreferences.DisplayName);

		if (IsValidDisplayName(name))
			GSessionManager.StartSinglePlayer(name);
		else
			LogInvalidDisplayName(name);

		return context.ReturnNothing();
	}

	private static ValueTask<int> stop_servers(LuaFunctionExecutionContext context, CancellationToken cancellationToken)
	{
		Log.Information("Stopped {Count} headless server(s)", StopServers());

		return context.ReturnNothing();
	}

	private static ValueTask<int> stop_session(LuaFunctionExecutionContext context, CancellationToken cancellationToken)
	{
		if (GSessionManager.Mode is SessionMode.Inactive)
			Log.Error("No session to stop");
		else
			GSessionManager.StopSession();

		return context.ReturnNothing();
	}

	private static Process StartServer(int port, SessionOptions options)
	{
		var info = new ProcessStartInfo(OS.GetExecutablePath())
		{
			UseShellExecute = false,
			RedirectStandardOutput = true,
			RedirectStandardError = true
		};

		// Exported games find their data themselves; project runs (e.g., from the editor) are pointed at it
		if (!OS.HasFeature("template"))
		{
			info.ArgumentList.Add("--path");
			info.ArgumentList.Add(ProjectSettings.GlobalizePath(ResourceScheme));
		}

		info.ArgumentList.Add("--headless");
		info.ArgumentList.Add("--");
		info.ArgumentList.Add(FormatArg(PortArg, port.ToString(CultureInfo.InvariantCulture)));

		if (options.Password is { } password)
			info.ArgumentList.Add(FormatArg(PasswordArg, password));

		if (options.MaxClients is { } maxClients)
			info.ArgumentList.Add(FormatArg(MaxClientsArg, maxClients.ToString(CultureInfo.InvariantCulture)));

		if (options.IsUpnpEnabled is true)
			info.ArgumentList.Add(FormatArg(UpnpArg));

		if (options.IsProfanityFilterEnabled is { } isEnabled)
			info.ArgumentList.Add(FormatArg(ProfanityFilterArg, isEnabled ? "true" : "false"));

		var process = Process.Start(info) ?? throw new InvalidOperationException("Failed to start the server.");

		// Drained and dropped, since the server writes the usual log files; a full pipe would stall it
		process.BeginOutputReadLine();
		process.BeginErrorReadLine();

		Servers.Add(process);
		HookServerCleanup();

		return process;
	}

	// A server outliving the game would keep its port
	private static void HookServerCleanup()
	{
		if (_isServerCleanupHooked)
			return;

		_isServerCleanupHooked = true;

		((SceneTree)Engine.GetMainLoop()).Root.TreeExiting += static () => StopServers();
		AppDomain.CurrentDomain.ProcessExit += static (_, _) => StopServers();
	}

	private static int StopServers()
	{
		var count = 0;

		foreach (var process in Servers)
		{
			try
			{
				if (!process.HasExited)
				{
					process.Kill(true);
					count++;
				}
			}
			catch (Exception exception) when (exception is InvalidOperationException or Win32Exception)
			{
				Log.Warning(exception, "Failed to stop the server with PID {ProcessId}", process.Id);
			}

			process.Dispose();
		}

		Servers.Clear();

		return count;
	}

	// Null if the OS can't say
	private static bool? IsUdpPortBound(int port)
	{
		try
		{
			return IPGlobalProperties.GetIPGlobalProperties().GetActiveUdpListeners()
				.Any(endPoint => endPoint.Port == port);
		}
		catch (Exception exception) when (exception is PlatformNotSupportedException or NetworkInformationException)
		{
			return null;
		}
	}

	private static string? GetDisplayName(SessionOptions options)
	{
		var name = options.Name ?? SessionPreferences.DisplayName;

		if (IsValidDisplayName(name))
			return name;

		LogInvalidDisplayName(name);
		return null;
	}

	private static void LogInvalidDisplayName(string name) =>
		Log.Error(
			"Invalid display name {Name}: use up to {Max} ASCII letters and digits, or none", name,
			MaxDisplayNameLength);

	private static bool TryReadSessionOptions(LuaTable? table, string[] keys, out SessionOptions options)
	{
		options = default;

		foreach (var (key, value) in table ?? [])
		{
			var name = key.Type is LuaValueType.String ? key.Read<string>() : null;
			var text = value.Type is LuaValueType.String ? value.Read<string>() : null;
			var number = value.Type is LuaValueType.Number ? ToInt(value) : null;
			bool? flag = value.Type is LuaValueType.Boolean ? value.Read<bool>() : null;

			SessionOptions? updated = name is not null && keys.Contains(name)
				? name switch
				{
					NameKey when text is not null => options with { Name = text },
					PortKey when number is > 0 and <= ushort.MaxValue => options with { Port = number },
					PasswordKey when text is { Length: > 0 } => options with { Password = text },
					MaxClientsKey when number is > 0 => options with { MaxClients = number },
					UpnpKey when flag is not null => options with { IsUpnpEnabled = flag },
					ProfanityFilterKey when flag is not null => options with { IsProfanityFilterEnabled = flag },
					_ => null
				}
				: null;

			if (updated is not { } valid)
			{
				Log.Error("Invalid option: {Key} = {Value} (takes {Keys})", key, value, string.Join(", ", keys));
				return false;
			}

			options = valid;
		}

		return true;
	}

	private readonly record struct SessionOptions(
		string? Name,
		int? Port,
		string? Password,
		int? MaxClients,
		bool? IsUpnpEnabled,
		bool? IsProfanityFilterEnabled);
}
