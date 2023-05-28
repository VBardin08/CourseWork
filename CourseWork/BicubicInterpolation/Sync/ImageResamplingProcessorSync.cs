using System.Diagnostics.CodeAnalysis;
using System.Drawing;

namespace CourseWork.BicubicInterpolation.Sync;

[SuppressMessage("Interoperability", "CA1416:Validate platform compatibility")]
public class ImageResamplingProcessorSync : ImageResamplingProcessorBase
{
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
        _inputImage = inputImage;
    }

    protected Bitmap ScaleImage(int outputWidth, int outputHeight)
    {
        var scaledImage = new Bitmap(outputWidth, outputHeight);

        for (var y = 0; y < outputHeight; y++)
        {
            for (var x = 0; x < outputWidth; x++)
            {
                var pixel = GetColorForPixel(
                    new Point(x, y),
                    ImageProperties,
                    _inputImage.GetPixel);
                scaledImage.SetPixel(pixel.X, pixel.Y, pixel.Color);
            }
        }

        return scaledImage;
    }

    protected override void ReleaseResources()
    {
        // Do not call the dispose for Bitmap. It's a responsibility of the higher object,
        // that called the processor, because the images it's its resource.
        // Just set the reference to the images to null to let processor work with the next image.
        _inputImage = null!;
    }
}