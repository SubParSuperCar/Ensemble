using Godot;
using Serilog;

namespace Root.Scripts.Camera;

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
	public float DollyMax { get; set; } = 96;

	[Export(PropertyHint.Range, "0,0,or_greater,hide_slider")]
	public float DollyStep { get; set; } = 4;

	[Export] public float DollyRate { get; set; } = 48;

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
		if (@event.IsActionPressed("orbit_camera"))
			CaptureMouse();
		else if (@event.IsActionReleased("orbit_camera"))
			ReleaseMouse();
		else
			switch (@event)
			{
				case InputEventMouseMotion motion when Input.IsActionPressed("orbit_camera"):
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
		var turnInput = Input.GetAxis("turn_left", "turn_right");

		if (turnInput is not 0)
			_yaw -= turnInput * YawRate * (float)delta;

		var dollyInput = Input.GetAxis("dolly_in", "dolly_out");

		if (dollyInput is not 0)
			ApplyDollyDelta(dollyInput * DollyRate * (float)delta);

		// ReSharper disable once ConditionalAccessQualifierIsNonNullableAccordingToAPIContract
		GlobalPosition = Focus?.GlobalPosition ?? Vector3.Zero;

		var rotation = Rotation;
		rotation.Y = _yaw;
		rotation.X = _pitch;

		Rotation = rotation;
	}

	private void ApplyDollyDelta(float delta)
	{
		var dollyMinLog = MathF.Log(DollyMin);
		var dollyMaxLog = MathF.Log(DollyMax);

		var logScale = (dollyMaxLog - dollyMinLog) / (DollyMax - DollyMin);
		var logLength = Mathf.Clamp(MathF.Log(SpringLength) + delta * logScale, dollyMinLog, dollyMaxLog);
		SpringLength = MathF.Exp(logLength);
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

		Log.Debug("Captured mouse @ {Position}", _capturedMousePosition.ToString());
	}

	private void ReleaseMouse()
	{
		if (Input.MouseMode is Input.MouseModeEnum.Visible)
			return;

		Input.MouseMode = Input.MouseModeEnum.Visible;
		Input.WarpMouse(_capturedMousePosition);

		Log.Debug("Released mouse");
	}
}
