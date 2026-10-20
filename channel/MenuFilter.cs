using System.Collections.Generic;

namespace RoRCheatPanel.Channel
{
    internal static class MenuFilter
    {
        private static readonly HashSet<string> Hidden = new HashSet<string>
        {
            "Ливень", "Item storm", "Tormenta",
            "Сундучный шторм", "Chest storm", "Tormenta de cofres",
            "Цитата", "Quote", "Cita",
            "Пинок 10% HP", "Kick 10% HP", "Patada 10% HP",
            "Пьяная камера", "Drunk camera", "Cámara borracha",
            "Выдать все видимые", "Give all visible", "Dar todos los visibles"
        };

        private static readonly HashSet<string> Once = new HashSet<string>
        {
            "Всех ко мне", "Everyone to me", "Todos a mí",
            "Все сюда", "All here", "Todos aquí"
        };

        public static bool Drop(string text, int frame, ref int seenFrame, HashSet<string> seen)
        {
            if (string.IsNullOrEmpty(text) || seen == null)
                return false;
            if (Hidden.Contains(text))
                return true;
            if (!Once.Contains(text))
                return false;
            if (seenFrame != frame)
            {
                seenFrame = frame;
                seen.Clear();
            }

            return !seen.Add(text);
        }
    }
}
