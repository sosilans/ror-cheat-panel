using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using RoR2;
using RoR2.Skills;
using UnityEngine;
using UnityEngine.Networking;

namespace RoRCheatPanel.Channel
{
    internal static class ListFavorites
    {
        private const string Pref = "RoRCheatPanel.ListFavs";
        private static readonly HashSet<string> Favs = new HashSet<string>();
        private static bool _loaded;
        private static GUIStyle _name;

        public static void Install(Harmony harmony)
        {
            foreach (var tab in new[] { "ItemsTab", "EquipmentTab", "SkillsTab", "CharactersTab", "StagesTab", "AchievementsTab" })
            {
                var type = Type.GetType("RoRCheatPanel.UI.Tabs." + tab + ", RoRCheatPanel");
                var draw = type?.GetMethod("Draw", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
                if (draw == null)
                    continue;
                var swap = tab == "SkillsTab" ? nameof(SwapSkills) : nameof(SwapVirtual);
                harmony.Patch(draw, transpiler: new HarmonyMethod(typeof(ListFavorites), swap));
            }

            var util = Type.GetType("RoRCheatPanel.GameUtil, RoRCheatPanel");
            var reset = util?.GetMethod("ResetSkills", BindingFlags.Public | BindingFlags.Static);
            if (reset != null)
                harmony.Patch(reset, prefix: new HarmonyMethod(typeof(ListFavorites), nameof(ResetFromPrefab)));
        }

        private static IEnumerable<CodeInstruction> SwapSkills(IEnumerable<CodeInstruction> instructions)
        {
            var bar = typeof(ListFavorites).GetMethod(nameof(DrawSkillBar), BindingFlags.NonPublic | BindingFlags.Static);
            var horizontals = 0;
            foreach (var ins in SwapVirtual(instructions))
            {
                yield return ins;
                if (ins.operand is MethodInfo method && method.Name == "BeginHorizontal")
                {
                    horizontals++;
                    if (horizontals == 1)
                        yield return new CodeInstruction(OpCodes.Call, bar);
                }
            }
        }

        private static SkillSlot _skillSlot = SkillSlot.Primary;

        private static void DrawSkillBar()
        {
            SlotChoice(SkillSlot.Primary, "M1");
            SlotChoice(SkillSlot.Secondary, "M2");
            SlotChoice(SkillSlot.Utility, "Shift");
            SlotChoice(SkillSlot.Special, "R");
        }

        private static void SlotChoice(SkillSlot slot, string label)
        {
            var prev = GUI.backgroundColor;
            if (_skillSlot == slot)
                GUI.backgroundColor = new Color(0.45f, 0.55f, 0.9f);
            var tip = Tr(
                "Сюда запишется скилл с карточки.",
                "The card skill is assigned here.",
                "Aquí se asigna la habilidad de la tarjeta.");
            if (GUILayout.Button(new GUIContent(label, tip), GUILayout.Height(26f)))
                _skillSlot = slot;
            GUI.backgroundColor = prev;
        }

        private static IEnumerable<CodeInstruction> SwapVirtual(IEnumerable<CodeInstruction> instructions)
        {
            var replacement = typeof(ListFavorites).GetMethod(nameof(DrawCards), BindingFlags.NonPublic | BindingFlags.Static);
            foreach (var ins in instructions)
            {
                if (ins.operand is MethodInfo method && method.Name == "DrawVirtual" && method.IsGenericMethod)
                    ins.operand = replacement.MakeGenericMethod(method.GetGenericArguments());
                yield return ins;
            }
        }

        private static void DrawCards<T>(IList<T> rows, Vector2 scroll, float rowHeight, Action<T> drawRow)
        {
            if (rows == null || rows.Count == 0)
                return;
            if (Key(rows[0]) == null || !(rows is IList list))
            {
                for (var i = 0; i < rows.Count; i++)
                    drawRow(rows[i]);
                return;
            }

            Promote(list);
            DrawGrid(list);
        }

        private static void Promote(IList list)
        {
            var fav = new List<object>();
            var rest = new List<object>();
            for (var i = 0; i < list.Count; i++)
            {
                var row = list[i];
                if (IsFav(Key(row)))
                    fav.Add(row);
                else
                    rest.Add(row);
            }

            list.Clear();
            for (var i = 0; i < fav.Count; i++)
                list.Add(fav[i]);
            for (var i = 0; i < rest.Count; i++)
                list.Add(rest[i]);
        }

        private static void DrawGrid(IList list)
        {
            int cols;
            float card;
            Metrics(out cols, out card);
            var col = 0;
            GUILayout.BeginHorizontal();
            for (var i = 0; i < list.Count; i++)
            {
                DrawCard(list[i], card);
                col++;
                if (col < cols)
                    continue;
                col = 0;
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
            }

            GUILayout.EndHorizontal();
            GUILayout.Space(12f);
        }

        private static void Metrics(out int cols, out float card)
        {
            var panel = 900f;
            var window = Type.GetType("RoRCheatPanel.UI.CheatWindow, RoRCheatPanel");
            if (window?.GetField("_rect", BindingFlags.NonPublic | BindingFlags.Static)?.GetValue(null) is Rect area && area.width > 200f)
                panel = area.width - 36f;
            cols = Mathf.Clamp(Mathf.FloorToInt(panel / 112f), 4, 14);
            card = Mathf.Floor((panel / cols) - 6f);
        }

        private static void DrawCard(object row, float card)
        {
            var key = Key(row);
            var tip = Describe(row);
            var title = Title(row);
            GUILayout.BeginVertical(GUILayout.Width(card));
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            Star(key);
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            var iconRect = GUILayoutUtility.GetRect(44f, 44f, GUILayout.Width(44f), GUILayout.Height(44f));
            GUI.Label(iconRect, new GUIContent(string.Empty, tip));
            if (Event.current.type == EventType.Repaint)
                PaintIcon(row, iconRect);
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            GUILayout.Label(new GUIContent(title, tip), NameStyle(), GUILayout.Width(card - 8f), GUILayout.Height(32f));
            Act(row, tip, card - 8f);
            GUILayout.EndVertical();
            Frame(GUILayoutUtility.GetLastRect());
        }

        private static void Act(object row, string tip, float width)
        {
            var def = row.GetType().GetField("Def")?.GetValue(row);
            if (def is ItemDef item)
            {
                var qty = ReadQty();
                if (GUILayout.Button(new GUIContent("+" + qty, tip), GUILayout.Width(width), GUILayout.Height(22f)))
                    Call("GiveItem", item, qty);
                return;
            }

            if (def is EquipmentDef equipment)
            {
                if (GUILayout.Button(new GUIContent(Tr("Надеть", "Equip", "Equipar"), tip), GUILayout.Width(width), GUILayout.Height(22f)))
                    Call("SetEquipment", equipment);
                return;
            }

            if (def is SkillDef skill)
            {
                if (GUILayout.Button(new GUIContent(Tr("Назначить", "Assign", "Asignar"), tip), GUILayout.Width(width), GUILayout.Height(22f)))
                    Call("SetSkill", skill, _skillSlot);
                return;
            }

            if (def is SceneDef scene)
            {
                var half = Mathf.Max(36f, Mathf.Floor(width / 2f) - 2f);
                GUILayout.BeginHorizontal();
                if (GUILayout.Button(new GUIContent(Tr("Идти", "Go", "Ir"), Tr("Сразу перейти на эту карту.", "Travel to this stage now.", "Ir a este mapa ahora.")), GUILayout.Width(half), GUILayout.Height(22f)))
                    Call("TravelNow", scene);
                if (GUILayout.Button(new GUIContent(Tr("След", "Next", "Sig."), Tr("Поставить карту следующей после телепорта.", "Queue this stage after the teleporter.", "Poner este mapa como el siguiente.")), GUILayout.Width(half), GUILayout.Height(22f)))
                    Call("SetNextStage", scene);
                GUILayout.EndHorizontal();
                return;
            }

            if (def is AchievementDef achievement)
            {
                if (GUILayout.Button(new GUIContent(Tr("Выдать", "Grant", "Dar"), tip), GUILayout.Width(width), GUILayout.Height(22f)))
                    Call("GrantAchievement", achievement);
                return;
            }

            var prefab = row.GetType().GetField("Prefab")?.GetValue(row) as GameObject;
            if (prefab != null)
            {
                var display = row.GetType().GetField("Display")?.GetValue(row) as string;
                if (GUILayout.Button(new GUIContent(Tr("Стать", "Become", "Ser"), tip), GUILayout.Width(width), GUILayout.Height(22f)))
                    Call("TransformBodyPrefab", prefab, display);
            }
        }

        private static int ReadQty()
        {
            var tab = Type.GetType("RoRCheatPanel.UI.Tabs.ItemsTab, RoRCheatPanel");
            var value = tab?.GetField("_qty", BindingFlags.NonPublic | BindingFlags.Static)?.GetValue(null);
            return value is int qty && qty > 0 ? qty : 1;
        }

        private static void Star(string key)
        {
            var on = IsFav(key);
            var prev = GUI.backgroundColor;
            if (on)
                GUI.backgroundColor = new Color(0.9f, 0.72f, 0.25f);
            var tip = Tr("Добавить или убрать из избранного. Избранное выше в списке.", "Add or remove a favorite. Favorites stay at the top.", "Añadir o quitar de favoritos. Los favoritos quedan arriba.");
            if (GUILayout.Button(new GUIContent(on ? "★" : "☆", tip), GUILayout.Width(28f), GUILayout.Height(22f)))
            {
                Load();
                if (on)
                    Favs.Remove(key);
                else
                    Favs.Add(key);
                Save();
            }

            GUI.backgroundColor = prev;
        }

        private static string Key(object row)
        {
            if (row == null)
                return null;
            var def = row.GetType().GetField("Def")?.GetValue(row);
            if (def is ItemDef item)
                return "item:" + item.name;
            if (def is EquipmentDef equipment)
                return "equip:" + equipment.name;
            if (def is SkillDef skill)
                return "skill:" + ((UnityEngine.ScriptableObject)skill).name;
            if (def is AchievementDef achievement)
                return "ach:" + achievement.identifier;
            if (def is SceneDef scene)
                return "stage:" + scene.baseSceneName;
            var body = row.GetType().GetField("BodyName")?.GetValue(row) as string;
            if (!string.IsNullOrEmpty(body))
                return "body:" + body;
            return null;
        }

        private static string Title(object row)
        {
            var type = row.GetType();
            var display = type.GetField("Display")?.GetValue(row) as string;
            if (!string.IsNullOrEmpty(display))
                return display;
            var title = type.GetField("Title")?.GetValue(row) as string;
            if (string.IsNullOrEmpty(title))
                return "?";
            var split = title.IndexOf('\n');
            return split > 0 ? title.Substring(0, split) : title;
        }

        private static string Describe(object row)
        {
            var def = row.GetType().GetField("Def")?.GetValue(row);
            if (def is ItemDef item)
                return DescribeOf("Description", item);
            if (def is EquipmentDef equipment)
                return DescribeOf("Description", equipment);
            if (def is SkillDef skill)
                return DescribeOf("Description", skill);
            if (def is AchievementDef achievement)
                return DescribeOf("Description", achievement);
            if (def is SceneDef scene)
                return DescribeOf("Description", scene);
            var tip = row.GetType().GetField("Tip")?.GetValue(row) as string;
            return string.IsNullOrEmpty(tip) ? Title(row) : tip;
        }

        private static void PaintIcon(object row, Rect rect)
        {
            var sprite = LiveSprite(row);
            if (sprite != null && sprite.texture != null)
            {
                var area = sprite.textureRect;
                var uv = new Rect(
                    area.x / sprite.texture.width,
                    area.y / sprite.texture.height,
                    area.width / sprite.texture.width,
                    area.height / sprite.texture.height);
                GUI.DrawTextureWithTexCoords(rect, sprite.texture, uv, true);
                return;
            }

            var preview = row.GetType().GetField("Preview")?.GetValue(row) as Texture;
            if (preview != null)
            {
                GUI.DrawTexture(rect, preview, ScaleMode.ScaleToFit, true);
                return;
            }

            var icon = row.GetType().GetField("Icon")?.GetValue(row);
            if (icon is Texture texture)
            {
                GUI.DrawTexture(rect, texture, ScaleMode.ScaleToFit, true);
                return;
            }

            var tex = icon?.GetType().GetField("Tex")?.GetValue(icon) as Texture;
            if (tex != null)
            {
                if (icon.GetType().GetField("Uv")?.GetValue(icon) is Rect uv && uv.width > 0.001f && uv.height > 0.001f)
                    GUI.DrawTextureWithTexCoords(rect, tex, uv, true);
                else
                    GUI.DrawTexture(rect, tex, ScaleMode.ScaleToFit, true);
                return;
            }

            GUI.DrawTexture(rect, Gem(), ScaleMode.ScaleToFit, true);
        }

        private static Sprite LiveSprite(object row)
        {
            var def = row.GetType().GetField("Def")?.GetValue(row);
            if (def is AchievementDef achievement)
            {
                var icon = achievement.GetType().GetMethod("GetAchievedIcon", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)?.Invoke(achievement, null);
                return icon as Sprite;
            }

            if (def is SkillDef skill && skill.icon != null && skill.icon.texture != null)
                return skill.icon;
            if (def is EquipmentDef equipment && equipment.pickupIconSprite != null && equipment.pickupIconSprite.texture != null)
                return equipment.pickupIconSprite;
            if (def is ItemDef item && item.pickupIconSprite != null && item.pickupIconSprite.texture != null)
                return item.pickupIconSprite;
            return null;
        }

        private static Texture2D _gem;
        private static Texture2D _pixel;

        private static void Frame(Rect rect)
        {
            if (Event.current.type != EventType.Repaint || rect.width < 2f)
                return;
            var prev = GUI.color;
            GUI.color = new Color(0.62f, 0.72f, 0.98f, 0.95f);
            const float edge = 1f;
            var pixel = Pixel();
            GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width, edge), pixel);
            GUI.DrawTexture(new Rect(rect.x, rect.yMax - edge, rect.width, edge), pixel);
            GUI.DrawTexture(new Rect(rect.x, rect.y, edge, rect.height), pixel);
            GUI.DrawTexture(new Rect(rect.xMax - edge, rect.y, edge, rect.height), pixel);
            GUI.color = prev;
        }

