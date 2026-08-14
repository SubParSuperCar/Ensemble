using Godot;
using Root.Common.Input;
using Serilog;

namespace Root.Scripts.Camera;

[GlobalClass]
public partial class PopperCam : SpringArm3D
{
	private Vector2 _capturedMousePosition;
	private float _pitch;
	private float _viewportDiagonal;
	private float _yaw;

	[Export] public Node3D Focus { get; set; } = null!;

	[Export] public float OrbitRatio { get; set; } = 2;

	[Export(PropertyHint.Range, "0,90,radians_as_degrees")]
	public float PitchMinMax { get; set; } = Mathf.DegToRad(85);

	[Export(PropertyHint.None, "radians_as_degrees,suffix:\u00B0/s")]
	public float YawRate { get; set; } = Mathf.DegToRad(90);

	[Export(PropertyHint.Range, "0,0,or_greater,hide_slider,suffix:m")]
	public float DollyMin { get; set; } = 1.25f;

	[Export(PropertyHint.Range, "0,0,or_greater,hide_slider,suffix:m")]
	public float DollyMax { get; set; } = 192;

	[Export(PropertyHint.Range, "0,0,or_greater,hide_slider")]
	public float DollyStep { get; set; } = 8;

	[Export] public float DollyRate { get; set; } = 96;

	public override void _Ready()
	{
		_yaw = Rotation.Y;
		_pitch = Rotation.X;

		RecalculateViewportDiagonal();
		GetViewport().SizeChanged += RecalculateViewportDiagonal;
	}

	public override void _Notification(int what)
	{
		if (what == NotificationWMWindowFocusOut)
			ReleaseMouse();
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (InputExtensions.IsSunk)
			return;

		if (@event.IsActionPressed("cam_orbit"))
			CaptureMouse();
		else if (@event.IsActionReleased("cam_orbit"))
			ReleaseMouse();
		else
			switch (@event)
			{
				case InputEventMouseMotion motion when Input.IsActionPressed("cam_orbit"):
					var radiansPerPixel = Mathf.Tau * OrbitRatio / _viewportDiagonal;
					_yaw -= motion.Relative.X * radiansPerPixel;
					_pitch = Mathf.Clamp(_pitch - motion.Relative.Y * radiansPerPixel, -PitchMinMax, PitchMinMax);

					break;
				case InputEventMouseButton { Pressed: true } button:
					// ReSharper disable once SwitchStatementMissingSomeEnumCasesNoDefault
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
		if (!InputExtensions.IsSunk)
		{
			var yawInput = Input.GetAxis("cam_yaw_left", "cam_yaw_right");

			if (yawInput is not 0)
				_yaw -= yawInput * YawRate * (float)delta;

			var dollyInput = Input.GetAxis("cam_dolly_in", "cam_dolly_out");

			if (dollyInput is not 0)
				ApplyDollyDelta(dollyInput * DollyRate * (float)delta);
		}

		// ReSharper disable once ConditionalAccessQualifierIsNonNullableAccordingToAPIContract
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
			MathF.Log(MathF.Max(SpringLength, DollyMin)) + delta * scale,
			minLog,
			maxLog);

		var length = MathF.Exp(logLength);
		SpringLength = delta < 0 && length <= DollyMin ? 0 : length;
	}

	private void RecalculateViewportDiagonal()
	{
		var size = GetViewport().GetVisibleRect().Size;
		_viewportDiagonal = MathF.Max(size.Length(), 1);
	}

	private void CaptureMouse()
	{
		if (Input.MouseMode is Input.MouseModeEnum.Captured)
			return;

		_capturedMousePosition = GetViewport().GetMousePosition();
		Input.MouseMode = Input.MouseModeEnum.Captured;

		Log.Verbose("Captured mouse at {Position}", _capturedMousePosition.ToString());
	}

	private void ReleaseMouse()
	{
		if (Input.MouseMode is Input.MouseModeEnum.Visible)
			return;

		Input.MouseMode = Input.MouseModeEnum.Visible;
		Input.WarpMouse(_capturedMousePosition);

		Log.Verbose("Released mouse");
	}
}
