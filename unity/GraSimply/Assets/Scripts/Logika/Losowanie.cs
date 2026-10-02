using System;
using System.Collections.Generic;

namespace GraSimply.Logika
{
    /// <summary>
    /// Generator losowy zgodny bit w bit z <c>random.Random</c> z CPythona.
    ///
    /// Świat gry (biomy, punkty, bossowie) powstaje z seeda postaci, więc port
    /// musi powtarzać nie tylko „jakieś” losowanie, ale dokładnie to samo —
    /// inaczej ten sam zapis dałby w Unity inną mapę niż w wersji pythonowej.
    /// Dlatego siedzi tu Mersenne Twister MT19937 razem z semantyką metod
    /// <c>random()</c>, <c>getrandbits()</c>, <c>randrange()</c>, <c>choice()</c>
    /// i <c>sample()</c> przepisaną z CPythona (Modules/_randommodule.c
    /// oraz Lib/random.py).
    /// </summary>
    public sealed class Losowanie
    {
        private const int N = 624;
        private const int M = 397;
        private const uint MATRIX_A = 0x9908b0dfu;
        private const uint UPPER_MASK = 0x80000000u;
        private const uint LOWER_MASK = 0x7fffffffu;

        private readonly uint[] _mt = new uint[N];
        private int _mti = N + 1;
        private int _zuzyte;

        /// <summary>
        /// Ile słów generator już oddał. Nie jest potrzebne grze, ale pozwala
        /// testom sprawdzić rzecz, której inaczej nie widać: że dana akcja
        /// zużywa dokładnie tyle losowań co w wersji pythonowej. Jedno losowanie
        /// za dużo przesuwa cały dalszy świat przy tym samym seedzie.
        /// </summary>
        public int Zuzyte => _zuzyte;

        /// <summary>Jak <c>random.Random(seed)</c> dla nieujemnej liczby całkowitej.</summary>
        public Losowanie(long seed)
        {
            Zasiej(seed);
        }

        /// <summary>Jak <c>random.Random()</c> — zasiew z zegara, bez powtarzalności.</summary>
        public Losowanie()
        {
            Zasiej(DateTime.UtcNow.Ticks & 0x7FFFFFFF);
        }

        // ------------------------------------------------------------------ //
        //  Zasiew                                                             //
        // ------------------------------------------------------------------ //

        /// <summary>
        /// CPython bierze wartość bezwzględną seeda i rozbija ją na 32-bitowe
        /// słowa od prawej, a potem woła <c>init_by_array</c>. Seedy w grze
        /// mieszczą się w 31 bitach, ale rozbicie jest tu ogólne, żeby nie
        /// zaskoczyło przy większej liczbie.
        /// </summary>
        private void Zasiej(long seed)
        {
            ulong n = seed < 0 ? (ulong)(-seed) : (ulong)seed;
            var slowa = new List<uint>();
            if (n == 0)
            {
                slowa.Add(0u);
            }
            else
            {
                while (n > 0)
                {
                    slowa.Add((uint)(n & 0xFFFFFFFFu));
                    n >>= 32;
                }
            }
            InitByArray(slowa.ToArray());
        }

        private void InitGenrand(uint s)
        {
            _mt[0] = s;
            for (uint i = 1; i < N; i++)
            {
                _mt[i] = unchecked(1812433253u * (_mt[i - 1] ^ (_mt[i - 1] >> 30)) + i);
            }
            _mti = N;
        }

        private void InitByArray(uint[] klucz)
        {
            InitGenrand(19650218u);
            int i = 1;
            int j = 0;
            int k = Math.Max(N, klucz.Length);
            for (; k > 0; k--)
            {
                _mt[i] = unchecked((_mt[i] ^ ((_mt[i - 1] ^ (_mt[i - 1] >> 30)) * 1664525u))
                                   + klucz[j] + (uint)j);
                i++;
                j++;
                if (i >= N)
                {
                    _mt[0] = _mt[N - 1];
                    i = 1;
                }
                if (j >= klucz.Length)
                {
                    j = 0;
                }
            }
            for (k = N - 1; k > 0; k--)
            {
                _mt[i] = unchecked((_mt[i] ^ ((_mt[i - 1] ^ (_mt[i - 1] >> 30)) * 1566083941u))
                                   - (uint)i);
                i++;
                if (i >= N)
                {
                    _mt[0] = _mt[N - 1];
                    i = 1;
                }
            }
            _mt[0] = 0x80000000u;
            _mti = N;
        }

        // ------------------------------------------------------------------ //
        //  Rdzeń MT19937                                                      //
        // ------------------------------------------------------------------ //

