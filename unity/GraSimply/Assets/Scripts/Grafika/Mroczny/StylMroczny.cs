using GraSimply.Grafika.Mroczny;

namespace GraSimply.Grafika
{
    public sealed partial class Styl
    {
        private static Styl _mroczny;

        /// <summary>
        /// Mroczne fantasy w języku obrazu pierwszego Fallouta: ciągły grunt
        /// w rzucie trimetrycznym, prerenderowane bryły, ciemność poza mapą,
        /// noc z mnożenia i jedna wspólna paleta. Interfejs fantasy: ciemne
        /// drewno, złote okucia, pergamin i krój pikselowy z polskimi znakami.
        ///
        /// Leniwie, nie w inicjalizatorze pola: ta część klasy leży w innym
        /// pliku, a kolejność inicjalizacji pól statycznych między plikami
        /// częściowej klasy nie jest określona.
        /// </summary>
        public static Styl Mroczny => _mroczny ?? (_mroczny = ZbudujMroczny());

        private static Styl ZbudujMroczny()
        {
            // Rampy terenu klasycznego zostają — czyta je tylko generator
            // klasyczny, a pełny obiekt nie zostawia nigdzie nulli.
            var s = (Styl)Klasyczny.MemberwiseClone();
            s.Nazwa = "mroczny";
            s.Generator = new GeneratorMroczny();

            s.NieboGora = s.NieboDol = s.NieboGoraZmierzch = s.NieboDolZmierzch = new Barwa(0, 0, 0);
            s.Kontur = new Barwa(12, 10, 8);

            // Interfejs: ciemne drewno, złote okucia, tekst barwy pergaminu.
            s.Tlo = Paleta.Siatka6(0x12, 0x0C, 0x08);
            s.TloPanelu = Paleta.Siatka6(0x24, 0x18, 0x10);
            s.RamkaJasna = Paleta.Siatka6(0xC8, 0xA0, 0x50);
            s.RamkaCiemna = Paleta.Siatka6(0x6C, 0x4C, 0x24);
            s.Cwiek = Paleta.Siatka6(0xE0, 0xB8, 0x60);
            s.Wyrozniony = Paleta.Siatka6(0xFC, 0xC4, 0x5C);
            s.Tekst = Paleta.Siatka6(0xE4, 0xD4, 0xB0);
            s.Przygaszony = Paleta.Siatka6(0xA0, 0x88, 0x60);
            s.PasekHp = Paleta.Siatka6(0xB4, 0x24, 0x1C);
            s.PasekMany = Paleta.Siatka6(0x40, 0x60, 0xC8);
            s.Ozdobnik = Paleta.Siatka6(0x7C, 0x60, 0x38);

            s.Emoji = false;
            s.Kroj = "Kroje/DepartureMono-Regular";
            s.SzerokoscZnaku = 10;
            return s;
        }
    }
}
