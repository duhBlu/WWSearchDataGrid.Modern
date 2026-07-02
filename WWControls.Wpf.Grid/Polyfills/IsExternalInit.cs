#if NETFRAMEWORK
namespace System.Runtime.CompilerServices
{
    /// <summary>Enables C# init-only setters when targeting .NET Framework.</summary>
    internal static class IsExternalInit
    {
    }
}
#endif
