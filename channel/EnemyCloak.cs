using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using RoR2;
using RoR2.CharacterAI;
using UnityEngine;
using UnityEngine.Networking;

namespace RoRCheatPanel.Channel
{
    internal static class EnemyCloak
    {
        private static readonly HashSet<CharacterBody> Applied = new HashSet<CharacterBody>();
        private static readonly List<CharacterBody> Drop = new List<CharacterBody>();
        private static readonly FieldInfo CurrentEnemy = AccessTools.Field(typeof(BaseAI), "<currentEnemy>k__BackingField");
        private static readonly FieldInfo CustomEnemy = AccessTools.Field(typeof(BaseAI), "<customTarget>k__BackingField");
        private static BepInEx.Configuration.ConfigEntry<bool> _enabled;
        private static MethodInfo _targets;
        private static MethodInfo _toggle;

        public static void Install(Harmony harmony, BepInEx.Configuration.ConfigFile file)
        {
            _enabled = file.Bind("Cheats", "Unseen", false, "Hostile monsters do not target the selected players.");
            var util = Type.GetType("RoRCheatPanel.GameUtil, RoRCheatPanel");
            _targets = util?.GetMethod("Targets", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            var helpers = Type.GetType("RoRCheatPanel.UI.UiHelpers, RoRCheatPanel");
            _toggle = helpers?.GetMethod(
                "ToggleButton",
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static,
                null,
                new[] { typeof(string), typeof(bool), typeof(float) },
                null);

            var tools = Type.GetType("RoRCheatPanel.UI.Tabs.ToolsTab, RoRCheatPanel");
            var draw = tools?.GetMethod("DrawToggles", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            if (draw != null)
                harmony.Patch(draw, postfix: new HarmonyMethod(typeof(EnemyCloak), nameof(DrawToggle)));

            var search = AccessTools.Method(typeof(BullseyeSearch), nameof(BullseyeSearch.GetResults));
            if (search != null)
                harmony.Patch(search, postfix: new HarmonyMethod(typeof(EnemyCloak), nameof(HideFromSearch)));
        }

        public static void Tick()
        {
            if (!NetworkServer.active)
                return;

            var want = new HashSet<CharacterBody>();
            if (_enabled != null && _enabled.Value)
            {
                foreach (var user in Users(_targets))
                {
                    var body = user != null ? user.GetCurrentBody() : null;
                    if (body == null)
                        continue;
                    want.Add(body);
                    Ensure(body);
                }
            }

            Drop.Clear();
            foreach (var body in Applied)
            {
                if (body == null || !want.Contains(body))
                    Drop.Add(body);
            }

            for (var i = 0; i < Drop.Count; i++)
                Release(Drop[i]);

            if (want.Count > 0)
                ReleaseAggro(want);
        }

        private static void DrawToggle()
        {
            var on = _enabled != null && _enabled.Value;
            GUILayout.BeginHorizontal("box");
            var next = on;
            if (_toggle != null && _toggle.Invoke(null, new object[] { Label(), on, 190f }) is bool pressed)
                next = pressed;
            GUILayout.Label(Help());
            GUILayout.EndHorizontal();
            if (_enabled != null && next != on)
            {
                _enabled.Value = next;
                Tick();
            }
        }

        private static void Ensure(CharacterBody body)
        {
            var cloak = RoR2Content.Buffs.Cloak;
            if (cloak == null || body == null)
                return;
            if (!Applied.Contains(body) || !body.HasBuff(cloak))
                body.AddBuff(cloak);
            Applied.Add(body);
        }

        private static void Release(CharacterBody body)
        {
            Applied.Remove(body);
            var cloak = RoR2Content.Buffs.Cloak;
            if (body != null && cloak != null && body.HasBuff(cloak))
                body.RemoveBuff(cloak);
        }

        private static void ReleaseAggro(HashSet<CharacterBody> hidden)
        {
            var ais = InstanceTracker.GetInstancesList<BaseAI>();
            if (ais == null)
                return;
            for (var i = 0; i < ais.Count; i++)
            {
                var ai = ais[i];
                if (ai == null)
                    continue;
                Clear(ai, hidden, true);
                Clear(ai, hidden, false);
            }
        }

        private static void Clear(BaseAI ai, HashSet<CharacterBody> hidden, bool custom)
        {
            var field = custom ? CustomEnemy : CurrentEnemy;
            if (field == null || !(field.GetValue(ai) is BaseAI.Target target))
                return;
            var body = target.characterBody;
            if (body == null || !hidden.Contains(body))
                return;
            var viewer = ai.body;
            if (viewer != null && viewer.teamComponent != null && body.teamComponent != null
                && !TeamManager.IsTeamEnemy(viewer.teamComponent.teamIndex, body.teamComponent.teamIndex))
                return;
            target.Reset();
            field.SetValue(ai, target);
            if (!custom)
                ai.enemyAttention = 0f;
        }

        private static void HideFromSearch(BullseyeSearch __instance, ref IEnumerable<HurtBox> __result)
        {
            if (__result == null || Applied.Count == 0)
                return;
            var viewer = __instance != null ? __instance.viewer : null;
            var list = new List<HurtBox>();
            foreach (var box in __result)
            {
                if (box == null || !HiddenFrom(box, viewer))
                    list.Add(box);
            }

            __result = list;
        }

        private static bool HiddenFrom(HurtBox box, CharacterBody viewer)
        {
            var health = box.healthComponent;
            var body = health != null ? health.body : null;
            if (body == null || !Applied.Contains(body))
                return false;
            if (viewer == null || viewer.teamComponent == null || body.teamComponent == null)
                return true;
            return TeamManager.IsTeamEnemy(viewer.teamComponent.teamIndex, body.teamComponent.teamIndex);
        }

        private static IEnumerable<NetworkUser> Users(MethodInfo method)
        {
            if (method == null || !(method.Invoke(null, null) is System.Collections.IEnumerable items))
                yield break;
            foreach (var item in items)
            {
                if (item is NetworkUser user && user != null)
                    yield return user;
            }
        }

        private static string Label()
        {
            switch (Lang())
            {
                case "En":
                    return "Unseen";
                case "Es":
                    return "Invisible";
                default:
                    return "Невидимость";
            }
        }

        private static string Help()
        {
            switch (Lang())
            {
                case "En":
                    return "Enemies will not target you.";
                case "Es":
                    return "Los enemigos no te eligen.";
                default:
                    return "Враги не выбирают тебя целью.";
            }
        }

        private static string Lang()
        {
            var loc = Type.GetType("RoRCheatPanel.Loc, RoRCheatPanel");
            var current = loc != null
                ? loc.GetProperty("Current", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)?.GetValue(null)
                  ?? loc.GetField("Current", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)?.GetValue(null)
                : null;
            return current != null ? current.ToString() : "Ru";
        }
    }
}
