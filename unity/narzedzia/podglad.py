#!/usr/bin/env python3
"""Zamienia klatki podglądu z portu na C# w gotowe obrazki PNG.

Warstwa Grafika portu rysuje klatkę 1280×720 w czystym C# i zapisuje surowe
bajty RGBA; tekst w grze dokłada Unity, więc tutaj dorysowujemy go pygame'em
według listy napisów. Dzięki temu da się zobaczyć, jak gra wygląda, nie mając
pod ręką edytora Unity.

Uruchomienie (z katalogu głównego repozytorium):

    cd unity/Testy && dotnet run -- --podglad ../podglad && cd ../..
    python unity/narzedzia/podglad.py unity/podglad
"""
from __future__ import annotations

import os
import sys

try:
    import pygame
except ImportError:
    print("Potrzebny pygame-ce: pip install pygame-ce")
    raise SystemExit(1)

# Kroje o stałej szerokości, w kolejności prób — tak samo jak w silniku Unity.
KROJE = ["consolas", "dejavusansmono", "liberationmono", "couriernew", "freemono"]
KROJE_EMOJI = ["seguiemj", "notocoloremoji", "notoemoji", "symbola", "opensymbol"]
ROZMIARY = {"maly": 14, "konsola": 16, "hud": 17, "duzy": 24}


_pamiec: dict = {}


def wczytaj_czcionke(nazwy: list[str], rozmiar: int, pogrubiona: bool = False):
    klucz = (tuple(nazwy), rozmiar, pogrubiona)
    if klucz in _pamiec:
        return _pamiec[klucz]
    wynik = None
    for nazwa in nazwy:
        sciezka = pygame.font.match_font(nazwa, bold=pogrubiona)
        if sciezka:
            wynik = pygame.font.Font(sciezka, rozmiar)
            break
    if wynik is None:
        wynik = pygame.font.Font(None, rozmiar + 4)
    _pamiec[klucz] = wynik
    return wynik


def _czy_emoji(ch: str) -> bool:
    """Ta sama reguła co w oknie pygame: ramki i bloki zostają w kroju mono."""
    o = ord(ch)
    return (o >= 0x2300 and not 0x2500 <= o <= 0x25FF) or o in (0xFE0F, 0x200D)


def rysuj_tekst(cel, tekst: str, x: int, y: int, rgb, rozmiar: int, pogrubiona: bool) -> None:
    """Tekst krojem mono, emoji krojem z ikonami — inaczej byłyby prostokąty."""
    mono = wczytaj_czcionke(KROJE, rozmiar, pogrubiona)
    emoji = wczytaj_czcionke(KROJE_EMOJI, max(10, rozmiar - 2))
    odcinki, biezacy, tryb = [], "", None
    for ch in tekst:
        e = _czy_emoji(ch)
        if tryb is not None and e != tryb:
            odcinki.append((biezacy, tryb))
            biezacy = ""
        biezacy += ch
        tryb = e
    if biezacy:
        odcinki.append((biezacy, tryb))
    for kawalek, jest_emoji in odcinki:
        if jest_emoji:
            kawalek = kawalek.replace("\ufe0f", "")
            if not kawalek:
                continue
            obraz = emoji.render(kawalek, True, rgb)
            # Kolorowe kroje emoji (CBDT) mają bitmapy o stałym rozmiarze
            # i ignorują rozmiar podany przy wczytaniu — trzeba je zmniejszyć
            # samemu, inaczej jedna ikona zasłoniłaby pół ekranu.
            docelowa = mono.get_linesize()
            if obraz.get_height() > docelowa:
                skala = docelowa / obraz.get_height()
                obraz = pygame.transform.smoothscale(
                    obraz, (max(1, int(obraz.get_width() * skala)), docelowa))
            cel.blit(obraz, (x, y + (mono.get_linesize() - obraz.get_height()) // 2))
        else:
            obraz = mono.render(kawalek, True, rgb)
            cel.blit(obraz, (x, y))
        x += obraz.get_width()


def zloz(katalog: str, nazwa: str) -> str:
    """Składa jeden podgląd: surowe piksele + napisy → PNG."""
    raw = os.path.join(katalog, nazwa + ".raw")
    opis = os.path.join(katalog, nazwa + ".napisy")
    with open(opis, encoding="utf-8") as f:
        wiersze = f.read().splitlines()
    szer, wys = (int(v) for v in wiersze[0].split())

    with open(raw, "rb") as f:
        dane = f.read()
    oczekiwane = szer * wys * 4
    if len(dane) != oczekiwane:
        raise SystemExit(f"{raw}: {len(dane)} bajtów, oczekiwano {oczekiwane}")

    powierzchnia = pygame.image.frombuffer(dane, (szer, wys), "RGBA").convert_alpha()

    for wiersz in wiersze[1:]:
        if not wiersz.strip():
            continue
        czesci = wiersz.split("\t")
        if len(czesci) < 5:
            continue
        x, y, krój, kolor, tekst = czesci[0], czesci[1], czesci[2], czesci[3], czesci[4]
        if not tekst:
            continue
        rgb = tuple(int(v) for v in kolor.split(","))
        rysuj_tekst(powierzchnia, tekst, int(x), int(y), rgb,
                    ROZMIARY.get(krój, 17), pogrubiona=(krój == "duzy"))

    png = os.path.join(katalog, nazwa + ".png")
    pygame.image.save(powierzchnia, png)
    return png


def main(argumenty: list[str]) -> int:
    katalog = argumenty[0] if argumenty else "unity/podglad"
    if not os.path.isdir(katalog):
        print(f"Nie ma katalogu {katalog}. Najpierw: cd unity/Testy && "
              f"dotnet run -- --podglad ../podglad")
        return 1
    os.environ.setdefault("SDL_VIDEODRIVER", "dummy")
    pygame.init()
    pygame.display.set_mode((1, 1))
    nazwy = sorted(p[:-4] for p in os.listdir(katalog) if p.endswith(".raw"))
    if not nazwy:
        print(f"Brak plików .raw w {katalog}.")
        return 1
    for nazwa in nazwy:
        print(f"  {zloz(katalog, nazwa)}")
    pygame.quit()
    return 0


if __name__ == "__main__":
    raise SystemExit(main(sys.argv[1:]))
