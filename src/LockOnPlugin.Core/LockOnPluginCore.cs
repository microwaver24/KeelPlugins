using BepInEx;
using BepInEx.Configuration;
using KeelPlugins.Utils;
using UnityEngine;

namespace LockOnPlugin.Core
{
    public abstract class LockOnPluginCore : BaseUnityPlugin
    {
        public const string GUID = "keelhauled.lockonplugin";
        public const string PluginName = "LockOnPlugin";

        private const string SECTION_GENERAL = "General";
        private const string SECTION_HOTKEYS = "Keyboard Shortcuts";

        private const string DESCRIPTION_TRACKSPEED = "The speed at which the target is followed.";
        private const string DESCRIPTION_SCROLLMALES = "Choose whether to include males in the rotation when switching between characters using the hotkeys from the plugin.";
        private const string DESCRIPTION_LEASHLENGTH = "The amount of slack allowed when tracking.";
        private const string DESCRIPTION_AUTOLOCK = "Lock on automatically after switching characters.";
        private const string DESCRIPTION_SHOWINFOMSG = "Show various messages about the plugin on screen.";

        private const string DESCRIPTION_LOCKON = "Lock on to the next bone in order. Press and hold this to release the lock.";
        private const string DESCRIPTION_NEXTCHARA = "Lock on to the next character in the scene.";
        private const string DESCRIPTION_PREVCHARA = "Lock on to the previous character in the scene.";
        private const string DESCRIPTION_CAMERAFOV = "Only while locked on. Hold this button, then click and drag the right mouse button to adjust the field of view (FOV) of the camera.";
        private const string DESCRIPTION_CAMERATILT = "Only while locked on. Hold this button, then click and drag the right mouse button to adjust the tilt of the camera.";
        private static readonly string DESCRIPTION_LOCKMOVE = "Only while locked on. Add offset to the locked on bone by moving the target {0}. Offset is reset when you change the lock target.";
        private static readonly string DESCRIPTION_LOCKMOVEUP = string.Format(DESCRIPTION_LOCKMOVE, "up");
        private static readonly string DESCRIPTION_LOCKMOVEDOWN = string.Format(DESCRIPTION_LOCKMOVE, "down");
        private static readonly string DESCRIPTION_LOCKMOVELEFT = string.Format(DESCRIPTION_LOCKMOVE, "to the left");
        private static readonly string DESCRIPTION_LOCKMOVERIGHT = string.Format(DESCRIPTION_LOCKMOVE, "to the right");
        private static readonly string DESCRIPTION_LOCKMOVEFORWARD = string.Format(DESCRIPTION_LOCKMOVE, "forward");
        private static readonly string DESCRIPTION_LOCKMOVEBACKWARD = string.Format(DESCRIPTION_LOCKMOVE, "backward");
        private static readonly string KEY_LOCKMOVE = "Move lock offset {0}";
        private static readonly string KEY_LOCKMOVEUP = string.Format(KEY_LOCKMOVE, "up");
        private static readonly string KEY_LOCKMOVEDOWN = string.Format(KEY_LOCKMOVE, "down");
        private static readonly string KEY_LOCKMOVELEFT = string.Format(KEY_LOCKMOVE, "left");
        private static readonly string KEY_LOCKMOVERIGHT = string.Format(KEY_LOCKMOVE, "right");
        private static readonly string KEY_LOCKMOVEFORWARD = string.Format(KEY_LOCKMOVE, "forward");
        private static readonly string KEY_LOCKMOVEBACKWARD = string.Format(KEY_LOCKMOVE, "backward");

        internal static ConfigEntry<float> TrackingSpeedNormal { get; set; }
        internal static ConfigEntry<bool> ScrollThroughMalesToo { get; set; }
        internal static ConfigEntry<bool> ShowInfoMsg { get; set; }
        internal static ConfigEntry<float> LockLeashLength { get; set; }
        internal static ConfigEntry<bool> AutoSwitchLock { get; set; }
        internal static ConfigEntry<KeyboardShortcut> LockOnKey { get; set; }
        internal static ConfigEntry<KeyboardShortcut> PrevCharaKey { get; set; }
        internal static ConfigEntry<KeyboardShortcut> NextCharaKey { get; set; }
        internal static ConfigEntry<KeyboardShortcut> CameraFovKey { get; set; }
        internal static ConfigEntry<KeyboardShortcut> CameraTiltKey { get; set; }
        internal static ConfigEntry<KeyboardShortcut> LockMoveUpKey { get; set; }
        internal static ConfigEntry<KeyboardShortcut> LockMoveDownKey { get; set; }
        internal static ConfigEntry<KeyboardShortcut> LockMoveLeftKey { get; set; }
        internal static ConfigEntry<KeyboardShortcut> LockMoveRightKey { get; set; }
        internal static ConfigEntry<KeyboardShortcut> LockMoveForwardKey { get; set; }
        internal static ConfigEntry<KeyboardShortcut> LockMoveBackwardKey { get; set; }

        internal static GameObject bepinex;

