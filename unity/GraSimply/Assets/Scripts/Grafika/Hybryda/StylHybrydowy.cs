namespace GraSimply.Grafika
{
    public sealed partial class Styl
    {
        private static Styl _hybrydowy;

        /// <summary>
        /// Pixelart i Mroczny w jednym: interfejs, rzut, grunt, światło i paleta
        /// stylu Mroczny, ale obiekty (chaty, namiot, ludzie) jako pixelart.
        /// Jedyny styl ze sceną osady z bliska i chodzącymi osadnikami.
        ///
        /// Leniwie, z tego samego powodu co <see cref="Mroczny"/>: kolejność
        /// inicjalizacji pól statycznych między plikami klasy częściowej
        /// nie jest określona.
        /// </summary>
        public static Styl Hybrydowy => _hybrydowy ?? (_hybrydowy = ZbudujHybrydowy());

        private static Styl ZbudujHybrydowy()
        {
            var s = (Styl)Mroczny.MemberwiseClone();
            s.Nazwa = "hybrydowy";
            s.Generator = new Hybryda.GeneratorHybrydowy();
            return s;
        }
    }
}
