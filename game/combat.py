"""Moduł obsługujący system walki turowej."""

import random

from game.player import Gracz
from game.enemy import Przeciwnik, losuj_przeciwnika
from game.skills import UMIEJETNOSCI
from game.utils import wyczysc, nacisnij_enter, wyswietl_linie


# ------------------------------------------------------------------ #
#  Obliczenia obrażeń                                                  #
# ------------------------------------------------------------------ #

def _oblicz_obrazenia(atak: int, obrona: int) -> int:
    """Oblicza zadane obrażenia z losową wariancją ±20%."""
    bazowe = max(1, atak - obrona)
    wariancja = max(1, int(bazowe * 0.2))
    return random.randint(max(1, bazowe - wariancja), bazowe + wariancja)


# ------------------------------------------------------------------ #
#  Stan walki (buffy, debuffs, efekty)                                #
# ------------------------------------------------------------------ #

def _nowy_stan_walki() -> dict:
    """Zwraca zainicjalizowany słownik stanu walki."""
    return {
        "tura": 1,
        # Buffs gracza
        "buff_atak_mnoznik": 1.0,     # mnożnik ataku (>1 = buff, <1 = debuff)
        "buff_atak_tury": 0,          # ile tur buff ataku trwa
        "buff_obrona_mnoznik": 1.0,   # mnożnik obrony na turę wroga
        "buff_obrona_tury": 0,        # ile tur buff obrony trwa
        "brak_obrony_tura": False,    # Potężny cios: gracz traci obronę przy odwecie
        "leczenie_zablokowane": False,  # Szał berserka: brak leczenia
        "tarcza_runowa": 0,           # punkty absorpcji tarczy runowej
        "unik_aktywny": False,        # Unik: szansa na ominięcie ataku
        "przyspieszenie": False,      # Arcymag: następny czar darmowy i 2× silniejszy
        # Debuffs wroga
        "wrog_ogluszone_tury": 0,
        "wrog_trucizna_tury": 0,
        "wrog_trucizna_obrazenia": 10,
        "wrog_oslabienie_tury": 0,    # Klątwa śmierci: 50% mniej obrażeń
    }


def _koniec_rundy(stan: dict) -> None:
    """Dekrementuje tury aktywnych buffów ataku gracza po każdej pełnej rundzie."""
    if stan["buff_atak_tury"] > 0:
        stan["buff_atak_tury"] -= 1
        if stan["buff_atak_tury"] == 0:
            stan["buff_atak_mnoznik"] = 1.0
            if stan["leczenie_zablokowane"]:
                stan["leczenie_zablokowane"] = False
                print("  Szał berserka minął. Możesz znów się leczyć.")
    stan["tura"] += 1


# ------------------------------------------------------------------ #
#  Wyświetlanie stanu walki                                           #
# ------------------------------------------------------------------ #

def _wyswietl_stan_walki(gracz: Gracz, przeciwnik: Przeciwnik, stan: dict) -> None:
    """Wyświetla aktualny stan walki wraz z aktywnymi efektami."""
    wyswietl_linie()
    print(f"  {przeciwnik}")
    mana_str = ""
    if gracz.max_mana > 0:
        mana_str = f"   Mana: {gracz.mana}/{gracz.max_mana} {gracz.pasek_many()}"
    print(f"  {gracz.imie}  HP: {gracz.hp}/{gracz.max_hp} {gracz.pasek_hp()}{mana_str}")

    efekty: list[str] = []
    if stan["buff_atak_tury"] > 0:
        efekty.append(f"Atak ×{stan['buff_atak_mnoznik']:.1f} ({stan['buff_atak_tury']} tur)")
    if stan["buff_obrona_tury"] > 0:
        efekty.append(f"Obrona ×{stan['buff_obrona_mnoznik']:.1f} ({stan['buff_obrona_tury']} tur)")
    if stan["tarcza_runowa"] > 0:
        efekty.append(f"Tarcza runowa ({stan['tarcza_runowa']} HP)")
    if stan["leczenie_zablokowane"]:
        efekty.append("Leczenie zablokowane")
    if stan["wrog_trucizna_tury"] > 0:
        efekty.append(f"{przeciwnik.nazwa} zatruty ({stan['wrog_trucizna_tury']} tur)")
    if stan["wrog_ogluszone_tury"] > 0:
        efekty.append(f"{przeciwnik.nazwa} ogłuszony ({stan['wrog_ogluszone_tury']} tur)")
    if stan["wrog_oslabienie_tury"] > 0:
        efekty.append(f"{przeciwnik.nazwa} osłabiony ({stan['wrog_oslabienie_tury']} tur)")
    if efekty:
        print(f"  Efekty: {', '.join(efekty)}")

    wyswietl_linie()


