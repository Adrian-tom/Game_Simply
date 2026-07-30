"""Moduł obsługujący system walki turowej."""

import random

from game.player import Gracz
from game.enemy import Przeciwnik, losuj_przeciwnika
from game.utils import wyczysc, nacisnij_enter, wyswietl_linie


def _oblicz_obrazenia(atak: int, obrona: int) -> int:
    """Oblicza zadane obrażenia z losową wariancją ±20%."""
    bazowe = max(1, atak - obrona)
    wariancja = max(1, int(bazowe * 0.2))
    return random.randint(max(1, bazowe - wariancja), bazowe + wariancja)


def _tura_gracza(gracz: Gracz, przeciwnik: Przeciwnik) -> str | None:
    """
    Obsługuje turę gracza. Zwraca 'wygrana', 'ucieczka' lub None
    (walka trwa dalej).
    """
    print(f"\n  Co robisz?\n"
          f"  [1] Atakuj\n"
          f"  [2] Użyj mikstury ({gracz.mikstury} szt.)\n"
          f"  [3] Uciekaj")

    while True:
        wybor = input("\n  Twój wybór: ").strip()
        if wybor == "1":
            obrazenia = _oblicz_obrazenia(gracz.atak, przeciwnik.obrona)
            przeciwnik.hp -= obrazenia
            print(f"\n  ⚔  Atakujesz {przeciwnik.nazwa}! Zadajesz {obrazenia} obrażeń.")
            if not przeciwnik.zyje():
                return "wygrana"
            return None
        elif wybor == "2":
            print(f"\n  🧪  {gracz.uzyj_miksture()}")
            return None
        elif wybor == "3":
            szansa = random.random()
            if szansa < 0.5:
                print("\n  🏃  Udało ci się uciec!")
                return "ucieczka"
            print("\n  🏃  Nie udało się uciec — przeciwnik blokuje drogę!")
            return None
        else:
            print("  Nieprawidłowy wybór. Wpisz 1, 2 lub 3.")


def _tura_przeciwnika(gracz: Gracz, przeciwnik: Przeciwnik) -> None:
    """Obsługuje atak przeciwnika na gracza."""
    obrazenia = _oblicz_obrazenia(przeciwnik.atak, gracz.obrona)
    gracz.hp = max(0, gracz.hp - obrazenia)
    print(f"  💀  {przeciwnik.nazwa} atakuje cię! Otrzymujesz {obrazenia} obrażeń.")


def _wyswietl_stan_walki(gracz: Gracz, przeciwnik: Przeciwnik) -> None:
    """Wyświetla aktualny stan walki."""
    wyswietl_linie()
    print(f"  {przeciwnik}")
    print(f"  {gracz.imie}  HP: {gracz.hp}/{gracz.max_hp} {gracz.pasek_hp()}")
    wyswietl_linie()


def przeprowadz_walke(gracz: Gracz) -> str:
    """
    Główna pętla walki. Zwraca: 'wygrana', 'przegrana' lub 'ucieczka'.
    """
    przeciwnik = losuj_przeciwnika(gracz.poziom)
    wyczysc()
    print(f"\n  *** STARCIE! ***")
    print(f"  {przeciwnik.opis}")
    print(f"  Napotkałeś: {przeciwnik.nazwa}!\n")
    nacisnij_enter()

    while gracz.zyje() and przeciwnik.zyje():
        _wyswietl_stan_walki(gracz, przeciwnik)
        wynik = _tura_gracza(gracz, przeciwnik)

        if wynik == "wygrana":
            _zakonczenie_wygrana(gracz, przeciwnik)
            return "wygrana"
        if wynik == "ucieczka":
            return "ucieczka"

        # Tura przeciwnika (jeśli żyje po ataku gracza)
        if przeciwnik.zyje():
            _tura_przeciwnika(gracz, przeciwnik)

        if not gracz.zyje():
            print(f"\n  💀  Zostałeś pokonany przez {przeciwnik.nazwa}...")
            nacisnij_enter()
            return "przegrana"

        nacisnij_enter()

    return "wygrana"


def _zakonczenie_wygrana(gracz: Gracz, przeciwnik: Przeciwnik) -> None:
    """Przetwarza nagrody po wygranej walce."""
    zloto = przeciwnik.losowe_zloto()
    gracz.zloto += zloto
    komunikaty = gracz.zdobadz_exp(przeciwnik.exp_nagroda)

    wyswietl_linie()
    print(f"\n  🏆  Pokonałeś {przeciwnik.nazwa}!")
    print(f"  💰  Zdobyłeś {zloto} złota!")
    for msg in komunikaty:
        print(f"  {msg}")
    nacisnij_enter()
