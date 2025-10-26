// Description: C# Extension Methods | Enhance the .NET Framework and .NET Core with over 1000 extension methods.
// Website & Documentation: https://csharp-extension.com/
// Issues: https://github.com/zzzprojects/Z.ExtensionMethods/issues
// License (MIT): https://github.com/zzzprojects/Z.ExtensionMethods/blob/master/LICENSE
// More projects: https://zzzprojects.com/
// Copyright � ZZZ Projects Inc. All rights reserved.
namespace Jube.Dictionary.Extensions.System.Double.System.DateTime
{
    using DateTime=global::System.DateTime;
    using Double=double;

    public static class Extensions
    {
        /// <summary>
        ///     Returns a  equivalent to the specified OLE Automation Date.
        /// </summary>
        /// <param name="d">An OLE Automation Date value.</param>
        /// <returns>An object that represents the same date and time as .</returns>
        public static DateTime FromOADate(this Double d)
        {
            return DateTime.FromOADate(d);
        }
    }
}
