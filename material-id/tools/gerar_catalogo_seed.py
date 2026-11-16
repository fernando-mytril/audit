"""Gera o arquivo de carga inicial do catálogo (E2-01) a partir das tabelas de referência.

Fontes:
  * Sampaio — https://sampaio-sa.com.br/produtos/{tubos,perfis,bobinas-e-chapas} (HTML salvo localmente)
  * Aço Cearense — tabela de tubo retangular (imagem), transcrita abaixo.

Uso: python3 gerar_catalogo_seed.py <pasta-com-htmls-da-sampaio> <arquivo-saida.json>
Linhas cujo peso publicado não fecha com a própria tabela são marcadas como "requerRevisao".
"""
import html, json, re, sys

ACABAMENTO_POR_PREFIXO = {"FF": "FinaFrio", "ZC": "Galvanizado", "FQ": "FinaQuente", "LF": "FinaFrio", "GA": "Galvanizado", "LQ": "FinaQuente"}
# Na Sampaio, a espessura define o acabamento (cabeçalho FF/ZC/FQ da tabela de tubos).
ACABAMENTO_POR_ESPESSURA = {0.60: "FinaFrio", 0.65: "Galvanizado", 0.75: "FinaFrio", 0.80: "Galvanizado", 0.90: "FinaFrio",
    0.95: "Galvanizado", 1.06: "FinaFrio", 1.11: "Galvanizado", 1.20: "FinaFrio", 1.25: "Galvanizado", 1.50: "FinaFrio",
    1.55: "Galvanizado", 1.90: "FinaFrio", 1.95: "Galvanizado", 2.00: "FinaQuente", 2.25: "FinaQuente", 2.65: "FinaQuente", 3.00: "FinaQuente"}

def num(s):
    s = s.strip()
    return None if s in ("", "-", "—") else float(s.replace(".", "").replace(",", "."))

def tabelas(path):
    s = open(path, encoding="utf-8", errors="ignore").read()
    out = []
    for t in re.findall(r"<table.*?</table>", s, flags=re.S):
        rows = []
        for tr in re.findall(r"<tr.*?</tr>", t, flags=re.S):
            cells = [re.sub(r"\s+", " ", html.unescape(re.sub(r"<[^>]+>", " ", c))).strip()
                     for c in re.findall(r"<t[dh][^>]*>(.*?)</t[dh]>", tr, flags=re.S)]
            rows.append(cells)
        out.append(rows)
    return out

itens = []
def item(**kw):
    kw.setdefault("comprimentoPadraoMm", None); kw.setdefault("pecasPorAmarrado", None)
    kw.setdefault("requerRevisao", False); kw.setdefault("observacao", None)
    itens.append(kw)

