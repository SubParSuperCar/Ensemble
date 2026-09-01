using System.Diagnostics;
using Godot;
using Root.Autoloading;
using Serilog;
using TinyDialogsNet;
using Environment = System.Environment;

namespace Root;

public partial class Main : Node
{
	public static Main Instance { get; private set; } = null!;

	private static AutoloadScope RuntimeScope => IsHeadlessServer ? AutoloadScope.Server : AutoloadScope.Client;

	public static bool IsHeadlessServer { get; } =
		string.Equals(DisplayServer.GetName(), "headless", StringComparison.Ordinal);

	public static bool AutoloadsLoaded { get; private set; }

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
			Instance = null!;
	}

	public override void _Ready()
	{
		Console.WriteLine($"Starting {nameof(Main)}... (IsHeadlessServer={IsHeadlessServer})");

		if (IsHeadlessServer)
			Load();
		else
			_ = LoadDeferredAsync();
	}

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
			Log.Error(notifyException, "Failed to show crash popup.");
		}
		finally
		{
			Environment.FailFast(null, exception);
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

	private static void OnUnhandledException(object? _, UnhandledExceptionEventArgs e)
	{
		if (e.ExceptionObject is Exception exception)
			Log.Fatal(exception, "Unhandled exception. (IsTerminating={IsTerminating})", e.IsTerminating);
		else
			Log.Fatal("Unhandled exception. (IsTerminating={IsTerminating}):\n{Exception}",
				e.IsTerminating,
				e.ExceptionObject);

		if (e.IsTerminating)
			FailFast();
	}

	private static void OnUnobservedTaskException(object? _, UnobservedTaskExceptionEventArgs e)
	{
		Log.Error(e.Exception, "Unobserved task exception.");
		e.SetObserved();
	}

	private async Task LoadDeferredAsync()
	{
		for (var i = 0; i < 3; i++)
		{
			RenderingServer.ForceDraw();
			await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
		}

		CallDeferred(nameof(Load));
	}

	private void Load()
	{
		Console.WriteLine($"Starting {nameof(Main)} loading sequence...");

		LoadAutoloads(AutoloadRegistry.GetAll());

		Log.Debug("Finished {Class} loading sequence. Emitting {Event}...", nameof(Main), nameof(AutoloadsReady));

		AutoloadsLoaded = true;
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
						"Autoload Init Failed",
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

	private static string SanitizeMessageBoxBody(string message) =>
		message
			.Replace("\"", string.Empty, StringComparison.Ordinal)
			.Replace("'", string.Empty, StringComparison.Ordinal)
			.Replace("`", string.Empty, StringComparison.Ordinal);

	private enum AutoloadLoadStage : byte
	{
		Factory,
		AddChild,
		Initialize
	}
}
