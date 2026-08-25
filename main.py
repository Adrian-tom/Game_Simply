"""
Główny plik gry RPG – Pro RPG (tekstowa gra fantasy po polsku).

Uruchomienie:
    python main.py
"""

from game.player import Gracz
from game.shop import otworz_sklep, otworz_kuznia
from game.skills import PODKLASY, otworz_ksiege_umiejetnosci
from game.utils import wyczysc, wyswietl_linie, nacisnij_enter, baner_tytulowy
from game.world import wyrusz_w_podroz, przegladaj_mape
from game.savegame import zapisz_gre, wczytaj_gre, zapis_istnieje, usun_zapis
from game.quests import pokaz_tablice_questow, sprawdz_questy
from game.items import otworz_ekwipunek
from game.oboz import (
    menu_rozbudowy,
    menu_stajni,
    ma_budynek,
    linia_surowcow,
    opis_obozu,
)
from game.rekruci import menu_druzyny, etykieta_towarzysza
from game.osada import menu_pracy, menu_osady
from game.atrybuty import (
    ATRYBUTY,
    KOLEJNOSC_ATRYBUTOW,
    MAX_ATRYBUT,
    wartosc,
    modyfikator,
    tekst_modyfikatora,
    podnies_atrybut,
    wyswietl_karte_postaci,
    zapewnij_atrybuty,
)
from game.pochodzenie import (
    wybierz_pochodzenie,
    wybierz_trzy_cechy,
    zastosuj_pochodzenie,
    zastosuj_ceche,
)


# ------------------------------------------------------------------ #
#  Menu główne                                                         #
# ------------------------------------------------------------------ #

def menu_glowne() -> str:
    """Wyświetla menu główne i zwraca wybór gracza."""
    baner_tytulowy()
    print("  [1]  ✨  Nowa gra")
    if zapis_istnieje():
        print("  [2]  💾  Wczytaj grę  ✔")
    else:
        print("  [2]  💾  Wczytaj grę  (brak zapisu)")
    print("  [3]  🚪  Wyjście")
    print()
    return input("  Twój wybór: ").strip()


# ------------------------------------------------------------------ #
#  Tryb trudności                                                      #
# ------------------------------------------------------------------ #

_TRYBY_TRUDNOSCI = {
    "1": {"nazwa": "Łatwy",    "opis": "Więcej złota i mikstur, wrogowie słabsi.",      "klucz": "latwy"},
    "2": {"nazwa": "Normalny", "opis": "Standardowa rozgrywka.",                         "klucz": "normalny"},
    "3": {"nazwa": "Hardcore", "opis": "Permadeath — śmierć usuwa zapis gry. Trudniejsi wrogowie.", "klucz": "hardcore"},
}


def _wybierz_trudnosc() -> str:
    """Prowadzi gracza przez wybór trybu trudności. Zwraca klucz trybu."""
    wyswietl_linie("─")
    print("  Wybierz tryb trudności:\n")
    for k, v in _TRYBY_TRUDNOSCI.items():
        print(f"  [{k}]  {v['nazwa']} — {v['opis']}")
    print()
    while True:
        wybor = input("  Twój wybór: ").strip()
        if wybor in _TRYBY_TRUDNOSCI:
            return _TRYBY_TRUDNOSCI[wybor]["klucz"]
        print("  Nieprawidłowy wybór. Wpisz 1, 2 lub 3.")


def _zastosuj_trudnosc(gracz: Gracz, tryb: str) -> None:
    """Modyfikuje statystyki gracza wg wybranego trybu trudności."""
    gracz.tryb_trudnosci = tryb
    if tryb == "latwy":
        gracz.zloto += 20
        gracz.mikstury += 2
        gracz.hp = min(gracz.max_hp, gracz.hp + 20)
        mag = getattr(gracz, "surowce", None)
        if mag is not None:
            mag["drewno"] = mag.get("drewno", 0) + 4
            mag["kamien"] = mag.get("kamien", 0) + 3
            mag["ziola"] = mag.get("ziola", 0) + 2
    elif tryb == "hardcore":
        gracz.max_hp = max(1, int(gracz.max_hp * 0.85))
        gracz.hp = gracz.max_hp


# ------------------------------------------------------------------ #
#  Tworzenie postaci                                                   #
# ------------------------------------------------------------------ #

