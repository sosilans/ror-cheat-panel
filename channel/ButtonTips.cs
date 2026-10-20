using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace RoRCheatPanel.Channel
{
    internal static class ButtonTips
    {
        private static readonly Dictionary<string, string> Tips = new Dictionary<string, string>(StringComparer.Ordinal);
        private static bool _busy;

        public static void Install(Harmony harmony)
        {
            Fill();
            foreach (var method in typeof(GUILayout).GetMethods(BindingFlags.Public | BindingFlags.Static))
            {
                if (method.Name != "Button")
                    continue;
                var parameters = method.GetParameters();
                if (parameters.Length == 0)
                    continue;
                try
                {
                    if (parameters[0].ParameterType == typeof(string))
                    {
                        var hasOptions = false;
                        for (var i = 0; i < parameters.Length; i++)
                        {
                            if (parameters[i].ParameterType == typeof(GUILayoutOption[]) && parameters[i].Name == "options")
                                hasOptions = true;
                        }
                        if (!hasOptions)
                            continue;
                        var styled = parameters.Length >= 2 && parameters[1].ParameterType == typeof(GUIStyle);
                        harmony.Patch(method, prefix: new HarmonyMethod(typeof(ButtonTips), styled ? nameof(StyledPrefix) : nameof(ButtonPrefix)));
                    }
                    else if (parameters[0].ParameterType == typeof(GUIContent))
                    {
                        harmony.Patch(method, prefix: new HarmonyMethod(typeof(ButtonTips), nameof(ContentPrefix)));
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning("RoRCheatPanel button tips: " + ex.Message);
                }
            }

            var loc = AccessTools.TypeByName("RoRCheatPanel.Loc");
            var translate = loc?.GetMethod("T", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static, null, new[] { typeof(string) }, null);
            if (translate != null)
                harmony.Patch(translate, postfix: new HarmonyMethod(typeof(ButtonTips), nameof(AfterLoc)));
        }

        public static bool ButtonPrefix(string text, GUILayoutOption[] options, ref bool __result)
        {
            if (_busy)
                return true;
            var shown = TeamWords.Apply(text, PanelFixes.SelectionIncludesOthers());
            var tip = For(text);
            if (string.IsNullOrEmpty(tip))
            {
                if (shown == text)
                    return true;
                _busy = true;
                try
                {
                    __result = GUILayout.Button(shown, options ?? Array.Empty<GUILayoutOption>());
                }
                finally
                {
                    _busy = false;
                }

                return false;
            }
            _busy = true;
            try
            {
                __result = GUILayout.Button(new GUIContent(shown, tip), options ?? Array.Empty<GUILayoutOption>());
            }
            finally
            {
                _busy = false;
            }
            return false;
        }

        public static bool StyledPrefix(string text, GUIStyle style, GUILayoutOption[] options, ref bool __result)
        {
            if (_busy)
                return true;
            var shown = TeamWords.Apply(text, PanelFixes.SelectionIncludesOthers());
            var tip = For(text);
            if (string.IsNullOrEmpty(tip))
            {
                if (shown == text)
                    return true;
                _busy = true;
                try
                {
                    __result = GUILayout.Button(shown, style, options ?? Array.Empty<GUILayoutOption>());
                }
                finally
                {
                    _busy = false;
                }

                return false;
            }
            _busy = true;
            try
            {
                __result = GUILayout.Button(new GUIContent(shown, tip), style, options ?? Array.Empty<GUILayoutOption>());
            }
            finally
            {
                _busy = false;
            }
            return false;
        }

        public static void ContentPrefix(GUIContent content)
        {
            if (content == null || string.IsNullOrEmpty(content.text))
                return;
            var tip = For(content.text);
            if (string.IsNullOrEmpty(tip))
                return;
            if (string.IsNullOrEmpty(content.tooltip) || content.tooltip == content.text)
                content.tooltip = tip;
        }

        public static void AfterLoc(string key, ref string __result)
        {
            if (key == "hover_hint")
            {
                __result = Tr(
                    "Наведи курсор на кнопку, предмет или артефакт — всплывёт, что это делает.",
                    "Hover a button, item, or artifact to see what it does.",
                    "Pasa el cursor por un botón, objeto o artefacto para ver qué hace.");
            }
            else if (key == "fun_help")
            {
                __result = Tr(
                    "Бесполезное, но приятное. Часть кнопок — только хост. Наведи курсор на кнопку — всплывёт, что она делает.",
                    "Useless, but fun. Some buttons need the host. Hover a button to see what it does.",
                    "Inútil, pero divertido. Algunos botones necesitan anfitrión. Pasa el cursor por un botón para ver qué hace.");
            }
        }

        private static string For(string label)
        {
            if (string.IsNullOrEmpty(label))
                return null;
            Tips.TryGetValue(Lang() + "\n" + label, out var tip);
            return tip;
        }

        private static void Set(string lang, string label, string tip)
        {
            if (!string.IsNullOrEmpty(label))
                Tips[lang + "\n" + label] = tip;
        }

        private static void Add(string ruLabel, string enLabel, string esLabel, string ru, string en, string es)
        {
            Set("Ru", ruLabel, ru);
            Set("En", enLabel, en);
            Set("Es", esLabel, es);
        }

        private static void Hard(string label, string ru, string en, string es)
        {
            Set("Ru", label, ru);
            Set("En", label, en);
            Set("Es", label, es);
        }

        private static void Fill()
        {
            Add("Предметы", "Items", "Objetos",
                "Вкладка выдачи предметов выбранным игрокам.",
                "Tab for giving items to the selected players.",
                "Pestaña para dar objetos a los jugadores seleccionados.");
            Add("Снаряжение", "Equipment", "Equipo",
                "Вкладка снаряжения: надеть, снять, перезарядить.",
                "Equipment tab: equip, unequip, recharge.",
                "Pestaña de equipo: equipar, quitar, recargar.");
            Add("Пресеты", "Presets", "Presets",
                "Сохранённые наборы предметов из папки Presets.",
                "Saved item sets from the Presets folder.",
                "Conjuntos de objetos guardados en la carpeta Presets.");
            Add("Скиллы", "Skills", "Habilidades",
                "Смена умений выбранного персонажа.",
                "Change the selected survivor's skills.",
                "Cambiar las habilidades del superviviente seleccionado.");
            Add("Персонажи", "Characters", "Personajes",
                "Стать любым телом из каталога: выживший, монстр, дрон.",
                "Turn into any catalog body: survivor, monster, or drone.",
                "Convertirte en cualquier cuerpo del catálogo: superviviente, monstruo o dron.");
            Add("Ачивки", "Achievements", "Logros",
                "Выдача достижений. Себе — сразу, другим — если у них тоже стоит панель.",
                "Grant achievements. Yours apply immediately; others need this panel too.",
                "Dar logros. Los tuyos se aplican ya; a otros, solo si también tienen el panel.");
            Add("Ран", "Run", "Partida",
                "Сложность, скорость времени, артефакты и смена выжившего.",
                "Difficulty, time speed, artifacts, and survivor swap.",
                "Dificultad, velocidad del tiempo, artefactos y cambio de superviviente.");
            Add("Команды", "Teams", "Equipos",
                "Перевести выбранных в другую команду или убить команду.",
                "Move the selected players to another team, or kill a team.",
                "Pasar a los seleccionados a otro equipo, o matar un equipo.");
            Add("Хаос", "Chaos", "Caos",
                "Фановые кнопки: лут, размер, камера, ник, босс.",
                "Fun buttons: loot, size, camera, name, boss.",
                "Botones de caos: botín, tamaño, cámara, nombre, jefe.");
            Add("Инструменты", "Tools", "Herramientas",
                "Годмод, полёт, хил, телепорт, деньги, уровень и призыв.",
                "God mode, flight, heal, teleporter, money, level, and summons.",
                "God mode, vuelo, cura, teletransportador, dinero, nivel e invocaciones.");
            Add("О панели", "About", "Acerca de",
                "Язык, цвет панели, прозрачность и ссылка на канал.",
                "Language, panel color, opacity, and the channel link.",
                "Idioma, color del panel, opacidad y el enlace del canal.");

            Add("Игроки", "Players", "Jugadores",
                "Вкладка игроков: кик, бан, мут, годмод. В командах — вернуть выбранных в команду игроков.",
                "Players tab: boot, ban, mute, god mode. On Teams, this puts the selected players back on the player team.",
                "Pestaña de jugadores: echar, ban, mute, god. En Equipos, devuelve a los seleccionados al equipo de jugadores.");
            Add("Этапы", "Stages", "Etapas",
                "Вкладка перехода на карту. Как фильтр — показать обычные этапы в списке.",
                "Stage-travel tab. As a filter, show normal stages in the list.",
                "Pestaña para viajar de etapa. Como filtro, muestra las etapas normales en la lista.");
            Add("Монстры", "Monsters", "Monstruos",
                "В персонажах — показать монстров в списке. В командах — перевести выбранных в команду монстров.",
                "On Characters, show monsters in the list. On Teams, move the selected players to the monster team.",
                "En Personajes, muestra monstruos en la lista. En Equipos, pasa a los seleccionados al equipo de monstruos.");
            Add("Прочее", "Other", "Otros",
                "Показать или скрыть эту группу в списке: прочие тела или скрытые сцены.",
                "Show or hide this group in the list: other bodies, other items, or hidden scenes.",
                "Mostrar u ocultar este grupo en la lista: otros cuerpos, otros objetos o escenas ocultas.");
            Add("Другое", "Other", "Otros",
                "Показать предметы, которые не попали в обычные тиры.",
                "Show or hide this group in the list: other bodies, other items, or hidden scenes.",
                "Mostrar u ocultar este grupo en la lista: otros cuerpos, otros objetos o escenas ocultas.");

            Add("Случайный предмет", "Random item", "Objeto aleatorio",
                "Даёт выбранным игрокам один случайный предмет.",
                "Gives the selected players one random item.",
                "Da a los jugadores seleccionados un objeto al azar.");
            Add("10 случайных", "10 random", "10 aleatorios",
                "Даёт выбранным десять случайных предметов.",
                "Gives the selected players ten random items.",
                "Da a los seleccionados diez objetos al azar.");
            Add("Случайное снаряжение", "Random equipment", "Equipo aleatorio",
                "Надевает выбранным случайное снаряжение.",
                "Equips a random equipment on the selected players.",
                "Equipa un equipo al azar a los seleccionados.");
            Add("Случайный выживший", "Random survivor", "Superviviente aleatorio",
                "Меняет выбранных на случайного выжившего.",
                "Turns the selected players into a random survivor.",
                "Convierte a los seleccionados en un superviviente al azar.");
            Add("Дождь предметов", "Item rain", "Lluvia de objetos",
                "Роняет немного лута с неба рядом с тобой.",
                "Drops a little loot from the sky near you.",
                "Deja caer un poco de botín del cielo cerca de ti.");
            Add("Ливень", "Item storm", "Tormenta",
                "Роняет много лута с неба рядом с тобой.",
                "Drops a lot of loot from the sky near you.",
                "Deja caer mucho botín del cielo cerca de ti.");
            Add("Пнуть в небо", "Yeet", "Lanzar",
                "Подбрасывает выбранных высоко вверх.",
                "Launches the selected players high into the air.",
                "Lanza a los seleccionados muy arriba.");
            Add("Цитата", "Quote", "Cita",
                "Пишет случайную фразу в чат.",
                "Sends a random line to chat.",
                "Escribe una frase al azar en el chat.");
            Add("Дождь сундуков", "Chest rain", "Lluvia de cofres",
                "Ставит несколько сундуков вокруг тебя.",
                "Spawns a few chests around you.",
                "Pone algunos cofres a tu alrededor.");
            Add("Сундучный шторм", "Chest storm", "Tormenta de cofres",
                "Ставит очень много сундуков вокруг тебя.",
                "Spawns a lot of chests around you.",
                "Pone muchísimos cofres a tu alrededor.");
            Add("Случайный артефакт", "Random artifact", "Artefacto aleatorio",
                "Включает или выключает один случайный артефакт.",
                "Toggles one random artifact on or off.",
                "Activa o desactiva un artefacto al azar.");
            Add("Взрыв золота", "Gold explosion", "Explosión de oro",
                "Разбрасывает золото вокруг тебя.",
                "Scatters gold around you.",
                "Esparce oro a tu alrededor.");
            Add("Пинок", "Kick", "Patada",
                "Отталкивает игрока без урона.",
                "Knocks a player back without dealing damage.",
                "Empuja a un jugador sin hacerle daño.");
            Add("Пинок 10% HP", "Kick 10% HP", "Patada 10% HP",
                "Отталкивает игрока и снимает 10% здоровья.",
                "Knocks a player back and removes 10% health.",
                "Empuja a un jugador y le quita un 10% de vida.");
            Add("Я БОСС (с пафосом)", "I AM THE BOSS", "YO SOY EL JEFE",
                "Превращает выбранных в случайного босса и включает бессмертие. Диско-панель не трогает.",
                "Turns the selected players into a random boss and turns on god mode. Does not enable the disco panel.",
                "Convierte a los seleccionados en un jefe al azar y activa god mode. No enciende el panel disco.");
            Add("Кроха", "Tiny", "Mini",
                "Делает тело очень маленьким (масштаб 0.35).",
                "Makes the body very small (scale 0.35).",
                "Hace el cuerpo muy pequeño (escala 0.35).");
            Add("Норма", "Normal", "Normal",
                "Возвращает обычный размер тела. На вкладке снаряжения это же слово показывает обычное, не лунное, снаряжение.",
                "Sets body size back to normal. On the Equipment tab, the same word shows non-lunar gear.",
                "Devuelve el tamaño normal. En la pestaña Equipo, la misma palabra muestra el equipo que no es lunar.");
            Add("Обычное", "Normal", "Normal",
                "Показать в списке обычное снаряжение, не лунное.",
                "Sets body size back to normal. On the Equipment tab, the same word shows non-lunar gear.",
                "Devuelve el tamaño normal. En la pestaña Equipo, la misma palabra muestra el equipo que no es lunar.");
            Add("Гигант", "Giant", "Gigante",
                "Делает тело огромным (масштаб 3.2).",
                "Makes the body huge (scale 3.2).",
                "Hace el cuerpo enorme (escala 3.2).");
            Add("Титан", "Titan", "Titán",
                "Делает тело гигантским (масштаб 6).",
                "Makes the body gigantic (scale 6).",
                "Hace el cuerpo gigantesco (escala 6).");
            Add("Низкая гравитация", "Low gravity", "Gravedad baja",
                "Включает или выключает слабую гравитацию. Прыжки уносят далеко вверх.",
                "Toggles low gravity. Jumps carry you much higher.",
                "Activa o desactiva la gravedad baja. Los saltos suben mucho más.");
            Add("Диско-панель", "Disco panel", "Panel disco",
                "Панель начинает переливаться цветами. На персонажа не влияет.",
                "The panel cycles colors. It does not affect the character.",
                "El panel cambia de colores. No afecta al personaje.");
            Add("Третье лицо", "Third person", "Tercera persona",
                "Камера встаёт за спину. Дистанция — ползунок рядом.",
                "Puts the camera behind the character. Distance is the slider next to it.",
                "Pone la cámara detrás del personaje. La distancia es el control de al lado.");
            Add("Пьяная камера", "Drunk camera", "Cámara borracha",
                "Камера покачивается. Если картинка останется кривой, нажми «Восстановить камеру».",
                "The camera wobbles. If the view stays crooked, press Reset camera.",
                "La cámara se tambalea. Si la vista se queda torcida, pulsa Restaurar cámara.");
            Add("Восстановить камеру", "Reset camera", "Restaurar cámara",
                "Выравнивает камеру, если её перекосило после пьяного режима.",
                "Straightens the camera if drunk mode left it crooked.",
                "Endereza la cámara si el modo borracho la dejó torcida.");
            Add("Сменить ник", "Change name", "Cambiar nombre",
                "Ставит выбранным игрокам ник из поля слева.",
                "Sets the selected players' name to the text on the left.",
                "Pone a los seleccionados el nombre escrito a la izquierda.");
            Add("Я с луны", "From the moon", "De la luna",
                "Меняет ник на «я с луны».",
                "Changes the name to a moon line.",
                "Cambia el nombre a una frase de la luna.");
            Hard("БОСС",
                "Меняет ник на «БОСС». Это не превращение в босса.",
                "Changes the name to BOSS. This does not turn you into a boss.",
                "Cambia el nombre a JEFE. Esto no te convierte en un jefe.");
            Hard("60",
                "Вернуть поле зрения к обычным 60.",
                "Set the field of view back to 60.",
                "Devolver el campo de visión a 60.");
            Hard("8",
                "Вернуть дистанцию камеры от третьего лица к 8.",
                "Set the third-person camera distance back to 8.",
                "Devolver la distancia de la cámara en tercera persona a 8.");
            Hard("1x",
                "Вернуть скорость времени к обычной.",
                "Set the time speed back to normal.",
                "Devolver la velocidad del tiempo a la normal.");
            Hard("100",
                "Сделать панель полностью видимой (100%).",
                "Make the panel fully visible (100%).",
                "Dejar el panel totalmente visible (100%).");
            Hard("1",
                "Поставить количество выдачи на 1.",
                "Set the give amount to 1.",
                "Poner la cantidad a dar en 1.");
            Hard("10",
                "Поставить количество выдачи на 10.",
                "Set the give amount to 10.",
                "Poner la cantidad a dar en 10.");
            Hard("50",
                "Поставить количество выдачи на 50.",
                "Set the give amount to 50.",
                "Poner la cantidad a dar en 50.");
            Hard("-",
                "Уменьшить число на 1.",
                "Decrease the number by 1.",
                "Bajar el número en 1.");
            Hard("+",
                "Увеличить число на 1.",
                "Increase the number by 1.",
                "Subir el número en 1.");
            Hard("X",
                "Закрыть панель.",
                "Close the panel.",
                "Cerrar el panel.");
            Hard("Пылающий",
                "Красит выбранных в огненный цвет элиты. Бонуса элиты нет, только скин. Нажми ещё раз, чтобы снять.",
                "Paints the selected players with the blazing elite color. No elite bonus, skin only. Press again to clear.",
                "Pinta a los seleccionados con el color de élite llameante. Sin bonus, solo el skin. Pulsa otra vez para quitarlo.");
            Set("En", "Blazing", "Paints the selected players with the blazing elite color. No elite bonus, skin only. Press again to clear.");
            Set("Es", "Llameante", "Pinta a los seleccionados con el color de élite llameante. Sin bonus, solo el skin. Pulsa otra vez para quitarlo.");
            Add("Перегрузка", "Overloading", "Sobrecarga",
                "Красит выбранных в синий цвет элиты перегрузки. Бонуса нет, только скин. Нажми ещё раз, чтобы снять.",
                "Paints the selected players with the overloading elite color. No bonus, skin only. Press again to clear.",
                "Pinta a los seleccionados con el color de élite de sobrecarga. Sin bonus, solo el skin. Pulsa otra vez para quitarlo.");
            Add("Лёд", "Glacial", "Glacial",
                "Красит выбранных в ледяной цвет элиты. Бонуса нет, только скин. Нажми ещё раз, чтобы снять.",
                "Paints the selected players with the glacial elite color. No bonus, skin only. Press again to clear.",
                "Pinta a los seleccionados con el color de élite glacial. Sin bonus, solo el skin. Pulsa otra vez para quitarlo.");
            Add("Перетасовка", "Shuffle items", "Mezclar objetos",
                "Перемешивает предметы между выбранными игроками. Нужны хотя бы двое.",
                "Shuffles items between the selected players. Needs at least two.",
                "Mezcla los objetos entre los jugadores seleccionados. Hacen falta al menos dos.");
            Add("Все сюда", "All here", "Todos aquí",
                "Телепортирует монстров этого этапа к тебе.",
                "Teleports this stage's monsters to you.",
                "Teletransporta a ti a los monstruos de esta etapa.");
            Add("Салют", "Fireworks", "Fuegos",
                "Запускает фейерверк вокруг тебя и пишет фразу в чат.",
                "Sets off fireworks around you and sends a line to chat.",
                "Lanza fuegos artificiales a tu alrededor y escribe una frase en el chat.");
            Add("Сбросить хаос", "Reset chaos", "Resetear caos",
                "Возвращает тело до превращения в босса и выключает годмод, диско, низкую гравитацию, третье лицо и элитный цвет. Предметы и ник не трогает.",
                "Restores the body from before the boss transform and turns off god mode, disco, low gravity, third person, and elite colors. Items and the name stay.",
                "Devuelve el cuerpo de antes de ser jefe y apaga god mode, disco, gravedad baja, tercera persona y colores de élite. Los objetos y el nombre se quedan.");

            Add("Белые", "White", "Blancos",
                "Показать белые предметы в списке.",
                "Show white items in the list.",
                "Mostrar objetos blancos en la lista.");
            Add("Зелёные", "Green", "Verdes",
                "Показать зелёные предметы в списке.",
                "Show green items in the list.",
                "Mostrar objetos verdes en la lista.");
            Add("Красные", "Red", "Rojos",
                "Показать красные предметы в списке.",
                "Show red items in the list.",
                "Mostrar objetos rojos en la lista.");
            Add("Лунные", "Lunar", "Lunares",
                "Показать лунные предметы в списке.",
                "Show lunar items or lunar equipment in the list.",
                "Mostrar objetos lunares en la lista.");
            Add("Лунное", "Lunar", "Lunar",
                "Показать лунное снаряжение в списке.",
                "Show lunar items or lunar equipment in the list.",
                "Mostrar el equipo lunar en la lista.");
            Add("Босс", "Boss", "Jefe",
                "Показать боссовые предметы в списке.",
                "Show boss items in the list.",
                "Mostrar objetos de jefe en la lista.");
            Add("Воид", "Void", "Vacío",
                "Тема панели «Воид» или фильтр воид-предметов — смотря где кнопка.",
                "Show void items in the list.",
                "Mostrar objetos del vacío en la lista.");
            Hard("Void Fiend",
                "Сиреневая тема панели, цвет Воид Финда.",
                "Lavender panel theme, Void Fiend's color.",
                "Tema lavanda del panel, el color de Void Fiend.");
            Add("Еда", "Food", "Comida",
                "Показать еду в списке предметов.",
                "Show food items in the list.",
                "Mostrar comida en la lista de objetos.");
            Add("Скрытые", "Hidden", "Ocultos",
                "Показать скрытые предметы, которых нет в обычном списке.",
                "Show hidden items that are not in the normal list.",
                "Mostrar objetos ocultos que no están en la lista normal.");
            Add("Выдать все видимые", "Give all visible", "Dar todos los visibles",
                "Выдаёт все предметы из текущего фильтра выбранным игрокам, в выбранном количестве.",
                "Gives every item in the current filter to the selected players, using the chosen amount.",
                "Da todos los objetos del filtro actual a los seleccionados, en la cantidad elegida.");
            Add("Очистить инвентарь", "Clear inventory", "Vaciar inventario",
                "Удаляет все предметы у выбранных игроков.",
                "Removes every item from the selected players.",
                "Quita todos los objetos de los jugadores seleccionados.");
            Add("Убрать все", "Remove all", "Quitar todo",
                "Снимает все копии этого предмета у выбранных.",
                "Removes every copy of this item from the selected players.",
                "Quita todas las copias de este objeto a los seleccionados.");
            Add("Снять снаряжение", "Unequip", "Quitar equipo",
                "Снимает надетое снаряжение у выбранных.",
                "Unequips the selected players.",
                "Quita el equipo de los seleccionados.");
            Add("Мгновенная перезарядка", "Instant recharge", "Recarga instantánea",
                "Сразу заряжает снаряжение выбранных.",
                "Recharges the selected players' equipment immediately.",
                "Recarga al instante el equipo de los seleccionados.");
            Hard("Reset skills",
                "Возвращает умения персонажа к обычным для его тела.",
                "Restores the survivor's skills to the defaults for that body.",
                "Devuelve las habilidades a las normales de ese cuerpo.");
            Add("Сбросить кулдауны", "Reset cooldowns", "Resetear enfriamientos",
                "Сбрасывает перезарядку всех умений.",
                "Clears the cooldown on every skill.",
                "Quita el enfriamiento de todas las habilidades.");
            Add("Бесконечные скиллы", "Infinite skills", "Habilidades infinitas",
                "Умения больше не уходят на перезарядку, пока кнопка включена.",
                "Skills do not go on cooldown while this is on.",
                "Las habilidades no entran en enfriamiento mientras esto está activo.");
            Add("Выдать все ачивки", "Grant all achievements", "Dar todos los logros",
                "Открывает все достижения локальному профилю. Хост не нужен.",
                "Unlocks every achievement on the local profile. Host is not required.",
                "Desbloquea todos los logros del perfil local. No hace falta ser anfitrión.");
            Add("Все ачивки выбранным", "All ach. to selected", "Todos los logros a seleccionados",
                "Шлёт все достижения выбранным. Сработает, только если у них тоже стоит эта панель.",
                "Sends every achievement to the selected players. Works only if they also have this panel.",
                "Envía todos los logros a los seleccionados. Solo funciona si también tienen este panel.");
            Add("Создать из инвентаря", "Create from inventory", "Crear del inventario",
                "Сохраняет текущие предметы в новый пресет.",
                "Saves the current items as a new preset.",
                "Guarda los objetos actuales como un preset nuevo.");
            Add("Обновить список", "Refresh list", "Actualizar lista",
                "Перечитывает файлы пресетов с диска.",
                "Reloads preset files from disk.",
                "Vuelve a leer los presets del disco.");
            Add("Наложить", "Apply", "Aplicar",
                "Добавляет предметы пресета к тем, что уже есть.",
                "Adds the preset's items on top of what you already have.",
                "Añade los objetos del preset a los que ya tienes.");
            Add("Заменить инвентарь", "Replace inventory", "Reemplazar inventario",
                "Стирает предметы и ставит только то, что в пресете.",
                "Clears items and keeps only what is in the preset.",
                "Borra los objetos y deja solo los del preset.");
            Add("Удалить", "Delete", "Borrar",
                "Удаляет файл этого пресета.",
                "Deletes this preset file.",
                "Borra el archivo de este preset.");
            Add("Выжившие", "Survivors", "Supervivientes",
                "Показать выживших в списке персонажей.",
                "Show survivors in the character list.",
                "Mostrar supervivientes en la lista de personajes.");
            Add("Боссы", "Bosses", "Jefes",
                "Показать боссов в списке персонажей.",
                "Show bosses in the character list.",
                "Mostrar jefes en la lista de personajes.");
            Add("Дроны", "Drones", "Drones",
                "Показать дронов в списке персонажей.",
                "Show drones in the character list.",
                "Mostrar drones en la lista de personajes.");
            Add("Нейтралы", "Neutral", "Neutral",
                "Перевести выбранных в нейтральную команду: их никто не бьёт и они никого не бьют.",
                "Move the selected players to the neutral team: nobody fights them, and they fight nobody.",
                "Pasa a los seleccionados al equipo neutral: nadie les pega y ellos no pegan a nadie.");
            Add("Убить всех врагов", "Kill all enemies", "Matar enemigos",
                "Убивает всех врагов на текущей сцене.",
                "Kills every enemy on the current stage.",
                "Mata a todos los enemigos de la etapa actual.");
            Add("Убить команду", "Kill team", "Matar equipo",
                "Убивает всех, кто сейчас в выбранной команде.",
                "Kills everyone currently on the selected team.",
                "Mata a todos los que están ahora en el equipo elegido.");
            Hard("Kill",
                "Убить этого персонажа на сцене.",
                "Kill this character on the stage.",
                "Matar a este personaje en la escena.");
            Add("Интермиссии", "Intermissions", "Intermedios",
                "Показать промежуточные сцены, например базар и луну.",
                "Show in-between scenes, such as the bazaar and the moon.",
                "Mostrar escenas intermedias, como el bazar y la luna.");
            Add("Следующий", "Next", "Siguiente",
                "Назначить эту карту следующей после телепорта.",
                "Queue this stage as the next one after the teleporter.",
                "Poner esta etapa como la siguiente después del teletransportador.");
            Add("Сейчас", "Now", "Ahora",
                "Сразу загрузить эту карту, не дожидаясь телепорта.",
                "Load this stage immediately, without waiting for the teleporter.",
                "Cargar esta etapa ahora, sin esperar al teletransportador.");
            Add("Заморозить таймер рана", "Freeze run timer", "Congelar el temporizador",
                "Останавливает или снова запускает таймер забега. Скорость мира этим не ставится на паузу.",
                "Pauses or resumes the run timer. This does not freeze the world itself.",
                "Pausa o reanuda el temporizador de la partida. Esto no congela el mundo.");
            Add("Беск. снаряжение", "Inf. equipment", "Equipo inf.",
                "Снаряжение перезаряжается сразу, пока кнопка включена.",
                "Equipment recharges instantly while this is on.",
                "El equipo se recarga al instante mientras esto está activo.");
            Add("Полный хил", "Full heal", "Curar todo",
                "Восстанавливает здоровье выбранных до полного.",
                "Heals the selected players to full.",
                "Cura por completo a los jugadores seleccionados.");
            Add("Применить порталы", "Apply portals", "Aplicar portales",
                "Ставит на телепорт те порталы, которые включены кнопками выше.",
                "Places the portals that are toggled on above onto the teleporter.",
                "Pone en el teletransportador los portales que están activados arriba.");
            Add("Активировать", "Activate", "Activar",
                "Запускает телепорт на этой сцене, как будто к нему подошли.",
                "Starts the teleporter on this stage, as if someone walked up to it.",
                "Arranca el teletransportador de esta etapa, como si alguien se hubiera acercado.");
            Add("Зарядить", "Charge", "Cargar",
                "Сразу заполняет шкалу заряда телепорта.",
                "Fills the teleporter charge bar immediately.",
                "Llena al instante la barra de carga del teletransportador.");
            Add("Пинг", "Ping", "Ping",
                "Ставит пинг на телепорт, чтобы его было видно.",
                "Pings the teleporter so it is easy to see.",
                "Marca el teletransportador para que se vea.");
            Add("Выдать золото", "Give gold", "Dar oro",
                "Добавляет выбранным золото из поля выше.",
                "Adds the gold amount above to the selected players.",
                "Añade a los seleccionados el oro del campo de arriba.");
            Add("Выдать луну", "Give lunar", "Dar lunar",
                "Добавляет лунные монеты из поля выше.",
                "Adds the lunar coins from the field above.",
                "Añade las monedas lunares del campo de arriba.");
            Add("Уровень команды", "Team level", "Nivel del equipo",
                "Ставит уровень всей команды на число из поля выше.",
                "Sets the whole team's level to the number above.",
                "Pone el nivel de todo el equipo en el número de arriba.");
            Add("Прочитать текущие", "Read current", "Leer actuales",
                "Читает базовые статы тела в ползунки. Бонусы от предметов сюда не входят.",
                "Reads the body's base stats into the sliders. Item bonuses are not included.",
                "Lee las stats base del cuerpo en los controles. No incluye bonuses de objetos.");
            Hard("Apply changes",
                "Записывает значения ползунков статов в персонажа.",
                "Writes the stat sliders onto the character.",
                "Escribe los controles de stats en el personaje.");
            Add("Рядовые", "Grunts", "Comunes",
                "Показать в списке спавна обычных монстров, не боссов.",
                "Show regular monsters, not bosses, in the spawn list.",
                "Mostrar monstruos normales, no jefes, en la lista de invocación.");
            Add("Чемпионы", "Champions", "Campeones",
                "Показать в списке спавна боссов.",
                "Show bosses in the spawn list.",
                "Mostrar jefes en la lista de invocación.");
            Add("Без интеллекта", "No AI", "Sin IA",
                "Заспавненные стоят на месте, не атакуют и не взрываются сами.",
                "Spawned monsters stand still, do not attack, and do not detonate on their own.",
                "Los invocados se quedan quietos, no atacan y no explotan solos.");
            Add("Союзники", "Allies", "Aliados",
                "Монстры появляются в твоей команде и дерутся за тебя.",
                "Monsters spawn on your team and fight for you.",
                "Los monstruos aparecen en tu equipo y pelean por ti.");
            Add("Без лута", "No loot", "Sin botín",
                "С них не падает золото, опыт и боссовый предмет.",
                "They drop no gold, experience, or boss item.",
                "No sueltan oro, experiencia ni objeto de jefe.");
            Add("Спавн", "Spawn", "Invocar",
                "Вкладка спавна: NPC, предметы на землю, снаряжение и объекты карты. Настройки интеллекта, команды и лута сверху.",
                "Spawn tab: NPCs, ground items, equipment, and map objects. AI, team, and loot settings are at the top.",
                "Pestaña de invocación: PNJ, objetos en el suelo, equipo y cosas del mapa. Arriba están la IA, el equipo y el botín.");
            Add("НПС", "NPCs", "PNJ",
                "Список всех, кого можно вызвать: рядовые, боссы, дроны и выжившие.",
                "Everyone you can summon: grunts, bosses, drones, and survivors.",
                "Todos los que se pueden invocar: comunes, jefes, drones y supervivientes.");
            Add("На 4", "Set 4", "A 4",
                "Вернуть обычное лобби на 4 человека.",
                "Back to a normal 4-player lobby.",
                "Volver a una sala normal de 4.");
            Add("На 8", "Set 8", "A 8",
                "Лобби на 8 человек. Запиши, потом создавай сервер.",
                "8-player lobby. Apply it, then create the server.",
                "Sala de 8. Guárdala y luego crea el servidor.");
            Add("На 16", "Set 16", "A 16",
                "Лобби на 16 человек, потолок самой игры.",
                "16-player lobby, the game's own ceiling.",
                "Sala de 16, el tope de la propia partida.");
            Add("Клавиша", "Key", "Tecla",
                "Ждёт новую комбинацию. Можно с Ctrl, Shift или Alt. Escape отменяет.",
                "Waits for a new shortcut. Ctrl, Shift, or Alt work. Escape cancels.",
                "Espera una combinación nueva. Valen Ctrl, Shift o Alt. Escape cancela.");
            Add("Сброс", "Clear", "Borrar",
                "Снимает эту клавишу.",
                "Clears this shortcut.",
                "Quita esta tecla.");
            Add("Развернуть", "Restore", "Abrir",
                "Снова открывает панель.",
                "Opens the panel again.",
                "Vuelve a abrir el panel.");
            Add("Записать лобби", "Apply lobby", "Guardar sala",
                "Пишет число в размер лобби, слоты сервера и точки появления. Делай это в главном меню до создания сервера.",
                "Writes the number into the lobby size, server slots, and spawn points. Do this in the main menu before creating the server.",
                "Escribe el número en el tamaño de la sala, los huecos del servidor y los puntos de aparición. Hazlo en el menú principal antes de crear el servidor.");
            Add("Убрать вызванное", "Clear spawned", "Quitar invocado",
                "Удаляет NPC, лут и объекты, которые появились из этой вкладки.",
                "Removes the NPCs, loot, and objects this tab spawned.",
                "Quita los PNJ, el botín y los objetos que invocó esta pestaña.");
            Add("Перед собой", "In front", "Delante",
                "Ставит вызванное перед персонажем.",
                "Places the spawn in front of you.",
                "Pone lo invocado delante del personaje.");
            Add("Под ноги", "At feet", "A los pies",
                "Ставит вызванное прямо у ног.",
                "Places the spawn at your feet.",
                "Pone lo invocado a tus pies.");
            Add("Кольцо", "Ring", "Anillo",
                "Ставит вызванное кольцом вокруг персонажа.",
                "Places the spawn in a ring around you.",
                "Pone lo invocado en un anillo a tu alrededor.");
            Add("Без элиты", "No elite", "Sin élite",
                "Обычный цвет модели, без элитного скина.",
                "Normal model color, no elite skin.",
                "Color normal del modelo, sin skin de élite.");
            Add("Огонь", "Fire", "Fuego",
                "Только огненный цвет элиты, без бонуса.",
                "Blazing elite color only, no bonus.",
                "Solo el color de élite de fuego, sin bonus.");
            Add("Молния", "Lightning", "Rayo",
                "Только синий цвет элиты, без бонуса.",
                "Overloading elite color only, no bonus.",
                "Solo el color de élite de rayo, sin bonus.");
            Add("Объекты", "Objects", "Cosas",
                "Сундуки, бочки, святилища и прочие объекты карты.",
                "Chests, barrels, shrines, and other map objects.",
                "Cofres, barriles, santuarios y demás objetos del mapa.");
            Add("Спавн", "Spawn", "Invocar",
                "Создаёт выбранного монстра перед тобой, столько раз, сколько стоит в количестве.",
                "Creates that monster in front of you, as many times as the count says.",
                "Crea ese monstruo delante de ti, tantas veces como diga la cantidad.");
            Add("Армия", "Army", "Ejército",
                "Призывает толпу союзников через игровой призыв армии.",
                "Summons a crowd of allies through the in-game army call.",
                "Invoca una muchedumbre de aliados con la llamada de ejército del juego.");
            Hard("Godmode",
                "Бессмертие выбранных. Выключи — и урон снова проходит, ждать смену этапа не нужно.",
                "Selected players cannot die. Turn it off and damage works again, without waiting for a stage change.",
                "Los seleccionados no pueden morir. Al apagarlo, el daño vuelve a funcionar, sin esperar un cambio de etapa.");
            Hard("Noclip",
                "Полёт сквозь стены: WASD, Space вверх, Q вниз, Shift быстрее.",
                "Fly through walls: WASD, Space up, Q down, Shift faster.",
                "Vuela a través de las paredes: WASD, Space arriba, Q abajo, Shift más rápido.");
            Hard("Gold",
                "После «Применить порталы» на телепорте будет портал в золотой берег.",
                "After Apply portals, the teleporter gets a gold coast portal.",
                "Después de Aplicar portales, el teletransportador tendrá un portal a la costa de oro.");
            Hard("Bazaar",
                "После «Применить порталы» будет портал в базар.",
                "After Apply portals, the teleporter gets a bazaar portal.",
                "Después de Aplicar portales, habrá un portal al bazar.");
            Hard("Celestial",
                "После «Применить порталы» будет портал на небесный остров.",
                "After Apply portals, the teleporter gets a celestial portal.",
                "Después de Aplicar portales, habrá un portal celestial.");
            Hard("Hidden Realm",
                "После «Применить порталы» будет портал в скрытое царство.",
                "After Apply portals, the teleporter gets a hidden realm portal.",
                "Después de Aplicar portales, habrá un portal al reino oculto.");
            Hard("Scan area",
                "Ищет интерактивные объекты рядом и пишет, что нашёл.",
                "Looks for nearby interactables and reports what it found.",
                "Busca objetos interactivos cerca y dice qué encontró.");
            Hard("Gummy Clone",
                "Призывает клонов-мармеладок, как снаряжение клона.",
                "Summons gummy clones, the same way the clone equipment does.",
                "Invoca clones de gominola, igual que el equipo del clon.");
            Hard("Drone Backup",
                "Призывает дронов, как снаряжение запаса дронов.",
                "Summons drones, the same way the drone backup equipment does.",
                "Invoca drones, igual que el equipo de reserva de drones.");
            Add("Выгнать", "Boot", "Echar",
                "Выкидывает игрока с сервера. Себя выгнать нельзя.",
                "Removes the player from the server. You cannot boot yourself.",
                "Echa al jugador del servidor. No puedes echarte a ti.");
            Add("Бан", "Ban", "Ban",
                "Банит игрока: зайти снова не сможет, пока не разбанишь.",
                "Bans the player. They cannot rejoin until you unban them.",
                "Banea al jugador. No podrá volver hasta que lo desbanees.");
            Add("Разбан", "Unban", "Desbanear",
                "Снимает бан, игрок снова может зайти.",
                "Removes the ban so the player can join again.",
                "Quita el ban para que el jugador pueda entrar otra vez.");
            Add("Мут", "Mute", "Mute",
                "Глушит или возвращает чат этого игрока.",
                "Mutes or unmutes this player's chat.",
                "Silencia o devuelve el chat de este jugador.");
            Add("Заморозка", "Freeze", "Congelar",
                "Останавливает этого игрока на месте. Это не таймер забега.",
                "Freezes this player in place. This is not the run timer.",
                "Deja a este jugador quieto. Esto no es el temporizador de la partida.");
            Add("Сказать", "Say", "Decir",
                "Пишет текст из поля в общий чат.",
                "Sends the text from the field to chat.",
                "Escribe en el chat el texto del campo.");
            Add("Выбранных ко мне", "Selected to me", "Seleccionados a mí",
                "Телепортирует отмеченных игроков к тебе.",
                "Teleports the checked players to you.",
                "Teletransporta a ti a los jugadores marcados.");
            Add("Всех ко мне", "Everyone to me", "Todos a mí",
                "Телепортирует всех остальных игроков к тебе.",
                "Teleports every other player to you.",
                "Teletransporta a ti a todos los demás jugadores.");
            Add("Ко мне", "To me", "A mí",
                "Телепортирует этого игрока к тебе.",
                "Teleports this player to you.",
                "Teletransporta a este jugador hacia ti.");
            Add("К нему", "Go to", "Ir a",
                "Телепортирует тебя к этому игроку.",
                "Teleports you to this player.",
                "Te teletransporta hasta este jugador.");
            Add("Да", "Yes", "Sí",
                "Подтвердить действие.",
                "Confirm the action.",
                "Confirmar la acción.");
            Add("Нет", "No", "No",
                "Отменить действие.",
                "Cancel the action.",
                "Cancelar la acción.");
            Add("Обновить каталоги", "Refresh catalogs", "Actualizar catálogos",
                "Заново читает предметы, карты и тела из игры.",
                "Rereads items, stages, and bodies from the game.",
                "Vuelve a leer objetos, etapas y cuerpos del juego.");
            Hard("Русский",
                "Переключить панель на русский.",
                "Switch the panel to Russian.",
                "Cambiar el panel al ruso.");
            Hard("English",
                "Переключить панель на английский.",
                "Switch the panel to English.",
                "Cambiar el panel al inglés.");
            Hard("Español",
                "Переключить панель на испанский.",
                "Switch the panel to Spanish.",
                "Cambiar el panel al español.");
            Add("Ночь", "Night", "Noche",
                "Тёмная тема панели.",
                "Dark panel theme.",
                "Tema oscuro del panel.");
            Add("Коммандо", "Commando", "Commando",
                "Оранжевая тема панели, цвет Коммандо.",
                "Orange panel theme, Commando's color.",
                "Tema naranja del panel, el color de Commando.");
            Add("Охотница", "Huntress", "Huntress",
                "Красная тема панели, цвет Охотницы.",
                "Red panel theme, Huntress's color.",
                "Tema rojo del panel, el color de Huntress.");
            Add("Изобретательница", "Artificer", "Artificer",
                "Синяя тема панели, цвет Изобретательницы.",
                "Blue panel theme, Artificer's color.",
                "Tema azul del panel, el color de Artificer.");
            Add("Акрид", "Acrid", "Acrid",
                "Зелёная тема панели, цвет Акрида.",
                "Green panel theme, Acrid's color.",
                "Tema verde del panel, el color de Acrid.");
            Hard("REX",
                "Бирюзовая тема панели, цвет REX.",
                "Teal panel theme, REX's color.",
                "Tema turquesa del panel, el color de REX.");
            Add("Рейлганнер", "Railgunner", "Railgunner",
                "Фиолетовая тема панели, цвет Рейлганнера.",
                "Purple panel theme, Railgunner's color.",
                "Tema morado del panel, el color de Railgunner.");
            Add("Погрузчик", "Loader", "Loader",
                "Золотая тема панели, цвет Погрузчика.",
                "Gold panel theme, Loader's color.",
                "Tema dorado del panel, el color de Loader.");
            Add("Бандит", "Bandit", "Bandit",
                "Розовая тема панели, цвет Бандита.",
                "Pink panel theme, Bandit's color.",
                "Tema rosa del panel, el color de Bandit.");
            Add("God", "God", "God",
                "Бессмертие этого игрока. Выключи — и урон снова проходит.",
                "This player cannot die. Turn it off and damage works again.",
                "Este jugador no puede morir. Al apagarlo, el daño vuelve a funcionar.");
            Add("Инвентарь", "Inventory", "Inventario",
                "Предметы выбранного игрока: снять, задать стак, скопировать или обменять.",
                "The selected player's items: remove, set the stack, copy, or swap.",
                "Objetos del jugador elegido: quitar, fijar la pila, copiar o cambiar.");
            Add("Предмет −", "Item −", "Objeto −",
                "Предыдущий предмет в инвентаре выбранного.",
                "Previous item in the selected inventory.",
                "Objeto anterior del inventario elegido.");
            Add("Предмет +", "Item +", "Objeto +",
                "Следующий предмет в инвентаре выбранного.",
                "Next item in the selected inventory.",
                "Siguiente objeto del inventario elegido.");
            Add("Снять 1", "Remove 1", "Quitar 1",
                "Убирает одну штуку текущего предмета.",
                "Removes one of the current item.",
                "Quita una unidad del objeto actual.");
            Add("Задать стак", "Set stack", "Fijar pila",
                "Ставит количество текущего предмета из поля слева.",
                "Sets the current item's count from the field on the left.",
                "Pone la cantidad del objeto actual según el campo de la izquierda.");
            Add("Копировать", "Copy", "Copiar",
                "Копирует предметы первого выбранного остальным выбранным.",
                "Copies the first selected player's items to the other selected players.",
                "Copia los objetos del primero a los demás seleccionados.");
            Add("Обменять", "Swap", "Cambiar",
                "Меняет инвентари двух первых выбранных.",
                "Swaps the inventories of the first two selected players.",
                "Intercambia los inventarios de los dos primeros elegidos.");
            Add("Враги", "Enemies", "Enemigos",
                "Куда смотрят враги, их команда и призыв боссов.",
                "Where enemies look, their team, and boss summons.",
                "Adónde miran los enemigos, su equipo y la invocación de jefes.");
            Add("Целиться в выбранного", "Aim at selected", "Apuntar al elegido",
                "Живые враги выбирают целью выбранного игрока.",
                "Living enemies target the selected player.",
                "Los enemigos vivos apuntan al jugador elegido.");
            Add("Без цели", "No target", "Sin objetivo",
                "Выключает интеллект живым врагам, пока кнопка включена.",
                "Turns off living enemies' AI while this stays on.",
                "Apaga la IA de los enemigos vivos mientras siga activo.");
            Add("Команда игрока", "Player team", "Equipo jugador",
                "Переводит живых врагов в команду игроков.",
                "Moves living enemies onto the player team.",
                "Pasa a los enemigos vivos al equipo de los jugadores.");
            Add("Команда монстров", "Monster team", "Equipo monstruo",
                "Возвращает их в команду монстров.",
                "Sends them back to the monster team.",
                "Los devuelve al equipo de los monstruos.");
            Add("Дождь боссов", "Boss rain", "Lluvia de jefes",
                "Призывает несколько боссов рядом с тобой.",
                "Summons several bosses near you.",
                "Invoca varios jefes cerca de ti.");
            Add("Воскресить", "Revive", "Revivir",
                "Поднимает этого игрока, если он умер в текущем забеге.",
                "Brings this player back if they died during the current run.",
                "Levanta a este jugador si murió en la partida actual.");
            Add("Воскресить мёртвых", "Revive the dead", "Revivir a los muertos",
                "Поднимает всех игроков, которые уже умерли в этом забеге.",
                "Brings back every player who has already died this run.",
                "Levanta a todos los jugadores que ya murieron en esta partida.");
            Add("Диско-панель", "Disco panel", "Panel disco",
                "Панель переливается цветом. На игру не влияет.",
                "The panel cycles colors. It does not affect the game.",
                "El panel cambia de color. No afecta a la partida.");
            Add("Восстановить камеру", "Reset camera", "Restaurar cámara",
                "Возвращает поле зрения и дистанцию камеры к обычным.",
                "Returns the field of view and camera distance to normal.",
                "Devuelve el campo de visión y la distancia de la cámara a lo normal.");
            Add("Кроха", "Tiny", "Mini",
                "Маленький масштаб модели и камера чуть дальше.",
                "A small model scale, and the camera moves out a little.",
                "Escala pequeña del modelo, y la cámara se aleja un poco.");
            Add("Норма", "Normal", "Normal",
                "Обычный размер модели и обычная камера.",
                "Normal model size and the normal camera.",
                "Tamaño normal del modelo y cámara normal.");
            Add("Гигант", "Giant", "Gigante",
                "Крупная модель. Камера отъезжает, чтобы персонаж влез в кадр.",
                "A large model. The camera backs up so the character fits.",
                "Modelo grande. La cámara retrocede para que el personaje quepa.");
            Add("Титан", "Titan", "Titán",
                "Очень крупная модель и далёкая камера.",
                "A very large model and a far camera.",
                "Modelo enorme y cámara lejana.");
            Add("Большое тело", "Large body", "Cuerpo grande",
                "Меняет выбранных на крупное тело из каталога. Это видят все.",
                "Turns the selected players into a large catalog body. Everyone sees it.",
                "Convierte a los elegidos en un cuerpo grande del catálogo. Todos lo ven.");
            Add("Маленькое тело", "Small body", "Cuerpo pequeño",
                "Меняет выбранных на маленькое тело из каталога. Это видят все.",
                "Turns the selected players into a small catalog body. Everyone sees it.",
                "Convierte a los elegidos en un cuerpo pequeño del catálogo. Todos lo ven.");
            Add("Вернуть выжившего", "Restore survivor", "Volver al superviviente",
                "Возвращает выбранным их исходного выжившего.",
                "Returns the selected players to their original survivor.",
                "Devuelve a los elegidos su superviviente original.");
            Add("Выдать золото", "Give gold", "Dar oro",
                "Добавляет золото из поля выбранным и в общую казну команды.",
                "Adds the gold from the field to the selected players and the team purse.",
                "Suma el oro del campo a los elegidos y a la bolsa del equipo.");
        }

        private static string Lang()
        {
            var loc = Type.GetType("RoRCheatPanel.Loc, RoRCheatPanel");
            var current = loc?.GetField("Current", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)?.GetValue(null);
            return current != null ? current.ToString() : "Ru";
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
    }
}
