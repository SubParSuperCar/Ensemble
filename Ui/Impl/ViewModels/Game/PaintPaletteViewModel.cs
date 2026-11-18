using System.Collections.ObjectModel;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EnsembleRoot.Common.Utils;
using EnsembleRoot.Scripts.Assets.Materials;
using EnsembleRoot.Tooling.Tools;
using EnsembleRoot.Ui.Abstractions;
using EnsembleRoot.Ui.Controls;
using EnsembleRoot.Ui.Extensions;
using EnsembleRoot.Ui.ViewModels.Utils;
using Estragonia;
using Godot;
using Serilog;
using Bitmap = Avalonia.Media.Imaging.Bitmap;
using Color = Avalonia.Media.Color;
using Colors = Avalonia.Media.Colors;
using KnownColor = System.Drawing.KnownColor;

// ReSharper disable UnusedParameterInPartialMethod

namespace EnsembleRoot.Ui.ViewModels;

/// <summary>
///     The paint tool's brush: its color (by field, sliders, text, or swatch), its material, which of them it applies,
///     and its eyedropper. The colors it paints are kept as recent swatches, across launches.
/// </summary>
// ReSharper disable once ClassNeverInstantiated.Global
public sealed partial class PaintPaletteViewModel : ViewModelBase, IWidget
{
	private const int MaxRecentCount = 36;

	private const string Section = "paint_palette";
	private const string RecentColorsKey = "recent_colors";

	// Below this saturation, a named color counts as a gray, sorted after the hues
	private const double GraySaturation = 0.1;

	private readonly MaterialItem[] _materials;

	private bool _isSyncing;

	public PaintPaletteViewModel()
	{
		_materials =
		[
			new MaterialItem(None, "Default", null),
			.. MaterialBank.All.Select(static material =>
				new MaterialItem(material.MaterialId, material.MaterialName.ToString(), material))
		];

		RecentSwatches = [.. LoadRecentColors().Select(static color => CreateSwatch(null, color))];
		FilterMaterials();

		OnBrushChanged();
		Paint.BrushChanged += OnBrushChanged;
		Paint.Painted += OnPainted;
	}

	private static PaintTool Paint => GToolManager.Paint;

	/// <summary>The named web colors, by hue and then lightness, with the grays last.</summary>
	public static IReadOnlyList<Swatch> NamedSwatches { get; } = CreateNamedSwatches();

	public ObservableCollection<Swatch> RecentSwatches { get; }
	public ObservableCollection<MaterialItem> VisibleMaterials { get; } = [];

