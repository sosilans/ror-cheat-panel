using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using RoR2;
using RoR2.CharacterAI;
using UnityEngine;
using UnityEngine.Networking;

namespace RoRCheatPanel.Channel
{
    internal static class MonsterSpawn
    {
        private enum NpcKind
        {
            Grunt,
            Boss,
            Drone,
            Survivor
        }

        private struct Entry
        {
            public string Name;
            public GameObject Master;
            public Texture Icon;
            public TeamIndex Team;
            public NpcKind Kind;
        }

        private struct Drop
        {
            public string Name;
            public Sprite Icon;
            public Texture Tex;
            public PickupIndex Pickup;
            public bool Hidden;
            public int Tier;
        }

        private struct Prop
        {
            public string Name;
            public InteractableSpawnCard Card;
            public Texture Icon;
        }

        private struct Pending
        {
            public CharacterMaster Master;
            public int Frames;
            public bool NoAi;
            public bool NoLoot;
            public int Elite;
        }

        private static readonly List<Entry> Catalog = new List<Entry>();
        private static readonly List<Drop> Items = new List<Drop>();
        private static readonly List<Drop> Gear = new List<Drop>();
        private static readonly List<Prop> Props = new List<Prop>();
        private static readonly List<Pending> Fresh = new List<Pending>();
        private static readonly List<GameObject> Spawned = new List<GameObject>();
        private static readonly List<DropWatch> DropWatches = new List<DropWatch>();
        private static readonly List<int> Order = new List<int>();
        private static readonly HashSet<string> Favs = new HashSet<string>();
        private static bool _favsLoaded;
        private static int _catalogCount = -1;
        private static int _itemCount = -1;
        private static int _gearCount = -1;
        private static bool _propsReady;
        private static Vector2 _scroll;
        private static float _scrollHeight = 360f;
        private static string _search = "";
        private static int _page;
        private static bool _grunts = true;
        private static bool _bosses = true;
        private static bool _drones = true;
        private static bool _survivors = true;
        private static bool _hiddenItems;
        private static bool _tWhite = true;
        private static bool _tGreen = true;
        private static bool _tRed = true;
        private static bool _tLunar = true;
        private static bool _tBoss = true;
        private static bool _tVoid = true;
        private static bool _tOther = true;
        private static bool _noAi;
        private static bool _allies;
        private static bool _noLoot;
        private static int _count = 1;
        private static int _place;
        private static int _elite;
        private static string[] _navNames;
        private static int _navLang = -1;

        public static void Install(HarmonyLib.Harmony harmony)
        {
            var window = Type.GetType("RoRCheatPanel.UI.CheatWindow, RoRCheatPanel");
            if (window == null)
            {
                Debug.LogWarning("[RoRCheatPanel.Channel] missing CheatWindow");
                return;
            }

            var nav = window.GetMethod("DrawNav", BindingFlags.NonPublic | BindingFlags.Static);
            var category = window.GetMethod("DrawCategory", BindingFlags.NonPublic | BindingFlags.Static);
            if (nav != null)
                harmony.Patch(nav, prefix: new HarmonyLib.HarmonyMethod(typeof(MonsterSpawn), nameof(DrawNav)));
            if (category != null)
                harmony.Patch(category, prefix: new HarmonyLib.HarmonyMethod(typeof(MonsterSpawn), nameof(DrawCategory)));
        }

        public static void Tick()
        {
            WatchDrops();
            for (var i = Fresh.Count - 1; i >= 0; i--)
            {
                var item = Fresh[i];
                if (item.Master == null || item.Frames <= 0)
                {
                    Fresh.RemoveAt(i);
                    continue;
                }

                Apply(item.Master, item.NoAi, item.NoLoot, item.Elite);
                item.Frames--;
                Fresh[i] = item;
            }
        }

        private static bool DrawNav()
        {
            var window = WindowType();
            var catField = window != null ? window.GetField("_cat", BindingFlags.NonPublic | BindingFlags.Static) : null;
            var subField = window != null ? window.GetField("Sub", BindingFlags.NonPublic | BindingFlags.Static) : null;
            if (catField == null || subField == null || !(subField.GetValue(null) is int[] sub))
                return true;

            var generation = LocGeneration();
            if (_navNames == null || _navNames.Length != 7 || _navLang != generation)
            {
                _navLang = generation;
                _navNames = new[]
                {
                    L("cat_loot"),
                    L("cat_char"),
                    L("cat_world"),
                    L("cat_tools"),
                    Tr("Спавн", "Spawn", "Invocar"),
                    L("cat_fun"),
                    L("cat_panel")
                };
            }

            var cat = Pills(_navNames, (int)catField.GetValue(null), 32f);
            catField.SetValue(null, cat);
            var names = SubNames(cat);
            if (names != null && names.Length > 1 && cat >= 0 && cat < sub.Length)
            {
                GUILayout.Space(2f);
                sub[cat] = Pills(names, sub[cat], 26f);
            }

            return false;
        }

        private static bool DrawCategory()
        {
            var window = WindowType();
            var catField = window != null ? window.GetField("_cat", BindingFlags.NonPublic | BindingFlags.Static) : null;
            var subField = window != null ? window.GetField("Sub", BindingFlags.NonPublic | BindingFlags.Static) : null;
            if (catField == null || !(subField?.GetValue(null) is int[] sub))
                return true;

            var cat = (int)catField.GetValue(null);
            switch (cat)
            {
                case 0:
                    Tab(sub[0] == 1 ? "EquipmentTab" : sub[0] == 2 ? "PresetsTab" : "ItemsTab");
                    break;
                case 1:
                    Tab(sub[1] == 1 ? "CharactersTab" : sub[1] == 2 ? "AchievementsTab" : "SkillsTab");
                    break;
                case 2:
                    Tab(sub[2] == 1 ? "StagesTab" : sub[2] == 2 ? "TeamsTab" : sub[2] == 3 ? "PlayersTab" : "RunTab");
                    break;
                case 3:
                    Tab("ToolsTab");
                    break;
                case 4:
                    DrawPage();
                    break;
                case 5:
                    Tab("FunTab");
                    break;
                default:
                    Tab("AboutTab");
                    break;
            }

            return false;
        }

