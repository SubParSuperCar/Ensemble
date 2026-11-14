using EnsembleRoot.Common.Utils;
using EnsembleRoot.Scripts.Adornments;
using Godot;

namespace EnsembleRoot.Scripts.Plots.Outlines;

/// <summary>
///     A plot boundary outline that rises from the grid and fades in when raised, and sinks back into it and fades out
///     when lowered.
/// </summary>
public partial class PlotOutline : Node3D
{
	// Plots dwarf instances, so their ants are far longer and faster than a selection's. It draws beneath the plots'
	// forward indicators and the characters' halos, which also ignore depth.
	private readonly SolidHighlight _highlight = new()
	{
		Name = "Highlight", EdgeThickness = 1 / 16f, AntSpacing = 2f, AntSpeed = 4f, RenderPriority = 1
	};

	private bool? _isRaising;
	private float _progress;
	private Tween? _tween;

	public Aabb Bounds { get; init; }

	public override void _Ready()
	{
		AddChild(_highlight);
		SetProgress(0f);
	}

	public void Raise(Color color, bool hasMarchingAnts)
	{
		_highlight.Tint = color;
		_highlight.MarchingAnts = hasMarchingAnts;
		AnimateTo(true);
	}

	public void Lower()
	{
		if (AnimateTo(false))
			_tween!.TweenCallback(Callable.From(QueueFree));
	}

	private bool AnimateTo(bool isRaising)
	{
		if (_isRaising == isRaising)
			return false;

		_isRaising = isRaising;
		_tween?.Kill();

		var target = isRaising ? 1f : 0f;
		var duration = AnimationDuration.TotalSeconds * MathF.Abs(target - _progress);

		_tween = CreateTween();
		_tween.TweenMethod(Callable.From<float>(SetProgress), _progress, target, duration);

		return true;
	}

	// Linear progress, eased here, so a turnaround carries on from where the outline is
	private void SetProgress(float progress)
	{
		_progress = progress;
		var eased = (float)Ease.InOut(progress);

		_highlight.Aabb = Bounds with { Size = Bounds.Size with { Y = Bounds.Size.Y * eased } };
		_highlight.Opacity = eased;
		_highlight.Visible = progress > 0;
	}
}
