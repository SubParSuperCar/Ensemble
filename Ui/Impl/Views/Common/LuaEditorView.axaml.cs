using System.Diagnostics;
using Avalonia.Controls;
using Avalonia.Threading;
using AvaloniaEdit.TextMate;
using EnsembleRoot.Ui.Impl.Abstractions;
using EnsembleRoot.Ui.Impl.ViewModels;
using Serilog;
using TextMateSharp.Grammars;

namespace EnsembleRoot.Ui.Impl.Views;

public partial class LuaEditorView : UserControl, IViewFor<LuaEditorViewModel>
{
	private const string LanguageExtension = ".lua";
	private new const ThemeName Theme = ThemeName.OneDark;
	private const int IndentationSize = 2;
	private const int RulerPosition = 60;

	public LuaEditorView()
	{
		InitializeComponent();
		Dispatcher.UIThread.Post(InitializeEditor, DispatcherPriority.Background);
	}

	private void InitializeEditor()
	{
		Log.Debug("Initializing {Control}...", nameof(Editor));
		var stopwatch = Stopwatch.StartNew();

		var registryOptions = new RegistryOptions(Theme);
		var installation = Editor.InstallTextMate(registryOptions);

		var language = registryOptions.GetLanguageByExtension(LanguageExtension);
		var scope = registryOptions.GetScopeByLanguageId(language.Id);
		installation.SetGrammar(scope);

		var options = Editor.Options;
		options.ShowSpaces = true;
		options.ShowTabs = true;
		options.ShowEndOfLine = true;
		options.HighlightCurrentLine = true;
		options.IndentationSize = IndentationSize;
		options.ShowColumnRulers = true;
		options.ColumnRulerPositions = [RulerPosition];

		stopwatch.Stop();
		Log.Debug("Initialized {Control} in {ElapsedMs:F3} ms", nameof(Editor), stopwatch.Elapsed.TotalMilliseconds);
	}
}
