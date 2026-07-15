using System.Runtime.CompilerServices;
using Microsoft.Maui.Controls.Shapes;

namespace FitISO.Maui;

public class FancyTabBarShape : Shape
{
    public static readonly BindableProperty InsetWidthProperty = BindableProperty.Create(nameof(InsetWidth), typeof(float), typeof(FancyTabBarShape), 80.0f);
    public static readonly BindableProperty InsetHeightProperty = BindableProperty.Create(nameof(InsetHeight), typeof(float), typeof(FancyTabBarShape), 24.0f);
    public static readonly BindableProperty InsetPositionProperty = BindableProperty.Create(nameof(InsetPosition), typeof(float), typeof(FancyTabBarShape), 0.0f);

    public float InsetWidth
    {
        get => (float)GetValue(InsetWidthProperty);
        set => SetValue(InsetWidthProperty, value);
    }

    public float InsetHeight
    {
        get => (float)GetValue(InsetHeightProperty);
        set => SetValue(InsetHeightProperty, value);
    }

    public float InsetPosition
    {
        get => (float)GetValue(InsetPositionProperty);
        set => SetValue(InsetPositionProperty, value);
    }

    public FancyTabBarShape()
    {
        Aspect = Stretch.Fill;
    }

    public override PathF GetPath()
    {
        var width = (float)GetWidthForPathComputation(this);
        var height = (float)GetHeightForPathComputation(this);

        var insetWidth = InsetWidth;
        var insetCurveWidth = insetWidth / 2;
        var insetCurveBezierWidth = insetCurveWidth / 2;
        var insetHeight = InsetHeight;
        var insetPosition = Math.Clamp(InsetPosition, 0.0f, 1.0f);

        var path = new PathF(0, 0);

        var insetStartX = insetPosition * (width - insetWidth);
        path.LineTo(insetStartX, 0);

        var p1 = path.LastPoint;
        path.CurveTo(
            p1.X + insetCurveBezierWidth, p1.Y,
            p1.X + insetCurveBezierWidth, p1.Y + insetHeight,
            p1.X + insetCurveWidth, p1.Y + insetHeight);

        var p2 = path.LastPoint;
        path.CurveTo(
            p2.X + insetCurveBezierWidth, p2.Y,
            p2.X + insetCurveBezierWidth, p2.Y - insetHeight,
            p2.X + insetCurveWidth, p2.Y - insetHeight);

        path.LineTo(width, 0);
        path.LineTo(width, height);
        path.LineTo(0, height);
        path.LineTo(0, 0);
        path.Close();

        return path;
    }

    [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "get_WidthForPathComputation")]
    private static extern double GetWidthForPathComputation(Shape shape);

    [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "get_HeightForPathComputation")]
    private static extern double GetHeightForPathComputation(Shape shape);
}
