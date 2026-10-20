using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace RoRCheatPanel.Channel
{
    internal static class MenuTidy
    {
        internal static bool OwnsFun = true;

        private static int _frame = -1;
        private static Vector2 _scroll;
        private static float _scrollHeight = 420f;
        private static readonly HashSet<string> Seen = new HashSet<string>();
        private static bool _teams;
        private static string _nick = "БОСС";

        public static void Install(Harmony harmony)
        {
            foreach (var method in typeof(GUILayout).GetMethods(BindingFlags.Public | BindingFlags.Static))
            {
                if (method.Name != "Button")
                    continue;
                var parameters = method.GetParameters();
                if (parameters.Length == 0 || parameters[0].ParameterType != typeof(string))
                    continue;
                var options = false;
                for (var i = 0; i < parameters.Length; i++)
                {
                    if (parameters[i].ParameterType == typeof(GUILayoutOption[]) && parameters[i].Name == "options")
                        options = true;
                }

                if (!options)
                    continue;
                var styled = parameters.Length >= 2 && parameters[1].ParameterType == typeof(GUIStyle);
                harmony.Patch(method, prefix: new HarmonyMethod(typeof(MenuTidy), styled ? nameof(HideStyled) : nameof(HidePlain)));
            }

            var fun = Type.GetType("RoRCheatPanel.UI.Tabs.FunTab, RoRCheatPanel");
            var draw = fun?.GetMethod("Draw", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            if (draw != null)
                harmony.Patch(draw, prefix: new HarmonyMethod(typeof(MenuTidy), nameof(DrawFun)));

            var teams = Type.GetType("RoRCheatPanel.UI.Tabs.TeamsTab, RoRCheatPanel");
            var teamsDraw = teams?.GetMethod("Draw", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            if (teamsDraw != null)
            {
                harmony.Patch(
                    teamsDraw,
                    prefix: new HarmonyMethod(typeof(MenuTidy), nameof(TeamsOn)),
                    postfix: new HarmonyMethod(typeof(MenuTidy), nameof(TeamsOff)));
            }

            var teleport = Type.GetType("RoRCheatPanel.UI.UiHelpers, RoRCheatPanel")
                ?.GetMethod("DrawPlayerTeleport", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            if (teleport != null)
                harmony.Patch(teleport, prefix: new HarmonyMethod(typeof(MenuTidy), nameof(SkipTeamsTeleport)));
        }

        private static void TeamsOn()
        {
            _teams = true;
        }

        private static void TeamsOff()
        {
            _teams = false;
        }

        private static bool SkipTeamsTeleport()
        {
            return !_teams;
        }

        [HarmonyPriority(Priority.First)]
        private static bool HidePlain(string text, ref bool __result)
        {
            if (!Drop(text))
                return true;
            __result = false;
            return false;
        }

        [HarmonyPriority(Priority.First)]
        private static bool HideStyled(string text, GUIStyle style, ref bool __result)
        {
            if (!Drop(text))
                return true;
            __result = false;
            return false;
        }

        private static bool Drop(string text)
        {
            return MenuFilter.Drop(text, Time.frameCount, ref _frame, Seen);
        }

        private static bool DrawFun()
        {
            _scroll = GUILayout.BeginScrollView(_scroll, GUILayout.Height(FitScroll()));
            Call("RoRCheatPanel.UI.UiHelpers", "DrawPlayerPicker");
            GUILayout.Label(L("fun_help"));

            Box(Tr("Камера", "Camera", "Cámara"), () =>
            {
                GUILayout.BeginHorizontal();
                var face = ChannelPlugin.FirstPerson
                    ? Tr("От первого лица: вкл", "First person: on", "Primera persona: sí")
                    : Tr("От первого лица", "First person", "Primera persona");
                if (GUILayout.Button(face, GUILayout.Width(180f), GUILayout.Height(28f)))
                    ChannelPlugin.FirstPerson = !ChannelPlugin.FirstPerson;
                Toggle("fun_disco", "Disco", 150f);
                if (GUILayout.Button(L("fun_cam_reset"), GUILayout.Width(190f), GUILayout.Height(28f)))
                    Call("RoRCheatPanel.CheatRuntime", "RestoreCamera");
                GUILayout.EndHorizontal();
                Slider("fun_fov", "Fov", 30f, 140f, 60f, "60", false);
                Slider("fun_tpv_dist", "ThirdPersonDistance", 0f, 30f, 0f, "0", true);
            });

            Box(Tr("Тело", "Body", "Cuerpo"), () =>
            {
                GUILayout.BeginHorizontal();
                ScaleButton("fun_tiny", 0.35f);
                ScaleButton("fun_normal", 1f);
                ScaleButton("fun_giant", 3.2f);
                ScaleButton("fun_mega", 6f);
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
                var scale = ReadFloat("BodyScale", 1f);
                GUILayout.Label(string.Format(L("fun_scale"), scale.ToString("0.00")), GUILayout.Width(160f));
                Write("BodyScale", GUILayout.HorizontalSlider(scale, 0.2f, 8f));
                GUILayout.EndHorizontal();
                GUILayout.Label(Tr(
                    "Другие игроки этот ползунок не видят.",
                    "Other players do not see this slider.",
                    "Los demás no ven este control."));
                GUILayout.BeginHorizontal();
                if (GUILayout.Button(Tr("Большое тело", "Large body", "Cuerpo grande"), GUILayout.Height(28f)))
                    HostVanilla.LargeBody();
                if (GUILayout.Button(Tr("Маленькое тело", "Small body", "Cuerpo pequeño"), GUILayout.Height(28f)))
                    HostVanilla.SmallBody();
                if (GUILayout.Button(Tr("Вернуть выжившего", "Restore survivor", "Volver al superviviente"), GUILayout.Height(28f)))
                    HostVanilla.RestoreSurvivor();
                GUILayout.EndHorizontal();
                GUILayout.Label(Tr(
                    "Эти кнопки меняют тело через игру, поэтому их видят все.",
                    "These buttons change the body through the game, so everyone sees them.",
                    "Estos botones cambian el cuerpo por el juego, así que todos los ven."));
                Toggle("fun_lowgrav", "LowGravity", 200f);
            });

            Box(Tr("Хаос", "Chaos", "Caos"), () =>
            {
                GUILayout.BeginHorizontal();
                Act("fun_rand_item", "GiveRandomItem", 1);
                Act("fun_rand_item10", "GiveRandomItem", 10);
                Act("fun_rand_equip", "GiveRandomEquipment");
                Act("fun_rand_survivor", "RandomSurvivor");
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
                Act("fun_rain", "RainPickups", 24);
                Act("fun_chests", "RainChests", 10);
                Act("fun_gold", "GoldExplosion");
                Act("fun_yeet_btn", "LaunchUp", 55f);
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
                Act("fun_kick", "FunnyKick", false);
                Act("fun_artifact", "ToggleRandomArtifact");
                Act("fun_boss", "BecomeBoss");
                GUILayout.EndHorizontal();
                HostVanilla.DrawChaos();
                FunExtras.DrawExtras();
            });

            Box(Tr("Ник", "Name", "Nombre"), () =>
            {
                GUILayout.BeginHorizontal();
                _nick = GUILayout.TextField(_nick ?? "", GUILayout.MinWidth(180f), GUILayout.Height(24f));
                if (GUILayout.Button(L("fun_nick_set"), GUILayout.Width(140f), GUILayout.Height(28f)))
                    Call("RoRCheatPanel.GameUtil", "SetPlayerNick", _nick);
                if (GUILayout.Button(L("fun_nick_boss"), GUILayout.Width(70f), GUILayout.Height(28f)))
                {
                    _nick = L("fun_nick_boss");
                    Call("RoRCheatPanel.GameUtil", "SetPlayerNick", _nick);
                }

                if (GUILayout.Button(L("fun_nick_moon"), GUILayout.Width(120f), GUILayout.Height(28f)))
                {
                    _nick = L("fun_nick_moon");
                    Call("RoRCheatPanel.GameUtil", "SetPlayerNick", _nick);
                }

                GUILayout.EndHorizontal();
            });

            GUILayout.Space(16f);
            GUILayout.EndScrollView();
            return false;
        }

        private static float FitScroll()
        {
            var window = Type.GetType("RoRCheatPanel.UI.CheatWindow, RoRCheatPanel");
            var panel = window?.GetField("_rect", BindingFlags.NonPublic | BindingFlags.Static)?.GetValue(null) is Rect area
                ? area.height
                : 720f;
            if (Event.current != null && Event.current.type == EventType.Repaint)
            {
                var last = GUILayoutUtility.GetLastRect();
                if (last.yMax > 24f && panel > last.yMax + 80f)
                    _scrollHeight = Mathf.Clamp(panel - last.yMax - 18f, 180f, panel - 36f);
            }

            return _scrollHeight;
        }

        private static void Box(string title, Action body)
        {
            GUILayout.BeginVertical(GUI.skin.box);
            GUILayout.Label(title);
            body();
            GUILayout.EndVertical();
        }

        private static void ScaleButton(string key, float scale)
        {
            if (GUILayout.Button(L(key), GUILayout.Width(110f), GUILayout.Height(28f)))
            {
                Write("BodyScale", scale);
                ChannelPlugin.AcceptDistance(CameraFraming.DistanceForBody(scale));
            }
        }

        private static void Toggle(string key, string field, float width)
        {
            var value = ReadBool(field);
            var next = ToggleButton(L(key), value, width);
            if (next != value)
                Write(field, next);
        }

        private static void Slider(string key, string field, float min, float max, float reset, string resetLabel, bool enablesThirdPerson)
        {
            var value = ReadFloat(field, reset);
            GUILayout.BeginHorizontal();
            var caption = string.Format(L(key), field == "Fov" ? value.ToString("0") : value.ToString("0.0"));
            var tip = field == "Fov"
                ? Tr("Поле зрения. Меньше — уже картинка, больше — шире.", "Field of view. Lower is narrower, higher is wider.", "Campo de visión. Menos es más estrecho, más es más ancho.")
                : Tr("Насколько камера отъезжает от персонажа. 0 — как обычно.", "How far the camera sits from the character. 0 is the normal view.", "Cuánto se aleja la cámara del personaje. 0 es la vista normal.");
            GUILayout.Label(new GUIContent(caption, tip), GUILayout.Width(170f));
            var next = GUILayout.HorizontalSlider(value, min, max);
            if (GUILayout.Button(resetLabel, GUILayout.Width(40f)))
                next = reset;
            GUILayout.EndHorizontal();
            if (enablesThirdPerson && Mathf.Abs(next - value) > 0.01f)
                ChannelPlugin.TouchDistance();
            Write(field, next);
        }

        private static void Act(string key, string method, object arg)
        {
            if (!GUILayout.Button(L(key), GUILayout.Height(28f)))
                return;
            Call("RoRCheatPanel.GameUtil", method, arg);
        }

        private static void Act(string key, string method)
        {
            if (!GUILayout.Button(L(key), GUILayout.Height(28f)))
                return;
            Call("RoRCheatPanel.GameUtil", method);
        }

        private static bool ToggleButton(string label, bool value, float width)
        {
            var helpers = Type.GetType("RoRCheatPanel.UI.UiHelpers, RoRCheatPanel");
            if (helpers == null)
                return GUILayout.Toggle(value, label);
            foreach (var method in helpers.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static))
            {
                if (method.Name != "ToggleButton")
                    continue;
                var parameters = method.GetParameters();
                if (parameters.Length == 3
                    && parameters[0].ParameterType == typeof(string)
                    && parameters[1].ParameterType == typeof(bool)
                    && parameters[2].ParameterType == typeof(float))
                {
                    var result = method.Invoke(null, new object[] { label, value, width });
                    return result is bool flag && flag;
                }
            }

            return value;
        }

        private static void Call(string typeName, string method, object arg)
        {
            var type = Type.GetType(typeName + ", RoRCheatPanel");
            foreach (var info in type != null ? type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static) : Array.Empty<MethodInfo>())
            {
                if (info.Name != method)
                    continue;
                var parameters = info.GetParameters();
                if (parameters.Length != 1)
                    continue;
                info.Invoke(null, new[] { ConvertArg(arg, parameters[0].ParameterType) });
                return;
            }
        }

        private static void Call(string typeName, string method)
        {
            var type = Type.GetType(typeName + ", RoRCheatPanel");
            var info = type?.GetMethod(method, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static, null, Type.EmptyTypes, null);
            info?.Invoke(null, null);
        }

        private static object ConvertArg(object arg, Type type)
        {
            if (arg == null || type.IsInstanceOfType(arg))
                return arg;
            return Convert.ChangeType(arg, type);
        }

        private static bool ReadBool(string field)
        {
            var value = Field(field)?.GetValue(null);
            return value is bool flag && flag;
        }

        private static float ReadFloat(string field, float fallback)
        {
            var value = Field(field)?.GetValue(null);
            return value is float number ? number : fallback;
        }

        private static void Write(string field, object value)
        {
            var info = Field(field);
            if (info == null)
                return;
            if (info.FieldType == typeof(float) && value is float number)
                info.SetValue(null, number);
            else if (info.FieldType == typeof(bool) && value is bool flag)
                info.SetValue(null, flag);
        }

        private static FieldInfo Field(string name)
        {
            var type = Type.GetType("RoRCheatPanel.CheatRuntime, RoRCheatPanel");
            return type?.GetField(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
        }

        private static string L(string key)
        {
            var loc = Type.GetType("RoRCheatPanel.Loc, RoRCheatPanel");
            var text = loc?.GetMethod("T", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static, null, new[] { typeof(string) }, null)
                ?.Invoke(null, new object[] { key }) as string;
            return string.IsNullOrEmpty(text) ? key : text;
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
