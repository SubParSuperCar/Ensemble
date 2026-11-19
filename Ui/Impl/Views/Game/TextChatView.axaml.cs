using System.Collections.Specialized;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Avalonia.Styling;
using Avalonia.Threading;
using AvaloniaEdit.Document;
using AvaloniaEdit.Rendering;
using EnsembleRoot.Ui.Abstractions;
using EnsembleRoot.Ui.Controls;
using EnsembleRoot.Ui.Extensions;
using EnsembleRoot.Ui.ViewModels;

namespace EnsembleRoot.Ui.Views;

public sealed partial class TextChatView : UserControl, IViewFor<TextChatViewModel>
{
	private const double ScrollBarGap = 2d;
	private const string EditorScrollViewerName = "PART_ScrollViewer";

	private TextChatViewModel? _viewModel;

	public TextChatView()
	{
		InitializeComponent();

		ChatLog.Document.UndoStack.SizeLimit = 0;
		ChatLog.TextArea.TextView.LineTransformers.Add(new NametagColorizer(this));
		ActualThemeVariantChanged += OnActualThemeVariantChanged;
	}

	protected override void OnDataContextChanged(EventArgs e)
	{
		base.OnDataContextChanged(e);

		_viewModel?.Lines.CollectionChanged -= OnLinesChanged;
		_viewModel?.FocusRequested -= OnFocusRequested;

		_viewModel = DataContext as TextChatViewModel;
		RebuildLog();

		_viewModel?.Lines.CollectionChanged += OnLinesChanged;
		_viewModel?.FocusRequested += OnFocusRequested;
	}

	// Nametag colors depend on the theme, which is null while detached (e.g., as the widget closes) despite its type
	private void OnActualThemeVariantChanged(object? sender, EventArgs e)
	{
		// ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
		if (ActualThemeVariant is not null)
			ChatLog.TextArea.TextView.Redraw();
	}

	private void RebuildLog()
	{
		ChatLog.Document.Text = string.Join('\n', _viewModel?.Lines.Select(Format) ?? []);
		ChatLog.TextArea.TextView.Redraw();
	}

	// Each line of the document is a line of the chat, styled by the colorizer
	private void OnLinesChanged(object? sender, NotifyCollectionChangedEventArgs e)
	{
		var document = ChatLog.Document;

		if (e.OldItems is { } oldItems)
			for (var i = 0; i < oldItems.Count && document.TextLength > 0; i++)
				document.Remove(0, document.GetLineByNumber(1).TotalLength);

		if (e.NewItems is not { } newItems)
			return;

		// By index, as an empty line leaves the document empty too
		for (var i = 0; i < newItems.Count; i++)
		{
			var text = Format((TextChatLine)newItems[i]!);
			document.Insert(document.TextLength, e.NewStartingIndex + i is 0 ? text : "\n" + text);
		}
	}

	private void OnChatLogTemplateApplied(object? sender, TemplateAppliedEventArgs e)
	{
		if (e.NameScope.Find<ScrollViewer>(EditorScrollViewerName) is not { } scrollViewer)
			return;

		StickyBottom.Apply(scrollViewer, ChatLog);
		ScrollBarInset.ApplyGap(scrollViewer, ChatLog.TextArea, ScrollBarGap);
	}

	// Notices may come from anywhere, so their line breaks become spaces to keep one chat line to a document line
	private static string Format(TextChatLine line) =>
		line.IsNotice ? line.Text.ReplaceLineEndings(" ") : $"{line.Nametag} {line.Text}";

	// Deferred, so the key that requested focus isn't typed into the box
	private void OnFocusRequested() => Dispatcher.UIThread.Post(() => MessageBox.Focus());

	private void OnMessageBoxKeyDown(object? sender, KeyEventArgs e)
	{
		if (e.Key is not (Key.Enter or Key.Escape))
			return;

		if (e.Key is Key.Enter)
			_viewModel?.SendCommand.Execute(null);

		e.Handled = true;
		TopLevel.GetTopLevel(this)?.FocusManager.Focus(null);
	}

	// Bolds and colors each message's nametag, and italicizes notices
	private sealed class NametagColorizer(TextChatView view) : DocumentColorizingTransformer
	{
		protected override void ColorizeLine(DocumentLine line)
		{
			if (view._viewModel?.Lines is not { } lines || line.LineNumber > lines.Count)
				return;

			var chatLine = lines[line.LineNumber - 1];

			if (chatLine.IsNotice)
			{
				ChangeLinePart(line.Offset, line.EndOffset, Italicize);
				return;
			}

			var brush = new ImmutableSolidColorBrush(
				chatLine.NametagColor.ToReadable(ThemeVariant.Dark.Equals(view.ActualThemeVariant)));

			ChangeLinePart(line.Offset, line.Offset + chatLine.Nametag!.Length, element => Embolden(element, brush));
		}

		private static void Italicize(VisualLineElement element)
		{
			var typeface = element.TextRunProperties.Typeface;

			element.TextRunProperties.SetTypeface(
				new Typeface(typeface.FontFamily, FontStyle.Italic, typeface.Weight, typeface.Stretch));
		}

		private static void Embolden(VisualLineElement element, IBrush brush)
		{
			var typeface = element.TextRunProperties.Typeface;

			element.TextRunProperties.SetTypeface(
				new Typeface(typeface.FontFamily, typeface.Style, FontWeight.Bold, typeface.Stretch));

			element.TextRunProperties.SetForegroundBrush(brush);
		}
	}
}
