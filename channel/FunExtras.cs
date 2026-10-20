using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace RoRCheatPanel.Channel
{
    internal static class FunExtras
    {
        private const int Fire = 1;
        private const int Lightning = 2;
        private const int Ice = 3;

        private static readonly Dictionary<string, int> EliteByUser = new Dictionary<string, int>();
        private static readonly Dictionary<string, GameObject> OriginalBodies = new Dictionary<string, GameObject>();
        private static Vector2 _funScroll;
        private static readonly Dictionary<int, string> IntBuffers = new Dictionary<int, string>();
        private static readonly List<GameObject> FireworkPrefabs = new List<GameObject>();
        private static bool _fireworksSearched;

        private static FieldInfo _eliteIndexField;
        private static FieldInfo _eliteRampField;
        private static FieldInfo _materialsDirtyField;
        private static MethodInfo _updateMaterials;
        private static EliteDef _fire;
        private static EliteDef _lightning;
        private static EliteDef _ice;
        private static bool _elitesReady;
        private static float _bossScale = 1f;
        private static bool _bossScaleSaved;

        private static readonly string[] SaluteRu =
        {
            "<color=#ffd36a>Салют. Красиво и без урона.</color>",
            "<color=#9ad7ff>Это не атака. Это настроение.</color>",
            "<color=#ff9ad2>Хост зажёг небо.</color>"
        };

        private static readonly string[] SaluteEn =
        {
            "<color=#ffd36a>Fireworks. Pretty, and no damage.</color>",
            "<color=#9ad7ff>This is not an attack. This is a mood.</color>",
            "<color=#ff9ad2>The host lit up the sky.</color>"
        };

        private static readonly string[] SaluteEs =
        {
            "<color=#ffd36a>Fuegos. Bonito y sin daño.</color>",
            "<color=#9ad7ff>No es un ataque. Es el ambiente.</color>",
            "<color=#ff9ad2>El anfitrión encendió el cielo.</color>"
        };

        public static void Install(Harmony harmony)
        {
            var fun = Type.GetType("RoRCheatPanel.UI.Tabs.FunTab, RoRCheatPanel");
            var draw = fun != null
                ? fun.GetMethod("Draw", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)
                : null;
            if (draw == null)
            {
                Debug.LogWarning("[RoRCheatPanel.Channel] missing FunTab.Draw for extras");
                return;
            }

            harmony.Patch(
                draw,
                prefix: new HarmonyMethod(typeof(FunExtras), nameof(BeginFunScroll)),
                postfix: new HarmonyMethod(typeof(FunExtras), nameof(EndFunScroll)),
                transpiler: new HarmonyMethod(typeof(FunExtras), nameof(InsertExtras)));

            var helpers = Type.GetType("RoRCheatPanel.UI.UiHelpers, RoRCheatPanel");
            var intField = helpers != null
                ? helpers.GetMethod("IntField", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)
                : null;
            if (intField != null)
                harmony.Patch(intField, prefix: new HarmonyMethod(typeof(FunExtras), nameof(DrawIntField)));

            var give = GiveMoneyMethod();
            if (give != null)
            {
                try
                {
                    harmony.Patch(give, prefix: new HarmonyMethod(typeof(FunExtras), nameof(GiveGold)));
                }
                catch (Exception ex)
                {
                    Debug.LogError("[RoRCheatPanel.Channel] gold patch failed: " + ex);
                }
            }

            var tools = Type.GetType("RoRCheatPanel.UI.Tabs.ToolsTab, RoRCheatPanel");
            var money = tools != null
                ? tools.GetMethod("DrawMoney", BindingFlags.NonPublic | BindingFlags.Static)
                : null;
            if (money != null)
                harmony.Patch(money, prefix: new HarmonyMethod(typeof(FunExtras), nameof(DrawMoneyBeside)));

            var runtime = Type.GetType("RoRCheatPanel.GameUtil, RoRCheatPanel");
            var boss = runtime != null
                ? runtime.GetMethod("BecomeBoss", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)
                : null;
            if (boss != null)
            {
                harmony.Patch(
                    boss,
                    prefix: new HarmonyMethod(typeof(FunExtras), nameof(BeforeBoss)),
                    postfix: new HarmonyMethod(typeof(FunExtras), nameof(AfterBoss)));
            }
        }

        private static bool DrawIntField(string label, int value, int min, int max, int id, ref int __result)
        {
            if (!IntBuffers.TryGetValue(id, out var text))
                text = value.ToString();

            var button = new GUIStyle(GUI.skin.button)
            {
                alignment = TextAnchor.MiddleCenter,
                fixedWidth = 28f,
                fixedHeight = 24f,
                margin = new RectOffset(2, 2, 1, 1),
                padding = new RectOffset(0, 0, 0, 0)
            };

            GUILayout.BeginHorizontal();
            GUILayout.Label(label, GUILayout.Width(90f), GUILayout.Height(24f));
            text = GUILayout.TextField(text ?? "", GUILayout.Width(128f), GUILayout.Height(24f));
            if (int.TryParse(text, out var parsed))
                value = Mathf.Clamp(parsed, min, max);
            if (GUILayout.Button("-", button))
            {
                value = Mathf.Max(min, value - 1);
                text = value.ToString();
            }
            if (GUILayout.Button("+", button))
            {
                value = Mathf.Min(max, value + 1);
                text = value.ToString();
            }
            if (id == 20 || id == 21 || id == 22)
            {
                var give = new GUIStyle(GUI.skin.button)
                {
                    alignment = TextAnchor.MiddleCenter,
                    fixedHeight = 24f,
                    margin = new RectOffset(8, 2, 1, 1),
                    padding = new RectOffset(10, 10, 0, 0)
                };
                var caption = id == 20 ? L("give_gold") : id == 21 ? L("give_lunar") : L("set_level");
                if (GUILayout.Button(caption, give))
                    ApplyMoney(id, value);
            }
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            IntBuffers[id] = text;
            __result = value;
            return false;
        }

        private static bool DrawMoneyBeside()
        {
            var tab = Type.GetType("RoRCheatPanel.UI.Tabs.ToolsTab, RoRCheatPanel");
            var helpers = Type.GetType("RoRCheatPanel.UI.UiHelpers, RoRCheatPanel");
            var intField = helpers != null
                ? helpers.GetMethod("IntField", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)
                : null;
            var moneyField = tab != null ? tab.GetField("_money", BindingFlags.NonPublic | BindingFlags.Static) : null;
            var lunarField = tab != null ? tab.GetField("_lunar", BindingFlags.NonPublic | BindingFlags.Static) : null;
            var levelField = tab != null ? tab.GetField("_level", BindingFlags.NonPublic | BindingFlags.Static) : null;
            if (intField == null || moneyField == null || lunarField == null || levelField == null)
                return true;

            GUILayout.BeginVertical("box");
            GUILayout.Label(L("money_level"));
            moneyField.SetValue(null, CallInt(intField, L("gold"), (int)moneyField.GetValue(null), 1, 999999999, 20));
            lunarField.SetValue(null, CallInt(intField, L("lunar"), (int)lunarField.GetValue(null), 1, 999999999, 21));
            levelField.SetValue(null, CallInt(intField, L("level"), (int)levelField.GetValue(null), 1, 999, 22));
            GUILayout.EndVertical();
            return false;
        }

        private static int CallInt(MethodInfo intField, string label, int value, int min, int max, int id)
        {
            var result = intField.Invoke(null, new object[] { label, value, min, max, id });
            return result is int number ? number : value;
        }

        private static MethodInfo GiveMoneyMethod()
        {
            var type = Type.GetType("RoRCheatPanel.GameUtil, RoRCheatPanel");
            if (type == null)
                return null;
            foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static))
            {
                if (method.Name != "GiveMoney")
                    continue;
                var parameters = method.GetParameters();
                if (parameters.Length == 1 && parameters[0].ParameterType == typeof(int))
                    return method;
            }

            return null;
        }

        private static bool GiveGold(int amount)
        {
            if (!NetworkServer.active)
            {
                Toast(Tr("Только хост", "Host only", "Solo el anfitrión"));
                return false;
            }

            try
            {
                var add = (uint)Math.Max(0, amount);
                var masters = new List<CharacterMaster>();
                foreach (var user in Enumerate("Targets"))
                    Remember(masters, MasterOf(user));
                if (masters.Count == 0)
                    Remember(masters, LocalMaster());

                if (masters.Count == 0)
                {
                    Toast(Tr("Нет игрока для золота", "No player to pay", "No hay jugador para el oro"));
                    return false;
                }

                foreach (var master in masters)
                    Pay(master, add);

                var total = masters[0] != null ? masters[0].money : add;
                Toast(Tr("Золото +" + add + " (" + total + ")", "Gold +" + add + " (" + total + ")", "Oro +" + add + " (" + total + ")"));
            }
            catch (Exception ex)
            {
                Debug.LogError("[RoRCheatPanel.Channel] gold failed: " + ex);
                Toast(Tr("Золото не выдалось", "Gold was not given", "El oro no se dio"));
            }

            return false;
        }

        private static CharacterMaster MasterOf(NetworkUser user)
        {
            if (user == null)
                return null;
            if (user.master != null)
                return user.master;
            var body = user.GetCurrentBody();
            if (body != null && body.master != null)
                return body.master;
            var controller = user.masterController;
            if (controller != null && controller.master != null)
                return controller.master;
            return null;
        }

        private static CharacterMaster LocalMaster()
        {
            var local = LocalUserManager.GetFirstLocalUser();
            if (local == null)
                return null;
            if (local.cachedMaster != null)
                return local.cachedMaster;
            if (local.cachedBody != null && local.cachedBody.master != null)
                return local.cachedBody.master;
            return MasterOf(local.currentNetworkUser);
        }

        private static void Remember(List<CharacterMaster> masters, CharacterMaster master)
        {
            if (master == null || masters.Contains(master))
                return;
            masters.Add(master);
        }

        private static bool Pay(CharacterMaster master, uint add)
        {
            if (master == null)
                return false;
            var next = master.money + add;
            if (next < master.money)
                next = uint.MaxValue;
            master.money = next;
            return true;
        }

        private static void ApplyMoney(int id, int value)
        {
            var util = Type.GetType("RoRCheatPanel.GameUtil, RoRCheatPanel");
            if (util == null)
                return;
            if (id == 20)
                GiveGold(value);
            else if (id == 21)
                util.GetMethod("GiveLunar", BindingFlags.Public | BindingFlags.Static)?.Invoke(null, new object[] { (uint)Mathf.Max(0, value) });
            else
                util.GetMethod("SetTeamLevel", BindingFlags.Public | BindingFlags.Static)?.Invoke(null, new object[] { (uint)Mathf.Max(1, value) });
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

        private static void BeginFunScroll()
        {
            if (MenuTidy.OwnsFun)
                return;
            var height = 560f;
            var window = Type.GetType("RoRCheatPanel.UI.CheatWindow, RoRCheatPanel");
            var rectField = window != null
                ? window.GetField("_rect", BindingFlags.NonPublic | BindingFlags.Static)
                : null;
            if (rectField != null && rectField.GetValue(null) is Rect rect)
                height = Mathf.Max(240f, rect.height - 176f);
            _funScroll = GUILayout.BeginScrollView(_funScroll, GUILayout.Height(height));
        }

        private static void EndFunScroll()
        {
            if (MenuTidy.OwnsFun)
                return;
            GUILayout.Space(8f);
            GUILayout.EndScrollView();
        }

        private static void BeforeBoss()
        {
            var scale = Type.GetType("RoRCheatPanel.CheatRuntime, RoRCheatPanel")
                ?.GetField("BodyScale", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            if (scale != null && scale.GetValue(null) is float value)
            {
                _bossScale = value;
                _bossScaleSaved = true;
            }

            foreach (var user in Enumerate("Targets"))
            {
                var key = Key(user);
                if (string.IsNullOrEmpty(key) || OriginalBodies.ContainsKey(key))
                    continue;
                var prefab = user.master != null ? user.master.bodyPrefab : null;
                if (prefab != null)
                    OriginalBodies[key] = prefab;
            }
        }

        private static void AfterBoss()
        {
            SetRuntime("Disco", false);
            if (_bossScaleSaved)
                SetRuntime("BodyScale", _bossScale);
        }

        public static void LateApply()
        {
            if (EliteByUser.Count == 0)
                return;
            EnsureElites();
            foreach (var user in Enumerate("Targets"))
            {
                if (!EliteByUser.TryGetValue(Key(user), out var kind) || kind == 0)
                    continue;
                Paint(user.GetCurrentBody(), DefFor(kind));
            }
        }

        private static IEnumerable<CodeInstruction> InsertExtras(IEnumerable<CodeInstruction> instructions)
        {
            var endVertical = AccessTools.Method(typeof(GUILayout), "EndVertical", Type.EmptyTypes);
            var draw = AccessTools.Method(typeof(FunExtras), nameof(DrawExtras));
            var inserted = false;
            foreach (var ins in instructions)
            {
                if (!inserted && endVertical != null && draw != null && ins.Calls(endVertical))
                {
                    yield return new CodeInstruction(OpCodes.Call, draw);
                    inserted = true;
                }
                yield return ins;
            }
        }

        public static void DrawExtras()
        {
            GUILayout.Space(6f);
            GUILayout.BeginHorizontal();
            EliteButton(Fire, EliteName(Fire), new Color(0.95f, 0.38f, 0.16f));
            EliteButton(Lightning, EliteName(Lightning), new Color(0.35f, 0.55f, 0.98f));
            EliteButton(Ice, EliteName(Ice), new Color(0.55f, 0.9f, 0.95f));
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            if (GUILayout.Button(ActionName("shuffle"), GUILayout.Height(28f)))
                ShuffleItems();
            if (GUILayout.Button(ActionName("gather"), GUILayout.Height(28f)))
                GatherMonsters();
            if (GUILayout.Button(ActionName("salute"), GUILayout.Height(28f)))
                Salute();
            GUILayout.EndHorizontal();
            if (GUILayout.Button(ActionName("reset"), GUILayout.Height(28f)))
                ResetChaos();
        }

        private static void EliteButton(int kind, string label, Color color)
        {
            var prev = GUI.backgroundColor;
            if (TargetsHave(kind))
                GUI.backgroundColor = color;
            if (GUILayout.Button(label, GUILayout.Height(28f)))
                ToggleElite(kind);
            GUI.backgroundColor = prev;
        }

        private static void ToggleElite(int kind)
        {
            if (!RequireHost())
                return;
            EnsureElites();
            if (DefFor(kind) == null)
            {
                Toast(Tr("В каталоге нет такого элитного скина", "That elite skin is not in the catalog", "Ese skin de élite no está en el catálogo"));
                return;
            }

            var users = new List<NetworkUser>(Enumerate("Targets"));
            var allOn = users.Count > 0;
            foreach (var user in users)
            {
                if (!EliteByUser.TryGetValue(Key(user), out var current) || current != kind)
                    allOn = false;
            }

            foreach (var user in users)
            {
                var key = Key(user);
                if (allOn)
                {
                    EliteByUser.Remove(key);
                    Paint(user.GetCurrentBody(), null);
                }
                else
                {
                    EliteByUser[key] = kind;
                    Paint(user.GetCurrentBody(), DefFor(kind));
                }
            }

            Toast(allOn
                ? Tr("Элитный цвет снят", "Elite color off", "Color de élite quitado")
                : Tr("Элитный цвет: ", "Elite color: ", "Color de élite: ") + EliteName(kind));
        }

        private static bool TargetsHave(int kind)
        {
            foreach (var user in Enumerate("Targets"))
            {
                if (EliteByUser.TryGetValue(Key(user), out var current) && current == kind)
                    return true;
            }

            return false;
        }

        private static void Paint(CharacterBody body, EliteDef def)
        {
            if (body == null || body.modelLocator == null || body.modelLocator.modelTransform == null)
                return;
            var model = body.modelLocator.modelTransform.GetComponent<CharacterModel>()
                ?? body.modelLocator.modelTransform.GetComponentInChildren<CharacterModel>();
            if (model == null)
                return;

            _eliteIndexField = _eliteIndexField ?? AccessTools.Field(typeof(CharacterModel), "myEliteIndex");
            _eliteRampField = _eliteRampField ?? AccessTools.Field(typeof(CharacterModel), "shaderEliteRampIndex");
            _materialsDirtyField = _materialsDirtyField ?? AccessTools.Field(typeof(CharacterModel), "materialsDirty");
            _updateMaterials = _updateMaterials ?? AccessTools.Method(typeof(CharacterModel), "UpdateMaterials");
            if (_eliteIndexField == null || _updateMaterials == null)
                return;

            _eliteIndexField.SetValue(model, def != null ? (object)def.eliteIndex : EliteIndex.None);
            _eliteRampField?.SetValue(model, def != null ? def.shaderEliteRampIndex : 0);
            _materialsDirtyField?.SetValue(model, true);
            _updateMaterials.Invoke(model, null);
        }

        private static void EnsureElites()
        {
            if (_elitesReady)
                return;
            _elitesReady = true;
            var field = AccessTools.Field(typeof(EliteCatalog), "eliteDefs");
            if (!(field?.GetValue(null) is IEnumerable defs))
                return;
            foreach (var item in defs)
            {
                if (!(item is EliteDef def) || def.eliteEquipmentDef == null)
                    continue;
                var name = ((UnityEngine.Object)def.eliteEquipmentDef).name ?? "";
                var token = def.modifierToken ?? "";
                if (_fire == null && Matches(name, token, "Fire", "FIRE"))
                    _fire = def;
                else if (_lightning == null && Matches(name, token, "Lightning", "LIGHTNING"))
                    _lightning = def;
                else if (_ice == null && Matches(name, token, "Ice", "ICE"))
                    _ice = def;
            }
        }

        private static bool Matches(string name, string token, string equip, string modifier)
        {
            return name.IndexOf(equip, StringComparison.OrdinalIgnoreCase) >= 0
                || token.IndexOf(modifier, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        public static void PaintSpawned(CharacterBody body, int kind)
        {
            if (body == null || kind == 0)
                return;
            EnsureElites();
            Paint(body, DefFor(kind));
        }

        private static EliteDef DefFor(int kind)
        {
            EnsureElites();
            if (kind == Fire)
                return _fire;
            if (kind == Lightning)
                return _lightning;
            if (kind == Ice)
                return _ice;
            return null;
        }

        private static void ResetChaos()
        {
            if (!RequireHost())
                return;

            var restored = 0;
            foreach (var user in Enumerate("Targets"))
            {
                if (user.master == null)
                    continue;
                if (!OriginalBodies.TryGetValue(Key(user), out var prefab) || prefab == null)
                    prefab = PreferredBody(user);
                if (prefab == null)
                    continue;
                var bodyName = ((UnityEngine.Object)prefab).name;
                var index = BodyCatalog.FindBodyIndex(prefab);
                if (index != BodyIndex.None)
                {
                    var catalogName = BodyCatalog.GetBodyName(index);
                    if (!string.IsNullOrEmpty(catalogName))
                        bodyName = catalogName;
                }

                user.master.bodyPrefab = prefab;
                user.master.TransformBody(bodyName);
                restored++;
            }

            SetRuntime("Disco", false);
            SetRuntime("GodMode", false);
            SetRuntime("LowGravity", false);
            SetRuntime("DrunkCamera", false);
            SetRuntime("ThirdPerson", false);
            SetRuntime("Fov", 60f);
            SetRuntime("BodyScale", 1f);
            ChannelPlugin.AcceptDistance(8f);
            ClearElites();

            var restore = Type.GetType("RoRCheatPanel.CheatRuntime, RoRCheatPanel")
                ?.GetMethod("RestoreCamera", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            restore?.Invoke(null, null);

            Toast(restored > 0
                ? Tr("Хаос сброшен, персонаж возвращён", "Chaos reset, character restored", "Caos reiniciado, personaje restaurado")
                : Tr("Хаос сброшен", "Chaos reset", "Caos reiniciado"));
        }

        private static GameObject PreferredBody(NetworkUser user)
        {
            var field = typeof(NetworkUser).GetField("bodyIndexPreference", BindingFlags.Public | BindingFlags.Instance);
            if (field != null && field.GetValue(user) is BodyIndex index && index != BodyIndex.None)
                return BodyCatalog.GetBodyPrefab(index);
            return null;
        }

        private static void ClearElites()
        {
            foreach (var user in Enumerate("AllUsers"))
            {
                if (EliteByUser.ContainsKey(Key(user)))
                    Paint(user.GetCurrentBody(), null);
            }

            EliteByUser.Clear();
        }

        private static void SetRuntime(string name, bool value)
        {
            var field = Type.GetType("RoRCheatPanel.CheatRuntime, RoRCheatPanel")
                ?.GetField(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            field?.SetValue(null, value);
        }

        private static void SetRuntime(string name, float value)
        {
            var field = Type.GetType("RoRCheatPanel.CheatRuntime, RoRCheatPanel")
                ?.GetField(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            field?.SetValue(null, value);
        }

        private static void ShuffleItems()
        {
            if (!RequireHost())
                return;
            var inventories = new List<Inventory>();
            foreach (var user in Enumerate("Targets"))
            {
                var inventory = user.master != null ? user.master.inventory : null;
                if (inventory != null)
                    inventories.Add(inventory);
            }

            if (inventories.Count < 2)
            {
                var all = new List<Inventory>();
                foreach (var user in Enumerate("AllUsers"))
                {
                    var inventory = user.master != null ? user.master.inventory : null;
                    if (inventory != null && !all.Contains(inventory))
                        all.Add(inventory);
                }

                if (all.Count >= 2)
                    inventories = all;
            }

            if (inventories.Count < 2)
            {
                Toast(Tr("Для перетасовки нужны хотя бы двое", "Shuffle needs at least two players", "Para mezclar hacen falta al menos dos"));
                return;
            }

            var count = ItemCatalog.itemCount;
            var bags = new List<List<KeyValuePair<ItemIndex, int>>>(inventories.Count);
            for (var p = 0; p < inventories.Count; p++)
            {
                var bag = new List<KeyValuePair<ItemIndex, int>>();
                for (var i = 0; i < count; i++)
                {
                    var index = (ItemIndex)i;
                    var stacks = inventories[p].GetItemCountPermanent(index);
                    if (stacks > 0)
                        bag.Add(new KeyValuePair<ItemIndex, int>(index, stacks));
                }

                bags.Add(bag);
            }

            for (var p = 0; p < inventories.Count; p++)
            {
                foreach (var stack in bags[p])
                    inventories[p].RemoveItemPermanent(stack.Key, stack.Value);
            }

            for (var i = bags.Count - 1; i > 0; i--)
            {
                var j = UnityEngine.Random.Range(0, i + 1);
                var swap = bags[i];
                bags[i] = bags[j];
                bags[j] = swap;
            }

            for (var p = 0; p < inventories.Count; p++)
            {
                foreach (var stack in bags[p])
                    inventories[p].GiveItemPermanent(stack.Key, stack.Value);
            }

            Toast(Tr("Предметы перемешаны: ", "Items shuffled: ", "Objetos mezclados: ") + inventories.Count);
        }

        private static void GatherMonsters()
        {
            if (!RequireHost())
                return;
            var local = LocalUserManager.GetFirstLocalUser();
            var body = local != null ? local.cachedBody : null;
            if (body == null)
            {
                Toast(Tr("Нет тела", "No body", "No hay cuerpo"));
                return;
            }

            var moved = 0;
            var list = CharacterBody.readOnlyInstancesList;
            if (list == null)
            {
                Toast(Tr("На этапе никого нет", "Nobody on this stage", "No hay nadie en esta fase"));
                return;
            }

            foreach (var other in list)
            {
                if (other == null || other == body || other.isPlayerControlled)
                    continue;
                if (other.healthComponent == null || !other.healthComponent.alive)
                    continue;
                if (other.teamComponent == null)
                    continue;
                var team = other.teamComponent.teamIndex;
                if (team != TeamIndex.Monster && team != TeamIndex.Lunar && team != TeamIndex.Void)
                    continue;
                var angle = UnityEngine.Random.Range(0f, 360f) * Mathf.Deg2Rad;
                var radius = UnityEngine.Random.Range(2.5f, 8f);
                var dest = body.footPosition + new Vector3(Mathf.Cos(angle) * radius, 0.4f, Mathf.Sin(angle) * radius);
                TeleportHelper.TeleportBody(other, dest);
                moved++;
                if (moved >= 180)
                    break;
            }

            Toast(moved > 0
                ? Tr("Монстров рядом: ", "Monsters nearby: ", "Monstruos cerca: ") + moved
                : Tr("Монстров на этапе нет", "No monsters on this stage", "No hay monstruos en esta fase"));
        }

        private static void Salute()
        {
            if (!RequireHost())
                return;
            var local = LocalUserManager.GetFirstLocalUser();
            var body = local != null ? local.cachedBody : null;
            if (body == null)
            {
                Toast(Tr("Нет тела", "No body", "No hay cuerpo"));
                return;
            }

            EnsureFireworks();
            var origin = body.corePosition + Vector3.up * 1.2f;
            for (var i = 0; i < 16; i++)
            {
                if (FireworkPrefabs.Count == 0)
                    break;
                var prefab = FireworkPrefabs[UnityEngine.Random.Range(0, FireworkPrefabs.Count)];
                var angle = i * 22.5f * Mathf.Deg2Rad;
                var pos = origin + new Vector3(Mathf.Cos(angle) * 3.2f, UnityEngine.Random.Range(1.5f, 6f), Mathf.Sin(angle) * 3.2f);
                EffectManager.SimpleEffect(prefab, pos, Quaternion.identity, true);
            }

            var lines = Lang() == "En" ? SaluteEn : Lang() == "Es" ? SaluteEs : SaluteRu;
            Chat.SendBroadcastChat(new Chat.SimpleChatMessage
            {
                baseToken = lines[UnityEngine.Random.Range(0, lines.Length)]
            });
            Toast(Tr("Салют", "Fireworks", "Fuegos"));
        }

        private static void EnsureFireworks()
        {
            if (_fireworksSearched)
                return;
            _fireworksSearched = true;
            var effects = Resources.FindObjectsOfTypeAll<EffectComponent>();
            foreach (var effect in effects)
            {
                if (effect == null)
                    continue;
                var go = effect.gameObject;
                var name = go.name ?? "";
                if (name.IndexOf("Firework", StringComparison.OrdinalIgnoreCase) < 0)
                    continue;
                if (go.GetComponent<RoR2.Projectile.ProjectileController>() != null)
                    continue;
                FireworkPrefabs.Add(go);
            }
        }

        private static bool RequireHost()
        {
            if (NetworkServer.active)
                return true;
            Toast(Tr("Только хост", "Host only", "Solo el anfitrión"));
            return false;
        }

        private static string Key(NetworkUser user)
        {
            return user != null ? user.id.ToString() : "";
        }

        private static IEnumerable<NetworkUser> Enumerate(string methodName)
        {
            var type = Type.GetType("RoRCheatPanel.GameUtil, RoRCheatPanel");
            var method = type != null
                ? type.GetMethod(methodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)
                : null;
            if (method == null || !(method.Invoke(null, null) is IEnumerable users))
                yield break;
            foreach (var item in users)
            {
                if (item is NetworkUser user && user != null)
                    yield return user;
            }
        }

        private static void Toast(string message)
        {
            var helpers = Type.GetType("RoRCheatPanel.UI.UiHelpers, RoRCheatPanel");
            helpers?.GetMethod("Toast", BindingFlags.Public | BindingFlags.Static)?.Invoke(null, new object[] { message });
        }

        private static string Tr(string ru, string en, string es)
        {
            switch (Lang())
            {
                case "En":
                    return en;
                case "Es":
                    return es;
                default:
                    return ru;
            }
        }

        private static string EliteName(int kind)
        {
            var en = Lang() == "En";
            var es = Lang() == "Es";
            if (kind == Fire)
                return en ? "Blazing" : es ? "Llameante" : "Пылающий";
            if (kind == Lightning)
                return en ? "Overloading" : es ? "Sobrecarga" : "Перегрузка";
            return en ? "Glacial" : es ? "Glacial" : "Лёд";
        }

        private static string ActionName(string id)
        {
            var en = Lang() == "En";
            var es = Lang() == "Es";
            if (id == "reset")
                return en ? "Reset chaos" : es ? "Resetear caos" : "Сбросить хаос";
            if (id == "shuffle")
                return en ? "Shuffle items" : es ? "Mezclar objetos" : "Перетасовка";
            if (id == "gather")
                return en ? "All here" : es ? "Todos aquí" : "Все сюда";
            return en ? "Fireworks" : es ? "Fuegos" : "Салют";
        }

        private static string Lang()
        {
            var loc = Type.GetType("RoRCheatPanel.Loc, RoRCheatPanel");
            var current = loc?.GetField("Current", BindingFlags.Public | BindingFlags.Static)?.GetValue(null);
            return current != null ? current.ToString() : "Ru";
        }
    }
}
