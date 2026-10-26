"""Ruch osadników: ścieżki, przeszkody, rezerwacje pól i rytm dnia.

Moduł ``game.ruch`` nie zna pygame, więc te testy działają wszędzie. Sprawdzają
rzeczy, które da się stwierdzić liczbą: czy osadnik dochodzi, czy nie przechodzi
przez budynek, czy dwóch nie stoi na jednym polu i czy ten sam przebieg daje
ten sam wynik.
"""
import math
import unittest

from game import ruch
from game.ruch import DLUGOSC_DNIA, PREDKOSC, Ruch, Siatka, faza, sciezka, swiatlo, zadanie


def _symuluj(r: Ruch, sekundy: float, krok: float = 0.05, po_kroku=None) -> None:
    for _ in range(int(sekundy / krok)):
        r.aktualizuj(krok)
        if po_kroku:
            po_kroku()


class TestSciezek(unittest.TestCase):
    def test_prosta_ma_tyle_pol_co_odleglosc(self):
        s = Siatka(7)
        trasa = sciezka(s, (0, 0), (4, 0))
        self.assertEqual(trasa, [(1, 0), (2, 0), (3, 0), (4, 0)])

    def test_start_rowny_celowi_to_pusta_trasa(self):
        self.assertEqual(sciezka(Siatka(7), (2, 2), (2, 2)), [])

    def test_trasa_omija_przeszkode(self):
        s = Siatka(7, {(2, 0), (2, 1), (2, 2)})
        trasa = sciezka(s, (0, 1), (4, 1))
        self.assertIsNotNone(trasa)
        self.assertFalse(set(trasa) & s.zablokowane)
        self.assertEqual(trasa[-1], (4, 1))

    def test_zamkniety_cel_nie_ma_trasy(self):
        # Cel otoczony murem ze wszystkich stron.
        mur = {(2, 2), (3, 2), (4, 2), (2, 3), (4, 3), (2, 4), (3, 4), (4, 4)}
        self.assertIsNone(sciezka(Siatka(7, mur), (0, 0), (3, 3)))

    def test_cel_zablokowany_nie_ma_trasy(self):
        self.assertIsNone(sciezka(Siatka(7, {(3, 3)}), (0, 0), (3, 3)))

    def test_po_skosie_nie_scina_rogu(self):
        # Dwa pola po skosie, z murem po obu bokach przekątnej — nie wolno się przecisnąć.
        s = Siatka(7, {(1, 0), (0, 1)})
        self.assertIsNone(sciezka(s, (0, 0), (1, 1)))

    def test_skos_bywa_krotszy_niz_dwa_proste_kroki(self):
        trasa = sciezka(Siatka(7), (0, 0), (3, 3))
        self.assertEqual(len(trasa), 3)

    def test_najblizsze_wolne(self):
        s = Siatka(7, {(3, 3)})
        n = s.najblizsze_wolne((3, 3))
        self.assertIsNotNone(n)
        self.assertNotIn(n, s.zablokowane)
        self.assertEqual(abs(n[0] - 3) + abs(n[1] - 3), 1)


class TestRytmuDnia(unittest.TestCase):
    def test_fazy_po_kolei(self):
        d = DLUGOSC_DNIA
        self.assertEqual(faza(0.0), "rano")
        self.assertEqual(faza(d * 0.3), "praca")
        self.assertEqual(faza(d * 0.7), "wieczor")
        self.assertEqual(faza(d * 0.9), "noc")

    def test_doba_sie_powtarza(self):
        self.assertEqual(faza(5.0), faza(5.0 + 3 * DLUGOSC_DNIA))

    def test_poludnie_jasniejsze_od_polnocy(self):
        self.assertGreater(swiatlo(DLUGOSC_DNIA * 0.3), swiatlo(DLUGOSC_DNIA * 0.9))
        for i in range(0, 90):
            self.assertTrue(0.0 <= swiatlo(float(i)) <= 1.0)

    def test_swiatlo_jest_ciagle(self):
        # Brak skoków między sąsiednimi klatkami — inaczej scena by migała.
        prev = swiatlo(0.0)
        for i in range(1, 900):
            teraz = swiatlo(i * 0.1)
            self.assertLess(abs(teraz - prev), 0.05)
            prev = teraz

    def test_chory_zostaje_w_domu(self):
        o = {"imie": "Olek", "chory": 1}
        for pora in ("rano", "praca", "wieczor", "noc"):
            self.assertEqual(zadanie(o, (1, 1), (2, 2), (3, 3), (5, 4), pora)[0], (2, 2))

    def test_praca_wieczor_noc(self):
        o = {"imie": "Jagna"}
        self.assertEqual(zadanie(o, (1, 1), (2, 2), (3, 3), None, "praca"), ((1, 1), "praca"))
        self.assertEqual(zadanie(o, (1, 1), (2, 2), (3, 3), None, "wieczor"), ((3, 3), "ognisko"))
        self.assertEqual(zadanie(o, (1, 1), (2, 2), (3, 3), None, "noc"), ((2, 2), "sen"))

    def test_wieczorem_czesc_idzie_do_tawerny_i_wybor_jest_staly(self):
        imiona = ["Nessa", "Torin", "Elka", "Bram", "Sira", "Olek", "Mila", "Gareth"]
        wybory = [zadanie({"imie": i}, (1, 1), (2, 2), (3, 3), (5, 4), "wieczor")[1]
                  for i in imiona]
        self.assertIn("tawerna", wybory)
        self.assertIn("ognisko", wybory)
        self.assertEqual(wybory, [zadanie({"imie": i}, (1, 1), (2, 2), (3, 3), (5, 4),
                                          "wieczor")[1] for i in imiona])


