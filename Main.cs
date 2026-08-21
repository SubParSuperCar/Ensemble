using System.Diagnostics;
using Godot;
using Root.Autoloading;
using Serilog;
using TinyDialogsNet;
using Environment = System.Environment;

namespace Root;

public partial class Main : Node
{
	private static AutoloadScope CurrentScope => IsHeadlessServer ? AutoloadScope.Server : AutoloadScope.Client;

	public static bool IsHeadlessServer => string.Equals(DisplayServer.GetName(), "headless", StringComparison.Ordinal);
	public static bool AutoloadsLoaded { get; private set; }

	public static event Action? AutoloadsReady;

	public override void _EnterTree()
	{
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

		if (IsHeadlessServer)
			Load();
		else
			_ = LoadDeferredAsync();
	}

	private async Task LoadDeferredAsync()
	{
		RenderingServer.ForceDraw();
		await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);

		CallDeferred(nameof(Load));
	}

	private void Load()
	{
		LoadAutoloads(AutoloadRegistry.GetAll());

		AutoloadsLoaded = true;
		AutoloadsReady?.Invoke();
	}

	private static void OnUnhandledException(object? _, UnhandledExceptionEventArgs e)
	{
		if (e.ExceptionObject is Exception exception)
			Log.Fatal(exception, "Unhandled exception (IsTerminating={IsTerminating})", e.IsTerminating);
		else
			Log.Fatal("Unhandled exception (IsTerminating={IsTerminating}):\n{Exception}",
				e.IsTerminating,
				e.ExceptionObject);
	}

	private static void OnUnobservedTaskException(object? _, UnobservedTaskExceptionEventArgs e)
	{
		Log.Error(e.Exception, "Unobserved task exception.");
		e.SetObserved();
	}

	private void LoadAutoloads(AutoloadDefinition[] definitions)
	{
		var loadStopwatch = new Stopwatch();
		var totalStopwatch = Stopwatch.StartNew();

		var loadedCount = definitions
			.Where(definition => (definition.Scope & CurrentScope) is not AutoloadScope.None)
			.OrderBy(definition => definition.Order)
			.Count(definition => LoadAutoload(definition, loadStopwatch));

		totalStopwatch.Stop();
		Log.Debug("Loaded {Count} autoload(s) in {ElapsedMs:F3} ms.", loadedCount,
			totalStopwatch.Elapsed.TotalMilliseconds);
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
			Log.Debug("Loaded {Type} in {ElapsedMs:F3} ms.",
				fullName, stopwatch.Elapsed.TotalMilliseconds);

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
				OnFailFast();
				break;

			case AutoloadFailurePolicy.AskUser:
				if (!AskUser(
						"Autoload Initialization Failed",
#pragma warning disable MA0001
						$"{exception.ToString().Replace("\"", "").Replace("'", "").Replace("`", "")}\n\n" +
#pragma warning restore MA0001
						"Continue anyway?\nThe program may be left in an unstable or partially initialized state."))
					OnFailFast();
				break;

			default:
				throw new UnreachableException();
		}
	}

	private static void OnFailFast()
	{
		try
		{
			TinyDialogs.Beep();
		}
		finally
		{
			Environment.FailFast(null);
		}
	}

	private static bool AskUser(string title, string message)
	{
		try
		{
			var response = TinyDialogs.MessageBox(
				title,
				message.Replace('"', '\''),
				MessageBoxDialogType.YesNo,
				MessageBoxIconType.Error,
				MessageBoxButton.No);

			return response is MessageBoxButton.Yes;
		}
		catch (Exception exception)
		{
			Log.Error(exception, "Failed to display failure dialog.");
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
