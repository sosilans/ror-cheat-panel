using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using Rewired;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;
using IoPath = System.IO.Path;

namespace RoRCheatPanel.Channel
{
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    [BepInDependency("com.rorcheatpanel.plugin")]
    public sealed class ChannelPlugin : BaseUnityPlugin
    {
        public const string PluginGuid = "com.rorcheatpanel.channel";
        public const string PluginName = "RoR Cheat Panel Channel";
        public const string PluginVersion = "1.0.56";
        public const string YoutubeUrl = "https://www.youtube.com/@Silans";
        public const string SiteUrl = "https://ror-cheat-panel.netlify.app";
        public const string ShownVersion = "1.1";

        private const int FlattenFrames = 24;

        private static readonly Type CheatRuntimeType = Type.GetType("RoRCheatPanel.CheatRuntime, RoRCheatPanel");
        private static readonly FieldInfo DrunkField = CheatRuntimeType != null
            ? CheatRuntimeType.GetField("DrunkCamera", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)
            : null;
        private static readonly FieldInfo ThirdPersonField = CheatRuntimeType != null
            ? CheatRuntimeType.GetField("ThirdPerson", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)
            : null;
        private static readonly FieldInfo FovField = CheatRuntimeType != null
            ? CheatRuntimeType.GetField("Fov", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)
            : null;
        private static readonly FieldInfo GodModeField = CheatRuntimeType != null
            ? CheatRuntimeType.GetField("GodMode", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)
            : null;
        private static readonly FieldInfo FreezeField = CheatRuntimeType != null
            ? CheatRuntimeType.GetField("FreezeStopwatch", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)
            : null;
        private static readonly FieldInfo TimeScaleField = CheatRuntimeType != null
            ? CheatRuntimeType.GetField("TimeScale", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)
            : null;
        private static readonly FieldInfo BodyScaleField = CheatRuntimeType != null
            ? CheatRuntimeType.GetField("BodyScale", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)
            : null;
        private static readonly FieldInfo DistanceField = CheatRuntimeType != null
            ? CheatRuntimeType.GetField("ThirdPersonDistance", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)
            : null;
        private static readonly FieldInfo DesiredStateField = AccessTools.Field(typeof(CameraRigController), "desiredCameraState");
        private static readonly FieldInfo LerpStateField = AccessTools.Field(typeof(CameraRigController), "lerpCameraState");
        private static readonly FieldInfo LerpTimeField = AccessTools.Field(typeof(CameraRigController), "lerpCameraTime");

        private static Texture2D _tex;
        private static Texture2D _siteTex;
        private static Texture2D _pixel;
        private static Font _toastFont;
        private static Font _kissFont;
        private static bool _tried;
        private static bool _siteTried;
        private static int _flattenFrames;
        private static bool _wasDrunk;
        private static bool _applying;
        private static bool _lookHeld;
        private static Quaternion _heldRot = Quaternion.identity;
        private static bool _mouseOff;
        private static bool _mouseWasOn = true;
        private static FieldInfo _toastField;
        private static FieldInfo _toastUntilField;
        private static Type _themeType;
        private static readonly List<HealthComponent> GodApplied = new List<HealthComponent>();
        private static bool _godWasOn;
        private static float _lastDistance = 8f;
        private static bool _distanceInit;
        private static bool _tpWasOn;
        private static bool _haveFpRot;
        private static Quaternion _fpRot = Quaternion.identity;
        private static int _seatFrames;
        private static float _savedLook = -1f;
        internal static bool DistanceDriven;
        internal static bool FirstPerson;
        private static bool _stockDistanceCleared;
        private static ConfigEntry<float> PanelOpacity;
        private static ConfigFile _ownConfig;

        private void Awake()
        {
            _ownConfig = Config;
            var harmony = new Harmony(PluginGuid);
            Safe("opacity", BindPanelOpacity);
            Safe("spawn", () => MonsterSpawn.Install(harmony));
            Safe("about", () => PatchAboutTab(harmony));
            Safe("extras", () => FunExtras.Install(harmony));
            Safe("survivors", () => SurvivorGrid.Install(harmony));
            Safe("tips", () => ButtonTips.Install(harmony));
            Safe("lobby", () => LobbySize.Install(harmony));
            Safe("chrome", () => PanelChrome.Install(harmony));
            Safe("menu", () => MenuTidy.Install(harmony));
            Safe("fixes", () => PanelFixes.Install(harmony));
            Safe("noclip", () => RemoteNoclip.Install(harmony));
            Safe("cloak", () => EnemyCloak.Install(harmony, Config));
            Safe("lists", () => ListFavorites.Install(harmony));
            Safe("artifacts", () => IconWarmup.Install(harmony));
            Safe("host", () => HostVanilla.Install(harmony));
            Safe("drunk", () => PatchDrunkRemoval(harmony));
            Safe("fun", () => PatchFunTab(harmony));
            Safe("window", () => PatchWindowAlpha(harmony));
            Safe("toast", () => PatchToast(harmony));
            Safe("god", () => PatchGodMode(harmony));
            Safe("tick", () => Patch(harmony, "RoRCheatPanel.CheatRuntime", "Tick", nameof(AfterRuntimeTick)));
            Safe("camera pin", () => PatchPrefix(harmony, "RoRCheatPanel.CheatRuntime", "ClearCameraOverlay", nameof(SkipDrunkPositionPin)));
            Safe("camera restore", () => Patch(harmony, "RoRCheatPanel.CheatRuntime", "RestoreCamera", nameof(AfterRestoreCamera)));
            Safe("late tick", () => Patch(harmony, "RoRCheatPanel.CheatRuntime", "LateTick", nameof(AfterLateTick)));

            Safe("camera late", () =>
            {
                var lateUpdate = AccessTools.Method(typeof(CameraRigController), "LateUpdate");
                if (lateUpdate != null)
                    harmony.Patch(lateUpdate, postfix: new HarmonyMethod(typeof(ChannelPlugin), nameof(AfterRigLateUpdate)));
            });
            Safe("camera state", () =>
            {
                var setState = AccessTools.Method(typeof(CameraRigController), "SetCameraState");
                if (setState != null)
                    harmony.Patch(setState, postfix: new HarmonyMethod(typeof(ChannelPlugin), nameof(AfterSetCameraState)));
            });
            Safe("look", () => PatchLook(harmony));
            Safe("camera early", () =>
            {
                var early = AccessTools.Method(typeof(CameraRigController), "EarlyUpdate");
                if (early != null)
                {
                    harmony.Patch(
                        early,
                        prefix: new HarmonyMethod(typeof(ChannelPlugin), nameof(BeforeCameraDistance)),
                        postfix: new HarmonyMethod(typeof(ChannelPlugin), nameof(AfterRigLateUpdate)));
                }
            });

            Logger.LogInfo("RoR Cheat Panel Channel " + PluginVersion + " loaded.");
        }

