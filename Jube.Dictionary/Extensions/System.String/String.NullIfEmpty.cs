// Description: C# Extension Methods | Enhance the .NET Framework and .NET Core with over 1000 extension methods.
// Website & Documentation: https://csharp-extension.com/
// Issues: https://github.com/zzzprojects/Z.ExtensionMethods/issues
// License (MIT): https://github.com/zzzprojects/Z.ExtensionMethods/blob/master/LICENSE
// More projects: https://zzzprojects.com/
// © ZZZ Projects Inc. All rights reserved.

namespace Jube.Dictionary.Extensions.System.String
{
    public static partial class Extensions
    {
        /// <summary>
        /// Returns <c>null</c> if the string is <c>null</c> or empty; otherwise, returns the original string.
        /// </summary>
        /// <param name="value">The string to check.</param>
        /// <returns><c>null</c> if the string is null or empty, otherwise the original value.</returns>
        public static string? NullIfEmpty(this string? value)
        {
            return string.IsNullOrEmpty(value) ? null : value;
        }
    }
}
