using System.Diagnostics;
using Godot;

// ReSharper disable MemberCanBePrivate.Global

namespace EnsembleRoot.Scripts.Adornments;

public abstract partial class HighlightBase : MeshInstance3D
{
	private static readonly Shader EdgesShader = GD.Load<Shader>(ShadersDir + "highlight_edges.gdshader");
	private static readonly Shader FillShader = GD.Load<Shader>(ShadersDir + "highlight_fill.gdshader");

	private static readonly StringName BoxSizeParameter = "box_size";
	private static readonly StringName EdgeThicknessParameter = "edge_thickness";
	private static readonly StringName FillOpacityParameter = "fill_opacity";
	private static readonly StringName WorldToAxesParameter = "world_to_axes";
	private static readonly StringName OpacityParameter = "opacity";
	private static readonly StringName OriginEdgesOnlyParameter = "origin_edges_only";
	private static readonly StringName MarchingAntsParameter = "marching_ants";
	private static readonly StringName AntSpacingParameter = "ant_spacing";
	private static readonly StringName AntSpeedParameter = "ant_speed";
	private static readonly StringName PulsingParameter = "pulsing";
	private static readonly StringName PulseMinimumParameter = "pulse_minimum";
	private static readonly StringName PulsePeriodParameter = "pulse_period";
	private static readonly StringName PulseTimeParameter = "pulse_time";

	// Only one overlay fits a mesh, so the highlight shown last fills it, handing it back to the one before when hidden
	private static readonly Dictionary<MeshInstance3D, List<ShaderMaterial>> FillsByMesh = [];

	// Every pulse shares one clock, so they stay in step; it restarts when pulses begin after none were showing
	private static int _pulseCount;
	private static ulong _pulsesEndedFrame;
	private static long _pulseStartTimestamp;

	private readonly BoxMesh _box = new();

	private readonly ShaderMaterial _edges = new()
	{
		Shader = EdgesShader, RenderPriority = (int)Material.RenderPriorityMax
	};

	// Depth-tested on the target's surfaces, it sorts with the scene, just over translucent geometry at the default
	// priority (which it may be tinting)
	private readonly ShaderMaterial _fill = new() { Shader = FillShader, RenderPriority = 1 };

	private readonly List<MeshInstance3D> _filled = [];

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

	/// <summary>The node whose meshes the fill tints, if any; the box only outlines it.</summary>
	public Node3D? Target
	{
		get;
		set
		{
			if (ReferenceEquals(field, value))
				return;

			field = value;
			UpdateFill();
		}
	}

	/// <summary>The share of light the fill replaces on the target's surfaces.</summary>
	// ReSharper disable once MemberCanBeProtected.Global
	[Export(PropertyHint.Range, "0,1")]
	public float FillOpacity
	{
		get;
		set
		{
			field = value;
			UpdateFillOpacity();
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

	/// <summary>The edges' render priority, as they ignore depth.</summary>
	[Export(PropertyHint.Range, "-128,127")]
	public int RenderPriority
	{
		get => _edges.RenderPriority;
		set => _edges.RenderPriority = value;
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

		Mesh = _box;
		MaterialOverride = _edges;
		SetNotifyTransform(true);

		UpdateMesh();
		UpdateAxes();
		UpdateFill();
		UpdateEdgeThickness();
		UpdateFillOpacity();
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
	public override void _ExitTree()
	{
		UpdatePulseClock(false);
		ClearFill();
	}

	public override void _Process(double delta) =>
		SetParameter(PulseTimeParameter, Stopwatch.GetElapsedTime(_pulseStartTimestamp).TotalSeconds);

	public override void _Notification(int what)
	{
		switch ((long)what)
		{
			case NotificationVisibilityChanged:
				UpdatePulseClock();
				UpdateFill();
				break;

			case NotificationTransformChanged:
				UpdateAxes();
				break;
		}
	}

	protected void SetParameter(StringName name, Variant value)
	{
		_edges.SetShaderParameter(name, value);
		_fill.SetShaderParameter(name, value);
	}

	private void UpdateMesh()
	{
		if (!IsNodeReady())
			return;

		_box.Size = Aabb.Size;
		Position = Aabb.Position + Aabb.Size / 2;

		SetParameter(BoxSizeParameter, Aabb.Size);
	}

	private void UpdateAxes() =>
		_fill.SetShaderParameter(WorldToAxesParameter, GlobalBasis.Orthonormalized().Transposed());

	private void UpdateFill()
	{
		ClearFill();

		if (
			Target is not { } target || !IsInstanceValid(target) || !IsInsideTree() || !IsVisibleInTree() ||
			IsQueuedForDeletion())
			return;

		foreach (var node in target.FindChildren("*", nameof(MeshInstance3D), true, false))
			if (node is MeshInstance3D mesh and not HighlightBase)
			{
				if (!FillsByMesh.TryGetValue(mesh, out var fills))
					FillsByMesh.Add(mesh, fills = []);

				fills.Add(_fill);
				mesh.MaterialOverlay = _fill;
				_filled.Add(mesh);
			}
	}

	private void ClearFill()
	{
		foreach (var mesh in _filled)
		{
			var fills = FillsByMesh[mesh];
			fills.Remove(_fill);

			if (fills.Count is 0)
				FillsByMesh.Remove(mesh);

			if (IsInstanceValid(mesh))
				mesh.MaterialOverlay = fills.LastOrDefault();
		}

		_filled.Clear();
	}

	private void UpdateEdgeThickness() => SetParameter(EdgeThicknessParameter, EdgeThickness);
	private void UpdateFillOpacity() => SetParameter(FillOpacityParameter, FillOpacity);
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
