namespace CourseWork.BicubicInterpolation.Helpers;

internal static class MathExtensions
{
    public static double[] GetCubicWeights(this double value)
    {
        var weights = new double[4];

        weights[0] = (value + 1).GetCubicWeight();
        weights[1] = value.GetCubicWeight();
        weights[2] = (1 - value).GetCubicWeight();
        weights[3] = (2 - value).GetCubicWeight();

        return weights;
    }

    private static double GetCubicWeight(this double x)
    {
        if (x < 0)
        {
            x = -x;
        }

        return x switch
        {
            <= 1 => 1.5 * x * x * x - 2.5 * x * x + 1,
            < 2 => -0.5 * Math.Pow(x, 3) + 2.5 * Math.Pow(x, 2) - 4 * x + 2,
            _ => 0
        };
    }

    internal static byte Clamp(this double val)
    {
        return val switch
        {
            < 0 => 0,
            > 255 => 255,
            _ => (byte)val
        };
    }
}