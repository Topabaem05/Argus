namespace ArgusUnity.Robots
{
    public struct RobotVector3
    {
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }
    }

    public sealed class RobotAvatar
    {
        public string AgentId { get; private set; }
        public string DisplayName { get; private set; }
        public string GroupId { get; private set; }
        public string GroupBadge { get; private set; }
        public string GroupColorHex { get; private set; }
        public string PrefabKey { get; private set; }
        public bool UsesFallbackPrefab { get; private set; }
        public RobotVector3 Position { get; private set; }
        public bool Visible { get; private set; }

        public RobotAvatar(
            string agentId,
            string displayName,
            string groupId,
            string groupBadge,
            string groupColorHex,
            string prefabKey,
            bool usesFallbackPrefab,
            RobotVector3 position,
            bool visible)
        {
            AgentId = agentId;
            Update(
                displayName,
                groupId,
                groupBadge,
                groupColorHex,
                prefabKey,
                usesFallbackPrefab,
                position,
                visible);
        }

        public void Update(
            string displayName,
            string groupId,
            string groupBadge,
            string groupColorHex,
            string prefabKey,
            bool usesFallbackPrefab,
            RobotVector3 position,
            bool visible)
        {
            DisplayName = displayName;
            GroupId = groupId;
            GroupBadge = groupBadge;
            GroupColorHex = groupColorHex;
            PrefabKey = prefabKey;
            UsesFallbackPrefab = usesFallbackPrefab;
            Position = position;
            Visible = visible;
        }
    }
}
