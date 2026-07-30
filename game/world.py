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
            "budynki": [
                {"nazwa": "zajazd na rozstaju", "typ": "karczma"},
                {"nazwa": "stara strażnica", "typ": "jaskinia"},
            ],
        },
        {
            "nazwa": "ruiny",
            "opis": "Pęknięte mury i kolumny przypominają o dawnym królestwie.",
            "lokacje": [
                {"nazwa": "zawalony dziedziniec", "opis": "Pod gruzem znajdujesz coś przydatnego.", "efekt": "mikstura"},
                {"nazwa": "zapomniany ołtarz", "opis": "Dawna magia nadal tu pulsuje.", "efekt": "mana"},
                {"nazwa": "wybita brama", "opis": "Wiatr niesie szept dawno zaginionych strażników.", "efekt": "nic"},
            ],
            "budynki": [
                {"nazwa": "opuszczona biblioteka", "typ": "świątynia"},
                {"nazwa": "pęknięta wieża maga", "typ": "jaskinia"},
            ],
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
            "budynki": [
                {"nazwa": "drewniana chatka", "typ": "karczma"},
                {"nazwa": "myśliwska wieża", "typ": "jaskinia"},
            ],
        },
        {
            "nazwa": "bagna",
            "opis": "Mgła snuje się nad cuchnącą wodą, a teren zdradliwie chlupocze.",
            "lokacje": [
                {"nazwa": "mokradła z ziołami", "opis": "Wśród trzcin znajdujesz lecznicze rośliny.", "efekt": "mikstura"},
                {"nazwa": "zatopiony pomost", "opis": "Między deskami tkwi sakiewka.", "efekt": "zloto"},
                {"nazwa": "cisza bagiennego oczka", "opis": "To miejsce jest niepokojąco spokojne.", "efekt": "nic"},
            ],
            "budynki": [
                {"nazwa": "zapadła chata zielarki", "typ": "karczma"},
                {"nazwa": "pochylona kaplica", "typ": "świątynia"},
            ],
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
            "budynki": [
                {"nazwa": "kamienna strażnica", "typ": "jaskinia"},
                {"nazwa": "górski posterunek", "typ": "karczma"},
            ],
        },
        {
            "nazwa": "kanion",
            "opis": "Czerwone ściany wąwozu odbijają każdy dźwięk twoich kroków.",
            "lokacje": [
                {"nazwa": "sucha grota", "opis": "W cieniu skał odkrywasz pozostawiony ekwipunek.", "efekt": "mikstura"},
                {"nazwa": "wąska półka skalna", "opis": "Ktoś ukrył tu awaryjny zapas monet.", "efekt": "zloto"},
                {"nazwa": "echo wąwozu", "opis": "Na chwilę wydaje ci się, że ktoś cię obserwuje.", "efekt": "nic"},
            ],
            "budynki": [
                {"nazwa": "wykuta brama kopalni", "typ": "jaskinia"},
                {"nazwa": "opuszczony magazyn kupców", "typ": "świątynia"},
            ],
        },
    ],
}


def _rysuj_mape(gracz: Gracz) -> None:
    """Wyświetla prostą mapę z aktualną pozycją gracza."""
    print("  MAPA OKOLICY")
    for y in range(5):
        pola = []
        for x in range(5):
            if x == gracz.mapa_x and y == gracz.mapa_y:
                pola.append("P")
            elif x == 2 and y == 2:
                pola.append("O")
            else:
                pola.append("·")
        print("   " + " ".join(pola))
    print(f"\n  P = twoja pozycja   O = okolice startowe")
    print(f"  Koordynaty: ({gracz.mapa_x}, {gracz.mapa_y})   Ostatni biom: {gracz.aktualny_biom}")
    print()


def _menu_kierunku(gracz: Gracz) -> str:
    """Pyta gracza o kierunek podróży."""
    while True:
        wyczysc()
        wyswietl_linie("═")
        print("  WYBÓR TRASY")
        wyswietl_linie("═")
        _rysuj_mape(gracz)
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


def _przesun_gracza(gracz: Gracz, kierunek: str) -> None:
    """Aktualizuje pozycję gracza na mapie."""
    ruchy = {
        "1": (0, -1),
        "2": (-1, 0),
        "3": (1, 0),
    }
    dx, dy = ruchy[kierunek]
    gracz.mapa_x = min(4, max(0, gracz.mapa_x + dx))
    gracz.mapa_y = min(4, max(0, gracz.mapa_y + dy))


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


