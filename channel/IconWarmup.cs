using System;
using System.Collections.Generic;
using System.Reflection;
using RoR2;
using UnityEngine;

namespace RoRCheatPanel.Channel
{
    internal static class IconWarmup
    {
        private static readonly List<object> Handles = new List<object>();
        private static readonly List<UnityEngine.Object> Owners = new List<UnityEngine.Object>();
        private static readonly List<FieldInfo> Fields = new List<FieldInfo>();
        private static readonly HashSet<int> Busy = new HashSet<int>();
        private static readonly HashSet<int> Done = new HashSet<int>();
        private static int _items;
        private static int _equipment;
        private static int _artifactAt;
        private static ArtifactDef[] _artifactCache;
        private static int _achievementAt;
        private static UnityEngine.Object[] _achievementCache;
        private static int _seenSprites = -1;
        private static bool _dirty;

        public static void Tick()
        {
            Poll();
            if (Time.frameCount % 20 != 0)
                return;
            RequestItems();
            RequestEquipment();
            RequestArtifacts();
            NoteSpriteCount();
        }

        private static void RequestItems()
        {
            if (ItemCatalog.itemCount <= 0)
                return;
            var defs = ItemCatalog.allItemDefs;
            if (_items >= defs.Length)
            {
                if (Time.frameCount % 600 == 0)
                    _items = 0;
                return;
            }

            for (var n = 0; n < 16 && _items < defs.Length; n++)
            {
                Consider(defs[_items]);
                _items++;
            }
        }

        private static void RequestEquipment()
        {
            var list = EquipmentCatalog.equipmentList;
            if (list == null)
                return;
            if (_equipment >= list.Count)
            {
                if (Time.frameCount % 600 == 0)
                    _equipment = 0;
                return;
            }

            for (var n = 0; n < 8 && _equipment < list.Count; n++)
            {
                Consider(EquipmentCatalog.GetEquipmentDef(list[_equipment]));
                _equipment++;
            }
        }

        private static void RequestArtifacts()
        {
            if (_artifactCache == null)
                _artifactCache = ArtifactIcons();
            if (_artifactAt >= _artifactCache.Length)
            {
                if (Time.frameCount % 300 == 0)
                    _artifactAt = 0;
                else
                    return;
            }
            for (var n = 0; n < 8 && _artifactAt < _artifactCache.Length; n++)
                Consider(_artifactCache[_artifactAt++]);
            if (_achievementCache == null)
                _achievementCache = Collect<UnityEngine.Object>(typeof(AchievementManager));
            if (_achievementAt >= _achievementCache.Length)
                return;
            for (var n = 0; n < 8 && _achievementAt < _achievementCache.Length; n++)
                Consider(_achievementCache[_achievementAt++]);
        }

        private static ArtifactDef[] ArtifactIcons()
        {
            var found = Collect<ArtifactDef>(typeof(ArtifactCatalog));
            if (found.Length > 0 || ArtifactCatalog.artifactCount <= 0)
                return found;
            var list = new List<ArtifactDef>();
            for (var i = 0; i < ArtifactCatalog.artifactCount; i++)
            {
                var def = ArtifactCatalog.GetArtifactDef((ArtifactIndex)i);
                if (def != null)
                    list.Add(def);
            }

            return list.ToArray();
        }

        private static T[] Collect<T>(Type catalog) where T : UnityEngine.Object
        {
            var found = new List<T>();
            if (catalog == null)
                return found.ToArray();
            var flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;
            foreach (var member in catalog.GetMembers(flags))
            {
                object value = null;
                try
                {
                    if (member is PropertyInfo property && property.GetIndexParameters().Length == 0)
                        value = property.GetValue(null, null);
                    else if (member is FieldInfo field)
                        value = field.GetValue(null);
                }
                catch
                {
                    continue;
                }

                if (!(value is System.Collections.IEnumerable items) || value is string)
                    continue;
                foreach (var item in items)
                {
                    if (item is T def)
                        found.Add(def);
                }

                if (found.Count > 0)
                    break;
            }

            return found.ToArray();
        }