        private static void PatchGodMode(Harmony harmony)
        {
            var apply = CheatRuntimeType != null
                ? CheatRuntimeType.GetMethod("ApplyGodMode", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)
                : null;
            if (apply == null)
            {
                Debug.LogWarning("[RoRCheatPanel.Channel] missing CheatRuntime.ApplyGodMode");
                return;
            }

            harmony.Patch(apply, prefix: new HarmonyMethod(typeof(ChannelPlugin), nameof(ApplyGodModePrefix)));
        }

        private static bool ApplyGodModePrefix()
        {
            var on = ReadBool(GodModeField);
            if (on)
            {
                _godWasOn = true;
                var body = LocalBody();
                var health = body != null ? body.healthComponent : null;
                if (health != null)
                {
                    health.godMode = true;
                    if (!GodApplied.Contains(health))
                        GodApplied.Add(health);
                }

                return false;
            }

            if (_godWasOn || GodApplied.Count > 0)
                ClearAppliedGodMode();
            return false;
        }

        private static void ClearAppliedGodMode()
        {
            for (var i = 0; i < GodApplied.Count; i++)
            {
                var health = GodApplied[i];
                if (health != null)
                    health.godMode = false;
            }

            GodApplied.Clear();
            _godWasOn = false;
        }

        private static CharacterBody LocalBody()
        {
            var local = LocalUserManager.GetFirstLocalUser();
            if (local == null)
                return null;
            if (local.cachedBody != null)
                return local.cachedBody;
            var net = local.currentNetworkUser;
            return net != null ? net.GetCurrentBody() : null;
        }

        private static void PatchToast(Harmony harmony)
        {
            var helpers = Type.GetType("RoRCheatPanel.UI.UiHelpers, RoRCheatPanel");
            var drawToast = helpers != null
                ? helpers.GetMethod("DrawToast", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)
                : null;
            if (drawToast == null)
            {
                Debug.LogWarning("[RoRCheatPanel.Channel] missing UiHelpers.DrawToast");
                return;
            }

            _toastField = helpers.GetField("_toast", BindingFlags.NonPublic | BindingFlags.Static);
            _toastUntilField = helpers.GetField("_toastUntil", BindingFlags.NonPublic | BindingFlags.Static);
            _themeType = Type.GetType("RoRCheatPanel.UiTheme, RoRCheatPanel");
            harmony.Patch(drawToast, prefix: new HarmonyMethod(typeof(ChannelPlugin), nameof(DrawThemedToastPrefix)));
        }

        private static bool DrawThemedToastPrefix()
        {
            DrawThemedToast();
            return false;
        }

        private static void DrawThemedToast()
        {
            var text = _toastField != null ? _toastField.GetValue(null) as string : null;
            var until = _toastUntilField != null && _toastUntilField.GetValue(null) is float value ? value : 0f;
            if (string.IsNullOrEmpty(text) || Time.realtimeSinceStartup > until)
                return;

            if (_toastFont == null)
                _toastFont = Font.CreateDynamicFontFromOSFont(new[] { "Segoe UI", "Arial", "Tahoma" }, 15);

            SplitToast(text, out var title, out var subtitle);

            var left = until - Time.realtimeSinceStartup;
            var alpha = 1f;
            if (left < 0.4f)
                alpha = Mathf.Clamp01(left / 0.4f);
            else if (left > 2.9f)
                alpha = Mathf.Clamp01((3.2f - left) / 0.25f);

            var accent = ThemeColor("Accent", new Color(0.38f, 0.58f, 0.88f));
            var window = ThemeColor("Window", new Color(0.12f, 0.14f, 0.2f, 0.97f));
            window.a = 0.92f * alpha;
            accent.a = alpha;

            var titleStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Bold,
                fontSize = 16,
                wordWrap = false,
                clipping = TextClipping.Overflow,
                font = _toastFont
            };
            titleStyle.normal.textColor = new Color(
                Mathf.Lerp(1f, accent.r, 0.35f),
                Mathf.Lerp(1f, accent.g, 0.35f),
                Mathf.Lerp(1f, accent.b, 0.35f),
                alpha);

            var subStyle = new GUIStyle(titleStyle)
            {
                fontStyle = FontStyle.Normal,
                fontSize = 13
            };
            subStyle.normal.textColor = new Color(0.92f, 0.93f, 0.96f, 0.88f * alpha);

            var titleSize = titleStyle.CalcSize(new GUIContent(title));
            var subSize = string.IsNullOrEmpty(subtitle) ? Vector2.zero : subStyle.CalcSize(new GUIContent(subtitle));
            var width = Mathf.Clamp(Mathf.Max(titleSize.x, subSize.x) + 44f, 260f, Screen.width - 48f);
            var height = 18f + titleSize.y + (string.IsNullOrEmpty(subtitle) ? 0f : subSize.y + 2f);
            var x = (Screen.width - width) * 0.5f;
            var y = 14f;

