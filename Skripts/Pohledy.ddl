1. v_objednavky_prehled

( Přehled objednávek — spojení více tabulek )

zobrazuje objednávky se jménem zákazníka, stavem a cenou.

CREATE OR REPLACE VIEW v_obj_prehled AS
SELECT 
    o.id_objednavka,
    s.jmeno || ' ' || s.primeni AS zakaznik,
    st.nazev AS stav_objednavky,
    o.datum,
    o.celkova_cena
FROM objednavky o
JOIN stravnik s ON o.id_stravnik = s.id_stravnik
JOIN stav st ON o.id_stav = st.id_stav;


Co dělá:

Zobrazuje seznam všech objednávek s jménem zákazníka a stavem.

Lze použít pro „výpis objednávek“ v DA.

2. v_souhrn_platby

(Součet plateb pro každého uživatele)

Typ: AGGREGATION (GROUP BY)

CREATE OR REPLACE VIEW v_souhrn_platby AS
SELECT 
    s.id_stravnik,
    s.jmeno || ' ' || s.primeni AS zakaznik,
    COUNT(p.id_platba) AS pocet_plateb,
    SUM(p.castka) AS celkem_zaplaceno
FROM stravnik s
LEFT JOIN platba p ON s.id_stravnik = p.id_stravnik
GROUP BY s.id_stravnik, s.jmeno, s.primeni;


Co dělá:

Počítá, kolik plateb udělal každý uživatel a jakou celkovou částku zaplatil.

Lze použít pro „přehled plateb“ nebo pro účetní report.

3. v_jidla_menu

(Seznam jídel s názvem menu a cenou)

Typ: JOIN + filtrace (WHERE)

CREATE OR REPLACE VIEW v_jidla_menu AS
SELECT 
    j.id_jidlo,
    j.nazev AS jidlo,
    j.kategorie,
    j.cena,
    m.nazev AS menu_nazev,
    m.typ_menu
FROM jidlo j
LEFT JOIN menu m ON j.menu_id_menu = m.id_menu;


Co dělá:

Zobrazuje jídla a k jakému menu patří.

Lze použít v DA pro „výpis jídel podle menu“.


4. Tabulka strvavniky a jejich dietni omezeni nebo/a alergie

CREATE OR REPLACE VIEW v_stravnici_omezeni_alergie AS
SELECT 
    s.id_stravnik,
    s.jmeno,
    s.primeni,
    s.email,
    LISTAGG(DISTINCT a.nazev, ', ') WITHIN GROUP (ORDER BY a.nazev) AS alergie,
    LISTAGG(DISTINCT d.nazev, ', ') WITHIN GROUP (ORDER BY d.nazev) AS dietni_omezeni
FROM stravnik s
LEFT JOIN alergie_stravnici asr ON s.id_stravnik = asr.id_stravnik
LEFT JOIN alergie a ON a.id_alergie = asr.id_alergie
LEFT JOIN omezeni_stravnici osr ON s.id_stravnik = osr.id_stravnik
LEFT JOIN dietni_omezeni d ON d.id_omezeni = osr.id_omezeni
WHERE a.id_alergie IS NOT NULL OR d.id_omezeni IS NOT NULL
GROUP BY s.id_stravnik, s.jmeno, s.primeni, s.email;


5. Jídla и jejich složení

CREATE OR REPLACE VIEW v_jidla_slozeni AS
SELECT 
    j.id_jidlo,
    j.nazev AS nazev_jidla,
    j.popis AS popis_jidla,
    j.kategorie,
    j.cena,
    LISTAGG(slo.nazev || ' (' || sj.mnozstvi || ' ' || slo.merna_jednotka || ')', ', ') 
        WITHIN GROUP (ORDER BY slo.nazev) AS slozeni
FROM jidla j
JOIN slozky_jidla sj ON j.id_jidlo = sj.id_jidlo
JOIN slozky slo ON sj.id_slozka = slo.id_slozka
GROUP BY j.id_jidlo, j.nazev, j.popis, j.kategorie, j.cena;