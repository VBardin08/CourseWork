using CourseWork.BicubicInterpolation.Async;
using CourseWork.BicubicInterpolation.Sync;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Reflection;

namespace CourseWork;

[SuppressMessage("Interoperability", "CA1416:Validate platform compatibility")]
internal static class Program
{
    private static readonly Bitmap Image500X500X3Resampling = new(500, 500);
    private static readonly Bitmap Image1000X1000X2Resampling = new(1000, 1000);
    private static readonly Bitmap Image2000X2000X2Resampling = new(2000, 2000);

    private static readonly ImageResamplingProcessorAsync ProcessorAsync = new();
    private static readonly ImageResamplingProcessorSync ProcessorSync = new();

    private static void Main()
    {
        var tasks = new List<Task>
        {
            Run(Image_500x500_x3_Sync),
            Run(Image_500x500_x3_Async),
            Run(Image_1000x1000_x2_Sync),
            Run(Image_1000x1000_x2_Async),
            Run(Image_2000x2000_x2_Sync),
            Run(Image_2000x2000_x2_Async),
        }.ToArray();

        foreach (var task in tasks)
        {
            task.GetAwaiter().GetResult();
        }
    }

    private static Task Image_500x500_x3_Sync()
    {
        ProcessorSync.BicubicInterpolation(Image500X500X3Resampling, 1500, 1500);
        return Task.CompletedTask;
    }

    private static async Task Image_500x500_x3_Async()
    {
        await ProcessorAsync.BicubicInterpolation(Image500X500X3Resampling, 1500, 1500);
    }

    private static Task Image_1000x1000_x2_Sync()
    {
        ProcessorSync.BicubicInterpolation(Image1000X1000X2Resampling, 2000, 2000);
        return Task.CompletedTask;
    }

    private static async Task Image_1000x1000_x2_Async()
    {
        await ProcessorAsync.BicubicInterpolation(Image2000X2000X2Resampling, 2000, 2000);
    }

    private static Task Image_2000x2000_x2_Sync()
    {
        ProcessorSync.BicubicInterpolation(Image500X500X3Resampling, 4000, 4000);
        return Task.CompletedTask;
    }

    private static async Task Image_2000x2000_x2_Async()
    {
        await ProcessorAsync.BicubicInterpolation(Image500X500X3Resampling, 4000, 4000);
    }
    
    private static async Task Run(Func<Task> action)
    {
        var sw = new Stopwatch();

        sw.Start();

        await action();

        sw.Stop();
        Console.WriteLine(action.GetMethodInfo().Name +
                          " is completed in " +
                          sw.ElapsedMilliseconds + "ms");
    }
}