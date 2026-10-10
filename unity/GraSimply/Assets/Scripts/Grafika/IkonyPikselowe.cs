using System.Collections.Generic;

namespace GraSimply.Grafika
{
    /// <summary>
    /// Ikony surowców 7×7 w pikselach — zastępują emoji, których krój
    /// pikselowy nie ma. Klucze jak w <see cref="Logika.Oboz.Surowce"/>
    /// plus „zloto”.
    /// </summary>
    public static class IkonyPikselowe
    {
        private static readonly Dictionary<string, string[]> Wzory = new Dictionary<string, string[]>
        {
            { "zywnosc", new[] { "..rr...", ".rRRr..", "rRRRRr.", "rRRRRr.", ".rRRrw.", "...www.", "....ww." } },
            { "drewno", new[] { ".......", ".bbbbb.", "bBBBBBb", "bBoBBBb", "bBBBBBb", ".bbbbb.", "......." } },
            { "kamien", new[] { ".......", "..ggg..", ".gGGGg.", "gGGGGGg", "gGGGggg", ".ggggg.", "......." } },
            { "ziola", new[] { "...l...", ".l.l.l.", "..lll..", ".llLll.", "...l...", "...s...", "...s..." } },
            { "skora", new[] { ".......", "kk...kk", ".kKKKk.", ".kKKKk.", ".kKKKk.", "kk...kk", "......." } },
            { "ruda", new[] { "...ss..", "..s.ss.", ".s..s..", "...s...", "..s.yy.", ".s.yYy.", "....y.." } },
            { "deski", new[] { ".......", "bbbbbbb", "BBBBBBB", "bbbbbbb", "BBBBBBB", "bbbbbbb", "......." } },
            { "zelazo", new[] { ".......", ".ss.ss.", "sSSsSSs", "sSSsSSs", ".ss.ss.", ".......", "......." } },
            { "zloto", new[] { "..yyy..", ".yYYYy.", "yYyyyYy", "yYyYyYy", "yYyyyYy", ".yYYYy.", "..yyy.." } },
        };

        private static readonly Dictionary<char, Barwa> Barwy = new Dictionary<char, Barwa>
        {
            { 'r', new Barwa(140, 40, 24) }, { 'R', new Barwa(200, 72, 48) }, { 'w', new Barwa(232, 220, 192) },
            { 'b', new Barwa(90, 56, 24) }, { 'B', new Barwa(140, 92, 44) }, { 'o', new Barwa(200, 152, 80) },
            { 'g', new Barwa(84, 80, 76) }, { 'G', new Barwa(140, 136, 132) }, { 'l', new Barwa(60, 112, 40) },
            { 'L', new Barwa(120, 176, 72) }, { 's', new Barwa(108, 108, 116) }, { 'S', new Barwa(176, 176, 184) },
            { 'k', new Barwa(108, 72, 40) }, { 'K', new Barwa(160, 120, 72) }, { 'y', new Barwa(176, 124, 32) },
            { 'Y', new Barwa(252, 216, 96) },
        };

        public static bool Ma(string klucz) => klucz != null && Wzory.ContainsKey(klucz);

        /// <summary>Rysuje ikonę lewym górnym rogiem w (x, y); nieznany klucz — nic.</summary>
        public static void Rysuj(Plotno cel, string klucz, int x, int y)
        {
            if (!Ma(klucz))
            {
                return;
            }
            string[] wzor = Wzory[klucz];
            for (int j = 0; j < wzor.Length; j++)
            {
                for (int i = 0; i < wzor[j].Length; i++)
                {
                    if (Barwy.TryGetValue(wzor[j][i], out Barwa b))
                    {
                        cel.Ustaw(x + i, y + j, b);
                    }
                }
            }
        }
    }
}
