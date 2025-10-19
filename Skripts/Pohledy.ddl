1. v_objednavky_prehled

( Přehled objednávek — spojení více tabulek )

Typ: JOIN view
Používá několik tabulek, zobrazuje objednávky se jménem zákazníka, stavem a cenou.

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