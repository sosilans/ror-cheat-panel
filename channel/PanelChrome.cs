using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using UnityEngine;

namespace RoRCheatPanel.Channel
{
    internal static class PanelChrome
    {
        private static readonly string[] HotkeyFields =
        {
            "OpenKey",
            "OpenKeyAlt",
            "ScanKey",
            "GummyKey",
            "DronesKey",
            "ArmyKey",
            "GodModeKey",
            "NoclipKey",
            "ChargeTeleporterKey"
        };

        private static readonly string[] HotkeyLabels =
        {
            "hk_open",
            "hk_open",
            "hk_scan",
            "hk_gummy",
            "hk_drones",
            "hk_army",
            "hk_god",
            "hk_noclip",
            "hk_charge"
        };

        private static string _capture;
        internal static bool HotkeysReplaced;

        internal static bool Capturing => _capture != null;

        public static void Install(Harmony harmony)
        {
            var window = Type.GetType("RoRCheatPanel.UI.CheatWindow, RoRCheatPanel");
            var draw = window?.GetMethod("Draw", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            var onGui = window?.GetMethod("OnGUI", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            if (draw != null)
                harmony.Patch(
                    draw,
                    transpiler: new HarmonyMethod(typeof(PanelChrome), nameof(ShrinkDrag)),
                    postfix: new HarmonyMethod(typeof(PanelChrome), nameof(AfterDraw)));
            if (onGui != null)
                harmony.Patch(onGui, postfix: new HarmonyMethod(typeof(PanelChrome), nameof(AfterPanelGui)));

            var about = Type.GetType("RoRCheatPanel.UI.Tabs.AboutTab, RoRCheatPanel");
            var aboutDraw = about?.GetMethod("Draw", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            if (aboutDraw != null)
                harmony.Patch(aboutDraw, transpiler: new HarmonyMethod(typeof(PanelChrome), nameof(SwapHotkeyBox)));

            var plugin = Type.GetType("RoRCheatPanel.Plugin, RoRCheatPanel");
            var wasPressed = plugin?.GetMethod("WasPressed", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            if (wasPressed != null)
                harmony.Patch(wasPressed, prefix: new HarmonyMethod(typeof(PanelChrome), nameof(BlockWhileCapturing)));
        }

        private static bool BlockWhileCapturing(ref bool __result)
        {
            if (_capture == null)
                return true;
            __result = false;
            return false;
        }

        private static void AfterDraw()
        {
            var width = ReadRect().width;
            var button = new Rect(width - 28f, 3f, 22f, 18f);
            var current = Event.current;
            var hit = current != null
                && current.button == 0
                && (current.rawType == EventType.MouseUp || current.type == EventType.MouseUp)
                && button.Contains(current.mousePosition);
            GUI.Button(button, "_");
            if (!hit)
                return;

            current.Use();
            _capture = null;
            if (WindowOpened())
                Toggle();
        }

        private static void DragTitleBar()
        {
            var width = ReadRect().width;
            GUI.DragWindow(new Rect(0f, 0f, Mathf.Max(40f, width - 34f), 22f));
        }

        private static IEnumerable<CodeInstruction> ShrinkDrag(IEnumerable<CodeInstruction> instructions)
        {
            var drag = AccessTools.Method(typeof(GUI), "DragWindow", new[] { typeof(Rect) });
            var ours = AccessTools.Method(typeof(PanelChrome), nameof(DragTitleBar));
            foreach (var instruction in instructions)
            {
                if (drag != null && instruction.Calls(drag))
                {
                    yield return new CodeInstruction(OpCodes.Pop);
                    yield return new CodeInstruction(OpCodes.Call, ours);
                    continue;
                }

                yield return instruction;
            }
        }

        private static void AfterPanelGui()
        {
            if (!WindowOpened())
                _capture = null;
        }

        public static void DrawHotkeys()
        {
            ConsumeCapture();
            GUILayout.BeginVertical(GUI.skin.box);
            GUILayout.Label(Tr("Клавиши", "Hotkeys", "Teclas"));
            GUILayout.Label(Tr(
                "Нажми «Клавиша» и новую комбинацию. Запись сама попадает в настройки, файл открывать не нужно.",
                "Press Key, then the new shortcut. It is saved for you. No need to open the config file.",
                "Pulsa Tecla y la nueva combinación. Se guarda sola, no hace falta abrir el archivo."));
            for (var i = 0; i < HotkeyFields.Length; i++)
                DrawRow(HotkeyFields[i], RowLabel(i));
            GUILayout.EndVertical();
        }

        private static string RowLabel(int index)
        {
            var text = L(HotkeyLabels[index]);
            if (HotkeyFields[index] == "OpenKeyAlt")
                text += " 2";
            return text;
        }

        private static void DrawRow(string field, string label)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(label, GUILayout.Width(220f));
            var binding = _capture == field
                ? Tr("жми клавишу...", "press a key...", "pulsa una tecla...")
                : BindText(field);
            GUILayout.Label(binding, GUILayout.Width(200f));
            if (GUILayout.Button(Tr("Клавиша", "Key", "Tecla"), GUILayout.Width(90f), GUILayout.Height(22f)))
                _capture = field;
            if (GUILayout.Button(Tr("Сброс", "Clear", "Borrar"), GUILayout.Width(70f), GUILayout.Height(22f)))
            {
                if (_capture == field)
                    _capture = null;
                Assign(field, KeyCode.None, Array.Empty<KeyCode>());
            }

            GUILayout.EndHorizontal();
        }

        private static void ConsumeCapture()
        {
            if (_capture == null)
                return;
            var current = Event.current;
            if (current == null)
                return;
            if (current.type == EventType.KeyDown)
            {
                if (current.keyCode == KeyCode.Escape || current.keyCode == KeyCode.None)
                {
                    if (current.keyCode == KeyCode.Escape)
                        _capture = null;
                    current.Use();
                    return;
                }

                if (IsModifier(current.keyCode))
                    return;
                var mods = new List<KeyCode>();
                if (current.control)
                    mods.Add(KeyCode.LeftControl);
                if (current.shift)
                    mods.Add(KeyCode.LeftShift);
                if (current.alt)
                    mods.Add(KeyCode.LeftAlt);
                Assign(_capture, current.keyCode, mods.ToArray());
                _capture = null;
                current.Use();
                return;
            }

            if (current.type == EventType.MouseDown && current.button > 0)
            {
                var key = current.button == 1 ? KeyCode.Mouse1 : KeyCode.Mouse2;
                Assign(_capture, key, Array.Empty<KeyCode>());
                _capture = null;
                current.Use();
            }
        }

        private static bool IsModifier(KeyCode key)
        {
            return key == KeyCode.LeftControl || key == KeyCode.RightControl
                || key == KeyCode.LeftShift || key == KeyCode.RightShift
                || key == KeyCode.LeftAlt || key == KeyCode.RightAlt
                || key == KeyCode.LeftCommand || key == KeyCode.RightCommand;
        }

        private static void Assign(string fieldName, KeyCode key, KeyCode[] mods)
        {
            var entry = Entry(fieldName);
            var value = entry?.GetType().GetProperty("Value");
            var current = value?.GetValue(entry);
            if (current == null)
                return;
            var ctor = current.GetType().GetConstructor(new[] { typeof(KeyCode), typeof(KeyCode[]) });
            if (ctor == null)
                return;
            value.SetValue(entry, ctor.Invoke(new object[] { key, mods ?? Array.Empty<KeyCode>() }));
        }

        private static string BindText(string fieldName)
        {
            var entry = Entry(fieldName);
            var current = entry?.GetType().GetProperty("Value")?.GetValue(entry);
            if (current == null)
                return "—";
            var main = current.GetType().GetProperty("MainKey")?.GetValue(current);
            if (main is KeyCode code && code == KeyCode.None)
                return Tr("нет", "none", "ninguna");
            return current.ToString();
        }

        private static object Entry(string fieldName)
        {
            var config = Type.GetType("RoRCheatPanel.CheatConfig, RoRCheatPanel");
            return config?.GetField(fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)?.GetValue(null);
        }

        private static IEnumerable<CodeInstruction> SwapHotkeyBox(IEnumerable<CodeInstruction> instructions)
        {
            var list = new List<CodeInstruction>(instructions);
            var marker = -1;
            for (var i = 0; i < list.Count; i++)
            {
                if (list[i].opcode == OpCodes.Ldstr && list[i].operand as string == "hotkeys")
                {
                    marker = i;
                    break;
                }
            }

            if (marker < 0)
            {
                Debug.LogWarning("[RoRCheatPanel.Channel] hotkey block not found");
                return list;
            }

            var call = marker;
            for (var i = marker; i >= 0; i--)
            {
                if (IsCall(list[i], "BeginVertical"))
                {
                    call = i;
                    break;
                }
            }

            var start = call;
            for (var i = call - 1; i >= Math.Max(0, call - 6); i--)
            {
                start = i;
                if (list[i].opcode == OpCodes.Ldstr)
                    break;
            }

            var depth = 0;
            var finish = -1;
            for (var i = call; i < list.Count; i++)
            {
                if (IsCall(list[i], "BeginVertical"))
                    depth++;
                if (IsCall(list[i], "EndVertical"))
                {
                    depth--;
                    if (depth == 0)
                    {
                        finish = i;
                        break;
                    }
                }
            }

            if (finish < start)
                return list;

            var draw = new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(PanelChrome), nameof(DrawHotkeys)));
            list.RemoveRange(start, finish - start + 1);
            list.Insert(start, draw);
            HotkeysReplaced = true;
            return list;
        }

        private static bool IsCall(CodeInstruction instruction, string name)
        {
            return instruction.operand is MethodInfo method && method.Name == name;
        }

        private static void Toggle()
        {
            var window = Type.GetType("RoRCheatPanel.UI.CheatWindow, RoRCheatPanel");
            window?.GetMethod("Toggle", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)?.Invoke(null, null);
        }

        private static bool WindowOpened()
        {
            var window = Type.GetType("RoRCheatPanel.UI.CheatWindow, RoRCheatPanel");
            var opened = window?.GetProperty("Opened", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)?.GetValue(null);
            return opened is bool flag && flag;
        }

        private static Rect ReadRect()
        {
            var window = Type.GetType("RoRCheatPanel.UI.CheatWindow, RoRCheatPanel");
            var field = window?.GetField("_rect", BindingFlags.NonPublic | BindingFlags.Static);
            return field?.GetValue(null) is Rect rect ? rect : new Rect(48f, 48f, 1140f, 760f);
        }

        private static string L(string key)
        {
            var loc = Type.GetType("RoRCheatPanel.Loc, RoRCheatPanel");
            var text = loc?.GetMethod("T", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)?.Invoke(null, new object[] { key }) as string;
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
