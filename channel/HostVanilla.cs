using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using RoR2;
using RoR2.CharacterAI;
using RoR2.Networking;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;

namespace RoRCheatPanel.Channel
{
    internal static class HostVanilla
    {
        private static readonly FieldInfo CurrentEnemy = AccessTools.Field(typeof(BaseAI), "<currentEnemy>k__BackingField");
        private static readonly FieldInfo CustomEnemy = AccessTools.Field(typeof(BaseAI), "<customTarget>k__BackingField");
        private static readonly HashSet<BaseAI> Silenced = new HashSet<BaseAI>();
        private static readonly HashSet<CharacterMaster> Befriended = new HashSet<CharacterMaster>();
        private static readonly List<ItemIndex> Held = new List<ItemIndex>();
        private static readonly List<BuffDef> Buffs = new List<BuffDef>();

        private static bool _aim;
        private static bool _brainless;
        private static bool _chaos;
        private static bool _united;
        private static float _chaosAt;
        private static int _itemSlot;
        private static string _stackText = "1";
        private static bool _buffsReady;

        public static void Install(Harmony harmony)
        {
            var tools = Type.GetType("RoRCheatPanel.UI.Tabs.ToolsTab, RoRCheatPanel");
            var toggles = tools?.GetMethod("DrawToggles", BindingFlags.NonPublic | BindingFlags.Static);
            if (toggles != null)
                harmony.Patch(toggles, postfix: new HarmonyMethod(typeof(HostVanilla), nameof(DrawNoclipNote)));
            var spawns = tools?.GetMethod("DrawSpawns", BindingFlags.NonPublic | BindingFlags.Static);
            if (spawns != null)
                harmony.Patch(spawns, postfix: new HarmonyMethod(typeof(HostVanilla), nameof(DrawTools)));

            var players = Type.GetType("RoRCheatPanel.UI.Tabs.PlayersTab, RoRCheatPanel");
            var drawUser = players?.GetMethod("DrawUser", BindingFlags.NonPublic | BindingFlags.Static);
            if (drawUser != null)
                harmony.Patch(drawUser, postfix: new HarmonyMethod(typeof(HostVanilla), nameof(DrawRevive)));

            var run = Type.GetType("RoRCheatPanel.UI.Tabs.RunTab, RoRCheatPanel");
            var difficulty = run?.GetMethod("DrawDifficulty", BindingFlags.NonPublic | BindingFlags.Static);
            if (difficulty != null)
                harmony.Patch(difficulty, postfix: new HarmonyMethod(typeof(HostVanilla), nameof(DrawKarma)));
        }

        private static void DrawKarma()
        {
            GUILayout.Space(6f);
            GUILayout.Label(Tr("Карма", "Karma", "Karma"));
            var now = ReadKarma();
            var max = MaxKarma();
            GUILayout.BeginHorizontal();
            for (var level = 0; level <= max; level++)
            {
                var current = level == now;
                var prev = GUI.backgroundColor;
                if (current)
                    GUI.backgroundColor = new Color(0.4f, 0.7f, 0.4f);
                var tip = Tr(
                    "Поставить карму " + level + ". Только хост, только в забеге.",
                    "Set karma to " + level + ". Host only, during a run.",
                    "Poner karma en " + level + ". Solo anfitrión, en una partida.");
                if (GUILayout.Button(new GUIContent(level.ToString(), tip), GUILayout.Width(110f), GUILayout.Height(28f)))
                    SetKarma(level);
                GUI.backgroundColor = prev;
                if ((level + 1) % 5 == 0)
                {
                    GUILayout.EndHorizontal();
                    GUILayout.BeginHorizontal();
                }
            }

            GUILayout.EndHorizontal();
            var unlock = Tr(
                "Снимает замки уровней кармы в меню выбора.",
                "Unlocks the karma levels in the character select menu.",
                "Quita los candados de karma en el menú de selección.");
            if (GUILayout.Button(new GUIContent(Tr("Открыть все уровни кармы", "Unlock all karma levels", "Abrir todos los niveles de karma"), unlock), GUILayout.Width(280f), GUILayout.Height(28f)))
                UnlockKarmaLevels();
        }

        private static void SetKarma(int level)
        {
            if (!Host())
                return;
            if (Run.instance == null)
            {
                Toast(Tr("Карма меняется только в забеге", "Karma changes only during a run", "La karma solo cambia en una partida"));
                return;
            }

            var manager = KarmaManager.instance;
            if (manager == null)
            {
                Toast(Tr("Карма ещё не готова", "Karma is not ready", "La karma aún no está lista"));
                return;
            }

            var delta = level - manager.effectiveKarma;
            if (delta != 0)
                manager.ModifyKarma(delta);
            Toast(Tr("Карма: " + manager.effectiveKarma, "Karma: " + manager.effectiveKarma, "Karma: " + manager.effectiveKarma));
        }

        private static int ReadKarma()
        {
            var manager = KarmaManager.instance;
            return manager != null ? manager.effectiveKarma : 0;
        }

