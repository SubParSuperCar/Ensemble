using System.Diagnostics;
using Godot;

// ReSharper disable MemberCanBePrivate.Global

namespace EnsembleRoot.Scripts.Adornments;

public abstract partial class HighlightBase : MeshInstance3D
{
	private static readonly Shader FacesShader = GD.Load<Shader>(ShadersDir + "highlight_faces.gdshader");
	private static readonly Shader EdgesShader = GD.Load<Shader>(ShadersDir + "highlight_edges.gdshader");

	private static readonly StringName BoxSizeParameter = "box_size";
	private static readonly StringName EdgeThicknessParameter = "edge_thickness";
	private static readonly StringName FaceTintParameter = "face_tint";
	private static readonly StringName OpacityParameter = "opacity";
	private static readonly StringName OriginEdgesOnlyParameter = "origin_edges_only";
	private static readonly StringName MarchingAntsParameter = "marching_ants";
	private static readonly StringName AntSpacingParameter = "ant_spacing";
	private static readonly StringName AntSpeedParameter = "ant_speed";
	private static readonly StringName PulsingParameter = "pulsing";
	private static readonly StringName PulseMinimumParameter = "pulse_minimum";
	private static readonly StringName PulsePeriodParameter = "pulse_period";
	private static readonly StringName PulseTimeParameter = "pulse_time";

	// Every pulse shares one clock, so they stay in step; it restarts when pulses begin after none were showing
	private static int _pulseCount;
	private static ulong _pulsesEndedFrame;
	private static long _pulseStartTimestamp;

	private readonly BoxMesh _box = new();

	// Edges draw over every face, so each shows its full color
	private readonly ShaderMaterial _edges = new()
	{
		Shader = EdgesShader, RenderPriority = (int)Material.RenderPriorityMax
	};

	private readonly ShaderMaterial _faces = new()
	{
		Shader = FacesShader, RenderPriority = (int)Material.RenderPriorityMax - 1
	};

	private bool _isPulseCounted;

	[Export]
	public Aabb Aabb
	{
		get;
		set
		{
			field = value;
			UpdateMesh();
		}
	}

	[Export(PropertyHint.Range, "0,0,or_greater,hide_slider,suffix:m")]
	public float EdgeThickness
	{
		get;
		set
		{
			field = value;
			UpdateEdgeThickness();
		}
	} = 1 / 64f;

	/// <summary>How strongly front faces tint what is behind them, from 0 (not at all) to 1 (in their full color).</summary>
	[Export(PropertyHint.Range, "0,1")]
	public float FaceTint
	{
		get;
		set
		{
			field = value;
			UpdateFaceTint();
		}
	} = 1 / 4f;

	[Export(PropertyHint.Range, "0,1")]
	public float Opacity
	{
		get;
		set
		{
			field = value;
			UpdateOpacity();
		}
	} = 1f;

	/// <summary>The edges' render priority; the faces draw one beneath it.</summary>
	[Export(PropertyHint.Range, "-127,127")]
	public int RenderPriority
	{
		get => _edges.RenderPriority;
		set
		{
			_edges.RenderPriority = value;
			_faces.RenderPriority = value - 1;
		}
	}

	[Export]
	public bool OriginEdgesOnly
	{
		get;
		set
		{
			field = value;
			UpdateOriginEdgesOnly();
		}
	}

	[Export]
	public bool MarchingAnts
	{
		get;
		set
		{
			field = value;
			UpdateMarchingAnts();
		}
	}

	/// <summary>The length of each dash, and of each gap between them.</summary>
	[Export(PropertyHint.Range, "0,0,or_greater,hide_slider,suffix:m")]
	public float AntSpacing
	{
		get;
		set
		{
			field = value;
			UpdateAntSpacing();
		}
	} = 1 / 12f;

	[Export(PropertyHint.Range, "0,0,or_less,or_greater,hide_slider,suffix:m/s")]
	public float AntSpeed
	{
		get;
		set
		{
			field = value;
			UpdateAntSpeed();
		}
	} = 1 / 8f;