# ------------------------------------------------------------------ #
#  Submenu umiejętności                                               #
# ------------------------------------------------------------------ #

def _menu_umiejetnosci(gracz: Gracz) -> str | None:
    """
    Wyświetla submenu umiejętności. Zwraca klucz wybranego skilla lub None
    (gdy gracz wraca do głównego menu walki).
    """
    while True:
        print("\n  === UMIEJĘTNOŚCI ===")
        for i, klucz in enumerate(gracz.umiejetnosci, 1):
            info = UMIEJETNOSCI[klucz]
            koszt_str = f"  [{info['koszt_many']} many]" if info["koszt_many"] > 0 else ""
            brak_str = "  ✗" if gracz.mana < info["koszt_many"] else ""
            print(
                f"  [{i}] {info['ikona']} {info['nazwa']}{koszt_str}"
                f"  — {info['opis']}{brak_str}"
            )
        print("  [0] Wróć\n")

        wybor = input("  Wybierz umiejętność: ").strip()
        if wybor == "0":
            return None
        try:
            idx = int(wybor) - 1
            if 0 <= idx < len(gracz.umiejetnosci):
                klucz = gracz.umiejetnosci[idx]
                info = UMIEJETNOSCI[klucz]
                if gracz.mana < info["koszt_many"]:
                    print(
                        f"  Niewystarczająca mana!"
                        f" (Masz {gracz.mana}, potrzebujesz {info['koszt_many']})"
                    )
                    continue
                return klucz
        except ValueError:
            pass
        print("  Nieprawidłowy wybór.")


# ------------------------------------------------------------------ #
#  Obsługa umiejętności                                               #
# ------------------------------------------------------------------ #

