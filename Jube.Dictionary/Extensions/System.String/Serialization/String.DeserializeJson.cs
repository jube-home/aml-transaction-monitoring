// Description: C# Extension Methods | Enhance .NET Framework and .NET Core with over 1000 extension methods.
// Website & Documentation: https://csharp-extension.com/
// Issues: https://github.com/zzzprojects/Z.ExtensionMethods/issues
// License (MIT): https://github.com/zzzprojects/Z.ExtensionMethods/blob/master/LICENSE
// More projects: https://zzzprojects.com/
// © ZZZ Projects Inc. All rights reserved.

namespace Jube.Dictionary.Extensions.System.String.Serialization
{
    using global::System.IO;
    using global::System.Runtime.Serialization.Json;
    using global::System.Text;

    public static class Extensions
    {
        /// <summary>
        /// Deserializes a JSON string into an object of type <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">The type to deserialize into.</typeparam>
        /// <param name="json">The JSON string to deserialize.</param>
        /// <returns>The deserialized object, or <c>default</c> if input is null or invalid.</returns>
        public static T? DeserializeJson<T>(this string? json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return default;
            }

            try
            {
                var serializer = new DataContractJsonSerializer(typeof(T));

                using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
                var result = serializer.ReadObject(stream);
                return result is T typed ? typed : default;
            }
            catch
            {
                // Optionally log or rethrow depending on use case
                return default;
            }
        }

        /// <summary>
        /// Deserializes a JSON string into an object of type <typeparamref name="T"/> using a specific encoding.
        /// </summary>
        /// <typeparam name="T">The type to deserialize into.</typeparam>
        /// <param name="json">The JSON string to deserialize.</param>
        /// <param name="encoding">The text encoding to use.</param>
        /// <returns>The deserialized object, or <c>default</c> if input is null or invalid.</returns>
        public static T? DeserializeJson<T>(this string? json, Encoding? encoding)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return default;
            }

            encoding ??= Encoding.UTF8;

            try
            {
                var serializer = new DataContractJsonSerializer(typeof(T));

                using var stream = new MemoryStream(encoding.GetBytes(json));
                var result = serializer.ReadObject(stream);
                return result is T typed ? typed : default;
            }
            catch
            {
                // Optionally log or rethrow depending on use case
                return default;
            }
        }
    }
}