	[Export]
	public bool Pulsing
	{
		get;
		set
		{
			field = value;
			UpdatePulsing();
		}
	}

	[Export(PropertyHint.Range, "0,1")]
	public float PulseMinimum
	{
		get;
		set
		{
			field = value;
			UpdatePulseMinimum();
		}
	} = 1 / 2f;

	[Export(PropertyHint.Range, "0,0,or_greater,hide_slider,suffix:s")]
	public float PulsePeriod
	{
		get;
		set
		{
			field = value;
			UpdatePulsePeriod();
		}
	} = 1 / 2f;

	public override void _Ready()
	{
		// Highlights are placed between physics ticks, so interpolating would drag them from where they last were
		PhysicsInterpolationMode = PhysicsInterpolationModeEnum.Off;

		_faces.NextPass = _edges;

		Mesh = _box;
		MaterialOverride = _faces;

		UpdateMesh();
		UpdateEdgeThickness();
		UpdateFaceTint();
		UpdateOpacity();
		UpdateOriginEdgesOnly();
		UpdateMarchingAnts();
		UpdateAntSpacing();
		UpdateAntSpeed();
		UpdatePulsing();
		UpdatePulseMinimum();
		UpdatePulsePeriod();
	}

	// Still inside the tree at this point
	public override void _ExitTree() => UpdatePulseClock(false);

	public override void _Process(double delta) =>
		SetParameter(PulseTimeParameter, Stopwatch.GetElapsedTime(_pulseStartTimestamp).TotalSeconds);

	public override void _Notification(int what)
	{
		if (what == NotificationVisibilityChanged)
			UpdatePulseClock();
	}

	protected void SetParameter(StringName name, Variant value)
	{
		_faces.SetShaderParameter(name, value);
		_edges.SetShaderParameter(name, value);
	}

	private void UpdateMesh()
	{
		if (!IsNodeReady())
			return;

		_box.Size = Aabb.Size;
		Position = Aabb.Position + Aabb.Size / 2;

		SetParameter(BoxSizeParameter, Aabb.Size);
	}

	private void UpdateEdgeThickness() => SetParameter(EdgeThicknessParameter, EdgeThickness);
	private void UpdateFaceTint() => SetParameter(FaceTintParameter, FaceTint);
	private void UpdateOpacity() => SetParameter(OpacityParameter, Opacity);
	private void UpdateOriginEdgesOnly() => SetParameter(OriginEdgesOnlyParameter, OriginEdgesOnly);
	private void UpdateMarchingAnts() => SetParameter(MarchingAntsParameter, MarchingAnts);
	private void UpdateAntSpacing() => SetParameter(AntSpacingParameter, AntSpacing);
	private void UpdateAntSpeed() => SetParameter(AntSpeedParameter, AntSpeed);

	private void UpdatePulsing()
	{
		SetParameter(PulsingParameter, Pulsing);
		UpdatePulseClock();
	}

	private void UpdatePulseMinimum() => SetParameter(PulseMinimumParameter, PulseMinimum);
	private void UpdatePulsePeriod() => SetParameter(PulsePeriodParameter, PulsePeriod);

	// One pulse handing over to another in the same frame (e.g., hovering from one selection to another) keeps the beat
	private void UpdatePulseClock(bool isStaying = true)
	{
		var isPulsing = isStaying && Pulsing && IsInsideTree() && !IsQueuedForDeletion() && IsVisibleInTree();

		SetProcess(isPulsing);

		if (isPulsing == _isPulseCounted)
			return;

		_isPulseCounted = isPulsing;

		if (!isPulsing)
		{
			if (--_pulseCount is 0)
				_pulsesEndedFrame = Engine.GetProcessFrames();
		}
		else if (_pulseCount++ is 0 && _pulsesEndedFrame != Engine.GetProcessFrames())
			_pulseStartTimestamp = Stopwatch.GetTimestamp();
	}
}
