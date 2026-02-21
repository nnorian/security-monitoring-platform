public record SecurityLog(
    Guid Id,
    string Source,
    string Format,
    string RawMessage,
    string? SourceIp,
    DateTime TImestamp,
    string Severity
);