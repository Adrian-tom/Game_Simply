"""Moduł zawierający definicje przeciwników."""

import random


class Przeciwnik:
    """Klasa bazowa dla wszystkich przeciwników."""

    def __init__(
        self,
        nazwa: str,
        hp: int,
        atak: int,
        obrona: int,
        exp_nagroda: int,
        zloto_nagroda: tuple[int, int],
        opis: str = "",
    ) -> None:
        self.nazwa = nazwa
        self.max_hp = hp
        self.hp = hp
        self.atak = atak
        self.obrona = obrona
        self.exp_nagroda = exp_nagroda
        self._zloto_min, self._zloto_max = zloto_nagroda
        self.opis = opis
        self.poziom = 1
        # Zdolności wynikają z nazwy (patrz ZDOLNOSCI) — dzięki temu ręcznie
        # tworzony Przeciwnik("Troll", ...) w teście zachowuje się jak z fabryki.
        zdolnosci = zdolnosci_wroga(nazwa)
        self.zapowiedzi: dict[str, float] = dict(zdolnosci["zapowiedzi"])
        self.slabosci: set[str] = set(zdolnosci["slabosci"])
        self.odpornosci: set[str] = set(zdolnosci["odpornosci"])
        self.regeneracja: float = float(zdolnosci["regeneracja"])
        self.szal_uzyty = False

    def zyje(self) -> bool:
        return self.hp > 0

    def losowe_zloto(self) -> int:
        return random.randint(self._zloto_min, self._zloto_max)

    def pasek_hp(self, szerokosc: int = 20) -> str:
        wypelniony = int((self.hp / self.max_hp) * szerokosc)
        return "[" + "█" * wypelniony + "░" * (szerokosc - wypelniony) + "]"

    def __str__(self) -> str:
        from game.ikony import wrog
        return (
            f"{wrog(self.nazwa)} {self.nazwa}  ❤️ {self.hp}/{self.max_hp} {self.pasek_hp()}"
        )


# ------------------------------------------------------------------ #
#  Fabryka losowych przeciwników                                       #
# ------------------------------------------------------------------ #

_SZABLONY = [
    dict(
        nazwa="Goblin",
        hp=40,
        atak=8,
        obrona=2,
        exp_nagroda=30,
        zloto_nagroda=(5, 15),
        opis="Mały, zielony i wredny.",
    ),
    dict(
        nazwa="Szkielet",
        hp=55,
        atak=11,
        obrona=4,
        exp_nagroda=45,
        zloto_nagroda=(8, 18),
        opis="Ożywione kości dawnego wojownika.",
    ),
    dict(
        nazwa="Ork",
        hp=80,
        atak=16,
        obrona=6,
        exp_nagroda=70,
        zloto_nagroda=(15, 30),
        opis="Potężny, zielonoskóry wojownik.",
    ),
    dict(
        nazwa="Trolle",
        hp=100,
        atak=20,
        obrona=8,
        exp_nagroda=100,
        zloto_nagroda=(20, 40),
        opis="Ogromne stworzenie regenerujące zdrowie.",
    ),
    dict(
        nazwa="Wiedźma",
        hp=60,
        atak=22,
        obrona=3,
        exp_nagroda=90,
        zloto_nagroda=(25, 45),
        opis="Czarownica rzucająca mroczne zaklęcia.",
    ),
    dict(
        nazwa="Młody smok",
        hp=130,
        atak=28,
        obrona=12,
        exp_nagroda=150,
        zloto_nagroda=(40, 70),
        opis="Skrzydlata bestia ziejąca ogniem.",
    ),
]


