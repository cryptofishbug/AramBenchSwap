namespace AramBenchSwap.Core
{
    public sealed class WindowBounds
    {
        public WindowBounds(double left, double top, double width, double height)
            : this(left, top, width, height, 1.0, 1.0)
        {
        }

        public WindowBounds(double left, double top, double width, double height, double dpiScaleX, double dpiScaleY)
        {
            Left = left;
            Top = top;
            Width = width;
            Height = height;
            DpiScaleX = dpiScaleX;
            DpiScaleY = dpiScaleY;
        }

        public double Left { get; private set; }
        public double Top { get; private set; }
        public double Width { get; private set; }
        public double Height { get; private set; }
        public double DpiScaleX { get; private set; }
        public double DpiScaleY { get; private set; }
    }

    public sealed class WindowPosition
    {
        public WindowPosition(double left, double top)
        {
            Left = left;
            Top = top;
        }

        public double Left { get; private set; }
        public double Top { get; private set; }
    }

    public static class WindowPlacement
    {
        public static double CalculateOverlayWidth(double baseWidth)
        {
            return baseWidth * 2;
        }

        public static WindowPosition CalculateTopCenter(WindowBounds anchor, double windowWidth, double windowHeight, double margin)
        {
            var left = anchor.Left / anchor.DpiScaleX;
            var top = anchor.Top / anchor.DpiScaleY;
            var width = anchor.Width / anchor.DpiScaleX;
            return new WindowPosition(
                left + (width - windowWidth) / 2,
                top + margin);
        }

        public static WindowPosition CalculateAboveTopCenter(WindowBounds anchor, double windowWidth, double windowHeight, double margin)
        {
            return CalculateAboveTopCenter(anchor, windowWidth, windowHeight, margin, double.NegativeInfinity);
        }

        public static WindowPosition CalculateAboveTopCenter(WindowBounds anchor, double windowWidth, double windowHeight, double margin, double minTop)
        {
            var left = anchor.Left / anchor.DpiScaleX;
            var top = anchor.Top / anchor.DpiScaleY;
            var width = anchor.Width / anchor.DpiScaleX;
            return new WindowPosition(
                left + (width - windowWidth) / 2,
                System.Math.Max(minTop, top - windowHeight - margin));
        }
    }
}
