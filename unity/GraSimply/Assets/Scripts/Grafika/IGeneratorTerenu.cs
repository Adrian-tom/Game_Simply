using System.Collections.Generic;

namespace GraSimply.Grafika
{
    /// <summary>
    /// Wszystko, co scena mapy musi wiedzieć o stylu rysowania: jak duży jest
    /// kafel, jak wygląda teren i obiekty, gdzie stoi postać, czym jest tło.
    ///
    /// Scena (<see cref="ScenaMapy"/>) zna tylko kolejność rysowania i światło —
    /// kafle, sprite'y i obróbkę bierze stąd. Dzięki temu drugi styl to osobny
    /// generator, a nie sieć warunków w środku sceny, i stary wygląd zostaje
    /// nietknięty co do bajtu.
    /// </summary>
    public interface IGeneratorTerenu
    {
        /// <summary>Szerokość rombu kafla.</summary>
        int TW { get; }

        /// <summary>Wysokość rombu kafla.</summary>
        int TH { get; }

        /// <summary>Rozdzielczość sceny w pikselach stylu.</summary>
        int SzerSceny { get; }

        int WysSceny { get; }

        /// <summary>
        /// Ile razy scena jest powiększana na pulpicie. Iloczyn z rozmiarem
        /// sceny daje zawsze 640×480 — układ pulpitu jest ten sam w każdym stylu.
        /// </summary>
        int SkalaSceny { get; }

        /// <summary>Pionowa pozycja górnego wierzchołka pola (0, 0).</summary>
        int Y0 { get; }

        int WysokoscPola(string biom, int x, int y, int s);

        /// <summary>Ile klatek animacji ma kafel danego biomu (woda faluje).</summary>
        int Klatki(string biom);

        /// <summary>Kafel z blokiem terenu; lewy górny róg leży w (sx − TW/2, sy − h).</summary>
        Plotno Kafel(string biom, int h, int s, int klatka);

        Plotno Zamglij(Plotno kafel, int x, int y, bool noc);

        Plotno Obwodka();

        /// <summary>Dekoracje względem środka wierzchu pola; sprite null = ognisko.</summary>
        List<Dekoracja> Dekoracje(string biom, string punkt, int s);

        Plotno OsadaObozu(int chaty, int palisada, int wieza);

        /// <summary>Gdzie przyłożyć lewy górny róg obrazka osady względem środka pola.</summary>
        (int Dx, int Dy) KotwicaOsady { get; }

        void Ognisko(Plotno cel, int x, int y, double t);

        /// <summary>Przesunięcie źródła światła ogniska względem jego podstawy.</summary>
        int SwiatloOgniskaDy { get; }

        Dictionary<string, (string Rodzaj, int Dx, int Dy)> SwiatlaPunktow { get; }

        /// <summary>Lewy górny róg sprite'a postaci stojącej na polu.</summary>
        (int X, int Y) PozycjaGracza((int X, int Y) srodek, bool naPunkcie);

        /// <summary>Gdzie świeci latarnia postaci, względem jej pozycji.</summary>
        (int Dx, int Dy) SwiatloGracza { get; }

        /// <summary>Postać z cieniem i znacznikiem nad głową.</summary>
        void RysujGracza(Plotno cel, int x, int y, string klasa, double t);

        /// <summary>Tło sceny pod mapą — niebo albo ciemność, zależnie od stylu.</summary>
        Plotno Tlo(bool zmierzch);

        /// <summary>Maska do mnożenia: przyciemnione brzegi kadru.</summary>
        Plotno Winieta();

        /// <summary>Obróbka gotowej klatki (ziarno, wspólna paleta); styl klasyczny nic tu nie robi.</summary>
        void Obrobka(Plotno cel, double t);
    }
}
