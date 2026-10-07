using EnsembleRoot.Common.Input;
using EnsembleRoot.Common.Utils;
using Godot;
using Serilog;
using MouseButton = Godot.MouseButton;

// ReSharper disable ConditionalAccessQualifierIsNonNullableAccordingToAPIContract
// ReSharper disable SwitchStatementMissingSomeEnumCasesNoDefault

namespace EnsembleRoot.Scripts.Cameras;

[GlobalClass]
public partial class PopperCam : SpringArm3D
{
	private static readonly StringName OrbitAction = "cam_orbit";
	private static readonly StringName YawLeftAction = "cam_yaw_left";
	private static readonly StringName YawRightAction = "cam_yaw_right";
	private static readonly StringName DollyInAction = "cam_dolly_in";
	private static readonly StringName DollyOutAction = "cam_dolly_out";

	private bool _isCaptureSettling;
	private float _pitch;
	private float _targetLength;
	private float _viewportDiagonal;
	private float _yaw;

	[Export] public Node3D Focus { get; set; } = null!;

	[Export] public float OrbitRatio { get; set; } = 2f;

	[Export(PropertyHint.Range, "0,90,radians_as_degrees")]
	public float PitchMinMax { get; set; } = Mathf.DegToRad(80f);

	[Export(PropertyHint.None, "radians_as_degrees,suffix:\u00B0/s")]
	public float YawRate { get; set; } = Mathf.DegToRad(90f);

	// The technically correct term is "dolly", not "zoom", because zoom is FOV, and dollying is physical in-out
	[Export(PropertyHint.Range, "0,0,or_greater,hide_slider,suffix:m")]
	public float DollyMin { get; set; } = 1.25f;

	[Export(PropertyHint.Range, "0,0,or_greater,hide_slider,suffix:m")]
	public float DollyMax { get; set; } = 192f;

	[Export(PropertyHint.Range, "0,0,or_greater,hide_slider")]
	public float DollyStep { get; set; } = 8f;

	[Export] public float DollyRate { get; set; } = 96f;

	[Export(PropertyHint.Range, "0,0,or_greater,hide_slider")]
	public float DollySmoothingRate { get; set; } = 28f;

	public override void _Ready()
	{
		_yaw = Rotation.Y;
		_pitch = Rotation.X;
		_targetLength = SpringLength;

		RecalculateViewportDiagonal();
		GetViewport().SizeChanged += RecalculateViewportDiagonal;
	}

	public override void _ExitTree()
	{
		GetViewport().SizeChanged -= RecalculateViewportDiagonal;
		ReleaseMouse();
	}

	public override void _Notification(int what)
	{
		if (what == NotificationWMWindowFocusOut)
			ReleaseMouse();
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (InputSink.IsSunk)
			return;

		if (@event.IsActionPressed(OrbitAction))
			CaptureMouse();
		else if (@event.IsActionReleased(OrbitAction))
			ReleaseMouse();
		else
			switch (@event)
			{
				case InputEventMouseMotion when _isCaptureSettling:
					_isCaptureSettling = false;
					break;

				case InputEventMouseMotion motion when Input.IsActionPressed(OrbitAction):
					var radiansPerPixel = Mathf.Tau * OrbitRatio / _viewportDiagonal;
					_yaw -= motion.Relative.X * radiansPerPixel;
					_pitch = Mathf.Clamp(_pitch - motion.Relative.Y * radiansPerPixel, -PitchMinMax, PitchMinMax);
					break;

				case InputEventMouseButton { Pressed: true } button:
					switch (button.ButtonIndex)
					{
						case MouseButton.WheelUp:
							ApplyDollyDelta(-DollyStep);
							break;

						case MouseButton.WheelDown:
							ApplyDollyDelta(DollyStep);
							break;
					}

					break;
			}
	}

	public override void _PhysicsProcess(double delta)
	{
		if (!InputSink.IsSunk)
		{
			var yawInput = Input.GetAxis(YawLeftAction, YawRightAction);
			if (yawInput is not 0)
				_yaw -= yawInput * YawRate * (float)delta;

			var dollyInput = Input.GetAxis(DollyInAction, DollyOutAction);
			if (dollyInput is not 0)
				ApplyDollyDelta(dollyInput * DollyRate * (float)delta);
		}

		_yaw = Mathf.Wrap(_yaw, -Mathf.Pi, Mathf.Pi);
		SpringLength = Mathf.Lerp(SpringLength, _targetLength, Smoothing.GetWeight(DollySmoothingRate, delta));

		GlobalPosition = Focus?.GlobalPosition ?? Vector3.Zero;

		var rotation = Rotation;
		rotation.Y = _yaw;
		rotation.X = _pitch;

		Rotation = rotation;
	}

	private void ApplyDollyDelta(float delta)
	{
		var minLog = MathF.Log(DollyMin);
		var maxLog = MathF.Log(DollyMax);
		var scale = (maxLog - minLog) / (DollyMax - DollyMin);

		var logLength = Mathf.Clamp(
			MathF.Log(MathF.Max(_targetLength, DollyMin)) + delta * scale,
			minLog, maxLog);

		var length = MathF.Exp(logLength);
		_targetLength = delta < 0 && length <= DollyMin ? 0 : length;
	}

	private void RecalculateViewportDiagonal()
	{
		var size = GetViewport().GetVisibleRect().Size;
		_viewportDiagonal = MathF.Max(size.Length(), 1);
	}

	// Capturing warps the cursor to the center, which can arrive as one large motion event
	private void CaptureMouse()
	{
		if (Pointer.IsCaptured)
			return;

		Pointer.Capture(GetViewport());
		_isCaptureSettling = true;

		Log.Verbose("Mouse captured at: {$Position}", Pointer.GetPosition(GetViewport()));
	}

	private void ReleaseMouse()
	{
		if (!Pointer.IsCaptured)
			return;

		Pointer.Release();
		_isCaptureSettling = false;

		Log.Verbose("Mouse released");
	}
}
