namespace ArgusUnity.Runtime
{
    /// <summary>
    /// Derives REST base URL from Unity WebSocket settings (scheme + authority only).
    /// </summary>
    public static class BridgeEndpoints
    {
        public static string HttpAuthorityFromWs(string wsUrl)
        {
            if (string.IsNullOrWhiteSpace(wsUrl))
            {
                return "http://127.0.0.1:8765";
            }

            try
            {
                var uri = new System.Uri(wsUrl);
                var scheme = uri.Scheme;
                if (scheme.Equals("ws", System.StringComparison.OrdinalIgnoreCase))
                {
                    scheme = "http";
                }
                else if (scheme.Equals("wss", System.StringComparison.OrdinalIgnoreCase))
                {
                    scheme = "https";
                }

                return $"{scheme}://{uri.Authority}";
            }
            catch (System.UriFormatException)
            {
                return "http://127.0.0.1:8765";
            }
        }
    }
}
