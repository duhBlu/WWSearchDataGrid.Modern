using System;
using System.Collections.Generic;

namespace WWSearchDataGrid.Modern.Core
{
    public enum FilterStringDiagnosticSeverity
    {
        Warning,
        Error
    }

    public sealed class FilterStringDiagnostic
    {
        public FilterStringDiagnostic(FilterStringDiagnosticSeverity severity, string message, int position)
        {
            Severity = severity;
            Message = message;
            Position = position;
        }

        public FilterStringDiagnosticSeverity Severity { get; }
        public string Message { get; }
        public int Position { get; }

        public override string ToString()
            => $"[{Severity} @ {Position}] {Message}";
    }

    public sealed class FilterStringClause
    {
        public string FieldName { get; set; }
        public SearchType SearchType { get; set; }
        public string RawPrimary { get; set; }
        public string RawSecondary { get; set; }
        public IReadOnlyList<string> RawValues { get; set; }
        public DateInterval? DateInterval { get; set; }
        public string Combinator { get; set; }
        public int GroupIndex { get; set; }
    }

    public sealed class FilterStringParseResult
    {
        public FilterStringParseResult(
            IReadOnlyList<FilterStringClause> clauses,
            IReadOnlyList<FilterStringDiagnostic> diagnostics,
            bool isFatal)
        {
            Clauses = clauses ?? Array.Empty<FilterStringClause>();
            Diagnostics = diagnostics ?? Array.Empty<FilterStringDiagnostic>();
            IsFatal = isFatal;
        }

        public IReadOnlyList<FilterStringClause> Clauses { get; }
        public IReadOnlyList<FilterStringDiagnostic> Diagnostics { get; }
        public bool IsFatal { get; }

        public static FilterStringParseResult Empty { get; }
            = new FilterStringParseResult(Array.Empty<FilterStringClause>(), Array.Empty<FilterStringDiagnostic>(), false);
    }
}
