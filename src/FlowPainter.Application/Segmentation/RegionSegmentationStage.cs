namespace FlowPainter.Application.Segmentation;

public enum RegionSegmentationStage
{
    Preparing = 0,
    Smoothing = 1,
    ConvertingColor = 2,
    InitializingClusters = 3,
    AssigningPixels = 4,
    UpdatingClusters = 5,
    RepairingConnectivity = 6,
    BuildingResult = 7,
    Completed = 8,
}
