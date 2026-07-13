namespace FlowPainter.Application.FlowPainting.Fields;

public interface IFlowFieldFactory
{
    IFlowField Create(int seed, FlowFieldSettings settings);
}