_SZABLONY_BIOM = {
    "równiny": [
        dict(
            nazwa="Hiena stepowa",
            hp=52,
            atak=12,
            obrona=3,
            exp_nagroda=42,
            zloto_nagroda=(8, 18),
            opis="Drapieżnik czający się w wysokiej trawie.",
        ),
    ],
    "ruiny": [
        dict(
            nazwa="Strażnik ruin",
            hp=72,
            atak=15,
            obrona=7,
            exp_nagroda=68,
            zloto_nagroda=(14, 28),
            opis="Dawny obrońca, który nie zaznał spokoju po śmierci.",
        ),
    ],
    "las": [
        dict(
            nazwa="Wilk cienia",
            hp=60,
            atak=14,
            obrona=4,
            exp_nagroda=55,
            zloto_nagroda=(10, 20),
            opis="Bezgłośny drapieżnik stapiający się z cieniem drzew.",
        ),
    ],
    "bagna": [
        dict(
            nazwa="Topielec",
            hp=68,
            atak=15,
            obrona=5,
            exp_nagroda=62,
            zloto_nagroda=(12, 24),
            opis="Zgniła istota wynurzająca się z bagiennej toni.",
        ),
    ],
    "wzgórza": [
        dict(
            nazwa="Harpii zwiadowca",
            hp=66,
            atak=17,
            obrona=4,
            exp_nagroda=70,
            zloto_nagroda=(14, 26),
            opis="Skrzydlata bestia krążąca nad skalistymi grzbietami.",
        ),
    ],
    "kanion": [
        dict(
            nazwa="Skalny skorpion",
            hp=78,
            atak=18,
            obrona=6,
            exp_nagroda=78,
            zloto_nagroda=(16, 32),
            opis="Pancerny drapieżnik polujący między rozgrzanymi skałami.",
        ),
    ],
}


_BOSSOWIE = [
    dict(
        nazwa="Smok Cienia",
        hp=230,
        atak=30,
        obrona=12,
        exp_nagroda=500,
        zloto_nagroda=(80, 150),
        opis="Starożytny smok opatulony mrokiem, władca tych ziem.",
    ),
    dict(
        nazwa="Licz Prawieczny",
        hp=200,
        atak=28,
        obrona=10,
        exp_nagroda=480,
        zloto_nagroda=(70, 130),
        opis="Nieumarły czarownik gromadzący dusze poległych przez wieki.",
    ),
    dict(
        nazwa="Król Trolli",
        hp=260,
        atak=26,
        obrona=13,
        exp_nagroda=520,
        zloto_nagroda=(90, 160),
        opis="Potworny władca trolli, którego ryk rozrysa góry.",
    ),
    dict(
        nazwa="Arcydemon Khaor",
        hp=210,
        atak=33,
        obrona=11,
        exp_nagroda=560,
        zloto_nagroda=(100, 180),
        opis="Demon przyzwany z głębin otchłani, żądny zniszczenia.",
    ),
    dict(
        nazwa="Strażniczka Wieczności",
        hp=240,
        atak=29,
        obrona=12,
        exp_nagroda=540,
        zloto_nagroda=(85, 165),
        opis="Pradawna istota pilnująca przejścia między światami.",
    ),
]


_MITYCZNI = {
    "portal": dict(
        nazwa="Strażnik Otchłani",
        hp=210,
        atak=30,
        obrona=11,
        exp_nagroda=420,
        zloto_nagroda=(70, 120),
        opis="Istota ze szczeliny między światami. Powietrze wokół niej pęka.",
    ),
    "leze_smoka": dict(
        nazwa="Stary smok Ashkaryx",
        hp=270,
        atak=33,
        obrona=14,
        exp_nagroda=580,
        zloto_nagroda=(110, 190),
        opis="Pradawny smok w swoim leżu. Skarbiec lśni pod jego brzuchem.",
    ),
    "latajaca_wyspa": dict(
        nazwa="Gryf Niebios",
        hp=220,
        atak=31,
        obrona=12,
        exp_nagroda=460,
        zloto_nagroda=(80, 140),
        opis="Skrzydlaty strażnik unoszącej się wyspy. Wiatr tnie jak ostrza.",
    ),
}


# ------------------------------------------------------------------ #
#  Zdolności: zapowiadane ataki, słabości, odporności                  #
# ------------------------------------------------------------------ #

