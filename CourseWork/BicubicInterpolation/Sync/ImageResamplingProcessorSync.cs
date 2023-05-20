using CourseWork.BicubicInterpolation.Helpers;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;

namespace CourseWork.BicubicInterpolation.Sync;

[SuppressMessage("Interoperability", "CA1416:Validate platform compatibility")]
public class ImageResamplingProcessorSync : ImageResamplingProcessorBase
{
    private int _height;
    private int _width;
    private double _ratioX;
    private double _ratioY;
    private Bitmap _inputImage = null!;

    public Bitmap BicubicInterpolation(Bitmap? inputImage, int outputWidth, int outputHeight)
    {
        if (inputImage is null)
        {
            throw new ArgumentNullException(nameof(inputImage));
        }

        Init(InitProcessor, inputImage, outputWidth, outputHeight);

        var scaledImage = ScaleImage(outputWidth, outputHeight);

        Dispose();

        return scaledImage;
    }

    private void InitProcessor(Bitmap inputImage, int outputWidth, int outputHeight)
    {
        _width = inputImage.Width;
        _height = inputImage.Height;

        _ratioX = (double)(inputImage.Width - 1) / outputWidth;
        _ratioY = (double)(inputImage.Height - 1) / outputHeight;

        _inputImage = inputImage;
    }

    private Bitmap ScaleImage(int outputWidth, int outputHeight)
    {
        var scaledImage = new Bitmap(outputWidth, outputHeight);

        for (var y = 0; y < outputHeight; y++)
        {
            for (var x = 0; x < outputWidth; x++)
            {
                var color = GetColorForPixel(x, y);
                scaledImage.SetPixel(x, y, color);
            }
        }

        return scaledImage;
    }

    private Color GetColorForPixel(int x, int y)
    {
        var px = x * _ratioX;
        var py = y * _ratioY;

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
                else if (xIndex >= _width)
                    xIndex = _width - 1;

                if (yIndex < 0)
                    yIndex = 0;
                else if (yIndex >= _height)
                    yIndex = _height - 1;

                pixels[j * 4 + i] = _inputImage.GetPixel(xIndex, yIndex);
            }
        }

        var weightsX = dx.GetCubicWeights();
        var weightsY = dy.GetCubicWeights();

        var color = GetColorForPixels(pixels, weightsX, weightsY);
        return color;
    }

    private static Color GetColorForPixels(
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

    protected override void ReleaseResources()
    {
        _width = _height = 0;
        _ratioX = _ratioY = 0;

        // Do not call the dispose for Bitmap. It's a responsibility of the higher object,
        // that called the processor, because the images it's its resource.
        // Just set the reference to the images to null to let processor work with the next image.
        _inputImage = null!;
    }
}