            var pixel = Pixel();
            var old = GUI.color;
            GUI.color = new Color(0f, 0f, 0f, 0.35f * alpha);
            GUI.DrawTexture(new Rect(x + 3f, y + 4f, width, height), pixel);
            GUI.color = new Color(accent.r, accent.g, accent.b, 0.95f * alpha);
            GUI.DrawTexture(new Rect(x - 2f, y - 2f, width + 4f, height + 4f), pixel);
            GUI.color = window;
            GUI.DrawTexture(new Rect(x, y, width, height), pixel);
            GUI.color = accent;
            GUI.DrawTexture(new Rect(x, y, 4f, height), pixel);
            GUI.DrawTexture(new Rect(x + width - 4f, y, 4f, height), pixel);
            GUI.DrawTexture(new Rect(x, y + height - 3f, width, 3f), pixel);
            GUI.color = old;

            var titleRect = new Rect(x + 14f, y + 8f, width - 28f, titleSize.y);
            GUI.Label(titleRect, title, titleStyle);
            if (!string.IsNullOrEmpty(subtitle))
                GUI.Label(new Rect(x + 14f, titleRect.yMax + 1f, width - 28f, subSize.y), subtitle, subStyle);
        }

        private static void SplitToast(string text, out string title, out string subtitle)
        {
            var split = text.IndexOf(" — ", StringComparison.Ordinal);
            if (split < 0)
                split = text.IndexOf(" - ", StringComparison.Ordinal);
            if (split > 0)
            {
                title = text.Substring(0, split).Trim();
                subtitle = text.Substring(split + 3).Trim();
                return;
            }

            title = text;
            subtitle = null;
        }

        private static Color ThemeColor(string name, Color fallback)
        {
            var prop = _themeType != null
                ? _themeType.GetProperty(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)
                : null;
            return prop != null && prop.GetValue(null) is Color color ? color : fallback;
        }

        private static Texture2D Pixel()
        {
            if (_pixel != null)
                return _pixel;
            _pixel = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            _pixel.wrapMode = TextureWrapMode.Clamp;
            _pixel.SetPixel(0, 0, Color.white);
            _pixel.Apply();
            return _pixel;
        }

