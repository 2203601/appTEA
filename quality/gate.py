#!/usr/bin/env python3
"""Quality gate del pipeline. Lee los reportes de quality/out y falla (exit 1) si no se cumplen los umbrales.

Umbrales (consigna del TP, nivel declarado):
  - todas las pruebas pasan
  - cobertura de líneas de la lógica de los juegos >= MIN_COBERTURA
  - duplicación de código de los juegos <= MAX_DUPLICACION
  - 0 bugs / vulnerabilidades Blocker o Critical en el análisis estático
"""
import glob
import json
import os
import sys
import xml.etree.ElementTree as ET

QUALITY = os.path.dirname(os.path.abspath(__file__))
OUT = os.path.join(QUALITY, "out")

MIN_COBERTURA = float(os.environ.get("MIN_COBERTURA", "80"))
MAX_DUPLICACION = float(os.environ.get("MAX_DUPLICACION", "5"))
SEVERIDADES_BLOQUEANTES = ("Blocker", "Critical")
TIPOS_BLOQUEANTES = ("Bug", "Vulnerability")


def pruebas(carpeta="tests"):
    archivos = glob.glob(os.path.join(OUT, carpeta, "**", "*.trx"), recursive=True)
    if not archivos:
        return None
    ns = {"t": "http://microsoft.com/schemas/VisualStudio/TeamTest/2010"}
    contadores = ET.parse(archivos[0]).getroot().find(".//t:ResultSummary/t:Counters", ns)
    return {k: int(contadores.get(k, 0)) for k in ("total", "executed", "passed", "failed")}


def cobertura():
    archivos = glob.glob(os.path.join(OUT, "tests", "**", "coverage.cobertura.xml"), recursive=True)
    if not archivos:
        return None
    raiz = ET.parse(archivos[0]).getroot()
    return {
        "lineas": float(raiz.get("line-rate")) * 100,
        "ramas": float(raiz.get("branch-rate")) * 100,
    }


def analisis_estatico():
    ruta = os.path.join(OUT, "analisis.sarif")
    if not os.path.exists(ruta):
        return None
    run = json.load(open(ruta, encoding="utf-8"))["runs"][0]
    reglas = run.get("rules", {})
    total, bloqueantes, por_tipo = 0, [], {}
    for resultado in run["results"]:
        regla = reglas.get(resultado["ruleId"], {})
        categoria = regla.get("properties", {}).get("category", "")   # ej. "Major Code Smell"
        partes = categoria.split(" ", 1)
        severidad, tipo = (partes[0], partes[1]) if len(partes) == 2 else ("", categoria)
        if resultado["level"] == "note":
            continue
        total += 1
        por_tipo[tipo or "otro"] = por_tipo.get(tipo or "otro", 0) + 1
        if resultado["level"] == "error" or (severidad in SEVERIDADES_BLOQUEANTES and tipo in TIPOS_BLOQUEANTES):
            bloqueantes.append(resultado["ruleId"])
    return {"total": total, "por_tipo": por_tipo, "bloqueantes": bloqueantes}


def duplicacion():
    ruta = os.path.join(OUT, "jscpd", "jscpd-report.json")
    if not os.path.exists(ruta):
        return None
    total = json.load(open(ruta, encoding="utf-8"))["statistics"]["total"]
    return {"porcentaje": total["percentage"], "clones": total["clones"], "lineas": total["lines"]}


def fila(nombre, valor, umbral, ok):
    return f"| {'✅' if ok else '❌'} | {nombre} | {valor} | {umbral} |"


def main():
    filas, ok_total = [], True

    sm = pruebas("smoke")
    ok = sm is not None and sm["failed"] == 0 and sm["executed"] > 0
    filas.append(fila("Smoke test automático", "sin resultados" if sm is None else f"{sm['passed']}/{sm['total']} pasan, {sm['failed']} fallan", "0 fallos", ok))
    ok_total &= ok

    t = pruebas()
    ok = t is not None and t["failed"] == 0 and t["executed"] > 0
    filas.append(fila("Pruebas", "sin resultados" if t is None else f"{t['passed']}/{t['total']} pasan, {t['failed']} fallan", "0 fallos", ok))
    ok_total &= ok

    c = cobertura()
    ok = c is not None and c["lineas"] >= MIN_COBERTURA
    filas.append(fila("Cobertura de líneas (lógica de juegos)", "sin datos" if c is None else f"{c['lineas']:.1f} % (ramas {c['ramas']:.1f} %)", f">= {MIN_COBERTURA:.0f} %", ok))
    ok_total &= ok

    d = duplicacion()
    ok = d is not None and d["porcentaje"] <= MAX_DUPLICACION
    filas.append(fila("Duplicación de código (módulo de juegos)", "sin datos" if d is None else f"{d['porcentaje']:.2f} % ({d['clones']} clones)", f"<= {MAX_DUPLICACION:.0f} %", ok))
    ok_total &= ok

    a = analisis_estatico()
    ok = a is not None and not a["bloqueantes"]
    detalle = "sin datos" if a is None else f"{a['total']} hallazgos {a['por_tipo']}; bloqueantes: {len(a['bloqueantes'])}"
    filas.append(fila("Análisis estático (bugs Blocker/Critical)", detalle, "0 bloqueantes", ok))
    ok_total &= ok

    resumen = ["## Quality gate: " + ("✅ APROBADO" if ok_total else "❌ RECHAZADO"), "",
               "| | Chequeo | Resultado | Umbral |", "|---|---|---|---|", *filas, ""]
    texto = "\n".join(resumen)
    print(texto)
    open(os.path.join(OUT, "resumen.md"), "w", encoding="utf-8").write(texto)
    destino = os.environ.get("GITHUB_STEP_SUMMARY")
    if destino:
        open(destino, "a", encoding="utf-8").write(texto + "\n")
    return 0 if ok_total else 1


if __name__ == "__main__":
    sys.exit(main())
