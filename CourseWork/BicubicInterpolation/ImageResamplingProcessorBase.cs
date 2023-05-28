using CourseWork.BicubicInterpolation.Helpers;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;

namespace CourseWork.BicubicInterpolation;

[SuppressMessage("Interoperability", "CA1416:Validate platform compatibility")]
public abstract class ImageResamplingProcessorBase : IDisposable
{
    private readonly object _lockObject = new();

    protected ProcessingProperties ImageProperties { get; set; } = null!;

    protected bool IsInitialized;

    protected virtual void Init(
        Action<Bitmap, int, int>? initFactory,
        Bitmap inputImage,
        int outputWidth,
        int outputHeight)
    {
        if (IsInitialized)
        {
            throw new InvalidOperationException(
                "This processor is already in use. Try later or use another processor");
        }

        lock (_lockObject)
        {
            if (IsInitialized)
            {
                throw new InvalidOperationException(
                    "This processor is already in use. Try later or use another processor");
            }

            var width = inputImage.Width;
            var height = inputImage.Height;
            var ratioX = (double)(inputImage.Width - 1) / outputWidth;
            var ratioY = (double)(inputImage.Height - 1) / outputHeight;
            ImageProperties = new ProcessingProperties(width, height, ratioX, ratioY);

            initFactory?.Invoke(inputImage, outputWidth, outputHeight);
        }
        
        IsInitialized = true;
    }

    internal Pixel GetColorForPixel(
        Point point,
        ProcessingProperties properties,
        Func<int, int, Color> getPixel)
    {
        var px = point.X * properties.RatioX;
        var py = point.Y * properties.RatioY;

        var srcImageX = (int)px - 1;
        var srcImageY = (int)py - 1;

        var dx = px - srcImageX;
        var dy = py - srcImageY;

        var pixels = new Color[16];
        for (var j = 0; j < 4; j++)
        {
            for (var i = 0; i < 4; i++)
            {
                var xIndex = srcImageX + i;
                var yIndex = srcImageY + j;

                if (xIndex < 0)
                    xIndex = 0;
                else if (xIndex >= properties.Width)
                    xIndex = properties.Width - 1;

                if (yIndex < 0)
                    yIndex = 0;
                else if (yIndex >= properties.Height)
                    yIndex = properties.Height - 1;

                pixels[j * 4 + i] = getPixel(xIndex, yIndex);
            }
        }

        var weightsX = dx.GetCubicWeights();
        var weightsY = dy.GetCubicWeights();

        var color = GetColorForPixels(pixels, weightsX, weightsY);
        return new Pixel(point.X, point.Y, color);
    }

    protected static Color GetColorForPixels(
        IReadOnlyList<Color> pixels,
        IReadOnlyList<double> weightsX,
        IReadOnlyList<double> weightsY)
    {
        double r = 0, g = 0, b = 0;

        for (var j = 0; j < 4; j++)
        {
            for (var i = 0; i < 4; i++)
            {
                r += pixels[j * 4 + i].R * weightsX[i] * weightsY[j];
                g += pixels[j * 4 + i].G * weightsX[i] * weightsY[j];
                b += pixels[j * 4 + i].B * weightsX[i] * weightsY[j];
            }
        }

        var color = Color.FromArgb(r.Clamp(), g.Clamp(), b.Clamp());
        return color;
    }

    protected virtual void ReleaseResources()
    {
    }

    protected virtual void Dispose(bool disposing)
    {
        ReleaseResources();
        if (disposing)
        {
            lock (_lockObject)
            {
                IsInitialized = false;
                ImageProperties = null!;
            }
        }
    }

    public void Dispose()
    {
        Dispose(IsInitialized);
        GC.SuppressFinalize(this);
    }

    ~ImageResamplingProcessorBase()
    {
        Dispose(false);
    }
}
