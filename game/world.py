"""Prosty system podróży, biomów i zdarzeń losowych."""

import random

from game.combat import przeprowadz_walke
from game.player import Gracz
from game.shop import otworz_sklep, otworz_kuznia
from game.dialogues import (
    dialog_karczmarz, dialog_kupiec, dialog_kowal,
    dialog_kaplan, dialog_stary_rycerz, dialog_tajemniczy, losowy_npc,
)
from game.quests import sprawdz_questy
from game.utils import wyczysc, wyswietl_linie, nacisnij_enter
from game.mapa import (
    zapewnij_mape,
    rysuj_mape,
    przesun_gracza,
    pole_gracza,
    etykieta_kierunku,
    kierunki,
    opis_punktu,
    PUNKTY_MITYCZNE,
)
from game.oboz import zbierz_na_polu, pozostale_zbiory, linia_surowcow
from game.mityczne import zdarzenie_mityczne
from game.rekruci import oferta_rekrutacji, rozlicz_zbieraczy
from game.savegame import zapisz_gre
from game.atrybuty import SKILLE, przeprowadz_test, trudnosc
from game.osada import dodaj_czas, oznacz_wyjscie, rozlicz_powrot_do_obozu
from game.miasto import wejdz_do_miasta
from game.ikony import etykieta_biomu, kierunek as ikona_kierunku, punkt as ikona_punktu


