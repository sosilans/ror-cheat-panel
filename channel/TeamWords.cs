namespace RoRCheatPanel.Channel
{
    internal static class TeamWords
    {
        public static string Apply(string label, bool others)
        {
            if (!others || string.IsNullOrEmpty(label))
                return label;
            const string ru = "Стать: ";
            const string en = "Join: ";
            const string es = "Unirse: ";
            if (label.StartsWith(ru))
                return "Сделать: " + label.Substring(ru.Length);
            if (label.StartsWith(en))
                return "Make: " + label.Substring(en.Length);
            if (label.StartsWith(es))
                return "Hacer: " + label.Substring(es.Length);
            return label;
        }
    }
}
