using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;

using Newtonsoft.Json.Linq;

namespace Xenial.Delicious.Beer.Json
{
    /// <summary>
    /// Provides extension methods to poke json
    /// </summary>
    public static class PokeJson
    {
        /// <summary>
        /// Replaces a value in a json object. Can be a simple value or complex object (or anonymous type)
        /// This will not merge any values.
        /// </summary>
        /// <example>
        /// Set the connection string for an application
        /// <code>
        /// var json = System.IO.File.ReadAllText("path/to/appsettings.json");
        /// json = PokeJson.AddOrUpdateJsonValue(json, "ConnectionStrings:DefaultConnection", "MyConnectionString");
        /// System.IO.File.WriteAllText("path/to/appsettings.json", json);
        /// </code>
        /// </example>
        /// <example>
        /// Key's don't need to exist yet so the following calls will be equivalent:
        /// <code>
        /// var json1 = PokeJson.AddOrUpdateJsonValue(string.Empty, "MySection:Nested:Val", 1);
        /// var json2 = PokeJson.AddOrUpdateJsonValue(string.Empty, "MySection", new
        /// {
        ///     Nested = new
        ///     {
        ///         Val = 1
        ///     }
        /// });
        /// 
        /// json1.ShouldBe(json2);
        /// </code>
        /// </example>
        /// <typeparam name="T">type of the value. Should be inferred by the compiler</typeparam>
        /// <param name="json">A json object string. Will replace empty string with empty json object</param>
        /// <param name="keyPath">path splited by delimiter</param>
        /// <param name="value">the value to use. Can be null to erase an object</param>
        /// <param name="delimiter">the delimiter to split the keyPath</param>
        /// <returns>formated json with value replaced</returns>
        public static string AddOrUpdateJsonValue<T>(this string json, string keyPath, T value, char delimiter = ':')
        {
            if (string.IsNullOrEmpty(keyPath))
            {
                throw new ArgumentNullException(nameof(keyPath));
            }
            if (string.IsNullOrEmpty(json))
            {
                json = "{}";
            }

            var jsonObject = JObject.Parse(json);

            _ = jsonObject ?? throw new ArgumentNullException();

            if (keyPath.Contains(delimiter))
            {
                var obj = jsonObject;
                var keys = keyPath.Split(delimiter);

                foreach (var key in keys.SkipLast())
                {
                    var newObject = (JObject)obj[key]! ?? new JObject();
                    obj[key] = newObject;
                    obj = newObject;
                }

                obj = jsonObject;
                for (var i = 0; i < keys.Length - 1; i++)
                {
                    var key = keys[i];
                    obj = (JObject)obj[key]!;
                }
                obj[keys.Last()] = CreateValue();
            }
            else
            {
                jsonObject[keyPath] = CreateValue();
            }

            JToken CreateValue()
            {
                if (value != null)
                {
                    if (value.GetType().IsAnonymousType())
                    {
                        return JObject.FromObject(value);
                    }
                }
                return new JValue(value);
            }

            return jsonObject.ToString(Newtonsoft.Json.Formatting.Indented);
        }

        private static IEnumerable<T> SkipLast<T>(this IEnumerable<T> source)
        {
            using var iterator = source.GetEnumerator();

            if (!iterator.MoveNext())
            {
                yield break;
            }

            var previous = iterator.Current;

            while (iterator.MoveNext())
            {
                yield return previous;
                previous = iterator.Current;
            }
        }

        private static bool IsAnonymousType(this Type type)
        {
            var hasCompilerGeneratedAttribute = type.GetCustomAttributes(typeof(CompilerGeneratedAttribute), false).Count() > 0;
            var nameContainsAnonymousType = type.FullName?.Contains("AnonymousType") ?? false;
            var isAnonymousType = hasCompilerGeneratedAttribute && nameContainsAnonymousType;

            return isAnonymousType;
        }
    }
}
