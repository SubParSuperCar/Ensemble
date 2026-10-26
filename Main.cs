using System.Diagnostics;
using System.Globalization;
using EnsembleRoot.Autoloading;
using EnsembleRoot.Common.Interop;
using EnsembleRoot.Common.Utils;
using EnsembleRoot.Scripts.Watchdog;
using Godot;
using Serilog;
using TinyDialogsNet;
using Environment = System.Environment;

namespace EnsembleRoot;

/// <summary>
///     The entry point of Ensemble's code-behind. Boot-loads the nodes marked with <see cref="AutoloadAttribute" />
///     and owns error reporting, the application lifetime, and shutdown.
/// </summary>
public partial class Main : Node
{
	private static readonly TimeSpan ShutdownTimeout = TimeSpan.FromSeconds(3);

	private bool _isQuitting;
	private bool _isShutDown;

	public static Main? Instance { get; private set; }

	public static bool IsHeadlessServer { get; } =
		string.Equals(DisplayServer.GetName(), "headless", StringComparison.Ordinal);

	public static bool AreAutoloadsLoaded { get; private set; }

	private static AutoloadScope RuntimeScope =>
		IsHeadlessServer ? AutoloadScope.HeadlessServer : AutoloadScope.RegularClient;

	public static event Action? AutoloadsReady;

	public override void _EnterTree()
	{
		Instance = this;

		AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
		TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;
	}

	public override void _Ready()
	{
		Console.WriteLine($"Starting {nameof(Main)}... (IsHeadlessServer={IsHeadlessServer})");

		if (IsHeadlessServer)
			Load();
		else
			_ = LoadDeferredAsync();
	}

	public override void _ExitTree()
	{
		AppDomain.CurrentDomain.UnhandledException -= OnUnhandledException;
		TaskScheduler.UnobservedTaskException -= OnUnobservedTaskException;

		if (ReferenceEquals(Instance, this))
			Instance = null;
	}

	public override void _Notification(int what)
	{
		if (what == NotificationWMCloseRequest)
			_ = OnQuitAsync();
	}

	public void Quit() => GetTree().Root.PropagateNotification((int)NotificationWMCloseRequest);

	public static void FailFast(Exception? exception = null)
	{
		try
		{
			TinyDialogs.Beep();

			TinyDialogs.NotifyPopup(
				NotificationIconType.Error, "Ensemble Crashed",
				"Ensemble crashed. Please contact the developer(s) or review the logs. " +
				"Run the game in a console (Command Prompt, PowerShell, Terminal, etc.) to view stdout/stderr.");
		}
		catch (Exception notifyException)
		{
			PCall(() => Log.Error(notifyException, "Failed to show crash popup"));
		}

		PCall(Log.CloseAndFlush);
		Environment.FailFast(null, exception);
	}

	public static bool AskUser(string topic, string prompt)
	{
		var token = new object();
		Watchdog.Suspension.Acquire(token);

		try
		{
			return YesNoDialog.Ask(topic, SanitizeMessageBoxBody(prompt), MessageBoxIconType.Error, false);
		}
		catch (Exception exception)
		{
			Log.Error(exception, "Failed to show dialog");
			return false;
		}
		finally
		{
			Watchdog.Suspension.Release(token);
		}
	}

	public static string FormatFailureMessage(string action, Exception exception, string consequence) =>
		$"{action}:\n\n{exception}\n\nContinue anyway?\n{consequence}";

	public static string SanitizeMessageBoxBody(string message) =>
		message
			.Replace("\"", string.Empty, StringComparison.Ordinal)
			.Replace("'", string.Empty, StringComparison.Ordinal)
			.Replace("`", string.Empty, StringComparison.Ordinal);

	private static void PCall(Action action)
	{
		try
		{
			action();
		}
		catch
		{
			// Best effort: there is nowhere left to report a failure
		}
	}

	private static void OnUnhandledException(object? _, UnhandledExceptionEventArgs e)
	{
		if (e.ExceptionObject is Exception exception)
			Log.Fatal(
				exception,
				"Ensemble intercepted an unhandled exception (IsTerminating={IsTerminating})", e.IsTerminating);
		else
			Log.Fatal(
				"Ensemble intercepted an unhandled exception (IsTerminating={IsTerminating}):\n{Exception}",
				e.IsTerminating, e.ExceptionObject);

		if (e.IsTerminating)
			FailFast(e.ExceptionObject as Exception);
	}

	private static void OnUnobservedTaskException(object? _, UnobservedTaskExceptionEventArgs e)
	{
		e.SetObserved();
		Log.Error(e.Exception, "Ensemble mitigated an unobserved task exception");
	}

