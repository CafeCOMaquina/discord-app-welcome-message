using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.Processing;
using Color = SixLabors.ImageSharp.Color;


public static class ImageExtensions
{
    public static IImageProcessingContext ApplyRoundedCorners(this IImageProcessingContext ctx, float cornerRadius)
    {
        var size = ctx.GetCurrentSize();
        var corners = BuildCorners(size.Width, size.Height, cornerRadius);
        return ctx.SetGraphicsOptions(new GraphicsOptions { Antialias = true }).Fill(Color.Transparent, corners);
    }

    private static IPathCollection BuildCorners(int imageWidth, int imageHeight, float cornerRadius)
    {
        var rect = new RectangularPolygon(-0.5f, -0.5f, cornerRadius, cornerRadius);
        var cornerTopleft = rect.Clip(new EllipsePolygon(cornerRadius - 0.5f, cornerRadius - 0.5f, cornerRadius));
        var cornerTopRight = cornerTopleft.Rotate(90).Translate(imageWidth - cornerRadius, 0);
        var cornerBottomRight = cornerTopRight.Rotate(90).Translate(0, imageHeight - cornerRadius);
        var cornerBottomLeft = cornerBottomRight.Rotate(90).Translate(cornerRadius - imageWidth, 0);

        return new PathCollection(cornerTopleft, cornerTopRight, cornerBottomRight, cornerBottomLeft);
    }
}