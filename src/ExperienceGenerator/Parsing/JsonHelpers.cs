using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace ExperienceGenerator.Parsing
{
    public static class JsonHelpers
    {
        public static IEnumerable<JToken> AsArray(this JToken val)
        {
            if (val == null || val.Type == JTokenType.Null) yield break;

            var array = val as JArray;
            if (array == null)
            {
                yield return val;

            }
            else
            {
                foreach (var item in array)
                {
                    yield return item;
                }
            }
        }
    }
}