        private static void DrawPage()
        {
            GUILayout.Label(Tr(
                "Хост. НПС появляются перед тобой. Предметы и снаряжение падают на землю. Объекты — сундуки, бочки и прочее.",
                "Host only. NPCs appear in front of you. Items and equipment drop on the ground. Objects are chests, barrels, and the rest.",
                "Solo anfitrión. Los PNJ aparecen delante. Los objetos y el equipo caen al suelo. El resto son cofres, barriles y demás."));
            GUILayout.BeginHorizontal();
            _page = Pick(_page, Tr("НПС", "NPCs", "PNJ"), 0);
            _page = Pick(_page, Tr("Предметы", "Items", "Objetos"), 1);
            _page = Pick(_page, Tr("Снаряжение", "Equipment", "Equipo"), 2);
            _page = Pick(_page, Tr("Объекты", "Objects", "Cosas"), 3);
            GUILayout.EndHorizontal();

            _search = GUILayout.TextField(_search ?? "", GUILayout.Height(24f));
            GUILayout.BeginHorizontal();
            _place = Pick(_place, Tr("Перед собой", "In front", "Delante"), 0);
            _place = Pick(_place, Tr("Под ноги", "At feet", "A los pies"), 1);
            _place = Pick(_place, Tr("Кольцо", "Ring", "Anillo"), 2);
            GUILayout.Label(Tr("Кол-во ", "Count ", "Cant. ") + _count, GUILayout.Width(78f));
            if (GUILayout.Button("-", GUILayout.Width(28f), GUILayout.Height(24f)))
                _count = Mathf.Max(1, _count - 1);
            if (GUILayout.Button("+", GUILayout.Width(28f), GUILayout.Height(24f)))
                _count = Mathf.Min(20, _count + 1);
            if (GUILayout.Button(Tr("Убрать вызванное", "Clear spawned", "Quitar invocado"), GUILayout.Height(24f)))
                ClearSpawned();
            GUILayout.EndHorizontal();
            if (_page == 0)
            {
                GUILayout.BeginHorizontal();
                _grunts = Toggle(_grunts, Tr("Рядовые", "Grunts", "Comunes"));
                _bosses = Toggle(_bosses, Tr("Чемпионы", "Champions", "Campeones"));
                _drones = Toggle(_drones, Tr("Дроны", "Drones", "Drones"));
                _survivors = Toggle(_survivors, Tr("Выжившие", "Survivors", "Supervivientes"));
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
                _noAi = Toggle(_noAi, Tr("Без интеллекта", "No AI", "Sin IA"));
                _allies = Toggle(_allies, Tr("Союзники", "Allies", "Aliados"));
                _noLoot = Toggle(_noLoot, Tr("Без лута", "No loot", "Sin botín"));
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
                _elite = Pick(_elite, Tr("Без элиты", "No elite", "Sin élite"), 0);
                _elite = Pick(_elite, Tr("Огонь", "Fire", "Fuego"), 1);
                _elite = Pick(_elite, Tr("Молния", "Lightning", "Rayo"), 2);
                _elite = Pick(_elite, Tr("Лёд", "Glacial", "Glacial"), 3);
                GUILayout.EndHorizontal();
            }
            else if (_page == 1)
            {
                GUILayout.BeginHorizontal();
                _tWhite = Toggle(_tWhite, L("tier_white"));
                _tGreen = Toggle(_tGreen, L("tier_green"));
                _tRed = Toggle(_tRed, L("tier_red"));
                _tLunar = Toggle(_tLunar, L("tier_lunar"));
                _tBoss = Toggle(_tBoss, L("tier_boss"));
                _tVoid = Toggle(_tVoid, L("tier_void"));
                _tOther = Toggle(_tOther, L("tier_other"));
                _hiddenItems = Toggle(_hiddenItems, Tr("Скрытые", "Hidden", "Ocultos"));
                GUILayout.EndHorizontal();
            }

            _scroll = GUILayout.BeginScrollView(_scroll, GUILayout.Height(FitScroll(ref _scrollHeight)));
            var needle = (_search ?? "").Trim();
            var shown = 0;
            if (_page == 0)
                shown = DrawNpcs(needle);
            else if (_page == 1)
                shown = DrawDropGrid(Items, EnsureItems, needle, true);
            else if (_page == 2)
                shown = DrawDropGrid(Gear, EnsureGear, needle, false);
            else
                shown = DrawProps(needle);

            if (shown == 0)
                GUILayout.Label(Tr("Никого не нашёл", "Nothing matches", "Nada coincide"));
            GUILayout.Space(16f);
            GUILayout.EndScrollView();
        }

        private static float FitScroll(ref float remembered)
        {
            var window = WindowType();
            var panel = window?.GetField("_rect", BindingFlags.NonPublic | BindingFlags.Static)?.GetValue(null) is Rect area
                ? area.height
                : 720f;
            if (Event.current != null && Event.current.type == EventType.Repaint)
            {
                var last = GUILayoutUtility.GetLastRect();
                if (last.yMax > 24f && panel > last.yMax + 80f)
                    remembered = Mathf.Clamp(panel - last.yMax - 18f, 180f, panel - 36f);
            }

            return remembered;
        }

        private static int Pick(int page, string label, int index)
        {
            var prev = GUI.backgroundColor;
            if (page == index)
                GUI.backgroundColor = new Color(0.45f, 0.48f, 0.85f);
            if (GUILayout.Button(label, GUILayout.Height(28f)))
                page = index;
            GUI.backgroundColor = prev;
            return page;
        }

