using System;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace RoRCheatPanel.Channel
{
    internal static class PanelFixes
    {
        private static Texture2D _gem;
        private static int _nameFlush;

        public static void Install(Harmony harmony)
        {
            var lang = Type.GetType("RoRCheatPanel.GameUtil, RoRCheatPanel")
                ?.GetMethod("Lang", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            if (lang != null)
                harmony.Patch(lang, postfix: new HarmonyMethod(typeof(PanelFixes), nameof(FixLang)));

            var ping = Type.GetType("RoRCheatPanel.GameUtil, RoRCheatPanel")
                ?.GetMethod("PingTeleporter", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            if (ping != null)
                harmony.Patch(ping, prefix: new HarmonyMethod(typeof(PanelFixes), nameof(PingTeleporter)));

            var tools = Type.GetType("RoRCheatPanel.UI.Tabs.ToolsTab, RoRCheatPanel");
            var toggles = tools?.GetMethod("DrawToggles", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            if (toggles != null)
                harmony.Patch(toggles, transpiler: new HarmonyMethod(typeof(PanelFixes), nameof(RaiseNoclipCap)));

            var slider = AccessTools.Method(typeof(GUILayout), "HorizontalSlider", new[] { typeof(float), typeof(float), typeof(float), typeof(GUILayoutOption[]) });
            if (slider != null)
                harmony.Patch(slider, prefix: new HarmonyMethod(typeof(PanelFixes), nameof(StretchNoclipSlider)));

            var helpers = Type.GetType("RoRCheatPanel.UI.UiHelpers, RoRCheatPanel");
            if (helpers != null)
            {
                foreach (var method in helpers.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static))
                {
                    if (method.Name != "DrawIcon")
                        continue;
                    var parameters = method.GetParameters();
                    if (parameters.Length == 0)
                        continue;
                    var first = parameters[0].ParameterType;
                    if (first == typeof(Texture) || first == typeof(Sprite))
                        continue;
                    harmony.Patch(method, prefix: new HarmonyMethod(typeof(PanelFixes), nameof(FillEmptyIcon)));
                }
            }

            var tooltip = helpers?.GetMethod("DrawTooltip", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            if (tooltip != null)
                harmony.Patch(tooltip, prefix: new HarmonyMethod(typeof(PanelFixes), nameof(SkipClippedTooltip)));
        }

        public static void FixLang(string token, ref string __result)
        {
            if (string.IsNullOrEmpty(token) || (!string.IsNullOrEmpty(__result) && __result != token))
                return;
            foreach (var code in LanguageCodes())
            {
                string text;
                try
                {
                    text = Language.GetString(token, code);
                }
                catch
                {
                    continue;
                }

                if (string.IsNullOrEmpty(text) || text == token)
                    continue;
                __result = text;
                return;
            }

            if (string.IsNullOrEmpty(__result) || __result == token)
                __result = PrettyToken(token);
        }

        private static string PrettyToken(string token)
        {
            if (string.IsNullOrEmpty(token))
                return token;
            var core = token;
            if (core.StartsWith("ITEM_"))
                core = core.Substring(5);
            if (core.StartsWith("EQUIPMENT_"))
                core = core.Substring(10);
            if (core.EndsWith("_NAME"))
                core = core.Substring(0, core.Length - 5);
            var text = new System.Text.StringBuilder();
            for (var i = 0; i < core.Length; i++)
            {
                var letter = core[i];
                if (letter == '_')
                {
                    text.Append(' ');
                    continue;
                }

                if (i > 0 && char.IsUpper(letter) && char.IsLetter(core[i - 1]) && char.IsLower(core[i - 1]))
                    text.Append(' ');
                text.Append(letter);
            }

            var pretty = text.ToString().Trim();
            return string.IsNullOrEmpty(pretty) ? token : pretty;
        }

        public static void TickNames()
        {
            if (_nameFlush < 0 || Time.frameCount % 120 != 0)
                return;
            _nameFlush++;
            string probe;
            try
            {
                probe = Language.GetString("ITEM_BOOSTALLSTATS_NAME", "RU");
            }
            catch
            {
                probe = null;
            }

            if (string.IsNullOrEmpty(probe) || probe == "ITEM_BOOSTALLSTATS_NAME")
            {
                if (_nameFlush > 40)
                    _nameFlush = -1;
                return;
            }

            FlushLists();
            _nameFlush = -1;
        }

        public static void FlushLists()
        {
            foreach (var name in new[] { "ItemsTab", "EquipmentTab", "SkillsTab", "AchievementsTab" })
            {
                var type = Type.GetType("RoRCheatPanel.UI.Tabs." + name + ", RoRCheatPanel");
                type?.GetField("_cacheKey", BindingFlags.NonPublic | BindingFlags.Static)?.SetValue(null, int.MinValue);
            }
        }

        private static string[] LanguageCodes()
        {
            var loc = Type.GetType("RoRCheatPanel.Loc, RoRCheatPanel");
            var current = loc?.GetField("Current", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)?.GetValue(null);
            var lang = current != null ? current.ToString() : "Ru";
            if (lang == "En")
                return new[] { "en" };
            if (lang == "Es")
                return new[] { "es-ES", "es-419", "en" };
            return new[] { "RU", "en" };
        }

        public static bool SelectionIncludesOthers()
        {
            var util = Type.GetType("RoRCheatPanel.GameUtil, RoRCheatPanel");
            if (util == null)
                return false;
            var selected = util.GetField("SelectedUsers", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)?.GetValue(null) as System.Collections.IEnumerable;
            var local = util.GetProperty("LocalNetworkUser", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)?.GetValue(null, null);
            if (selected == null)
                return false;
            foreach (var user in selected)
            {
                if (user != null && !ReferenceEquals(user, local))
                    return true;
            }

            return false;
        }

        public static bool PingTeleporter()
        {
            var teleporter = TeleporterInteraction.instance;
            if (teleporter == null)
            {
                var found = UnityEngine.Object.FindObjectsOfType<TeleporterInteraction>();
                if (found != null && found.Length > 0)
                    teleporter = found[0];
            }

            if (teleporter == null)
            {
                Toast(Tr("Телепорт на этой карте не найден", "No teleporter on this stage", "No hay teletransportador en este mapa"));
                return false;
            }

            var discovered = typeof(TeleporterInteraction).GetProperty("NetworkisDiscovered", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            discovered?.SetValue(teleporter, true, null);
            var marker = typeof(TeleporterInteraction).GetField("teleporterPositionIndicator", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)?.GetValue(teleporter) as Component;
            if (marker != null && !marker.gameObject.activeSelf)
                marker.gameObject.SetActive(true);

            var user = LocalUserManager.GetFirstLocalUser();
            var body = user != null ? user.cachedBody : null;
            if (body == null && user != null && user.currentNetworkUser != null)
                body = user.currentNetworkUser.GetCurrentBody();
            var pinger = body != null ? body.GetComponent<PingerController>() : null;
            if (pinger == null)
            {
                Toast(Tr("Телепорт отмечен на карте", "Teleporter marked on the map", "Teletransportador marcado en el mapa"));
                return false;
            }

            var info = new PingerController.PingInfo
            {
                active = true,
                origin = teleporter.transform.position + Vector3.up * 2f,
                normal = Vector3.up,
                targetNetworkIdentity = teleporter.GetComponent<NetworkIdentity>(),
                forceUseHitOriginForPosition = true
            };
            pinger.CallCmdPing(info);
            Toast(Tr("Пинг на телепорт", "Teleporter pinged", "Teletransportador marcado"));
            return false;
        }

        public static bool FillEmptyIcon(object[] __args)
        {
            if (__args == null || __args.Length == 0 || __args[0] == null)
                return true;
            var tex = __args[0].GetType().GetField("Tex")?.GetValue(__args[0]) as UnityEngine.Object;
            if (tex != null)
                return true;
            var size = __args.Length > 1 && __args[1] is float value ? value : 36f;
            var rect = GUILayoutUtility.GetRect(size, size, GUILayout.Width(size), GUILayout.Height(size));
            if (Event.current.type == EventType.Repaint)
                GUI.DrawTexture(rect, Gem(), ScaleMode.ScaleToFit, true);
            return false;
        }

        private static string _tip;
        private static Vector2 _tipAt;

        public static bool SkipClippedTooltip()
        {
            if (Event.current == null || Event.current.type != EventType.Repaint)
                return false;
            var text = GUI.tooltip;
            if (string.IsNullOrEmpty(text))
            {
                _tip = null;
                return false;
            }

            var mouse = Event.current.mousePosition;
            var window = Type.GetType("RoRCheatPanel.UI.CheatWindow, RoRCheatPanel");
            var rect = window?.GetField("_rect", BindingFlags.NonPublic | BindingFlags.Static)?.GetValue(null) is Rect area
                ? area
                : new Rect(48f, 48f, 1140f, 760f);
            var scale = 1f;
            var config = Type.GetType("RoRCheatPanel.CheatConfig, RoRCheatPanel");
            var entry = config?.GetField("UiScale", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)?.GetValue(null);
            if (entry?.GetType().GetProperty("Value")?.GetValue(entry) is float value)
                scale = Mathf.Clamp(value, 0.6f, 2f);
            _tip = text;
            _tipAt = new Vector2((rect.x + mouse.x) * scale, (rect.y + mouse.y) * scale);
            return false;
        }

        public static void DrawFreeTooltip()
        {
            var text = _tip;
            if (string.IsNullOrEmpty(text))
                return;
            var style = new GUIStyle(GUI.skin.box)
            {
                wordWrap = true,
                alignment = TextAnchor.UpperLeft,
                padding = new RectOffset(8, 8, 6, 6)
            };
            const float width = 360f;
            var height = style.CalcHeight(new GUIContent(text), width) + 8f;
            var mouse = _tipAt;
            var x = mouse.x + 16f;
            var y = mouse.y - height - 16f;
            var limitX = Screen.width - 8f;
            if (y < 4f)
                y = mouse.y + 20f;
            if (x + width > limitX)
                x = Mathf.Max(4f, limitX - width);
            if (x < 4f)
                x = 4f;
            _tipStyle = style;
            GUI.Window(0x52C0E0, new Rect(x, y, width, height), DrawTipWindow, GUIContent.none, style);
            GUI.BringWindowToFront(0x52C0E0);
        }

        private static GUIStyle _tipStyle;

        private static void DrawTipWindow(int id)
        {
            var style = _tipStyle ?? GUI.skin.label;
            GUI.Label(new Rect(8f, 6f, 344f, 200f), _tip ?? "", style);
        }

        private static bool _sliderBusy;

        public static bool StretchNoclipSlider(float value, float leftValue, float rightValue, ref float __result)
        {
            if (_sliderBusy || Mathf.Abs(rightValue - 80f) > 0.05f)
                return true;
            _sliderBusy = true;
            try
            {
                __result = GUILayout.HorizontalSlider(value, leftValue, 600f);
            }
            finally
            {
                _sliderBusy = false;
            }

            return false;
        }

        private static System.Collections.Generic.IEnumerable<CodeInstruction> RaiseNoclipCap(System.Collections.Generic.IEnumerable<CodeInstruction> instructions)
        {
            foreach (var ins in instructions)
            {
                if (ins.opcode == OpCodes.Ldc_R4 && ins.operand is float value && Mathf.Abs(value - 80f) < 0.01f)
                    ins.operand = 600f;
                if (ins.opcode == OpCodes.Ldc_R4 && ins.operand is float width && Mathf.Abs(width - 180f) < 0.01f)
                    ins.operand = 230f;
                yield return ins;
            }
        }

        private static Texture2D Gem()
        {
            if (_gem != null)
                return _gem;
            const int n = 32;
            _gem = new Texture2D(n, n, TextureFormat.RGBA32, false);
            var pixels = new Color[n * n];
            var clear = new Color(0f, 0f, 0f, 0f);
            for (var i = 0; i < pixels.Length; i++)
                pixels[i] = clear;
            for (var y = 0; y < n; y++)
            {
                for (var x = 0; x < n; x++)
                {
                    var dx = Mathf.Abs(x - 15.5f);
                    var dy = Mathf.Abs(y - 15.5f);
                    if (dx / 11f + dy / 14f > 1f)
                        continue;
                    var edge = dx / 11f + dy / 14f > 0.82f;
                    pixels[y * n + x] = edge
                        ? new Color(0.75f, 0.9f, 1f, 1f)
                        : Color.Lerp(new Color(0.25f, 0.15f, 0.55f, 1f), new Color(0.35f, 0.85f, 0.95f, 1f), y / 31f);
                }
            }

            _gem.SetPixels(pixels);
            _gem.Apply();
            _gem.hideFlags = HideFlags.HideAndDontSave;
            return _gem;
        }

        private static void Toast(string message)
        {
            var helpers = Type.GetType("RoRCheatPanel.UI.UiHelpers, RoRCheatPanel");
            helpers?.GetMethod("Toast", BindingFlags.Public | BindingFlags.Static)?.Invoke(null, new object[] { message });
        }

        private static string Tr(string ru, string en, string es)
        {
            var loc = Type.GetType("RoRCheatPanel.Loc, RoRCheatPanel");
            var current = loc?.GetField("Current", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)?.GetValue(null);
            var lang = current != null ? current.ToString() : "Ru";
            if (lang == "En")
                return en;
            if (lang == "Es")
                return es;
            return ru;
        }
    }
}
