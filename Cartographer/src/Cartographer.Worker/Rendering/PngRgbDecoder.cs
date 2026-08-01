using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Cartographer.Worker.Rendering;

/// <summary>
/// Decodes PNG screenshots into tightly packed RGB bytes.
/// </summary>
public static class PngRgbDecoder
{
    public static (byte[] Rgb, int Width, int Height) Decode(Stream pngStream)
    {
        using var image = Image.Load<Rgba32>(pngStream);
        var rgb = new byte[image.Width * image.Height * 3];
        var i = 0;
        image.ProcessPixelRows(accessor =>
        {
            for (var y = 0; y < accessor.Height; y++)
            {
                var row = accessor.GetRowSpan(y);
                for (var x = 0; x < row.Length; x++)
                {
                    rgb[i++] = row[x].R;
                    rgb[i++] = row[x].G;
                    rgb[i++] = row[x].B;
                }
            }
        });
        return (rgb, image.Width, image.Height);
    }

    public static (byte[] Rgb, int Width, int Height) Decode(byte[] pngBytes)
    {
        using var ms = new MemoryStream(pngBytes);
        return Decode(ms);
    }
}