        private static int DrawNpcs(string needle)
        {
            EnsureCatalog();
            Order.Clear();
            for (var pass = 0; pass < 2; pass++)
            {
                for (var i = 0; i < Catalog.Count; i++)
                {
                    var entry = Catalog[i];
                    if (!NpcVisible(entry, needle))
                        continue;
                    var fav = IsFav("n:" + entry.Name);
                    if (pass == 0 && fav)
                        Order.Add(i);
                    if (pass == 1 && !fav)
                        Order.Add(i);
                }
            }

            return DrawCards(Order.Count, index =>
            {
                var entry = Catalog[Order[index]];
                DrawCard(entry.Icon, null, entry.Name, "n:" + entry.Name, NpcTip(entry), () => Spawn(entry));
            });
        }

        private static bool NpcVisible(Entry entry, string needle)
        {
            if (entry.Kind == NpcKind.Grunt && !_grunts)
                return false;
            if (entry.Kind == NpcKind.Boss && !_bosses)
                return false;
            if (entry.Kind == NpcKind.Drone && !_drones)
                return false;
            if (entry.Kind == NpcKind.Survivor && !_survivors)
                return false;
            if (needle.Length > 0 && entry.Name.IndexOf(needle, StringComparison.OrdinalIgnoreCase) < 0)
                return false;
            return true;
        }

        private static int DrawCards(int count, Action<int> draw)
        {
            var col = 0;
            GUILayout.BeginHorizontal();
            for (var i = 0; i < count; i++)
            {
                draw(i);
                col++;
                if (col < 8)
                    continue;
                col = 0;
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
            }

            GUILayout.EndHorizontal();
            return count;
        }

        private static void DrawCard(Texture texture, Sprite sprite, string name, string key, string tip, Action spawn)
        {
            var hint = string.IsNullOrEmpty(tip) ? name : tip;
            GUILayout.BeginVertical(GUILayout.Width(100f));
            GUILayout.BeginHorizontal();
            Star(key);
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            DrawPortrait(texture, sprite);
            GUI.Label(GUILayoutUtility.GetLastRect(), new GUIContent(string.Empty, hint));
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            GUILayout.Label(new GUIContent(name, hint), NameStyle(), GUILayout.Width(92f), GUILayout.Height(32f));
            var spawnTip = Tr("Заспавнить: ", "Spawn: ", "Invocar: ") + name + "\n" + hint;
            if (GUILayout.Button(new GUIContent(Tr("Спавн", "Spawn", "Invocar"), spawnTip), GUILayout.Width(92f), GUILayout.Height(24f)))
                spawn();
            GUILayout.EndVertical();
            Frame(GUILayoutUtility.GetLastRect());
        }

        private static int DrawDropGrid(List<Drop> list, Action ensure, string needle, bool tiers)
        {
            ensure();
            Order.Clear();
            for (var pass = 0; pass < 2; pass++)
            {
                for (var i = 0; i < list.Count; i++)
                {
                    var drop = list[i];
                    if (!DropVisible(drop, needle, tiers))
                        continue;
                    var fav = IsFav(DropKey(drop));
                    if (pass == 0 && fav)
                        Order.Add(i);
                    if (pass == 1 && !fav)
                        Order.Add(i);
                }
            }

            return DrawCards(Order.Count, index =>
            {
                var drop = list[Order[index]];
                DrawCard(drop.Tex, drop.Icon, drop.Name, DropKey(drop), DropTip(drop), () => SpawnDrop(drop));
            });
        }

        private static bool DropVisible(Drop drop, string needle, bool tiers)
        {
            if (drop.Hidden && !_hiddenItems)
                return false;
            if (tiers && !TierOn(drop.Tier))
                return false;
            if (needle.Length > 0 && drop.Name.IndexOf(needle, StringComparison.OrdinalIgnoreCase) < 0)
                return false;
            return true;
        }

        private static string DropKey(Drop drop)
        {
            return "d:" + drop.Name;
        }

        private static int DrawProps(string needle)
        {
            EnsureProps();
            Order.Clear();
            for (var pass = 0; pass < 2; pass++)
            {
                for (var i = 0; i < Props.Count; i++)
                {
                    var prop = Props[i];
                    if (needle.Length > 0 && prop.Name.IndexOf(needle, StringComparison.OrdinalIgnoreCase) < 0)
                        continue;
                    var fav = IsFav("p:" + prop.Name);
                    if (pass == 0 && fav)
                        Order.Add(i);
                    if (pass == 1 && !fav)
                        Order.Add(i);
                }
            }

            return DrawCards(Order.Count, index =>
            {
                var prop = Props[Order[index]];
                DrawCard(prop.Icon, null, prop.Name, "p:" + prop.Name, prop.Name, () => SpawnProp(prop));
            });
        }

        private static string NpcTip(Entry entry)
        {
            var master = entry.Master != null ? entry.Master.GetComponent<CharacterMaster>() : null;
            var body = master != null && master.bodyPrefab != null ? master.bodyPrefab.GetComponent<CharacterBody>() : null;
            var text = body != null ? Describe(body) : null;
            return string.IsNullOrEmpty(text) ? entry.Name : text;
        }

        private static string DropTip(Drop drop)
        {
            var def = PickupCatalog.GetPickupDef(drop.Pickup);
            if (def.itemIndex != ItemIndex.None)
            {
                var item = ItemCatalog.GetItemDef(def.itemIndex);
                if (item != null)
                    return Describe(item);
            }

            if (def.equipmentIndex != EquipmentIndex.None)
            {
                var gear = EquipmentCatalog.GetEquipmentDef(def.equipmentIndex);
                if (gear != null)
                    return Describe(gear);
            }

            return drop.Name;
        }

