using EnsembleRoot.Scripts.Adornments;
using Godot;

namespace EnsembleRoot.Scripts.Plots.Impl;

/// <summary>A plot boundary outline that rises from the grid when raised and sinks back into it when lowered.</summary>
public partial class PlotOutline : Node3D
{
	private readonly SolidHighlight _highlight = new() { Name = "Highlight", EdgeThickness = 1 / 16f, FaceAlpha = 0f };

	private float _height;
	private Tween? _tween;

	public Aabb Bounds { get; init; }

	public override void _Ready()
	{
		PhysicsInterpolationMode = PhysicsInterpolationModeEnum.Off;

		AddChild(_highlight);
		SetHeight(0f);
	}

	public void Raise(Color color)
	{
		_highlight.Tint = color;
		AnimateTo(1f, Tween.EaseType.Out);
	}

	public void Lower()
	{
		AnimateTo(0f, Tween.EaseType.In);
		_tween!.TweenCallback(Callable.From(QueueFree));
	}

	private void AnimateTo(float height, Tween.EaseType ease)
	{
		_tween?.Kill();

		var duration = AnimationDuration.TotalSeconds * MathF.Abs(height - _height);

		_tween = CreateTween().SetTrans(Tween.TransitionType.Quad).SetEase(ease);
		_tween.TweenMethod(Callable.From<float>(SetHeight), _height, height, duration);
	}

	private void SetHeight(float height)
	{
		_height = height;

		_highlight.Aabb = Bounds with { Size = Bounds.Size with { Y = Bounds.Size.Y * height } };
		_highlight.Visible = height > 0;
	}
}
