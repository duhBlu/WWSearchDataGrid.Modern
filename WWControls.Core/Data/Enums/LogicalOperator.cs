namespace WWControls.Core
{
    /// <summary>
    /// Logical operators available to a <see cref="SearchTemplateGroup"/> in the Filter Editor.
    /// Negated variants (<see cref="NotAnd"/>, <see cref="NotOr"/>) wrap the combined group body
    /// in <c>Expression.Not</c> when the predicate is built.
    /// </summary>
    public enum LogicalOperator
    {
        And,
        Or,
        NotAnd,
        NotOr
    }
}
