using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using AvaloniaEdit.Document;
using AvaloniaEdit.Rendering;
using EnsembleRoot.Common.Text;
using EnsembleRoot.Ui.Abstractions;
using EnsembleRoot.Ui.Controls;
using EnsembleRoot.Ui.ViewModels;

namespace EnsembleRoot.Ui.Views;

public sealed partial class LogOutputView : UserControl, IViewFor<LogOutputViewModel>
{
	private const string EditorScrollViewerName = "PART_ScrollViewer";
	private const string SelectionClass = "selection";

	public LogOutputView()
	{
		InitializeComponent();

		// Before the selection's, so selected text keeps the selection's foreground
		Output.TextArea.TextView.LineTransformers.Insert(0, new AnsiColorizer(this));
		Output.TextArea.SelectionChanged += OnSelectionChanged;
	}

	private void OnOutputTemplateApplied(object? sender, TemplateAppliedEventArgs e)
	{
		if (e.NameScope.Find<ScrollViewer>(EditorScrollViewerName) is not { } scrollViewer)
			return;

		StickyBottom.Apply(scrollViewer, Output);
		ScrollBarInset.Apply(scrollViewer, SourceLabel, Actions);
	}

	private void OnSelectionChanged(object? sender, EventArgs e) =>
		Output.TextArea.Classes.Set(SelectionClass, !Output.TextArea.Selection.IsEmpty);

	// Colors the output as a terminal shows its escape codes, in xterm's palette
	private sealed class AnsiColorizer(LogOutputView view) : DocumentColorizingTransformer
	{
		private static readonly uint[] SystemColors =
		[
			0xFF000000, 0xFFCD0000, 0xFF00CD00, 0xFFCDCD00, 0xFF0000EE, 0xFFCD00CD, 0xFF00CDCD, 0xFFE5E5E5,
			0xFF7F7F7F, 0xFFFF0000, 0xFF00FF00, 0xFFFFFF00, 0xFF5C5CFF, 0xFFFF00FF, 0xFF00FFFF, 0xFFFFFFFF
		];

		private static readonly ImmutableSolidColorBrush[] Brushes =
			[.. Enumerable.Range(0, 256).Select(static index => new ImmutableSolidColorBrush(ToColor(index)))];

		protected override void ColorizeLine(DocumentLine line)
		{
			if (view.DataContext is not LogOutputViewModel { Styles: var styles })
				return;

			foreach (var segment in styles.FindOverlappingSegments(line.Offset, line.Length))
			{
				var start = Math.Max(segment.StartOffset, line.Offset);
				var end = Math.Min(segment.EndOffset, line.EndOffset);

				if (start < end)
					ChangeLinePart(start, end, element => Apply(element, segment.Style));
			}
		}

		// Like terminals, bold brightens the eight basic colors
		private static void Apply(VisualLineElement element, AnsiStyle style)
		{
			if (style.Foreground is { } foreground)
				element.TextRunProperties.SetForegroundBrush(
					Brushes[style.IsBold && foreground < 8 ? foreground + 8 : foreground]);

			if (!style.IsBold)
				return;

			var typeface = element.TextRunProperties.Typeface;

			element.TextRunProperties.SetTypeface(
				new Typeface(typeface.FontFamily, typeface.Style, FontWeight.Bold, typeface.Stretch));
		}

		// 16 system colors, a 6 x 6 x 6 cube, then 24 grays
		private static Color ToColor(int index)
		{
			if (index < SystemColors.Length)
				return Color.FromUInt32(SystemColors[index]);

			if (index >= 232)
			{
				var gray = (byte)(8 + (index - 232) * 10);
				return Color.FromRgb(gray, gray, gray);
			}

			var cube = index - 16;
			return Color.FromRgb(GetCubeLevel(cube / 36), GetCubeLevel(cube / 6 % 6), GetCubeLevel(cube % 6));
		}

		private static byte GetCubeLevel(int step) => (byte)(step is 0 ? 0 : 55 + step * 40);
	}
}