        private static int MaxKarma()
        {
            var manager = KarmaManager.instance;
            var field = typeof(KarmaManager).GetField("maxKarma", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (manager != null && field != null && field.GetValue(manager) is int value && value > 0)
                return value;
            return 8;
        }

        private static void UnlockKarmaLevels()
        {
            var local = LocalUserManager.GetFirstLocalUser();
            var profile = local != null ? local.userProfile : null;
            if (profile == null)
            {
                Toast(Tr("Нет профиля", "No profile", "Sin perfil"));
                return;
            }

            var granted = 0;
            var max = MaxKarma();
            for (var level = 0; level <= max; level++)
            {
                var def = KarmaManager.GetRequiredUnlockableForKarmaLevel(level);
                if (def == null || profile.HasUnlockable(def))
                    continue;
                profile.GrantUnlockable(def);
                granted++;
            }

            Toast(Tr("Открыто уровней: " + granted, "Levels unlocked: " + granted, "Niveles abiertos: " + granted));
        }

        public static void Tick()
        {
            if (!NetworkServer.active)
                return;
            TickAim();
            TickBrainless();
            TickChaos();
        }

        public static void LargeBody()
        {
            TransformPreset(true);
        }

        public static void SmallBody()
        {
            TransformPreset(false);
        }

        public static void RestoreSurvivor()
        {
            if (!Host())
                return;

            var restored = 0;
            foreach (var user in Users("Targets"))
            {
                var master = user.master;
                if (master == null)
                    continue;
                var index = user.bodyIndexPreference;
                var prefab = index != BodyIndex.None ? BodyCatalog.GetBodyPrefab(index) : null;
                if (prefab == null)
                    continue;
                var bodyName = BodyCatalog.GetBodyName(index);
                if (string.IsNullOrEmpty(bodyName))
                    bodyName = prefab.name;
                var source = prefab.GetComponent<CharacterBody>();
                master.bodyPrefab = prefab;
                master.TransformBody(bodyName);
                CopySkills(master.GetBody(), source);
                restored++;
            }

            Toast(restored > 0
                ? Tr("Выживший возвращён", "Survivor restored", "Superviviente restaurado")
                : Tr("Нет выбранного выжившего", "No chosen survivor", "No hay superviviente elegido"));
        }

        public static void DrawChaos()
        {
            var next = Toggle(_chaos, Tr("Хаос каждые 20 с", "Chaos every 20s", "Caos cada 20 s"));
            if (next != _chaos)
            {
                _chaos = next;
                _chaosAt = Time.unscaledTime + 20f;
            }

            GUILayout.Label(Tr(
                "Каждые 20 с у выбранных одно из: бафф, обмен инвентаря, обмен местами, смена тела, дождь предметов, дождь боссов или общая команда.",
                "Every 20s, the selected players get one of: a buff, an inventory swap, a place swap, a body change, an item rain, a boss rain, or one shared team.",
                "Cada 20 s, a los elegidos les toca uno: un buff, un cambio de inventario, un cambio de lugar, un cambio de cuerpo, una lluvia de objetos, una lluvia de jefes o un equipo común."));
        }

        private static void DrawNoclipNote()
        {
            GUILayout.Label(Tr(
                "Чужим игрокам noclip не передаётся: хост двигает их телепортом.",
                "Other players do not receive noclip: the host moves them by teleport.",
                "Los demás no reciben noclip: el anfitrión los mueve con teletransporte."));
        }

        private static void DrawTools()
        {
            DrawReviveAll();
            DrawInventory();
            DrawEnemies();
            DrawDebug();
        }

        private static void DrawRevive(NetworkUser user)
        {
            if (user == null)
                return;
            if (!GUILayout.Button(Tr("Воскресить", "Revive", "Revivir"), GUILayout.Height(26f)))
                return;
            if (!Dead(user))
            {
                Toast(Tr("Игрок жив", "That player is alive", "Ese jugador está vivo"));
                return;
            }

            ReviveOne(user, true);
        }

        private static void DrawReviveAll()
        {
            GUILayout.BeginVertical(GUI.skin.box);
            GUILayout.Label(Tr("Воскрешение", "Revive", "Revivir"));
            if (GUILayout.Button(Tr("Воскресить мёртвых", "Revive the dead", "Revivir a los muertos"), GUILayout.Width(200f), GUILayout.Height(28f)))
                ReviveAll();
            GUILayout.EndVertical();
        }

        private static void DrawInventory()
        {
            GUILayout.BeginVertical(GUI.skin.box);
            GUILayout.Label(Tr("Инвентарь", "Inventory", "Inventario"));
            Note(Tr(
                "Предметы выбранного игрока. «Снять» и «Задать стак» меняют число. «Копировать» отдаёт вещи первого остальным. «Обменять» меняет инвентари двух первых.",
                "Items of the selected player. Remove and Set stack change the count. Copy gives the first player's items to the others. Swap exchanges the first two inventories.",
                "Objetos del jugador elegido. Quitar y Fijar pila cambian la cantidad. Copiar da los objetos del primero a los demás. Cambiar intercambia los dos primeros inventarios."));
            var item = CurrentItem(out var stacks, out var name);
            GUILayout.Label(item.HasValue
                ? name + "  x" + stacks
                : Tr("Нет предметов у первой цели", "The first target has no items", "El primer objetivo no tiene objetos"));
            GUILayout.BeginHorizontal();
            if (GUILayout.Button(Tr("Предмет −", "Item −", "Objeto −"), GUILayout.Width(150f), GUILayout.Height(28f)))
                _itemSlot--;
            if (GUILayout.Button(Tr("Предмет +", "Item +", "Objeto +"), GUILayout.Width(150f), GUILayout.Height(28f)))
                _itemSlot++;
            if (GUILayout.Button(Tr("Снять 1", "Remove 1", "Quitar 1"), GUILayout.Width(150f), GUILayout.Height(28f)))
                RemoveOne(item);
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            _stackText = GUILayout.TextField(_stackText ?? "", GUILayout.Width(56f), GUILayout.Height(24f));
            if (GUILayout.Button(Tr("Задать стак", "Set stack", "Fijar pila"), GUILayout.Width(150f), GUILayout.Height(28f)))
                SetStack(item);
            if (GUILayout.Button(Tr("Копировать", "Copy", "Copiar"), GUILayout.Width(150f), GUILayout.Height(28f)))
                CopyInventories();
            if (GUILayout.Button(Tr("Обменять", "Swap", "Cambiar"), GUILayout.Width(150f), GUILayout.Height(28f)))
                SwapInventories();
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
        }

        private static void DrawEnemies()
        {
            GUILayout.BeginVertical(GUI.skin.box);
            GUILayout.Label(Tr("Враги", "Enemies", "Enemigos"));
            DrawPlayerList();
            Note(Tr(
                "«Целиться» направляет живых врагов на выбранного игрока. «Без цели» отключает им интеллект. Команды переводят их к игрокам или обратно к монстрам. «Дождь боссов» призывает боссов рядом.",
                "Aim points living enemies at the selected player. No target turns off their AI. The team buttons move them to the players or back to monsters. Boss rain summons bosses nearby.",
                "Apuntar dirige a los enemigos vivos al jugador elegido. Sin objetivo les apaga la IA. Los botones de equipo los pasan a los jugadores o a los monstruos. Lluvia de jefes invoca jefes cerca."));
            GUILayout.BeginHorizontal();
            var aim = Toggle(_aim, Tr("Целиться в выбранного", "Aim at selected", "Apuntar al elegido"), 200f);
            var brain = Toggle(_brainless, Tr("Без цели", "No target", "Sin objetivo"), 140f);
            GUILayout.EndHorizontal();
            if (aim != _aim || brain != _brainless)
            {
                if (aim && brain)
                {
                    if (aim != _aim)
                        brain = false;
                    else
                        aim = false;
                }

                _aim = aim;
                _brainless = brain;
                if (!_brainless)
                    RestoreBrains();
            }

            GUILayout.BeginHorizontal();
            if (GUILayout.Button(Tr("Команда игрока", "Player team", "Equipo jugador"), GUILayout.Width(160f), GUILayout.Height(28f)))
                SetEnemyTeam(TeamIndex.Player);
            if (GUILayout.Button(Tr("Команда монстров", "Monster team", "Equipo monstruo"), GUILayout.Width(170f), GUILayout.Height(28f)))
                SetEnemyTeam(TeamIndex.Monster);
            if (GUILayout.Button(Tr("Дождь боссов", "Boss rain", "Lluvia de jefes"), GUILayout.Width(150f), GUILayout.Height(28f)))
                MonsterSpawn.RainBosses(6);
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
        }

        private static void DrawPlayerList()
        {
            var helpers = Type.GetType("RoRCheatPanel.UI.UiHelpers, RoRCheatPanel");
            helpers?.GetMethod("DrawPlayerPicker", BindingFlags.Public | BindingFlags.Static)?.Invoke(null, null);
        }

        private static void DrawDebug()
        {
            if (!NetworkServer.active)
                return;

            GUILayout.BeginVertical(GUI.skin.box);
            GUILayout.Label(Tr("Отладка", "Debug", "Depuración"));
            NetworkUser user = null;
            foreach (var candidate in Users("Targets"))
            {
                user = candidate;
                break;
            }

            if (user == null)
            {
                GUILayout.Label(Tr("Нет цели", "No target", "Sin objetivo"));
                GUILayout.EndVertical();
                return;
            }

            var master = user.master;
            var body = user.GetCurrentBody();
            var health = body != null ? body.healthComponent : null;
            var team = body != null && body.teamComponent != null
                ? body.teamComponent.teamIndex
                : master != null ? master.teamIndex : TeamIndex.None;
            GUILayout.Label(Tr("Имя: ", "Name: ", "Nombre: ") + (user.userName ?? ""));
            GUILayout.Label(Tr("Игрок netId: ", "User netId: ", "NetId del jugador: ") + Net(user));
            GUILayout.Label(Tr("Мастер: ", "Master: ", "Amo: ") + Net(master));
            GUILayout.Label(Tr("Тело: ", "Body: ", "Cuerpo: ") + BodyName(body));
            GUILayout.Label(Tr("Тело netId: ", "Body netId: ", "NetId del cuerpo: ") + Net(body));
            GUILayout.Label(Tr("Здоровье: ", "Health: ", "Salud: ") + HealthText(health, body));
            GUILayout.Label(Tr("Команда: ", "Team: ", "Equipo: ") + team + " (" + (int)team + ")");
            GUILayout.Label(Tr("Авторитет движения: ", "Movement authority: ", "Autoridad de movimiento: ") + Authority(body));
            var buffs = new GUIStyle(GUI.skin.label) { wordWrap = true };
            GUILayout.Label(Tr("Баффы: ", "Buffs: ", "Buffs: ") + BuffText(body), buffs);
            GUILayout.Label(Tr("Жив: ", "Alive: ", "Vivo: ") + (health != null && health.alive
                ? Tr("да", "yes", "sí")
                : Tr("нет", "no", "no")));
            GUILayout.Label(Tr("Сцена: ", "Scene: ", "Escena: ") + SceneName());
            GUILayout.EndVertical();
        }

        private static void TransformPreset(bool champion)
        {
            if (!Host())
                return;
            if (!BodyCatalog.availability.available || BodyCatalog.bodyCount <= 0)
            {
                Toast(Tr("Каталог тел ещё не готов", "The body catalog is not ready", "El catálogo de cuerpos no está listo"));
                return;
            }

            GameObject best = null;
            var bestBulk = champion ? float.MinValue : float.MaxValue;
            for (var i = 0; i < BodyCatalog.bodyCount; i++)
            {
                var index = (BodyIndex)i;
                var prefab = BodyCatalog.GetBodyPrefab(index);
                var body = BodyCatalog.GetBodyPrefabBodyComponent(index);
                if (prefab == null || body == null || body.isChampion != champion)
                    continue;
                if (Junk(prefab.name) || IsSurvivor(prefab))
                    continue;
                if (body.modelLocator == null || body.modelLocator.modelTransform == null)
                    continue;
                var bulk = Bulk(body);
                if (bulk <= 0.05f)
                    continue;
                if (champion ? bulk > bestBulk : bulk < bestBulk)
                {
                    bestBulk = bulk;
                    best = prefab;
                }
            }

            if (best == null)
            {
                Toast(champion
                    ? Tr("В каталоге нет крупного чемпиона", "No large champion is in the catalog", "No hay un campeón grande en el catálogo")
                    : Tr("В каталоге нет маленького тела", "No small body is in the catalog", "No hay un cuerpo pequeño en el catálogo"));
                return;
            }

            var shown = best.GetComponent<CharacterBody>();
            var label = shown != null ? shown.GetDisplayName() : best.name;
            var util = Type.GetType("RoRCheatPanel.GameUtil, RoRCheatPanel");
            var method = util?.GetMethod(
                "TransformBodyPrefab",
                BindingFlags.Public | BindingFlags.Static,
                null,
                new[] { typeof(GameObject), typeof(string) },
                null);
            if (method == null)
            {
                Toast(Tr("Нет смены тела", "Body change is missing", "Falta el cambio de cuerpo"));
                return;
            }

            method.Invoke(null, new object[] { best, label });
        }

        private static float Bulk(CharacterBody body)
        {
            if (body.radius > 0.05f)
                return body.radius;
            return body.baseMaxHealth;
        }

        private static bool Junk(string name)
        {
            if (string.IsNullOrEmpty(name))
                return true;
            string[] parts =
            {
                "Display", "Pod", "Corpse", "Dead", "Fake", "Temp", "Projectile", "Joint",
                "WeakPoint", "HurtBox", "Spectator", "Attachment", "Dummy", "Drone", "Turret"
            };
            for (var i = 0; i < parts.Length; i++)
            {
                if (name.IndexOf(parts[i], StringComparison.OrdinalIgnoreCase) >= 0)
                    return true;
            }

            return false;
        }

        private static bool IsSurvivor(GameObject prefab)
        {
            var defs = SurvivorCatalog.allSurvivorDefs;
            if (defs == null || prefab == null)
                return false;
            foreach (var survivor in defs)
            {
                if (survivor != null && survivor.bodyPrefab == prefab)
                    return true;
            }

            return false;
        }

        private static void RemoveOne(ItemIndex? item)
        {
            if (!Host() || !item.HasValue)
            {
                if (Host() && !item.HasValue)
                    Toast(Tr("Нечего снимать", "Nothing to remove", "Nada que quitar"));
                return;
            }

            var removed = 0;
            foreach (var inventory in Inventories())
            {
                if (inventory.GetItemCountPermanent(item.Value) <= 0)
                    continue;
                inventory.RemoveItemPermanent(item.Value, 1);
                removed++;
            }

            Toast(removed > 0
                ? Tr("Снят 1 стак", "Removed 1 stack", "Quitada 1 pila")
                : Tr("Этого предмета нет", "That item is absent", "Ese objeto no está"));
        }

        private static void SetStack(ItemIndex? item)
        {
            if (!Host() || !item.HasValue)
            {
                if (Host() && !item.HasValue)
                    Toast(Tr("Нет предмета", "No item", "No hay objeto"));
                return;
            }

            if (!int.TryParse(_stackText, out var want))
                want = 0;
            want = Mathf.Clamp(want, 0, 999);
            foreach (var inventory in Inventories())
            {
                var have = inventory.GetItemCountPermanent(item.Value);
                var delta = want - have;
                if (delta > 0)
                    inventory.GiveItemPermanent(item.Value, delta);
                else if (delta < 0)
                    inventory.RemoveItemPermanent(item.Value, -delta);
            }

            Toast(Tr("Стак задан: ", "Stack set: ", "Pila fijada: ") + want);
        }

        private static void CopyInventories()
        {
            var list = Inventories();
            if (!Host())
                return;
            if (list.Count < 2)
            {
                Toast(Tr("Нужны двое выбранных", "Select two players", "Elige a dos jugadores"));
                return;
            }

            for (var i = 1; i < list.Count; i++)
            {
                list[i].CopyItemsFrom(list[0]);
                list[i].CopyEquipmentFrom(list[0], true);
            }
            Toast(Tr("Инвентарь скопирован", "Inventory copied", "Inventario copiado"));
        }

        private static void SwapInventories()
        {
            var list = Inventories();
            if (!Host())
                return;
            if (list.Count < 2)
            {
                Toast(Tr("Нужны двое выбранных", "Select two players", "Elige a dos jugadores"));
                return;
            }

            var first = list[0];
            var second = list[1];
            var count = ItemCatalog.itemCount;
            var saved = new int[count];
            for (var i = 0; i < count; i++)
                saved[i] = first.GetItemCountPermanent((ItemIndex)i);
            var firstEquip = first.currentEquipmentIndex;
            var secondEquip = second.currentEquipmentIndex;
            first.CopyItemsFrom(second);
            for (var i = 0; i < count; i++)
            {
                var index = (ItemIndex)i;
                var have = second.GetItemCountPermanent(index);
                var delta = saved[i] - have;
                if (delta > 0)
                    second.GiveItemPermanent(index, delta);
                else if (delta < 0)
                    second.RemoveItemPermanent(index, -delta);
            }

            PutEquipment(first, secondEquip);
            PutEquipment(second, firstEquip);

            Toast(Tr("Инвентари обменены", "Inventories swapped", "Inventarios cambiados"));
        }

        private static ItemIndex? CurrentItem(out int stacks, out string name)
        {
            stacks = 0;
            name = "";
            Held.Clear();
            Inventory inventory = null;
            foreach (var user in Users("Targets"))
            {
                inventory = user.master != null ? user.master.inventory : null;
                if (inventory != null)
                    break;
            }

            if (inventory == null)
                return null;
            if (inventory.itemAcquisitionOrder != null)
            {
                for (var i = 0; i < inventory.itemAcquisitionOrder.Count; i++)
                {
                    var index = inventory.itemAcquisitionOrder[i];
                    if (inventory.GetItemCountPermanent(index) > 0 && !Held.Contains(index))
                        Held.Add(index);
                }
            }

            for (var i = 0; i < ItemCatalog.itemCount; i++)
            {
                var index = (ItemIndex)i;
                if (inventory.GetItemCountPermanent(index) > 0 && !Held.Contains(index))
                    Held.Add(index);
            }

            if (Held.Count == 0)
                return null;
            var slot = _itemSlot % Held.Count;
            if (slot < 0)
                slot += Held.Count;
            _itemSlot = slot;
            var item = Held[slot];
            stacks = inventory.GetItemCountPermanent(item);
            name = ItemLabel(item);
            return item;
        }

        private static string ItemLabel(ItemIndex index)
        {
            var def = ItemCatalog.GetItemDef(index);
            if (def == null)
                return index.ToString();
            var text = string.IsNullOrEmpty(def.nameToken) ? "" : Language.GetString(def.nameToken);
            if (string.IsNullOrEmpty(text) || text == def.nameToken || text == "???")
                text = def.name;
            return string.IsNullOrEmpty(text) ? index.ToString() : text;
        }

        private static List<Inventory> Inventories()
        {
            var list = new List<Inventory>();
            foreach (var user in Users("Targets"))
            {
                var inventory = user.master != null ? user.master.inventory : null;
                if (inventory != null && !list.Contains(inventory))
                    list.Add(inventory);
            }

            return list;
        }

        private static void SetEnemyTeam(TeamIndex team)
        {
            if (!Host())
                return;
            var changed = 0;
            var bodies = CharacterBody.readOnlyInstancesList;
            if (bodies != null)
            {
                for (var i = 0; i < bodies.Count; i++)
                {
                    var body = bodies[i];
                    if (body == null || body.isPlayerControlled || body.teamComponent == null)
                        continue;
                    var master = body.master;
                    if (master != null && master.playerCharacterMasterController != null)
                        continue;
                    var current = body.teamComponent.teamIndex;
                    var remembered = master != null && Befriended.Contains(master);
                    var wild = current == TeamIndex.Monster || current == TeamIndex.Lunar || current == TeamIndex.Void;
                    if (!wild && !remembered)
                        continue;
                    if (team == TeamIndex.Player && current == TeamIndex.Player)
                        continue;
                    body.teamComponent.teamIndex = team;
                    if (master != null)
                    {
                        master.teamIndex = team;
                        if (team == TeamIndex.Player)
                            Befriended.Add(master);
                        else
                            Befriended.Remove(master);
                    }

                    changed++;
                }
            }

            Toast(Tr("Команда врагов: ", "Enemy team: ", "Equipo enemigo: ") + team + " (" + changed + ")");
        }

        private static void TickAim()
        {
            if (!_aim || CurrentEnemy == null)
                return;
            CharacterBody victim = null;
            foreach (var user in Users("Targets"))
            {
                victim = user.GetCurrentBody();
                if (victim != null)
                    break;
            }

            if (victim == null || victim.teamComponent == null)
                return;
            var ais = InstanceTracker.GetInstancesList<BaseAI>();
            if (ais == null)
                return;
            HurtBox hurt = null;
            for (var n = 0; n < ais.Count && hurt == null; n++)
                hurt = HurtOf(victim, ais[n]);
            if (hurt == null)
                return;
            for (var i = 0; i < ais.Count; i++)
            {
                var ai = ais[i];
                if (ai == null || ai.body == null || ai.body.teamComponent == null || ai.body.isPlayerControlled)
                    continue;
                if (!TeamManager.IsTeamEnemy(ai.body.teamComponent.teamIndex, victim.teamComponent.teamIndex))
                    continue;
                var box = HurtOf(victim, ai) ?? hurt;
                if (box == null)
                    continue;
                var target = new BaseAI.Target(ai);
                target.gameObject = victim.gameObject;
                target.bestHurtBox = box;
                CurrentEnemy.SetValue(ai, target);
                ai.enemyAttention = ai.enemyAttentionDuration > 0f ? ai.enemyAttentionDuration : 30f;
            }
        }

        private static HurtBox HurtOf(CharacterBody body, BaseAI ai)
        {
            if (body == null)
                return null;
            if (body.mainHurtBox != null)
                return body.mainHurtBox;
            var group = body.hurtBoxGroup;
            if (group != null && group.mainHurtBox != null)
                return group.mainHurtBox;
            return ai != null ? ai.GetBestHurtBox(body.gameObject) : null;
        }

        private static void TickBrainless()
        {
            if (!_brainless)
            {
                RestoreBrains();
                return;
            }

            var ais = InstanceTracker.GetInstancesList<BaseAI>();
            var keep = new HashSet<BaseAI>();
            if (ais != null)
            {
                for (var i = 0; i < ais.Count; i++)
                {
                    var ai = ais[i];
                    if (!Wild(ai))
                        continue;
                    ClearTarget(ai);
                    if (ai.enabled)
                    {
                        ai.enabled = false;
                        Silenced.Add(ai);
                    }

                    if (Silenced.Contains(ai))
                        keep.Add(ai);
                }
            }

            var drop = new List<BaseAI>();
            foreach (var ai in Silenced)
            {
                if (ai == null || !keep.Contains(ai))
                    drop.Add(ai);
            }

            for (var i = 0; i < drop.Count; i++)
            {
                if (drop[i] != null)
                    drop[i].enabled = true;
                Silenced.Remove(drop[i]);
            }
        }

        private static void RestoreBrains()
        {
            if (Silenced.Count == 0)
                return;
            foreach (var ai in Silenced)
            {
                if (ai != null)
                    ai.enabled = true;
            }

            Silenced.Clear();
        }

        private static bool Wild(BaseAI ai)
        {
            if (ai == null || ai.body == null || ai.body.isPlayerControlled || ai.body.teamComponent == null)
                return false;
            var team = ai.body.teamComponent.teamIndex;
            return team == TeamIndex.Monster || team == TeamIndex.Lunar || team == TeamIndex.Void;
        }

        private static void ClearTarget(BaseAI ai)
        {
            ResetTarget(ai, CurrentEnemy);
            ResetTarget(ai, CustomEnemy);
            ai.enemyAttention = 0f;
        }

        private static void ResetTarget(BaseAI ai, FieldInfo field)
        {
            if (field == null || !(field.GetValue(ai) is BaseAI.Target target))
                return;
            target.Reset();
            field.SetValue(ai, target);
        }

        private static void TickChaos()
        {
            if (!_chaos || Run.instance == null)
                return;
            if (Time.unscaledTime < _chaosAt)
                return;
            _chaosAt = Time.unscaledTime + 20f;
            switch (UnityEngine.Random.Range(0, 7))
            {
                case 0:
                    ApplyRandomBuff();
                    break;
                case 1:
                    ChaosItems();
                    break;
                case 2:
                    SwapPlaces();
                    break;
                case 3:
                    Call("RandomSurvivor");
                    break;
                case 4:
                    Call("RainPickups", 12);
                    break;
                case 5:
                    MonsterSpawn.RainBosses(4);
                    break;
                default:
                    ToggleSharedTeam();
                    break;
            }
        }

        private static void ChaosItems()
        {
            if (Inventories().Count >= 2)
                SwapInventories();
            else
                Call("GiveRandomItem", 1);
        }

        private static void SwapPlaces()
        {
            var spots = new List<CharacterBody>();
            foreach (var user in Users("Targets"))
            {
                var body = user.GetCurrentBody();
                if (body != null)
                    spots.Add(body);
            }

            if (spots.Count >= 2)
            {
                var first = spots[0].footPosition;
                var second = spots[1].footPosition;
                TeleportHelper.TeleportBody(spots[0], second, false);
                TeleportHelper.TeleportBody(spots[1], first, false);
                Toast(Tr("Местами поменялись", "Places swapped", "Lugares cambiados"));
                return;
            }

            Call("TeleportSelectedToLocal");
        }

        private static void ToggleSharedTeam()
        {
            var users = new List<NetworkUser>(Users("Targets"));
            if (users.Count == 0)
                return;
            _united = !_united;
            var team = _united ? TeamIndex.Monster : TeamIndex.Player;
            for (var i = 0; i < users.Count; i++)
                AssignTeam(users[i], team);
            Toast(_united
                ? Tr("Все выбранные: монстры", "Selected players: monsters", "Elegidos: monstruos")
                : Tr("Все выбранные: игроки", "Selected players: players", "Elegidos: jugadores"));
        }

        private static void ApplyRandomBuff()
        {
            EnsureBuffs();
            if (Buffs.Count == 0)
            {
                Toast(Tr("В игре нет баффов", "The game has no buffs", "El juego no tiene buffs"));
                return;
            }

            var buff = Buffs[UnityEngine.Random.Range(0, Buffs.Count)];
            var given = 0;
            foreach (var user in Users("Targets"))
            {
                var body = user.GetCurrentBody();
                if (body == null)
                    continue;
                try
                {
                    body.AddBuff(buff);
                    given++;
                }
                catch (Exception)
                {
                }
            }

            var name = ((UnityEngine.Object)buff).name;
            Toast(given > 0
                ? Tr("Бафф: ", "Buff: ", "Buff: ") + name
                : Tr("Некому выдать бафф", "Nobody to buff", "Nadie para el buff"));
        }

        private static void EnsureBuffs()
        {
            if (_buffsReady)
                return;
            _buffsReady = true;
            var fields = typeof(RoR2Content.Buffs).GetFields(BindingFlags.Public | BindingFlags.Static);
            for (var i = 0; i < fields.Length; i++)
            {
                if (fields[i].FieldType != typeof(BuffDef))
                    continue;
                if (fields[i].GetValue(null) is BuffDef def && def != null)
                    Buffs.Add(def);
            }
        }

        private static void AssignTeam(NetworkUser user, TeamIndex team)
        {
            if (user == null)
                return;
            if (user.master != null)
                user.master.teamIndex = team;
            var body = user.GetCurrentBody();
            if (body != null && body.teamComponent != null)
                body.teamComponent.teamIndex = team;
        }

        private static void CopySkills(CharacterBody dest, CharacterBody src)
        {
            if (dest == null || src == null || dest.skillLocator == null || src.skillLocator == null)
                return;
            CopySkill(dest.skillLocator.primary, src.skillLocator.primary);
            CopySkill(dest.skillLocator.secondary, src.skillLocator.secondary);
            CopySkill(dest.skillLocator.utility, src.skillLocator.utility);
            CopySkill(dest.skillLocator.special, src.skillLocator.special);
        }

        private static void CopySkill(GenericSkill dest, GenericSkill src)
        {
            if (dest != null && src != null && src.skillDef != null)
                dest.SetBaseSkill(src.skillDef);
        }

        private static void PutEquipment(Inventory inventory, EquipmentIndex index)
        {
            if (inventory == null || inventory.currentEquipmentIndex == index)
                return;
            inventory.SetEquipmentIndex(index, index == EquipmentIndex.None);
        }

        private static string BuffText(CharacterBody body)
        {
            if (body == null || BuffCatalog.buffCount <= 0)
                return Tr("нет", "none", "ninguno");
            var names = new List<string>();
            var truncated = false;
            for (var i = 0; i < BuffCatalog.buffCount; i++)
            {
                var index = (BuffIndex)i;
                var stacks = body.GetBuffCount(index);
                if (stacks <= 0)
                    continue;
                var def = BuffCatalog.GetBuffDef(index);
                var label = def != null && !string.IsNullOrEmpty(def.name) ? def.name : index.ToString();
                names.Add(stacks > 1 ? label + " x" + stacks : label);
                if (names.Count < 12)
                    continue;
                for (var j = i + 1; j < BuffCatalog.buffCount; j++)
                {
                    if (body.GetBuffCount((BuffIndex)j) > 0)
                    {
                        truncated = true;
                        break;
                    }
                }

                break;
            }

            if (names.Count == 0)
                return Tr("нет", "none", "ninguno");
            var text = string.Join(", ", names);
            if (truncated)
                text += "…";
            return text;
        }

        private static string BodyName(CharacterBody body)
        {
            if (body == null)
                return Tr("нет", "none", "ninguno");
            var display = body.GetDisplayName();
            return string.IsNullOrEmpty(display) ? body.name : display;
        }

        private static string HealthText(HealthComponent health, CharacterBody body)
        {
            if (health == null || body == null)
                return "0/0";
            return health.health.ToString("0") + "/" + body.maxHealth.ToString("0");
        }

        private static string Authority(CharacterBody body)
        {
            if (body == null)
                return Tr("нет", "none", "ninguno");
            var transform = body.GetComponent<CharacterNetworkTransform>()
                ?? body.GetComponentInChildren<CharacterNetworkTransform>();
            if (transform == null)
                return Tr("нет", "none", "ninguno");
            return transform.hasEffectiveAuthority ? Tr("да", "yes", "sí") : Tr("нет", "no", "no");
        }

        private static string Net(NetworkBehaviour behaviour)
        {
            if (behaviour == null)
                return Tr("нет", "none", "ninguno");
            return behaviour.netId.Value.ToString();
        }

        private static string SceneName()
        {
            var stage = Stage.instance;
            if (stage != null && stage.sceneDef != null && !string.IsNullOrEmpty(stage.sceneDef.cachedName))
                return stage.sceneDef.cachedName;
            var scene = SceneManager.GetActiveScene();
            return string.IsNullOrEmpty(scene.name) ? Tr("нет", "none", "ninguno") : scene.name;
        }

        private static void Call(string method, object arg)
        {
            var util = Type.GetType("RoRCheatPanel.GameUtil, RoRCheatPanel");
            foreach (var info in util != null ? util.GetMethods(BindingFlags.Public | BindingFlags.Static) : Array.Empty<MethodInfo>())
            {
                if (info.Name != method)
                    continue;
                var parameters = info.GetParameters();
                if (parameters.Length != 1)
                    continue;
                info.Invoke(null, new[] { Convert.ChangeType(arg, parameters[0].ParameterType) });
                return;
            }
        }

        private static void Call(string method)
        {
            var util = Type.GetType("RoRCheatPanel.GameUtil, RoRCheatPanel");
            util?.GetMethod(method, BindingFlags.Public | BindingFlags.Static, null, Type.EmptyTypes, null)?.Invoke(null, null);
        }

        private static void ReviveAll()
        {
            if (!Host() || Run.instance == null)
            {
                if (NetworkServer.active && Run.instance == null)
                    Toast(Tr("Воскрешение только в забеге", "Revive works during a run", "Revivir solo durante una partida"));
                return;
            }

            var revived = 0;
            foreach (var user in Users("AllUsers"))
            {
                if (Dead(user) && ReviveOne(user, false))
                    revived++;
            }

            Toast(revived > 0
                ? Tr("Воскрешено: " + revived, "Revived: " + revived, "Revivido: " + revived)
                : Tr("Мёртвых игроков нет", "No dead players", "No hay jugadores muertos"));
        }

        private static bool ReviveOne(NetworkUser user, bool toast)
        {
            if (!NetworkServer.active)
            {
                if (toast)
                    Host();
                return false;
            }

            var run = Run.instance;
            if (run == null || user == null)
            {
                if (toast)
                    Toast(Tr("Воскрешение только в забеге", "Revive works during a run", "Revivir solo durante una partida"));
                return false;
            }

            var master = user.master;
            if (master == null || !Dead(user))
                return false;

            CharacterBody body = null;
            try
            {
                body = master.Respawn(RespawnSpot(master), Quaternion.identity, true);
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[RoRCheatPanel.Channel] revive failed: " + ex.Message);
            }

            if (body == null)
            {
                var prefab = master.bodyPrefab;
                var bodyName = prefab != null ? prefab.name : null;
                if (!string.IsNullOrEmpty(bodyName))
                {
                    try
                    {
                        master.Respawn(bodyName, true);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning("[RoRCheatPanel.Channel] revive by name failed: " + ex.Message);
                    }

                    body = user.GetCurrentBody();
                }
            }

            if (body == null)
            {
                if (toast)
                    Toast(Tr("Не удалось воскресить", "Revive failed", "No se pudo revivir"));
                return false;
            }

            var health = body.healthComponent;
            if (health != null)
                health.Networkhealth = health.fullHealth;
            if (toast)
                Toast(Tr("Воскрешён: " + user.userName, "Revived: " + user.userName, "Revivido: " + user.userName));
            return true;
        }

        private static bool Dead(NetworkUser user)
        {
            var master = user != null ? user.master : null;
            if (master == null)
                return false;
            if (master.lostBodyToDeath)
                return true;
            var body = user.GetCurrentBody();
            var health = body != null ? body.healthComponent : null;
            return health != null && !health.alive;
        }

        private static Vector3 RespawnSpot(CharacterMaster master)
        {
            var spot = master.deathFootPosition;
            if (spot.sqrMagnitude < 1f || spot.y < -50f)
            {
                try
                {
                    spot = master.CalculateSafeRespawnPosition();
                }
                catch (Exception)
                {
                    spot = Vector3.zero;
                }
            }

            if (spot.sqrMagnitude >= 1f && spot.y >= -50f)
                return spot;

            var local = LocalUserManager.GetFirstLocalUser();
            var body = local != null ? local.cachedBody : null;
            if (body == null && local != null && local.currentNetworkUser != null)
                body = local.currentNetworkUser.GetCurrentBody();
            return body != null ? body.footPosition + Vector3.up : new Vector3(0f, 3f, 0f);
        }

        private static bool Host()
        {
            if (NetworkServer.active)
                return true;
            Toast(Tr("Только хост", "Host only", "Solo el anfitrión"));
            return false;
        }

        private static GUIStyle _note;

        private static void Note(string text)
        {
            if (_note == null)
                _note = new GUIStyle(GUI.skin.label) { wordWrap = true };
            var width = 520f;
            var window = Type.GetType("RoRCheatPanel.UI.CheatWindow, RoRCheatPanel");
            if (window?.GetField("_rect", BindingFlags.NonPublic | BindingFlags.Static)?.GetValue(null) is Rect rect && rect.width > 120f)
                width = rect.width - 56f;
            GUILayout.Label(text, _note, GUILayout.MaxWidth(width));
        }

        private static bool Toggle(bool value, string label, float width = 0f)
        {
            var prev = GUI.backgroundColor;
            if (value)
                GUI.backgroundColor = new Color(0.42f, 0.72f, 0.48f);
            var clicked = width > 0f
                ? GUILayout.Button(label, GUILayout.Width(width), GUILayout.Height(26f))
                : GUILayout.Button(label, GUILayout.Height(26f));
            if (clicked)
                value = !value;
            GUI.backgroundColor = prev;
            return value;
        }

        private static IEnumerable<NetworkUser> Users(string methodName)
        {
            var type = Type.GetType("RoRCheatPanel.GameUtil, RoRCheatPanel");
            var method = type?.GetMethod(methodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            if (method == null || !(method.Invoke(null, null) is IEnumerable items))
                yield break;
            foreach (var item in items)
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
