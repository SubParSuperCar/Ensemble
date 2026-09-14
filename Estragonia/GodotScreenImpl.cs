using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Platform;
using Godot;

namespace Estragonia;

/// <summary>
///     <see cref="IScreenImpl" /> backed by Godot's <see cref="DisplayServer" />.
///     Enumerates all connected screens using Godot's multi-display API.
/// </summary>
internal sealed class GodotScreenImpl : IScreenImpl
{
	private readonly GodotScreen[] _allScreens;
	private readonly GodotScreen _primaryScreen;

	public GodotScreenImpl()
	{
		var screenCount = DisplayServer.GetScreenCount();
		var primaryIndex = DisplayServer.GetPrimaryScreen();

		var screens = new GodotScreen[screenCount];
		GodotScreen? primary = null;

		for (var i = 0; i < screenCount; i++)
		{
			var size = DisplayServer.ScreenGetSize(i);
			var position = DisplayServer.ScreenGetPosition(i);
			var scaling = DisplayServer.ScreenGetScale(i);

			var bounds = new PixelRect(position.X, position.Y, size.X, size.Y);

			// Godot doesn't expose a separate working area (the taskbar-free region),
			// so the full bounds are used as the working area
			var screen = new GodotScreen();
			screen.Initialize(
				$"Screen {i}",
				scaling,
				bounds,
				bounds,
				i == primaryIndex
			);

			screens[i] = screen;

			if (i == primaryIndex)
				primary = screen;
		}

		_allScreens = screens;
		_primaryScreen = primary ?? screens[0];
	}

	public int ScreenCount => _allScreens.Length;

	public IReadOnlyList<Screen> AllScreens => _allScreens;

	public Action? Changed { get; set; }

	public Screen ScreenFromWindow(IWindowBaseImpl window) => _primaryScreen;

	public Screen ScreenFromTopLevel(ITopLevelImpl topLevel) => _primaryScreen;

	public Screen ScreenFromPoint(PixelPoint point)
	{
		// Prefer the screen that contains the point
		if (Array.Find(_allScreens, screen => screen.Bounds.Contains(point)) is { } containingScreen)
			return containingScreen;

		// Otherwise, fall back to the screen whose center is closest to the point
		var bestScreen = _primaryScreen;
		var bestDistance = long.MaxValue;

		foreach (var screen in _allScreens)
		{
			var center = screen.Bounds.Center;
			var dx = point.X - center.X;
			var dy = point.Y - center.Y;
			var distance = (long)dx * dx + (long)dy * dy;

			if (distance >= bestDistance)
				continue;

			bestDistance = distance;
			bestScreen = screen;
		}

		return bestScreen;
	}

	public Screen ScreenFromRect(PixelRect rect)
	{
		// Pick the screen that contains the largest portion of the rectangle
		var bestScreen = _primaryScreen;
		var bestArea = 0;

		foreach (var screen in _allScreens)
		{
			var overlap = screen.Bounds.Intersect(rect);
			var area = overlap.Width * overlap.Height;

			if (area <= bestArea)
				continue;

			bestArea = area;
			bestScreen = screen;
		}

		return bestScreen;
	}

	public Task<bool> RequestScreenDetails() => Task.FromResult(true);

	private sealed class GodotScreen() : PlatformScreen(new PlatformHandle(IntPtr.Zero, "GodotScreen"))
	{
		public void Initialize(
			string displayName,
			double scaling,
			PixelRect bounds,
			PixelRect workingArea,
			bool isPrimary
		)
		{
			DisplayName = displayName;
			Scaling = scaling;
			Bounds = bounds;
			WorkingArea = workingArea;
			IsPrimary = isPrimary;
		}
	}
}
