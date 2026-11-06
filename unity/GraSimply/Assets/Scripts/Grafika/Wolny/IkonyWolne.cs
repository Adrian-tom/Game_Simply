using GraSimply.Grafika.Mroczny;

namespace GraSimply.Grafika.Wolny
{
    /// <summary>Małe ikony kafelków, około 24×24 piksele natywne, rysowane wokół środka (cx, cy).</summary>
    public static class IkonyWolne
    {
        public static void Rysuj(Plotno c, string klucz, int cx, int cy)
        {
            switch (klucz)
            {
                case "buduj":
                case "gornik":
                    c.Odcinek(Materialy.Deski[6], cx - 8, cy + 9, cx + 4, cy - 4);
                    c.Odcinek(Materialy.Deski[6], cx - 7, cy + 9, cx + 5, cy - 4);
                    c.Wypelnij(Materialy.Kamien[4], cx - 2, cy - 9, 12, 6);
                    c.Wypelnij(Materialy.Kamien[2], cx - 2, cy - 5, 12, 2);
                    break;
                case "magazyn":
                    c.Wypelnij(Materialy.Deski[3], cx - 10, cy - 5, 20, 14);
                    c.Wypelnij(Materialy.Deski[6], cx - 10, cy - 8, 20, 5);
                    c.Wypelnij(Materialy.Kosc[7], cx - 2, cy - 4, 4, 5);
                    break;
                case "osada":
                case "namiot":
                    Namiot(c, cx, cy);
                    break;
                case "jedz":
                case "mysliwy":
                    foreach ((int dx, int dy) in new[] { (-5, 1), (4, 2), (-1, -5) })
                    {
                        c.Elipsa(Materialy.SukmanaCzerwona[5], cx + dx - 4, cy + dy - 4, 8, 8);
                    }
                    c.Wypelnij(Materialy.Listowie[6], cx - 1, cy - 10, 4, 3);
                    break;
                case "spij":
                case "bezczynny":
                    c.Elipsa(Materialy.Kosc[7], cx - 8, cy - 8, 16, 16);
                    c.Elipsa(Materialy.Pyl[0], cx - 3, cy - 10, 14, 14);
                    break;
                case "warsztat":
                    c.Wypelnij(Materialy.Deski[3], cx - 11, cy - 2, 22, 4);
                    c.Wypelnij(Materialy.Deski[6], cx - 9, cy + 2, 3, 8);
                    c.Wypelnij(Materialy.Deski[6], cx + 6, cy + 2, 3, 8);
                    c.Wypelnij(Materialy.Kamien[4], cx - 5, cy - 8, 8, 5);
                    break;
                case "tartak":
                    c.Elipsa(Materialy.Kamien[4], cx - 9, cy - 9, 18, 18);
                    c.Elipsa(Materialy.Kamien[2], cx - 3, cy - 3, 6, 6);
                    c.Wypelnij(Materialy.Deski[3], cx - 12, cy + 6, 24, 4);
                    break;
                case "kuznia":
                    c.Wypelnij(Materialy.Kamien[2], cx - 10, cy + 4, 20, 6);
                    c.Wypelnij(Materialy.Kamien[4], cx - 12, cy - 3, 24, 6);
                    c.Wypelnij(Materialy.Kamien[2], cx + 2, cy - 7, 8, 5);
                    break;
                case "budowniczy":
                    c.Odcinek(Materialy.Deski[6], cx - 8, cy + 9, cx + 4, cy - 4);
                    c.Wypelnij(Materialy.Kamien[4], cx - 2, cy - 9, 12, 6);
                    break;
                case "rzemieslnik":
                    c.Wypelnij(Materialy.Deski[3], cx - 10, cy - 5, 20, 14);
                    c.Wypelnij(Materialy.Kamien[4], cx - 4, cy - 10, 8, 6);
                    break;
                case "palisada":
                    foreach (int ox in new[] { -9, -3, 3, 9 })
                    {
                        c.Wypelnij(Materialy.Deski[3], cx + ox - 2, cy - 7, 5, 17);
                        c.Wypelnij(Materialy.Deski[6], cx + ox - 1, cy - 10, 3, 3);
                    }
                    c.Wypelnij(Materialy.Kora[1], cx - 11, cy - 2, 23, 2);
                    break;
                case "brama":
                    c.Wypelnij(Materialy.Deski[3], cx - 11, cy - 9, 4, 19);
                    c.Wypelnij(Materialy.Deski[3], cx + 7, cy - 9, 4, 19);
                    c.Wypelnij(Materialy.Deski[6], cx - 11, cy - 11, 22, 4);
                    c.Wypelnij(Materialy.Kora[1], cx - 6, cy - 6, 12, 2);
                    break;
                case "wieza":
                    c.Wypelnij(Materialy.Kamien[2], cx - 5, cy - 4, 10, 14);
                    c.Wypelnij(Materialy.Kamien[4], cx - 7, cy - 7, 14, 4);
                    for (int r = 0; r < 5; r++)
                    {
                        c.Wypelnij(Materialy.SukmanaCzerwona[5], cx - 6 + r, cy - 8 - r, 12 - 2 * r, 1);
                    }
                    break;
                case "straznik":
                    c.Wypelnij(Materialy.Kamien[4], cx - 8, cy - 8, 16, 11);
                    for (int r = 0; r < 8; r++)
                    {
                        c.Wypelnij(Materialy.Kamien[4], cx - 8 + r, cy + 3 + r, 16 - 2 * r, 1);
                    }
                    c.Wypelnij(Materialy.SukmanaCzerwona[5], cx - 1, cy - 6, 2, 13);
                    c.Wypelnij(Materialy.SukmanaCzerwona[5], cx - 5, cy - 2, 10, 2);
                    break;
                case "wir":
                    c.Elipsa(Materialy.Kosc[7], cx - 10, cy - 10, 20, 20, 2);
                    c.Elipsa(Materialy.Kamien[4], cx - 5, cy - 5, 10, 10, 2);
                    c.Wypelnij(Materialy.Kosc[7], cx + 6, cy - 12, 6, 3);
                    break;
                case "szarza":
                    foreach (int ox in new[] { -10, -3, 4 })
                    {
                        c.Odcinek(Materialy.Kosc[7], cx + ox, cy - 7, cx + ox + 6, cy);
                        c.Odcinek(Materialy.Kosc[7], cx + ox + 6, cy, cx + ox, cy + 7);
                    }
                    break;
                case "okrzyk":
                    c.Elipsa(Materialy.SukmanaCzerwona[5], cx - 4, cy - 4, 8, 8);
                    c.Elipsa(Materialy.Kosc[7], cx - 9, cy - 9, 18, 18, 1);
                    c.Elipsa(Materialy.Kosc[6], cx - 13, cy - 13, 26, 26, 1);
                    break;
                case "spiwor":
                    c.Wypelnij(Materialy.SukmanaCzerwona[3], cx - 11, cy - 4, 22, 9);
                    c.Wypelnij(Materialy.SukmanaCzerwona[5], cx - 10, cy - 4, 20, 3);
                    c.Wypelnij(Materialy.Kosc[6], cx - 11, cy - 4, 6, 8);
                    break;
                case "ognisko":
                    c.Wypelnij(Materialy.Kora[1], cx - 9, cy + 5, 18, 4);
                    c.Wypelnij(Materialy.Kamien[4], cx - 11, cy + 7, 4, 3);
                    c.Wypelnij(Materialy.Kamien[4], cx + 7, cy + 7, 4, 3);
                    c.Elipsa(Materialy.SukmanaCzerwona[6], cx - 6, cy - 7, 12, 13);
                    c.Elipsa(Materialy.Kosc[7], cx - 3, cy - 1, 6, 6);
                    break;
                case "chata":
                    c.Wypelnij(Materialy.Deski[3], cx - 9, cy - 1, 18, 11);
                    c.Wypelnij(Materialy.Pyl[0], cx - 2, cy + 3, 5, 7);
                    for (int r = 0; r < 9; r++)
                    {
                        c.Wypelnij(Materialy.Strzecha[5], cx - 12 + r, cy - 1 - r, 24 - 2 * r, 1);
                    }
                    break;
                case "drwal":
                    c.Wypelnij(Materialy.Kora[1], cx - 2, cy + 3, 4, 7);
                    c.Elipsa(Materialy.Listowie[3], cx - 8, cy - 10, 16, 14);
                    c.Elipsa(Materialy.Listowie[6], cx - 5, cy - 8, 8, 6);
                    break;
                case "kamieniarz":
                    c.Elipsa(Materialy.Kamien[2], cx - 10, cy - 5, 20, 14);
                    c.Elipsa(Materialy.Kamien[4], cx - 6, cy - 4, 10, 7);
                    break;
                case "zielarz":
                    c.Wypelnij(Materialy.Listowie[3], cx - 1, cy - 3, 2, 12);
                    c.Elipsa(Materialy.Listowie[6], cx - 8, cy - 6, 8, 6);
                    c.Elipsa(Materialy.Listowie[6], cx, cy - 9, 8, 6);
                    break;
            }
        }

        private static void Namiot(Plotno c, int cx, int cy)
        {
            for (int r = 0; r < 18; r++)
            {
                int pol = 12 - (int)(r * 0.64);
                c.Wypelnij(Materialy.Strzecha[5], cx - pol, cy + 9 - r, pol, 1);
                c.Wypelnij(Materialy.Strzecha[3], cx, cy + 9 - r, pol, 1);
            }
            c.Wypelnij(Materialy.Pyl[0], cx - 2, cy + 1, 5, 9);
        }
    }
}
