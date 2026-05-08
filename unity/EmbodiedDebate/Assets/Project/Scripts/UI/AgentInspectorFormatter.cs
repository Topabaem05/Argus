using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json.Linq;

namespace ArgusUnity.UI
{
    /// <summary>
    /// Mirrors the Python allowlist (<c>_PUBLIC_AGENT_FIELDS</c>) for public agent payloads.
    /// </summary>
    public static class AgentInspectorFormatter
    {
        private static readonly HashSet<string> PublicKeys =
            new HashSet<string>(new[]
            {
                "agent_id",
                "display_name",
                "language",
                "background",
                "goals",
                "safety_notes",
            });

        /// <returns>Human-readable excerpt or empty when parsing fails.</returns>
        public static bool TryFilterPublicSubset(string rawJson, out string filteredText)
        {
            filteredText = string.Empty;
            if (string.IsNullOrWhiteSpace(rawJson))
            {
                return false;
            }

            try
            {
                var jo = JObject.Parse(rawJson);
                var sb = new StringBuilder();
                foreach (var key in jo.Properties())
                {
                    if (!PublicKeys.Contains(key.Name))
                    {
                        continue;
                    }

                    FormatProperty(sb, key.Name, key.Value);
                }

                filteredText = sb.Length == 0 ? "(no matching public keys)" : sb.ToString();
                return true;
            }
            catch (Newtonsoft.Json.JsonReaderException)
            {
                filteredText = string.Empty;
                return false;
            }
        }

        private static void FormatProperty(StringBuilder sb, string name, JToken value)
        {
            sb.AppendLine($"{name}:");
            if (value.Type == JTokenType.Array || value.Type == JTokenType.Object)
            {
                sb.AppendLine(value.ToString(Newtonsoft.Json.Formatting.None));
            }
            else
            {
                sb.AppendLine(value.ToString());
            }

            sb.AppendLine();
        }
    }
}
