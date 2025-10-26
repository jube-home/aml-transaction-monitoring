// Description: C# Extension Methods | Enhance the .NET Framework and .NET Core with over 1000 extension methods.
// Website & Documentation: https://csharp-extension.com/
// Issues: https://github.com/zzzprojects/Z.ExtensionMethods/issues
// License (MIT): https://github.com/zzzprojects/Z.ExtensionMethods/blob/master/LICENSE
// More projects: https://zzzprojects.com/
// Copyright � ZZZ Projects Inc. All rights reserved.
namespace Jube.Dictionary.Extensions.System.String._ExtractValueType
{
    using global::System.Text.RegularExpressions;

    public static partial class Extensions
    {
        /// <summary>
        ///     A string extension method that extracts all Int16 from the string.
        /// </summary>
        /// <param name="this">The @this to act on.</param>
        /// <returns>All extracted Int16.</returns>
        public static short[] ExtractManyInt16(this string @this)
        {
            return Regex.Matches(@this, @"[-]?\d+")
                .Select(x => Convert.ToInt16(x.Value))
                .ToArray();
        }
    }
}