def _wybierz_klase() -> str:
    """Prowadzi gracza przez wybór klasy postaci. Zwraca nazwę klasy."""
    wyswietl_linie("─")
    print("  Wybierz klasę postaci:\n")
    print("  [1]  ⚔  Wojownik    — Wysoki HP i obrona, walka wręcz")
    print("  [2]  🔮 Mag         — Niski HP, za to potężne zaklęcia (mana)")
    print("  [3]  🗡  Łotrzyk     — Zwinny oszust z kontrolą i trafieniami krytycznymi")
    print("  [4]  🌿 Druid       — Uzdrowiciel natury, regeneracja i żywiołowe zaklęcia (mana)")
    print("  [5]  💀 Nekromanta  — Mistrz mroku, wysysa życie i osłabia wrogów (mana)")
    print()
    while True:
        wybor = input("  Twój wybór: ").strip()
        if wybor == "1":
            return "Wojownik"
        if wybor == "2":
            return "Mag"
        if wybor == "3":
            return "Lotrzyk"
        if wybor == "4":
            return "Druid"
        if wybor == "5":
            return "Nekromanta"
        print("  Nieprawidłowy wybór. Wpisz 1, 2, 3, 4 lub 5.")


def stworz_postac() -> Gracz:
    """Prowadzi gracza przez tworzenie nowej postaci."""
    wyczysc()
    wyswietl_linie("═")
    print("  TWORZENIE POSTACI")
    wyswietl_linie("═")
    while True:
        imie = input("\n  Podaj imię swojego bohatera: ").strip()
        if imie:
            break
        print("  Imię nie może być puste. Spróbuj ponownie.")

    print()
    klasa = _wybierz_klase()
    print()
    pochodzenie = wybierz_pochodzenie()
    cechy = wybierz_trzy_cechy()
    print()
    tryb = _wybierz_trudnosc()
    gracz = Gracz(imie, klasa)
    zastosuj_pochodzenie(gracz, pochodzenie)
    for klucz in cechy:
        zastosuj_ceche(gracz, klucz)
    _zastosuj_trudnosc(gracz, tryb)
    print(f"\n  Witaj, {gracz.imie} [{klasa}]! Tryb: {tryb.capitalize()}. Twoja przygoda się rozpoczyna...")
    print(gracz)
    print("  Masz punkty atrybutów — rozdaj je w obozie na karcie postaci [7].")
    nacisnij_enter()
    return gracz


# ------------------------------------------------------------------ #
#  Wybór podklasy                                                      #
# ------------------------------------------------------------------ #

def _wybierz_podklase_dialog(gracz: Gracz) -> None:
    """Wyświetla menu wyboru podklasy i zapisuje wybór w obiekcie gracza."""
    wyczysc()
    wyswietl_linie("═")
    print(f"  WYBÓR PODKLASY  —  {gracz.klasa}")
    wyswietl_linie("═")
    print(f"\n  Osiągnąłeś poziom 5! Czas wybrać swoją specjalizację.\n")

    podklasy = PODKLASY[gracz.klasa]
    for i, pk in enumerate(podklasy, 1):
        print(f"  [{i}] {pk['nazwa']}")
        print(f"       {pk['opis']}")
        print()

    while True:
        wybor = input("  Twój wybór: ").strip()
        try:
            idx = int(wybor) - 1
            if 0 <= idx < len(podklasy):
                wybrana = podklasy[idx]
                komunikaty = gracz.wybierz_podklase(wybrana["klucz"])
                print()
                for msg in komunikaty:
                    print(msg)
                nacisnij_enter()
                return
        except ValueError:
            pass
        print("  Nieprawidłowy wybór.")


# ------------------------------------------------------------------ #
#  Rozdzielanie atrybutów                                              #
# ------------------------------------------------------------------ #