class TestRuchu(unittest.TestCase):
    def test_postac_dochodzi_w_czasie_wynikajacym_z_predkosci(self):
        r = Ruch()
        r.dodaj("Olek", (0, 0))
        r.ustaw_cel("Olek", (4, 0))
        _symuluj(r, 4 / PREDKOSC + 0.3)
        self.assertTrue(r.dotarl("Olek"))
        x, y = r.pozycja("Olek")
        self.assertAlmostEqual(x, 4.0, places=2)
        self.assertAlmostEqual(y, 0.0, places=2)

    def test_nie_dochodzi_wczesniej_niz_pozwala_predkosc(self):
        r = Ruch()
        r.dodaj("Olek", (0, 0))
        r.ustaw_cel("Olek", (6, 0))
        _symuluj(r, 3.0)
        x, _ = r.pozycja("Olek")
        self.assertLessEqual(x, 3.0 * PREDKOSC + 1e-6)
        self.assertFalse(r.dotarl("Olek"))

    def test_nigdy_nie_wchodzi_w_budynek(self):
        mur = {(2, 0), (2, 1), (2, 2), (2, 3), (2, 4)}
        r = Ruch()
        r.przebuduj(mur)
        r.dodaj("Olek", (0, 2))
        r.ustaw_cel("Olek", (4, 2))

        def sprawdz():
            x, y = r.pozycja("Olek")
            for bx, by in mur:
                # Środek postaci nie może wejść w kwadrat zablokowanego pola.
                self.assertFalse(abs(x - bx) < 0.5 and abs(y - by) < 0.5, (x, y))
        _symuluj(r, 12, po_kroku=sprawdz)
        self.assertTrue(r.dotarl("Olek"))

    def test_cel_w_budynku_konczy_sie_tuz_obok(self):
        r = Ruch()
        r.przebuduj({(3, 3)})
        r.dodaj("Olek", (0, 0))
        r.ustaw_cel("Olek", (3, 3), "praca")
        _symuluj(r, 10)
        self.assertTrue(r.dotarl("Olek"))
        px, py = r.postacie["Olek"].pole
        self.assertEqual(max(abs(px - 3), abs(py - 3)), 1)
        self.assertEqual(r.postacie["Olek"].czynnosc, "praca")

    def test_przy_budynku_staje_od_frontu(self):
        r = Ruch()
        r.przebuduj({(3, 3)})
        r.dodaj("Olek", (0, 0))
        r.ustaw_cel("Olek", (3, 3))
        _symuluj(r, 10)
        self.assertEqual(r.postacie["Olek"].pole, (3, 4))

    def test_dwoch_do_jednego_celu_staje_na_roznych_polach(self):
        r = Ruch()
        r.przebuduj({(3, 3)})
        for i, imie in enumerate(("Ala", "Bram", "Cira", "Dan")):
            r.dodaj(imie, (i, 0))
            r.ustaw_cel(imie, (3, 3))
        _symuluj(r, 20)
        pola = [r.postacie[i].pole for i in r.postacie]
        self.assertEqual(len(set(pola)), len(pola), pola)
        self.assertTrue(all(r.dotarl(i) for i in r.postacie))

    def test_nikt_nie_stoi_na_jednym_polu_w_trakcie_marszu(self):
        r = Ruch()
        r.przebuduj({(3, 2), (3, 3), (3, 4)})
        for i, imie in enumerate(("Ala", "Bram", "Cira", "Dan", "Ewa", "Fil")):
            r.dodaj(imie, (0, i))
            r.ustaw_cel(imie, (6, 5 - i))

        def sprawdz():
            # Rezerwacja trzyma jedno pole na postać — nie mogą dzielić pola.
            rez = {}
            for p in r.postacie.values():
                for pole in (p.pole, p.nastepne):
                    if pole is not None and pole != p.pole:
                        self.assertNotIn(pole, [q.pole for q in r.postacie.values()
                                                if q.imie != p.imie], (p.imie, pole))
        _symuluj(r, 25, po_kroku=sprawdz)
        self.assertTrue(all(r.dotarl(i) for i in r.postacie))

    def test_mijaja_sie_w_dwupolowym_przejsciu(self):
        # Wszystko poza dwoma rzędami zamurowane.
        mur = {(x, y) for x in range(7) for y in range(7) if y not in (3, 4)}
        r = Ruch()
        r.przebuduj(mur)
        r.dodaj("Ala", (0, 3))
        r.dodaj("Bram", (6, 3))
        r.ustaw_cel("Ala", (6, 3))
        r.ustaw_cel("Bram", (0, 3))
        _symuluj(r, 30)
        self.assertTrue(r.dotarl("Ala"))
        self.assertTrue(r.dotarl("Bram"))

    def test_nowy_budynek_zrzuca_stojacego_z_pola(self):
        r = Ruch()
        r.dodaj("Olek", (3, 3))
        r.przebuduj({(3, 3)})
        self.assertNotEqual(r.postacie["Olek"].pole, (3, 3))
        x, y = r.pozycja("Olek")
        self.assertNotEqual((round(x), round(y)), (3, 3))

    def test_usuniety_zwalnia_pole(self):
        r = Ruch()
        r.dodaj("Ala", (2, 2))
        r.usun("Ala")
        r.dodaj("Bram", (2, 2))
        self.assertEqual(r.postacie["Bram"].pole, (2, 2))

    def test_zmiana_celu_w_marszu(self):
        r = Ruch()
        r.dodaj("Olek", (0, 0))
        r.ustaw_cel("Olek", (6, 0))
        _symuluj(r, 1.0)
        r.ustaw_cel("Olek", (0, 6))
        _symuluj(r, 12)
        self.assertTrue(r.dotarl("Olek"))
        self.assertEqual(r.postacie["Olek"].pole, (0, 6))

    def test_przebieg_jest_deterministyczny(self):
        def przebieg():
            r = Ruch()
            r.przebuduj({(3, 3), (2, 4)})
            for i, imie in enumerate(("Ala", "Bram", "Cira")):
                r.dodaj(imie, (i, 0))
                r.ustaw_cel(imie, (3, 3))
            _symuluj(r, 6)
            return [(round(r.pozycja(i)[0], 4), round(r.pozycja(i)[1], 4)) for i in sorted(r.postacie)]
        self.assertEqual(przebieg(), przebieg())

    def test_nie_wybiera_wolnego_pola_za_murem(self):
        # Wolne pole wprost przed celem leży w zamkniętej kieszeni. Gdyby miejsce
        # wybierano bez sprawdzenia drogi, postać stałaby w miejscu, a nie dochodziła.
        kieszen = {(2, 2), (3, 2), (4, 2), (2, 3), (4, 3), (2, 4), (3, 4), (4, 4)}
        r = Ruch()
        r.przebuduj(kieszen)
        r.dodaj("Olek", (0, 0))
        r.ustaw_cel("Olek", (3, 4))        # front celu to (3, 5), a „za murem” — (3, 3)
        _symuluj(r, 12)
        self.assertTrue(r.dotarl("Olek"))
        self.assertNotEqual(r.postacie["Olek"].pole, (3, 3))
        r.ustaw_cel("Olek", (3, 2))        # od frontu (3, 3) — kieszeń, więc musi obejść
        _symuluj(r, 12)
        self.assertTrue(r.dotarl("Olek"))
        px, py = r.postacie["Olek"].pole
        self.assertNotEqual((px, py), (3, 3))
        # Stoi przy nowym celu (a nie został przy poprzednim, bo droga do kieszeni nie istnieje).
        self.assertLessEqual(max(abs(px - 3), abs(py - 2)), 1, (px, py))

    def test_zastawiony_plac_nie_zbija_postaci_na_jednym_polu(self):
        # Jedno wolne pole i brama na dziewięciu ludzi: nadmiar czeka na dziedzińcu.
        zablokowane = {(x, y) for x in range(7) for y in range(7)} - {(3, 5), (3, 6)}
        zablokowane |= {(x, y) for y in range(9) for x in range(7, 9)}
        r = Ruch(9)
        r.przebuduj(zablokowane)
        for i in range(9):
            r.dodaj(f"Osada{i}", (3, 5))
            r.ustaw_cel(f"Osada{i}", (3, 5))
        _symuluj(r, 15)
        pola = [p.pole for p in r.postacie.values()]
        self.assertEqual(len(set(pola)), len(pola), pola)
        self.assertFalse(set(pola) & zablokowane)

    def test_dystans_rosnie_tylko_w_marszu(self):
        r = Ruch()
        r.dodaj("Olek", (1, 1))
        _symuluj(r, 1.0)
        self.assertEqual(r.postacie["Olek"].dystans, 0.0)
        r.ustaw_cel("Olek", (1, 4))
        _symuluj(r, 5.0)
        self.assertAlmostEqual(r.postacie["Olek"].dystans, 3.0, places=2)


if __name__ == "__main__":
    unittest.main()
