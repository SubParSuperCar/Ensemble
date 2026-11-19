using EnsembleRoot.Common.Utils;
using Godot;
using Environment = Godot.Environment;

namespace EnsembleRoot.Scripts.Assets.Materials;

/// <summary>Pictures of the materials: a white sphere painted with each, rendered once and kept.</summary>
public static class MaterialPreviews
{
	private const int Size = 128;
	private const float FillEnergy = 3 / 8f;

	private static readonly Dictionary<int, Task<Image>> PreviewsByMaterialId = [];

	/// <summary>
	///     Gets a material's picture, or the plain one for None, rendering it the first time (or again, should that
	///     have failed).
	/// </summary>
	public static Task<Image> GetAsync(int materialId)
	{
		if (!PreviewsByMaterialId.TryGetValue(materialId, out var preview) || preview.IsFaulted)
			PreviewsByMaterialId[materialId] = preview = RenderAsync(MaterialBank.Get(materialId));

		return preview;
	}

	// In a world of its own, so nothing else shows or lights it
	private static async Task<Image> RenderAsync(AssetMaterial? material)
	{
		var viewport = new SubViewport
		{
			Name = "Material Preview",
			Size = new Vector2I(Size, Size),
			TransparentBg = true,
			OwnWorld3D = true,
			Msaa3D = Viewport.Msaa.Msaa4X,
			RenderTargetUpdateMode = SubViewport.UpdateMode.Once
		};

		viewport.AddChild(new Camera3D
		{
			Projection = Camera3D.ProjectionType.Orthogonal, Size = 1.05f, Position = new Vector3(0, 0, 2)
		});

		// Exposed and lit like the world (its default camera attributes and sun), plus a fill light beside the camera
		viewport.AddChild(new DirectionalLight3D { RotationDegrees = new Vector3(-45, -45, 0) });

		viewport.AddChild(new DirectionalLight3D
		{
			RotationDegrees = new Vector3(-10, 30, 0), LightEnergy = FillEnergy
		});

		viewport.AddChild(new WorldEnvironment
		{
			Environment = new Environment { BackgroundMode = Environment.BGMode.ClearColor },
			CameraAttributes = new CameraAttributesPractical()
		});

		viewport.AddChild(new MeshInstance3D
		{
			Mesh = new SphereMesh { RadialSegments = 64, Rings = 32 },
			MaterialOverride = Paints.Get(material, Colors.White, BaseMaterial3D.ShadingModeEnum.PerPixel)
		});

		((SceneTree)Engine.GetMainLoop()).Root.AddChild(viewport);
		await RenderingServer.Singleton.ToSignalAsync(RenderingServer.SignalName.FramePostDraw);

		var image = viewport.GetTexture().GetImage();
		viewport.QueueFree();

		return image;
	}
}
