"""Prosty system podróży, biomów i zdarzeń losowych."""

import random

from game.combat import przeprowadz_walke
from game.player import Gracz
from game.shop import otworz_sklep
from game.utils import wyczysc, wyswietl_linie, nacisnij_enter


_KIERUNKI = {
    "1": {"nazwa": "prosto", "opis": "podążasz głównym traktem"},
    "2": {"nazwa": "w lewo", "opis": "skręcasz na zarośnięty szlak"},
    "3": {"nazwa": "w prawo", "opis": "wybierasz węższą kamienistą drogę"},
    "4": {"nazwa": "zawróć", "opis": "wracasz bezpiecznie do obozu"},
}

_BIOMY = {
    "1": [
        {
            "nazwa": "równiny",
            "opis": "Falujące trawy i stary trakt ciągną się aż po horyzont.",
            "lokacje": [
                {"nazwa": "kamienny krąg", "opis": "Między głazami unosi się ciepła energia.", "efekt": "leczenie"},
                {"nazwa": "porzucony wóz", "opis": "Wśród skrzyń wciąż coś błyszczy.", "efekt": "zloto"},
                {"nazwa": "kapliczka przydrożna", "opis": "Czujesz spokój i odzyskujesz siły.", "efekt": "mana"},
            ],
            "budynki": ["zajazd na rozstaju", "stara strażnica"],
        },
        {
            "nazwa": "ruiny",
            "opis": "Pęknięte mury i kolumny przypominają o dawnym królestwie.",
            "lokacje": [
                {"nazwa": "zawalony dziedziniec", "opis": "Pod gruzem znajdujesz coś przydatnego.", "efekt": "mikstura"},
                {"nazwa": "zapomniany ołtarz", "opis": "Dawna magia nadal tu pulsuje.", "efekt": "mana"},
                {"nazwa": "wybita brama", "opis": "Wiatr niesie szept dawno zaginionych strażników.", "efekt": "nic"},
            ],
            "budynki": ["opuszczona biblioteka", "pęknięta wieża maga"],
        },
    ],
    "2": [
        {
            "nazwa": "las",
            "opis": "Gęste korony drzew tłumią światło i każdy krok brzmi podejrzanie.",
            "lokacje": [
                {"nazwa": "leśny zagajnik", "opis": "Źródło wśród paproci koi twoje rany.", "efekt": "leczenie"},
                {"nazwa": "myśliwski obóz", "opis": "Ktoś zostawił zapasy i kilka monet.", "efekt": "zloto"},
                {"nazwa": "druidyczny krąg", "opis": "Naturalna energia wraca do twojego ciała.", "efekt": "mana"},
            ],
            "budynki": ["drewniana chatka", "myśliwska wieża"],
        },
        {
            "nazwa": "bagna",
            "opis": "Mgła snuje się nad cuchnącą wodą, a teren zdradliwie chlupocze.",
            "lokacje": [
                {"nazwa": "mokradła z ziołami", "opis": "Wśród trzcin znajdujesz lecznicze rośliny.", "efekt": "mikstura"},
                {"nazwa": "zatopiony pomost", "opis": "Między deskami tkwi sakiewka.", "efekt": "zloto"},
                {"nazwa": "cisza bagiennego oczka", "opis": "To miejsce jest niepokojąco spokojne.", "efekt": "nic"},
            ],
            "budynki": ["zapadła chata zielarki", "pochylona kaplica"],
        },
    ],
    "3": [
        {
            "nazwa": "wzgórza",
            "opis": "Ścieżka wspina się między skałami i daje szeroki widok na okolicę.",
            "lokacje": [
                {"nazwa": "skalny balkon", "opis": "Znajdujesz stary schowek zwiadowcy.", "efekt": "zloto"},
                {"nazwa": "górskie źródełko", "opis": "Krystaliczna woda przywraca ci siły.", "efekt": "leczenie"},
                {"nazwa": "wietrzny menhir", "opis": "Powietrze trzeszczy od dzikiej magii.", "efekt": "mana"},
            ],
            "budynki": ["kamienna strażnica", "górski posterunek"],
        },
        {
            "nazwa": "kanion",
            "opis": "Czerwone ściany wąwozu odbijają każdy dźwięk twoich kroków.",
            "lokacje": [
                {"nazwa": "sucha grota", "opis": "W cieniu skał odkrywasz pozostawiony ekwipunek.", "efekt": "mikstura"},
                {"nazwa": "wąska półka skalna", "opis": "Ktoś ukrył tu awaryjny zapas monet.", "efekt": "zloto"},
                {"nazwa": "echo wąwozu", "opis": "Na chwilę wydaje ci się, że ktoś cię obserwuje.", "efekt": "nic"},
            ],
            "budynki": ["wykuta brama kopalni", "opuszczony magazyn kupców"],
        },
    ],
}