        private static void PatchAboutTab(Harmony harmony)
        {
            var about = Type.GetType("RoRCheatPanel.UI.Tabs.AboutTab, RoRCheatPanel");
            if (about == null)
            {
                Debug.LogWarning("[RoRCheatPanel.Channel] missing AboutTab");
                return;
            }

            var draw = about.GetMethod("Draw", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            if (draw != null)
                harmony.Patch(draw, transpiler: new HarmonyMethod(typeof(ChannelPlugin), nameof(DrawTranspiler)));

            var existing = about.GetMethod("DrawChannelLink", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            if (existing != null)
                harmony.Patch(existing, prefix: new HarmonyMethod(typeof(ChannelPlugin), nameof(SkipExistingChannel)));
        }

        private static void BindPanelOpacity()
        {
            var file = ConfigFileForPanel();
            PanelOpacity = file.Bind("UI", "PanelOpacity", 1f, new ConfigDescription("Panel opacity. 1 = solid, 0.25 = more transparent.", new AcceptableValueRange<float>(0.25f, 1f)));
        }

        private static ConfigFile ConfigFileForPanel()
        {
            var pluginType = Type.GetType("RoRCheatPanel.Plugin, RoRCheatPanel");
            var instance = pluginType != null
                ? pluginType.GetProperty("Instance", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)?.GetValue(null)
                : null;
            if (instance is BaseUnityPlugin plugin)
                return plugin.Config;
            return _ownConfig;
        }

        private static void PatchFunTab(Harmony harmony)
        {
            var fun = Type.GetType("RoRCheatPanel.UI.Tabs.FunTab, RoRCheatPanel");
            var draw = fun != null
                ? fun.GetMethod("Draw", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)
                : null;
            if (draw == null)
            {
                Debug.LogWarning("[RoRCheatPanel.Channel] missing FunTab.Draw");
                return;
            }

            harmony.Patch(draw, transpiler: new HarmonyMethod(typeof(ChannelPlugin), nameof(FunTabTranspiler)));
        }

        private static IEnumerable<CodeInstruction> FunTabTranspiler(IEnumerable<CodeInstruction> instructions)
        {
            var endHorizontal = AccessTools.Method(typeof(GUILayout), "EndHorizontal", Type.EmptyTypes);
            var reset = AccessTools.Method(typeof(ChannelPlugin), nameof(DrawDistanceReset));
            var afterDistance = false;
            foreach (var ins in instructions)
            {
                if (ins.opcode == OpCodes.Ldstr && ins.operand as string == "fun_tpv_dist")
                    afterDistance = true;
                if (afterDistance && endHorizontal != null && ins.Calls(endHorizontal))
                {
                    yield return new CodeInstruction(OpCodes.Call, reset);
                    afterDistance = false;
                }
                yield return ins;
            }
        }

        private static void DrawDistanceReset()
        {
            if (!GUILayout.Button("0", GUILayout.Width(40f)))
                return;
            AcceptDistance(0f);
        }

        private static void PatchWindowAlpha(Harmony harmony)
        {
            var window = Type.GetType("RoRCheatPanel.UI.CheatWindow, RoRCheatPanel");
            var onGui = window != null
                ? window.GetMethod("OnGUI", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)
                : null;
            if (onGui == null)
            {
                Debug.LogWarning("[RoRCheatPanel.Channel] missing CheatWindow.OnGUI");
                return;
            }

            harmony.Patch(
                onGui,
                transpiler: new HarmonyMethod(typeof(ChannelPlugin), nameof(OnGuiTranspiler)),
                postfix: new HarmonyMethod(typeof(ChannelPlugin), nameof(AfterPanelGui)));

            var draw = window.GetMethod("Draw", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            if (draw != null)
                harmony.Patch(draw, prefix: new HarmonyMethod(typeof(ChannelPlugin), nameof(PaintWindowBackdrop)));
        }

        private static IEnumerable<CodeInstruction> OnGuiTranspiler(IEnumerable<CodeInstruction> instructions)
        {
            var setter = AccessTools.PropertySetter(typeof(GUI), "backgroundColor");
            var apply = AccessTools.Method(typeof(ChannelPlugin), nameof(ApplyPanelAlpha));
            var patched = false;
            foreach (var ins in instructions)
            {
                if (ins.opcode == OpCodes.Ldstr && ins.operand as string == "3.6.3")
                    ins.operand = ShownVersion;
                yield return ins;
                if (!patched && setter != null && ins.Calls(setter))
                {
                    yield return new CodeInstruction(OpCodes.Call, apply);
                    patched = true;
                }
            }
        }

        private static Texture2D _solid;

        private static void ApplyPanelAlpha()
        {
            var color = GUI.backgroundColor;
            color.a = Mathf.Clamp(PanelOpacity != null ? PanelOpacity.Value : 1f, 0.25f, 1f);
            GUI.backgroundColor = color;
        }

        private static void PaintWindowBackdrop()
        {
            if (Event.current == null || Event.current.type != EventType.Repaint)
                return;
            var window = Type.GetType("RoRCheatPanel.UI.CheatWindow, RoRCheatPanel");
            var field = window?.GetField("_rect", BindingFlags.NonPublic | BindingFlags.Static);
            if (!(field?.GetValue(null) is Rect rect) || rect.width < 8f || rect.height < 8f)
                return;

            var color = ThemePlate();
            color.a = Mathf.Clamp(PanelOpacity != null ? PanelOpacity.Value : 1f, 0.25f, 1f);
            var prev = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(new Rect(0f, -28f, rect.width, rect.height + 28f), SolidPixel(), ScaleMode.StretchToFill, true);
            GUI.color = prev;
        }

        private static Color ThemePlate()
        {
            var disco = CheatRuntimeType?.GetField("Disco", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            if (disco?.GetValue(null) is bool on && on)
                return Color.HSVToRGB(Time.unscaledTime * 0.12f % 1f, 0.55f, 0.55f);

            var theme = Type.GetType("RoRCheatPanel.UiTheme, RoRCheatPanel");
            var window = theme?.GetProperty("Window", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            if (window?.GetValue(null, null) is Color color)
                return color;
            return new Color(0.16f, 0.18f, 0.24f, 1f);
        }

        private static Texture2D SolidPixel()
        {
            if (_solid != null)
                return _solid;
            _solid = new Texture2D(4, 4, TextureFormat.RGBA32, false);
            var pixels = new Color[16];
            for (var i = 0; i < pixels.Length; i++)
                pixels[i] = Color.white;
            _solid.SetPixels(pixels);
            _solid.Apply();
            _solid.hideFlags = HideFlags.HideAndDontSave;
            _solid.wrapMode = TextureWrapMode.Clamp;
            _solid.filterMode = FilterMode.Point;
            return _solid;
        }

        private static void AfterPanelGui()
        {
            PanelFixes.DrawFreeTooltip();
        }

        private static bool SkipExistingChannel()
        {
            return false;
        }

        private static IEnumerable<CodeInstruction> DrawTranspiler(IEnumerable<CodeInstruction> instructions)
        {
            var endScroll = AccessTools.Method(typeof(GUILayout), "EndScrollView", Type.EmptyTypes);
            var footer = AccessTools.Method(typeof(ChannelPlugin), nameof(DrawSettingsFooter));
            foreach (var ins in instructions)
            {
                if (ins.opcode == OpCodes.Ldstr && ins.operand as string == "RoR Cheat Panel  3.6.3")
                    ins.operand = "RoR Cheat Panel  " + ShownVersion;
                if (endScroll != null && ins.Calls(endScroll))
                    yield return new CodeInstruction(OpCodes.Call, footer);
                yield return ins;
            }
        }

        private static void DrawSettingsFooter()
        {
            DrawOpacitySlider();
            if (!PanelChrome.HotkeysReplaced)
                PanelChrome.DrawHotkeys();
            DrawLink();
        }

        private static void DrawOpacitySlider()
        {
            var opacity = PanelOpacity != null ? Mathf.Clamp(PanelOpacity.Value, 0.25f, 1f) : 1f;
            var percent = Mathf.RoundToInt(opacity * 100f);
            GUILayout.BeginVertical("box");
            GUILayout.Label(OpacityTitle());
            GUILayout.BeginHorizontal();
            GUILayout.Label(OpacityValue(percent), GUILayout.Width(170f));
            var next = GUILayout.HorizontalSlider(opacity, 0.25f, 1f);
            if (GUILayout.Button("100", GUILayout.Width(40f)))
                next = 1f;
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
            if (PanelOpacity != null)
                PanelOpacity.Value = next;
        }

        private static string OpacityTitle()
        {
            switch (CurrentLang())
            {
                case "En":
                    return "Panel transparency";
                case "Es":
                    return "Transparencia del panel";
                default:
                    return "Прозрачность панели";
            }
        }

        private static string OpacityValue(int percent)
        {
            switch (CurrentLang())
            {
                case "En":
                    return "Visibility " + percent + "%";
                case "Es":
                    return "Visibilidad " + percent + "%";
                default:
                    return "Вид " + percent + "%";
            }
        }

        private static string CurrentLang()
        {
            var loc = Type.GetType("RoRCheatPanel.Loc, RoRCheatPanel");
            var current = loc != null
                ? loc.GetProperty("Current", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)?.GetValue(null)
                  ?? loc.GetField("Current", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)?.GetValue(null)
                : null;
            return current != null ? current.ToString() : "Ru";
        }

        private void LateUpdate()
        {
            FunExtras.LateApply();
            MonsterSpawn.Tick();
        }

        private void Update()
        {
            HoldMouseWhilePanelOpen();
            HoldLookSensitivity();
            if (ReadBool(ThirdPersonField))
                ThirdPersonField.SetValue(null, false);
            TrackThirdPerson();
            TickSeat();
            LobbySize.Tick();
            PanelFixes.TickNames();
            IconWarmup.Tick();
            SyncDistanceToggle();
            if (_flattenFrames <= 0)
                return;
            FlattenLocal();
            _flattenFrames--;
        }

        private static void Safe(string name, Action action)
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                Debug.LogError("[RoRCheatPanel.Channel] " + name + " failed: " + ex);
            }
        }

        private static void PatchLook(Harmony harmony)
        {
            var lookMode = Type.GetType("RoR2.CameraModes.CameraModePlayerBasic, RoR2");
            var applyLook = lookMode != null
                ? lookMode.GetMethod("ApplyLookInputInternal", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                : null;
            if (applyLook != null)
                harmony.Patch(applyLook, prefix: new HarmonyMethod(typeof(ChannelPlugin), nameof(SkipLookWhilePanelOpen)));
            else
                Debug.LogWarning("[RoRCheatPanel.Channel] missing ApplyLookInputInternal");

            var lookBase = Type.GetType("RoR2.CameraModes.CameraModeBase, RoR2");
            if (lookBase == null)
                return;
            foreach (var method in lookBase.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            {
                if (method.Name != "ApplyLookInput")
                    continue;
                harmony.Patch(method, prefix: new HarmonyMethod(typeof(ChannelPlugin), nameof(SkipLookWhilePanelOpen)));
            }
        }

        private static void PatchPrefix(Harmony harmony, string typeName, string method, string prefix)
        {
            var type = Type.GetType(typeName + ", RoRCheatPanel");
            var info = type != null
                ? type.GetMethod(method, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)
                : null;
            if (info == null)
            {
                Debug.LogWarning("[RoRCheatPanel.Channel] missing " + typeName + "." + method);
                return;
            }

            harmony.Patch(info, prefix: new HarmonyMethod(typeof(ChannelPlugin), prefix));
        }

        private static void Patch(Harmony harmony, string typeName, string method, string postfix)
        {
            var type = Type.GetType(typeName + ", RoRCheatPanel");
            var info = type != null
                ? type.GetMethod(method, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)
                : null;
            if (info == null)
            {
                Debug.LogWarning("[RoRCheatPanel.Channel] missing " + typeName + "." + method);
                return;
            }

            harmony.Patch(info, postfix: new HarmonyMethod(typeof(ChannelPlugin), postfix));
        }

        private static bool ReadBool(FieldInfo field)
        {
            return field != null && field.GetValue(null) is bool value && value;
        }

        private static float ReadFov()
        {
            if (FovField != null && FovField.GetValue(null) is float fov)
                return Mathf.Clamp(fov, 30f, 140f);
            return 60f;
        }

        private static void BeginFlatten()
        {
            _flattenFrames = FlattenFrames;
            FlattenLocal();
        }

        private static void AfterRestoreCamera()
        {
            FirstPerson = false;
            DistanceDriven = false;
            _stockDistanceCleared = true;
            AcceptDistance(0f);
            if (FovField != null)
                FovField.SetValue(null, 60f);
            SeatFirstPerson(true);
            SnapCameraToBody();
        }

        private static void AfterRuntimeTick()
        {
            RepairTimeScale();
            RepairStopwatch();
            EnemyCloak.Tick();
            HostVanilla.Tick();
        }

        private static void RepairTimeScale()
        {
            var scale = TimeScaleField != null && TimeScaleField.GetValue(null) is float value ? value : 1f;
            if (float.IsNaN(scale) || scale <= 0f)
            {
                scale = 1f;
                TimeScaleField?.SetValue(null, scale);
            }

            scale = Mathf.Clamp(scale, 0.1f, 3f);
            if (float.IsNaN(Time.timeScale) || Time.timeScale <= 0f)
            {
                Time.timeScale = scale;
                Time.fixedDeltaTime = 0.02f * scale;
            }
        }

        private static void RepairStopwatch()
        {
            if (!NetworkServer.active || Run.instance == null)
                return;
            var frozen = ReadBool(FreezeField);
            if (!frozen && PauseWhileOpen() && WindowOpened())
                return;
            Run.instance.SetForcePauseRunStopwatch(frozen);
        }

        private static bool PauseWhileOpen()
        {
            var config = Type.GetType("RoRCheatPanel.CheatConfig, RoRCheatPanel");
            var entry = config != null
                ? config.GetField("PauseStopwatchOnOpen", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)?.GetValue(null)
                : null;
            var value = entry?.GetType().GetProperty("Value")?.GetValue(entry);
            return value is bool flag && flag;
        }

        private static void HoldMouseWhilePanelOpen()
        {
            var user = LocalUserManager.GetFirstLocalUser();
            var mouse = user != null && user.inputPlayer != null ? user.inputPlayer.controllers.Mouse : null;
            if (mouse == null)
                return;
            if (WindowOpened())
            {
                if (!_mouseOff)
                {
                    _mouseWasOn = mouse.enabled;
                    _mouseOff = true;
                }

                mouse.enabled = false;
                return;
            }

            if (!_mouseOff)
                return;
            mouse.enabled = _mouseWasOn;
            _mouseOff = false;
        }

        private static void HoldLookSensitivity()
        {
            var user = LocalUserManager.GetFirstLocalUser();
            var profile = user != null ? user.userProfile : null;
            var field = profile != null
                ? profile.GetType().GetField("mouseLookSensitivity", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                : null;
            if (field == null || field.FieldType != typeof(float))
                return;
            if (WindowOpened())
            {
                if (_savedLook < 0f)
                    _savedLook = (float)field.GetValue(profile);
                field.SetValue(profile, 0f);
                return;
            }

            if (_savedLook < 0f)
                return;
            field.SetValue(profile, _savedLook);
            _savedLook = -1f;
        }

        private static void TrackThirdPerson()
        {
            var on = ReadBool(ThirdPersonField);
            if (on && !_tpWasOn)
            {
                var rig = LocalRig();
                if (rig != null)
                {
                    _fpRot = rig.currentCameraState.rotation;
                    _haveFpRot = true;
                }
            }

            if (!on && _tpWasOn)
                SeatFirstPerson(false);
            _tpWasOn = on;
        }

        private static void TickSeat()
        {
            if (_seatFrames <= 0 || ReadBool(ThirdPersonField))
                return;
            var rig = LocalRig();
            if (rig == null)
                return;
            if (FirstPerson)
                SetInstanceCameraDistance(rig, 0f);
            _seatFrames--;
        }

        private static void SeatFirstPerson(bool resetFov)
        {
            ThirdPersonField?.SetValue(null, false);
            _tpWasOn = false;
            _seatFrames = 12;
            var overlay = CheatRuntimeType != null
                ? CheatRuntimeType.GetField("_camOverlayOn", BindingFlags.NonPublic | BindingFlags.Static)
                : null;
            overlay?.SetValue(null, false);
            if (resetFov)
            {
                FovField?.SetValue(null, 60f);
                var rigFov = LocalRig();
                if (rigFov != null)
                    rigFov.baseFov = 60f;
            }

            var rig = LocalRig();
            if (rig == null || !FirstPerson)
                return;
            SetInstanceCameraDistance(rig, 0f);
        }

        private static void ApplySeatedLook(CameraRigController rig)
        {
            var body = LocalUserManager.GetFirstLocalUser()?.cachedBody;
            var aim = _haveFpRot ? _fpRot * Vector3.forward : Vector3.forward;
            if (!_haveFpRot && body != null)
                aim = body.inputBank != null && body.inputBank.aimDirection.sqrMagnitude > 0.001f
                    ? body.inputBank.aimDirection
                    : body.transform.forward;
            if (aim.sqrMagnitude < 0.001f)
                aim = Vector3.forward;
            var rotation = _haveFpRot ? _fpRot : Quaternion.LookRotation(aim.normalized, Vector3.up);
            aim = rotation * Vector3.forward;
            _heldRot = rotation;
            _lookHeld = WindowOpened();
            if (_applying)
                return;
            _applying = true;
            try
            {
                var state = rig.currentCameraState;
                state.rotation = rotation;
                if (FovField != null && FovField.GetValue(null) is float fov)
                    state.fov = fov;
                rig.currentCameraState = state;
                DesiredStateField?.SetValue(rig, state);
                LerpStateField?.SetValue(rig, state);
                LerpTimeField?.SetValue(rig, 1f);
                SyncPitchYaw(rig, aim);
                if (body != null && body.inputBank != null)
                    body.inputBank.aimDirection = aim.normalized;
                rig.SetCameraState(state);
                if (rig.sceneCam != null)
                    rig.sceneCam.transform.rotation = rotation;
            }
            finally
            {
                _applying = false;
            }
        }

        private static CameraRigController LocalRig()
        {
            return LocalUserManager.GetFirstLocalUser()?.cameraRigController;
        }

        private static bool SkipLookWhilePanelOpen()
        {
            return !WindowOpened();
        }

        private static void HoldLook(CameraRigController rig)
        {
            if (rig == null)
                return;
            if (!WindowOpened())
            {
                _lookHeld = false;
                return;
            }

            if (!_lookHeld)
            {
                _heldRot = rig.currentCameraState.rotation;
                _lookHeld = true;
            }

            if (_applying)
                return;

            _applying = true;
            try
            {
                var state = rig.currentCameraState;
                state.rotation = _heldRot;
                rig.currentCameraState = state;
                if (DesiredStateField != null)
                {
                    var desired = (CameraState)DesiredStateField.GetValue(rig);
                    desired.rotation = _heldRot;
                    DesiredStateField.SetValue(rig, desired);
                }

                if (LerpStateField != null)
                {
                    var lerp = (CameraState)LerpStateField.GetValue(rig);
                    lerp.rotation = _heldRot;
                    LerpStateField.SetValue(rig, lerp);
                }

                LerpTimeField?.SetValue(rig, 1f);
                var forward = _heldRot * Vector3.forward;
                SyncPitchYaw(rig, forward);
                if (rig.sceneCam != null)
                    rig.sceneCam.transform.rotation = _heldRot;
                var crosshair = typeof(CameraRigController).GetField("<crosshairWorldPosition>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic);
                crosshair?.SetValue(rig, state.position + forward * 250f);
                var body = LocalUserManager.GetFirstLocalUser()?.cachedBody;
                if (body != null && body.inputBank != null)
                    body.inputBank.aimDirection = forward;
                rig.SetCameraState(state);
            }
            finally
            {
                _applying = false;
            }
        }

        private static bool WindowOpened()
        {
            var window = Type.GetType("RoRCheatPanel.UI.CheatWindow, RoRCheatPanel");
            var opened = window != null
                ? window.GetProperty("Opened", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)?.GetValue(null)
                : null;
            return opened is bool flag && flag;
        }

        private static void PatchDrunkRemoval(Harmony harmony)
        {
            var helpers = Type.GetType("RoRCheatPanel.UI.UiHelpers, RoRCheatPanel");
            var toggle = helpers != null
                ? helpers.GetMethod("ToggleButton", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)
                : null;
            if (toggle != null)
                harmony.Patch(toggle, prefix: new HarmonyMethod(typeof(ChannelPlugin), nameof(HideDrunkToggle)));

            var late = CheatRuntimeType != null
                ? CheatRuntimeType.GetMethod("LateTick", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)
                : null;
            if (late != null)
                harmony.Patch(late, prefix: new HarmonyMethod(typeof(ChannelPlugin), nameof(ForceDrunkOff)));
        }

        private static bool HideDrunkToggle(string label, ref bool __result)
        {
            if (label != "Пьяная камера" && label != "Drunk camera" && label != "Cámara borracha")
                return true;
            __result = false;
            DrunkField?.SetValue(null, false);
            return false;
        }

        private static void ForceDrunkOff()
        {
            if (ReadBool(DrunkField))
                DrunkField.SetValue(null, false);
        }

        private static bool SkipDrunkPositionPin()
        {
            return false;
        }

        private static void AfterLateTick()
        {
            var drunk = ReadBool(DrunkField);
            if (_wasDrunk && !drunk && !ReadBool(ThirdPersonField))
                BeginFlatten();
            _wasDrunk = drunk;

            var local = LocalUserManager.GetFirstLocalUser();
            var rig = local != null ? local.cameraRigController : null;
            if (rig == null)
                return;
            KeepDrunkFollowing(rig);
        }

        private static void KeepDrunkFollowing(CameraRigController rig)
        {
            if (rig == null || !ReadBool(DrunkField) || ReadBool(ThirdPersonField) || _applying)
                return;
            var follow = rig.currentCameraState.position;
            var body = LocalUserManager.GetFirstLocalUser()?.cachedBody;
            if (body != null && (follow - body.corePosition).sqrMagnitude > 64f)
                follow = body.corePosition + Vector3.up * 1.4f;

            if (rig.sceneCam != null)
                rig.sceneCam.transform.position = follow;

            var savedPos = CheatRuntimeType != null
                ? CheatRuntimeType.GetField("_savedCamPos", BindingFlags.NonPublic | BindingFlags.Static)
                : null;
            savedPos?.SetValue(null, follow);
        }

        private static void BeforeCameraDistance(CameraRigController __instance)
        {
            if (!IsLocal(__instance) || !FirstPerson || DistanceDriven)
                return;
            SetInstanceCameraDistance(__instance, 0f);
        }

        private static void AfterRigLateUpdate(CameraRigController __instance)
        {
            if (!IsLocal(__instance))
                return;
            if (_flattenFrames > 0)
                FlattenRig(__instance);
            ApplyCameraSlider(__instance);
            HoldLook(__instance);
        }

        private static void AfterSetCameraState(CameraRigController __instance)
        {
            if (_flattenFrames <= 0 || _applying || !IsLocal(__instance))
                return;
            FlattenRig(__instance);
        }

        private static bool IsLocal(CameraRigController rig)
        {
            var local = LocalUserManager.GetFirstLocalUser();
            return local != null && local.cameraRigController == rig;
        }

        private static float ReadDistance()
        {
            if (DistanceField != null && DistanceField.GetValue(null) is float distance)
                return CameraFraming.ClampDistance(distance);
            return 0f;
        }

        private static void SyncDistanceToggle()
        {
            var distance = ReadDistance();
            if (!_distanceInit)
            {
                _lastDistance = distance;
                _distanceInit = true;
                return;
            }

            if (Mathf.Abs(distance - _lastDistance) > 0.01f)
                DistanceDriven = true;
            _lastDistance = distance;
        }

        private static float ReadBodyScale()
        {
            if (BodyScaleField != null && BodyScaleField.GetValue(null) is float value)
                return value;
            return 1f;
        }

        internal static void TouchDistance()
        {
            DistanceDriven = true;
        }

        internal static void AcceptDistance(float distance)
        {
            distance = CameraFraming.ClampDistance(distance);
            DistanceField?.SetValue(null, distance);
            _lastDistance = distance;
            _distanceInit = true;
        }

        private static void ApplyCameraSlider(CameraRigController rig)
        {
            if (rig == null)
                return;
            if (!_stockDistanceCleared && !DistanceDriven)
            {
                _stockDistanceCleared = true;
                AcceptDistance(0f);
            }

            var fov = ReadFov();
            if (Mathf.Abs(fov - 60f) > 0.5f)
                WriteFov(rig, fov);
            if (FirstPerson && !DistanceDriven)
                SetInstanceCameraDistance(rig, 0f);
            var distance = DistanceDriven ? ReadDistance() : 0f;
            if (distance <= 0.05f || rig.sceneCam == null || FirstPerson)
                return;
            var body = LocalBody();
            if (body == null)
                return;
            var aim = body.inputBank != null && body.inputBank.aimDirection.sqrMagnitude > 0.001f
                ? body.inputBank.aimDirection
                : body.transform.forward;
            if (aim.sqrMagnitude < 0.001f)
                return;
            var anchor = body.corePosition + Vector3.up * 1.4f;
            rig.sceneCam.transform.position = anchor - aim.normalized * distance;
        }

        private static void WriteFov(CameraRigController rig, float fov)
        {
            rig.baseFov = fov;
            var state = rig.currentCameraState;
            state.fov = fov;
            rig.currentCameraState = state;
            if (DesiredStateField?.GetValue(rig) is CameraState desired)
            {
                desired.fov = fov;
                DesiredStateField.SetValue(rig, desired);
            }

            if (LerpStateField?.GetValue(rig) is CameraState lerp)
            {
                lerp.fov = fov;
                LerpStateField.SetValue(rig, lerp);
            }

            if (rig.sceneCam != null)
                rig.sceneCam.fieldOfView = fov;
        }

        private static void SnapCameraToBody()
        {
            var rig = LocalRig();
            var body = LocalBody();
            if (rig == null || body == null)
                return;
            var aim = body.inputBank != null && body.inputBank.aimDirection.sqrMagnitude > 0.001f
                ? body.inputBank.aimDirection
                : body.transform.forward;
            if (aim.sqrMagnitude < 0.001f)
                aim = Vector3.forward;
            var anchor = body.corePosition + Vector3.up * 1.4f;
            var rotation = Quaternion.LookRotation(aim.normalized, Vector3.up);
            var state = rig.currentCameraState;
            state.position = anchor;
            state.rotation = rotation;
            state.fov = ReadFov();
            rig.currentCameraState = state;
            DesiredStateField?.SetValue(rig, state);
            LerpStateField?.SetValue(rig, state);
            if (rig.sceneCam != null)
                rig.sceneCam.transform.SetPositionAndRotation(anchor, rotation);
            SetInstanceCameraDistance(rig, 0f);
            rig.SetCameraState(state);
        }

        private static void SetInstanceCameraDistance(CameraRigController rig, float distance)
        {
            var mode = rig.cameraMode;
            if (mode == null)
                return;
            var data = mode.DebugGetInstanceData(rig);
            if (data == null)
                return;
            var type = data.GetType();
            type.GetField("currentCameraDistance", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)?.SetValue(data, distance);
            type.GetField("cameraDistanceVelocity", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)?.SetValue(data, 0f);
        }

        private static void FlattenLocal()
        {
            var local = LocalUserManager.GetFirstLocalUser();
            var rig = local != null ? local.cameraRigController : null;
            if (rig)
                FlattenRig(rig);
        }

        private static void FlattenRig(CameraRigController rig)
        {
            if (!rig || _applying)
                return;

            _applying = true;
            try
            {
                var fov = ReadFov();
                rig.baseFov = fov;

                var current = FlattenState(rig.currentCameraState, fov);
                var desired = DesiredStateField != null
                    ? FlattenState((CameraState)DesiredStateField.GetValue(rig), fov)
                    : current;
                var lerp = LerpStateField != null
                    ? FlattenState((CameraState)LerpStateField.GetValue(rig), fov)
                    : current;

                rig.currentCameraState = current;
                DesiredStateField?.SetValue(rig, desired);
                LerpStateField?.SetValue(rig, lerp);
                LerpTimeField?.SetValue(rig, 1f);

                var look = current.rotation * Vector3.forward;
                SyncPitchYaw(rig, look);

                if (rig.sceneCam)
                    FlattenTransform(rig.sceneCam.transform);
                if (rig.uiCam)
                    FlattenTransform(rig.uiCam.transform);
                if (rig.skyboxCam)
                    FlattenTransform(rig.skyboxCam.transform);

                rig.SetCameraState(current);
            }
            finally
            {
                _applying = false;
            }
        }

        private static void SyncPitchYaw(CameraRigController rig, Vector3 look)
        {
            if (look.sqrMagnitude < 0.0001f)
                return;

            var mode = rig.cameraMode;
            if (mode == null)
                return;

            var data = mode.DebugGetInstanceData(rig);
            if (data == null)
                return;

            var method = data.GetType().GetMethod("SetPitchYawFromLookVector", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (method != null && method.GetParameters().Length == 1)
                method.Invoke(data, new object[] { look.normalized });
        }

        private static CameraState FlattenState(CameraState state, float fov)
        {
            var forward = state.rotation * Vector3.forward;
            if (forward.sqrMagnitude < 0.0001f)
                forward = Vector3.forward;
            state.rotation = Quaternion.LookRotation(forward.normalized, Vector3.up);
            state.fov = fov;
            return state;
        }

        private static void FlattenTransform(Transform t)
        {
            if (!t)
                return;
            var forward = t.forward;
            if (forward.sqrMagnitude < 0.0001f)
                return;
            t.rotation = Quaternion.LookRotation(forward.normalized, Vector3.up);
        }

        private static string SupportCaption()
        {
            switch (CurrentLang())
            {
                case "En":
                    return "support me 😘";
                case "Es":
                    return "apóyame 😘";
                default:
                    return "поддержите меня 😘";
            }
        }

        private static void DrawLink()
        {
            var tex = ChannelTexture();
            var height = 118f;
            var width = tex
                ? Mathf.Clamp(tex.width * (height / Mathf.Max(1, tex.height)), 220f, 420f)
                : 260f;

            GUILayout.Space(28);
            var caption = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Bold
            };
            var prev = GUI.color;
            if (_kissFont == null)
                _kissFont = Font.CreateDynamicFontFromOSFont("Segoe UI Emoji", 14);
            if (_kissFont != null)
                caption.font = _kissFont;
            GUI.color = new Color(0.82f, 0.8f, 1f);
            GUILayout.Label(SupportCaption(), caption, GUILayout.ExpandWidth(true));
            GUI.color = prev;

            var siteTex = SiteTexture();
            var siteHeight = 58f;
            var siteWidth = siteTex
                ? siteTex.width * (siteHeight / Mathf.Max(1f, siteTex.height))
                : 200f;

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            var rect = GUILayoutUtility.GetRect(width, height, GUILayout.Width(width), GUILayout.Height(height));
            if (tex)
                GUI.DrawTexture(rect, tex, ScaleMode.ScaleToFit, true);
            if (GUI.Button(rect, new GUIContent(string.Empty, YoutubeUrl), GUIStyle.none))
                Application.OpenURL(YoutubeUrl);

            GUILayout.Space(18f);
            GUILayout.BeginVertical(GUILayout.Width(siteWidth), GUILayout.Height(height));
            GUILayout.Space((height - siteHeight) * 0.5f);
            var siteRect = GUILayoutUtility.GetRect(siteWidth, siteHeight, GUILayout.Width(siteWidth), GUILayout.Height(siteHeight));
            if (siteTex)
                GUI.DrawTexture(siteRect, siteTex, ScaleMode.ScaleToFit, true);
            if (GUI.Button(siteRect, new GUIContent(string.Empty, SiteTip()), GUIStyle.none))
                Application.OpenURL(SiteUrl);
            GUILayout.EndVertical();
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            GUILayout.Space(12);
        }

        private static string SiteTip()
        {
            switch (CurrentLang())
            {
                case "En":
                    return "Download the latest version here, and send a note through the feedback form.";
                case "Es":
                    return "Aquí puedes descargar la última versión y escribir por el formulario.";
                default:
                    return "Тут можно скачать последнюю версию чита и написать через форму обратной связи.";
            }
        }

        private static Texture2D ChannelTexture()
        {
            if (_tried)
                return _tex;
            _tried = true;
            try
            {
                var dir = IoPath.GetDirectoryName(typeof(ChannelPlugin).Assembly.Location);
                var path = IoPath.Combine(string.IsNullOrEmpty(dir) ? "." : dir, "voicenextdoor.png");
                if (!File.Exists(path))
                    return null;
                var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                tex.wrapMode = TextureWrapMode.Clamp;
                tex.filterMode = FilterMode.Bilinear;
                if (!ImageConversion.LoadImage(tex, File.ReadAllBytes(path), true))
                    return null;
                _tex = tex;
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[RoRCheatPanel.Channel] voicenextdoor.png: " + ex.Message);
            }

            return _tex;
        }

        private static Texture2D SiteTexture()
        {
            if (_siteTried)
                return _siteTex;
            _siteTried = true;
            try
            {
                var dir = IoPath.GetDirectoryName(typeof(ChannelPlugin).Assembly.Location);
                var path = IoPath.Combine(string.IsNullOrEmpty(dir) ? "." : dir, "website.png");
                if (!File.Exists(path))
                    return null;
                var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                tex.wrapMode = TextureWrapMode.Clamp;
                tex.filterMode = FilterMode.Bilinear;
                if (!ImageConversion.LoadImage(tex, File.ReadAllBytes(path), true))
                    return null;
                _siteTex = tex;
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[RoRCheatPanel.Channel] website.png: " + ex.Message);
            }

            return _siteTex;
        }
    }
}
