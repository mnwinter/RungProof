namespace RungProof.Next.Diagnostics;

public enum DiagnosticSeverity
{
    Information,
    Warning,
    Error,
}

public sealed record DiagnosticIssue(
    string Code,
    DiagnosticSeverity Severity,
    string Scope,
    string Message,
    string Resolution
);