	[ObservableProperty] public partial HsvColor HsvColor { get; set; } = Colors.White.ToHsv();

	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(ModeIndex))]
	private partial ColorEntryMode Mode { get; set; }

	// The mode list shows the modes in order, by display name
	public int ModeIndex
	{
		get => (int)Mode;
		set => Mode = (ColorEntryMode)value;
	}

	[ObservableProperty] public partial string ColorText { get; set; } = string.Empty;

	[ObservableProperty] public partial ColorModel SliderModel { get; private set; } = ColorModel.Rgba;
	[ObservableProperty] public partial decimal ComponentMaximum1 { get; private set; } = byte.MaxValue;
	[ObservableProperty] public partial decimal ComponentMaximum23 { get; private set; } = byte.MaxValue;

	[ObservableProperty] public partial decimal? Component1 { get; set; }
	[ObservableProperty] public partial decimal? Component2 { get; set; }
	[ObservableProperty] public partial decimal? Component3 { get; set; }
	[ObservableProperty] public partial decimal? Alpha { get; set; }

	[ObservableProperty] public partial string MaterialQuery { get; set; } = string.Empty;
	[ObservableProperty] public partial MaterialItem? SelectedMaterial { get; set; }
	[ObservableProperty] public partial MaterialItem? CurrentMaterial { get; private set; }

	[ObservableProperty] public partial bool IsColorApplied { get; set; }
	[ObservableProperty] public partial bool IsMaterialApplied { get; set; }
	[ObservableProperty] public partial bool IsEyedropping { get; set; }

	public static WidgetDescriptor Descriptor { get; } = new(WidgetDescriptor.Cells(0, 20, 8, 12))
	{
		Title = "Paint Palette",
		Description = "Lets you choose the paint tool's color and material.",
		MinSize = new Size(448, 352)
	};

	protected override void OnDispose()
	{
		Paint.BrushChanged -= OnBrushChanged;
		Paint.Painted -= OnPainted;
	}

	[RelayCommand]
	private void ApplySwatch(Swatch swatch) => HsvColor = swatch.Color.ToHsv();

	private static IEnumerable<Color> LoadRecentColors()
	{
		var hexes = UserData.GetValue(Section, RecentColorsKey, Array.Empty<string>()).AsStringArray();

		foreach (var hex in hexes.Take(MaxRecentCount))
			if (PaintColorFormat.TryParse(hex, ColorEntryMode.Hex, out var color))
				yield return color.ToRgb();
	}

	private static Swatch[] CreateNamedSwatches() =>
	[
		.. Enum.GetValues<KnownColor>()
			.Select(static known => (known, color: System.Drawing.Color.FromKnownColor(known)))
			.Where(static pair => pair.color is { IsSystemColor: false, A: byte.MaxValue })
			.Select(static pair => CreateSwatch(
				SplitWords(pair.known.ToString()), Color.FromRgb(pair.color.R, pair.color.G, pair.color.B)))
			.DistinctBy(static swatch => swatch.Color)
			.OrderBy(static swatch => swatch.Color.ToHsv().S < GraySaturation)
			.ThenBy(static swatch => swatch.Color.ToHsv().S < GraySaturation ? 0 : swatch.Color.ToHsv().H)
			.ThenBy(static swatch => swatch.Color.ToHsv().V)
	];

	private static Swatch CreateSwatch(string? name, Color color)
	{
		var hex = PaintColorFormat.Format(color.ToHsv(), ColorEntryMode.Hex);
		return new Swatch(color, Checkerboard.Under(color), name is null ? hex : $"{name} ({hex})");
	}

	// "CornflowerBlue" -> "Cornflower Blue"
	private static string SplitWords(string name) =>
		string.Concat(name.Select(static (c, i) => i > 0 && char.IsUpper(c) ? " " + c : c.ToString()));

	// Each edit lands in the brush, and each brush change in every view of it; the flag stops the echo
	private void Sync(Action update)
	{
		if (_isSyncing)
			return;

		_isSyncing = true;

		try
		{
			update();
		}
		finally
		{
			_isSyncing = false;
		}
	}

	partial void OnHsvColorChanged(HsvColor value)
	{
		if (!_isSyncing)
			Paint.Color = value.ToRgb().ToGodotColor();

		Sync(UpdateColorViews);
	}

	partial void OnModeChanged(ColorEntryMode value)
	{
		SliderModel = value is ColorEntryMode.Hsv ? ColorModel.Hsva : ColorModel.Rgba;
		ComponentMaximum1 = value is ColorEntryMode.Hsv ? 360 : byte.MaxValue;
		ComponentMaximum23 = value is ColorEntryMode.Hsv ? 100 : byte.MaxValue;

		Sync(UpdateColorViews);
	}

	// An unreadable entry reverts to the brush's color
	partial void OnColorTextChanged(string value) =>
		Sync(() =>
		{
			if (PaintColorFormat.TryParse(value, Mode, out var color))
				SetColor(color);

			UpdateColorViews();
		});

	partial void OnComponent1Changed(decimal? value) => Sync(ApplyComponents);
	partial void OnComponent2Changed(decimal? value) => Sync(ApplyComponents);
	partial void OnComponent3Changed(decimal? value) => Sync(ApplyComponents);
	partial void OnAlphaChanged(decimal? value) => Sync(ApplyComponents);

	partial void OnMaterialQueryChanged(string value) => FilterMaterials();

	// A filter that hides the selection clears it, which leaves the brush alone
	partial void OnSelectedMaterialChanged(MaterialItem? value)
	{
		if (value is not null && !_isSyncing)
			Paint.MaterialId = value.Id;
	}

	partial void OnIsColorAppliedChanged(bool value) => Paint.IsColorApplied = value;
	partial void OnIsMaterialAppliedChanged(bool value) => Paint.IsMaterialApplied = value;
	partial void OnIsEyedroppingChanged(bool value) => Paint.IsEyedropping = value;

	private void SetColor(HsvColor color)
	{
		HsvColor = color;
		Paint.Color = color.ToRgb().ToGodotColor();
	}

	private void UpdateColorViews()
	{
		var color = HsvColor;
		var rgb = color.ToRgb();
		var isHsv = Mode is ColorEntryMode.Hsv;

		ColorText = PaintColorFormat.Format(color, Mode);
		Component1 = isHsv ? Round(color.H) : rgb.R;
		Component2 = isHsv ? Round(color.S * 100) : rgb.G;
		Component3 = isHsv ? Round(color.V * 100) : rgb.B;
		Alpha = rgb.A;
	}

	private void ApplyComponents()
	{
		if (Component1 is not { } c1 || Component2 is not { } c2 || Component3 is not { } c3 || Alpha is not { } a)
			return;

		SetColor(
			Mode is ColorEntryMode.Hsv
				? new HsvColor((double)a / byte.MaxValue, (double)c1, (double)c2 / 100, (double)c3 / 100)
				: Color.FromArgb((byte)a, (byte)c1, (byte)c2, (byte)c3).ToHsv());

		UpdateColorViews();
	}

	private static decimal Round(double value) => Math.Round((decimal)value, MidpointRounding.AwayFromZero);

	private void FilterMaterials()
	{
		var query = MaterialQuery.Trim();

		VisibleMaterials.Clear();

		foreach (var material in _materials.Where(material => material.Matches(query)))
			VisibleMaterials.Add(material);

		Sync(() => SelectedMaterial = VisibleMaterials.Contains(CurrentMaterial!) ? CurrentMaterial : null);
	}

	private void OnBrushChanged() =>
		Sync(() =>
		{
			var color = Paint.Color.ToAvaloniaColor();

			// Kept as it is when it already matches, so a gray keeps its hue
			if (HsvColor.ToRgb() != color)
				HsvColor = color.ToHsv();

			CurrentMaterial = _materials.FirstOrDefault(static material => material.Id == Paint.MaterialId);
			SelectedMaterial = VisibleMaterials.Contains(CurrentMaterial!) ? CurrentMaterial : null;

			IsColorApplied = Paint.IsColorApplied;
			IsMaterialApplied = Paint.IsMaterialApplied;
			IsEyedropping = Paint.IsEyedropping;

			UpdateColorViews();
		});

	private void OnPainted()
	{
		if (!Paint.IsColorApplied)
			return;

		var color = Paint.Color.ToAvaloniaColor();

		if (RecentSwatches.FirstOrDefault(swatch => swatch.Color == color) is { } existing)
			RecentSwatches.Remove(existing);

		RecentSwatches.Insert(0, CreateSwatch(null, color));

		while (RecentSwatches.Count > MaxRecentCount)
			RecentSwatches.RemoveAt(RecentSwatches.Count - 1);

		UserData.SetValue(
			Section, RecentColorsKey,
			RecentSwatches.Select(static swatch => PaintColorFormat.Format(swatch.Color.ToHsv(), ColorEntryMode.Hex))
				.ToArray());
	}
}