def _rozdziel_atrybuty(gracz: Gracz) -> None:
    """Karta postaci: podgląd atrybutów i rozdział punktów (Siła, Zręczność…)."""
    zapewnij_atrybuty(gracz)
    while True:
        wyczysc()
        wyswietl_linie("═")
        print(f"  KARTA POSTACI  —  {gracz.imie}")
        wyswietl_linie("═")
        print(f"  Punkty do rozdania: {gracz.punkty_atrybutow}   (maks. atrybutu: {MAX_ATRYBUT})")
        print(f"  HP {gracz.max_hp}  Atak {gracz.atak}  Obrona {gracz.obrona}")
        if gracz.max_mana > 0:
            print(f"  Mana {gracz.max_mana}")
        print()
        for i, klucz in enumerate(KOLEJNOSC_ATRYBUTOW, 1):
            info = ATRYBUTY[klucz]
            wart = wartosc(gracz, klucz)
            mod = tekst_modyfikatora(modyfikator(gracz, klucz))
            print(
                f"  [{i}]  {info['ikona']} {info['nazwa']:14} {wart:2} ({mod:>3})  — {info['opis']}"
            )
        print()
        print("  Siła → atak, Zręczność → obrona/uniki/krytyki, Kondycja → HP.")
        print("  Testy na mapie: wspinaczka, zamki, perswazja, zastraszanie…")
        print("  [K]  Pełna karta (biegłości i premie do testów)")
        print("  [0]  Wróć")
        print()
        wybor = input("  Twój wybór: ").strip().lower()
        if wybor == "0":
            return
        if wybor == "k":
            wyswietl_karte_postaci(gracz)
            continue
        if gracz.punkty_atrybutow <= 0:
            print("  Nie masz punktów atrybutów. Zdobywasz je przy awansie.")
            nacisnij_enter()
            continue
        try:
            idx = int(wybor) - 1
            if 0 <= idx < len(KOLEJNOSC_ATRYBUTOW):
                print(podnies_atrybut(gracz, KOLEJNOSC_ATRYBUTOW[idx]))
                nacisnij_enter()
                continue
        except ValueError:
            pass
        print("  Nieprawidłowy wybór.")
        nacisnij_enter()


# ------------------------------------------------------------------ #
#  Osiągnięcia                                                         #
# ------------------------------------------------------------------ #

def _sprawdz_i_wyswietl_osiagniecia(gracz: Gracz) -> None:
    """Sprawdza osiągnięcia i wyświetla nowe."""
    nowe = gracz.sprawdz_osiagniecia()
    for msg in nowe:
        print(msg)
    if nowe:
        nacisnij_enter()


def _menu_osiagniec(gracz: Gracz) -> None:
    """Wyświetla listę osiągnięć gracza."""
    wyczysc()
    wyswietl_linie("═")
    print("  OSIĄGNIĘCIA")
    wyswietl_linie("═")
    if not gracz.osiagniecia:
        print("\n  Brak odblokowanych osiągnięć. Eksploruj świat!")
    else:
        print(f"\n  Odblokowane: {len(gracz.osiagniecia)}/{len(gracz._OSIAGNIECIA)}\n")
        for klucz, nazwa, _ in gracz._OSIAGNIECIA:
            status = "✔" if klucz in gracz.osiagniecia else "·"
            print(f"  [{status}]  {nazwa}")
    print()
    nacisnij_enter()


# ------------------------------------------------------------------ #
#  Statystyki końca gry                                                #
# ------------------------------------------------------------------ #

def _pokaz_statystyki_konca(gracz: Gracz) -> None:
    """Wyświetla podsumowanie rozgrywki po śmierci lub ukończeniu."""
    wyczysc()
    wyswietl_linie("═")
    print("  PODSUMOWANIE PRZYGODY")
    wyswietl_linie("═")
    print(f"\n  Bohater: {gracz.imie} [{gracz.klasa}]")
    print(f"  Osiągnięty poziom: {gracz.poziom}")
    print(f"  Zdobyte EXP: {gracz.exp}")
    print(f"  Odwiedzone mapy: {gracz.mapa_gen}")
    print(f"  Złoto przy śmierci: {gracz.zloto} szt.")
    print(f"  Karma: {getattr(gracz, 'karma', 0)}")
    print(f"  Zabite potwory: {gracz.statystyki.get('zabite_potwory', 0)}")
    print(f"  Wygrane walki: {gracz.statystyki.get('wygrane_walki', 0)}")
    print(f"  Odwiedzone świątynie: {gracz.statystyki.get('odwiedzone_swiatynie', 0)}")
    if gracz.osiagniecia:
        print(f"\n  Odblokowane osiągnięcia ({len(gracz.osiagniecia)}):")
        for klucz, nazwa, _ in gracz._OSIAGNIECIA:
            if klucz in gracz.osiagniecia:
                print(f"  {nazwa}")
    wyswietl_linie()
    nacisnij_enter()


