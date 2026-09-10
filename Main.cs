using System.Diagnostics;
using Godot;
using Root.Autoloading;
using Serilog;
using TinyDialogsNet;
using Environment = System.Environment;

namespace Root;

/// <summary>
///     The main entry point for Ensemble's code-behind.
///     Handles boot-loading Node-inheriting classes marked with <see cref="AutoloadAttribute" />,
///     and provides resources for managing the application lifetime and shutdowns.
/// </summary>
public partial class Main : Node
{
	private bool _isQuitting;

	public static Main? Instance { get; private set; }

	public static bool IsHeadlessServer { get; } =
		string.Equals(DisplayServer.GetName(), "headless", StringComparison.Ordinal);

	public static bool AreAutoloadsLoaded { get; private set; }

	private static AutoloadScope RuntimeScope => IsHeadlessServer ? AutoloadScope.Server : AutoloadScope.Client;

	public static event Action? AutoloadsReady;

	public override void _EnterTree()
	{
		Instance = this;

		AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
		TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;
	}

	public override void _ExitTree()
	{
		AppDomain.CurrentDomain.UnhandledException -= OnUnhandledException;
		TaskScheduler.UnobservedTaskException -= OnUnobservedTaskException;

		if (ReferenceEquals(Instance, this))
			Instance = null;
	}

	public override void _Ready()
	{
		Console.WriteLine($"Starting {nameof(Main)}... (IsHeadlessServer={IsHeadlessServer})");

		if (IsHeadlessServer)
			Load();
		else
			_ = LoadDeferredAsync();
	}

	public override void _Notification(int what)
	{
		if (what == NotificationWMCloseRequest)
			_ = OnQuit();
	}

	private async Task OnQuit()
	{
		if (_isQuitting)
			return;

		_isQuitting = true;

		Log.Debug("Shutdown notification received. Starting shutdown sequence...");

		var children = GetChildren();

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

		while (tree.Root.GetChildCount() > 1 || GetChildCount() > 0)
			await ToSignal(tree, SceneTree.SignalName.ProcessFrame);

		Log.Debug("All children removed. Quitting the application...");
		tree.Quit();
	}

	public void Quit() => GetTree().Root.PropagateNotification((int)NotificationWMCloseRequest);

	public static void FailFast(Exception? exception = null)
	{
		try
		{
			TinyDialogs.Beep();

			TinyDialogs.NotifyPopup(
				NotificationIconType.Error,
				"Ensemble Crashed",
				"Ensemble crashed. Please contact the developer(s) or review the logs. " +
				"Run the game in a console (Cmd Prompt, PowerShell, Terminal, etc.) to view stdout/stderr.");
		}
		catch (Exception notifyException)
		{
			PCall((Action<Exception, string>)Log.Error, notifyException, "Failed to show crash popup.");
		}

		PCall(Log.CloseAndFlush);
		Environment.FailFast(null, exception);
	}

	private static void PCall(Delegate action, params object?[] args)
	{
		try { action.DynamicInvoke(args); }
		catch
		{
			// Ignore
		}
	}

	public static bool AskUser(string topic, string prompt)
	{
		try
		{
			var response = TinyDialogs.MessageBox(
				topic,
				SanitizeMessageBoxBody(prompt),
				MessageBoxDialogType.YesNo,
				MessageBoxIconType.Error,
				MessageBoxButton.No);

			return response is MessageBoxButton.Yes;
		}
		catch (Exception exception)
		{
			Log.Error(exception, "Failed to show dialog.");
			return false;
		}
	}

	public static string FormatFailureMessage(string action, Exception exception, string consequence) =>
		$"{action}:\n\n{exception}\n\nContinue anyway?\n{consequence}";

	public static string SanitizeMessageBoxBody(string message) =>
		message
			.Replace("\"", string.Empty, StringComparison.Ordinal)
			.Replace("'", string.Empty, StringComparison.Ordinal)
			.Replace("`", string.Empty, StringComparison.Ordinal);