        private static Texture2D Pixel()
        {
            if (_pixel != null)
                return _pixel;
            _pixel = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            _pixel.SetPixel(0, 0, Color.white);
            _pixel.Apply();
            _pixel.hideFlags = HideFlags.HideAndDontSave;
            return _pixel;
        }

        private static Texture2D Gem()
        {
            if (_gem != null)
                return _gem;
            const int n = 32;
            _gem = new Texture2D(n, n, TextureFormat.RGBA32, false);
            var pixels = new Color[n * n];
            var clear = new Color(0f, 0f, 0f, 0f);
            var fill = new Color(0.45f, 0.62f, 0.95f, 1f);
            for (var i = 0; i < pixels.Length; i++)
                pixels[i] = clear;
            var mid = (n - 1) / 2f;
            for (var y = 0; y < n; y++)
            {
                for (var x = 0; x < n; x++)
                {
                    if (Mathf.Abs(x - mid) + Mathf.Abs(y - mid) <= mid - 1f)
                        pixels[y * n + x] = fill;
                }
            }

            _gem.SetPixels(pixels);
            _gem.Apply();
            _gem.hideFlags = HideFlags.HideAndDontSave;
            return _gem;
        }

        private static bool IsFav(string key)
        {
            Load();
            return key != null && Favs.Contains(key);
        }

