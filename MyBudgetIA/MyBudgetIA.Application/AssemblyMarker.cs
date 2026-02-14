using System.Diagnostics.CodeAnalysis;

namespace MyBudgetIA.Application
{
    /// <summary>
    /// Marker type used for assembly scanning (FluentValidation)
    /// Never instantiate this class directly
    /// </summary>
    [ExcludeFromCodeCoverage]
    public sealed class AssemblyMarker
    {
        private AssemblyMarker() { }
    }
}
