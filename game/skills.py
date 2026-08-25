"""Definicje umiejętności i podklas postaci."""

# Słownik umiejętności: klucz -> metadane
# klasa:    klasa główna (Wojownik / Mag / Lotrzyk)
# podklasa: None = skill klasy głównej, inaczej nazwa podklasy
# poziom:   poziom odblokowania
# koszt_many: 0 jeśli umiejętność nie używa many
UMIEJETNOSCI: dict[str, dict] = {

    # ------------------------------------------------------------------ #
    #  WOJOWNIK – klasa główna                                            #
    # ------------------------------------------------------------------ #
    "potezny_cios": {
        "nazwa": "Potężny cios",
        "opis": "2× obrażenia, ale obrona = 0 przy odwecie wroga",
        "klasa": "Wojownik",
        "podklasa": None,
        "poziom": 1,
        "koszt_many": 0,
        "ikona": "⚔",
    },
    "tarcza_wiary": {
        "nazwa": "Tarcza wiary",
        "opis": "Obrona ×2 przez jedną turę wroga",
        "klasa": "Wojownik",
        "podklasa": None,
        "poziom": 3,
        "koszt_many": 0,
        "ikona": "🛡",
    },
    "okrzyk_bojowy": {
        "nazwa": "Okrzyk bojowy",
        "opis": "+30% do ataku przez 2 tury",
        "klasa": "Wojownik",
        "podklasa": None,
        "poziom": 5,
        "koszt_many": 0,
        "ikona": "📣",
    },
    "szal_berserka": {
        "nazwa": "Szał berserka",
        "opis": "+50% do ataku przez 3 tury, ale HP nie może być leczone",
        "klasa": "Wojownik",
        "podklasa": None,
        "poziom": 8,
        "koszt_many": 0,
        "ikona": "🔥",
    },

    # Wojownik → Paladyn
    "boskie_swiatlo": {
        "nazwa": "Boskie światło",
        "opis": "Leczy 50 HP (działa nawet podczas szału berserka)",
        "klasa": "Wojownik",
        "podklasa": "Paladyn",
        "poziom": 6,
        "koszt_many": 0,
        "ikona": "✨",
    },
    "swiety_cios": {
        "nazwa": "Święty cios",
        "opis": "2.5× obrażenia + 20 obrażeń świętych",
        "klasa": "Wojownik",
        "podklasa": "Paladyn",
        "poziom": 9,
        "koszt_many": 0,
        "ikona": "🌟",
    },

    # Wojownik → Barbarzyńca
    "wscieklosc": {
        "nazwa": "Wściekłość",
        "opis": "+80% do ataku przez 4 tury, ale obrona -50%",
        "klasa": "Wojownik",
        "podklasa": "Barbarzynca",
        "poziom": 6,
        "koszt_many": 0,
        "ikona": "😡",
    },
    "niszczace_uderzenie": {
        "nazwa": "Niszczące uderzenie",
        "opis": "Zadaje 30% obecnego HP wroga jako obrażenia",
        "klasa": "Wojownik",
        "podklasa": "Barbarzynca",
        "poziom": 9,
        "koszt_many": 0,
        "ikona": "💥",
    },

    # ------------------------------------------------------------------ #
    #  MAG – klasa główna                                                  #
    # ------------------------------------------------------------------ #
    "kula_ognia": {
        "nazwa": "Kula ognia",
        "opis": "35–55 obrażeń magicznych (ignoruje obronę wroga)",
        "klasa": "Mag",
        "podklasa": None,
        "poziom": 1,
        "koszt_many": 10,
        "ikona": "🔥",
    },
    "lodowe_wiezy": {
        "nazwa": "Lodowe więzy",
        "opis": "Wróg traci następną turę (ogłuszenie)",
        "klasa": "Mag",
        "podklasa": None,
        "poziom": 3,
        "koszt_many": 15,
        "ikona": "❄",
    },
    "tarcza_runowa": {
        "nazwa": "Tarcza runowa",
        "opis": "Absorbuje do 40 obrażeń w następnej turze wroga",
        "klasa": "Mag",
        "podklasa": None,
        "poziom": 5,
        "koszt_many": 15,
        "ikona": "🔮",
    },
    "meteor": {
        "nazwa": "Meteor",
        "opis": "80–120 obrażeń magicznych (ignoruje obronę wroga)",
        "klasa": "Mag",
        "podklasa": None,
        "poziom": 8,
        "koszt_many": 25,
        "ikona": "☄",
    },

    # Mag → Mroczny mag
    "mroczna_strzala": {
        "nazwa": "Mroczna strzała",
        "opis": "45–70 obrażeń mrocznych (ignoruje obronę wroga)",
        "klasa": "Mag",
        "podklasa": "MrocznyMag",
        "poziom": 6,
        "koszt_many": 20,
        "ikona": "🌑",
    },
    "klatwa_mroku": {
        "nazwa": "Klątwa mroku",
        "opis": "Wróg zadaje 50% mniej obrażeń i traci 15 HP/turę przez 3 tury",
        "klasa": "Mag",
        "podklasa": "MrocznyMag",
        "poziom": 9,
        "koszt_many": 25,
        "ikona": "🌑",
    },

    # Mag → Arcymag
    "przyspieszenie_magiczne": {
        "nazwa": "Przyspieszenie magiczne",
        "opis": "Następny czar jest darmowy i zadaje podwójne obrażenia",
        "klasa": "Mag",
        "podklasa": "Arcymag",
        "poziom": 6,
        "koszt_many": 10,
        "ikona": "⚡",
    },
    "kula_pioruna": {
        "nazwa": "Kula pioruna",
        "opis": "100–150 obrażeń magicznych (ignoruje obronę wroga)",
        "klasa": "Mag",
        "podklasa": "Arcymag",
        "poziom": 9,
        "koszt_many": 30,
        "ikona": "⚡",
    },

    # ------------------------------------------------------------------ #
    #  ŁOTRZYK – klasa główna                                             #
    # ------------------------------------------------------------------ #
    "cios_w_plecy": {
        "nazwa": "Cios w plecy",
        "opis": "40% szans na 2× obrażenia (gwarantowany w 1. turze walki)",
        "klasa": "Lotrzyk",
        "podklasa": None,
        "poziom": 1,
        "koszt_many": 0,
        "ikona": "🗡",
    },
    "trucizna": {
        "nazwa": "Trucizna",
        "opis": "Wróg traci 10 HP na turę przez 3 tury",
        "klasa": "Lotrzyk",
        "podklasa": None,
        "poziom": 3,
        "koszt_many": 0,
        "ikona": "☠",
    },
    "dymna_bomba": {
        "nazwa": "Dymna bomba",
        "opis": "Natychmiastowa ucieczka (100% skuteczność)",
        "klasa": "Lotrzyk",
        "podklasa": None,
        "poziom": 5,
        "koszt_many": 0,
        "ikona": "💨",
    },
    "smiertelne_uderzenie": {
        "nazwa": "Śmiertelne uderzenie",
        "opis": "3× obrażenia jeśli HP wroga < 25%, inaczej normalny atak",
        "klasa": "Lotrzyk",
        "podklasa": None,
        "poziom": 8,
        "koszt_many": 0,
        "ikona": "☠",
    },

    # Łotrzyk → Zabójca
    "cien_smierci": {
        "nazwa": "Cień śmierci",
        "opis": "60% szans na 4× obrażenia (krytyczne trafienie)",
        "klasa": "Lotrzyk",
        "podklasa": "Zabojca",
        "poziom": 6,
        "koszt_many": 0,
        "ikona": "🌑",
    },
    "egzekucja": {
        "nazwa": "Egzekucja",
        "opis": "Natychmiast zabija wroga jeśli ma < 15% HP",
        "klasa": "Lotrzyk",
        "podklasa": "Zabojca",
        "poziom": 9,
        "koszt_many": 0,
        "ikona": "💀",
    },

    # Łotrzyk → Zwiadowca
    "unik": {
        "nazwa": "Unik",
        "opis": "75% szans na uniknięcie następnego ataku wroga",
        "klasa": "Lotrzyk",
        "podklasa": "Zwiadowca",
        "poziom": 6,
        "koszt_many": 0,
        "ikona": "💨",
    },
    "grad_strzal": {
        "nazwa": "Grad strzał",
        "opis": "Wykonuje 3 szybkie ataki, każdy za normalne obrażenia",
        "klasa": "Lotrzyk",
        "podklasa": "Zwiadowca",
        "poziom": 9,
        "koszt_many": 0,
        "ikona": "🏹",
    },

    # ------------------------------------------------------------------ #
    #  DRUID – klasa główna                                               #
    # ------------------------------------------------------------------ #
    "splot_korzeni": {
        "nazwa": "Splot korzeni",
        "opis": "Unieruchamia wroga — pomija następną turę",
        "klasa": "Druid",
        "podklasa": None,
        "poziom": 1,
        "koszt_many": 10,
        "ikona": "🌿",
    },
    "forma_niedzwiedzia": {
        "nazwa": "Forma niedźwiedzia",
        "opis": "Przemiana: obrona i atak w górę, regeneracja HP (kilka tur)",
        "klasa": "Druid",
        "podklasa": None,
        "poziom": 1,
        "koszt_many": 12,
        "ikona": "🐻",
    },
    "uzdrowienie": {
        "nazwa": "Uzdrowienie",
        "opis": "Leczy 50 HP",
        "klasa": "Druid",
        "podklasa": None,
        "poziom": 3,
        "koszt_many": 15,
        "ikona": "💚",
    },
    "burza_natury": {
        "nazwa": "Burza natury",
        "opis": "40–60 obrażeń żywiołowych (ignoruje obronę wroga)",
        "klasa": "Druid",
        "podklasa": None,
        "poziom": 5,
        "koszt_many": 20,
        "ikona": "⛈",
    },
    "forma_wilka": {
        "nazwa": "Forma wilka",
        "opis": "Przemiana: duży atak i krytyki, słabsza obrona",
        "klasa": "Druid",
        "podklasa": None,
        "poziom": 5,
        "koszt_many": 15,
        "ikona": "🐺",
    },
    "regeneracja": {
        "nazwa": "Regeneracja",
        "opis": "Gracz leczy się o 15 HP na turę przez 4 tury",
        "klasa": "Druid",
        "podklasa": None,
        "poziom": 8,
        "koszt_many": 20,
        "ikona": "🌱",
    },
    "forma_kruka": {
        "nazwa": "Forma kruka",
        "opis": "Przemiana: wysoka szansa na unik ataków wroga",
        "klasa": "Druid",
        "podklasa": None,
        "poziom": 8,
        "koszt_many": 15,
        "ikona": "🐦",
    },

    # Druid → Szaman
    "totem_zycia": {
        "nazwa": "Totem życia",
        "opis": "Leczy 30 HP na turę przez 3 tury",
        "klasa": "Druid",
        "podklasa": "Szaman",
        "poziom": 6,
        "koszt_many": 20,
        "ikona": "🔺",
    },
    "forma_ducha": {
        "nazwa": "Forma ducha",
        "opis": "Przemiana: unik i regeneracja many co turę",
        "klasa": "Druid",
        "podklasa": "Szaman",
        "poziom": 6,
        "koszt_many": 18,
        "ikona": "👻",
    },
    "piorun_szamana": {
        "nazwa": "Piorun szamana",
        "opis": "70–100 obrażeń błyskawicznych + 50% szans na ogłuszenie",
        "klasa": "Druid",
        "podklasa": "Szaman",
        "poziom": 9,
        "koszt_many": 30,
        "ikona": "⚡",
    },

    # Druid → Strażnik Lasu
    "kolce_natury": {
        "nazwa": "Kolce natury",
        "opis": "Wróg traci 20 HP na turę przez 4 tury (trucizna roślinna)",
        "klasa": "Druid",
        "podklasa": "StraznikLasu",
        "poziom": 6,
        "koszt_many": 15,
        "ikona": "🌵",
    },
    "gniew_puszczy": {
        "nazwa": "Gniew puszczy",
        "opis": "50–80 obrażeń × liczba aktywnych efektów na wrogu (min. 1×)",
        "klasa": "Druid",
        "podklasa": "StraznikLasu",
        "poziom": 9,
        "koszt_many": 25,
        "ikona": "🌲",
    },

    # ------------------------------------------------------------------ #
    #  NEKROMANTA – klasa główna                                          #
    # ------------------------------------------------------------------ #
    "wysysanie_zycia": {
        "nazwa": "Wysysanie życia",
        "opis": "30–50 obrażeń mrocznych, gracz leczy się o tę samą ilość",
        "klasa": "Nekromanta",
        "podklasa": None,
        "poziom": 1,
        "koszt_many": 15,
        "ikona": "🩸",
    },
    "przywolaj_szkielet": {
        "nazwa": "Przywołaj szkielet",
        "opis": "Przywołuje sługę — atakuje i może przejąć cios wroga",
        "klasa": "Nekromanta",
        "podklasa": None,
        "poziom": 1,
        "koszt_many": 12,
        "ikona": "💀",
    },
    "klatwa_smierci": {
        "nazwa": "Klątwa śmierci",
        "opis": "Wróg zadaje 50% mniej obrażeń przez 3 tury",
        "klasa": "Nekromanta",
        "podklasa": None,
        "poziom": 3,
        "koszt_many": 15,
        "ikona": "💀",
    },
    "rozpad": {
        "nazwa": "Rozpad",
        "opis": "Zmniejsza maksymalne HP wroga o 20% (trwałe na czas walki)",
        "klasa": "Nekromanta",
        "podklasa": None,
        "poziom": 5,
        "koszt_many": 20,
        "ikona": "🦴",
    },
    "przywolaj_ghul": {
        "nazwa": "Przywołaj ghula",
        "opis": "Wytrzymały sługa — więcej HP, chętniej przejmuje ciosy",
        "klasa": "Nekromanta",
        "podklasa": None,
        "poziom": 5,
        "koszt_many": 20,
        "ikona": "🧟",
    },
    "dotyk_smierci": {
        "nazwa": "Dotyk śmierci",
        "opis": "Wróg traci 25 HP na turę przez 3 tury (trucizna nekrotyczna)",
        "klasa": "Nekromanta",
        "podklasa": None,
        "poziom": 8,
        "koszt_many": 20,
        "ikona": "☠",
    },

    # Nekromanta → Lich
    "fala_smierci": {
        "nazwa": "Fala śmierci",
        "opis": "60–90 obrażeń mrocznych (ignoruje obronę), gracz leczy się o 30%",
        "klasa": "Nekromanta",
        "podklasa": "Lich",
        "poziom": 6,
        "koszt_many": 25,
        "ikona": "💀",
    },
    "przywolaj_widmo": {
        "nazwa": "Przywołaj widmo",
        "opis": "Sługa z Otchłani — atak ignoruje połowę obrony wroga",
        "klasa": "Nekromanta",
        "podklasa": "Lich",
        "poziom": 6,
        "koszt_many": 22,
        "ikona": "👻",
    },
    "wiecznie_zywi": {
        "nazwa": "Wiecznie żywi",
        "opis": "Aktywuje ochronę — jeśli gracz miałby umrzeć, leczy go o 40 HP (raz na walkę)",
        "klasa": "Nekromanta",
        "podklasa": "Lich",
        "poziom": 9,
        "koszt_many": 30,
        "ikona": "💀",
    },

    # Nekromanta → Kapłan Mroku
    "pakt_krwi": {
        "nazwa": "Pakt krwi",
        "opis": "Gracz traci 20 HP, ale następny atak zadaje 3× obrażenia",
        "klasa": "Nekromanta",
        "podklasa": "KaplanMroku",
        "poziom": 6,
        "koszt_many": 10,
        "ikona": "🗡",
    },
    "krwawy_sluga": {
        "nazwa": "Krwawy sługa",
        "opis": "Poświęcasz HP, by przywołać silnego sługę",
        "klasa": "Nekromanta",
        "podklasa": "KaplanMroku",
        "poziom": 6,
        "koszt_many": 12,
        "ikona": "🩸",
    },
    "ofiarny_rytual": {
        "nazwa": "Ofiarny rytuał",
        "opis": "Gracz traci 30% HP, wróg traci tę samą wartość × 3",
        "klasa": "Nekromanta",
        "podklasa": "KaplanMroku",
        "poziom": 9,
        "koszt_many": 15,
        "ikona": "🩸",
    },
}

