using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Threading;
using AvaloniaEdit.TextMate;
using EnsembleRoot.Ui.Abstractions;
using EnsembleRoot.Ui.Controls;
using EnsembleRoot.Ui.ViewModels;
using Serilog;
using TextMateSharp.Grammars;

namespace EnsembleRoot.Ui.Views;

public sealed partial class LuaEditorView : UserControl, IViewFor<LuaEditorViewModel>
{
	private const string LanguageExtension = ".lua";
	private const ThemeName SyntaxTheme = ThemeName.OneDark;
	private const int IndentationSize = 2;
	private const int RulerPosition = 60;
	private const string EditorScrollViewerName = "PART_ScrollViewer";

	private TextMate.Installation? _textMate;

	public LuaEditorView()
	{
		InitializeComponent();

		Editor.TemplateApplied += OnEditorTemplateApplied;
	}

	protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
	{
		base.OnAttachedToVisualTree(e);
		Dispatcher.UIThread.Post(InitializeEditor, DispatcherPriority.Background);
	}

	// The installation subscribes to the shared document, so it would otherwise keep this view alive
	protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
	{
		base.OnDetachedFromVisualTree(e);

		_textMate?.Dispose();
		_textMate = null;
	}

	private void OnEditorTemplateApplied(object? sender, TemplateAppliedEventArgs e)
	{
		if (e.NameScope.Find<ScrollViewer>(EditorScrollViewerName) is { } scrollViewer)
			ScrollBarInset.Apply(scrollViewer, Actions);
	}

	private void InitializeEditor()
	{
		if (_textMate is not null || VisualRoot is null)
			return;

		Log.Debug("Initializing {Control}...", nameof(Editor));
		var stopwatch = Stopwatch.StartNew();

		var registryOptions = new RegistryOptions(SyntaxTheme);
		_textMate = Editor.InstallTextMate(registryOptions);

		var language = registryOptions.GetLanguageByExtension(LanguageExtension);
		var scope = registryOptions.GetScopeByLanguageId(language.Id);
		_textMate.SetGrammar(scope);

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
