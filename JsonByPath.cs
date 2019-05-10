using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web.Script.Serialization;

namespace Appelgran.Helpers
{
    public class JsonByPath
    {
        private Dictionary<string, object> _data;

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
            var jss = new JavaScriptSerializer();
            _data = jss.Deserialize<Dictionary<string, object>>(json);
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
        /// Tries to retrieve an array of values, defaults to null if unsuccessful.
        /// </summary>
        /// <returns>ArrayList or null.</returns>
        /// <example>var staffGroups = data.GetArray("staff.groups");</example>
        public ArrayList GetArray(string path)
        {
            return Get<ArrayList>(_data, path, null);
        }

        /// <summary>
        /// Tries to retrieve an object, defaults to null if unsuccessful.
        /// </summary>
        /// <returns>Dictionary or null.</returns>
        /// <example>var stats = data.GetObject("ranches[0].horse_division.statistics");</example>
        public Dictionary<string, object> GetObject(string path)
        {
            return Get<Dictionary<string, object>>(_data, path, null);
        }

        /// <summary>
        /// Tries to retrieve a string, defaults to fallback if unsuccessful.
        /// </summary>
        /// <returns>string or fallback.</returns>
        /// <example>var uri = data.GetString("ranches[0].horse_division.website_uri");</example>
        public string GetString(string path, string fallback)
        {
            return Get<string>(_data, path, fallback);
        }

        /// <summary>
        /// Tries to retrieve a string, defaults to fallback if unsuccessful.
        /// </summary>
        /// <returns>string or fallback.</returns>
        /// <example>var kg = data.GetInt("ranches[0].statistics.total_weight");</example>
        public string GetInt(string path, string fallback)
        {
            return Get<string>(_data, path, fallback);
        }

        /// <summary>
        /// Tries to retrieve a T, defaults to fallback if unsuccessful.
        /// </summary>
        /// <returns>T or fallback.</returns>
        private T Get<T>(Dictionary<string, object> data, string jsonPath, T fallback)
        {
            var paths = jsonPath.Split('.');

            if (!paths.All(x => Regex.IsMatch(x, @"^[^[]+(?:\[[0-9]+\])*$")))
            {
                throw new ArgumentException("Invalid jsonpath syntax!");
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

                if (data.TryGetValue(objName, out object obj))
                {
                    if (indexes != null)
                    {
                        // cast to ArrayLists as long as we're going down along the [1][0]
                        foreach (var index in indexes)
                        {
                            try
                            {
                                var array = (ArrayList)obj;
                                obj = array[index];
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
                            T value = (T)obj;
                            return value;
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
                            data = (Dictionary<string, object>)obj;
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