# Klucz = fragment nazwy (małe litery). "zapowiedzi" to szansa na turę, że
# wróg zapowie dany ruch — wykona go w NASTĘPNEJ turze, więc gracz ma czas
# odpowiedzieć gardą, miksturą, przerwaniem albo ucieczką.
ZDOLNOSCI: dict[str, dict] = {
    "goblin": {"slabosci": ("ogien",)},
    "szkielet": {"odpornosci": ("trucizna",), "slabosci": ("swiete",)},
    "ork": {"zapowiedzi": {"ciezki_cios": 0.25}},
    "troll": {"zapowiedzi": {"ciezki_cios": 0.2}, "regeneracja": 0.05, "slabosci": ("ogien",)},
    "wiedźma": {"zapowiedzi": {"klatwa": 0.35}, "slabosci": ("swiete",)},
    "smok": {"zapowiedzi": {"ogien": 0.34}, "odpornosci": ("ogien",)},
    "strażnik ruin": {"zapowiedzi": {"ciezki_cios": 0.25}, "odpornosci": ("trucizna",), "slabosci": ("swiete",)},
    "topielec": {"slabosci": ("ogien",)},
    "skorpion": {"zapowiedzi": {"klatwa": 0.2}},
    "harpii": {"zapowiedzi": {"ciezki_cios": 0.2}},
    "licz": {"zapowiedzi": {"klatwa": 0.3, "ciezki_cios": 0.15}, "odpornosci": ("trucizna",), "slabosci": ("swiete",)},
    "król trolli": {"zapowiedzi": {"ciezki_cios": 0.3}, "regeneracja": 0.04, "slabosci": ("ogien",)},
    "arcydemon": {"zapowiedzi": {"ogien": 0.25, "ciezki_cios": 0.2}, "odpornosci": ("ogien",), "slabosci": ("swiete",)},
    "strażniczka": {"zapowiedzi": {"ciezki_cios": 0.3}, "odpornosci": ("trucizna",)},
    "otchłani": {"zapowiedzi": {"klatwa": 0.25, "ciezki_cios": 0.2}, "slabosci": ("swiete",)},
    "gryf": {"zapowiedzi": {"ciezki_cios": 0.35}},
    "herszt": {"zapowiedzi": {"ciezki_cios": 0.3}},
}

NAZWY_TYPOW = {"ogien": "ogień", "swiete": "święte", "trucizna": "trucizna"}


def zdolnosci_wroga(nazwa: str) -> dict:
    """Łączy wpisy pasujące do nazwy — 'Król Trolli' dostaje też cechy trolla."""
    nazwa = nazwa.lower()
    wynik: dict = {"zapowiedzi": {}, "slabosci": set(), "odpornosci": set(), "regeneracja": 0.0}
    for klucz, wpis in ZDOLNOSCI.items():
        if klucz not in nazwa:
            continue
        wynik["zapowiedzi"].update(wpis.get("zapowiedzi", {}))
        wynik["slabosci"].update(wpis.get("slabosci", ()))
        wynik["odpornosci"].update(wpis.get("odpornosci", ()))
        wynik["regeneracja"] = max(wynik["regeneracja"], wpis.get("regeneracja", 0.0))
    wynik["slabosci"] -= wynik["odpornosci"]
    return wynik


def mnoznik_typu(przeciwnik: "Przeciwnik", typ: str) -> float:
    """Słabość ×1.5, odporność ×0.5, reszta ×1."""
    if typ in getattr(przeciwnik, "slabosci", ()):
        return 1.5
    if typ in getattr(przeciwnik, "odpornosci", ()):
        return 0.5
    return 1.0


# ------------------------------------------------------------------ #
#  Poziomy: trudność zależy od REGIONU, nie od gracza                  #
# ------------------------------------------------------------------ #

# Region N ma wrogów na poziomach 2N-1..2N. Gracz, który awansuje, naprawdę
# staje się silniejszy od okolicy — a dalekie regiony są groźne, dopóki
# się do nich nie dorośnie. Wcześniej wrogowie rośli z poziomem gracza
# i awans nic nie zmieniał (szansa wygranej ~70% na każdym poziomie).
WZROST_NA_POZIOM = 0.13
WZROST_BOSSA_NA_POZIOM = 0.10


def poziom_wroga(mapa_gen: int) -> int:
    return max(1, 2 * int(mapa_gen) - 1 + random.randint(0, 1))


def poziom_bossa(mapa_gen: int) -> int:
    return max(3, 2 * int(mapa_gen) + 1)


def _mnoznik_trudnosci(tryb: str) -> float:
    """Mnożnik statystyk wrogów zależny od trybu trudności."""
    if tryb == "hardcore":
        return 1.2
    if tryb == "latwy":
        return 0.85
    return 1.0