def tubos_sampaio(pasta):
    t = tabelas(f"{pasta}/tubos.html")
    esp16 = [0.60, 0.65, 0.75, 0.80, 0.90, 0.95, 1.06, 1.11, 1.20, 1.25, 1.50, 1.55, 1.90, 1.95, 2.00, 2.25]
    # Tabelas 1 (redondo) e 3 (retangular) têm o mesmo layout: 16 espessuras, 2º "peças por fardo", 2,65 e 3,00.
    for idx, familia in ((0, "TuboRedondo"), (2, "TuboRetangular")):
        for r in t[idx][2:]:
            bitola, medida, pcs = r[0], r[1], int(num(r[2]))
            valores = r[3:]
            pares = list(zip(esp16, valores[:16])) + [(2.65, valores[17] if len(valores) > 17 else "-"), (3.00, valores[18] if len(valores) > 18 else "-")]
            pcs2 = num(valores[16]) if len(valores) > 16 else None
            for esp, v in pares:
                kg = num(v)
                if kg is None: continue
                base = dict(fabricante="Sampaio", familia=familia, espessuraMm=esp, acabamento=ACABAMENTO_POR_ESPESSURA[esp],
                            tipoPeso="KgPorPeca", valorPeso=kg, comprimentoPadraoMm=6000,
                            pecasPorAmarrado=int(pcs2) if esp >= 2.65 and pcs2 else pcs, bitolaPol=bitola)
                if familia == "TuboRedondo":
                    item(**base, diametroMm=num(medida), larguraMm=None, alturaMm=None, abaMm=None)
                else:
                    b, h = [num(x) for x in medida.lower().split("x")]
                    item(**base, diametroMm=None, larguraMm=b, alturaMm=h, abaMm=None)
    # Tabela 2: tubo quadrado (17 espessuras, sem 0,65).
    esp17 = [0.60, 0.75, 0.80, 0.90, 0.95, 1.06, 1.11, 1.20, 1.25, 1.50, 1.55, 1.90, 1.95, 2.00, 2.25, 2.65, 3.00]
    for r in t[1][1:]:
        lado = num(r[1].lower().split("x")[0]); pcs = int(num(r[2]))
        for esp, v in zip(esp17, r[3:]):
            kg = num(v)
            if kg is None: continue
            obs = None; rev = False
            if kg > 100:  # "164" publicado para 15x15 0,60 — provável 1,64
                rev, obs = True, f"Valor publicado '{v}' incoerente com a série; provável {kg/100:.2f} kg/peça. Confirmar com o fabricante."
            item(fabricante="Sampaio", familia="TuboQuadrado", espessuraMm=esp, acabamento=ACABAMENTO_POR_ESPESSURA[esp],
                 tipoPeso="KgPorPeca", valorPeso=kg, comprimentoPadraoMm=6000, pecasPorAmarrado=pcs, bitolaPol=r[0].replace("*", "\""),
                 diametroMm=None, larguraMm=lado, alturaMm=lado, abaMm=None, requerRevisao=rev, observacao=obs)
    # Tabela 4: tubo oblongo/retangular 16x30.
    esp16b = [0.60, 0.75, 0.80, 0.90, 0.95, 1.06, 1.11, 1.20, 1.25, 1.50, 1.55, 1.90, 1.95, 2.00, 2.25]
    for r in t[3][1:]:
        a, b = [num(x) for x in r[1].lower().split("x")]; pcs = int(num(r[2]))
        for esp, v in zip(esp16b, r[3:]):
            kg = num(v)
            if kg is None: continue
            item(fabricante="Sampaio", familia="TuboRetangular", espessuraMm=esp, acabamento=ACABAMENTO_POR_ESPESSURA[esp],
                 tipoPeso="KgPorPeca", valorPeso=kg, comprimentoPadraoMm=6000, pecasPorAmarrado=pcs, bitolaPol=r[0].replace("*", "\""),
                 diametroMm=None, larguraMm=max(a, b), alturaMm=min(a, b), abaMm=None)

def perfis_sampaio(pasta):
    t = tabelas(f"{pasta}/perfis.html")
    for idx, familia in ((0, "PerfilUEnrijecido"), (1, "PerfilUSimples")):
        esps = [num(x) for x in t[idx][0][2:]]
        for r in t[idx][1:]:
            d = [num(x) for x in r[0].lower().split("x")]
            if familia == "PerfilUEnrijecido":   # C x A x B  (enrijecedor x aba x alma)
                aba_enr, aba, alma = d
            else:                                 # A x B x A  (aba x alma x aba)
                aba, alma, aba_enr = d[0], d[1], None
            for esp, v in zip(esps, r[2:]):
                item(fabricante="Sampaio", familia=familia, espessuraMm=esp, acabamento="FinaQuente", tipoPeso="KgPorPeca",
                     valorPeso=num(v), comprimentoPadraoMm=6000, pecasPorAmarrado=int(num(r[1])), bitolaPol=None,
                     diametroMm=None, larguraMm=alma, alturaMm=aba, abaMm=aba_enr,
                     observacao="Peso publicado por barra; comprimento de 6000 mm presumido — confirmar com o fabricante.")

def chapas_sampaio(pasta):
    t = tabelas(f"{pasta}/bobinas-e-chapas.html")
    # 1: laminada a quente; 2: laminada a frio; 3: galvanizada; 4: complementar de LQ (duplicada, ignorada); 5: galvalume.
    for idx, acab in ((0, "FinaQuente"), (1, "FinaFrio"), (2, "Galvanizado"), (4, "Galvalume")):
        for r in t[idx][1:]:
            item(fabricante="Sampaio", familia="Chapa", espessuraMm=num(r[1]), acabamento=acab, tipoPeso="KgPorMetroQuadrado",
                 valorPeso=num(r[2]), bitolaPol=r[0], diametroMm=None, larguraMm=None, alturaMm=None, abaMm=None)

