using System;
using System.Reflection;
using HarmonyLib;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace RoRCheatPanel.Channel
{
    internal static class LobbySize
    {
        private const string PrefKey = "RoRCheatPanel.LobbySize";
        private const int VanillaHardCap = 16;

        private static int _count = 4;
        private static bool _drawn;
        private static bool _bootDone;
        private static bool _waitSlots;

        public static void Install(Harmony harmony)
        {
            var draw = Type.GetType("RoRCheatPanel.UI.Tabs.ToolsTab, RoRCheatPanel")
                ?.GetMethod("Draw", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            if (draw == null)
            {
                Debug.LogWarning("[RoRCheatPanel.Channel] missing ToolsTab.Draw");
                return;
            }

            harmony.Patch(draw, prefix: new HarmonyMethod(typeof(LobbySize), nameof(DrawBox)));

            var server = typeof(RoR2.Networking.NetworkManagerSystem).GetMethod(
                "OnStartServer",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (server != null)
                harmony.Patch(server, postfix: new HarmonyMethod(typeof(LobbySize), nameof(OnServerStarted)));
        }

        private static void OnServerStarted()
        {
            WriteCaps();
            if (NetworkManager.singleton != null)
                NetworkManager.singleton.maxConnections = _count;
        }

        public static void Tick()
        {
            if (!_bootDone && PlayerPrefs.HasKey(PrefKey))
            {
                _count = LobbyRules.Clamp(PlayerPrefs.GetInt(PrefKey, 4));
                _bootDone = true;
                _waitSlots = true;
                WriteCaps();
            }

            if (_waitSlots)
                _waitSlots = !WriteSlots();
        }

        private static void DrawBox()
        {
            if (!_drawn)
            {
                _drawn = true;
                if (PlayerPrefs.HasKey(PrefKey))
                    _count = LobbyRules.Clamp(PlayerPrefs.GetInt(PrefKey, 4));
                else
                    _count = LobbyRules.Clamp(ReadInt(typeof(RoR2Application), "maxPlayers", 4));
            }

            GUILayout.BeginVertical(GUI.skin.box);
            GUILayout.Label(Tr("Лобби", "Lobby", "Sala"));
            var wrap = new GUIStyle(GUI.skin.label) { wordWrap = true };
            GUILayout.Label(Tr(
                "Панель в главном меню открывается той же клавишей (Insert или Ctrl+F10). Число ставь до кнопки создания сервера. Точки появления и слоты лобби берутся отсюда.",
                "The panel opens in the main menu with the same key (Insert or Ctrl+F10). Set the number before you create the server. Spawn points and lobby slots follow it.",
                "El panel se abre en el menú principal con la misma tecla (Insert o Ctrl+F10). Pon el número antes de crear el servidor. Los puntos de aparición y los huecos de la sala salen de aquí."), wrap, GUILayout.MaxWidth(520f));
            GUILayout.BeginHorizontal();
            GUILayout.Label(Tr("Игроков ", "Players ", "Jugadores ") + _count, GUILayout.Width(120f));
            if (GUILayout.Button("-", GUILayout.Width(28f), GUILayout.Height(24f)))
                _count = Mathf.Max(2, _count - 1);
            if (GUILayout.Button("+", GUILayout.Width(28f), GUILayout.Height(24f)))
                _count = Mathf.Min(64, _count + 1);
            if (GUILayout.Button(Tr("На 4", "Set 4", "A 4"), GUILayout.Width(64f), GUILayout.Height(24f)))
                _count = 4;
            if (GUILayout.Button(Tr("На 8", "Set 8", "A 8"), GUILayout.Width(64f), GUILayout.Height(24f)))
                _count = 8;
            if (GUILayout.Button(Tr("На 16", "Set 16", "A 16"), GUILayout.Width(72f), GUILayout.Height(24f)))
                _count = 16;
            if (GUILayout.Button(Tr("Записать лобби", "Apply lobby", "Guardar sala"), GUILayout.Height(24f)))
                Commit(true);
            GUILayout.EndHorizontal();
            GUILayout.Label(Status());
            GUILayout.EndVertical();
        }

        private static void Commit(bool toast)
        {
            _count = LobbyRules.Clamp(_count);
            PlayerPrefs.SetInt(PrefKey, _count);
            _bootDone = true;
            WriteCaps();
            var slots = WriteSlots();
            _waitSlots = !slots;
            if (!toast)
                return;
            if (NetworkServer.active)
            {
                Toast(Tr(
                    "Лобби на " + _count + ". Уже запущенный сервер слоты не меняет — выйди и создай заново.",
                    "Lobby set to " + _count + ". A server that is already running keeps its old slots. Leave and create it again.",
                    "Sala en " + _count + ". Un servidor ya abierto no cambia los huecos. Sal y créalo de nuevo."));
                return;
            }

            Toast(Tr(
                "Лобби на " + _count + ". Создавай сервер после этого.",
                "Lobby set to " + _count + ". Create the server after this.",
                "Sala en " + _count + ". Crea el servidor después."));
        }

        private static void WriteCaps()
        {
            var flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;
            typeof(RoR2Application).GetField("maxPlayers", flags)?.SetValue(null, _count);
            typeof(RoR2Application).GetField("hardMaxPlayers", flags)?.SetValue(null, Math.Max(VanillaHardCap, _count));
            var lobby = Type.GetType("RoR2.LobbyManager, RoR2");
            var field = lobby?.GetField("cvSteamLobbyMaxMembers", flags);
            var convar = field?.GetValue(null);
            convar?.GetType().GetProperty("value", BindingFlags.Public | BindingFlags.Instance)?.SetValue(convar, _count);
        }

        private static bool WriteSlots()
        {
            if (NetworkManager.singleton == null)
                return false;
            NetworkManager.singleton.maxConnections = _count;
            return true;
        }

        private static string Status()
        {
            var lobby = ReadInt(typeof(RoR2Application), "maxPlayers", 4);
            var ceiling = ReadInt(typeof(RoR2Application), "hardMaxPlayers", VanillaHardCap);
            var slots = NetworkManager.singleton != null ? NetworkManager.singleton.maxConnections : 0;
            return Tr(
                "Сейчас: лобби " + lobby + ", потолок " + ceiling + ", слоты сервера " + slots + ".",
                "Now: lobby " + lobby + ", ceiling " + ceiling + ", server slots " + slots + ".",
                "Ahora: sala " + lobby + ", tope " + ceiling + ", huecos del servidor " + slots + ".");
        }

        private static int ReadInt(Type type, string name, int fallback)
        {
            var field = type.GetField(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            return field != null && field.GetValue(null) is int value ? value : fallback;
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
