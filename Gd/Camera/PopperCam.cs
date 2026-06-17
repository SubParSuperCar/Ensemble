using Godot;

namespace Root.Gd.Camera;

// Based on Roblox's PopperCam (DevCameraOcclusionMode.Zoom)
public partial class PopperCam : SpringArm3D
{
	// Rider's "Code Cleanup" modifies the order of these members:
	private float _dollyMaxLog; // These two "dolly log" vars could become computed props, but that'd be more expensive
	private float _dollyMinLog;
	private float _pitch;
	private float _viewportDiagonal;
	private float _yaw;

	// The Focus is the center point the cam orbits around. If null, falls back to Basis.Identity
	[Export] public Node3D Focus { get; set; } = null!;

	// Ratio -> TAU orbits per viewport diagonal traveled by the pointing device
	[Export(PropertyHint.Range, "hide_slider")]
	public float OrbitRatio { get; set; } = 2;

	[Export(PropertyHint.Range, "0,90,radians_as_degrees")]
	public float PitchMinMax { get; set; } = Mathf.DegToRad(80);

	[Export(PropertyHint.Range, "0,0,or_greater,or_less,hide_slider,radians_as_degrees,suffix:\u00B0/s")]
	public float YawRate { get; set; } = Mathf.DegToRad(90); // For turn-like key input such as L/R directional arrows

	// "Dolly" is the technically correct term here as "Zoom" relates to FOV, not distance
	// I prefer technical correct terms rather than commonly misused terms
	[Export(PropertyHint.Range, "0,0,or_greater,hide_slider,suffix:m")]
	public float DollyMin
	{
		get;
		set
		{
			field = value;
			_dollyMinLog = MathF.Log(DollyMin); // Update the logarithmic dolly min. Max is handled below
		}
	} = 2;

	[Export(PropertyHint.Range, "0,0,or_greater,hide_slider,suffix:m")]
	public float DollyMax
	{
		get;
		set
		{
			field = value;
			_dollyMaxLog = MathF.Log(DollyMax);
		}
	} = 20;

	[Export(PropertyHint.Range, "0,0,or_greater,hide_slider,suffix:m")]
	public float DollyStep { get; set; } = 2; // For scrolling input

	[Export(PropertyHint.Range, "0,0,or_greater,or_less,hide_slider,suffix:m/s")]
	public float DollyRate { get; set; } = 20; // For keyboard/button input such as I/O or -/+ (similar to Roblox)

	public override void _Ready()
	{
		// TODO: Remove repeated code
		_dollyMinLog = MathF.Log(DollyMin);
		_dollyMaxLog = MathF.Log(DollyMax);

		UpdateViewportDiagonal();
		GetViewport().SizeChanged += UpdateViewportDiagonal;
	}

	// Free the cursor when the window is unfocused
	public override void _Notification(int what)
	{
		if (what == NotificationWMWindowFocusOut)
			Input.MouseMode = Input.MouseModeEnum.Visible;
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (Input.IsActionJustPressed("orbit_camera")) // Use "Just" method so mouse mode is changed just once
			Input.MouseMode = Input.MouseModeEnum.Captured; // Keep the mouse still while orbiting the cam
		else if (Input.IsActionJustReleased("orbit_camera"))
			Input.MouseMode = Input.MouseModeEnum.Visible; // Free the mouse after orbiting the cam
		else
			switch (@event)
			{
				case InputEventMouseMotion motion when Input.IsActionPressed("orbit_camera"):
					var radiansPerPixel = (float)Math.Tau * OrbitRatio / _viewportDiagonal;
					_yaw -= motion.Relative.X * radiansPerPixel; // Left/right (should wrap around TAU?), below up/down
					_pitch = Mathf.Clamp(_pitch - motion.Relative.Y * radiansPerPixel, -PitchMinMax, PitchMinMax);

					break;
				case InputEventMouseButton { Pressed: true } button: // 'Pressed' just means the input was started
																	 // ReSharper disable once SwitchStatementMissingSomeEnumCasesNoDefault
					switch (button.ButtonIndex)
					{
						case MouseButton.WheelUp:
							DollyLog(-DollyStep); // Dolly/zoom in
							break;
						case MouseButton.WheelDown:
							DollyLog(DollyStep); // Dolly/zoom out
							break;
					}

					break;
			}
	}

	public override void _PhysicsProcess(double delta)
	{
		// These var names could be improved a bit
		// All input bindings are created elsewhere exactly as they are used in this class
		var yaw = Input.GetAxis("turn_left", "turn_right");

		if (yaw != 0)
			_yaw -= yaw * YawRate * (float)delta;

		var dolly = Input.GetAxis("dolly_in", "dolly_out");

		if (dolly != 0)
			DollyLog(dolly * DollyRate * (float)delta);

		// ReSharper disable once ConditionalAccessQualifierIsNonNullableAccordingToAPIContract
		GlobalPosition = Focus?.GlobalPosition ?? Vector3.Zero; // Fallback because Focus can technically be null

		var rotation = Rotation;
		rotation.Y = _yaw;
		rotation.X = _pitch;

		Rotation = rotation;
	}

	// These 3 methods down here could be improved a bit
	private void DollyLog(float delta)
	{
		var logLength = Mathf.Clamp(MathF.Log(SpringLength) + delta * LogScale(), _dollyMinLog, _dollyMaxLog);
		SpringLength = MathF.Exp(logLength);
	}

	private float LogScale() => (_dollyMaxLog - _dollyMinLog) / (DollyMax - DollyMin);

	private void UpdateViewportDiagonal()
	{
		var size = GetViewport().GetVisibleRect().Size;
		_viewportDiagonal = MathF.Max(size.Length(), 1);
	}
}
