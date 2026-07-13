namespace FlowPainter.Imaging.Skia.Images;

public enum ImageOperationStage
{
    ReadingEncodedData = 0,
    InspectingMetadata = 1,
    DecodingPixels = 2,
    CreatingProxy = 3,
    EncodingPng = 4,
    EncodingJpeg = 5,
    Completed = 6
}