def _zdarzenie_karczma(gracz: Gracz, nazwa_budynku: str) -> None:
    """Obsługuje karczmę z osobnymi zdarzeniami."""
    wynik = random.choice(["odpoczynek", "kupiec", "plotka"])
    wyczysc()
    wyswietl_linie()
    print(f"  🍺  Wchodzisz do miejsca: {nazwa_budynku}.")

    if wynik == "odpoczynek":
        poprzednie_hp = gracz.hp
        poprzednia_mana = gracz.mana
        _odnawianie(gracz, hp=30, mana=15)
        print("  Karczmarz pozwala ci odpocząć przy ogniu.")
        print(f"  ❤️  HP +{gracz.hp - poprzednie_hp}")
        if gracz.max_mana > 0:
            print(f"  🔮  Mana +{gracz.mana - poprzednia_mana}")
    elif wynik == "kupiec":
        print("  Przy stoliku czeka wędrowny handlarz.")
        nacisnij_enter()
        otworz_sklep(gracz)
        return
    else:
        print("  Słyszysz plotki o pobliskich skrytkach i dostajesz napiwek od podróżnych.")
        zloto = random.randint(10, 20)
        gracz.zloto += zloto
        print(f"  💰  Zyskujesz {zloto} szt. złota.")
    nacisnij_enter()


def _zdarzenie_jaskinia(gracz: Gracz, nazwa_budynku: str, biom_nazwa: str) -> str | None:
    """Obsługuje jaskinię lub podobne niebezpieczne miejsce."""
    wynik = random.choice(["skarb", "mikstura", "zasadzka"])
    wyczysc()
    wyswietl_linie()
    print(f"  🕳️  Wchodzisz do miejsca: {nazwa_budynku}.")

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

    if wynik == "mikstura":
        gracz.mikstury += 1
        print("  W niszy skalnej znajdujesz 1 miksturę leczenia.")
        nacisnij_enter()
        return None

    print("  To pułapka! Z cienia wyskakuje wróg.")
    nacisnij_enter()
    return przeprowadz_walke(gracz, biom_nazwa)


def _zdarzenie_swiatynia(gracz: Gracz, nazwa_budynku: str) -> None:
    """Obsługuje świątynię z błogosławieństwem lub darem."""
    wynik = random.choice(["blogoslawienstwo", "mana", "dar"])
    wyczysc()
    wyswietl_linie()
    print(f"  🛕  Wchodzisz do miejsca: {nazwa_budynku}.")

    if wynik == "blogoslawienstwo":
        gracz.hp = min(gracz.max_hp, gracz.hp + 20)
        gracz.obrona += 1
        print("  Otrzymujesz błogosławieństwo ochrony.")
        print("  ❤️  HP +20 (do limitu)   🛡️  Obrona +1")
    elif wynik == "mana":
        poprzednia_mana = gracz.mana
        _odnawianie(gracz, mana=30)
        print("  Starożytne runy wzmacniają twoją energię.")
        if gracz.max_mana > 0:
            print(f"  🔮  Mana +{gracz.mana - poprzednia_mana}")
        else:
            print("  Czujesz spokój, choć magia cię nie dotyczy.")
    else:
        gracz.mikstury += 1
        zloto = random.randint(8, 16)
        gracz.zloto += zloto
        print(f"  Kapłani zostawili po sobie dar: 1 mikstura i {zloto} złota.")
    nacisnij_enter()


def _wejdz_do_budynku(gracz: Gracz, budynek: dict, biom_nazwa: str) -> str | None:
    """Obsługuje rezultat wejścia do budynku zależnie od jego typu."""
    typ = budynek["typ"]
    nazwa_budynku = budynek["nazwa"]

    if typ == "karczma":
        _zdarzenie_karczma(gracz, nazwa_budynku)
        return None
    if typ == "jaskinia":
        return _zdarzenie_jaskinia(gracz, nazwa_budynku, biom_nazwa)
    if typ == "świątynia":
        _zdarzenie_swiatynia(gracz, nazwa_budynku)
        return None

    return None


def _budynek(gracz: Gracz, biom: dict) -> str | None:
    """Obsługuje spotkanie z budynkiem i wybór wejścia lub ominięcia."""
    budynek = random.choice(biom["budynki"])
    nazwa_budynku = budynek["nazwa"]
    typ = budynek["typ"]

    while True:
        wyczysc()
        wyswietl_linie()
        print(f"  🏚️  Dostrzegasz: {nazwa_budynku}")
        print(f"  Typ miejsca: {typ}")
        print("  [1]  Wejdź do środka")
        print("  [2]  Omiń budynek")
        print()
        wybor = input("  Twój wybór: ").strip()

        if wybor == "1":
            return _wejdz_do_budynku(gracz, budynek, biom["nazwa"])
        if wybor == "2":
            print("  Zachowujesz ostrożność i omijasz budynek szerokim łukiem.")
            nacisnij_enter()
            return None

        print("  Nieprawidłowy wybór.")
        nacisnij_enter()


def wyrusz_w_podroz(gracz: Gracz) -> str:
    """Uruchamia prostą podróż z kierunkami, biomami i zdarzeniami."""
    kierunek = _menu_kierunku(gracz)
    if kierunek == "4":
        print("\n  Zmieniasz zdanie i wracasz do obozu.")
        nacisnij_enter()
        return "powrot"

    _przesun_gracza(gracz, kierunek)
    biom = random.choice(_BIOMY[kierunek])
    gracz.aktualny_biom = biom["nazwa"]
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
        wynik = przeprowadz_walke(gracz, biom["nazwa"])
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