        private uint GenrandUint32()
        {
            uint y;
            if (_mti >= N)
            {
                int kk;
                for (kk = 0; kk < N - M; kk++)
                {
                    y = (_mt[kk] & UPPER_MASK) | (_mt[kk + 1] & LOWER_MASK);
                    _mt[kk] = _mt[kk + M] ^ (y >> 1) ^ ((y & 0x1u) != 0 ? MATRIX_A : 0u);
                }
                for (; kk < N - 1; kk++)
                {
                    y = (_mt[kk] & UPPER_MASK) | (_mt[kk + 1] & LOWER_MASK);
                    _mt[kk] = _mt[kk + (M - N)] ^ (y >> 1) ^ ((y & 0x1u) != 0 ? MATRIX_A : 0u);
                }
                y = (_mt[N - 1] & UPPER_MASK) | (_mt[0] & LOWER_MASK);
                _mt[N - 1] = _mt[M - 1] ^ (y >> 1) ^ ((y & 0x1u) != 0 ? MATRIX_A : 0u);
                _mti = 0;
            }

            _zuzyte++;
            y = _mt[_mti++];
            y ^= y >> 11;
            y ^= (y << 7) & 0x9d2c5680u;
            y ^= (y << 15) & 0xefc60000u;
            y ^= y >> 18;
            return y;
        }

        // ------------------------------------------------------------------ //
        //  Odpowiedniki metod z Pythona                                       //
        // ------------------------------------------------------------------ //

        /// <summary>Jak <c>random.random()</c> — 53 bity mantysy z dwóch słów.</summary>
        public double Losowa()
        {
            uint a = GenrandUint32() >> 5;
            uint b = GenrandUint32() >> 6;
            return (a * 67108864.0 + b) * (1.0 / 9007199254740992.0);
        }

        /// <summary>Jak <c>random.getrandbits(k)</c> dla k do 64.</summary>
        public ulong Bity(int k)
        {
            if (k <= 0)
            {
                return 0UL;
            }
            if (k <= 32)
            {
                return GenrandUint32() >> (32 - k);
            }
            if (k > 64)
            {
                throw new ArgumentOutOfRangeException(nameof(k), "Obsługiwane jest do 64 bitów.");
            }
            // CPython składa słowa od najmniej znaczącego, ostatnie przycina.
            ulong wynik = GenrandUint32();
            int zostalo = k - 32;
            ulong gorne = GenrandUint32() >> (32 - zostalo);
            return wynik | (gorne << 32);
        }

        /// <summary>
        /// Jak <c>_randbelow_with_getrandbits(n)</c>: losuje tyle bitów, ile ma
        /// <c>n</c>, i powtarza, póki nie trafi w zakres. Odrzucanie prób jest
        /// tu istotne — to ono ustala, ile słów zużyje generator.
        /// </summary>
        public int Ponizej(int n)
        {
            if (n <= 0)
            {
                return 0;
            }
            int k = DlugoscBitowa((uint)n);
            ulong r = Bity(k);
            while (r >= (ulong)n)
            {
                r = Bity(k);
            }
            return (int)r;
        }

        private static int DlugoscBitowa(uint n)
        {
            int ile = 0;
            while (n > 0)
            {
                ile++;
                n >>= 1;
            }
            return ile;
        }

        /// <summary>Jak <c>random.randrange(poczatek, koniec)</c> (koniec wyłączny).</summary>
        public int Zakres(int poczatek, int koniec)
        {
            return poczatek + Ponizej(koniec - poczatek);
        }

        /// <summary>Jak <c>random.randint(a, b)</c> (oba końce włącznie).</summary>
        public int Calkowita(int a, int b)
        {
            return Zakres(a, b + 1);
        }

        /// <summary>Jak <c>random.choice(seq)</c>.</summary>
        public T Wybierz<T>(IReadOnlyList<T> lista)
        {
            if (lista.Count == 0)
            {
                throw new InvalidOperationException("Nie da się wybrać z pustej listy.");
            }
            return lista[Ponizej(lista.Count)];
        }

        /// <summary>
        /// Jak <c>random.sample(population, k)</c> — losowanie bez zwracania.
        /// CPython wybiera jedną z dwóch strategii w zależności od rozmiarów;
        /// obie są tu odwzorowane, bo różnią się liczbą zużytych losowań.
        /// </summary>
        public List<T> Probka<T>(IReadOnlyList<T> populacja, int k)
        {
            int n = populacja.Count;
            if (k < 0 || k > n)
            {
                throw new ArgumentOutOfRangeException(nameof(k), "Próbka większa niż populacja.");
            }

            var wynik = new List<T>(k);
            int rozmiarZbioru = 21;
            if (k > 5)
            {
                rozmiarZbioru += (int)Math.Pow(4, Math.Ceiling(Math.Log(k * 3.0, 4.0)));
            }

            if (n <= rozmiarZbioru)
            {
                var pula = new List<T>(populacja);
                for (int i = 0; i < k; i++)
                {
                    int j = Ponizej(n - i);
                    wynik.Add(pula[j]);
                    pula[j] = pula[n - i - 1];
                }
            }
            else
            {
                var wybrane = new HashSet<int>();
                for (int i = 0; i < k; i++)
                {
                    int j = Ponizej(n);
                    while (wybrane.Contains(j))
                    {
                        j = Ponizej(n);
                    }
                    wybrane.Add(j);
                    wynik.Add(populacja[j]);
                }
            }
            return wynik;
        }
    }
}