        protected virtual void Awake()
        {
            bepinex = gameObject;
            Log.SetLogSource(Logger);

            TargetData.LoadData();

            TrackingSpeedNormal = Config.Bind(SECTION_GENERAL, "Tracking speed", 0.1f, new ConfigDescription(DESCRIPTION_TRACKSPEED, new AcceptableValueRange<float>(0.01f, 0.3f), new ConfigurationManagerAttributes { Order = 10 }));
            ScrollThroughMalesToo = Config.Bind(SECTION_GENERAL, "Scroll through males too", true, new ConfigDescription(DESCRIPTION_SCROLLMALES, null, new ConfigurationManagerAttributes { Order = 8 }));
            ShowInfoMsg = Config.Bind(SECTION_GENERAL, "Show info messages", false, new ConfigDescription(DESCRIPTION_SHOWINFOMSG, null, new ConfigurationManagerAttributes { Order = 6 }));
            LockLeashLength = Config.Bind(SECTION_GENERAL, "Leash length", 0f, new ConfigDescription(DESCRIPTION_LEASHLENGTH, new AcceptableValueRange<float>(0f, 0.5f), new ConfigurationManagerAttributes { Order = 9 }));
            AutoSwitchLock = Config.Bind(SECTION_GENERAL, "Auto switch lock", false, new ConfigDescription(DESCRIPTION_AUTOLOCK, null, new ConfigurationManagerAttributes { Order = 7 }));

            int hotKeyOrder = 100;
            LockOnKey = Config.Bind(SECTION_HOTKEYS, "Lock on (Hold to unlock)", new KeyboardShortcut(KeyCode.Mouse4), new ConfigDescription(DESCRIPTION_LOCKON, null, new ConfigurationManagerAttributes { Order = hotKeyOrder-- }));
            PrevCharaKey = Config.Bind(SECTION_HOTKEYS, "Select previous character", new KeyboardShortcut(KeyCode.None), new ConfigDescription(DESCRIPTION_PREVCHARA, null, new ConfigurationManagerAttributes { Order = hotKeyOrder-- }));
            NextCharaKey = Config.Bind(SECTION_HOTKEYS, "Select next character", new KeyboardShortcut(KeyCode.None), new ConfigDescription(DESCRIPTION_NEXTCHARA, null, new ConfigurationManagerAttributes { Order = hotKeyOrder-- }));
            CameraFovKey = Config.Bind(SECTION_HOTKEYS, "Change camera FOV (Hold + RMB drag)", new KeyboardShortcut(KeyCode.LeftShift), new ConfigDescription(DESCRIPTION_CAMERAFOV, null, new ConfigurationManagerAttributes { Order = hotKeyOrder-- }));
            CameraTiltKey = Config.Bind(SECTION_HOTKEYS, "Change camera tilt (Hold + RMB drag)", new KeyboardShortcut(KeyCode.LeftControl), new ConfigDescription(DESCRIPTION_CAMERATILT, null, new ConfigurationManagerAttributes { Order = hotKeyOrder-- }));
            LockMoveUpKey = Config.Bind(SECTION_HOTKEYS, KEY_LOCKMOVEUP, new KeyboardShortcut(KeyCode.PageUp), new ConfigDescription(DESCRIPTION_LOCKMOVEUP, null, new ConfigurationManagerAttributes { Order = hotKeyOrder-- }));
            LockMoveDownKey = Config.Bind(SECTION_HOTKEYS, KEY_LOCKMOVEDOWN, new KeyboardShortcut(KeyCode.PageDown), new ConfigDescription(DESCRIPTION_LOCKMOVEDOWN, null, new ConfigurationManagerAttributes { Order = hotKeyOrder-- }));
            LockMoveLeftKey = Config.Bind(SECTION_HOTKEYS, KEY_LOCKMOVELEFT, new KeyboardShortcut(KeyCode.LeftArrow), new ConfigDescription(DESCRIPTION_LOCKMOVELEFT, null, new ConfigurationManagerAttributes { Order = hotKeyOrder-- }));
            LockMoveRightKey = Config.Bind(SECTION_HOTKEYS, KEY_LOCKMOVERIGHT, new KeyboardShortcut(KeyCode.RightArrow), new ConfigDescription(DESCRIPTION_LOCKMOVERIGHT, null, new ConfigurationManagerAttributes { Order = hotKeyOrder-- }));
            LockMoveForwardKey = Config.Bind(SECTION_HOTKEYS, KEY_LOCKMOVEFORWARD, new KeyboardShortcut(KeyCode.UpArrow), new ConfigDescription(DESCRIPTION_LOCKMOVEFORWARD, null, new ConfigurationManagerAttributes { Order = hotKeyOrder-- }));
            LockMoveBackwardKey = Config.Bind(SECTION_HOTKEYS, KEY_LOCKMOVEBACKWARD, new KeyboardShortcut(KeyCode.DownArrow), new ConfigDescription(DESCRIPTION_LOCKMOVEBACKWARD, null, new ConfigurationManagerAttributes { Order = hotKeyOrder-- }));
        }
    }
}