_BIOMY = {
    "równiny": {
        "nazwa": "równiny",
        "ikona": "🌾",
        "opis": "Falujące trawy i stary trakt ciągną się aż po horyzont.",
        "lokacje": [
            {"nazwa": "kamienny krąg", "opis": "Między głazami unosi się ciepła energia.", "efekt": "leczenie"},
            {"nazwa": "porzucony wóz", "opis": "Wśród skrzyń wciąż coś błyszczy.", "efekt": "zloto"},
            {"nazwa": "kapliczka przydrożna", "opis": "Czujesz spokój i odzyskujesz siły.", "efekt": "mana"},
        ],
        "budynki": [
            {"nazwa": "zajazd na rozstaju", "typ": "karczma"},
            {"nazwa": "stara strażnica", "typ": "jaskinia"},
            {"nazwa": "wiejska kuźnia", "typ": "kuźnia"},
        ],
    },
    "ruiny": {
        "nazwa": "ruiny",
        "ikona": "🏚",
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
    "las": {
        "nazwa": "las",
        "ikona": "🌲",
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
    "bagna": {
        "nazwa": "bagna",
        "ikona": "🐸",
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
    "wzgórza": {
        "nazwa": "wzgórza",
        "ikona": "⛰",
        "opis": "Ścieżka wspina się między skałami i daje szeroki widok na okolicę.",
        "lokacje": [
            {"nazwa": "skalny balkon", "opis": "Znajdujesz stary schowek zwiadowcy.", "efekt": "zloto"},
            {"nazwa": "górskie źródełko", "opis": "Krystaliczna woda przywraca ci siły.", "efekt": "leczenie"},
            {"nazwa": "wietrzny menhir", "opis": "Powietrze trzeszczy od dzikiej magii.", "efekt": "mana"},
        ],
        "budynki": [
            {"nazwa": "kamienna strażnica", "typ": "jaskinia"},
            {"nazwa": "górski posterunek", "typ": "karczma"},
            {"nazwa": "kuźnia pod szczytem", "typ": "kuźnia"},
        ],
    },
    "kanion": {
        "nazwa": "kanion",
        "ikona": "🏜",
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
}


def _szablon_biomu(nazwa: str) -> dict:
    """Zwraca dane biomu; w razie luki — równiny."""
    return _BIOMY.get(nazwa, _BIOMY["równiny"])


def _budynek_z_pola(pole: dict) -> dict | None:
    """Budynek przypisany do pola (stały punkt na mapie)."""
    punkt = pole.get("punkt")
    if punkt in (None, "obóz", "boss", "miasto") or punkt in PUNKTY_MITYCZNE:
        return None
    szablon = _szablon_biomu(pole["biom"])
    pasujace = [b for b in szablon["budynki"] if b["typ"] == punkt]
    if pasujace:
        return pasujace[0]
    return {"nazwa": opis_punktu(punkt), "typ": punkt}


def przegladaj_mape(gracz: Gracz) -> None:
    """Podgląd mapy z obozu (bez ruchu)."""
    wyczysc()
    wyswietl_linie("═")
    print("  MAPA OKOLICY")
    wyswietl_linie("═")
    print()
    rysuj_mape(gracz)
    print("  🧭  Wyrusz na przygodę, aby iść na północ, południe, wschód lub zachód.")
    print()
    nacisnij_enter()


_NOWE_SRODOWISKA = [
    ("Wkraczasz na nowe ziemie...", "Horyzont odsłania przed tobą zupełnie nowy kraj."),
    ("Krajobraz się zmienia.", "Czujesz, że te tereny różnią się od wszystkiego, co widziałeś wcześniej."),
    ("Przekraczasz niewidzialną granicę.", "Powietrze staje się inne — to nowy region."),
    ("Świat zdaje się rozszerzać.", "Za horyzontem kryje się jeszcze więcej przygód."),
    ("Nowe środowisko, nowe wyzwania.", "Teren zmienia się gwałtownie — ruszasz dalej w nieznane."),
]


def _pokaz_nowe_srodowisko(gracz: Gracz) -> None:
    """Wyświetla efektowny komunikat o przejściu na nową mapę."""
    wyczysc()
    wyswietl_linie("═")
    print(f"  ✨  NOWY REGION #{gracz.mapa_gen}  ✨")
    wyswietl_linie("═")
    tytul, opis = random.choice(_NOWE_SRODOWISKA)
    print(f"\n  {tytul}")
    print(f"  {opis}")
    print(f"\n  Stoisz na skraju nieznanych ziem. Odkryte pola poprzedniego regionu")
    print(f"  zostają za tobą — tu wszystko trzeba poznać od nowa.\n")
    nacisnij_enter()


def _pokaz_wejscie_na_pole(gracz: Gracz, nazwa_kierunku: str) -> None:
    pole = pole_gracza(gracz)
    biom = _szablon_biomu(pole["biom"])
    wyczysc()
    wyswietl_linie("═")
    print("  🧭  PODRÓŻ")
    wyswietl_linie("═")
    print(f"  {ikona_kierunku(nazwa_kierunku)}  Idziesz na {nazwa_kierunku}.")
    print(f"  Trafiasz na: {etykieta_biomu(biom['nazwa'])}.")
    print(f"  {biom['opis']}")
    if pole.get("punkt") == "boss":
        print("\n  ☠  Powietrze gęstnieje. Coś potężnego czeka na tym polu.")
    elif pole.get("punkt") == "obóz":
        print("\n  🏕  Widzisz znajome palenisko — to twój obóz.")
    elif pole.get("punkt"):
        budynek = _budynek_z_pola(pole)
        if budynek:
            ik = ikona_punktu(budynek.get("typ") or pole.get("punkt"))
            print(f"\n  {ik}  Na polu stoi: {budynek['nazwa']} ({budynek['typ']}).")
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
    wyczysc()
    wyswietl_linie()
    print(f"  🍺  Wchodzisz do miejsca: {nazwa_budynku}.\n")
    print("  Co chcesz zrobić?\n")
    print("  [1]  🔥  Odpocząć przy ogniu")
    print("  [2]  🍺  Porozmawiać z karczmarzem")
    print("  [3]  💰  Skorzystać ze sklepu wędrownego kupca")
    print("  [4]  👂  Posłuchać plotek")
    print("  [5]  🗡  Porozmawiać z gościem przy kominku")
    print("  [6]  🤝  Szukać towarzysza (najem)")
    print()
    wybor = input("  Twój wybór: ").strip()

    if wybor == "1":
        poprzednie_hp = gracz.hp
        poprzednia_mana = gracz.mana
        _odnawianie(gracz, hp=30, mana=15)
        print("  Karczmarz pozwala ci odpocząć przy ogniu.")
        print(f"  ❤️  HP +{gracz.hp - poprzednie_hp}")
        if gracz.max_mana > 0:
            print(f"  🔮  Mana +{gracz.mana - poprzednia_mana}")
        nacisnij_enter()
    elif wybor == "2":
        dialog_karczmarz(gracz)
    elif wybor == "3":
        print("  Przy stoliku czeka wędrowny handlarz.")
        nacisnij_enter()
        dialog_kupiec(gracz)
        otworz_sklep(gracz)
    elif wybor == "4":
        print("  Słyszysz plotki o pobliskich skrytkach i dostajesz napiwek od podróżnych.")
        zloto = random.randint(10, 20)
        gracz.zloto += zloto
        print(f"  💰  Zyskujesz {zloto} szt. złota.")
        nacisnij_enter()
        losowy_npc(gracz)
    elif wybor == "5":
        dialog_stary_rycerz(gracz)
    elif wybor == "6":
        oferta_rekrutacji(gracz)
    else:
        print("  Nic nie robisz i szybko wychodzisz.")
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
    wyczysc()
    wyswietl_linie()
    print(f"  🛕  Wchodzisz do miejsca: {nazwa_budynku}.\n")

    # Zarejestruj wizytę w świątyni dla questa
    gracz.statystyki["odwiedzone_swiatynie"] = gracz.statystyki.get("odwiedzone_swiatynie", 0) + 1

    print("  Co chcesz zrobić?\n")
    print("  [1]  ✨  Przyjąć błogosławieństwo")
    print("  [2]  🙏  Porozmawiać z kapłanem")
    print()
    wybor = input("  Twój wybór: ").strip()

    if wybor == "2":
        dialog_kaplan(gracz)
        return

    # Domyślnie i dla [1]: błogosławieństwo
    if getattr(gracz, "blogoslawienstwo_wyprawy", False):
        print("  Kapłan unosi dłoń.")
        print("  „Dar świątyni przyjąłeś już na tej wyprawie. Wróć po następnym obozie.”")
        nacisnij_enter()
        return

    wynik = random.choice(["blogoslawienstwo", "mana", "dar"])
    gracz.blogoslawienstwo_wyprawy = True
    if wynik == "blogoslawienstwo":
        gracz.hp = min(gracz.max_hp, gracz.hp + 20)
        gracz.obrona += 1
        print("  Otrzymujesz błogosławieństwo ochrony. (raz na wyprawę)")
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
    for msg in sprawdz_questy(gracz):
        print(msg)
    nacisnij_enter()


def _zdarzenie_kuznia(gracz: Gracz, nazwa_budynku: str) -> None:
    """Obsługuje kuźnię — zakup ekwipunku lub rozmowa z kowalem."""
    wyczysc()
    wyswietl_linie()
    print(f"  ⚒  Wchodzisz do miejsca: {nazwa_budynku}.\n")
    print("  Co chcesz zrobić?\n")
    print("  [1]  ⚔  Obejrzeć i kupić ekwipunek")
    print("  [2]  ⚒  Porozmawiać z kowalem")
    print()
    wybor = input("  Twój wybór: ").strip()

    if wybor == "1":
        otworz_kuznia(gracz)
    elif wybor == "2":
        dialog_kowal(gracz)
    else:
        print("  Kowal kiwa głową i wraca do roboty.")
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
    if typ == "kuźnia":
        _zdarzenie_kuznia(gracz, nazwa_budynku)
        return None

    return None


_IKONY_BUDYNKOW = {
    "karczma": "🍺",
    "jaskinia": "🕳️",
    "świątynia": "🛕",
    "kuźnia": "⚒",
}


def _budynek(gracz: Gracz, biom: dict, budynek: dict | None = None) -> str | None:
    """Obsługuje spotkanie z budynkiem i wybór wejścia lub ominięcia."""
    if budynek is None:
        budynek = random.choice(biom["budynki"])
    nazwa_budynku = budynek["nazwa"]
    typ = budynek["typ"]
    ikona = _IKONY_BUDYNKOW.get(typ, "🏚️")

    ma_tajemniczego = random.random() < 0.2

    while True:
        wyczysc()
        wyswietl_linie()
        print(f"  {ikona}  Dostrzegasz: {nazwa_budynku}")
        print(f"  Typ miejsca: {ikona_punktu(typ)} {typ}")

        if ma_tajemniczego:
            print("  🌑  Przy wejściu siedzi tajemnicza postać...")
            print("  [1]  🚪  Wejdź do środka")
            print("  [2]  🌑  Porozmawiaj z tajemniczą postacią")
            print("  [3]  🚶  Omiń budynek")
        else:
            print("  [1]  🚪  Wejdź do środka")
            print("  [2]  🚶  Omiń budynek")

        print()
        wybor = input("  Twój wybór: ").strip()

        if wybor == "1":
            return _wejdz_do_budynku(gracz, budynek, biom["nazwa"])
        if ma_tajemniczego and wybor == "2":
            dialog_tajemniczy(gracz)
            continue
        if (ma_tajemniczego and wybor == "3") or (not ma_tajemniczego and wybor == "2"):
            print("  Zachowujesz ostrożność i omijasz budynek szerokim łukiem.")
            nacisnij_enter()
            return None

        print("  Nieprawidłowy wybór.")
        nacisnij_enter()


# ------------------------------------------------------------------ #
#  Zdarzenia narracyjne                                               #
# ------------------------------------------------------------------ #

_ZDARZENIA_NARRACYJNE = [
    {
        "tytul": "Samotny kupiec",
        "opis": "Przy drodze siedzi wyczerpany kupiec. Prosi o pomoc — ktoś go okradł.",
        "opcje": [
            ("Pomóż mu i daj 10 złota", "pomoc"),
            ("Okradnij go — jest bezbronny", "okradnij"),
            ("Idź dalej, nie twój problem", "ignoruj"),
        ],
        "efekty": {
            "pomoc": ("zloto", -10, "  Kupiec dziękuje serdecznie. Karma się opłaci.", "hp", 20),
            "okradnij": ("zloto", 25, "  Grabisz nieszczęśnika. Ciężki ciężar na sumieniu.", "hp", 0),
            "ignoruj": ("brak", 0, "  Mijasz go bez słowa.", "brak", 0),
        },
    },
    {
        "tytul": "Ranny żołnierz",
        "opis": "Na poboczu leży ranny żołnierz z zatrutą strzałą w ramieniu.",
        "opcje": [
            ("Użyj mikstury leczenia aby mu pomóc", "mikstura"),
            ("Zostaw go — sam się wykaraska", "zostaw"),
            ("Ograbisz go z jego ekwipunku", "okradnij"),
        ],
        "efekty": {
            "mikstura": ("mikstura", -1, "  Żołnierz odżywa. W podziękowaniu daje ci garść złota.", "zloto", 30),
            "zostaw": ("brak", 0, "  Żołnierz patrzy za tobą ze smutkiem.", "brak", 0),
            "okradnij": ("zloto", 15, "  Okradasz rannego. Obrzydliwy uczynek.", "hp", -15),
        },
    },
    {
        "tytul": "Tajemnicza skrzynka",
        "opis": "Na środku drogi stoi zamknięta skrzynka. Może być pułapka... lub skarb.",
        "opcje": [
            ("Otwórz skrzynkę", "otworz"),
            ("Zniszcz ją z bezpiecznej odległości", "zniszcz"),
            ("Omij szerokim łukiem", "omiń"),
        ],
        "efekty": {
            "otworz": ("zamek", 0, "  Próbujesz otworzyć zamek...", "brak", 0),
            "zniszcz": ("brak", 0, "  Skrzynka wybucha — byłeś bezpieczny.", "brak", 0),
            "omiń": ("brak", 0, "  Ostrożność przede wszystkim.", "brak", 0),
        },
    },
    {
        "tytul": "Głodny wieśniak",
        "opis": "Wieśniak z wychudzonym dzieckiem prosi o wsparcie.",
        "opcje": [
            ("Daj im 15 złota", "daj"),
            ("Odejdź — nie twoja sprawa", "odejdź"),
        ],
        "efekty": {
            "daj": ("zloto", -15, "  Wieśniak błogosławi cię. Twoje serce jest lżejsze.", "mana", 20),
            "odejdź": ("brak", 0, "  Odchodzisz. Ich spojrzenia prześladują cię przez chwilę.", "brak", 0),
        },
    },
]


def _zmien_karme(gracz: Gracz, delta: int) -> None:
    """Zmienia karmę i krótko o tym informuje."""
    gracz.karma = getattr(gracz, "karma", 0) + delta
    if delta > 0:
        print(f"  ✨  Karma +{delta}  (łącznie: {gracz.karma})")
    elif delta < 0:
        print(f"  🌑  Karma {delta}  (łącznie: {gracz.karma})")


def _zdarzenie_narracyjne(gracz: Gracz) -> None:
    """Losuje i obsługuje zdarzenie narracyjne z wyborem moralnym."""
    zdarzenie = random.choice(_ZDARZENIA_NARRACYJNE)
    wyczysc()
    wyswietl_linie("─")
    print(f"  📜  {zdarzenie['tytul'].upper()}")
    wyswietl_linie("─")
    print(f"\n  {zdarzenie['opis']}\n")

    for i, (tekst, _) in enumerate(zdarzenie["opcje"], 1):
        print(f"  [{i}]  {tekst}")
    print()

    while True:
        wybor = input("  Twój wybór: ").strip()
        try:
            idx = int(wybor) - 1
            if 0 <= idx < len(zdarzenie["opcje"]):
                _, klucz = zdarzenie["opcje"][idx]
                break
        except ValueError:
            pass
        print("  Nieprawidłowy wybór.")

    efekt = zdarzenie["efekty"][klucz]
    typ1, wartosc1, opis, typ2, wartosc2 = efekt

    print()
    if typ1 == "zamek":
        print(f"  {opis}")
        wynik = przeprowadz_test(gracz, "zwinne_palce", trudnosc(gracz, 13))
        if wynik.sukces:
            zloto = random.randint(20, 50)
            gracz.zloto += zloto
            gracz.mikstury += 1
            print(f"  💰  Zamek puszcza! W środku: {zloto} złota i mikstura.")
        elif wynik.wpadka:
            dmg = random.randint(12, 22)
            gracz.hp = max(1, gracz.hp - dmg)
            print(f"  ⚠  Zamek zacina się i odpala pułapkę! Tracisz {dmg} HP.")
        else:
            print("  Zamek nie ustępuje. Odchodzisz z pustymi rękami.")
    elif typ1 == "losowe":
        wynik = random.choice(["skarb", "pulapka", "nic"])
        if wynik == "skarb":
            zloto = random.randint(20, 50)
            gracz.zloto += zloto
            gracz.mikstury += 1
            print(f"  {opis}")
            print(f"  💰  W środku: {zloto} złota i mikstura leczenia!")
        elif wynik == "pulapka":
            dmg = random.randint(10, 25)
            gracz.hp = max(1, gracz.hp - dmg)
            print(f"  {opis}")
            print(f"  ⚠  Pułapka! Tracisz {dmg} HP.")
        else:
            print(f"  {opis}")
            print("  Skrzynka jest pusta.")
    else:
        print(f"  {opis}")
        # Efekt 1
        if typ1 == "zloto":
            gracz.zloto = max(0, gracz.zloto + wartosc1)
            if wartosc1 < 0:
                print(f"  💸  Wydajesz {abs(wartosc1)} złota.")
            elif wartosc1 > 0:
                print(f"  💰  Zyskujesz {wartosc1} złota.")
        elif typ1 == "mikstura" and wartosc1 < 0:
            if gracz.mikstury > 0:
                gracz.mikstury += wartosc1
                print(f"  🧪  Zużywasz {abs(wartosc1)} miksturę.")
            else:
                print("  🧪  Nie masz mikstury, ale starasz się jak możesz.")
        elif typ1 == "hp" and wartosc1 < 0:
            gracz.hp = max(1, gracz.hp + wartosc1)
            print(f"  ❤  Tracisz {abs(wartosc1)} HP.")
        # Efekt 2
        if typ2 == "hp" and wartosc2 > 0:
            gain = min(wartosc2, gracz.max_hp - gracz.hp)
            gracz.hp += gain
            print(f"  ❤  Odzyskujesz {gain} HP.")
        elif typ2 == "zloto" and wartosc2 > 0:
            gracz.zloto += wartosc2
            print(f"  💰  Zyskujesz {wartosc2} złota.")
        elif typ2 == "mana" and wartosc2 > 0 and gracz.max_mana > 0:
            gain = min(wartosc2, gracz.max_mana - gracz.mana)
            gracz.mana += gain
            print(f"  🔮  Odzyskujesz {gain} many.")

    karma_za = {
        "pomoc": 2,
        "okradnij": -2,
        "mikstura": 2,
        "zostaw": -1,
        "daj": 2,
        "odejdź": -1,
    }
    if klucz in karma_za:
        _zmien_karme(gracz, karma_za[klucz])

    nacisnij_enter()


# ------------------------------------------------------------------ #
#  Testy umiejętności (k20) na mapie                                   #
# ------------------------------------------------------------------ #

def _etykieta_testu(skill: str, st: int) -> str:
    info = SKILLE[skill]
    return f"{info.get('ikona', '🎲')} {info['nazwa']} ST {st}"


def _wybierz_opcje(opcje: list[str]) -> int:
    while True:
        wybor = input("  Twój wybór: ").strip()
        try:
            idx = int(wybor) - 1
            if 0 <= idx < len(opcje):
                return idx
        except ValueError:
            pass
        print("  Nieprawidłowy wybór.")


def _zdarzenie_testu(gracz: Gracz) -> str | None:
    """Losowe wyzwanie z testem k20 — wspinaczka, zamki, rozmowy…"""
    biom = pole_gracza(gracz).get("biom", "")
    pula = [
        z for z in _ZDARZENIA_TESTOW
        if not z.get("biomy") or biom in z["biomy"]
    ]
    zdarzenie = random.choice(pula or _ZDARZENIA_TESTOW)
    return zdarzenie["fn"](gracz)


def _test_wspinaczka(gracz: Gracz) -> str | None:
    st = trudnosc(gracz, 13)
    wyczysc()
    wyswietl_linie("─")
    print("  🧗  ŚCIANA SKALNA")
    wyswietl_linie("─")
    print("\n  Stroma ściana odcina krótszą ścieżkę. Na górze coś błyszczy.\n")
    print(f"  [1]  🧗  Wespnij się  ({_etykieta_testu('atletyka', st)})")
    print("  [2]  🚶  Idź dłuższą drogą (bezpiecznie)")
    print()
    if _wybierz_opcje(["wspinaczka", "omin"]) == 1:
        print("  Omijasz ścianę. Tracisz czas, ale nie skórę.")
        nacisnij_enter()
        return None
    wynik = przeprowadz_test(gracz, "atletyka", st)
    if wynik.sukces:
        zloto = random.randint(18, 40)
        gracz.zloto += zloto
        print(f"  Wbiegasz na półkę. W szczelinie leży sakiewka — {zloto} złota!")
    elif wynik.wpadka:
        dmg = random.randint(14, 24)
        gracz.hp = max(1, gracz.hp - dmg)
        print(f"  Odpadasz od ściany! Tracisz {dmg} HP.")
    else:
        dmg = random.randint(6, 12)
        gracz.hp = max(1, gracz.hp - dmg)
        print(f"  Ześlizgujesz się w pół drogi. Otarcie za {dmg} HP.")
    nacisnij_enter()
    return None


def _test_zamek(gracz: Gracz) -> str | None:
    st = trudnosc(gracz, 14)
    wyczysc()
    wyswietl_linie("─")
    print("  🔐  ZAMKNIĘTA SKRZYNIA")
    wyswietl_linie("─")
    print("\n  Przy ścieżce stoi okuta skrzynia. Zamek wygląda na stary, ale solidny.\n")
    print(f"  [1]  🔑  Wytrych  ({_etykieta_testu('zwinne_palce', st)})")
    print("  [2]  💥  Rozbij siłą  (ryzyko pułapki)")
    print("  [3]  🚶  Zostaw")
    print()
    wybor = _wybierz_opcje(["wytrych", "sila", "zostaw"])
    if wybor == 2:
        print("  Odchodzisz. Skrzynia zostaje.")
        nacisnij_enter()
        return None
    if wybor == 1:
        wynik = przeprowadz_test(gracz, "atletyka", trudnosc(gracz, 12))
        if wynik.sukces:
            zloto = random.randint(10, 22)
            gracz.zloto += zloto
            print(f"  Wieko pęka. W środku {zloto} złota — reszta rozsypana.")
        else:
            dmg = random.randint(8, 16)
            gracz.hp = max(1, gracz.hp - dmg)
            print(f"  Pułapka! Igły wbijają się w dłoń. Tracisz {dmg} HP.")
        nacisnij_enter()
        return None
    wynik = przeprowadz_test(gracz, "zwinne_palce", st)
    if wynik.sukces:
        zloto = random.randint(25, 55)
        gracz.zloto += zloto
        gracz.mikstury += 1
        print(f"  Zamek puszcza bez szelestu. Łup: {zloto} złota i mikstura.")
    elif wynik.wpadka:
        dmg = random.randint(10, 20)
        gracz.hp = max(1, gracz.hp - dmg)
        print(f"  Wytrych pęka i odpala igły. Tracisz {dmg} HP.")
    else:
        print("  Zamek nie ustępuje. Ktoś tu znał się na rzeczy.")
    nacisnij_enter()
    return None


def _test_bandyci(gracz: Gracz) -> str | None:
    st_z = trudnosc(gracz, 14)
    st_p = trudnosc(gracz, 15)
    wyczysc()
    wyswietl_linie("─")
    print("  🗡  BANDYCI NA DRODZE")
    wyswietl_linie("─")
    print("\n  Trzech zbójów zastępuje ci ścieżkę. Jeden uśmiecha się krzywo:")
    print('  „Sakiewka albo krew. Wybieraj szybko."\n')
    print(f"  [1]  😠  Zastrasz  ({_etykieta_testu('zastraszanie', st_z)})")
    print(f"  [2]  💬  Przekonaj  ({_etykieta_testu('perswazja', st_p)})")
    print("  [3]  ⚔  Walcz")
    print("  [4]  💰  Oddaj 15 złota")
    print()
    wybor = _wybierz_opcje(["z", "p", "w", "o"])
    if wybor == 3:
        if gracz.zloto >= 15:
            gracz.zloto -= 15
            print("  Rzuca im sakiewkę. Odchodzą, śmiejąc się.")
        else:
            print("  Nie masz dość złota. Sięgają po bronie!")
            nacisnij_enter()
            wynik = przeprowadz_walke(gracz, pole_gracza(gracz)["biom"])
            return "przegrana" if wynik == "przegrana" else None
        nacisnij_enter()
        return None
    if wybor == 2:
        wynik_w = przeprowadz_walke(gracz, pole_gracza(gracz)["biom"])
        return "przegrana" if wynik_w == "przegrana" else None
    skill = "zastraszanie" if wybor == 0 else "perswazja"
    st = st_z if wybor == 0 else st_p
    wynik = przeprowadz_test(gracz, skill, st)
    if wynik.sukces:
        if skill == "zastraszanie":
            zloto = random.randint(8, 18)
            gracz.zloto += zloto
            print(f"  Zbledli i uciekli, gubiąc {zloto} złota.")
        else:
            print("  Udało ci się ich przekonać, że nie jesteś warci zachodu.")
        nacisnij_enter()
        return None
    print("  Nie kupili tego. Dobywają broni!")
    nacisnij_enter()
    walka = przeprowadz_walke(gracz, pole_gracza(gracz)["biom"])
    return "przegrana" if walka == "przegrana" else None


def _test_akrobatyka(gracz: Gracz) -> str | None:
    st = trudnosc(gracz, 13)
    wyczysc()
    wyswietl_linie("─")
    print("  🪵  KŁODA NAD WĄWOZEM")
    wyswietl_linie("─")
    print("\n  Przerzucona kłoda to jedyna przeprawa. W dole szumią kamienie.\n")
    print(f"  [1]  🤸  Przejdź  ({_etykieta_testu('akrobatyka', st)})")
    print("  [2]  🚶  Szukaj objazdu")
    print()
    if _wybierz_opcje(["idz", "objazd"]) == 1:
        print("  Objeżdżasz wąwóz. Nic się nie dzieje.")
        nacisnij_enter()
        return None
    wynik = przeprowadz_test(gracz, "akrobatyka", st)
    if wynik.sukces:
        zloto = random.randint(12, 28)
        gracz.zloto += zloto
        print(f"  Przechodzisz jak po linie. Po drugiej stronie leży {zloto} złota.")
    else:
        dmg = random.randint(10, 20)
        gracz.hp = max(1, gracz.hp - dmg)
        print(f"  Kłoda się chwieje — spadasz na półkę. Tracisz {dmg} HP.")
    nacisnij_enter()
    return None


def _test_przetrwanie(gracz: Gracz) -> str | None:
    st = trudnosc(gracz, 12)
    wyczysc()
    wyswietl_linie("─")
    print("  🐾  ŚWIEŻY TROP")
    wyswietl_linie("─")
    print("\n  Na ziemi widać odciski i złamane gałęzie. Ktoś — albo coś — szło tędy.\n")
    print(f"  [1]  🐾  Trop  ({_etykieta_testu('przetrwanie', st)})")
    print("  [2]  🚶  Zostaw trop w spokoju")
    print()
    if _wybierz_opcje(["trop", "zostaw"]) == 1:
        print("  Nie ryzykujesz. Idziesz dalej utartą ścieżką.")
        nacisnij_enter()
        return None
    wynik = przeprowadz_test(gracz, "przetrwanie", st)
    if wynik.sukces:
        from game.oboz import dodaj_surowiec, SUROWCE
        klucz = random.choice(["drewno", "ziola", "skora"])
        ile = random.randint(1, 3)
        dodaj_surowiec(gracz, klucz, ile)
        print(f"  Trop prowadzi do obozowiska. +{ile} {SUROWCE[klucz]['nazwa']}.")
        nacisnij_enter()
        return None
    print("  Gubisz trop — i wpadasz na właściciela śladów!")
    nacisnij_enter()
    walka = przeprowadz_walke(gracz, pole_gracza(gracz)["biom"])
    return "przegrana" if walka == "przegrana" else None


def _test_spostrzegawczosc(gracz: Gracz) -> str | None:
    st = trudnosc(gracz, 13)
    wyczysc()
    wyswietl_linie("─")
    print("  👁  COŚ SIĘ NIE ZGADZA")
    wyswietl_linie("─")
    print("\n  Ścieżka wygląda zwyczajnie… zbyt zwyczajnie. Liście ułożone za równo.\n")
    print(f"  [1]  👁  Rozejrzyj się  ({_etykieta_testu('spostrzegawczosc', st)})")
    print("  [2]  🚶  Idź prosto")
    print()
    if _wybierz_opcje(["patrz", "idz"]) == 1:
        dmg = random.randint(8, 16)
        gracz.hp = max(1, gracz.hp - dmg)
        print(f"  Wpadasz w sidła. Tracisz {dmg} HP.")
        nacisnij_enter()
        return None
    wynik = przeprowadz_test(gracz, "spostrzegawczosc", st)
    if wynik.sukces:
        zloto = random.randint(15, 35)
        gracz.zloto += zloto
        print(f"  Dostrzegasz linkę pułapki i omijasz ją. Przy sidłach sakiewka: {zloto} złota.")
    else:
        dmg = random.randint(8, 16)
        gracz.hp = max(1, gracz.hp - dmg)
        print(f"  Za późno — sidła! Tracisz {dmg} HP.")
    nacisnij_enter()
    return None


def _test_oszustwo(gracz: Gracz) -> str | None:
    st = trudnosc(gracz, 14)
    wyczysc()
    wyswietl_linie("─")
    print("  🎲  SZULER PRZY DRODZE")
    wyswietl_linie("─")
    print("\n  Człowiek w wytartym płaszczu proponuje grę w kości. Stawka: 12 złota.\n")
    print(f"  [1]  🃏  Blefuj i oszukaj  ({_etykieta_testu('oszustwo', st)})")
    print("  [2]  🎲  Graj uczciwie (50/50)")
    print("  [3]  🚶  Odmów")
    print()
    wybor = _wybierz_opcje(["blef", "graj", "nie"])
    if wybor == 2:
        print("  Machasz ręką i idziesz dalej.")
        nacisnij_enter()
        return None
    if wybor == 1:
        if gracz.zloto < 12:
            print("  Nie masz stawki. Szuler wzrusza ramionami.")
            nacisnij_enter()
            return None
        if random.random() < 0.5:
            gracz.zloto += 12
            print("  Kości padają na twoją korzyść. +12 złota.")
        else:
            gracz.zloto -= 12
            print("  Przegrywasz 12 złota. Szuler kłania się złośliwie.")
        nacisnij_enter()
        return None
    wynik = przeprowadz_test(gracz, "oszustwo", st)
    if wynik.sukces:
        zysk = random.randint(16, 30)
        gracz.zloto += zysk
        print(f"  Podmieniasz kości. Szuler nie widzi — zgarniasz {zysk} złota.")
    else:
        strata = min(12, gracz.zloto)
        gracz.zloto -= strata
        print(f"  Przyłapany! Oddajesz {strata} złota i znikasz, zanim dobędzie noża.")
    nacisnij_enter()
    return None


_ZDARZENIA_TESTOW = [
    {"fn": _test_wspinaczka, "biomy": ("wzgórza", "kanion", "ruiny")},
    {"fn": _test_zamek, "biomy": ()},
    {"fn": _test_bandyci, "biomy": ("równiny", "ruiny", "las")},
    {"fn": _test_akrobatyka, "biomy": ("wzgórza", "kanion", "bagna")},
    {"fn": _test_przetrwanie, "biomy": ("las", "bagna", "równiny")},
    {"fn": _test_spostrzegawczosc, "biomy": ()},
    {"fn": _test_oszustwo, "biomy": ("równiny", "ruiny")},
]


def _konfrontacja_przed_walka(gracz: Gracz, biom_nazwa: str) -> str | None:
    """Szansa na zastraszenie, perswazję albo unik zamiast od razu walczyć."""
    st_z = trudnosc(gracz, 14)
    st_p = trudnosc(gracz, 15)
    st_a = trudnosc(gracz, 13)
    wyczysc()
    wyswietl_linie()
    print(f"  ⚔️  Ktoś zastępuje ci drogę w biomie: {etykieta_biomu(biom_nazwa)}!")
    print("  Jeszcze nie dobył broni — jest chwila na słowa albo unik.\n")
    print("  [1]  ⚔  Walcz")
    print(f"  [2]  😠  Zastrasz  ({_etykieta_testu('zastraszanie', st_z)})")
    print(f"  [3]  💬  Przekonaj  ({_etykieta_testu('perswazja', st_p)})")
    print(f"  [4]  💨  Uniknij starcia  ({_etykieta_testu('akrobatyka', st_a)})")
    print()
    wybor = _wybierz_opcje(["walka", "z", "p", "u"])
    if wybor == 0:
        wynik = przeprowadz_walke(gracz, biom_nazwa)
        return "przegrana" if wynik == "przegrana" else None
    skill = ("zastraszanie", "perswazja", "akrobatyka")[wybor - 1]
    st = (st_z, st_p, st_a)[wybor - 1]
    wynik = przeprowadz_test(gracz, skill, st)
    if wynik.sukces:
        if skill == "zastraszanie":
            print("  Wróg się waha, odkłada broń i znika między drzewami.")
        elif skill == "perswazja":
            print("  Udaje ci się go przekonać, że nie warto się bić.")
        else:
            print("  Zsuwasz się w bok i znikasz, zanim cios padnie.")
        nacisnij_enter()
        return None
    print("  Słowa i unik zawodzą. Walka!")
    nacisnij_enter()
    walka = przeprowadz_walke(gracz, biom_nazwa)
    return "przegrana" if walka == "przegrana" else None


# ------------------------------------------------------------------ #
#  Ukryte lokacje                                                      #
# ------------------------------------------------------------------ #

def _sprawdz_ukryta_lokacje(gracz: Gracz, wymus: bool = False) -> str | None:
    """Ukryta jaskinia. Zwraca 'przegrana' albo None."""
    if not wymus and random.random() > 0.12:
        return None
    st = trudnosc(gracz, 12)
    wyczysc()
    wyswietl_linie("─")
    print("  🔍  COŚ PRZYKUWA UWAGĘ")
    wyswietl_linie("─")
    print("\n  Krzaki po lewej są gęstsze niż powinny. Może coś kryją?\n")
    print(f"  [1]  👁  Rozejrzyj się  ({_etykieta_testu('spostrzegawczosc', st)})")
    print("  [2]  🚶  Idź dalej")
    print()
    if _wybierz_opcje(["patrz", "idz"]) != 0:
        print("  Mijasz zarośla. Może następnym razem.")
        nacisnij_enter()
        return None
    wynik = przeprowadz_test(gracz, "spostrzegawczosc", st)
    if not wynik.sukces:
        print("  Nic szczególnego. Pewnie zwierzyna przetarła trop.")
        nacisnij_enter()
        return None
    print("  Za gęstymi krzakami kryje się ukryte wejście do jaskini.\n")
    print("  [1]  🚪  Wejdź do środka")
    print("  [2]  🚶  Zignoruj")
    print()
    if _wybierz_opcje(["wejdz", "nie"]) != 0:
        print("  Mijasz ukrytą jaskinię.")
        nacisnij_enter()
        return None

    wynik = random.choices(["skarb", "mikstura", "walka"], weights=[40, 35, 25], k=1)[0]
    if wynik == "skarb":
        zloto = random.randint(30, 70)
        gracz.zloto += zloto
        gracz.mikstury += 1
        print(f"\n  Wewnątrz odkrywasz zapomniane repozytorium!")
        print(f"  💰  {zloto} złota i 1 mikstura — idealne znalezisko.")
    elif wynik == "mikstura":
        gracz.mikstury += 2
        print(f"\n  Na półce skalnej leżą dwie nienaruszone mikstury leczenia.")
        print(f"  🧪  Zdobywasz 2 mikstury.")
    else:
        print(f"\n  Z mroku jaskini wyłania się strażnik!")
        nacisnij_enter()
        wynik_w = przeprowadz_walke(gracz, gracz.aktualny_biom)
        if wynik_w == "przegrana":
            return "przegrana"
    nacisnij_enter()
    return None


def _zakoncz_wyprawe(gracz: Gracz) -> str:
    """Powrót do obozu + zbiory rekrutów."""
    wyczysc()
    wyswietl_linie("═")
    print("  KONIEC WYPRAWY")
    wyswietl_linie("═")
    print(f"  Wracasz do obozu z pola ({gracz.mapa_x}, {gracz.mapa_y}).")
    print("  Twoja pozycja na mapie zostaje zapamiętana.")
    for msg in rozlicz_zbieraczy(gracz):
        print(msg)
    for msg in rozlicz_powrot_do_obozu(gracz):
        print(msg)
    nacisnij_enter()
    return "powrot"


def wyrusz_w_podroz(gracz: Gracz) -> str:
    """Eksploracja trwałej mapy: ruch w 4 strony, zwiad pola, powrót do obozu."""
    zapewnij_mape(gracz)
    gracz.blogoslawienstwo_wyprawy = False
    oznacz_wyjscie(gracz)

    while True:
        wybor = _menu_eksploracji(gracz)

        if wybor == "0":
            return _zakoncz_wyprawe(gracz)

        if wybor in kierunki():
            nazwa, dx, dy = kierunki()[wybor]
            nowy_region = przesun_gracza(gracz, dx, dy)
            dodaj_czas(gracz, 1)
            if nowy_region:
                _pokaz_nowe_srodowisko(gracz)
            _pokaz_wejscie_na_pole(gracz, nazwa)
            wynik = _po_wejsciu_na_pole(gracz)
            if wynik == "przegrana":
                return "przegrana"
            zapisz_gre(gracz)
            continue

        if wybor == "5":
            wynik = _zbadaj_pole(gracz)
            if wynik == "przegrana":
                return "przegrana"
            if wynik == "oboz":
                return _zakoncz_wyprawe(gracz)
            zapisz_gre(gracz)
            continue

        if wybor == "6":
            wynik = zbierz_na_polu(gracz)
            if wynik == "walka":
                print("\n  ⚔️  Przy zbieraniu ktoś cię zaskoczył!")
                nacisnij_enter()
                walka = przeprowadz_walke(gracz, pole_gracza(gracz)["biom"])
                if walka == "przegrana":
                    return "przegrana"
            zapisz_gre(gracz)
            if wynik != "walka":
                nacisnij_enter()
            continue

        print("  Nieprawidłowy wybór.")
        nacisnij_enter()


def _menu_eksploracji(gracz: Gracz) -> str:
    """Rysuje mapę i pyta o ruch albo zwiad."""
    while True:
        wyczysc()
        wyswietl_linie("═")
        print("  🧭  EKSPLORACJA")
        wyswietl_linie("═")
        print()
        rysuj_mape(gracz)

        pole = pole_gracza(gracz)
        for klucz, (nazwa, dx, dy) in kierunki().items():
            cel = etykieta_kierunku(gracz, dx, dy)
            print(f"  [{klucz}]  {ikona_kierunku(nazwa)}  {nazwa:10}  →  {cel}")

        if pole.get("punkt") == "obóz":
            print("  [5]  🏕  Wejdź do obozu")
        elif pole.get("punkt") == "boss":
            print("  [5]  ☠  Wejdź w głąb legowiska")
        elif pole.get("punkt") in PUNKTY_MITYCZNE:
            print(f"  [5]  {ikona_punktu(pole['punkt'])}  Wejdź: {opis_punktu(pole['punkt'])}")
        elif pole.get("punkt") == "miasto":
            print("  [5]  🏙  Wejdź do miasta (osobna mapa)")
        elif pole.get("punkt"):
            budynek = _budynek_z_pola(pole)
            nazwa_b = budynek["nazwa"] if budynek else opis_punktu(pole["punkt"])
            print(f"  [5]  {ikona_punktu(pole['punkt'])}  Wejdź: {nazwa_b}")
        else:
            print("  [5]  👁  Rozglądnij się po okolicy")
        zost = pozostale_zbiory(pole)
        if pole.get("punkt") in ("obóz", "boss", "miasto") or pole.get("punkt") in PUNKTY_MITYCZNE:
            print("  [6]  🧺  Zbierz surowce (tu niedostępne)")
        elif zost > 0:
            print(f"  [6]  🧺  Zbierz surowce  (zostało {zost} na tym polu)")
        else:
            print("  [6]  🧺  Zbierz surowce  (pole wyczerpane)")
        print(f"  {linia_surowcow(gracz)}")
        print("  [0]  🏕  Wróć do obozu (pozycja zostaje)")
        print()
        wybor = input("  Twój wybór: ").strip()
        if wybor in ("0", "5", "6") or wybor in kierunki():
            return wybor
        print("  Nieprawidłowy wybór.")
        nacisnij_enter()


def _po_wejsciu_na_pole(gracz: Gracz) -> str | None:
    """Zasadzka albo boss przy wejściu na pole. Zwraca 'przegrana' albo None."""
    pole = pole_gracza(gracz)
    biom_nazwa = pole["biom"]
    pierwsze = not pole.get("odwiedzone")
    pole["odwiedzone"] = True

    if pole.get("punkt") == "boss":
        wyczysc()
        wyswietl_linie("═")
        print("  ⚠  LEGENDARNY BOSS")
        wyswietl_linie("═")
        print("\n  Wkraczasz w samo serce jego domeny...")
        nacisnij_enter()
        wynik = przeprowadz_walke(gracz, biom_nazwa, jest_boss=True)
        if wynik == "wygrana":
            pole["punkt"] = None
        return "przegrana" if wynik == "przegrana" else None

    if pole.get("punkt") in ("obóz", "karczma", "kuźnia", "świątynia", "miasto") or pole.get("punkt") in PUNKTY_MITYCZNE:
        return None

    szansa = 0.22 if pierwsze else 0.08
    if random.random() < szansa:
        if random.random() < 0.55:
            return _konfrontacja_przed_walka(gracz, biom_nazwa)
        wyczysc()
        wyswietl_linie()
        print(f"  ⚔️  Zasadzka w biomie: {biom_nazwa}!")
        nacisnij_enter()
        wynik = przeprowadz_walke(gracz, biom_nazwa)
        if wynik == "przegrana":
            return "przegrana"
    return None


def _zbadaj_pole(gracz: Gracz) -> str | None:
    """Interakcja ze stałym punktem albo losowe zdarzenie na pustym polu."""
    pole = pole_gracza(gracz)
    biom_nazwa = pole["biom"]
    biom = _szablon_biomu(biom_nazwa)

    if pole.get("punkt") == "obóz":
        print("\n  Wchodzisz między namioty. Palenisko wciąż się tli.")
        nacisnij_enter()
        return "oboz"

    if pole.get("punkt") == "boss":
        return _po_wejsciu_na_pole(gracz)

    if pole.get("punkt") in PUNKTY_MITYCZNE:
        return zdarzenie_mityczne(gracz, pole)

    if pole.get("punkt") == "miasto":
        wejdz_do_miasta(gracz)
        return None

    budynek = _budynek_z_pola(pole)
    if budynek:
        return _budynek(gracz, biom, budynek)

    typ = random.choices(
        ["lokacja", "narracja", "test", "walka", "ukryta"],
        weights=[32, 16, 28, 14, 10],
        k=1,
    )[0]
    if typ == "lokacja":
        _losowa_lokacja(gracz, biom)
        return None
    if typ == "narracja":
        _zdarzenie_narracyjne(gracz)
        return None
    if typ == "test":
        return _zdarzenie_testu(gracz)
    if typ == "ukryta":
        return _sprawdz_ukryta_lokacje(gracz, wymus=True)

    if random.random() < 0.65:
        return _konfrontacja_przed_walka(gracz, biom_nazwa)
    wyczysc()
    wyswietl_linie()
    print(f"  ⚔️  W biomie {biom_nazwa} ktoś zastępuje ci drogę!")
    nacisnij_enter()
    wynik = przeprowadz_walke(gracz, biom_nazwa)
    return "przegrana" if wynik == "przegrana" else None