	private static void OnUnhandledException(object? _, UnhandledExceptionEventArgs e)
	{
		if (e.ExceptionObject is Exception exception)
			Log.Fatal(exception, "Ensemble intercepted an unhandled exception. (IsTerminating={IsTerminating})",
				e.IsTerminating);
		else
			Log.Fatal("Ensemble intercepted an unhandled exception. (IsTerminating={IsTerminating}):\n{Exception}",
				e.IsTerminating,
				e.ExceptionObject);

		if (e.IsTerminating)
			FailFast();
	}

	private static void OnUnobservedTaskException(object? _, UnobservedTaskExceptionEventArgs e)
	{
		e.SetObserved();
		Log.Error(e.Exception, "Ensemble mitigated an unobserved task exception.");
	}

	private static void OnAutoloadFailed(
		AutoloadDefinition definition,
		AutoloadLoadStage stage,
		Exception exception)
	{
		Log.Error(exception, "Failed to load {Type} during {Stage} stage.", definition.Type.FullName, stage);

		// ReSharper disable once SwitchStatementHandlesSomeKnownEnumValuesWithDefault
		switch (definition.FailurePolicy)
		{
			case AutoloadFailurePolicy.LogAndContinue:
				break;

			case AutoloadFailurePolicy.FailFast:
				FailFast();
				break;

			case AutoloadFailurePolicy.AskUser:
				if (
					!AskUser(
						"Autoload Initialization Failed",
						FormatFailureMessage(
							$"Failed to load the {definition.Type.Name} autoload during the {stage} stage",
							exception,
							"Ensemble may be left in an unstable or partially initialized state.")))
					FailFast();
				break;

			default:
				throw new UnreachableException();
		}
	}

	private async Task LoadDeferredAsync()
	{
		// TODO: Don't await an arbitrary/magical number of times; use a readiness signal (if available)
		// I've tested this, and it takes exactly 3 frames for Avalonia UI to show up. Unsure why.
		for (var i = 0; i < 3; i++)
		{
			RenderingServer.ForceDraw();
			await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
		}

		CallDeferred(nameof(Load));
	}

	private void Load()
	{
		Console.WriteLine($"Starting {nameof(Main)} loading sequence (boot-load autoloads)...");

		LoadAutoloads(AutoloadRegistry.GetAll());

		Log.Debug("Finished {Class} loading sequence. Emitting {Event}...", nameof(Main), nameof(AutoloadsReady));

		AreAutoloadsLoaded = true;
		AutoloadsReady?.Invoke();
	}

	private void LoadAutoloads(AutoloadDefinition[] definitions)
	{
		var perAutoloadStopwatch = new Stopwatch();
		var totalStopwatch = Stopwatch.StartNew();

		var loadedCount = definitions
			.Where(static definition => (definition.Scope & RuntimeScope) is not AutoloadScope.None)
			.OrderBy(static definition => definition.Order)
			.Count(definition => LoadAutoload(definition, perAutoloadStopwatch));

		totalStopwatch.Stop();
		Log.Debug("Loaded {Count} autoload(s) in {ElapsedMs:F3} ms.",
			loadedCount, totalStopwatch.Elapsed.TotalMilliseconds);
	}

	private bool LoadAutoload(AutoloadDefinition definition, Stopwatch stopwatch)
	{
		var stage = AutoloadLoadStage.Factory;
		Node? instance = null;

		try
		{
			var fullName = definition.Type.FullName;

			Log.Debug(
				"Loading {Type}... (Scope={Scope}, Order={Order}, FailurePolicy={FailurePolicy})",
				fullName,
				definition.Scope,
				definition.Order,
				definition.FailurePolicy);

			stopwatch.Restart();
			instance = definition.Factory();

			if (fullName is not null)
				instance.Name = fullName.Replace('.', '-');

			stage = AutoloadLoadStage.AddChild;
			AddChild(instance);

			stage = AutoloadLoadStage.Initialize;
			if (instance is IAutoload autoload)
				autoload.Initialize();

			stopwatch.Stop();
			Log.Debug("Loaded {Type} in {ElapsedMs:F3} ms.", fullName, stopwatch.Elapsed.TotalMilliseconds);

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