def _uzyj_umiejetnosci(
    klucz: str, gracz: Gracz, przeciwnik: Przeciwnik, stan: dict
) -> str | None:
    """
    Wykonuje wybraną umiejętność. Zwraca 'wygrana', 'ucieczka' lub None.
    """
    info = UMIEJETNOSCI[klucz]
    koszt = info["koszt_many"]

    # Arcymag: przyspieszenie — następny czar darmowy i 2× silniejszy
    dmg_wzmocnienie = 1.0
    if stan["przyspieszenie"] and koszt > 0:
        dmg_wzmocnienie = 2.0
        koszt = 0
        stan["przyspieszenie"] = False
        print(f"\n  ⚡ Przyspieszenie magiczne! Obrażenia ×2, mana darmowa!")

    gracz.mana -= koszt
    print(f"\n  {info['ikona']}  Używasz: {info['nazwa']}!")

    efektywny_atak = max(1, int(gracz.atak * stan["buff_atak_mnoznik"]))

    # ---- WOJOWNIK – klasa główna ----

    if klucz == "potezny_cios":
        obrazenia = _oblicz_obrazenia(efektywny_atak, przeciwnik.obrona) * 2
        przeciwnik.hp -= obrazenia
        stan["brak_obrony_tura"] = True
        print(f"  Zadajesz {obrazenia} obrażeń! (Tracisz obronę przy odwecie)")
        if not przeciwnik.zyje():
            return "wygrana"

    elif klucz == "tarcza_wiary":
        stan["buff_obrona_mnoznik"] = 2.0
        stan["buff_obrona_tury"] = 1
        print("  Twoja obrona jest podwojona do końca tury wroga!")

    elif klucz == "okrzyk_bojowy":
        stan["buff_atak_mnoznik"] = 1.3
        stan["buff_atak_tury"] = 2
        print("  Okrzyk bojowy! Atak +30% przez 2 tury!")

    elif klucz == "szal_berserka":
        stan["buff_atak_mnoznik"] = 1.5
        stan["buff_atak_tury"] = 3
        stan["leczenie_zablokowane"] = True
        print("  Szał berserka! Atak +50% przez 3 tury — leczenie zablokowane!")

    # ---- WOJOWNIK – Paladyn ----

    elif klucz == "boskie_swiatlo":
        wyleczone = min(50, gracz.max_hp - gracz.hp)
        gracz.hp += wyleczone
        print(f"  Boskie światło! Przywróciłeś {wyleczone} HP!")

    elif klucz == "swiety_cios":
        bazowe = int(_oblicz_obrazenia(efektywny_atak, przeciwnik.obrona) * 2.5)
        obrazenia = bazowe + 20
        przeciwnik.hp -= obrazenia
        print(f"  Zadajesz {obrazenia} obrażeń ({bazowe} fizycznych + 20 świętych)!")
        if not przeciwnik.zyje():
            return "wygrana"

    # ---- WOJOWNIK – Barbarzyńca ----

    elif klucz == "wscieklosc":
        stan["buff_atak_mnoznik"] = 1.8
        stan["buff_atak_tury"] = 4
        stan["buff_obrona_mnoznik"] = 0.5
        stan["buff_obrona_tury"] = 4
        print("  Wściekłość! Atak +80% przez 4 tury — obrona -50%!")

    elif klucz == "niszczace_uderzenie":
        obrazenia = max(1, int(przeciwnik.hp * 0.30))
        przeciwnik.hp -= obrazenia
        print(f"  Niszczące uderzenie! Zadajesz {obrazenia} obrażeń (30% HP wroga)!")
        if not przeciwnik.zyje():
            return "wygrana"

    # ---- MAG – klasa główna ----

    elif klucz == "kula_ognia":
        obrazenia = int(random.randint(35, 55) * dmg_wzmocnienie)
        przeciwnik.hp -= obrazenia
        print(f"  Kula ognia trafia za {obrazenia} obrażeń magicznych!")
        if not przeciwnik.zyje():
            return "wygrana"

    elif klucz == "lodowe_wiezy":
        stan["wrog_ogluszone_tury"] = 1
        print(f"  {przeciwnik.nazwa} jest zamrożony i pomija następną turę!")

    elif klucz == "tarcza_runowa":
        stan["tarcza_runowa"] = 40
        print("  Tarcza runowa aktywna! Absorbuje do 40 obrażeń.")

    elif klucz == "meteor":
        obrazenia = int(random.randint(80, 120) * dmg_wzmocnienie)
        przeciwnik.hp -= obrazenia
        print(f"  Meteor uderza za {obrazenia} obrażeń magicznych!")
        if not przeciwnik.zyje():
            return "wygrana"

    # ---- MAG – Nekromanta ----

    elif klucz == "wysysanie_zycia":
        obrazenia = int(random.randint(30, 50) * dmg_wzmocnienie)
        przeciwnik.hp -= obrazenia
        wyleczone = min(obrazenia, gracz.max_hp - gracz.hp)
        gracz.hp += wyleczone
        print(f"  Wysysasz {obrazenia} HP od {przeciwnik.nazwa}!")
        print(f"  Leczysz się o {wyleczone} HP!")
        if not przeciwnik.zyje():
            return "wygrana"

    elif klucz == "klatwa_smierci":
        stan["wrog_oslabienie_tury"] = 3
        print(f"  Klątwa śmierci! {przeciwnik.nazwa} zadaje 50% mniej obrażeń przez 3 tury.")

    # ---- MAG – Arcymag ----

    elif klucz == "przyspieszenie_magiczne":
        stan["przyspieszenie"] = True
        print("  Przyspieszenie magiczne! Następny czar będzie darmowy i ×2 silniejszy.")

    elif klucz == "kula_pioruna":
        obrazenia = int(random.randint(100, 150) * dmg_wzmocnienie)
        przeciwnik.hp -= obrazenia
        print(f"  Kula pioruna uderza za {obrazenia} obrażeń magicznych!")
        if not przeciwnik.zyje():
            return "wygrana"

    # ---- ŁOTRZYK – klasa główna ----

    elif klucz == "cios_w_plecy":
        bazowe = _oblicz_obrazenia(efektywny_atak, przeciwnik.obrona)
        aktywuje = stan["tura"] == 1 or random.random() < 0.4
        if aktywuje:
            obrazenia = bazowe * 2
            print(f"  Cios w plecy! Zadajesz {obrazenia} obrażeń (podwójne)!")
        else:
            obrazenia = bazowe
            print(f"  Zadajesz {obrazenia} obrażeń.")
        przeciwnik.hp -= obrazenia
        if not przeciwnik.zyje():
            return "wygrana"

    elif klucz == "trucizna":
        stan["wrog_trucizna_tury"] = 3
        stan["wrog_trucizna_obrazenia"] = 10
        print(f"  {przeciwnik.nazwa} jest zatruty! Traci 10 HP na turę przez 3 tury.")

    elif klucz == "dymna_bomba":
        print("  Rzucasz bombę dymną! Znikasz w chmurze dymu...")
        return "ucieczka"

    elif klucz == "smiertelne_uderzenie":
        if przeciwnik.hp < przeciwnik.max_hp * 0.25:
            obrazenia = _oblicz_obrazenia(efektywny_atak, przeciwnik.obrona) * 3
            print(f"  Śmiertelne uderzenie! Zadajesz {obrazenia} obrażeń!")
        else:
            obrazenia = _oblicz_obrazenia(efektywny_atak, przeciwnik.obrona)
            print(f"  Zadajesz {obrazenia} obrażeń (wróg zbyt silny na egzekucję).")
        przeciwnik.hp -= obrazenia
        if not przeciwnik.zyje():
            return "wygrana"

    # ---- ŁOTRZYK – Zabójca ----

    elif klucz == "cien_smierci":
        bazowe = _oblicz_obrazenia(efektywny_atak, przeciwnik.obrona)
        if random.random() < 0.6:
            obrazenia = bazowe * 4
            print(f"  KRYTYCZNE TRAFIENIE! Cień śmierci zadaje {obrazenia} obrażeń!")
        else:
            obrazenia = bazowe
            print(f"  Cios chybił — zadajesz {obrazenia} obrażeń.")
        przeciwnik.hp -= obrazenia
        if not przeciwnik.zyje():
            return "wygrana"

    elif klucz == "egzekucja":
        if przeciwnik.hp < przeciwnik.max_hp * 0.15:
            print(f"  Egzekucja! Kończysz {przeciwnik.nazwa} jednym ciosem!")
            przeciwnik.hp = 0
            return "wygrana"
        obrazenia = _oblicz_obrazenia(efektywny_atak, przeciwnik.obrona)
        przeciwnik.hp -= obrazenia
        print(f"  Zadajesz {obrazenia} obrażeń (wróg zbyt silny na egzekucję).")
        if not przeciwnik.zyje():
            return "wygrana"

    # ---- ŁOTRZYK – Zwiadowca ----

    elif klucz == "unik":
        stan["unik_aktywny"] = True
        print("  Przygotowujesz się do uniku! (75% szans na ominięcie ataku wroga)")

    elif klucz == "grad_strzal":
        total = 0
        trafienia = 0
        for _ in range(3):
            if not przeciwnik.zyje():
                break
            dam = _oblicz_obrazenia(efektywny_atak, przeciwnik.obrona)
            przeciwnik.hp = max(0, przeciwnik.hp - dam)
            total += dam
            trafienia += 1
        print(f"  Grad strzał: {trafienia} trafień za łącznie {total} obrażeń!")
        if not przeciwnik.zyje():
            return "wygrana"

    return None


