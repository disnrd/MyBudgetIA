using System;
using System.Collections.Generic;
using System.Text;

namespace Shared.Helpers
{
    /// <summary>
    /// Marks the annotated object as being exposed only to unit tests (by being <see langword="internal"/> instead of
    /// <see langword="private"/>). Such an object is not part of the public surface.
    /// <para/>Granting unit tests access to internal types of the tested code assembly is done by using the
    /// <see cref="System.Runtime.CompilerServices.InternalsVisibleToAttribute"/> in the tested code assembly, with
    /// the name of the unit test project assembly.
    /// </summary>
    [AttributeUsage(AttributeTargets.All, Inherited = false, AllowMultiple = false)]
    public sealed class ExposedOnlyToUnitTestsAttribute : Attribute
    {
    }
}
