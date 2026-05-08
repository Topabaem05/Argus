namespace ArgusUnity.Robots
{
    /// <summary>Deterministic group tint shared by spawn payloads and incremental group updates.</summary>
    public static class GroupColorPalette
    {
        public static string HexForGroupId(string groupId)
        {
            if (string.IsNullOrEmpty(groupId))
            {
                return "#B8C2CC";
            }

            uint hash = 2166136261;
            foreach (var character in groupId)
            {
                hash ^= character;
                hash *= 16777619;
            }

            var red = 80 + (hash & 0x7F);
            var green = 80 + ((hash >> 8) & 0x7F);
            var blue = 80 + ((hash >> 16) & 0x7F);
            return $"#{red:X2}{green:X2}{blue:X2}";
        }
    }
}
