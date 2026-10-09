using GraSimply.Grafika.Mroczny;

namespace GraSimply.Grafika
{
    public sealed partial class Styl
    {
        private static Styl _mroczny;

        /// <summary>
        /// Mroczne fantasy w języku obrazu pierwszego Fallouta: ciągły grunt
        /// w rzucie trimetrycznym, prerenderowane bryły, ciemność poza mapą,
        /// noc z mnożenia i jedna wspólna paleta. Chrom jak konsola z metalu:
        /// szaro-zielone panele, mosiężne nity, bursztynowy tekst.
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

            s.Tlo = Paleta.Siatka6(0x10, 0x0C, 0x08);
            s.TloPanelu = Paleta.Siatka6(0x30, 0x28, 0x20);
            s.RamkaJasna = Paleta.Siatka6(0x94, 0xA4, 0x94);
            s.RamkaCiemna = Paleta.Siatka6(0x58, 0x68, 0x60);
            s.Cwiek = Paleta.Siatka6(0x9C, 0x74, 0x34);
            s.Wyrozniony = Paleta.Siatka6(0xFC, 0xC4, 0x5C);
            s.Tekst = Paleta.Siatka6(0xD0, 0xB8, 0x88);
            s.Przygaszony = Paleta.Siatka6(0x98, 0x7C, 0x50);
            s.PasekHp = Paleta.Siatka6(0xD4, 0x2C, 0x1C);
            s.PasekMany = Paleta.Siatka6(0x48, 0x70, 0xE0);
            s.Ozdobnik = Paleta.Siatka6(0x68, 0x58, 0x3C);

            s.ChromMetalowy = true;
            s.Plyta = Paleta.Siatka6(0x6C, 0x74, 0x6C);
            s.Ekran = Paleta.Siatka6(0x16, 0x12, 0x0C);
            s.PasekWylaczony = Paleta.Siatka6(0x28, 0x28, 0x28);
            s.Emoji = false;
            s.Kroj = "Kroje/DepartureMono-Regular";
            s.SzerokoscZnaku = 10;
            return s;
        }
    }
}
