using EnsembleRoot.Scripts.Adornments;
using Godot;

namespace EnsembleRoot.Scripts.Plots.Outlines;

/// <summary>
///     A plot boundary outline that rises from the grid and fades in when raised, and sinks back into it and fades out
///     when lowered.
/// </summary>
public partial class PlotOutline : Node3D
{
	private readonly SolidHighlight _highlight = new() { Name = "Highlight", EdgeThickness = 1 / 16f, FaceAlpha = 0f };

	private bool? _isRaising;
	private float _progress;
	private Tween? _tween;

	public Aabb Bounds { get; init; }

	public override void _Ready()
	{
		// Placed by a global transform right after entering the tree
		PhysicsInterpolationMode = PhysicsInterpolationModeEnum.Off;

		AddChild(_highlight);
		SetProgress(0f);
	}

	public void Raise(Color color)
	{
		_highlight.Tint = color;
		AnimateTo(true);
	}

	public void Lower()
	{
		if (AnimateTo(false))
			_tween!.TweenCallback(Callable.From(QueueFree));
	}

	// One easing both ways, so turning around midway carries on from where the outline is
	private bool AnimateTo(bool isRaising)
	{
		if (_isRaising == isRaising)
			return false;

		_isRaising = isRaising;
		_tween?.Kill();

		var target = isRaising ? 1f : 0f;
		var duration = AnimationDuration.TotalSeconds * MathF.Abs(target - _progress);

		_tween = CreateTween().SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.InOut);
		_tween.TweenMethod(Callable.From<float>(SetProgress), _progress, target, duration);

		return true;
	}

	private void SetProgress(float progress)
	{
		_progress = progress;

		_highlight.Aabb = Bounds with { Size = Bounds.Size with { Y = Bounds.Size.Y * progress } };
		_highlight.Opacity = progress;
		_highlight.Visible = progress > 0;
	}
}
