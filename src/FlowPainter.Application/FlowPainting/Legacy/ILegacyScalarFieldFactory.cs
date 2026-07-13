namespace FlowPainter.Application.FlowPainting.Legacy;

public interface ILegacyScalarFieldFactory
{
    ILegacyScalarField Create(int seed);
}
