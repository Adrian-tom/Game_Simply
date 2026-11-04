"""Pobiera modele natury CC0 z Poly Haven do Assets/_Wypiek (katalog poza gitem).

Modele są za ciężkie do gry: drzewa mają miliony trójkątów. Edytor wypieka je do obrazków
(GraSimply.Edytor.WypiekNatury.Zrob), a potem cały ten katalog można skasować.
Uruchomienie: python unity/narzedzia/pobierz_nature.py
"""
import concurrent.futures
import json
import os
import urllib.request

MODELE = [
    "island_tree_01", "island_tree_02", "tree_small_02", "fir_sapling_medium",
    "shrub_01", "shrub_03", "shrub_04", "fern_02",
    "rock_moss_set_01", "rock_moss_set_02", "boulder_01", "rock_07", "stone_01",
    "grass_medium_01", "dandelion_01", "nettle_plant",
]
CEL = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "GraSimply", "Assets", "_Wypiek")
UA = {"User-Agent": "GraSimply/1.0"}


def pobierz(url, sciezka):
    if os.path.exists(sciezka):
        return 0
    os.makedirs(os.path.dirname(sciezka), exist_ok=True)
    with urllib.request.urlopen(urllib.request.Request(url, headers=UA), timeout=600) as r:
        dane = r.read()
    with open(sciezka, "wb") as f:
        f.write(dane)
    return len(dane)


def model(nazwa):
    with urllib.request.urlopen(urllib.request.Request(f"https://api.polyhaven.com/files/{nazwa}", headers=UA), timeout=60) as r:
        pliki = json.load(r)
    fbx = pliki["fbx"]["1k"]["fbx"]
    zadania = [(fbx["url"], os.path.join(CEL, nazwa, nazwa + ".fbx"))]
    for wzgledna, opis in fbx.get("include", {}).items():
        nazwa_pliku = wzgledna.split("/")[-1]
        # Do wypieku starczy kolor, przezroczystość i mapa nierówności.
        if any(k in nazwa_pliku for k in ("_diff_", "_alpha_", "_nor_gl_")):
            zadania.append((opis["url"], os.path.join(CEL, nazwa, "textures", nazwa_pliku)))
    razem = sum(pobierz(u, s) for u, s in zadania)
    return f"{nazwa}: {len(zadania)} plików, {razem // 1048576} MB"


with concurrent.futures.ThreadPoolExecutor(4) as pula:
    for wynik in pula.map(model, MODELE):
        print(wynik, flush=True)