	private static void OnAutoloadFailed(AutoloadDefinition definition, AutoloadLoadStage stage, Exception exception)
	{
		Log.Error(exception, "Failed to load {Type} during {Stage} stage", definition.Type.FullName, stage);

		switch (definition.FailurePolicy)
		{
			case AutoloadFailurePolicy.LogAndContinue:
				break;

			case AutoloadFailurePolicy.FailFast:
				FailFast(exception);
				break;

			case AutoloadFailurePolicy.AskUser:
			{
				var message = FormatFailureMessage(
					$"Failed to load the {definition.Type.Name} autoload during the {stage} stage", exception,
					"Ensemble may be left in an unstable or partially initialized state.");

				if (!AskUser("Autoload Initialization Failed", message))
					FailFast(exception);

				break;
			}

			default:
				throw new UnreachableException();
		}
	}

	private async Task OnQuitAsync()
	{
		if (_isQuitting)
			return;

		_isQuitting = true;
		Log.Debug("Shutdown notification received. Starting shutdown sequence...");

		var children = GetChildren();

		// Queued frees run in order, so reversing the load order tears dependents down before what they depend on
		for (var i = children.Count - 1; i >= 0; i--)
			children[i].QueueFree();

		var tree = GetTree();
		var rootChildren = tree.Root.GetChildren();

		for (var i = rootChildren.Count - 1; i >= 0; i--)
		{
			var child = rootChildren[i];

			if (!ReferenceEquals(child, this))
				child.QueueFree();
		}

		Log.Debug("Queued children to be freed. Awaiting children removal...");

		// Doesn't rely on this method resuming, so a stuck teardown can't keep the game open (the logger is gone)
		tree.CreateTimer(ShutdownTimeout.TotalSeconds, true, false, true).Timeout += () =>
		{
			if (_isShutDown)
				return;

			Console.Error.WriteLine(string.Create(
				CultureInfo.InvariantCulture,
				$"Shutdown didn't finish within {ShutdownTimeout.TotalSeconds} s. Quitting anyway..."));

			tree.Quit();
		};

		while (tree.Root.GetChildCount() > 1 || GetChildCount() > 0)
			await tree.ToSignalAsync(SceneTree.SignalName.ProcessFrame);

		_isShutDown = true;

		Console.WriteLine("All children removed. Quitting the application...");
		tree.Quit();
	}

	private async Task LoadDeferredAsync()
	{
		// TODO: Await a readiness signal (if one exists) instead of a magic number of frames
		// Testing showed that the Avalonia UI takes exactly 3 frames to appear, for unclear reasons.
		var renderingServer = RenderingServer.Singleton;

		for (var i = 0; i < 3; i++)
		{
			RenderingServer.ForceDraw();
			await renderingServer.ToSignalAsync(RenderingServer.SignalName.FramePostDraw);
		}

		CallDeferred(MethodName.Load);
	}

	private void Load()
	{
		// Autoloads added mid-shutdown would never be freed, so the quit sequence would wait on them forever
		if (_isQuitting)
			return;

		Console.WriteLine($"Starting {nameof(Main)} loading sequence (boot-load autoloads)...");
		LoadAutoloads();

		Log.Debug("Finished {Class} loading sequence. Emitting {Event}...", nameof(Main), nameof(AutoloadsReady));

		AreAutoloadsLoaded = true;
		AutoloadsReady?.Invoke();
	}

	private void LoadAutoloads()
	{
		var start = Stopwatch.GetTimestamp();
		var definitions = AutoloadRegistry.GetAll()
			.Where(static definition => (definition.Scope & RuntimeScope) is not AutoloadScope.None)
			.OrderBy(static definition => definition.Order);

		var loadedCount = 0;

		// ReSharper disable once LoopCanBeConvertedToQuery
		foreach (var definition in definitions)
			if (LoadAutoload(definition))
				loadedCount++;

		Log.Debug(
			"Loaded {Count} autoload(s) in {ElapsedMs:F3} ms",
			loadedCount, Stopwatch.GetElapsedTime(start).TotalMilliseconds);
	}

	private bool LoadAutoload(AutoloadDefinition definition)
	{
		var fullName = definition.Type.FullName;
		var stage = AutoloadLoadStage.Factory;
		Node? instance = null;

		Log.Debug(
			"Loading {Type}... (Scope={Scope}, Order={Order}, FailurePolicy={FailurePolicy})",
			fullName, definition.Scope, definition.Order, definition.FailurePolicy);

		try
		{
			var start = Stopwatch.GetTimestamp();
			instance = definition.Factory();

			if (fullName is not null)
				instance.Name = fullName[(fullName.IndexOf('.', StringComparison.Ordinal) + 1)..].Replace('.', '-');

			stage = AutoloadLoadStage.AddChild;
			AddChild(instance);

			stage = AutoloadLoadStage.Initialize;
			if (instance is IAutoload autoload)
				autoload.Initialize();

			Log.Debug(
				"Loaded {Type} in {ElapsedMs:F3} ms", fullName, Stopwatch.GetElapsedTime(start).TotalMilliseconds);

			return true;
		}
		catch (Exception exception)
		{
			instance?.QueueFree();
			OnAutoloadFailed(definition, stage, exception);

			return false;
		}
	}

	private enum AutoloadLoadStage : byte
	{
		Factory,
		AddChild,
		Initialize
	}
}
