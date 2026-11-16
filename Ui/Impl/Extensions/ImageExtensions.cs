using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Image = Godot.Image;

namespace EnsembleRoot.Ui.Extensions;

public static class ImageExtensions
{
	private static readonly Vector Dpi = new(96, 96);

	/// <summary>Copies an image into a bitmap, converting it to RGBA8 first if needed.</summary>
	public static Bitmap ToAvaloniaBitmap(this Image image)
	{
		if (image.GetFormat() is not Image.Format.Rgba8)
		{
			image = (Image)image.Duplicate();
			image.Convert(Image.Format.Rgba8);
		}

		var data = image.GetData();
		var handle = GCHandle.Alloc(data, GCHandleType.Pinned);

		try
		{
			return new Bitmap(
				PixelFormat.Rgba8888, AlphaFormat.Unpremul, handle.AddrOfPinnedObject(),
				new PixelSize(image.GetWidth(), image.GetHeight()), Dpi, image.GetWidth() * 4);
		}
		finally
		{
			handle.Free();
		}
	}
}
