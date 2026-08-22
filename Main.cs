using System.Diagnostics;
using Godot;
using Root.Autoloading;
using Serilog;
using TinyDialogsNet;
using Environment = System.Environment;

namespace Root;

// Loads everything. Essentially our root of everything that happens.
public partial class Main : Node
{
	private static AutoloadScope RuntimeScope => IsHeadlessServer ? AutoloadScope.Server : AutoloadScope.Client;

	// Use a getter prop.
	public static bool IsHeadlessServer { get; } =
		string.Equals(DisplayServer.GetName(), "headless", StringComparison.Ordinal);

	public static bool AutoloadsLoaded { get; private set; }

	public static event Action? AutoloadsReady; // Let outsiders be notified when all loading is done, mainly UI.

	public override void _EnterTree()
	{
		// Handle exceptions.
		AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
		TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;
	}

	public override void _ExitTree()
	{
		AppDomain.CurrentDomain.UnhandledException -= OnUnhandledException;
		TaskScheduler.UnobservedTaskException -= OnUnobservedTaskException;
	}

	public override void _Ready()
	{
		Console.WriteLine($"Starting {nameof(Main)}... (IsHeadlessServer={IsHeadlessServer})");

		// If it's a client, wait a frame to let the Loading text render, even though it doesn't work for some reason.
		if (IsHeadlessServer)
			Load();
		else
			_ = LoadDeferredAsync();
	}

	public static void FailFast()
	{
		try
		{
			TinyDialogs.Beep(); // Terminal bell?

			TinyDialogs.NotifyPopup(
				NotificationIconType.Error,
				"Ensemble Crashed",
				"Ensemble crashed. Please contact the developer(s) or review the logs.");
		}
		catch (Exception exception)
		{
			Log.Error(exception, "Failed to show crash popup.");
		}
		finally
		{
			// I like FailFast's default message, but we could override it.
			Environment.FailFast(null);
		}
	}

	// Mainly used for asking the user if they want to proceed after some semi-important system component failed.
	public static bool AskUser(string title, string message)
	{
		try
		{
			var response = TinyDialogs.MessageBox(
				title,
				SanitizeMessageBoxBody(message), // Tiny dialogs requires NO quotes in the body: ", ', `
				MessageBoxDialogType.YesNo,
				MessageBoxIconType.Error,
				MessageBoxButton.No);

			return response is MessageBoxButton.Yes;
		}
		catch (Exception exception)
		{
			// It's possible that displaying fails if a DLL is missing.
			// Use "show" term because that's what TinyDialogs uses.
			Log.Error(exception, "Failed to show dialog.");
			return false;
		}
	}

	private static void OnUnhandledException(object? _, UnhandledExceptionEventArgs e)
	{
		if (e.ExceptionObject is Exception exception)
			Log.Fatal(exception, "Unhandled exception (IsTerminating={IsTerminating})", e.IsTerminating);
		else
			Log.Fatal("Unhandled exception (IsTerminating={IsTerminating}):\n{Exception}",
				e.IsTerminating,
				e.ExceptionObject);

		// Should we call FailFast to show a notif or just drop it immediately?
	}

	private static void OnUnobservedTaskException(object? _, UnobservedTaskExceptionEventArgs e)
	{
		Log.Error(e.Exception, "Unobserved task exception.");
		e.SetObserved(); // Save the program if we can.
	}

	private async Task LoadDeferredAsync()
	{
		// Get UI "Loading" to show if possible; doesn't really work, lol.
		RenderingServer.ForceDraw();
		await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);

		// Ensure Load runs on the right Godot thread context. This might be redundant.
		CallDeferred(nameof(Load));
	}

	private void Load()
	{
		LoadAutoloads(AutoloadRegistry.GetAll());

		AutoloadsLoaded = true;
		AutoloadsReady?.Invoke();
	}

	private void LoadAutoloads(AutoloadDefinition[] definitions)
	{
		// Per = for this autoload, Net = for all autoloads.
		var perStopwatch = new Stopwatch();
		var netStopwatch = Stopwatch.StartNew();

		// Fancy LINQ. Could specify "static," but Rider isn't telling me to.
		var loadedCount = definitions
			.Where(definition => (definition.Scope & RuntimeScope) is not AutoloadScope.None)
			.OrderBy(definition => definition.Order)
			.Count(definition => LoadAutoload(definition, perStopwatch));

		netStopwatch.Stop();
		Log.Debug("Loaded {Count} autoload(s) in {ElapsedMs:F3} ms.",
			loadedCount, netStopwatch.Elapsed.TotalMilliseconds);
	}

	private bool LoadAutoload(AutoloadDefinition definition, Stopwatch stopwatch)
	{
		var stage = AutoloadLoadStage.Factory; // Track the stage in-case an error occurs.
		Node? instance = null; // Track the instance to queue it free if it fails.

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
			instance?.QueueFree(); // Get rid of it. It's most likely dysfunctional.
			OnAutoloadFailed(definition, stage, exception);

			return false;
		}
	}

	private static void OnAutoloadFailed(
		AutoloadDefinition definition,
		AutoloadLoadStage stage,
		Exception exception)
	{
		// We could consider adding a new failure policy that simply prevents all descending autoloads from loading.
		// This could, for example, make it so if Session Manager fails, Core and other heavier-duty components stay alive.
		// The proc. would probably not be usable though regardless.
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
				if (!AskUser(
						"Autoload Init Failed",
						$"Failed to load the {definition.Type.Name} autoload during the {stage} stage:\n\n" +
						$"{exception}\n\nContinue anyway?\n" +
						"Ensemble may be left in an unstable or partially initialized state."))
					FailFast();
				break;

			default:
				throw new UnreachableException(); // This exception is probably right.
		}
	}

	private static string SanitizeMessageBoxBody(string message) =>
		message
			.Replace("\"", "", StringComparison.Ordinal) // View comment above.
			.Replace("'", "", StringComparison.Ordinal)
			.Replace("`", "", StringComparison.Ordinal);

	private enum AutoloadLoadStage : byte
	{
		Factory,
		AddChild,
		Initialize
	}
}
