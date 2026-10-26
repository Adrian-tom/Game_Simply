"""Badania w skryptorium: epoki, bramki budowli, punkty wiedzy, zgodność grafu projektów."""
import builtins
import contextlib
import io
import random
import unittest

from game import badania, budowa, oboz, osada, swiat
from game.player import Gracz


@contextlib.contextmanager
def cicho(odpowiedzi=None):
    kolejka = list(odpowiedzi or [])
    stary = builtins.input
    builtins.input = lambda *_: kolejka.pop(0) if kolejka else ""
    try:
        with contextlib.redirect_stdout(io.StringIO()):
            yield
    finally:
        builtins.input = stary


def nowy():
    random.seed(3)
    g = Gracz("Test", "Wojownik")
    g.w_obozie = True
    return g


def ze_skryptorium(poziom=1, badacze=1):
    g = nowy()
    oboz.ustaw_poziom(g, "skryptorium", poziom)
    for o in osada.osadnicy(g)[:badacze]:
        o["zajecie"] = "badacz"
    return g


def poznaj(g, *projekty):
    badania.stan(g)["zrobione"].extend(p for p in projekty if p not in badania.stan(g)["zrobione"])


class TestGrafu(unittest.TestCase):
    def test_odwolania_wskazuja_istniejace_projekty_i_budynki(self):
        for pid, p in badania.PROJEKTY.items():
            for w in p["wymaga"]:
                self.assertIn(w, badania.PROJEKTY, pid)
            for klucz, poz in p["odblokowuje"].items():
                self.assertIn(klucz, oboz.BUDYNKI, pid)
                self.assertLessEqual(poz, oboz.BUDYNKI[klucz]["max"], (pid, klucz))
            self.assertIn(p["era"], badania.ERY)
            self.assertGreater(p["koszt"], 0)

    def test_wszystko_da_sie_poznac_bez_zakleszczenia(self):
        """Symulacja: kto może badać, bada; na końcu muszą być poznane wszystkie projekty."""
        g = nowy()
        oboz.ustaw_poziom(g, "skryptorium", 1)
        for _ in range(len(badania.PROJEKTY) + 5):
            dostepne = badania.dostepne(g)
            if not dostepne:
                break
            badania.stan(g)["zrobione"].append(dostepne[0])
            # skryptorium rośnie razem z odblokowanymi poziomami
            oboz.ustaw_poziom(g, "skryptorium", max(1, badania.maks_poziom(g, "skryptorium")))
        self.assertEqual(badania.zrobione(g), set(badania.PROJEKTY))
        self.assertEqual(badania.era(g), 3)

    def test_po_wszystkich_badaniach_wolno_postawic_kazdy_poziom_i_12_chat(self):
        g = nowy()
        poznaj(g, *badania.PROJEKTY)
        for klucz, info in oboz.BUDYNKI.items():
            self.assertEqual(badania.maks_poziom(g, klucz), info["max"], klucz)
        self.assertEqual(badania.limit_chat(g), 12)

    def test_epoka_pierwsza_nie_wymaga_zelaza(self):
        """Żelazo daje dopiero epoka II — poziomy odblokowane wcześniej nie mogą go kosztować."""
        for pid, p in badania.PROJEKTY.items():
            if p["era"] != 1:
                continue
            for klucz, poz in p["odblokowuje"].items():
                g = nowy()
                oboz.ustaw_poziom(g, klucz, poz - 1)
                self.assertNotIn("zelazo", oboz.koszt_budowy(g, klucz), (pid, klucz, poz))