# ------------------------------------------------------------------ #
#  Tura gracza                                                        #
# ------------------------------------------------------------------ #

def _tura_gracza(gracz: Gracz, przeciwnik: Przeciwnik, stan: dict) -> str | None:
    """
    Obsługuje turę gracza. Zwraca 'wygrana', 'ucieczka' lub None
    (walka trwa dalej).
    """
    while True:
        print(f"\n  Co robisz?")
        print(f"  [1] Atakuj")
        if stan["leczenie_zablokowane"]:
            print(f"  [2] Użyj mikstury — ZABLOKOWANE (szał berserka)")
        else:
            print(f"  [2] Użyj mikstury ({gracz.mikstury} szt.)")
        print(f"  [3] Umiejętności")
        print(f"  [4] Uciekaj")

        wybor = input("\n  Twój wybór: ").strip()

        if wybor == "1":
            efektywny_atak = max(1, int(gracz.atak * stan["buff_atak_mnoznik"]))
            obrazenia = _oblicz_obrazenia(efektywny_atak, przeciwnik.obrona)
            przeciwnik.hp -= obrazenia
            print(f"\n  ⚔  Atakujesz {przeciwnik.nazwa}! Zadajesz {obrazenia} obrażeń.")
            if not przeciwnik.zyje():
                return "wygrana"
            return None

        elif wybor == "2":
            if stan["leczenie_zablokowane"]:
                print("  Nie możesz się leczyć podczas szału berserka!")
                continue
            print(f"\n  🧪  {gracz.uzyj_miksture()}")
            return None

        elif wybor == "3":
            klucz = _menu_umiejetnosci(gracz)
            if klucz is None:
                continue  # gracz wrócił — pokaż menu ponownie
            return _uzyj_umiejetnosci(klucz, gracz, przeciwnik, stan)

        elif wybor == "4":
            szansa = random.random()
            if szansa < 0.5:
                print("\n  🏃  Udało ci się uciec!")
                return "ucieczka"
            print("\n  🏃  Nie udało się uciec — przeciwnik blokuje drogę!")
            return None

        else:
            print("  Nieprawidłowy wybór. Wpisz 1, 2, 3 lub 4.")


