using System;
using System.Reflection;
using RoR2;
using UnityEngine;

namespace RoRCheatPanel.Channel
{
    internal static class SurvivorGrid
    {
        private static GUIStyle _card;

        public static void Install(HarmonyLib.Harmony harmony)
        {
            var tab = Type.GetType("RoRCheatPanel.UI.Tabs.RunTab, RoRCheatPanel");
            var draw = tab != null
                ? tab.GetMethod("DrawSurvivors", BindingFlags.NonPublic | BindingFlags.Static)
                : null;
            if (draw == null)
            {
                Debug.LogWarning("[RoRCheatPanel.Channel] missing RunTab.DrawSurvivors");
                return;
            }

            harmony.Patch(draw, prefix: new HarmonyLib.HarmonyMethod(typeof(SurvivorGrid), nameof(Draw)));
        }

        private static bool Draw()
        {
            GUILayout.Label(L("change_survivor") + "  →  " + L("tab_chars"));
            var helpers = Type.GetType("RoRCheatPanel.UI.UiHelpers, RoRCheatPanel");
            helpers?.GetMethod("DrawPlayerPicker", BindingFlags.Public | BindingFlags.Static)?.Invoke(null, null);

            EnsureStyle();
            GUILayout.BeginHorizontal();
            var count = 0;
            var defs = SurvivorCatalog.allSurvivorDefs;
            if (defs != null)
            {
                foreach (var survivor in defs)
                {
                    if (survivor == null || survivor.hidden)
                        continue;
                    var name = Display(survivor);
                    var tip = Describe(survivor);
                    var icon = Portrait(survivor);
                    var content = new GUIContent(name, icon, tip);
                    if (GUILayout.Button(content, _card, GUILayout.Width(86f), GUILayout.Height(68f)))
                        Transform(survivor);
                    count++;
                    if (count % 8 == 0)
                    {
                        GUILayout.EndHorizontal();
                        GUILayout.BeginHorizontal();
                    }
                }
            }

            GUILayout.EndHorizontal();
            return false;
        }

        private static void EnsureStyle()
        {
            if (_card != null)
                return;
            _card = new GUIStyle(GUI.skin.button)
            {
                imagePosition = ImagePosition.ImageAbove,
                alignment = TextAnchor.LowerCenter,
                wordWrap = true,
                fontSize = 10,
                padding = new RectOffset(2, 2, 3, 2)
            };
        }

        private static Texture Portrait(SurvivorDef survivor)
        {
            var prefab = survivor != null ? survivor.bodyPrefab : null;
            var body = prefab != null ? prefab.GetComponent<CharacterBody>() : null;
            return body != null ? body.portraitIcon : null;
        }

        private static string Display(SurvivorDef survivor)
        {
            var text = Call("DisplayName", survivor) as string;
            if (!string.IsNullOrEmpty(text))
                return text;
            return survivor != null ? survivor.cachedName : "?";
        }

        private static string Describe(SurvivorDef survivor)
        {
            return Call("Description", survivor) as string ?? "";
        }

        private static void Transform(SurvivorDef survivor)
        {
            var util = Type.GetType("RoRCheatPanel.GameUtil, RoRCheatPanel");
            util?.GetMethod("TransformSurvivor", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)
                ?.Invoke(null, new object[] { survivor });
        }

        private static object Call(string method, SurvivorDef survivor)
        {
            var util = Type.GetType("RoRCheatPanel.GameUtil, RoRCheatPanel");
            var info = util != null
                ? util.GetMethod(method, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static, null, new[] { typeof(SurvivorDef) }, null)
                : null;
            return info != null ? info.Invoke(null, new object[] { survivor }) : null;
        }

        private static string L(string key)
        {
            var loc = Type.GetType("RoRCheatPanel.Loc, RoRCheatPanel");
            var method = loc != null
                ? loc.GetMethod("T", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static, null, new[] { typeof(string) }, null)
                : null;
            var text = method != null ? method.Invoke(null, new object[] { key }) as string : null;
            return string.IsNullOrEmpty(text) ? key : text;
        }
    }
}
