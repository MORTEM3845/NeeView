namespace NeeView
{
    public record DiagnosticSpiderState(string Status, int ProcessId, string Executable, string Profile, string? BookPath, int PageCount, string? CurrentEntry, bool IsLoading, string Revision, string DiagnosticPipe);
}