        private static string Describe(object arg)
        {
            var type = Type.GetType("RoRCheatPanel.GameUtil, RoRCheatPanel");
            if (type == null || arg == null)
                return "";
            foreach (var info in type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static))
            {
                if (info.Name != "Description")
                    continue;
                var parameters = info.GetParameters();
                if (parameters.Length == 1 && parameters[0].ParameterType.IsInstanceOfType(arg))
                    return info.Invoke(null, new[] { arg }) as string ?? "";
            }

            return "";
        }

        private static void DrawPortrait(Texture texture, Sprite sprite)
        {
            var rect = GUILayoutUtility.GetRect(40f, 40f, GUILayout.Width(40f), GUILayout.Height(40f));
            if (sprite != null && sprite.texture != null)
            {
                var area = sprite.textureRect;
                var uv = new Rect(
                    area.x / sprite.texture.width,
                    area.y / sprite.texture.height,
                    area.width / sprite.texture.width,
                    area.height / sprite.texture.height);
                GUI.DrawTextureWithTexCoords(rect, sprite.texture, uv, true);
            }
            else if (texture != null)
            {
                GUI.DrawTexture(rect, texture, ScaleMode.ScaleToFit, true);
            }
            else if (Event.current.type == EventType.Repaint)
            {
                GUI.DrawTexture(rect, Gem(), ScaleMode.ScaleToFit, true);
            }
        }

        private static Texture2D _pixel;
        private static Texture2D _gem;

        private static void Frame(Rect rect)
        {
            if (Event.current.type != EventType.Repaint || rect.width < 2f)
                return;
            var prev = GUI.color;
            GUI.color = new Color(0.62f, 0.72f, 0.98f, 0.95f);
            var pixel = Pixel();
            const float edge = 1f;
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
            for (var i = 0; i < pixels.Length; i++)
                pixels[i] = new Color(0f, 0f, 0f, 0f);
            for (var y = 0; y < n; y++)
            {
                for (var x = 0; x < n; x++)
                {
                    var dx = Mathf.Abs(x - 15.5f);
                    var dy = Mathf.Abs(y - 15.5f);
                    if (dx / 11f + dy / 14f > 1f)
                        continue;
                    var border = dx / 11f + dy / 14f > 0.82f;
                    pixels[y * n + x] = border
                        ? new Color(0.75f, 0.9f, 1f, 1f)
                        : Color.Lerp(new Color(0.25f, 0.15f, 0.55f, 1f), new Color(0.35f, 0.85f, 0.95f, 1f), y / 31f);
                }
            }

            _gem.SetPixels(pixels);
            _gem.Apply();
            _gem.hideFlags = HideFlags.HideAndDontSave;
            return _gem;
        }

        private static bool Toggle(bool value, string label)
        {
            var prev = GUI.backgroundColor;
            if (value)
                GUI.backgroundColor = new Color(0.42f, 0.72f, 0.48f);
            if (GUILayout.Button(label, GUILayout.Height(26f)))
                value = !value;
            GUI.backgroundColor = prev;
            return value;
        }

