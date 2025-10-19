D1)
SELECT j.id_jidlo, j.nazev AS jidlo
FROM jidla j
JOIN slozky_jidla sj ON j.id_jidlo = sj.id_jidlo
JOIN slozky s ON sj.id_slozka = s.id_slozka
WHERE s.nazev = 'Maso';

D2)
SELECT s.id_stravnik, s.jmeno, s.primeni
FROM stravnici s
JOIN objednavky o ON s.id_stravnik = o.id_stravnik
WHERE o.id_stav IN (2, 3);

D3)
SELECT s.id_stravnik, s.jmeno, s.primeni
FROM stravnici s
JOIN studenti st ON s.id_stravnik = st.id_stravnik
JOIN objednavky o ON s.id_stravnik = o.id_stravnik
GROUP BY s.id_stravnik, s.jmeno, s.primeni
HAVING COUNT(o.id_objednavka) = 1;

D4)
SELECT o.id_objednavka
FROM objednavky o
JOIN polozky p ON o.id_objednavka = p.id_objednavka
JOIN jidla j ON p.id_jidlo = j.id_jidlo
GROUP BY o.id_objednavka
HAVING COUNT(DISTINCT j.id_menu) = 1 
   AND COUNT(p.id_jidlo) = (         
       SELECT COUNT(j2.id_jidlo)
       FROM jidla j2
       WHERE j2.id_menu = MAX(j.id_menu)
   );

D5)
SELECT id_stravnik, telefon
FROM pracovnici
JOIN platby USING (id_stravnik);

D6)
SELECT o.id_objednavka, o.id_stravnik, o.id_stav, s.nazev
FROM objednavky o
JOIN stavy s ON o.id_stav = s.id_stav
JOIN stravnici st ON o.id_stravnik = st.id_stravnik
WHERE s.nazev = 'Dokončeno'
  AND st.zustatek > 1500;

D7)
SELECT
    o.id_objednavka, 
    s.jmeno AS stravnik_jmeno, 
    s.primeni AS stravnik_primeni,
    a.psc AS stravnik_psc,
    a.obec AS stravnik_obec
FROM objednavky o
NATURAL JOIN stravnici s
NATURAL JOIN adresy a
WHERE a.obec = 'Praha';

D8)
SELECT stravnici.jmeno, menu.typ_menu
FROM stravnici
CROSS JOIN menu;

D9)
SELECT stravnici.id_stravnik, 
       stravnici.jmeno, 
       objednavky.id_objednavka, 
       objednavky.celkova_cena
FROM stravnici
LEFT OUTER JOIN objednavky ON stravnici.id_stravnik = objednavky.id_stravnik;

D10)
SELECT platby.id_platba, platby.castka, stravnici.jmeno
FROM stravnici
RIGHT OUTER JOIN platby ON stravnici.id_stravnik = platby.id_stravnik;

D11)
SELECT stravnici.id_stravnik, stravnici.jmeno, platby.datum
FROM stravnici
FULL OUTER JOIN platby ON stravnici.id_stravnik = platby.id_stravnik;

D12)
SELECT jmeno, primeni
FROM stravnici
WHERE id_stravnik IN (
    SELECT id_stravnik
    FROM platby
    WHERE castka > 0
);

D13)
SELECT dotaz.stredni_zustatek
FROM (
    SELECT AVG(zustatek) AS stredni_zustatek
    FROM stravnici
) dotaz;

D14)
SELECT jmeno, (SELECT COUNT(*) FROM objednavky WHERE 
objednavky.id_stravnik = stravnici.id_stravnik) AS pocet_objednavek
FROM stravnici;

D15)
SELECT m.nazev
FROM menu m
WHERE EXISTS (
    SELECT 1
    FROM jidla j
    WHERE j.id_menu = m.id_menu
);

D16)
SELECT s.jmeno
FROM stravnici s
JOIN studenti st ON s.id_stravnik = st.id_stravnik
WHERE s.zustatek <= 1500
UNION
SELECT s.jmeno
FROM stravnici s
JOIN studenti st ON s.id_stravnik = st.id_stravnik
WHERE st.vek > 10;

D17)
SELECT id_menu
FROM menu
MINUS
SELECT id_menu
FROM jidla;

D18)
SELECT id_stravnik
FROM stravnici
INTERSECT
SELECT id_stravnik
FROM omezeni_stravnici;

D19)
SELECT UPPER(CONCAT(CONCAT(nazev, ': '), popis)) AS nazev_popis_uppercase
FROM jidla;

D20)
SELECT s.jmeno, s.primeni
FROM stravnici s
JOIN pracovnici st ON s.id_stravnik = st.id_stravnik
WHERE s.zustatek > (
    SELECT AVG(zustatek)
    FROM stravnici
);


D21)
SELECT 
    o.datum AS datum_objednavky, 
    p.datum AS datum_platby, 
    ROUND((p.datum - o.datum) * 24 * 60) AS rozdil_v_minutach
FROM objednavky o
JOIN platby p ON o.id_stravnik = p.id_stravnik
WHERE o.id_stav = 1;

D22)
SELECT AVG(vek) AS prumerny_vek
FROM studenti;

D23)
SELECT id_platba, AVG(castka) AS avg_zustatek
FROM platby
GROUP BY id_platba
HAVING AVG(castka) > 4;

D24)
1.
SELECT nazev
FROM slozky
WHERE merna_jednotka = 'kus';

2.
SELECT nazev
FROM slozky
WHERE merna_jednotka IN (SELECT merna_jednotka FROM slozky WHERE merna_jednotka = 'kus');

3.
SELECT nazev
FROM slozky
GROUP BY nazev, merna_jednotka
HAVING merna_jednotka = 'kus';

D25)
SELECT id_stravnik, COUNT(*) AS pocet_objednavek
FROM objednavky
WHERE id_stravnik IS NOT NULL
GROUP BY id_stravnik
HAVING COUNT(*) > 1
ORDER BY pocet_objednavek DESC;

D26)
CREATE VIEW menu_obed AS
SELECT id_menu, nazev, typ_menu
FROM menu
WHERE typ_menu = 'OBED';

D27)
SELECT DISTINCT s.id_stravnik, str.jmeno, str.primeni
FROM studenti s
JOIN stravnici str ON s.id_stravnik = str.id_stravnik
JOIN objednavky o ON s.id_stravnik = o.id_stravnik
WHERE NOT EXISTS (
    SELECT 1 
    FROM omezeni_stravnici os 
    WHERE os.id_stravnik = s.id_stravnik
)
AND NOT EXISTS (
    SELECT 1 
    FROM alergie_stravnici als 
    WHERE als.id_stravnik = s.id_stravnik
);

D28)
INSERT INTO objednavky (id_objednavka, datum, celkova_cena, id_stravnik, id_stav)
SELECT s_obj.NEXTVAL, SYSDATE, 100, p.id_stravnik, 4
FROM pracovnici p
JOIN stravnici s ON p.id_stravnik = s.id_stravnik
WHERE p.telefon LIKE '2%';

D29)
UPDATE platby
SET castka = castka + 100.2
WHERE metoda = 'hotově' 
  AND castka > (SELECT AVG(castka) FROM platby);

D30)
DELETE FROM slozky
WHERE id_slozka IN (
    SELECT id_slozka
    FROM (
        SELECT MIN(id_slozka) AS id_slozka
        FROM slozky
        GROUP BY nazev
        HAVING COUNT(*) > 1
    )
);