# ------------------------------------------------------------------ #
#  Tura przeciwnika                                                   #
# ------------------------------------------------------------------ #

def _tura_przeciwnika(gracz: Gracz, przeciwnik: Przeciwnik, stan: dict) -> None:
    """Obsługuje turę przeciwnika z uwzględnieniem aktywnych efektów."""

    # Trucizna na wroga
    if stan["wrog_trucizna_tury"] > 0:
        dam = stan["wrog_trucizna_obrazenia"]
        przeciwnik.hp = max(0, przeciwnik.hp - dam)
        stan["wrog_trucizna_tury"] -= 1
        print(
            f"  ☠  Trucizna! {przeciwnik.nazwa} traci {dam} HP."
            f" (Pozostało tur: {stan['wrog_trucizna_tury']})"
        )
        if not przeciwnik.zyje():
            return

    # Ogłuszenie
    if stan["wrog_ogluszone_tury"] > 0:
        stan["wrog_ogluszone_tury"] -= 1
        print(f"  ❄  {przeciwnik.nazwa} jest ogłuszony i pomija turę!")
        return

    # Efektywna obrona gracza
    if stan["brak_obrony_tura"]:
        efektywna_obrona = 0
        stan["brak_obrony_tura"] = False
    elif stan["buff_obrona_tury"] > 0:
        efektywna_obrona = max(0, int(gracz.obrona * stan["buff_obrona_mnoznik"]))
        stan["buff_obrona_tury"] -= 1
        if stan["buff_obrona_tury"] == 0:
            stan["buff_obrona_mnoznik"] = 1.0
    else:
        efektywna_obrona = gracz.obrona

    obrazenia = _oblicz_obrazenia(przeciwnik.atak, efektywna_obrona)

    # Osłabienie (Klątwa śmierci)
    if stan["wrog_oslabienie_tury"] > 0:
        obrazenia = max(1, int(obrazenia * 0.5))
        stan["wrog_oslabienie_tury"] -= 1

    # Unik (Zwiadowca)
    if stan["unik_aktywny"]:
        stan["unik_aktywny"] = False
        if random.random() < 0.75:
            print(f"  💨  Uniknąłeś ataku {przeciwnik.nazwa}!")
            return
        print(f"  💨  Próbowałeś uniknąć, ale {przeciwnik.nazwa} trafił!")

    # Tarcza runowa
    if stan["tarcza_runowa"] > 0:
        absorbcja = min(stan["tarcza_runowa"], obrazenia)
        stan["tarcza_runowa"] -= absorbcja
        obrazenia -= absorbcja
        if absorbcja > 0:
            print(
                f"  🔮  Tarcza runowa absorbuje {absorbcja} obrażeń!"
                f" (Pozostało: {stan['tarcza_runowa']})"
            )

    gracz.hp = max(0, gracz.hp - obrazenia)
    if obrazenia > 0:
        print(f"  💀  {przeciwnik.nazwa} atakuje cię! Otrzymujesz {obrazenia} obrażeń.")
    else:
        print(f"  🔮  Tarcza runowa całkowicie zablokowała atak {przeciwnik.nazwa}!")