public sealed partial class MaterialItem : ObservableObject
{
	public MaterialItem(int id, string name, AssetMaterial? material)
	{
		Id = id;
		Name = name;
		Stats = material is null ? null : FormatStats(material);

		_ = LoadPreviewAsync();
	}

	public int Id { get; }
	public string Name { get; }

	/// <summary>The material's physics, or null for none (the asset's own).</summary>
	public string? Stats { get; }

	[ObservableProperty] public partial Bitmap? Preview { get; private set; }

	public bool Matches(string query) =>
		Name.Contains(query, StringComparison.OrdinalIgnoreCase) ||
		Id.ToString(CultureInfo.InvariantCulture).Contains(query, StringComparison.Ordinal);

	private static string FormatStats(AssetMaterial material)
	{
		var stats = string.Create(
			CultureInfo.InvariantCulture,
			$"Density: {material.Density} kg/m\u00b3\nFriction: {material.Friction}\nBounce: {material.Bounce}");

		return material.Look?.Transparency is null or BaseMaterial3D.TransparencyEnum.Disabled
			? stats
			: stats + "\nSee-through";
	}

	private async Task LoadPreviewAsync()
	{
		try
		{
			Preview = (await MaterialPreviews.GetAsync(Id).ConfigureAwait(true)).ToAvaloniaBitmap();
		}
		catch (Exception exception)
		{
			Log.Error(exception, "Failed to render the preview of material {MaterialId}", Id);
		}
	}
}
