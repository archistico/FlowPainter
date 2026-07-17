namespace FlowPainter.Domain.Segmentation;

public enum RegionRole
{
    Background = 0,
    Supporting = 1,
    Subject = 2,
    Focal = 3,
    CriticalDetail = 4,
    Ignore = 5
}