# ------------------------------------------------------------------ #
#  Nagrody i mana po walce                                           #
# ------------------------------------------------------------------ #

def _odnow_mane_po_walce(gracz: Gracz) -> None:
    """Odnawia 20 many Magowi po zakończeniu walki."""
    if gracz.max_mana > 0:
        gracz.mana = min(gracz.mana + 20, gracz.max_mana)


def _zakonczenie_wygrana(gracz: Gracz, przeciwnik: Przeciwnik) -> None:
    """Przetwarza nagrody po wygranej walce."""
    zloto = przeciwnik.losowe_zloto()
    gracz.zloto += zloto
    komunikaty = gracz.zdobadz_exp(przeciwnik.exp_nagroda)
    _odnow_mane_po_walce(gracz)

    wyswietl_linie()
    print(f"\n  🏆  Pokonałeś {przeciwnik.nazwa}!")
    print(f"  💰  Zdobyłeś {zloto} złota!")
    for msg in komunikaty:
        print(f"  {msg}")
    nacisnij_enter()


# ------------------------------------------------------------------ #
#  Główna pętla walki                                                 #
# ------------------------------------------------------------------ #

def przeprowadz_walke(gracz: Gracz) -> str:
    """
    Główna pętla walki. Zwraca: 'wygrana', 'przegrana' lub 'ucieczka'.
    """
    przeciwnik = losuj_przeciwnika(gracz.poziom)
    stan = _nowy_stan_walki()

    wyczysc()
    print(f"\n  *** STARCIE! ***")
    print(f"  {przeciwnik.opis}")
    print(f"  Napotkałeś: {przeciwnik.nazwa}!\n")
    nacisnij_enter()

    while gracz.zyje() and przeciwnik.zyje():
        _wyswietl_stan_walki(gracz, przeciwnik, stan)
        wynik = _tura_gracza(gracz, przeciwnik, stan)

        if wynik == "wygrana":
            _zakonczenie_wygrana(gracz, przeciwnik)
            return "wygrana"
        if wynik == "ucieczka":
            _odnow_mane_po_walce(gracz)
            return "ucieczka"

        # Tura przeciwnika (sprawdź czy żyje po ataku gracza)
        if przeciwnik.zyje():
            _tura_przeciwnika(gracz, przeciwnik, stan)

        # Wróg mógł umrzeć od trucizny w turze przeciwnika
        if not przeciwnik.zyje():
            _zakonczenie_wygrana(gracz, przeciwnik)
            return "wygrana"

        if not gracz.zyje():
            print(f"\n  💀  Zostałeś pokonany przez {przeciwnik.nazwa}...")
            nacisnij_enter()
            return "przegrana"

        _koniec_rundy(stan)
        nacisnij_enter()

    return "wygrana"
