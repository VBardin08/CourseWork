using CourseWork.BicubicInterpolation.Helpers;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace CourseWork.BicubicInterpolation.Async;

[SuppressMessage("Interoperability", "CA1416:Validate platform compatibility")]
public class ImageResamplingProcessorAsync : ImageResamplingProcessorBase
{
    private int _stride;
    private int _bytesPerPixel;
    private int _height;
    private int _width;
    private int _targetWidth;
    private int _targetHeight;
    private double _ratioX;
    private double _ratioY;
    
    private byte[] _imageBytes = null!;

    public async Task<Bitmap> BicubicInterpolation(Bitmap? inputImage, int outputWidth, int outputHeight)
    {
        if (inputImage is null)
        {
            throw new ArgumentNullException(nameof(inputImage));
        }

        Init(InitProcessor, inputImage, outputWidth, outputHeight);

        var scaledImage = await ScaleImageAsync();

        Dispose();

        return scaledImage;
    }

    private void InitProcessor(Bitmap inputImage, int outputWidth, int outputHeight)
    {
        _width = inputImage.Width;
        _height = inputImage.Height;

        _targetWidth = outputWidth;
        _targetHeight = outputHeight;

        _ratioX = (double)(inputImage.Width - 1) / outputWidth;
        _ratioY = (double)(inputImage.Height - 1) / outputHeight;

        _bytesPerPixel = Image.GetPixelFormatSize(inputImage.PixelFormat) / 8;

        _imageBytes = GetImageAsByteArray(inputImage);
    }

    private async Task<Bitmap> ScaleImageAsync()
    {
        var scaledImage = new Bitmap(_targetWidth, _targetHeight);

        var tasks = new List<Task<Pixel>>();
        
        for (var y = 0; y < _targetHeight; y++)
        {
            for (var x = 0; x < _targetWidth; x++)
            {
                tasks.Add(GetColorForPixel(x, y));
            }
        }

        var completedTasks = await Task.WhenAll(tasks);
        foreach (var pixel in completedTasks)
        {
            scaledImage.SetPixel(pixel.X, pixel.Y, pixel.Color);
        }

        return scaledImage;
    }

    private byte[] GetImageAsByteArray(Bitmap bitmap)
    {
        var bitmapData = bitmap.LockBits(
            new Rectangle(0, 0, bitmap.Width, bitmap.Height),
            ImageLockMode.ReadOnly,
            bitmap.PixelFormat);
        var length = bitmapData.Stride * bitmapData.Height;

        var bytes = new byte[length];

        Marshal.Copy(bitmapData.Scan0, bytes, 0, length);
        bitmap.UnlockBits(bitmapData);

        _stride = bitmapData.Stride;

        return bytes;
    }

    private Color GetPixel(int x, int y)
    {
        var index = y * _stride + x * _bytesPerPixel;

        try
        {
            var blue = _imageBytes[index];
            var green = _imageBytes[index + 1];
            var red = _imageBytes[index + 2];
            var alpha = _bytesPerPixel == 4 ? _imageBytes[index + 3] : (byte)255;
        
            var pixelColor = Color.FromArgb(alpha, red, green, blue);
            return pixelColor;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }

    private Task<Pixel> GetColorForPixel(int x,  int y)
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

                pixels[j * 4 + i] = GetPixel(xIndex, yIndex);
            }
        }

        var weightsX = dx.GetCubicWeights();
        var weightsY = dy.GetCubicWeights();

        var color = GetColorForPixels(pixels, weightsX, weightsY);
        return Task.FromResult(new Pixel(x, y, color));
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
        _targetHeight = _targetWidth = 0;
        _stride = _bytesPerPixel = 0;
        _imageBytes = null!;
    }
}