using System.Drawing;

namespace testSoulChat;

public static class CoordinateMapper
{
    public static Point RelativeToAbsolute(double relativeX, double relativeY, Rectangle windowRect)
    {
        var x = windowRect.Left + (int)Math.Round(windowRect.Width * Clamp01(relativeX));
        var y = windowRect.Top + (int)Math.Round(windowRect.Height * Clamp01(relativeY));
        return new Point(x, y);
    }

    public static Point ApplyJitter(Point original, int jitterPx = 10)
    {
        var random = Random.Shared;
        var offsetX = random.Next(-jitterPx, jitterPx + 1);
        var offsetY = random.Next(-jitterPx, jitterPx + 1);
        return new Point(original.X + offsetX, original.Y + offsetY);
    }

    private static double Clamp01(double value) => Math.Max(0, Math.Min(1, value));
}
