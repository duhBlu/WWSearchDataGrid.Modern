using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace WWControls.Core
{
    /// <summary>
    /// Parses a DevExpress-style <c>CriteriaOperator</c> subset into a flat list of
    /// <see cref="FilterStringClause"/> rows grouped to fit the 2-level
    /// <see cref="SearchTemplateController"/> model (groups of templates).
    /// </summary>
    /// <remarks>
    /// Grammar (informal):
    /// <code>
    /// filter      := orExpr | empty
    /// orExpr      := andExpr ("Or" andExpr)*
    /// andExpr     := unary ("And" unary)*
    /// unary       := "Not" unary | factor
    /// factor      := "(" orExpr ")" | clause
    /// clause      := fieldClause | functionClause
    /// fieldClause := field op operand?
    /// functionClause := identifier "(" field ("," scalar)* ")"
    /// field       := "[" identifier "]"
    /// </code>
    /// Operator precedence: <c>And</c> binds tighter than <c>Or</c>; <c>Not</c> binds tightest.
    /// The parser builds the tree then normalises to a flat list of groups; expressions that
    /// cannot fit the 2-level model (e.g. nested mixed And/Or groups) emit a fatal diagnostic.
    /// </remarks>
    public static class FilterStringParser
    {
        public static FilterStringParseResult Parse(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return FilterStringParseResult.Empty;

            var diagnostics = new List<FilterStringDiagnostic>();
            var tokenizer = new Tokenizer(input, diagnostics);
            var tokens = tokenizer.Tokenize();
            if (HasError(diagnostics))
                return new FilterStringParseResult(Array.Empty<FilterStringClause>(), diagnostics, true);

            var parser = new ExpressionParser(tokens, diagnostics);
            var tree = parser.ParseOrExpression();
            if (HasError(diagnostics))
                return new FilterStringParseResult(Array.Empty<FilterStringClause>(), diagnostics, true);
            if (parser.HasRemaining)
            {
                diagnostics.Add(new FilterStringDiagnostic(
                    FilterStringDiagnosticSeverity.Error,
                    $"Unexpected token '{parser.Peek()?.Text}' after expression.",
                    parser.Peek()?.Position ?? 0));
                return new FilterStringParseResult(Array.Empty<FilterStringClause>(), diagnostics, true);
            }

            var normaliser = new Normaliser(diagnostics);
            var clauses = normaliser.Normalise(tree);
            if (HasError(diagnostics))
                return new FilterStringParseResult(Array.Empty<FilterStringClause>(), diagnostics, true);

            return new FilterStringParseResult(clauses, diagnostics, false);
        }

        private static bool HasError(List<FilterStringDiagnostic> diagnostics)
        {
            for (int i = 0; i < diagnostics.Count; i++)
                if (diagnostics[i].Severity == FilterStringDiagnosticSeverity.Error)
                    return true;
            return false;
        }

        #region Tokenizer

        private enum TokenKind
        {
            Field,
            Ident,
            String,
            Number,
            Date,
            LParen,
            RParen,
            Comma,
            Eq,
            NotEq,
            Lt,
            Le,
            Gt,
            Ge,
            End
        }

        private sealed class Token
        {
            public Token(TokenKind kind, string text, int position)
            {
                Kind = kind;
                Text = text;
                Position = position;
            }

            public TokenKind Kind { get; }
            public string Text { get; }
            public int Position { get; }
        }

        private sealed class Tokenizer
        {
            private readonly string _input;
            private readonly List<FilterStringDiagnostic> _diagnostics;
            private int _pos;

            public Tokenizer(string input, List<FilterStringDiagnostic> diagnostics)
            {
                _input = input;
                _diagnostics = diagnostics;
            }

            public List<Token> Tokenize()
            {
                var tokens = new List<Token>();
                while (_pos < _input.Length)
                {
                    char c = _input[_pos];
                    if (char.IsWhiteSpace(c)) { _pos++; continue; }

                    int start = _pos;

                    if (c == '[') { tokens.Add(ReadField()); continue; }
                    if (c == '(') { _pos++; tokens.Add(new Token(TokenKind.LParen, "(", start)); continue; }
                    if (c == ')') { _pos++; tokens.Add(new Token(TokenKind.RParen, ")", start)); continue; }
                    if (c == ',') { _pos++; tokens.Add(new Token(TokenKind.Comma, ",", start)); continue; }
                    if (c == '=') { _pos++; tokens.Add(new Token(TokenKind.Eq, "=", start)); continue; }
                    if (c == '<')
                    {
                        _pos++;
                        if (_pos < _input.Length && _input[_pos] == '=') { _pos++; tokens.Add(new Token(TokenKind.Le, "<=", start)); }
                        else if (_pos < _input.Length && _input[_pos] == '>') { _pos++; tokens.Add(new Token(TokenKind.NotEq, "<>", start)); }
                        else tokens.Add(new Token(TokenKind.Lt, "<", start));
                        continue;
                    }
                    if (c == '>')
                    {
                        _pos++;
                        if (_pos < _input.Length && _input[_pos] == '=') { _pos++; tokens.Add(new Token(TokenKind.Ge, ">=", start)); }
                        else tokens.Add(new Token(TokenKind.Gt, ">", start));
                        continue;
                    }
                    if (c == '!' && _pos + 1 < _input.Length && _input[_pos + 1] == '=')
                    {
                        _pos += 2;
                        tokens.Add(new Token(TokenKind.NotEq, "!=", start));
                        continue;
                    }
                    if (c == '\'') { tokens.Add(ReadString()); continue; }
                    if (c == '#') { tokens.Add(ReadDate()); continue; }
                    if (char.IsDigit(c) || (c == '-' && _pos + 1 < _input.Length && char.IsDigit(_input[_pos + 1])))
                    {
                        tokens.Add(ReadNumber());
                        continue;
                    }
                    if (IsIdentStart(c)) { tokens.Add(ReadIdent()); continue; }

                    _diagnostics.Add(new FilterStringDiagnostic(
                        FilterStringDiagnosticSeverity.Error,
                        $"Unexpected character '{c}'.", _pos));
                    _pos++;
                }
                tokens.Add(new Token(TokenKind.End, string.Empty, _input.Length));
                return tokens;
            }

            private Token ReadField()
            {
                int start = _pos;
                _pos++;
                var sb = new StringBuilder();
                while (_pos < _input.Length && _input[_pos] != ']')
                {
                    sb.Append(_input[_pos]);
                    _pos++;
                }
                if (_pos >= _input.Length)
                {
                    _diagnostics.Add(new FilterStringDiagnostic(
                        FilterStringDiagnosticSeverity.Error,
                        "Unterminated field reference; missing ']'.", start));
                    return new Token(TokenKind.Field, sb.ToString(), start);
                }
                _pos++;
                return new Token(TokenKind.Field, sb.ToString(), start);
            }

            private Token ReadString()
            {
                int start = _pos;
                _pos++;
                var sb = new StringBuilder();
                while (_pos < _input.Length)
                {
                    char ch = _input[_pos];
                    if (ch == '\'')
                    {
                        if (_pos + 1 < _input.Length && _input[_pos + 1] == '\'')
                        {
                            sb.Append('\'');
                            _pos += 2;
                            continue;
                        }
                        _pos++;
                        return new Token(TokenKind.String, sb.ToString(), start);
                    }
                    sb.Append(ch);
                    _pos++;
                }
                _diagnostics.Add(new FilterStringDiagnostic(
                    FilterStringDiagnosticSeverity.Error,
                    "Unterminated string literal.", start));
                return new Token(TokenKind.String, sb.ToString(), start);
            }

            private Token ReadDate()
            {
                int start = _pos;
                _pos++;
                var sb = new StringBuilder();
                while (_pos < _input.Length && _input[_pos] != '#')
                {
                    sb.Append(_input[_pos]);
                    _pos++;
                }
                if (_pos >= _input.Length)
                {
                    _diagnostics.Add(new FilterStringDiagnostic(
                        FilterStringDiagnosticSeverity.Error,
                        "Unterminated date literal; missing '#'.", start));
                    return new Token(TokenKind.Date, sb.ToString(), start);
                }
                _pos++;
                return new Token(TokenKind.Date, sb.ToString(), start);
            }

            private Token ReadNumber()
            {
                int start = _pos;
                if (_input[_pos] == '-') _pos++;
                while (_pos < _input.Length && (char.IsDigit(_input[_pos]) || _input[_pos] == '.'))
                    _pos++;
                return new Token(TokenKind.Number, _input.Substring(start, _pos - start), start);
            }

            private Token ReadIdent()
            {
                int start = _pos;
                while (_pos < _input.Length && IsIdentPart(_input[_pos]))
                    _pos++;
                return new Token(TokenKind.Ident, _input.Substring(start, _pos - start), start);
            }

            private static bool IsIdentStart(char c)
                => char.IsLetter(c) || c == '_';

            private static bool IsIdentPart(char c)
                => char.IsLetterOrDigit(c) || c == '_';
        }

        #endregion

        #region Expression tree

        private abstract class Node
        {
            public int Position;
        }

        private sealed class OrNode : Node
        {
            public readonly List<AndNode> Children = new List<AndNode>();
        }

        private sealed class AndNode : Node
        {
            public readonly List<UnaryNode> Children = new List<UnaryNode>();
        }

        private sealed class UnaryNode : Node
        {
            public bool IsNegated;
            public FactorNode Factor;
        }

        private abstract class FactorNode : Node { }

        private sealed class ParenNode : FactorNode
        {
            public OrNode Inner;
        }

        private sealed class ClauseNode : FactorNode
        {
            public string FieldName;
            public SearchType SearchType;
            public string RawPrimary;
            public string RawSecondary;
            public List<string> RawValues;
            public DateInterval? DateInterval;
        }

        #endregion

        #region Parser

        private sealed class ExpressionParser
        {
            private readonly List<Token> _tokens;
            private readonly List<FilterStringDiagnostic> _diagnostics;
            private int _pos;

            public ExpressionParser(List<Token> tokens, List<FilterStringDiagnostic> diagnostics)
            {
                _tokens = tokens;
                _diagnostics = diagnostics;
            }

            public bool HasRemaining => Peek().Kind != TokenKind.End;

            public Token Peek() => _tokens[_pos];

            private Token Consume() => _tokens[_pos++];

            private bool Match(TokenKind kind)
            {
                if (_tokens[_pos].Kind == kind) { _pos++; return true; }
                return false;
            }

            private bool MatchKeyword(string keyword)
            {
                var t = _tokens[_pos];
                if (t.Kind == TokenKind.Ident && string.Equals(t.Text, keyword, StringComparison.OrdinalIgnoreCase))
                {
                    _pos++;
                    return true;
                }
                return false;
            }

            private bool PeekKeyword(string keyword)
            {
                var t = _tokens[_pos];
                return t.Kind == TokenKind.Ident && string.Equals(t.Text, keyword, StringComparison.OrdinalIgnoreCase);
            }

            public OrNode ParseOrExpression()
            {
                var node = new OrNode { Position = Peek().Position };
                node.Children.Add(ParseAndExpression());
                while (MatchKeyword("Or"))
                    node.Children.Add(ParseAndExpression());
                return node;
            }

            private AndNode ParseAndExpression()
            {
                var node = new AndNode { Position = Peek().Position };
                node.Children.Add(ParseUnary());
                while (PeekKeyword("And") && !LookaheadBetweenSecondOperand())
                {
                    Consume();
                    node.Children.Add(ParseUnary());
                }
                return node;
            }

            // Distinguish "x Between a And b" (the And here is part of the Between operand)
            // from "[F] = 1 And [G] = 2". Heuristic: if we just finished parsing a Between operand
            // pair, the inner-And consumer in ParseBetweenOperand() has already swallowed it.
            // This guard is a safety net for the top-level loop and always returns false in the
            // current grammar — Between is fully consumed within ParseFieldClause.
            private bool LookaheadBetweenSecondOperand() => false;

            private UnaryNode ParseUnary()
            {
                var node = new UnaryNode { Position = Peek().Position };
                if (MatchKeyword("Not"))
                {
                    node.IsNegated = true;
                    var inner = ParseUnary();
                    node.IsNegated ^= inner.IsNegated;
                    node.Factor = inner.Factor;
                    return node;
                }
                node.Factor = ParseFactor();
                return node;
            }

            private FactorNode ParseFactor()
            {
                if (Match(TokenKind.LParen))
                {
                    var paren = new ParenNode { Position = _tokens[_pos - 1].Position };
                    paren.Inner = ParseOrExpression();
                    if (!Match(TokenKind.RParen))
                    {
                        _diagnostics.Add(new FilterStringDiagnostic(
                            FilterStringDiagnosticSeverity.Error,
                            "Missing ')'.", Peek().Position));
                    }
                    return paren;
                }
                return ParseClause();
            }

            private ClauseNode ParseClause()
            {
                var t = Peek();

                if (t.Kind == TokenKind.Field)
                    return ParseFieldClause();

                if (t.Kind == TokenKind.Ident)
                    return ParseFunctionClause();

                _diagnostics.Add(new FilterStringDiagnostic(
                    FilterStringDiagnosticSeverity.Error,
                    $"Expected field reference or function name, got '{t.Text}'.", t.Position));
                Consume();
                return new ClauseNode { Position = t.Position, FieldName = string.Empty };
            }

            private ClauseNode ParseFieldClause()
            {
                var fieldTok = Consume();
                var clause = new ClauseNode { Position = fieldTok.Position, FieldName = fieldTok.Text };

                var opTok = Peek();
                switch (opTok.Kind)
                {
                    case TokenKind.Eq: Consume(); clause.SearchType = SearchType.Equals; clause.RawPrimary = ReadScalar(out var _); break;
                    case TokenKind.NotEq: Consume(); clause.SearchType = SearchType.NotEquals; clause.RawPrimary = ReadScalar(out var _); break;
                    case TokenKind.Lt: Consume(); clause.SearchType = SearchType.LessThan; clause.RawPrimary = ReadScalar(out var _); break;
                    case TokenKind.Le: Consume(); clause.SearchType = SearchType.LessThanOrEqualTo; clause.RawPrimary = ReadScalar(out var _); break;
                    case TokenKind.Gt: Consume(); clause.SearchType = SearchType.GreaterThan; clause.RawPrimary = ReadScalar(out var _); break;
                    case TokenKind.Ge: Consume(); clause.SearchType = SearchType.GreaterThanOrEqualTo; clause.RawPrimary = ReadScalar(out var _); break;
                    case TokenKind.Ident:
                        ParseFieldKeywordOp(clause);
                        break;
                    default:
                        _diagnostics.Add(new FilterStringDiagnostic(
                            FilterStringDiagnosticSeverity.Error,
                            $"Expected operator after [{clause.FieldName}], got '{opTok.Text}'.", opTok.Position));
                        break;
                }

                // Coerce "= Null" → IsNull (and "<> Null" → IsNotNull) so consumers can write either form.
                if ((clause.SearchType == SearchType.Equals || clause.SearchType == SearchType.NotEquals)
                    && string.Equals(clause.RawPrimary, "__NULL__", StringComparison.Ordinal))
                {
                    clause.SearchType = clause.SearchType == SearchType.Equals
                        ? SearchType.IsNull
                        : SearchType.IsNotNull;
                    clause.RawPrimary = null;
                }

                return clause;
            }

            private void ParseFieldKeywordOp(ClauseNode clause)
            {
                var keyword = Peek().Text;
                if (string.Equals(keyword, "Between", StringComparison.OrdinalIgnoreCase))
                {
                    Consume();
                    clause.SearchType = SearchType.Between;
                    clause.RawPrimary = ReadScalar(out var _);
                    if (!MatchKeyword("And"))
                    {
                        _diagnostics.Add(new FilterStringDiagnostic(
                            FilterStringDiagnosticSeverity.Error,
                            "Expected 'And' between the two operands of 'Between'.", Peek().Position));
                        return;
                    }
                    clause.RawSecondary = ReadScalar(out var _);
                    return;
                }
                if (string.Equals(keyword, "In", StringComparison.OrdinalIgnoreCase))
                {
                    Consume();
                    clause.SearchType = SearchType.IsAnyOf;
                    if (!Match(TokenKind.LParen))
                    {
                        _diagnostics.Add(new FilterStringDiagnostic(
                            FilterStringDiagnosticSeverity.Error,
                            "Expected '(' after 'In'.", Peek().Position));
                        return;
                    }
                    clause.RawValues = ReadScalarList();
                    return;
                }
                if (string.Equals(keyword, "Like", StringComparison.OrdinalIgnoreCase))
                {
                    Consume();
                    clause.SearchType = SearchType.IsLike;
                    clause.RawPrimary = ReadScalar(out var _);
                    return;
                }
                if (string.Equals(keyword, "Is", StringComparison.OrdinalIgnoreCase))
                {
                    // Allow "[F] Is Null" and "[F] Is Not Null"
                    Consume();
                    bool negate = MatchKeyword("Not");
                    if (!MatchKeyword("Null"))
                    {
                        _diagnostics.Add(new FilterStringDiagnostic(
                            FilterStringDiagnosticSeverity.Error,
                            "Expected 'Null' after 'Is'.", Peek().Position));
                        return;
                    }
                    clause.SearchType = negate ? SearchType.IsNotNull : SearchType.IsNull;
                    return;
                }

                _diagnostics.Add(new FilterStringDiagnostic(
                    FilterStringDiagnosticSeverity.Error,
                    $"Unrecognised operator '{keyword}'.", Peek().Position));
                Consume();
            }

            private ClauseNode ParseFunctionClause()
            {
                var nameTok = Consume();
                if (!Match(TokenKind.LParen))
                {
                    _diagnostics.Add(new FilterStringDiagnostic(
                        FilterStringDiagnosticSeverity.Error,
                        $"Expected '(' after function name '{nameTok.Text}'.", Peek().Position));
                    return new ClauseNode { Position = nameTok.Position, FieldName = string.Empty };
                }

                if (Peek().Kind != TokenKind.Field)
                {
                    _diagnostics.Add(new FilterStringDiagnostic(
                        FilterStringDiagnosticSeverity.Error,
                        $"First argument to '{nameTok.Text}' must be a [field] reference.", Peek().Position));
                }
                var fieldTok = Peek().Kind == TokenKind.Field ? Consume() : new Token(TokenKind.Field, string.Empty, Peek().Position);

                var extraArgs = new List<string>();
                while (Match(TokenKind.Comma))
                    extraArgs.Add(ReadScalar(out var _));

                if (!Match(TokenKind.RParen))
                {
                    _diagnostics.Add(new FilterStringDiagnostic(
                        FilterStringDiagnosticSeverity.Error,
                        $"Missing ')' after function call '{nameTok.Text}'.", Peek().Position));
                }

                var clause = new ClauseNode
                {
                    Position = nameTok.Position,
                    FieldName = fieldTok.Text
                };

                if (!FunctionRegistry.TryResolve(nameTok.Text, extraArgs.Count, out var resolved))
                {
                    _diagnostics.Add(new FilterStringDiagnostic(
                        FilterStringDiagnosticSeverity.Error,
                        $"Unknown function '{nameTok.Text}' or wrong argument count.", nameTok.Position));
                    return clause;
                }

                clause.SearchType = resolved.SearchType;
                clause.DateInterval = resolved.DateInterval;
                if (resolved.PrimaryArgIndex.HasValue)
                    clause.RawPrimary = extraArgs[resolved.PrimaryArgIndex.Value];
                return clause;
            }

            private string ReadScalar(out int position)
            {
                var t = Peek();
                position = t.Position;
                switch (t.Kind)
                {
                    case TokenKind.String:
                        Consume();
                        return t.Text;
                    case TokenKind.Number:
                        Consume();
                        return t.Text;
                    case TokenKind.Date:
                        Consume();
                        return t.Text;
                    case TokenKind.Ident:
                        if (string.Equals(t.Text, "True", StringComparison.OrdinalIgnoreCase)) { Consume(); return "True"; }
                        if (string.Equals(t.Text, "False", StringComparison.OrdinalIgnoreCase)) { Consume(); return "False"; }
                        if (string.Equals(t.Text, "Null", StringComparison.OrdinalIgnoreCase)) { Consume(); return "__NULL__"; }
                        _diagnostics.Add(new FilterStringDiagnostic(
                            FilterStringDiagnosticSeverity.Error,
                            $"Expected scalar value, got identifier '{t.Text}'.", t.Position));
                        Consume();
                        return null;
                    default:
                        _diagnostics.Add(new FilterStringDiagnostic(
                            FilterStringDiagnosticSeverity.Error,
                            $"Expected scalar value, got '{t.Text}'.", t.Position));
                        Consume();
                        return null;
                }
            }

            private List<string> ReadScalarList()
            {
                var list = new List<string>();
                if (Peek().Kind == TokenKind.RParen) { Consume(); return list; }
                list.Add(ReadScalar(out var _));
                while (Match(TokenKind.Comma))
                    list.Add(ReadScalar(out var _));
                if (!Match(TokenKind.RParen))
                {
                    _diagnostics.Add(new FilterStringDiagnostic(
                        FilterStringDiagnosticSeverity.Error,
                        "Missing ')' after list.", Peek().Position));
                }
                return list;
            }
        }

        #endregion

        #region Function registry

        private readonly struct ResolvedFunction
        {
            public ResolvedFunction(SearchType searchType, DateInterval? dateInterval, int? primaryArgIndex)
            {
                SearchType = searchType;
                DateInterval = dateInterval;
                PrimaryArgIndex = primaryArgIndex;
            }

            public SearchType SearchType { get; }
            public DateInterval? DateInterval { get; }
            public int? PrimaryArgIndex { get; }
        }

        private static class FunctionRegistry
        {
            private static readonly Dictionary<string, (SearchType type, DateInterval? interval, int extraArgs, int? primaryIdx)> _map
                = new Dictionary<string, (SearchType, DateInterval?, int, int?)>(StringComparer.OrdinalIgnoreCase)
                {
                    // Null
                    ["IsNull"] = (SearchType.IsNull, null, 0, null),
                    ["IsNotNull"] = (SearchType.IsNotNull, null, 0, null),

                    // Today / Yesterday (no-input search types)
                    ["IsToday"] = (SearchType.Today, null, 0, null),
                    ["IsOutlookIntervalToday"] = (SearchType.Today, null, 0, null),
                    ["IsYesterday"] = (SearchType.Yesterday, null, 0, null),
                    ["IsOutlookIntervalYesterday"] = (SearchType.Yesterday, null, 0, null),

                    // DateInterval-backed predicates — map to one named interval each
                    ["IsTomorrow"] = (SearchType.DateInterval, Core.DateInterval.Tomorrow, 0, null),
                    ["IsOutlookIntervalTomorrow"] = (SearchType.DateInterval, Core.DateInterval.Tomorrow, 0, null),
                    ["IsLastWeek"] = (SearchType.DateInterval, Core.DateInterval.LastWeek, 0, null),
                    ["IsOutlookIntervalLastWeek"] = (SearchType.DateInterval, Core.DateInterval.LastWeek, 0, null),
                    ["IsNextWeek"] = (SearchType.DateInterval, Core.DateInterval.NextWeek, 0, null),
                    ["IsOutlookIntervalNextWeek"] = (SearchType.DateInterval, Core.DateInterval.NextWeek, 0, null),
                    ["IsOutlookIntervalEarlierThisWeek"] = (SearchType.DateInterval, Core.DateInterval.EarlierThisWeek, 0, null),
                    ["IsOutlookIntervalLaterThisWeek"] = (SearchType.DateInterval, Core.DateInterval.LaterThisWeek, 0, null),
                    ["IsOutlookIntervalEarlierThisMonth"] = (SearchType.DateInterval, Core.DateInterval.EarlierThisMonth, 0, null),
                    ["IsOutlookIntervalLaterThisMonth"] = (SearchType.DateInterval, Core.DateInterval.LaterThisMonth, 0, null),
                    ["IsOutlookIntervalEarlierThisYear"] = (SearchType.DateInterval, Core.DateInterval.EarlierThisYear, 0, null),
                    ["IsOutlookIntervalLaterThisYear"] = (SearchType.DateInterval, Core.DateInterval.LaterThisYear, 0, null),
                    ["IsOutlookIntervalBeyondThisYear"] = (SearchType.DateInterval, Core.DateInterval.BeyondThisYear, 0, null),
                    ["IsOutlookIntervalPriorThisYear"] = (SearchType.DateInterval, Core.DateInterval.PriorThisYear, 0, null),

                    // String predicates (one extra arg = the value)
                    ["Contains"] = (SearchType.Contains, null, 1, 0),
                    ["StartsWith"] = (SearchType.StartsWith, null, 1, 0),
                    ["EndsWith"] = (SearchType.EndsWith, null, 1, 0),

                    // Statistical / uniqueness no-input predicates
                    ["AboveAverage"] = (SearchType.AboveAverage, null, 0, null),
                    ["BelowAverage"] = (SearchType.BelowAverage, null, 0, null),
                    ["Unique"] = (SearchType.Unique, null, 0, null),
                    ["Duplicate"] = (SearchType.Duplicate, null, 0, null)
                };

            public static bool TryResolve(string name, int extraArgs, out ResolvedFunction resolved)
            {
                if (_map.TryGetValue(name, out var entry) && entry.extraArgs == extraArgs)
                {
                    resolved = new ResolvedFunction(entry.type, entry.interval, entry.primaryIdx);
                    return true;
                }
                resolved = default;
                return false;
            }
        }

        #endregion

        #region Normaliser

        private sealed class Normaliser
        {
            private readonly List<FilterStringDiagnostic> _diagnostics;

            public Normaliser(List<FilterStringDiagnostic> diagnostics)
            {
                _diagnostics = diagnostics;
            }

            public IReadOnlyList<FilterStringClause> Normalise(OrNode root)
            {
                var groups = new List<List<(ClauseNode clause, string withinOp)>>();
                var groupCombinators = new List<string>(); // "And"/"Or"/null (first)

                for (int i = 0; i < root.Children.Count; i++)
                {
                    var subGroups = NormaliseAnd(root.Children[i]);
                    if (subGroups == null) return Array.Empty<FilterStringClause>();
                    for (int j = 0; j < subGroups.Count; j++)
                    {
                        groups.Add(subGroups[j]);
                        if (groups.Count == 1)
                            groupCombinators.Add(null);
                        else if (j == 0)
                            groupCombinators.Add("Or");
                        else
                            groupCombinators.Add("And");
                    }
                }

                var result = new List<FilterStringClause>();
                for (int gi = 0; gi < groups.Count; gi++)
                {
                    var grp = groups[gi];
                    for (int ci = 0; ci < grp.Count; ci++)
                    {
                        var (clause, withinOp) = grp[ci];
                        var emitted = ToFilterStringClause(clause);
                        emitted.GroupIndex = gi;
                        if (ci == 0)
                            emitted.Combinator = groupCombinators[gi]; // group-to-previous-group
                        else
                            emitted.Combinator = withinOp; // template-to-previous-template
                        result.Add(emitted);
                    }
                }
                return result;
            }

            // Returns one or more groups produced by this AndExpr. Each Unary becomes either a
            // member of the current implicit AND-group OR (when a paren wraps a non-trivial
            // sub-expression) its own group.
            private List<List<(ClauseNode, string)>> NormaliseAnd(AndNode and)
            {
                var groups = new List<List<(ClauseNode, string)>>();
                List<(ClauseNode, string)> pending = null;

                foreach (var unary in and.Children)
                {
                    var resolved = ResolveUnary(unary);
                    if (resolved.IsNull) return null;

                    if (resolved.IsLeaf)
                    {
                        if (pending == null) pending = new List<(ClauseNode, string)>();
                        pending.Add((resolved.Leaf, "And"));
                    }
                    else
                    {
                        if (pending != null) { groups.Add(pending); pending = null; }
                        groups.Add(resolved.SubGroup);
                    }
                }

                if (pending != null) groups.Add(pending);
                return groups;
            }

            private ResolvedUnary ResolveUnary(UnaryNode unary)
            {
                var factor = unary.Factor;
                if (factor is ClauseNode clause)
                {
                    if (unary.IsNegated) clause = NegateClause(clause);
                    return ResolvedUnary.AsLeaf(clause);
                }

                if (factor is ParenNode paren)
                {
                    var subResult = ResolveParen(paren.Inner);
                    if (subResult.IsNull) return ResolvedUnary.Null();

                    if (subResult.IsLeaf)
                    {
                        var leaf = subResult.Leaf;
                        if (unary.IsNegated) leaf = NegateClause(leaf);
                        return ResolvedUnary.AsLeaf(leaf);
                    }

                    if (unary.IsNegated)
                    {
                        _diagnostics.Add(new FilterStringDiagnostic(
                            FilterStringDiagnosticSeverity.Error,
                            "'Not' may only be applied to a single clause, not a parenthesised group.",
                            unary.Position));
                        return ResolvedUnary.Null();
                    }
                    return ResolvedUnary.AsGroup(subResult.SubGroup);
                }

                _diagnostics.Add(new FilterStringDiagnostic(
                    FilterStringDiagnosticSeverity.Error,
                    "Unsupported factor in expression.",
                    unary.Position));
                return ResolvedUnary.Null();
            }

            // Reduces a parenthesised sub-expression to either a single leaf clause or a single
            // group of leaves joined by one uniform operator. Deeper nesting is rejected.
            private ResolvedUnary ResolveParen(OrNode inner)
            {
                if (inner.Children.Count == 1)
                {
                    var and = inner.Children[0];
                    if (and.Children.Count == 1)
                        return ResolveUnary(and.Children[0]);

                    // Multiple AND siblings — must all reduce to leaves
                    var clauses = new List<(ClauseNode, string)>();
                    foreach (var u in and.Children)
                    {
                        var r = ResolveUnary(u);
                        if (r.IsNull) return ResolvedUnary.Null();
                        if (!r.IsLeaf)
                        {
                            _diagnostics.Add(new FilterStringDiagnostic(
                                FilterStringDiagnosticSeverity.Error,
                                "Filter expression is nested too deeply for the 2-level filter model.",
                                u.Position));
                            return ResolvedUnary.Null();
                        }
                        clauses.Add((r.Leaf, "And"));
                    }
                    return ResolvedUnary.AsGroup(clauses);
                }
                else
                {
                    // Multiple OR siblings — each AndExpr must collapse to one leaf
                    var clauses = new List<(ClauseNode, string)>();
                    for (int i = 0; i < inner.Children.Count; i++)
                    {
                        var and = inner.Children[i];
                        if (and.Children.Count != 1)
                        {
                            _diagnostics.Add(new FilterStringDiagnostic(
                                FilterStringDiagnosticSeverity.Error,
                                "Parenthesised group cannot mix 'And' and 'Or' at the same nesting level; the 2-level filter model requires a single uniform operator inside a group.",
                                and.Position));
                            return ResolvedUnary.Null();
                        }
                        var r = ResolveUnary(and.Children[0]);
                        if (r.IsNull) return ResolvedUnary.Null();
                        if (!r.IsLeaf)
                        {
                            _diagnostics.Add(new FilterStringDiagnostic(
                                FilterStringDiagnosticSeverity.Error,
                                "Filter expression is nested too deeply for the 2-level filter model.",
                                and.Position));
                            return ResolvedUnary.Null();
                        }
                        clauses.Add((r.Leaf, i == 0 ? "And" : "Or"));
                    }
                    return ResolvedUnary.AsGroup(clauses);
                }
            }

            private static ClauseNode NegateClause(ClauseNode c)
            {
                var n = new ClauseNode
                {
                    Position = c.Position,
                    FieldName = c.FieldName,
                    RawPrimary = c.RawPrimary,
                    RawSecondary = c.RawSecondary,
                    RawValues = c.RawValues,
                    DateInterval = c.DateInterval,
                    SearchType = InvertSearchType(c.SearchType)
                };
                return n;
            }

            private static SearchType InvertSearchType(SearchType t)
            {
                switch (t)
                {
                    case SearchType.Equals: return SearchType.NotEquals;
                    case SearchType.NotEquals: return SearchType.Equals;
                    case SearchType.Contains: return SearchType.DoesNotContain;
                    case SearchType.DoesNotContain: return SearchType.Contains;
                    case SearchType.IsLike: return SearchType.IsNotLike;
                    case SearchType.IsNotLike: return SearchType.IsLike;
                    case SearchType.IsNull: return SearchType.IsNotNull;
                    case SearchType.IsNotNull: return SearchType.IsNull;
                    case SearchType.Between: return SearchType.NotBetween;
                    case SearchType.NotBetween: return SearchType.Between;
                    case SearchType.BetweenDates: return SearchType.NotBetweenDates;
                    case SearchType.NotBetweenDates: return SearchType.BetweenDates;
                    case SearchType.IsAnyOf: return SearchType.IsNoneOf;
                    case SearchType.IsNoneOf: return SearchType.IsAnyOf;
                    case SearchType.GreaterThan: return SearchType.LessThanOrEqualTo;
                    case SearchType.GreaterThanOrEqualTo: return SearchType.LessThan;
                    case SearchType.LessThan: return SearchType.GreaterThanOrEqualTo;
                    case SearchType.LessThanOrEqualTo: return SearchType.GreaterThan;
                    default: return t;
                }
            }

            private static FilterStringClause ToFilterStringClause(ClauseNode c)
            {
                return new FilterStringClause
                {
                    FieldName = c.FieldName,
                    SearchType = c.SearchType,
                    RawPrimary = c.RawPrimary,
                    RawSecondary = c.RawSecondary,
                    RawValues = c.RawValues,
                    DateInterval = c.DateInterval
                };
            }
        }

        private readonly struct ResolvedUnary
        {
            private ResolvedUnary(ClauseNode leaf, List<(ClauseNode, string)> group, bool isNull)
            {
                Leaf = leaf;
                SubGroup = group;
                IsNull = isNull;
            }

            public ClauseNode Leaf { get; }
            public List<(ClauseNode, string)> SubGroup { get; }
            public bool IsNull { get; }

            public bool IsLeaf => !IsNull && Leaf != null;

            public static ResolvedUnary AsLeaf(ClauseNode c) => new ResolvedUnary(c, null, false);
            public static ResolvedUnary AsGroup(List<(ClauseNode, string)> g) => new ResolvedUnary(null, g, false);
            public static ResolvedUnary Null() => new ResolvedUnary(null, null, true);
        }

        #endregion
    }
}