class TestStartu(unittest.TestCase):
    def test_nowa_osada_umie_niewiele(self):
        g = nowy()
        self.assertEqual(badania.era(g), 1)
        self.assertEqual(badania.limit_chat(g), badania.START_CHATY)
        for klucz in ("dom", "farma", "palisada", "skryptorium"):
            self.assertEqual(badania.maks_poziom(g, klucz), 1, klucz)
        for klucz in ("tartak", "huta", "kuznia", "sklep", "targ", "laboratorium"):
            self.assertEqual(badania.maks_poziom(g, klucz), 0, klucz)

    def test_bez_skryptorium_nic_nie_mozna_badac(self):
        g = nowy()
        self.assertEqual(badania.dostepne(g), [])
        self.assertIn("skryptorium", badania.powod_blokady(g, "obrobka_drewna"))
        badania.wybierz_projekt(g, "obrobka_drewna")
        self.assertIsNone(badania.stan(g)["aktualny"])

    def test_wyzsza_epoka_wymaga_przelomu_i_skryptorium(self):
        g = ze_skryptorium()
        self.assertIn("epoki", badania.powod_blokady(g, "wytop"))
        poznaj(g, "obrobka_drewna", "kamieniarstwo", "plodozmian", "zwoje", "wiek_zelaza")
        self.assertEqual(badania.era(g), 2)
        self.assertIn("skryptorium na poziomie 2", badania.powod_blokady(g, "wytop"))
        oboz.ustaw_poziom(g, "skryptorium", 2)
        self.assertIsNone(badania.powod_blokady(g, "wytop"))

    def test_wymagania_projektu(self):
        g = ze_skryptorium()
        self.assertIn("Obróbka drewna", badania.powod_blokady(g, "handel"))
        poznaj(g, "obrobka_drewna")
        self.assertIsNone(badania.powod_blokady(g, "handel"))

    def test_nazwa_projektu_odblokowujacego_budynek(self):
        self.assertEqual(badania.projekt_dla("tartak", 1), "obrobka_drewna")
        self.assertEqual(badania.projekt_dla("farma", 2), "plodozmian")
        self.assertEqual(badania.projekt_dla("dom", 3), "wielka_architektura")


