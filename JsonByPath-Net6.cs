using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Appelgran.Helpers
{
    /// <summary>
    /// Parse JSON string for querying using normal javascript JSON syntax.
    /// (v1.0.3 (flavor: System.Text.Json), 2023-02-16, https://github.com/appelgran/JsonByPath, kopimi license)
    /// </summary>
    public class JsonByPath
    {
        private JsonElement _data;

        /// <summary>
        /// Parse JSON string for querying using normal javascript JSON syntax.
        /// </summary>
        /// <example>var data = new JsonByPath(json);</example>
        /// <example>var name = data.GetString("staff.groups[0].team_leader.name", "default name");</example>
        /// <example>var staffGroups = data.GetArray("staff.groups");</example>
        /// <param name="json">A valid JSON string.</param>
        /// <exception cref="ArgumentException"></exception>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="InvalidOperationException"></exception>
        public JsonByPath(string json)
        {
            _data = JsonDocument.Parse(json).RootElement;
        }

        private JsonByPath(JsonElement data)
        {
            _data = data;
        }

        /// <summary>
        /// Tries to parse JSON and give out a JsonByPath object. Returns a boolean indicating parsing success.
        /// </summary>
        public static bool TryParseJson(string json, out JsonByPath data)
        {
            try
            {
                data = new JsonByPath(json);
                return true;
            }
            catch
            {
                data = null;
                return false;
            }
        }

        /// <summary>
        /// Use a json "object" (object/Dictionary<string, object>) for further querying. Returns an empty JsonByPath if unsuccessful, safe for further querying (which all will fallback).
        /// </summary>
        /// <example>1: var items = data.GetArray("cart.items");</example>
        /// <example>2: var name = JsonByPath.Use(items[0]).GetString("name");</example>
        public static JsonByPath Use(JsonElement obj)
        {
            try
            {
                var data = obj;
                return new JsonByPath(data);
            }
            catch
            {
                return new JsonByPath(new JsonElement());
            }
        }

        /// <summary>
        /// Tries to return current as an array of values, default to null if unsuccesful. Useful for example when your json is an array at top-level.
        /// </summary>
        /// <returns>[] or null.</returns>
        /// <example>data.GetArray().Select(x => JsonByPath.Use(x).GetString("name", ""));</example>
        public JsonElement[] GetArray()
        {
            try
            {
                return _data.EnumerateArray().ToArray();
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Tries to retrieve an array of values, defaults to null if unsuccessful.
        /// </summary>
        /// <returns>ArrayList or null.</returns>
        /// <example>var staffGroups = data.GetArray("staff.groups");</example>
        public JsonElement[] GetArray(string path)
        {
            return Get<JsonElement[]>(_data, path, null, (JsonElement x) =>
            {
                try
                {
                    return x.EnumerateArray().ToArray();
                }
                catch
                {
                    return null;
                }
            });
        }

        /// <summary>
        /// Tries to retrieve an object, defaults to null if unsuccessful.
        /// </summary>
        /// <returns>Dictionary or default(JsonElement).</returns>
        /// <example>var stats = data.GetObject("ranches[0].horse_division.statistics");</example>
        public JsonElement GetObject(string path)
        {
            return Get<JsonElement>(_data, path, default(JsonElement), (JsonElement x) =>
            {
                return x;
            });
        }

        /// <summary>
        /// Tries to retrieve a string, defaults to fallback if unsuccessful.
        /// </summary>
        /// <returns>string or fallback.</returns>
        /// <example>var uri = data.GetString("ranches[0].horse_division.website_uri");</example>
        public string GetString(string path, string fallback)
        {
            return Get<string>(_data, path, fallback, (JsonElement x) =>
            {
                try
                {
                    return x.GetString();
                }
                catch
                {
                    return fallback;
                }
            });
        }

        /// <summary>
        /// Tries to retrieve a string, defaults to fallback if unsuccessful.
        /// </summary>
        /// <returns>string or fallback.</returns>
        /// <example>var kg = data.GetInt("ranches[0].statistics.total_weight");</example>
        public int GetInt(string path, int fallback)
        {
            return Get<int>(_data, path, fallback, (JsonElement x) =>
            {
                if (x.TryGetInt32(out int value))
                {
                    return value;
                }
                return fallback;
            });
        }

        /// <summary>
        /// Tries to retrieve a bool, defaults to fallback if unsuccessful.
        /// </summary>
        /// <returns>bool or fallback.</returns>
        /// <example>var kg = data.GetInt("ranches[0].isActive");</example>
        public bool GetBool(string path, bool fallback)
        {
            return Get<bool>(_data, path, fallback, (JsonElement x) =>
            {
                try
                {
                    return x.GetBoolean();
                }
                catch
                {
                    return fallback;
                }
            });
        }

        /// <summary>
        /// Tries to retrieve a T, defaults to fallback if unsuccessful.
        /// </summary>
        /// <returns>T or fallback.</returns>
        private T Get<T>(JsonElement data, string jsonPath, T fallback, Func<JsonElement, T> valueReader)
        {
            var paths = jsonPath.Split('.');

            if (!paths.All(x => Regex.IsMatch(x, @"^[^[]+(?:\[[0-9]+\])*$")))
            {
                throw new ArgumentException("Invalid jsonpath syntax!");
            }

            // data.TryGetProperty further on throws on null
            if (data.ValueKind == JsonValueKind.Null)
            {
                return fallback;
            }

            foreach (var path in paths)
            {
                var objName = path;
                List<int> indexes = null;

                // test for arrays: abc[1][0]
                var regexResult = Regex.Match(path, @"^([^[]+)(?:\[([0-9]+)\])+$");
                if (regexResult.Success)
                {
                    objName = regexResult.Groups[1].Value;
                    indexes = new List<int>();
                    foreach (Capture capture in regexResult.Groups[2].Captures)
                    {
                        indexes.Add(int.Parse(capture.Value));
                    }
                }

                if (data.TryGetProperty(objName, out JsonElement obj))
                {
                    if (indexes != null)
                    {
                        // cast to ArrayLists as long as we're going down along the [1][0]
                        foreach (var index in indexes)
                        {
                            try
                            {
                                obj = obj.EnumerateArray().ElementAt(index);
                            }
                            catch
                            {
                                return fallback;
                            }
                        }
                    }

                    if (path == paths.Last())
                    {
                        // if this is the end of the path, cast to target
                        try
                        {
                            return valueReader(obj);
                        }
                        catch
                        {
                            break;
                        }
                    }
                    else
                    {
                        // cast to dictionary for another round downwards
                        try
                        {
                            data = obj;
                        }
                        catch
                        {
                            break;
                        }
                    }
                }
                else
                {
                    break;
                }
            }
            return fallback;
        }
    }
}