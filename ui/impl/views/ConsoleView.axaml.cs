using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using AvaloniaEdit;
using AvaloniaEdit.TextMate;
using TextMateSharp.Grammars;
using InputExtensions = Root.Globals.Input.InputExtensions;

namespace Root.Ui.Impl.Views;

// ReSharper disable once UnusedType.Global
public partial class ConsoleView : UserControl
{
	private const string LanguageExtension = ".lua";
	private new const ThemeName Theme = ThemeName.OneDark;
	private const int IndentationSize = 2;
	private const int RulerPosition = 60;

	private ScrollViewer _outputScroll = null!;
	private bool _shouldScrollToBottom;

	public ConsoleView()
	{
		InitializeComponent();
		InitializeOutputScroll();
		InitializeEditor();
	}

	private void InitializeOutputScroll()
	{
		_outputScroll = this.FindControl<ScrollViewer>("OutputScroll")!;
		_outputScroll.ScrollChanged += OnOutputScrollChanged;
	}

	private void InitializeEditor()
	{
		var editor = this.FindControl<TextEditor>("Editor");

		var registryOptions = new RegistryOptions(Theme);
		var installation = editor.InstallTextMate(registryOptions);

		var language = registryOptions.GetLanguageByExtension(LanguageExtension);
		var scope = registryOptions.GetScopeByLanguageId(language.Id);

		installation.SetGrammar(scope);

		var options = editor!.Options;
		options.ShowSpaces = true;
		options.ShowTabs = true;
		options.ShowEndOfLine = true;
		options.ShowBoxForControlCharacters = true;
		options.EnableHyperlinks = true;
		options.EnableTextDragDrop = true;
		options.HighlightCurrentLine = true;
		options.IndentationSize = IndentationSize;
		options.ShowColumnRulers = true;
		options.ColumnRulerPositions = [RulerPosition];

		Editor.TextArea.GotFocus += OnEditorGotFocus;
		Editor.TextArea.LostFocus += OnEditorLostFocus;
	}

	private void OnOutputScrollChanged(object? sender, ScrollChangedEventArgs e)
	{
		if (e.ExtentDelta.Y > 0)
		{
			if (_shouldScrollToBottom)
				OutputScroll.ScrollToEnd();
		}
		else
		{
			var distanceToBottom = OutputScroll.Extent.Height - OutputScroll.Offset.Y - OutputScroll.Viewport.Height;
			_shouldScrollToBottom = distanceToBottom <= double.Epsilon;
		}
	}

	private void OnEditorGotFocus(object? sender, GotFocusEventArgs e) => InputExtensions.Sink.Acquire(this);
	private void OnEditorLostFocus(object? sender, RoutedEventArgs e) => InputExtensions.Sink.Release(this);

	protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
	{
		if (InputExtensions.Sink.IsHeldBy(this))
			InputExtensions.Sink.Release(this);

		base.OnDetachedFromVisualTree(e);
	}
}