# Podklasy dostępne dla każdej klasy głównej
PODKLASY: dict[str, list[dict]] = {
    "Wojownik": [
        {
            "klucz": "Paladyn",
            "nazwa": "Paladyn",
            "opis": "Wojownik wspierany boską mocą — leczy i zadaje święte obrażenia.",
        },
        {
            "klucz": "Barbarzynca",
            "nazwa": "Barbarzyńca",
            "opis": "Dziki wojownik — maksymalne obrażenia kosztem obrony.",
        },
    ],
    "Mag": [
        {
            "klucz": "MrocznyMag",
            "nazwa": "Mroczny mag",
            "opis": "Mag ciemności — mroczne strzały i klątwy łączące obrażenia z osłabieniem.",
        },
        {
            "klucz": "Arcymag",
            "nazwa": "Arcymag",
            "opis": "Mistrz magii — skupia się na niszczycielskich zaklęciach.",
        },
    ],
    "Lotrzyk": [
        {
            "klucz": "Zabojca",
            "nazwa": "Zabójca",
            "opis": "Precyzyjny morderca — krytyczne uderzenia i egzekucje.",
        },
        {
            "klucz": "Zwiadowca",
            "nazwa": "Zwiadowca",
            "opis": "Zwinny łowca — uniki i grad strzał.",
        },
    ],
    "Druid": [
        {
            "klucz": "Szaman",
            "nazwa": "Szaman",
            "opis": "Uzdrowiciel duchów — totemy leczące i pioruny ogłuszające.",
        },
        {
            "klucz": "StraznikLasu",
            "nazwa": "Strażnik Lasu",
            "opis": "Władca przyrody — trwałe trucizny roślinne i gniew natury.",
        },
    ],
    "Nekromanta": [
        {
            "klucz": "Lich",
            "nazwa": "Lich",
            "opis": "Nieśmiertelny czarnoksiężnik — fale śmierci i ochrona przed zagładą.",
        },
        {
            "klucz": "KaplanMroku",
            "nazwa": "Kapłan Mroku",
            "opis": "Rytualistyczny morderca — poświęca własne HP dla potężnych ataków.",
        },
    ],
}

