"""Ikony UI — biomy, punkty, kierunki, menu. Jedno źródło prawdy."""

IKONY_BIOM: dict[str, str] = {
    "równiny": "🌾",
    "ruiny": "🏚",
    "las": "🌲",
    "bagna": "🐸",
    "wzgórza": "⛰",
    "kanion": "🏜",
}

IKONY_PUNKT: dict[str, str] = {
    "obóz": "🏕",
    "karczma": "🍺",
    "kuźnia": "⚒",
    "świątynia": "🛕",
    "jaskinia": "🕳",
    "boss": "☠",
    "portal": "🌀",
    "leze_smoka": "🐉",
    "latajaca_wyspa": "☁",
    "miasto": "🏙",
}

IKONY_KIERUNEK: dict[str, str] = {
    "północ": "⬆",
    "zachód": "⬅",
    "wschód": "➡",
    "południe": "⬇",
}

IKONY_WROG: dict[str, str] = {
    "Goblin": "👺",
    "Szkielet": "💀",
    "Ork": "🟢",
    "Trolle": "👹",
    "Wiedźma": "🧙",
    "Młody smok": "🐲",
    "Hiena stepowa": "🐺",
    "Strażnik ruin": "🗿",
    "Wilk cienia": "🐺",
    "Topielec": "💧",
    "Harpii zwiadowca": "🦅",
    "Skalny skorpion": "🦂",
    "Smok Cienia": "🐉",
    "Licz Prawieczny": "☠",
    "Król Trolli": "👑",
    "Arcydemon Khaor": "😈",
    "Strażniczka Wieczności": "⏳",
    "Strażnik Otchłani": "🌀",
    "Stary smok Ashkaryx": "🔥",
    "Gryf Niebios": "🦅",
}

GRACZ_MAPA = "👤"
MGŁA = "❔"


def biom(nazwa: str | None) -> str:
    return IKONY_BIOM.get(nazwa or "", "🌍")


def punkt(klucz: str | None) -> str:
    if not klucz:
        return ""
    return IKONY_PUNKT.get(klucz, "📍")


def kierunek(nazwa: str) -> str:
    return IKONY_KIERUNEK.get(nazwa, "•")


def wrog(nazwa: str) -> str:
    return IKONY_WROG.get(nazwa, "👾")


def etykieta_biomu(nazwa: str | None) -> str:
    if not nazwa:
        return ""
    return f"{biom(nazwa)} {nazwa}"


def etykieta_punktu(klucz: str | None, nazwa: str | None = None) -> str:
    if not klucz:
        return ""
    return f"{punkt(klucz)} {nazwa or klucz}"


def glif_pola(biom_nazwa: str, punkt_klucz: str | None, *, ty: bool, odkryte: bool) -> str:
    if ty:
        return GRACZ_MAPA
    if not odkryte:
        return MGŁA
    if punkt_klucz:
        return punkt(punkt_klucz)
    return biom(biom_nazwa)