# Mnożniki bazy (strojone symulacją — patrz ANALIZA.md, „Balans walki”).
BAZA_ZWYKLYCH = {"hp": 1.2, "atak": 1.3, "obrona": 1.1}
BAZA_BOSSOW = {"hp": 0.65, "atak": 0.9, "obrona": 0.7}


def _z_szablonu(szablon: dict, poziom: int, skala: float, baza: dict | None = None) -> Przeciwnik:
    baza = baza or BAZA_ZWYKLYCH
    wrog = Przeciwnik(
        nazwa=szablon["nazwa"],
        hp=int(szablon["hp"] * skala * baza["hp"]),
        atak=int(szablon["atak"] * skala * baza["atak"]),
        obrona=int(szablon["obrona"] * skala * baza["obrona"]),
        exp_nagroda=int(szablon["exp_nagroda"] * (1 + (poziom - 1) * 0.15)),
        zloto_nagroda=(
            int(szablon["zloto_nagroda"][0] * skala),
            int(szablon["zloto_nagroda"][1] * skala),
        ),
        opis=szablon["opis"],
    )
    wrog.poziom = poziom
    return wrog


def losuj_bossa(
    poziom_gracza: int = 1, mapa_gen: int = 1, tryb: str = "normalny"
) -> Przeciwnik:
    """Boss regionu: poziom 2N+1 — wyzwanie, do którego trzeba się przygotować."""
    szablon = random.choice(_BOSSOWIE)
    poziom = poziom_bossa(mapa_gen)
    skala = (1 + (poziom - 3) * WZROST_BOSSA_NA_POZIOM) * _mnoznik_trudnosci(tryb)
    return _z_szablonu(szablon, poziom, skala, BAZA_BOSSOW)


def losuj_mitycznego(
    typ: str, poziom_gracza: int = 1, mapa_gen: int = 1, tryb: str = "normalny"
) -> Przeciwnik:
    """Unikalny wróg mitycznej lokacji — poziom bossa +1."""
    szablon = _MITYCZNI.get(typ, _MITYCZNI["portal"])
    poziom = poziom_bossa(mapa_gen) + 1
    skala = (1 + (poziom - 3) * WZROST_BOSSA_NA_POZIOM) * _mnoznik_trudnosci(tryb)
    return _z_szablonu(szablon, poziom, skala, BAZA_BOSSOW)


def losuj_przeciwnika(
    poziom_gracza: int = 1,
    biom: str | None = None,
    mapa_gen: int = 1,
    tryb: str = "normalny",
    poziom: int | None = None,
) -> Przeciwnik:
    """
    Losuje przeciwnika dla regionu. ``poziom_gracza`` zostaje w sygnaturze
    dla zgodności, ale nie wpływa na siłę — decyduje poziom regionu.
    """
    poziom = poziom if poziom is not None else poziom_wroga(mapa_gen)
    # Groźniejsze gatunki dochodzą z poziomem: troll od 3., wiedźma od 4., smok od 6.
    ile = 2 + (1 if poziom >= 2 else 0) + (1 if poziom >= 3 else 0) + (1 if poziom >= 4 else 0) + (1 if poziom >= 6 else 0)
    dostepne = _SZABLONY[: min(len(_SZABLONY), ile)]
    if biom in _SZABLONY_BIOM:
        dostepne = dostepne + _SZABLONY_BIOM[biom]
    szablon = random.choice(dostepne)
    skala = (1 + (poziom - 1) * WZROST_NA_POZIOM) * _mnoznik_trudnosci(tryb)
    return _z_szablonu(szablon, poziom, skala)


def herszt_najazdu(sila: int, tryb: str = "normalny") -> Przeciwnik:
    """Przywódca bandy napadającej na osadę — rośnie razem z siłą najazdu."""
    poziom = max(1, min(14, sila // 25))
    szablon = dict(
        nazwa="Herszt bandy",
        hp=95,
        atak=17,
        obrona=6,
        exp_nagroda=90,
        zloto_nagroda=(20, 45),
        opis="Przywódca najazdu. Blizny liczy jak trofea.",
    )
    skala = (1 + (poziom - 1) * WZROST_NA_POZIOM) * _mnoznik_trudnosci(tryb)
    return _z_szablonu(szablon, poziom, skala)
