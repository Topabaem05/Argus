using UnityEngine;

namespace ArgusUnity.UI
{
    /// <summary>
    /// Global gate that blocks 3D camera and interaction input while menus/drawers are open.
    /// </summary>
    public static class InputGate
    {
        public static bool IsMenuOpen { get; set; }

        public static bool AllowGameplayInput => !IsMenuOpen;
    }
}
