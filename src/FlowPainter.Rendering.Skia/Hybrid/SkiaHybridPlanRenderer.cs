using FlowPainter.Domain.Brushes;
using FlowPainter.Domain.Hybrid;
using FlowPainter.Domain.Images;
using FlowPainter.Imaging.Skia.Images;
using FlowPainter.Rendering.Skia.Primitives;
using FlowPainter.Rendering.Skia.Strokes;

namespace FlowPainter.Rendering.Skia.Hybrid;

public sealed class SkiaHybridPlanRenderer
{
    private readonly SkiaPrimitivePlanRenderer _primitiveRenderer;
    private readonly SkiaStrokePlanRenderer _strokeRenderer;

    public SkiaHybridPlanRenderer()
        : this(new SkiaPrimitivePlanRenderer(), new SkiaStrokePlanRenderer())
    {
    }

    public SkiaHybridPlanRenderer(
        SkiaPrimitivePlanRenderer primitiveRenderer,
        SkiaStrokePlanRenderer strokeRenderer)
    {
        ArgumentNullException.ThrowIfNull(primitiveRenderer);
        ArgumentNullException.ThrowIfNull(strokeRenderer);
        _primitiveRenderer = primitiveRenderer;
        _strokeRenderer = strokeRenderer;
    }

    public async Task<SkiaImage> RenderAsync(
        HybridPlan plan,
        ImageSize outputSize,
        BrushSettings flowBrush,
        BrushSettings refinementBrush,
        IProgress<HybridRenderProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(plan);
        ArgumentNullException.ThrowIfNull(flowBrush);
        ArgumentNullException.ThrowIfNull(refinementBrush);

        ForwardingProgress<PrimitiveRenderProgress>? primitiveProgress = progress is null
            ? null
            : new ForwardingProgress<PrimitiveRenderProgress>(value => progress.Report(
                new HybridRenderProgress(
                    HybridRenderStage.RenderingPrimitives,
                    value.Fraction * 0.35d,
                    $"Rendering primitive layer {value.CompletedPrimitives:N0} / {value.TotalPrimitives:N0}.")));
        using SkiaImage primitiveLayer = await _primitiveRenderer.RenderAsync(
            plan.PrimitivePlan,
            outputSize,
            primitiveProgress,
            cancellationToken).ConfigureAwait(false);

        ForwardingProgress<StrokeRenderProgress>? flowProgress = progress is null
            ? null
            : new ForwardingProgress<StrokeRenderProgress>(value => progress.Report(
                new HybridRenderProgress(
                    HybridRenderStage.RenderingFlowStrokes,
                    0.35d + (value.Fraction * 0.40d),
                    $"Rendering flow layer {value.CompletedStrokes:N0} / {value.TotalStrokes:N0}.")));
        using SkiaImage flowLayer = await _strokeRenderer.RenderAsync(
            plan.FlowStrokePlan,
            outputSize,
            primitiveLayer,
            flowProgress,
            flowBrush,
            cancellationToken).ConfigureAwait(false);

        ForwardingProgress<StrokeRenderProgress>? refinementProgress = progress is null
            ? null
            : new ForwardingProgress<StrokeRenderProgress>(value => progress.Report(
                new HybridRenderProgress(
                    HybridRenderStage.RenderingRefinementStrokes,
                    0.75d + (value.Fraction * 0.25d),
                    $"Rendering refinement layer {value.CompletedStrokes:N0} / {value.TotalStrokes:N0}.")));
        SkiaImage result = await _strokeRenderer.RenderAsync(
            plan.RefinementStrokePlan,
            outputSize,
            flowLayer,
            refinementProgress,
            refinementBrush,
            cancellationToken).ConfigureAwait(false);
        progress?.Report(new HybridRenderProgress(HybridRenderStage.Completed, 1d, "Hybrid rendering completed."));
        return result;
    }
    private sealed class ForwardingProgress<T> : IProgress<T>
    {
        private readonly Action<T> _report;

        public ForwardingProgress(Action<T> report)
        {
            ArgumentNullException.ThrowIfNull(report);
            _report = report;
        }

        public void Report(T value)
        {
            _report(value);
        }
    }

}
