// Description: C# Extension Methods | Enhance the .NET Framework and .NET Core with over 1000 extension methods.
// Website & Documentation: https://csharp-extension.com/
// Issues: https://github.com/zzzprojects/Z.ExtensionMethods/issues
// License (MIT): https://github.com/zzzprojects/Z.ExtensionMethods/blob/master/LICENSE
// More projects: https://zzzprojects.com/
// Copyright � ZZZ Projects Inc. All rights reserved.
namespace Jube.Dictionary.Extensions.System.Int32.System.Drawing.ColorTranslator
{
    using Color=global::System.Drawing.Color;
    using ColorTranslator=global::System.Drawing.ColorTranslator;
    using Int32=int;

    public static partial class Extensions
    {
#if !NETSTANDARD
        /// <summary>
        ///     Translates a Windows color value to a GDI+  structure.
        /// </summary>
        /// <param name="win32Color">The Windows color to translate.</param>
        /// <returns>The  structure that represents the translated Windows color.</returns>
        public static Color FromWin32(this Int32 win32Color)
        {
            return ColorTranslator.FromWin32(win32Color);
        }
#endif
    }
}