        private static void NoteSpriteCount()
        {
            if (ItemCatalog.itemCount <= 0 || Time.frameCount % 90 != 0)
                return;
            var count = 0;
            var defs = ItemCatalog.allItemDefs;
            for (var i = 0; i < defs.Length; i++)
            {
                var sprite = defs[i] != null ? defs[i].pickupIconSprite : null;
                if (sprite != null && sprite.texture != null)
                    count++;
            }

            if (count == _seenSprites)
                return;
            _seenSprites = count;
            if (count > 0)
                PanelFixes.FlushLists();
        }

        private static void Consider(UnityEngine.Object def)
        {
            if (def == null)
                return;
            var flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            foreach (var spriteField in def.GetType().GetFields(flags))
            {
                if (spriteField.FieldType != typeof(Sprite))
                    continue;
                if (spriteField.GetValue(def) is Sprite ready && ready.texture != null)
                    continue;
                var reference = def.GetType().GetField(spriteField.Name + "Reference", flags)?.GetValue(def);
                if (reference == null && spriteField.Name.EndsWith("Sprite"))
                {
                    var trimmed = spriteField.Name.Substring(0, spriteField.Name.Length - "Sprite".Length) + "Reference";
                    reference = def.GetType().GetField(trimmed, flags)?.GetValue(def);
                }

                if (reference == null)
                    continue;
                TryLoad(def, spriteField, reference);
            }

            var named = def.GetType().GetField("pickupIconReference", flags)?.GetValue(def)
                ?? def.GetType().GetProperty("pickupIconReference", flags)?.GetValue(def, null);
            var spriteFieldNamed = def.GetType().GetField("pickupIconSprite", flags);
            if (named != null && spriteFieldNamed != null && !(spriteFieldNamed.GetValue(def) is Sprite))
                TryLoad(def, spriteFieldNamed, named);

            var preload = def.GetType().GetMethod("PreloadIcon", flags);
            if (preload != null && preload.GetParameters().Length == 0)
                preload.Invoke(def, null);
        }

        private static void TryLoad(UnityEngine.Object def, FieldInfo spriteField, object reference)
        {
            var id = def.GetInstanceID() ^ spriteField.MetadataToken;
            if (Done.Contains(id) || !Busy.Add(id))
                return;
            var valid = reference.GetType().GetMethod("RuntimeKeyIsValid")?.Invoke(reference, null);
            var keyOk = valid is bool ok && ok;
            if (!keyOk && string.IsNullOrEmpty(reference.GetType().GetProperty("AssetGUID")?.GetValue(reference, null) as string))
            {
                Busy.Remove(id);
                return;
            }

            if (reference.GetType().GetProperty("Asset")?.GetValue(reference) is Sprite asset && asset.texture != null)
            {
                spriteField.SetValue(def, asset);
                Busy.Remove(id);
                Done.Add(id);
                _dirty = true;
                return;
            }

            MethodInfo load = null;
            foreach (var method in reference.GetType().GetMethods())
            {
                if (method.Name != "LoadAssetAsync" || method.GetParameters().Length != 0)
                    continue;
                if (!method.IsGenericMethodDefinition)
                {
                    load = method;
                    break;
                }

                if (method.GetGenericArguments().Length == 1)
                    load = method.MakeGenericMethod(typeof(Sprite));
            }

            object handle = null;
            try
            {
                handle = load?.Invoke(reference, null);
            }
            catch
            {
                handle = null;
            }

            if (handle == null)
                handle = LoadByGuid(reference);
            if (handle == null)
            {
                Busy.Remove(id);
                return;
            }

            Handles.Add(handle);
            Owners.Add(def);
            Fields.Add(spriteField);
        }

