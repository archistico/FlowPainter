namespace FlowPainter.Domain.Semantics;

public enum SemanticCorrectionKind
{
    ForcePrimarySubject = 0,
    ForceSubject = 1,
    ForceBackground = 2,
    IgnoreAutomaticDetection = 3
}