        private static void Load()
        {
            if (_loaded)
                return;
            _loaded = true;
            var raw = PlayerPrefs.GetString(Pref, "");
            var parts = raw.Split('\n');
            for (var i = 0; i < parts.Length; i++)
            {
                if (parts[i].Length > 0)
                    Favs.Add(parts[i]);
            }
        }

        private static void Save()
        {
            var builder = new System.Text.StringBuilder();
            foreach (var key in Favs)
            {
                if (builder.Length > 0)
                    builder.Append('\n');
                builder.Append(key);
            }

            PlayerPrefs.SetString(Pref, builder.ToString());
            PlayerPrefs.Save();
        }

        private static GUIStyle NameStyle()
        {
            if (_name == null)
            {
                _name = new GUIStyle(GUI.skin.label)
                {
                    alignment = TextAnchor.UpperCenter,
                    wordWrap = true,
                    fontSize = 10,
                    clipping = TextClipping.Clip
                };
            }

            return _name;
        }

        private static bool ResetFromPrefab()
        {
            if (!NetworkServer.active)
                return true;
            var count = 0;
            foreach (var user in Targets())
            {
                if (user == null || user.master == null)
                    continue;
                var body = user.GetCurrentBody();
                if (body == null)
                    continue;
                var prefab = BodyCatalog.GetBodyPrefab(body.bodyIndex);
                var source = prefab != null ? prefab.GetComponent<SkillLocator>() : null;
                if (source == null || body.skillLocator == null || !CopySkills(body.skillLocator, source))
                {
                    user.master.Respawn(body.footPosition, body.transform.rotation, true);
                    count++;
                    continue;
                }

                body.skillLocator.ResetSkills();
                count++;
            }

            Toast(Tr("Скиллы возвращены: " + count, "Skills restored: " + count, "Habilidades restauradas: " + count));
            return false;
        }