MAX_RANGA = 5

# Cooldown w turach gracza (0 = bez CD). Stuny i silne skille mają CD,
# żeby nie dało się co turę zamrażać wroga.
_CD = {
    "potezny_cios": 2,
    "tarcza_wiary": 2,
    "okrzyk_bojowy": 3,
    "szal_berserka": 4,
    "boskie_swiatlo": 2,
    "swiety_cios": 2,
    "wscieklosc": 4,
    "niszczace_uderzenie": 3,
    "kula_ognia": 0,
    "lodowe_wiezy": 2,
    "tarcza_runowa": 2,
    "meteor": 2,
    "mroczna_strzala": 2,
    "klatwa_mroku": 3,
    "przyspieszenie_magiczne": 3,
    "kula_pioruna": 2,
    "cios_w_plecy": 0,
    "trucizna": 2,
    "dymna_bomba": 99,
    "smiertelne_uderzenie": 2,
    "cien_smierci": 2,
    "egzekucja": 2,
    "unik": 2,
    "grad_strzal": 2,
    "splot_korzeni": 2,
    "forma_niedzwiedzia": 3,
    "uzdrowienie": 2,
    "burza_natury": 2,
    "forma_wilka": 3,
    "regeneracja": 3,
    "forma_kruka": 3,
    "totem_zycia": 3,
    "forma_ducha": 3,
    "piorun_szamana": 2,
    "kolce_natury": 2,
    "gniew_puszczy": 2,
    "wysysanie_zycia": 0,
    "przywolaj_szkielet": 2,
    "klatwa_smierci": 2,
    "rozpad": 3,
    "przywolaj_ghul": 3,
    "dotyk_smierci": 2,
    "fala_smierci": 2,
    "przywolaj_widmo": 3,
    "wiecznie_zywi": 4,
    "pakt_krwi": 2,
    "krwawy_sluga": 3,
    "ofiarny_rytual": 3,
}

