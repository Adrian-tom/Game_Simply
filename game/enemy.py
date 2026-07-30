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

    def zyje(self) -> bool:
        return self.hp > 0

    def losowe_zloto(self) -> int:
        return random.randint(self._zloto_min, self._zloto_max)

    def pasek_hp(self, szerokosc: int = 20) -> str:
        wypelniony = int((self.hp / self.max_hp) * szerokosc)
        return "[" + "█" * wypelniony + "░" * (szerokosc - wypelniony) + "]"

    def __str__(self) -> str:
        return (
            f"{self.nazwa}  HP: {self.hp}/{self.max_hp} {self.pasek_hp()}"
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


def losuj_przeciwnika(poziom_gracza: int = 1, biom: str | None = None) -> Przeciwnik:
    """
    Losuje przeciwnika odpowiedniego dla poziomu gracza,
    skalując jego statystyki.
    """
    dostepne = _SZABLONY[: min(poziom_gracza + 2, len(_SZABLONY))]
    if biom in _SZABLONY_BIOM:
        dostepne += _SZABLONY_BIOM[biom]
    szablon = random.choice(dostepne)
    skala = 1 + (poziom_gracza - 1) * 0.15

    return Przeciwnik(
        nazwa=szablon["nazwa"],
        hp=int(szablon["hp"] * skala),
        atak=int(szablon["atak"] * skala),
        obrona=int(szablon["obrona"] * skala),
        exp_nagroda=int(szablon["exp_nagroda"] * skala),
        zloto_nagroda=(
            int(szablon["zloto_nagroda"][0] * skala),
            int(szablon["zloto_nagroda"][1] * skala),
        ),
        opis=szablon["opis"],
    )