def _menu_kierunku() -> str:
    """Pyta gracza o kierunek podróży."""
    while True:
        wyczysc()
        wyswietl_linie("═")
        print("  WYBÓR TRASY")
        wyswietl_linie("═")
        print("  [1]  Idź prosto")
        print("  [2]  Skręć w lewo")
        print("  [3]  Skręć w prawo")
        print("  [4]  Zawróć do obozu")
        print()
        wybor = input("  Twój wybór: ").strip()
        if wybor in _KIERUNKI:
            return wybor
        print("  Nieprawidłowy wybór.")
        nacisnij_enter()


def _pokaz_biom(kierunek: str, biom: dict) -> None:
    wyczysc()
    wyswietl_linie("═")
    print("  PODRÓŻ")
    wyswietl_linie("═")
    print(f"  Ruszasz { _KIERUNKI[kierunek]['nazwa'] } — {_KIERUNKI[kierunek]['opis']}.")
    print(f"  Trafiasz na biom: {biom['nazwa'].title()}.")
    print(f"  {biom['opis']}")
    nacisnij_enter()


def _odnawianie(gracz: Gracz, hp: int = 0, mana: int = 0) -> None:
    """Przywraca graczowi HP i manę w bezpieczny sposób."""
    if hp > 0:
        gracz.hp = min(gracz.max_hp, gracz.hp + hp)
    if mana > 0 and gracz.max_mana > 0:
        gracz.mana = min(gracz.max_mana, gracz.mana + mana)


def _losowa_lokacja(gracz: Gracz, biom: dict) -> None:
    """Obsługuje proste losowe miejsce po drodze."""
    lokacja = random.choice(biom["lokacje"])
    wyczysc()
    wyswietl_linie()
    print(f"  📍  Natrafiasz na: {lokacja['nazwa']}")
    print(f"  {lokacja['opis']}")

    efekt = lokacja["efekt"]
    if efekt == "zloto":
        zloto = random.randint(8, 18)
        gracz.zloto += zloto
        print(f"  💰  Znajdujesz {zloto} szt. złota.")
    elif efekt == "leczenie":
        leczenie = random.randint(12, 28)
        poprzednie_hp = gracz.hp
        _odnawianie(gracz, hp=leczenie)
        print(f"  ❤️  Odzyskujesz {gracz.hp - poprzednie_hp} HP.")
    elif efekt == "mana":
        if gracz.max_mana > 0:
            mana = random.randint(10, 20)
            poprzednia_mana = gracz.mana
            _odnawianie(gracz, mana=mana)
            print(f"  🔮  Odzyskujesz {gracz.mana - poprzednia_mana} many.")
        else:
            zloto = random.randint(6, 12)
            gracz.zloto += zloto
            print(f"  💰  Zamiast magii odnajdujesz {zloto} szt. złota.")
    elif efekt == "mikstura":
        gracz.mikstury += 1
        print("  🧪  Zdobywasz 1 miksturę leczenia.")
    else:
        print("  Po krótkim postoju ruszasz dalej.")

    nacisnij_enter()


