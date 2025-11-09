using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace Foca.SerpApiSearch.Search
{
    /// <summary>
    /// Extracts links from SerpApi organic_results[].link (DDG/Google)
    /// </summary>
    public static class ResultMapper
    {
        // Aceptar string para no exponer JObject en API pública
        public static IEnumerable<string> ExtractLinks(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) yield break;
            JArray organic = null;
            try
            {
                var jobj = JObject.Parse(json);
                organic = jobj["organic_results"] as JArray;
            }
            catch
            {
                yield break;
            }
            if (organic == null) yield break;
            foreach (var item in organic.OfType<JObject>())
            {
                var link = item["link"]?.ToString() ?? item["url"]?.ToString();
                if (!string.IsNullOrWhiteSpace(link)) yield return link.Trim();
            }
        }

        public static (IEnumerable<string> links, bool hasNext) ExtractLinksAndHasNext(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) return (Enumerable.Empty<string>(), false);
            try
            {
                var jobj = JObject.Parse(json);
                var organic = jobj["organic_results"] as JArray;
                var links = organic?.OfType<JObject>()
                    .Select(o => ((o["link"]?.ToString() ?? o["url"]?.ToString()) ?? string.Empty).Trim())
                    .Where(s => !string.IsNullOrWhiteSpace(s))
                    ?? Enumerable.Empty<string>();
                var next = jobj.SelectToken("serpapi_pagination.next")
                    ?? jobj.SelectToken("serpapi_pagination.next_link")
                    ?? jobj.SelectToken("pagination.next");
                return (links, next != null);
            }
            catch
            {
                return (Enumerable.Empty<string>(), false);
            }
        }
    }
}


