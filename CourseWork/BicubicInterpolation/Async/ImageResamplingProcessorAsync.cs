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

    private byte[] _imageBytes = null!;

    public async Task<Bitmap> BicubicInterpolation(Bitmap? inputImage, int outputWidth, int outputHeight)
    {
        if (inputImage is null)
        {
            throw new ArgumentNullException(nameof(inputImage));
        }

        Init(InitProcessor, inputImage, outputWidth, outputHeight);

        var scaledImage = await ScaleImageAsync(outputWidth, outputHeight);

        Dispose();

        return scaledImage;
    }

    private void InitProcessor(Bitmap inputImage, int outputWidth, int outputHeight)
    {
        _bytesPerPixel = Image.GetPixelFormatSize(inputImage.PixelFormat) / 8;
        _imageBytes = GetImageAsByteArray(inputImage);
    }

    private async Task<Bitmap> ScaleImageAsync(int outputWidth, int outputHeight)
    {
        var scaledImage = new Bitmap(outputWidth, outputHeight);

        var tasks = new List<Task<Pixel>>();

        for (var y = 0; y < outputHeight; y++)
        {
            for (var x = 0; x < outputWidth; x++)
            {
                Task<Pixel> CalculatePixelTask()
                {
                    return Task.FromResult(GetColorForPixel(new Point(x, y), ImageProperties, GetPixel));
                }

                tasks.Add(CalculatePixelTask());
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

    protected override void ReleaseResources()
    {
        // Do not call the dispose for Bitmap. It's a responsibility of the higher object,
        // that called the processor, because the images it's its resource.
        // Just set the reference to the images to null to let processor work with the next image.
        _stride = _bytesPerPixel = 0;
        _imageBytes = null!;
    }
}