def _wejdz_do_budynku(gracz: Gracz, nazwa_budynku: str) -> str | None:
    """Obsługuje rezultat wejścia do budynku."""
    wynik = random.choice(["skarb", "odpoczynek", "kupiec", "zasadzka"])
    wyczysc()
    wyswietl_linie()
    print(f"  🚪  Wchodzisz do miejsca: {nazwa_budynku}.")

    if wynik == "skarb":
        zloto = random.randint(12, 28)
        gracz.zloto += zloto
        if random.random() < 0.4:
            gracz.mikstury += 1
            print(f"  Znajdujesz schowany kufer: {zloto} złota i 1 miksturę.")
        else:
            print(f"  Znajdujesz schowany kufer: {zloto} złota.")
        nacisnij_enter()
        return None

    if wynik == "odpoczynek":
        poprzednie_hp = gracz.hp
        poprzednia_mana = gracz.mana
        _odnawianie(gracz, hp=35, mana=20)
        print(f"  W środku znajdujesz bezpieczny kąt do odpoczynku.")
        print(f"  ❤️  HP +{gracz.hp - poprzednie_hp}")
        if gracz.max_mana > 0:
            print(f"  🔮  Mana +{gracz.mana - poprzednia_mana}")
        nacisnij_enter()
        return None

    if wynik == "kupiec":
        print("  Okazuje się, że w środku działa wędrowny kupiec.")
        nacisnij_enter()
        otworz_sklep(gracz)
        return None

    print("  To pułapka! Z cienia wyskakuje wróg.")
    nacisnij_enter()
    return przeprowadz_walke(gracz)


def _budynek(gracz: Gracz, biom: dict) -> str | None:
    """Obsługuje spotkanie z budynkiem i wybór wejścia lub ominięcia."""
    nazwa_budynku = random.choice(biom["budynki"])

    while True:
        wyczysc()
        wyswietl_linie()
        print(f"  🏚️  Dostrzegasz: {nazwa_budynku}")
        print("  [1]  Wejdź do środka")
        print("  [2]  Omiń budynek")
        print()
        wybor = input("  Twój wybór: ").strip()

        if wybor == "1":
            return _wejdz_do_budynku(gracz, nazwa_budynku)
        if wybor == "2":
            print("  Zachowujesz ostrożność i omijasz budynek szerokim łukiem.")
            nacisnij_enter()
            return None

        print("  Nieprawidłowy wybór.")
        nacisnij_enter()


def wyrusz_w_podroz(gracz: Gracz) -> str:
    """Uruchamia prostą podróż z kierunkami, biomami i zdarzeniami."""
    kierunek = _menu_kierunku()
    if kierunek == "4":
        print("\n  Zmieniasz zdanie i wracasz do obozu.")
        nacisnij_enter()
        return "powrot"

    biom = random.choice(_BIOMY[kierunek])
    _pokaz_biom(kierunek, biom)

    liczba_zdarzen = random.randint(2, 3)
    walka_odbyta = False

    for _ in range(liczba_zdarzen):
        typ = random.choices(
            ["lokacja", "budynek", "walka"],
            weights=[50, 25, 25] if not walka_odbyta else [70, 30, 0],
            k=1,
        )[0]

        if typ == "lokacja":
            _losowa_lokacja(gracz, biom)
            continue

        if typ == "budynek":
            wynik = _budynek(gracz, biom)
            if wynik == "przegrana":
                return "przegrana"
            if wynik == "wygrana":
                walka_odbyta = True
            continue

        wyczysc()
        wyswietl_linie()
        print(f"  ⚔️  W biomie {biom['nazwa']} ktoś zastępuje ci drogę!")
        nacisnij_enter()
        wynik = przeprowadz_walke(gracz)
        walka_odbyta = True
        if wynik == "przegrana":
            return "przegrana"

    wyczysc()
    wyswietl_linie("═")
    print("  KONIEC WYPRAWY")
    wyswietl_linie("═")
    print(f"  Wracasz do obozu z biomu: {biom['nazwa']}.")
    if not walka_odbyta:
        print("  Tym razem obyło się bez większej walki, ale przygód i tak nie brakowało.")
    nacisnij_enter()
    return "powrot"