# ------------------------------------------------------------------ #
#  Menu akcji w obozie                                                 #
# ------------------------------------------------------------------ #

def menu_obozu(gracz: Gracz) -> str:
    """Wyświetla menu obozu i zwraca wybór gracza."""
    wyczysc()
    wyswietl_linie("═")
    tryb_str = f"  [{getattr(gracz, 'tryb_trudnosci', 'normalny').upper()}]"
    print(f"  OBÓZ  —  {gracz.imie}  (Poz. {gracz.poziom}){tryb_str}")
    wyswietl_linie("═")
    print(gracz)
    print(f"  Obóz: {opis_obozu(gracz)}")
    print(f"  {linia_surowcow(gracz)}")
    towar = etykieta_towarzysza(gracz)
    if towar:
        print(f"  Towarzysz walki: {towar}")
    if getattr(gracz, "punkty_atrybutow", 0) > 0:
        print(f"\n  ⭐  Masz {gracz.punkty_atrybutow} punkt(y) atrybutów — karta postaci [7].")
    pkt_u = getattr(gracz, "punkty_umiejetnosci", 0)
    if pkt_u > 0:
        print(f"  ⭐  Masz {pkt_u} punkt(y) umiejętności — otwórz księgę [10].")
    print()
    print("  [1]  🗺  Wyrusz na przygodę")
    if ma_budynek(gracz, "sklep"):
        print("  [2]  🏪  Sklep")
    else:
        print("  [2]  🏪  Sklep  (zbuduj w [11])")
    if ma_budynek(gracz, "dom"):
        print("  [3]  😴  Odpoczynek w domu (+80 HP, 5 złota)")
    else:
        print("  [3]  🔥  Odpoczynek przy palenisku (+30 HP, 10 złota)")
    print("  [4]  🎒  Ekwipunek")
    print("  [5]  📜  Tablica questów")
    print("  [6]  🏆  Osiągnięcia")
    print("  [9]  🗺  Mapa okolicy")
    print("  [10] 📖  Księga umiejętności" + ("  ⭐" if pkt_u > 0 else ""))
    print("  [11] 🏗  Rozbudowa obozu")
    if ma_budynek(gracz, "kuznia"):
        print("  [12] ⚒  Kuźnia")
    if ma_budynek(gracz, "stajnie"):
        print("  [13] 🐴  Stajnie (szybka podróż)")
    print("  [14] 🤝  Drużyna (rekruci)")
    print("  [15] 🪓  Praca w obozie")
    print("  [16] 🛖  Osada (chaty, osadnicy, warsztat)")
    gwiazdka_atr = "  ⭐" if getattr(gracz, "punkty_atrybutow", 0) > 0 else ""
    print(f"  [7]  📋  Karta postaci (atrybuty, testy){gwiazdka_atr}")
    if gracz.podklasa_dostepna:
        print("  [8]  ⭐  Wybierz podklasę! (Dostępna)")
    print("  [0]  💾  Wróć do menu głównego")
    print()
    return input("  Twój wybór: ").strip()


def odpoczynek(gracz: Gracz) -> None:
    """Gracz odpoczywa. Dom w obozie leczy mocniej i taniej."""
    w_domu = ma_budynek(gracz, "dom")
    koszt = 5 if w_domu else 10
    lecz = 80 if w_domu else 30
    if gracz.zloto < koszt:
        print(f"\n  Nie masz wystarczająco złota (potrzebujesz {koszt} szt.).")
    else:
        poprzednie = gracz.hp
        gracz.zloto -= koszt
        gracz.hp = min(gracz.hp + lecz, gracz.max_hp)
        faktyczne = gracz.hp - poprzednie
        miejsce = "w domu" if w_domu else "przy palenisku"
        print(f"\n  😴  Odpocząłeś {miejsce} i odzyskałeś {faktyczne} HP. (-{koszt} złota)")
        if gracz.max_mana > 0:
            gracz.mana = gracz.max_mana
            print(f"  🔮  Mana uzupełniona do {gracz.max_mana}!")
    nacisnij_enter()


# ------------------------------------------------------------------ #
#  Pętla nowej gry                                                     #
# ------------------------------------------------------------------ #