class TestPracy(unittest.TestCase):
    def test_badacz_zbiera_punkty_i_konczy_projekt(self):
        g = ze_skryptorium()
        with cicho():
            self.assertIn("Badacze", badania.wybierz_projekt(g, "obrobka_drewna"))
            pilne = swiat.minij_dni(g, 12)
        self.assertIn("obrobka_drewna", badania.zrobione(g))
        self.assertTrue(any("BADANIA UKOŃCZONE" in m for m in pilne), pilne)
        self.assertEqual(badania.maks_poziom(g, "tartak"), 1)
        self.assertIsNone(badania.stan(g)["aktualny"])

    def test_bez_badacza_punktow_nie_przybywa(self):
        g = ze_skryptorium(badacze=0)
        badania.wybierz_projekt(g, "obrobka_drewna")
        with cicho():
            swiat.minij_dni(g, 5)
        self.assertEqual(badania.postep(g, "obrobka_drewna"), 0.0)
        self.assertIsNone(badania.dni_do_konca(g))

    def test_wyzsze_skryptorium_przyspiesza(self):
        tempo = []
        for poziom in (1, 3):
            g = ze_skryptorium(poziom)
            tempo.append(badania.punkty_dzienne(g))
        self.assertGreater(tempo[1], tempo[0] * 1.5)

    def test_chory_badacz_nie_pracuje(self):
        g = ze_skryptorium()
        osada.osadnicy(g)[0]["chory"] = 4
        self.assertEqual(badania.punkty_dzienne(g), 0)

    def test_praca_bez_projektu_idzie_do_zapasu(self):
        g = ze_skryptorium()
        with cicho():
            swiat.minij_dni(g, 3)
        zapas = badania.stan(g)["zapas"]
        self.assertGreater(zapas, 0)
        self.assertLessEqual(zapas, badania.LIMIT_ZAPASU)
        with cicho():
            wiesc = badania.wybierz_projekt(g, "kamieniarstwo")
        self.assertIn("z zapasu", wiesc)
        self.assertAlmostEqual(badania.postep(g, "kamieniarstwo"), zapas)
        self.assertEqual(badania.stan(g)["zapas"], 0.0)

    def test_zapas_moze_od_razu_dokonczyc_tani_projekt(self):
        g = ze_skryptorium()
        badania.stan(g)["zapas"] = float(badania.PROJEKTY["obrobka_drewna"]["koszt"])
        with cicho():
            wiesc = badania.wybierz_projekt(g, "obrobka_drewna")
        self.assertIn("BADANIA UKOŃCZONE", wiesc)
        self.assertIn("obrobka_drewna", badania.zrobione(g))

    def test_badacz_uczy_sie_wiedzy(self):
        g = ze_skryptorium()
        o = osada.osadnicy(g)[0]
        przed = o["kompetencje"]["wiedza"]["xp"] + o["kompetencje"]["wiedza"]["p"] * 1000
        badania.wybierz_projekt(g, "obrobka_drewna")
        with cicho():
            swiat.minij_dni(g, 3)
        po = o["kompetencje"]["wiedza"]["xp"] + o["kompetencje"]["wiedza"]["p"] * 1000
        self.assertGreater(po, przed)

    def test_poznany_projekt_podnosi_wydajnosc_zawodu(self):
        g = nowy()
        rolnik = osada.osadnicy(g)[1]
        rolnik["zajecie"] = "bezczynny"
        bez = osada.mnoznik_pracy(g, rolnik, "rolnik")
        poznaj(g, "plodozmian")
        self.assertAlmostEqual(osada.mnoznik_pracy(g, rolnik, "rolnik"), bez * 1.10)

    def test_bohater_studiuje_dzien(self):
        g = ze_skryptorium(badacze=0)
        badania.wybierz_projekt(g, "obrobka_drewna")
        czas = g.czas
        with cicho():
            tekst = badania.studiuj(g)
        self.assertIn("pkt wiedzy", tekst)
        self.assertEqual(g.czas, czas + 1)
        self.assertGreaterEqual(badania.postep(g, "obrobka_drewna"), badania.punkty_studiow_bohatera(g))

    def test_studia_bez_wybranego_projektu_nie_trwonia_dnia(self):
        g = ze_skryptorium(badacze=0)
        czas = g.czas
        with cicho():
            tekst = badania.studiuj(g)
        self.assertIn("Wybierz najpierw", tekst)
        self.assertEqual(g.czas, czas)

    def test_badacz_wymaga_skryptorium(self):
        g = nowy()
        o = osada.osadnicy(g)[0]
        self.assertIn("Potrzebny budynek", osada.ustaw_zajecie(g, o, "badacz"))
        oboz.ustaw_poziom(g, "skryptorium", 1)
        self.assertIn("zostaje", osada.ustaw_zajecie(g, o, "badacz"))
        self.assertIn("Brak wolnych stanowisk", osada.ustaw_zajecie(g, osada.osadnicy(g)[1], "badacz"))


class TestMenu(unittest.TestCase):
    def test_menu_wybor_projektu_i_wyjscie(self):
        g = ze_skryptorium()
        with cicho(["1", "", "0"]):
            badania.menu_badan(g)
        self.assertIsNotNone(badania.stan(g)["aktualny"])

    def test_menu_bez_skryptorium_nie_wywala(self):
        g = nowy()
        with cicho([""]):
            badania.menu_badan(g)

    def test_etykieta_w_menu_obozu(self):
        g = nowy()
        self.assertIn("zbuduj", badania.etykieta_menu(g))
        oboz.ustaw_poziom(g, "skryptorium", 1)
        self.assertIn("wybierz", badania.etykieta_menu(g))
        badania.wybierz_projekt(g, "obrobka_drewna")
        self.assertIn("Obróbka drewna", badania.etykieta_menu(g))

    def test_stary_zapis_dostaje_wszystko_poznane(self):
        g = nowy()
        badania.migruj_stary_zapis(g)
        self.assertEqual(badania.zrobione(g), set(badania.PROJEKTY))
        self.assertIsNone(budowa.powod_blokady(g, "kuznia"))


if __name__ == "__main__":
    unittest.main()
