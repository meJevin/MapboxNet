using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace MapboxNetCore
{
    public static class Core
    {
        static string GetEmbeddedResource(string name)
        {
            return GetEmbeddedResource(typeof(Core), nameof(MapboxNetCore) + "." + name);
        }

        public static string GetEmbeddedResource(Type assemblyType, string resourceName)
        {
            var assembly = assemblyType.GetTypeInfo().Assembly;

            using (Stream stream = assembly.GetManifestResourceStream(resourceName))
            using (StreamReader reader = new StreamReader(stream))
            {
                return reader.ReadToEnd();
            }
        }

        public static string GetFrameHTML(string accessToken, string style)
        {
            // HTML
            var indexHTML = GetEmbeddedResource("MapboxNetCore.html.index.html");

            // JS
            var indexJS = GetEmbeddedResource("MapboxNetCore.js.index.js");
            var mapBoxJS = GetEmbeddedResource("MapboxNetCore.lib.mapbox-gl.js");
            
            // CSS
            var indexCSS = GetEmbeddedResource("MapboxNetCore.css.index.css");
            var mapBoxCSS = GetEmbeddedResource("MapboxNetCore.css.mapbox-gl.css");

            // Insert access token and style into index.js
            indexJS = indexJS.Replace("MAPBOX_ACCESS_TOKEN", accessToken);
            indexJS = indexJS.Replace("MAPBOX_STYLE", style);

            // Replace references to files in index.html with actual data from embedded resources

            // References to JS
            indexHTML = indexHTML.Replace(
                "<script src=\"../lib/mapbox-gl.js\"></script>",
                $"<script>\n{mapBoxJS}\n</script>");
            indexHTML = indexHTML.Replace(
                "<script src=\"../js/index.js\"></script>",
                $"<script>\n{indexJS}\n</script>");

            // References to CSS
            indexHTML = indexHTML.Replace(
                "<link rel=\"stylesheet\" href=\"../css/index.css\">", 
                $"<style>\n{indexCSS}\n</style>");
            indexHTML = indexHTML.Replace(
                "<link rel=\"stylesheet\" href=\"../css/mapbox-gl.css\">",
                $"<style>\n{mapBoxCSS}\n</style>");

            // Return final HTML
            return indexHTML;
        }

        public static string ToLowerCamelCase(string s)
        {
            return Char.ToLowerInvariant(s[0]) + s.Substring(1);
        }

        public static string ToUpperCamelCase(string s)
        {
            return Char.ToUpperInvariant(s[0]) + s.Substring(1);
        }

        static object PlainifyJson(JToken token)
        {
            if (token.Type == JTokenType.Object)
            {
                IDictionary<string, JToken> dict = token as JObject;
                
                dynamic expandos = dict.Aggregate(new EnhancedExpandoObeject() as IDictionary<string, Object>,
                            (a, p) => { a.Add(p.Key, PlainifyJson(p.Value)); return a; });

                return expandos;
            }
            else if (token.Type == JTokenType.Array)
            {
                var array = token as JArray;
                return array.Select(item => PlainifyJson(item)).ToList();
            }
            else
            {
                return token.ToObject<object>();
            }
        }

        public static object DecodeJsonPlain(string json)
        {
            JsonSerializerSettings config = new JsonSerializerSettings {
                FloatParseHandling = FloatParseHandling.Double
            };

            if (json.Trim() == "")
                json = "null";

            dynamic data = JsonConvert.DeserializeObject("{a:" + json + "}");
            var deserialized = data.a as JToken;
            return PlainifyJson(deserialized);
        }
    }
}