        private static object LoadByGuid(object reference)
        {
            var guid = reference.GetType().GetProperty("AssetGUID")?.GetValue(reference, null) as string;
            if (string.IsNullOrEmpty(guid))
                return null;
            var addressables = Type.GetType("UnityEngine.AddressableAssets.Addressables, Unity.Addressables");
            if (addressables == null)
                return null;
            foreach (var method in addressables.GetMethods(BindingFlags.Public | BindingFlags.Static))
            {
                if (method.Name != "LoadAssetAsync" || !method.IsGenericMethodDefinition || method.GetParameters().Length != 1)
                    continue;
                try
                {
                    return method.MakeGenericMethod(typeof(Sprite)).Invoke(null, new object[] { guid });
                }
                catch
                {
                    return null;
                }
            }

            return null;
        }

        private static void Poll()
        {
            for (var i = Handles.Count - 1; i >= 0; i--)
            {
                var handle = Handles[i];
                var done = false;
                try
                {
                    done = handle.GetType().GetProperty("IsDone")?.GetValue(handle) is bool ready && ready;
                }
                catch
                {
                    done = true;
                }

                if (!done)
                    continue;

                try
                {
                    var status = handle.GetType().GetProperty("Status")?.GetValue(handle);
                    if (status != null && status.ToString() == "Succeeded"
                        && handle.GetType().GetProperty("Result")?.GetValue(handle) is Sprite sprite
                        && sprite.texture != null)
                    {
                        Fields[i].SetValue(Owners[i], sprite);
                        Done.Add(Owners[i].GetInstanceID() ^ Fields[i].MetadataToken);
                        _dirty = true;
                    }
                }
                catch
                {
                }

                var owner = Owners[i];
                var field = Fields[i];
                if (owner != null && field != null)
                    Busy.Remove(owner.GetInstanceID() ^ field.MetadataToken);
                Handles.RemoveAt(i);
                Owners.RemoveAt(i);
                Fields.RemoveAt(i);
            }

            if (!_dirty)
                return;
            PanelFixes.FlushLists();
            _dirty = false;
        }

        private static readonly Dictionary<int, Sprite> Loaded = new Dictionary<int, Sprite>();
        private static bool _scanned;
        private static Texture2D _gem;

        public static void Install(HarmonyLib.Harmony harmony)
        {
            var run = Type.GetType("RoRCheatPanel.UI.Tabs.RunTab, RoRCheatPanel");
            var draw = run?.GetMethod("DrawArtifacts", BindingFlags.NonPublic | BindingFlags.Static);
            if (draw != null)
                harmony.Patch(draw, prefix: new HarmonyLib.HarmonyMethod(typeof(IconWarmup), nameof(DrawWorldArtifacts)));
        }

        private static bool DrawWorldArtifacts()
        {
            if (RunArtifactManager.instance == null)
                return true;
            ScanLoaded();
            GUILayout.Label(Text("artifacts"));
            GUILayout.BeginHorizontal();
            var column = 0;
            for (var i = 0; i < ArtifactCatalog.artifactCount; i++)
            {
                var def = ArtifactCatalog.GetArtifactDef((ArtifactIndex)i);
                if (def == null)
                    continue;
                var on = RunArtifactManager.instance.IsArtifactEnabled(def);
                var prev = GUI.backgroundColor;
                GUI.backgroundColor = on ? new Color(0.45f, 0.75f, 0.4f) : new Color(0.3f, 0.3f, 0.3f);
                var tip = Text("Description", def);
                GUILayout.BeginHorizontal(GUILayout.Width(210f));
                var rect = GUILayoutUtility.GetRect(28f, 28f, GUILayout.Width(28f), GUILayout.Height(28f));
                var sprite = SpriteFor(def);
                if (Event.current.type == EventType.Repaint)
                {
                    if (sprite != null && sprite.texture != null)
                    {
                        var area = sprite.textureRect;
                        var uv = new Rect(area.x / sprite.texture.width, area.y / sprite.texture.height, area.width / sprite.texture.width, area.height / sprite.texture.height);
                        GUI.DrawTextureWithTexCoords(rect, sprite.texture, uv, true);
                    }
                    else
                        GUI.DrawTexture(rect, Gem(), ScaleMode.ScaleToFit, true);
                }

                if (GUILayout.Button(new GUIContent(Text("DisplayName", def), tip), GUILayout.Height(32f), GUILayout.MinWidth(150f)))
                    Call("SetArtifact", def, !on);
                GUILayout.EndHorizontal();
                GUI.backgroundColor = prev;
                column++;
                if (column % 4 == 0)
                {
                    GUILayout.EndHorizontal();
                    GUILayout.BeginHorizontal();
                }
            }

            GUILayout.EndHorizontal();
            return false;
        }

