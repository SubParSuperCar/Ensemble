using System.ComponentModel;
using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;
using LiveMarkdown.Avalonia;
using Markdig;
using Root.Common.Networking;
using Root.Ui.Impl.Abstractions;
using Root.Ui.Impl.ViewModels;
using Serilog;
using TinyDialogsNet;

namespace Root.Ui.Impl.Views;

public partial class DocFileView : UserControl, IViewFor<DocFileViewModel>
{
	private CancellationTokenSource? _cts;
	private DocFileViewModel? _viewModel;

	public DocFileView()
	{
		InitializeComponent();
		DataContextChanged += OnDataContextChanged;
	}

	protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
	{
		base.OnAttachedToVisualTree(e);
		OnAttached();
	}

	protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
	{
		OnDetached();
		Interlocked.Exchange(ref _cts, null)?.Cancel();

		base.OnDetachedFromVisualTree(e);
	}

	private void OnDataContextChanged(object? sender, EventArgs e)
	{
		if (ReferenceEquals(_viewModel, DataContext))
			return;

		OnDetached();
		OnAttached();
	}

	private void OnAttached()
	{
		if (DataContext is not DocFileViewModel vm || ReferenceEquals(_viewModel, vm))
			return;

		_viewModel = vm;
		vm.PropertyChanged += OnViewModelPropertyChanged;
		_ = LoadFileAsync(vm.SelectedFile);
	}

	private void OnDetached()
	{
		if (_viewModel is null)
			return;

		_viewModel.PropertyChanged -= OnViewModelPropertyChanged;
		_viewModel = null;
	}

	private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
	{
		if (!string.Equals(e.PropertyName, nameof(DocFileViewModel.SelectedFile), StringComparison.Ordinal))
			return;

		if (sender is DocFileViewModel vm && ReferenceEquals(vm, _viewModel))
			_ = LoadFileAsync(vm.SelectedFile);
	}

	private async Task LoadFileAsync(DocFile file)
	{
		Log.Debug("Loading {FileName}...", file.Name);
		var stopwatch = Stopwatch.StartNew();

		var cts = new CancellationTokenSource();
		var oldCts = Interlocked.Exchange(ref _cts, cts);

		try
		{
			if (oldCts is not null)
				await oldCts.CancelAsync().ConfigureAwait(false);

			var markdown = await Http.Client.GetStringAsync(file.Uri, cts.Token).ConfigureAwait(false);
			cts.Token.ThrowIfCancellationRequested();

			var document = Markdown.Parse(markdown, MarkdownUpdateProducer.DefaultPipeline);

			await Dispatcher.UIThread.InvokeAsync(() =>
			{
				cts.Token.ThrowIfCancellationRequested();

				if (!ReferenceEquals(_cts, cts))
					return;

				MarkdownRenderer.ImageBasePath = new Uri(new Uri(file.Uri), ".").ToString();
				MarkdownRenderer.DocumentUpdate = new MarkdownDocumentUpdate.Full(document);

				stopwatch.Stop();
				Log.Debug("Loaded {FileName} in {ElapsedMs:F3} ms.", file.Name, stopwatch.Elapsed.TotalMilliseconds);
			});
		}
		catch (OperationCanceledException)
		{
		}
		catch (HttpRequestException exception)
		{
			if (!ReferenceEquals(_cts, cts))
				return;

			Log.Error(exception, "HTTP request failed.");

#pragma warning disable MA0040
			// ReSharper disable once MethodSupportsCancellation
			_ = Task.Run(() => TinyDialogs.MessageBox(
#pragma warning restore MA0040
				"HTTP Request Failed",
				Main.SanitizeMessageBoxBody($"Failed to load {file.Name} at:\n{file.Uri}\n\n{exception}"),
				MessageBoxDialogType.Ok,
				MessageBoxIconType.Warning,
				MessageBoxButton.Ok));
		}
		finally
		{
			Interlocked.CompareExchange(ref _cts, null, cts);
			cts.Dispose();
		}
	}
}