        private static bool CopySkills(SkillLocator live, SkillLocator source)
        {
            var ok = true;
            ok &= CopySlot(live.primary, source.primary);
            ok &= CopySlot(live.secondary, source.secondary);
            ok &= CopySlot(live.utility, source.utility);
            ok &= CopySlot(live.special, source.special);
            return ok;
        }

        private static bool CopySlot(GenericSkill live, GenericSkill source)
        {
            if (source == null)
                return true;
            if (live == null)
                return false;
            var family = ReadMember(source, "skillFamily");
            if (family != null)
                WriteMember(live, "skillFamily", family);
            var def = family != null ? ReadMember(family, "defaultSkillDef") as SkillDef : null;
            if (def == null)
                def = ReadMember(source, "baseSkill") as SkillDef;
            if (def != null)
                live.SetBaseSkill(def);
            return true;
        }

        private static object ReadMember(object target, string name)
        {
            var type = target.GetType();
            var field = type.GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (field != null)
                return field.GetValue(target);
            return type.GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)?.GetValue(target, null);
        }

        private static void WriteMember(object target, string name, object value)
        {
            var type = target.GetType();
            var field = type.GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (field != null && (value == null || field.FieldType.IsInstanceOfType(value)))
            {
                field.SetValue(target, value);
                return;
            }

            var property = type.GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (property != null && property.CanWrite)
                property.SetValue(target, value, null);
        }

        private static IEnumerable<NetworkUser> Targets()
        {
            var type = Type.GetType("RoRCheatPanel.GameUtil, RoRCheatPanel");
            var method = type?.GetMethod("Targets", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            if (method == null || !(method.Invoke(null, null) is IEnumerable items))
                yield break;
            foreach (var item in items)
            {
                if (item is NetworkUser user && user != null)
                    yield return user;
            }
        }

        private static void Call(string method, params object[] args)
        {
            var type = Type.GetType("RoRCheatPanel.GameUtil, RoRCheatPanel");
            if (type == null)
                return;
            foreach (var info in type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static))
            {
                if (info.Name != method)
                    continue;
                var parameters = info.GetParameters();
                if (parameters.Length < args.Length)
                    continue;
                var match = true;
                for (var i = 0; i < args.Length; i++)
                {
                    if (args[i] != null && !parameters[i].ParameterType.IsInstanceOfType(args[i]))
                        match = false;
                }

                if (!match)
                    continue;
                var full = args;
                if (parameters.Length > args.Length)
                {
                    var optional = true;
                    for (var i = args.Length; i < parameters.Length; i++)
                    {
                        if (!parameters[i].IsOptional)
                            optional = false;
                    }

                    if (!optional)
                        continue;
                    full = new object[parameters.Length];
                    Array.Copy(args, full, args.Length);
                    for (var i = args.Length; i < parameters.Length; i++)
                        full[i] = parameters[i].DefaultValue;
                }

                info.Invoke(null, full);
                return;
            }
        }

        private static string DescribeOf(string method, object arg)
        {
            var type = Type.GetType("RoRCheatPanel.GameUtil, RoRCheatPanel");
            if (type == null || arg == null)
                return "";
            foreach (var info in type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static))
            {
                if (info.Name != method)
                    continue;
                var parameters = info.GetParameters();
                if (parameters.Length == 1 && parameters[0].ParameterType.IsInstanceOfType(arg))
                    return info.Invoke(null, new[] { arg }) as string ?? "";
            }

            return "";
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