        private static Sprite SpriteFor(ArtifactDef def)
        {
            if (def.smallIconSelectedSprite != null && def.smallIconSelectedSprite.texture != null)
                return def.smallIconSelectedSprite;
            if (def.smallIconDeselectedSprite != null && def.smallIconDeselectedSprite.texture != null)
                return def.smallIconDeselectedSprite;
            Sprite found;
            if (Loaded.TryGetValue((int)def.artifactIndex, out found) && found != null)
                return found;
            Consider(def);
            return null;
        }

        private static void ScanLoaded()
        {
            if (_scanned || ArtifactCatalog.artifactCount <= 0)
                return;
            _scanned = true;
            var sprites = Resources.FindObjectsOfTypeAll<Sprite>();
            for (var i = 0; i < ArtifactCatalog.artifactCount; i++)
            {
                var def = ArtifactCatalog.GetArtifactDef((ArtifactIndex)i);
                if (def == null || string.IsNullOrEmpty(def.cachedName))
                    continue;
                for (var s = 0; s < sprites.Length; s++)
                {
                    var sprite = sprites[s];
                    if (sprite == null || sprite.texture == null || string.IsNullOrEmpty(sprite.name))
                        continue;
                    if (sprite.name.IndexOf(def.cachedName, StringComparison.OrdinalIgnoreCase) < 0)
                        continue;
                    if (sprite.name.IndexOf("Artifact", StringComparison.OrdinalIgnoreCase) < 0 && sprite.name.IndexOf("artifact", StringComparison.OrdinalIgnoreCase) < 0)
                        continue;
                    Loaded[(int)def.artifactIndex] = sprite;
                    if (def.smallIconSelectedSprite == null)
                        def.smallIconSelectedSprite = sprite;
                    break;
                }
            }
        }

        private static string Text(string method, object arg = null)
        {
            var type = Type.GetType("RoRCheatPanel.GameUtil, RoRCheatPanel");
            if (method == "artifacts")
            {
                var loc = Type.GetType("RoRCheatPanel.Loc, RoRCheatPanel");
                return loc?.GetMethod("T", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static, null, new[] { typeof(string) }, null)
                    ?.Invoke(null, new object[] { "artifacts" }) as string ?? "Артефакты";
            }

            if (type == null || arg == null)
                return "";
            foreach (var info in type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static))
            {
                if (info.Name != method)
                    continue;
                var parameters = info.GetParameters();
                if (parameters.Length >= 1 && parameters[0].ParameterType.IsInstanceOfType(arg))
                    return info.Invoke(null, parameters.Length == 1 ? new[] { arg } : new[] { arg, Type.Missing }) as string ?? "";
            }

            return "";
        }

        private static void Call(string method, ArtifactDef def, bool enabled)
        {
            var type = Type.GetType("RoRCheatPanel.GameUtil, RoRCheatPanel");
            var info = type?.GetMethod(method, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            info?.Invoke(null, new object[] { def, enabled });
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
            var mid = (n - 1) / 2f;
            for (var y = 0; y < n; y++)
            {
                for (var x = 0; x < n; x++)
                    pixels[y * n + x] = Mathf.Abs(x - mid) + Mathf.Abs(y - mid) <= mid - 1f ? fill : clear;
            }

            _gem.SetPixels(pixels);
            _gem.Apply();
            _gem.hideFlags = HideFlags.HideAndDontSave;
            return _gem;
        }
    }
}