# Aço Cearense — tubo retangular: base, altura, espessura, tipo, comprimento, kg/m, quantidade, peso do amarrado.
ACO_CEARENSE = """30 20 0,75 LF 6000 0,573 130 448|30 20 0,80 GA 6000 0,611 144 529|30 20 0,90 LF 6000 0,685 130 534|30 20 0,95 GA 6000 0,722 144 624|
30 20 1,20 LF 6000 0,904 130 704|30 20 1,25 GA 6000 0,940 121 681|30 20 1,50 LF 6000 1,119 126 842|30 20 1,55 GA 6000 1,154 100 689|
35 25 0,90 LF 6000 0,826 108 535|35 25 1,20 LF 6000 1,092 108 708|40 20 0,90 LF 6000 0,826 98 487|40 20 0,95 GA 6000 0,870 96 502|
40 20 1,20 LF 6000 1,092 98 642|40 20 1,25 GA 6000 1,136 96 654|40 20 1,50 LF 6000 1,354 84 682|40 20 1,55 GA 6000 1,397 80 669|
40 30 0,90 LF 6000 0,967 72 419|40 30 0,95 GA 6000 1,019 72 442|40 30 1,20 LF 6000 1,280 72 554|40 30 1,50 LF 6000 1,589 72 686|
50 20 0,90 LF 6000 1,967 78 554|50 20 1,20 LF 6000 1,280 78 600|50 20 1,50 LF 6000 1,589 78 743|
50 30 0,90 LF 6000 1,107 77 511|50 30 0,95 GA 6000 1,168 63 443|50 30 1,20 LF 6000 1,468 77 680|50 30 1,25 GA 6000 1,527 70 643|
50 30 1,50 LF 6000 1,824 60 657|50 30 1,55 GA 6000 1,882 56 633|50 30 2,00 LQ 6000 2,407 30 432|
60 40 1,20 LF 6000 1,844 48 533|60 40 1,50 LF 6000 2,293 48 662|60 40 2,00 LF 6000 3,033 35 637|
70 30 1,20 LF 6000 1,844 50 555|70 30 1,50 LF 6000 2,293 50 690|70 30 2,00 LQ 6000 3,033 32 582|
80 40 1,20 LF 6000 2,219 40 535|80 40 1,50 LF 6000 2,763 40 665|80 40 2,00 LF 6000 3,660 28 616|
90 30 1,20 LF 6000 2,219 44 589|90 30 1,50 LF 6000 2,763 36 599|
100 40 1,20 LF 6000 2,595 36 564|100 40 1,25 GA 6000 2,716 36 587|100 40 1,50 LF 6000 3,247 36 701|100 40 1,55 GA 6000 3,353 36 724|
100 40 1,80 LQ 6000 3,879 24 559|100 40 1,95 GA 6000 4,193 24 604|100 40 2,00 LQ 6000 4,286 24 619"""

def aco_cearense():
    for linha in ACO_CEARENSE.replace("\n", "").split("|"):
        b, h, e, tipo, comp, kgm, qtd, peso = linha.split()
        kgm, qtd, peso, comp = num(kgm), int(qtd), num(peso), num(comp)
        calculado = kgm * comp / 1000 * qtd
        desvio = abs(calculado - peso) / peso
        rev = desvio > 0.02
        obs = (f"Massa linear publicada ({kgm:.3f} kg/m) não fecha com o peso do amarrado ({peso:.0f} kg para {qtd} peças): "
               f"desvio de {desvio:.0%}. Confirmar com o fabricante.") if rev else None
        item(fabricante="Aço Cearense", familia="TuboRetangular", espessuraMm=num(e), acabamento=ACABAMENTO_POR_PREFIXO[tipo],
             tipoPeso="KgPorMetro", valorPeso=kgm, comprimentoPadraoMm=comp, pecasPorAmarrado=qtd, bitolaPol=None,
             diametroMm=None, larguraMm=num(b), alturaMm=num(h), abaMm=None, requerRevisao=rev, observacao=obs)

if __name__ == "__main__":
    pasta, saida = sys.argv[1], sys.argv[2]
    tubos_sampaio(pasta); perfis_sampaio(pasta); chapas_sampaio(pasta); aco_cearense()
    fabricantes = [
        {"nome": "Sampaio", "fonte": "https://sampaio-sa.com.br/produtos/"},
        {"nome": "Aço Cearense", "fonte": "https://www.grupoacocearense.com.br/wp-content/uploads/2025/02/Tubo-Retangular.png",
         "observacao": "Variação de peso nominal de ±5% a ±6%, conforme a tabela do fabricante."},
    ]
    json.dump({"versao": 1, "fabricantes": fabricantes, "itens": itens}, open(saida, "w", encoding="utf-8"), ensure_ascii=False, indent=1)
    from collections import Counter
    print(len(itens), "itens"); print(Counter((i["fabricante"], i["familia"]) for i in itens))
    for i in itens:
        if i["requerRevisao"]: print("REVISAR:", i["fabricante"], i["familia"], i.get("larguraMm"), i.get("alturaMm"), i["espessuraMm"], i["observacao"])