        private static void EnsureCatalog()
        {
            if (!BodyCatalog.availability.available)
                return;
            if (_catalogCount == BodyCatalog.bodyCount && Catalog.Count > 0)
                return;

            Catalog.Clear();
            _catalogCount = BodyCatalog.bodyCount;
            for (var i = 0; i < BodyCatalog.bodyCount; i++)
            {
                var body = BodyCatalog.GetBodyPrefabBodyComponent((BodyIndex)i);
                if (body == null)
                    continue;
                var team = body.teamComponent != null ? body.teamComponent.teamIndex : TeamIndex.None;
                var masterIndex = MasterCatalog.FindAiMasterIndexForBody(body.bodyIndex);
                if (!masterIndex.isValid)
                    continue;
                var prefab = MasterCatalog.GetMasterPrefab(masterIndex);
                if (prefab == null)
                    continue;
                if (team == TeamIndex.None)
                    team = TeamIndex.Monster;
                var name = Pretty(body);
                Catalog.Add(new Entry
                {
                    Name = name,
                    Master = prefab,
                    Icon = body.portraitIcon,
                    Team = team,
                    Kind = Classify(body, team)
                });
            }

            Catalog.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase));
        }

        private static void Spawn(Entry entry)
        {
            if (!NetworkServer.active)
            {
                Toast(Tr("Спавн только у хоста", "Only the host can spawn", "Solo el anfitrión puede invocar"));
                return;
            }

            var local = LocalUserManager.GetFirstLocalUser();
            var body = local != null ? local.cachedBody : null;
            if (body == null || entry.Master == null)
            {
                Toast(Tr("Нет тела", "No body", "No hay cuerpo"));
                return;
            }

            var forward = FlatForward(body);
            var spawned = 0;
            for (var i = 0; i < _count; i++)
            {
                var pos = PlacePoint(body, i, 8f, 0.2f);
                var summon = new MasterSummon
                {
                    masterPrefab = entry.Master,
                    position = pos,
                    rotation = Quaternion.LookRotation(forward.sqrMagnitude > 0.01f ? -forward : Vector3.forward),
                    ignoreTeamMemberLimit = true,
                    useAmbientLevel = true,
                    teamIndexOverride = SpawnTeam(entry)
                };
                var master = summon.Perform();
                if (master == null)
                    continue;
                Apply(master, _noAi, _noLoot, _elite);
                Fresh.Add(new Pending
                {
                    Master = master,
                    Frames = 12,
                    NoAi = _noAi,
                    NoLoot = _noLoot,
                    Elite = _elite
                });
                Remember(master.gameObject);
                spawned++;
            }

            Toast(Tr("Заспавнено: ", "Spawned: ", "Invocados: ") + spawned);
        }

        public static void RainBosses(int count)
        {
            if (!NetworkServer.active)
            {
                Toast(Tr("Спавн только у хоста", "Only the host can spawn", "Solo el anfitrión puede invocar"));
                return;
            }

            EnsureCatalog();
            var bosses = new List<Entry>();
            for (var i = 0; i < Catalog.Count; i++)
            {
                if (Catalog[i].Kind == NpcKind.Boss && Catalog[i].Master != null)
                    bosses.Add(Catalog[i]);
            }

            var body = HostBody();
            if (body == null)
                return;
            if (bosses.Count == 0)
            {
                Toast(Tr("В каталоге нет чемпионов", "No champions are in the catalog", "No hay campeones en el catálogo"));
                return;
            }

            count = Mathf.Clamp(count, 1, 8);
            var forward = FlatForward(body);
            var spawned = 0;
            for (var i = 0; i < count; i++)
            {
                var entry = bosses[UnityEngine.Random.Range(0, bosses.Count)];
                var angle = i * Mathf.PI * 2f / count;
                var pos = body.footPosition + new Vector3(Mathf.Cos(angle) * 12f, 0.4f, Mathf.Sin(angle) * 12f);
                var summon = new MasterSummon
                {
                    masterPrefab = entry.Master,
                    position = pos,
                    rotation = Quaternion.LookRotation(forward.sqrMagnitude > 0.01f ? -forward : Vector3.forward),
                    ignoreTeamMemberLimit = true,
                    useAmbientLevel = true,
                    teamIndexOverride = TeamIndex.Monster
                };
                var master = summon.Perform();
                if (master == null)
                    continue;
                Apply(master, false, false, 0);
                Fresh.Add(new Pending
                {
                    Master = master,
                    Frames = 12,
                    NoAi = false,
                    NoLoot = false,
                    Elite = 0
                });
                Remember(master.gameObject);
                spawned++;
            }

            Toast(Tr("Дождь боссов: ", "Boss rain: ", "Lluvia de jefes: ") + spawned);
        }

        private static void Apply(CharacterMaster master, bool noAi, bool noLoot, int elite)
        {
            if (master == null)
                return;
            if (noAi)
            {
                var ai = master.GetComponent<BaseAI>();
                if (ai != null)
                    ai.enabled = false;
                var drivers = master.GetComponents<AISkillDriver>();
                for (var i = 0; i < drivers.Length; i++)
                {
                    if (drivers[i] != null)
                        drivers[i].enabled = false;
                }

                SilenceMachines(master.gameObject);
            }

            var body = master.GetBody();
            if (body == null)
                return;
            if (noAi)
            {
                SilenceMachines(body.gameObject);
                var behaviours = body.GetComponents<MonoBehaviour>();
                for (var i = 0; i < behaviours.Length; i++)
                {
                    var behaviour = behaviours[i];
                    if (behaviour == null)
                        continue;
                    var typeName = behaviour.GetType().Name;
                    if (typeName.IndexOf("Detonate", StringComparison.Ordinal) >= 0
                        || typeName.IndexOf("Suicide", StringComparison.Ordinal) >= 0
                        || typeName.IndexOf("Explode", StringComparison.Ordinal) >= 0)
                        behaviour.enabled = false;
                }
            }

            if (elite != 0)
                FunExtras.PaintSpawned(body, elite);
            if (!noLoot)
                return;
            var rewards = body.GetComponent<DeathRewards>();
            if (rewards == null)
                return;
            rewards.goldReward = 0;
            rewards.expReward = 0;
            rewards.bossDropTable = null;
        }

        private static void SilenceMachines(GameObject owner)
        {
            var machines = owner.GetComponents<EntityStateMachine>();
            for (var i = 0; i < machines.Length; i++)
            {
                var machine = machines[i];
                if (machine == null)
                    continue;
                if (machine.customName == "AI" || machine.customName == "Weapon")
                    machine.enabled = false;
            }
        }

        private static NpcKind Classify(CharacterBody body, TeamIndex team)
        {
            if (IsSurvivor(body))
                return NpcKind.Survivor;
            if (body.isChampion)
                return NpcKind.Boss;
            var name = body.name ?? "";
            if (team == TeamIndex.Player
                || name.IndexOf("Drone", StringComparison.OrdinalIgnoreCase) >= 0
                || name.IndexOf("Turret", StringComparison.OrdinalIgnoreCase) >= 0)
                return NpcKind.Drone;
            return NpcKind.Grunt;
        }

        private static bool IsSurvivor(CharacterBody body)
        {
            var defs = SurvivorCatalog.allSurvivorDefs;
            if (defs == null || body == null)
                return false;
            var prefab = BodyCatalog.GetBodyPrefab(body.bodyIndex);
            foreach (var survivor in defs)
            {
                if (survivor != null && survivor.bodyPrefab == prefab)
                    return true;
            }

            return false;
        }

        private static TeamIndex SpawnTeam(Entry entry)
        {
            if (_allies)
                return TeamIndex.Player;
            if (entry.Team == TeamIndex.None || entry.Team == TeamIndex.Player)
                return TeamIndex.Monster;
            return entry.Team;
        }

        private static void EnsureItems()
        {
            if (!ItemCatalog.availability.available)
                return;
            if (_itemCount == ItemCatalog.itemCount && Items.Count > 0)
                return;
            Items.Clear();
            _itemCount = ItemCatalog.itemCount;
            for (var i = 0; i < ItemCatalog.itemCount; i++)
            {
                var def = ItemCatalog.GetItemDef((ItemIndex)i);
                if (def == null)
                    continue;
                var pickup = PickupCatalog.FindPickupIndex(def.itemIndex);
                if (!pickup.isValid)
                    continue;
                Items.Add(new Drop
                {
                    Name = TokenName(def.nameToken, def.name),
                    Icon = def.pickupIconSprite,
                    Tex = def.pickupIconTexture,
                    Pickup = pickup,
                    Hidden = def.hidden,
                    Tier = (int)def.tier
                });
            }

            Items.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase));
        }

        private static void EnsureGear()
        {
            if (!EquipmentCatalog.availability.available)
                return;
            if (_gearCount == EquipmentCatalog.equipmentCount && Gear.Count > 0)
                return;
            Gear.Clear();
            _gearCount = EquipmentCatalog.equipmentCount;
            for (var i = 0; i < EquipmentCatalog.equipmentCount; i++)
            {
                var def = EquipmentCatalog.GetEquipmentDef((EquipmentIndex)i);
                if (def == null)
                    continue;
                var pickup = PickupCatalog.FindPickupIndex(def.equipmentIndex);
                if (!pickup.isValid)
                    continue;
                Gear.Add(new Drop
                {
                    Name = TokenName(def.nameToken, def.name),
                    Icon = def.pickupIconSprite,
                    Tex = def.pickupIconTexture,
                    Pickup = pickup
                });
            }

            Gear.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase));
        }

        private static void EnsureProps()
        {
            if (_propsReady && Props.Count > 0)
                return;
            Props.Clear();
            var seen = new HashSet<string>();
            var cards = Resources.FindObjectsOfTypeAll<InteractableSpawnCard>();
            for (var i = 0; i < cards.Length; i++)
            {
                var card = cards[i];
                if (card == null || card.prefab == null)
                    continue;
                var raw = card.name ?? "";
                if (raw.Length == 0 || !seen.Add(raw))
                    continue;
                Props.Add(new Prop
                {
                    Name = PropName(raw),
                    Card = card,
                    Icon = PropIcon(card.prefab)
                });
            }

            Props.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase));
            _propsReady = Props.Count > 0;
        }

        private static void SpawnDrop(Drop drop)
        {
            var body = HostBody();
            if (body == null)
                return;
            var before = SceneIds<PickupDropletController>();
            var beforePickups = SceneIds<GenericPickupController>();
            var spawned = 0;
            for (var i = 0; i < _count; i++)
            {
                var pos = PlacePoint(body, i, 4f, 3.2f);
                PickupDropletController.CreatePickupDroplet(drop.Pickup, pos, Vector3.up * 6f);
                spawned++;
            }

            RememberFresh<PickupDropletController>(before);
            RememberFresh<GenericPickupController>(beforePickups);
            DropWatches.Add(new DropWatch
            {
                Droplets = before,
                Pickups = beforePickups,
                Frames = 120
            });

            Toast(Tr("Упало: ", "Dropped: ", "Caído: ") + spawned);
        }

        private static void SpawnProp(Prop prop)
        {
            var body = HostBody();
            if (body == null || prop.Card == null)
                return;
            var rng = RoR2Application.rng;
            var spawned = 0;
            for (var i = 0; i < _count; i++)
            {
                var pos = PlacePoint(body, i, 7f, 1f);
                var rule = new DirectorPlacementRule
                {
                    placementMode = DirectorPlacementRule.PlacementMode.Direct,
                    position = pos
                };
                var request = new DirectorSpawnRequest(prop.Card, rule, rng);
                GameObject instance = null;
                if (DirectorCore.instance != null)
                    instance = DirectorCore.instance.TrySpawnObject(request);
                if (instance == null)
                    instance = prop.Card.DoSpawn(pos, Quaternion.identity, request).spawnedInstance;
                if (instance != null)
                {
                    Remember(instance);
                    spawned++;
                }
            }

            Toast(Tr("Поставлено: ", "Placed: ", "Puesto: ") + spawned);
        }

        private static CharacterBody HostBody()
        {
            if (!NetworkServer.active)
            {
                Toast(Tr("Спавн только у хоста", "Only the host can spawn", "Solo el anfitrión puede invocar"));
                return null;
            }

            var local = LocalUserManager.GetFirstLocalUser();
            var body = local != null ? local.cachedBody : null;
            if (body == null)
                Toast(Tr("Нет тела", "No body", "No hay cuerpo"));
            return body;
        }

        private static Vector3 FlatForward(CharacterBody body)
        {
            var forward = body.inputBank != null ? body.inputBank.aimDirection : body.transform.forward;
            forward.y = 0f;
            if (forward.sqrMagnitude < 0.01f)
                forward = Vector3.forward;
            return forward.normalized;
        }

        private static Vector3 PlacePoint(CharacterBody body, int index, float distance, float lift)
        {
            var forward = FlatForward(body);
            var right = Vector3.Cross(Vector3.up, forward);
            if (_place == 1)
            {
                var side = (index - (_count - 1) * 0.5f) * 1.5f;
                return body.footPosition + right * side + Vector3.up * lift;
            }

            if (_place == 2)
            {
                var angle = _count <= 1 ? 0f : index * Mathf.PI * 2f / _count;
                return body.footPosition + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * distance + Vector3.up * lift;
            }

            var spread = (index - (_count - 1) * 0.5f) * 2.6f;
            return body.footPosition + forward * distance + right * spread + Vector3.up * lift;
        }

        private static void Remember(GameObject go)
        {
            if (go != null)
                Spawned.Add(go);
        }

        private static HashSet<int> SceneIds<T>() where T : Component
        {
            var ids = new HashSet<int>();
            var found = UnityEngine.Object.FindObjectsOfType<T>();
            for (var i = 0; i < found.Length; i++)
            {
                if (found[i] != null)
                    ids.Add(found[i].GetInstanceID());
            }

            return ids;
        }

        private static void RememberNew<T>(HashSet<int> before) where T : Component
        {
            RememberFresh<T>(before);
        }

        private struct DropWatch
        {
            public HashSet<int> Droplets;
            public HashSet<int> Pickups;
            public int Frames;
        }

        private static void WatchDrops()
        {
            for (var i = DropWatches.Count - 1; i >= 0; i--)
            {
                var watch = DropWatches[i];
                if (watch.Droplets != null)
                    RememberFresh<PickupDropletController>(watch.Droplets);
                if (watch.Pickups != null)
                    RememberFresh<GenericPickupController>(watch.Pickups);
                watch.Frames--;
                if (watch.Frames <= 0)
                    DropWatches.RemoveAt(i);
                else
                    DropWatches[i] = watch;
            }
        }

        private static void RememberFresh<T>(HashSet<int> before) where T : Component
        {
            if (before == null)
                return;
            var found = UnityEngine.Object.FindObjectsOfType<T>();
            for (var i = 0; i < found.Length; i++)
            {
                var item = found[i];
                if (item == null)
                    continue;
                if (!before.Add(item.GetInstanceID()))
                    continue;
                Remember(item.gameObject);
            }
        }

        private static void DestroySpawnedObject(GameObject go)
        {
            if (go == null)
                return;
            var master = go.GetComponent<CharacterMaster>();
            if (master == null)
            {
                var asBody = go.GetComponent<CharacterBody>();
                if (asBody != null)
                    master = asBody.master;
            }

            if (master == null)
            {
                DestroyNet(go);
                return;
            }

            var extras = new List<CharacterMaster>();
            var all = UnityEngine.Object.FindObjectsOfType<CharacterMaster>();
            for (var i = 0; i < all.Length; i++)
            {
                var other = all[i];
                if (other == null || other == master)
                    continue;
                var link = other.minionOwnership;
                if (link != null && link.ownerMaster == master)
                    extras.Add(other);
            }

            for (var i = 0; i < extras.Count; i++)
                DestroyMaster(extras[i]);
            DestroyMaster(master);
        }

        private static void DestroyMaster(CharacterMaster master)
        {
            if (master == null)
                return;
            var body = master.GetBody();
            if (body != null)
                DestroyNet(body.gameObject);
            DestroyNet(master.gameObject);
        }

        private static void DestroyNet(GameObject go)
        {
            if (go == null)
                return;
            var identity = go.GetComponent<NetworkIdentity>();
            if (identity != null && NetworkServer.active)
                NetworkServer.Destroy(go);
            else
                UnityEngine.Object.Destroy(go);
        }

        private static void ClearSpawned()
        {
            if (!NetworkServer.active)
            {
                Toast(Tr("Спавн только у хоста", "Only the host can spawn", "Solo el anfitrión puede invocar"));
                return;
            }

            var removed = 0;
            for (var i = 0; i < Spawned.Count; i++)
            {
                var go = Spawned[i];
                if (go == null)
                    continue;
                removed++;
                DestroySpawnedObject(go);
            }

            Spawned.Clear();
            Fresh.Clear();
            DropWatches.Clear();
            Toast(Tr("Убрано: ", "Removed: ", "Quitado: ") + removed);
        }

        private static bool TierOn(int tier)
        {
            switch (tier)
            {
                case 0:
                    return _tWhite;
                case 1:
                    return _tGreen;
                case 2:
                    return _tRed;
                case 3:
                    return _tLunar;
                case 4:
                    return _tBoss;
                case 6:
                case 7:
                case 8:
                case 9:
                    return _tVoid;
                default:
                    return _tOther;
            }
        }

        private static void Star(string key)
        {
            LoadFavs();
            var on = Favs.Contains(key);
            var prev = GUI.backgroundColor;
            if (on)
                GUI.backgroundColor = new Color(0.9f, 0.72f, 0.25f);
            if (GUILayout.Button(on ? "★" : "☆", GUILayout.Width(28f), GUILayout.Height(28f)))
            {
                if (on)
                    Favs.Remove(key);
                else
                    Favs.Add(key);
                SaveFavs();
            }

            GUI.backgroundColor = prev;
        }

        private static bool IsFav(string key)
        {
            LoadFavs();
            return Favs.Contains(key);
        }

        private static void LoadFavs()
        {
            if (_favsLoaded)
                return;
            _favsLoaded = true;
            var raw = PlayerPrefs.GetString("RoRCheatPanel.SpawnFavs", "");
            var parts = raw.Split('\n');
            for (var i = 0; i < parts.Length; i++)
            {
                if (parts[i].Length > 0)
                    Favs.Add(parts[i]);
            }
        }

        private static void SaveFavs()
        {
            var builder = new StringBuilder();
            foreach (var key in Favs)
            {
                if (builder.Length > 0)
                    builder.Append('\n');
                builder.Append(key.Replace('\n', ' '));
            }

            PlayerPrefs.SetString("RoRCheatPanel.SpawnFavs", builder.ToString());
        }

        private static GUIStyle _nameStyle;

        private static GUIStyle NameStyle()
        {
            if (_nameStyle == null)
            {
                _nameStyle = new GUIStyle(GUI.skin.label)
                {
                    wordWrap = true,
                    alignment = TextAnchor.UpperCenter,
                    fontSize = 11
                };
            }

            return _nameStyle;
        }

        private static string PropName(string raw)
        {
            var rules = new[]
            {
                new[] { "GoldChest", "Золотой сундук", "Gold chest", "Cofre de oro" },
                new[] { "LunarChest", "Лунный сундук", "Lunar chest", "Cofre lunar" },
                new[] { "CategoryChest", "Сундук выбора", "Category chest", "Cofre de categoría" },
                new[] { "Chest2", "Большой сундук", "Large chest", "Cofre grande" },
                new[] { "EquipmentBarrel", "Бочка снаряжения", "Equipment barrel", "Barril de equipo" },
                new[] { "TripleShop", "Тройной магазин", "Triple shop", "Tienda triple" },
                new[] { "Scrapper", "Утилизатор", "Scrapper", "Reciclador" },
                new[] { "ShrineChance", "Святилище удачи", "Shrine of chance", "Santuario del azar" },
                new[] { "ShrineBlood", "Святилище крови", "Shrine of blood", "Santuario de sangre" },
                new[] { "ShrineCombat", "Святилище боя", "Shrine of combat", "Santuario de combate" },
                new[] { "ShrineBoss", "Святилище горы", "Shrine of the mountain", "Santuario de la montaña" },
                new[] { "ShrineRestack", "Святилище порядка", "Shrine of order", "Santuario del orden" },
                new[] { "ShrineCleanse", "Святилище очищения", "Shrine of cleansing", "Santuario de limpieza" },
                new[] { "ShrineHealing", "Святилище лечения", "Shrine of healing", "Santuario de curación" },
                new[] { "Casino", "Казино", "Casino", "Casino" },
                new[] { "Duplicator", "Принтер", "Printer", "Impresora" },
                new[] { "Printer", "Принтер", "Printer", "Impresora" },
                new[] { "Turret", "Турель", "Turret", "Torreta" },
                new[] { "Drone", "Дрон", "Drone", "Dron" },
                new[] { "Chest", "Сундук", "Chest", "Cofre" },
                new[] { "Barrel", "Бочка", "Barrel", "Barril" },
                new[] { "Shrine", "Святилище", "Shrine", "Santuario" }
            };
            for (var i = 0; i < rules.Length; i++)
            {
                if (raw.IndexOf(rules[i][0], StringComparison.OrdinalIgnoreCase) >= 0)
                    return Tr(rules[i][1], rules[i][2], rules[i][3]);
            }

            return CardTitle(raw);
        }

        private static Texture PropIcon(GameObject prefab)
        {
            if (prefab == null)
                return null;
            var body = prefab.GetComponentInChildren<CharacterBody>(true);
            if (body != null && body.portraitIcon != null)
                return body.portraitIcon;
            var renderers = prefab.GetComponentsInChildren<Renderer>(true);
            for (var i = 0; i < renderers.Length; i++)
            {
                var material = renderers[i] != null ? renderers[i].sharedMaterial : null;
                if (material != null && material.mainTexture != null)
                    return material.mainTexture;
            }

            return null;
        }

        private static string TokenName(string token, string fallback)
        {
            var text = Translated(token);
            if (!string.IsNullOrEmpty(text))
                return text;
            return CardTitle(fallback);
        }

        private static string CardTitle(string raw)
        {
            if (string.IsNullOrEmpty(raw))
                return "?";
            if (raw.StartsWith("isc", StringComparison.OrdinalIgnoreCase))
                raw = raw.Substring(3);
            return Spaces(raw.Replace('_', ' ')).Trim();
        }

        private static string[] SubNames(int cat)
        {
            switch (cat)
            {
                case 0:
                    return new[] { L("tab_items"), L("tab_equip"), L("tab_presets") };
                case 1:
                    return new[] { L("tab_skills"), L("tab_chars"), L("tab_ach") };
                case 2:
                    return new[] { L("tab_run"), L("tab_stages"), L("tab_teams"), L("tab_players") };
                default:
                    return null;
            }
        }

        private static void Tab(string name)
        {
            var type = Type.GetType("RoRCheatPanel.UI.Tabs." + name + ", RoRCheatPanel");
            type?.GetMethod("Draw", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)?.Invoke(null, null);
        }

        private static int Pills(string[] names, int selected, float height)
        {
            var helpers = Type.GetType("RoRCheatPanel.UI.UiHelpers, RoRCheatPanel");
            var method = helpers != null
                ? helpers.GetMethod("DrawPills", BindingFlags.Public | BindingFlags.Static, null, new[] { typeof(string[]), typeof(int), typeof(float) }, null)
                : null;
            var result = method != null ? method.Invoke(null, new object[] { names, selected, height }) : null;
            return result is int value ? value : selected;
        }

        private static Type WindowType()
        {
            return Type.GetType("RoRCheatPanel.UI.CheatWindow, RoRCheatPanel");
        }

        private static int LocGeneration()
        {
            var loc = Type.GetType("RoRCheatPanel.Loc, RoRCheatPanel");
            var field = loc != null ? loc.GetField("Generation", BindingFlags.Public | BindingFlags.Static) : null;
            return field != null && field.GetValue(null) is int value ? value : 0;
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

        private static string Pretty(CharacterBody body)
        {
            var token = body.baseNameToken;
            var text = Translated(token);
            if (!Usable(text) || text == token)
                text = Spaces(TrimBody(body.name));
            return text;
        }

        private static string Translated(string token)
        {
            if (string.IsNullOrEmpty(token))
                return "";
            var util = Type.GetType("RoRCheatPanel.GameUtil, RoRCheatPanel");
            var lang = util != null
                ? util.GetMethod("Lang", BindingFlags.Public | BindingFlags.Static, null, new[] { typeof(string) }, null)
                : null;
            var text = lang != null ? lang.Invoke(null, new object[] { token }) as string : null;
            if (Usable(text) && text != token)
                return text;
            text = Language.GetString(token);
            return Usable(text) && text != token ? text : "";
        }

        private static bool Usable(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return false;
            if (text == "???")
                return false;
            if (text.IndexOf("BODY_NAME", StringComparison.Ordinal) >= 0)
                return false;
            if (text.IndexOf("_NAME", StringComparison.Ordinal) >= 0)
                return false;
            return true;
        }

        private static string TrimBody(string name)
        {
            if (string.IsNullOrEmpty(name))
                return "";
            if (name.EndsWith("Body", StringComparison.Ordinal))
                name = name.Substring(0, name.Length - 4);
            return name.Replace('_', ' ').Trim();
        }

        private static string Spaces(string raw)
        {
            if (string.IsNullOrEmpty(raw))
                return "";
            var builder = new StringBuilder(raw.Length + 8);
            for (var i = 0; i < raw.Length; i++)
            {
                var letter = raw[i];
                if (i > 0 && char.IsUpper(letter) && char.IsLower(raw[i - 1]))
                    builder.Append(' ');
                builder.Append(letter);
            }

            return builder.ToString();
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