for _klucz, _cd in _CD.items():
    if _klucz in UMIEJETNOSCI:
        UMIEJETNOSCI[_klucz]["cd"] = _cd


def ranga_skilla(gracz, klucz: str) -> int:
    """Aktualna ranga umiejętności (1–5)."""
    rangi = getattr(gracz, "rangi_umiejetnosci", {}) or {}
    return max(1, min(MAX_RANGA, int(rangi.get(klucz, 1))))


def cd_skilla(klucz: str) -> int:
    return int(UMIEJETNOSCI.get(klucz, {}).get("cd", 0))


def skala_mocy(gracz, klucz: str) -> float:
    """Mnożnik mocy: poziom postaci + ranga skilla."""
    ranga = ranga_skilla(gracz, klucz)
    return (1.0 + 0.05 * (gracz.poziom - 1)) * (1.0 + 0.12 * (ranga - 1))


def skaluj_wartosc(gracz, klucz: str, baza: int) -> int:
    """Skaluje liczbę (obrażenia, leczenie, tarcza)."""
    return max(1, int(baza * skala_mocy(gracz, klucz)))


def czas_trwania(gracz, klucz: str, baza: int) -> int:
    """Czas trwania buffa/DoT: +1 tura co 2 rangi."""
    return max(1, baza + (ranga_skilla(gracz, klucz) - 1) // 2)


def kwalifikuje_sie(gracz, info: dict) -> bool:
    """Czy skill należy do klasy/podklasy gracza."""
    if info["klasa"] != gracz.klasa:
        return False
    if info["podklasa"] is not None and info["podklasa"] != gracz.podklasa:
        return False
    return True


def nastepne_umiejetnosci(gracz, ile: int = 2) -> list[dict]:
    """Najbliższe jeszcze zablokowane skille."""
    kandydaci = []
    odblokowane = set(gracz.umiejetnosci)
    for klucz, info in UMIEJETNOSCI.items():
        if klucz in odblokowane or not kwalifikuje_sie(gracz, info):
            continue
        if info["poziom"] > gracz.poziom:
            kandydaci.append({"klucz": klucz, **info})
    kandydaci.sort(key=lambda i: i["poziom"])
    return kandydaci[:ile]


def ulepsz_umiejetnosc(gracz, klucz: str) -> str:
    """Wydaje 1 punkt umiejętności na +1 rangę. Zwraca komunikat."""
    if klucz not in gracz.umiejetnosci:
        return "  Nie znasz tej umiejętności."
    rangi = getattr(gracz, "rangi_umiejetnosci", None)
    if rangi is None:
        gracz.rangi_umiejetnosci = {k: 1 for k in gracz.umiejetnosci}
        rangi = gracz.rangi_umiejetnosci
    aktualna = ranga_skilla(gracz, klucz)
    if aktualna >= MAX_RANGA:
        return f"  {UMIEJETNOSCI[klucz]['nazwa']} jest już na maksymalnej randze ({MAX_RANGA})."
    if getattr(gracz, "punkty_umiejetnosci", 0) <= 0:
        return "  Brak punktów umiejętności."
    gracz.punkty_umiejetnosci -= 1
    rangi[klucz] = aktualna + 1
    return (
        f"  {UMIEJETNOSCI[klucz]['ikona']}  {UMIEJETNOSCI[klucz]['nazwa']}"
        f"  ranga {aktualna} → {aktualna + 1}!"
    )


def otworz_ksiege_umiejetnosci(gracz) -> None:
    """Obozowa księga: podgląd, rangi i ulepszenia."""
    from game.utils import wyswietl_linie, nacisnij_enter, wyczysc

    while True:
        wyczysc()
        wyswietl_linie("═")
        print("  📖  KSIĘGA UMIEJĘTNOŚCI")
        wyswietl_linie("═")
        pkt = getattr(gracz, "punkty_umiejetnosci", 0)
        print(f"\n  Punkty do rozdania: {pkt}   (maks. ranga {MAX_RANGA})\n")

        if not gracz.umiejetnosci:
            print("  Nie znasz jeszcze żadnej umiejętności.")
        else:
            print("  ─── Znane ───")
            for i, klucz in enumerate(gracz.umiejetnosci, 1):
                info = UMIEJETNOSCI[klucz]
                ranga = ranga_skilla(gracz, klucz)
                cd = cd_skilla(klucz)
                moc = skala_mocy(gracz, klucz)
                cd_str = f"  CD {cd}" if cd else ""
                mana_str = f"  {info['koszt_many']} many" if info["koszt_many"] else ""
                print(
                    f"  [{i}] {info['ikona']} {info['nazwa']}  "
                    f"r.{ranga}/{MAX_RANGA}  moc ×{moc:.2f}{mana_str}{cd_str}"
                )
                print(f"       {info['opis']}")

        nadchodzace = nastepne_umiejetnosci(gracz)
        if nadchodzace:
            print("\n  ─── Wkrótce ───")
            for info in nadchodzace:
                print(
                    f"  · poz. {info['poziom']}: {info['ikona']} {info['nazwa']}"
                    f"  — {info['opis']}"
                )

        print("\n  Wpisz numer, aby ulepszyć (1 punkt = +1 ranga).")
        print("  [0] Wróć\n")
        wybor = input("  Twój wybór: ").strip()
        if wybor == "0":
            return
        try:
            idx = int(wybor) - 1
            if 0 <= idx < len(gracz.umiejetnosci):
                print(ulepsz_umiejetnosc(gracz, gracz.umiejetnosci[idx]))
                nacisnij_enter()
                continue
        except ValueError:
            pass
        print("  Nieprawidłowy wybór.")
        nacisnij_enter()
