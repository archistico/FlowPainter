namespace FlowPainter.Domain.Primitives;

[Flags]
public enum PrimitiveKindSet
{
    None = 0,
    Triangle = 1 << 0,
    Rectangle = 1 << 1,
    RotatedRectangle = 1 << 2,
    Circle = 1 << 3,
    Ellipse = 1 << 4,
    All = Triangle | Rectangle | RotatedRectangle | Circle | Ellipse
}
