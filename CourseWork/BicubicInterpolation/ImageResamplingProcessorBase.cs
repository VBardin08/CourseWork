using System.Diagnostics.CodeAnalysis;
using System.Drawing;

namespace CourseWork.BicubicInterpolation;

[SuppressMessage("Interoperability", "CA1416:Validate platform compatibility")]
public abstract class ImageResamplingProcessorBase : IDisposable
{
    private readonly object _lockObject = new();

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

            initFactory?.Invoke(inputImage, outputWidth, outputHeight);
        }
        
        IsInitialized = true;
    }

    protected virtual void ReleaseResources()
    {
        Console.WriteLine("Base version called");
    }

    protected virtual void Dispose(bool disposing)
    {
        ReleaseResources();
        if (disposing)
        {
            lock (_lockObject)
            {
                IsInitialized = false;
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
