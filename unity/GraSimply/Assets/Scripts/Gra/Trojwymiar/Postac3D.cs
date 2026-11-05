using System.Collections.Generic;
using GraSimply.Grafika.Trojwymiar;
using UnityEngine;

namespace GraSimply.Gra.Trojwymiar
{
    /// <summary>
    /// Animowana postać: model KayKit plus klipy ze wspólnego szkieletu Rig_Medium. Klipy leżą w osobnych
    /// plikach, więc jedna animacja chodu działa na bohaterze, osadnikach i szkieletach.
    /// </summary>
    public sealed class Postac3D
    {
        public const float Wzrost = 1.15f;

        public const string Spoczynek = "Idle_A";
        public const string Chod = "Walking_A";
        public const string Bieg = "Running_A";
        public const string Praca = "Interact";
        public const string Sen = "Death_A_Pose";
        public const string CiosLekki = "Melee_1H_Attack_Slice_Diagonal";
        public const string CiosCiezki = "Melee_2H_Attack_Chop";
        public const string CiosSztych = "Melee_1H_Attack_Stab";
        public const string CiosWir = "Melee_2H_Attack_Spinning";
        public const string CiosKrzyk = "Melee_Block_Attack";
        public const string CiosPiesc = "Melee_Unarmed_Attack_Punch_A";
        public const string Zaklecie = "Ranged_Magic_Shoot";
        public const string Blok = "Melee_Blocking";
        public const string Unik = "Dodge_Forward";
        public const string Rana = "Hit_A";
        public const string Smierc = "Death_A";

        private static readonly HashSet<string> Zapetlone = new HashSet<string> { Spoczynek, Chod, Bieg, Praca, Blok };
        private static readonly HashSet<string> Potrzebne = new HashSet<string>
        {
            Spoczynek, Chod, Bieg, Praca, Sen, CiosLekki, CiosCiezki, CiosSztych, CiosWir, CiosKrzyk, CiosPiesc,
            Zaklecie, Blok, Unik, Rana, Smierc,
        };

        private static Dictionary<string, AnimationClip> _klipy;

        public readonly GameObject Obiekt;
        public string Model;

        private readonly Animation _animacja;
        private string _gra;

        public Postac3D(Modele modele, string model, Transform rodzic, float wzrost = Wzrost)
        {
            Model = model;
            Obiekt = modele.Wez(new WpisModelu(model, wzrost), rodzic);
            _animacja = Obiekt.GetComponentInChildren<Animation>();
            if (_animacja == null)
            {
                _animacja = Obiekt.transform.GetChild(0).gameObject.AddComponent<Animation>();
            }
            _animacja.playAutomatically = false;
            _animacja.cullingType = AnimationCullingType.AlwaysAnimate;
            foreach (KeyValuePair<string, AnimationClip> k in Klipy())
            {
                if (_animacja.GetClip(k.Key) == null)
                {
                    _animacja.AddClip(k.Value, k.Key);
                }
            }
            Graj(Spoczynek);
        }

        private static Dictionary<string, AnimationClip> Klipy()
        {
            if (_klipy != null)
            {
                return _klipy;
            }
            _klipy = new Dictionary<string, AnimationClip>();
            foreach (string plik in new[] { "Rig_Medium_General", "Rig_Medium_MovementBasic", "Rig_Medium_CombatMelee", "Rig_Medium_CombatRanged", "Rig_Medium_MovementAdvanced" })
            {
                foreach (AnimationClip c in Resources.LoadAll<AnimationClip>("Modele/Postacie/Animacje/" + plik))
                {
                    if (Potrzebne.Contains(c.name))
                    {
                        c.wrapMode = Zapetlone.Contains(c.name) ? WrapMode.Loop : WrapMode.ClampForever;
                        _klipy[c.name] = c;
                    }
                }
            }
            return _klipy;
        }

        public void Graj(string klip)
        {
            if (_gra == klip || _animacja.GetClip(klip) == null)
            {
                return;
            }
            _gra = klip;
            _animacja.CrossFade(klip, 0.15f);
        }

        /// <summary>Animacja jednorazowa od początku, rozciągnięta do czasu akcji z logiki: ostrze trafia, gdy trafia cios.</summary>
        public void Cios(string klip, double czas)
        {
            AnimationState stan = _animacja[klip];
            if (stan == null)
            {
                return;
            }
            stan.speed = stan.length / Mathf.Max(0.05f, (float)czas);
            stan.time = 0f;
            _gra = klip;
            _animacja.CrossFade(klip, 0.06f);
        }

        /// <summary>Ustawia postać na polu gry i obraca w stronę kierunku (osie gry).</summary>
        public void Ustaw(double x, double y, double kx, double ky)
        {
            Obiekt.transform.position = Scena3D.NaSwiat(x, y);
            if (Mathf.Abs((float)kx) + Mathf.Abs((float)ky) > 1e-3f)
            {
                Obiekt.transform.rotation = Quaternion.LookRotation(new Vector3((float)kx, 0f, -(float)ky), Vector3.up);
            }
        }
    }
}
