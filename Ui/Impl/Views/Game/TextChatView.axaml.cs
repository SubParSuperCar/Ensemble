using System.Collections.Specialized;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Avalonia.Styling;
using Avalonia.Threading;
using EnsembleRoot.Ui.Impl.Abstractions;
using EnsembleRoot.Ui.Impl.Extensions;
using EnsembleRoot.Ui.Impl.ViewModels;

namespace EnsembleRoot.Ui.Impl.Views;

public sealed partial class TextChatView : UserControl, IViewFor<TextChatViewModel>
{
	// Starts stuck, so the history lands at the bottom however late layout settles
	private bool _shouldScrollToBottom = true;
	private TextChatViewModel? _viewModel;

	public TextChatView()
	{
		InitializeComponent();
		ChatScroll.ScrollChanged += OnChatScrollChanged;

		ActualThemeVariantChanged += OnActualThemeVariantChanged;
	}

	protected override void OnDataContextChanged(EventArgs e)
	{
		base.OnDataContextChanged(e);

		if (_viewModel is not null)
		{
			_viewModel.Lines.CollectionChanged -= OnLinesChanged;
			_viewModel.FocusRequested -= OnFocusRequested;
		}

		_viewModel = DataContext as TextChatViewModel;
		RebuildLog();

		if (_viewModel is null)
			return;

		_viewModel.Lines.CollectionChanged += OnLinesChanged;
		_viewModel.FocusRequested += OnFocusRequested;
	}

	// Nametag colors depend on the theme, which is null while detached (e.g., as the widget closes) despite its type
	private void OnActualThemeVariantChanged(object? sender, EventArgs e)
	{
		// ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
		if (ActualThemeVariant is not null)
			RebuildLog();
	}

	private void RebuildLog()
	{
		ChatLog.Inlines?.Clear();

		foreach (var line in _viewModel?.Lines ?? [])
			AppendLine(line);
	}

	private void OnLinesChanged(object? sender, NotifyCollectionChangedEventArgs e)
	{
		if (e.OldItems is { } oldItems)
			for (var i = 0; i < oldItems.Count; i++)
				RemoveFirstLine();

		if (e.NewItems is not { } newItems)
			return;

		foreach (TextChatLine line in newItems)
			AppendLine(line);
	}

	// Lines are separated by leading line breaks, so removing the first one leaves no blank line behind
	private void AppendLine(TextChatLine line)
	{
		var inlines = ChatLog.Inlines ??= [];

		if (inlines.Count > 0)
			inlines.Add(new LineBreak());

		if (line.IsNotice)
		{
			inlines.Add(new Run(line.Text) { FontStyle = FontStyle.Italic });
			return;
		}

		var nametagColor = line.NametagColor.ToReadable(ThemeVariant.Dark.Equals(ActualThemeVariant));
		var nametagBrush = new ImmutableSolidColorBrush(nametagColor);

		inlines.Add(new Run(line.Nametag) { Foreground = nametagBrush, FontWeight = FontWeight.Bold });
		inlines.Add(new Run(" " + line.Text));
	}

	private void RemoveFirstLine()
	{
		if (ChatLog.Inlines is not { } inlines)
			return;

		while (inlines.Count > 0 && inlines[0] is not LineBreak)
			inlines.RemoveAt(0);

		if (inlines.Count > 0)
			inlines.RemoveAt(0);
	}

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

	// Sticks to the bottom while messages arrive or the view resizes, until the user scrolls up
	private void OnChatScrollChanged(object? sender, ScrollChangedEventArgs e)
	{
		if (e.ExtentDelta.Y > 0 || e.ViewportDelta.Y is not 0)
		{
			if (_shouldScrollToBottom)
				ChatScroll.ScrollToEnd();

			return;
		}

		var distanceToBottom = ChatScroll.Extent.Height - ChatScroll.Offset.Y - ChatScroll.Viewport.Height;
		_shouldScrollToBottom = distanceToBottom <= ChatLog.FontSize;
	}
}