def nowa_gra(gracz: Gracz | None = None) -> None:
    """Główna pętla rozgrywki."""
    if gracz is None:
        gracz = stworz_postac()

    while True:
        # Sprawdź osiągnięcia i questy po każdym powrocie do obozu
        _sprawdz_i_wyswietl_osiagniecia(gracz)
        nowe_questy = sprawdz_questy(gracz)
        for msg in nowe_questy:
            print(msg)
        if nowe_questy:
            nacisnij_enter()

        wybor = menu_obozu(gracz)

        if wybor == "1":
            wynik = wyrusz_w_podroz(gracz)
            # Autosave po każdej wyprawie
            zapisz_gre(gracz)
            if wynik == "przegrana":
                wyczysc()
                wyswietl_linie("═")
                print("  KONIEC GRY")
                wyswietl_linie("═")
                print(f"\n  {gracz.imie} poległ w walce...")
                wyswietl_linie()
                _pokaz_statystyki_konca(gracz)
                # Hardcore: usuń zapis po śmierci
                if getattr(gracz, "tryb_trudnosci", "normalny") == "hardcore":
                    usun_zapis()
                    print("  ☠  HARDCORE — zapis usunięty.\n")
                    nacisnij_enter()
                return

        elif wybor == "2":
            if ma_budynek(gracz, "sklep"):
                otworz_sklep(gracz)
            else:
                print("\n  Nie masz jeszcze sklepu w obozie.")
                print("  Zbierz surowce na mapie ([6]) i wznieś sklep w [11] Rozbudowa.")
                print("  Kupca spotkasz też w karczmie w terenie.")
                nacisnij_enter()

        elif wybor == "3":
            odpoczynek(gracz)

        elif wybor == "4":
            otworz_ekwipunek(gracz)

        elif wybor == "5":
            wyczysc()
            pokaz_tablice_questow(gracz)

        elif wybor == "6":
            _menu_osiagniec(gracz)

        elif wybor == "9":
            przegladaj_mape(gracz)

        elif wybor == "10":
            otworz_ksiege_umiejetnosci(gracz)
            zapisz_gre(gracz)

        elif wybor == "11":
            menu_rozbudowy(gracz)
            zapisz_gre(gracz)

        elif wybor == "12" and ma_budynek(gracz, "kuznia"):
            otworz_kuznia(gracz)

        elif wybor == "13" and ma_budynek(gracz, "stajnie"):
            menu_stajni(gracz)
            zapisz_gre(gracz)

        elif wybor == "14":
            menu_druzyny(gracz)
            zapisz_gre(gracz)

        elif wybor == "15":
            menu_pracy(gracz)
            zapisz_gre(gracz)

        elif wybor == "16":
            menu_osady(gracz)
            zapisz_gre(gracz)

        elif wybor == "7":
            _rozdziel_atrybuty(gracz)
            zapisz_gre(gracz)

        elif wybor == "8" and gracz.podklasa_dostepna:
            _wybierz_podklase_dialog(gracz)

        elif wybor == "0":
            zapisz_gre(gracz)
            print("\n  Gra zapisana. Wracasz do menu głównego...")
            nacisnij_enter()
            return

        else:
            print("  Nieprawidłowy wybór.")
            nacisnij_enter()


# ------------------------------------------------------------------ #
#  Punkt wejścia                                                       #
# ------------------------------------------------------------------ #

def main() -> None:
    """Główna pętla programu z menu startowym."""
    while True:
        wybor = menu_glowne()

        if wybor == "1":
            nowa_gra()

        elif wybor == "2":
            gracz = wczytaj_gre()
            if gracz is None:
                wyczysc()
                print("\n  Brak zapisu gry lub zapis jest uszkodzony.")
                print("  Wybierz 'Nowa gra', aby rozpocząć przygodę!")
                nacisnij_enter()
            else:
                wyczysc()
                wyswietl_linie("═")
                print("  WCZYTANO GRĘ")
                wyswietl_linie("═")
                print(gracz)
                nacisnij_enter()
                nowa_gra(gracz)

        elif wybor == "3":
            wyczysc()
            print("\n  Dziękujemy za grę! Do zobaczenia w następnej przygodzie!\n")
            break

        else:
            print("  Nieprawidłowy wybór. Wpisz 1, 2 lub 3.")
            nacisnij_enter()


if __name__ == "__main__":
    main()

