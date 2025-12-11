1. Seznam jídel s přiřazeným menu (které jídlo patří do jaké nabídky).

  CREATE OR REPLACE VIEW v_jidla_menu AS 
  SELECT 
    j.id_jidlo,
    j.nazev AS jidlo,
    j.kategorie,
    j.cena,
    m.nazev AS menu_nazev
FROM jidla j
LEFT JOIN menu m ON j.id_menu = m.id_menu;


2. Detailní složení jídel – ukazuje ingredience, množství a jednotky.

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


3. Přehled objednávek – kdo objednal, stav objednávky, datum a cena.

  CREATE OR REPLACE VIEW v_obj_prehled AS 
  SELECT 
    o.id_objednavka,
    s.jmeno || ' ' || s.prijmeni AS zakaznik,
    st.nazev AS stav_objednavky,
    o.datum,
    o.celkova_cena
FROM objednavky o
JOIN stravnici s ON o.id_stravnik = s.id_stravnik
JOIN stavy st ON o.id_stav = st.id_stav;


4. Obsah objednávky – jaká jídla a v jakém množství si zákazník objednal.

  CREATE OR REPLACE VIEW v_obj_slozeni AS 
  SELECT 
o.id_objednavka,
s.jmeno || ' ' || s.prijmeni AS zakaznik,
o.datum,
o.celkova_cena,
LISTAGG(j.nazev || ' x ' || p.mnozstvi || ', ') WITHIN GROUP (ORDER BY j.nazev) AS jidla_v_objednavce    
FROM objednavky o
JOIN stravnici s ON o.id_stravnik = s.id_stravnik
JOIN polozky p ON o.id_objednavka = p.id_objednavka
JOIN jidla j ON p.id_jidlo = j.id_jidlo
GROUP BY o.id_objednavka, s.jmeno, s.prijmeni, o.datum, o.celkova_cena;


5. Seznam strávníků s jejich alergiemi a dietními omezeními.

  CREATE OR REPLACE VIEW v_str_omezeni_alergie AS 
  SELECT 
    s.id_stravnik,
    s.jmeno,
    s.prijmeni,
    s.email,
    LISTAGG(DISTINCT a.nazev, ', ') WITHIN GROUP (ORDER BY a.nazev) AS alergie,
    LISTAGG(DISTINCT d.nazev, ', ') WITHIN GROUP (ORDER BY d.nazev) AS dietni_omezeni
FROM stravnici s
LEFT JOIN stravnici_alergie asr ON s.id_stravnik = asr.id_stravnik
LEFT JOIN alergie a ON a.id_alergie = asr.id_alergie
LEFT JOIN stravnici_omezeni osr ON s.id_stravnik = osr.id_stravnik
LEFT JOIN dietni_omezeni d ON d.id_omezeni = osr.id_omezeni
WHERE a.id_alergie IS NOT NULL OR d.id_omezeni IS NOT NULL
GROUP BY s.id_stravnik, s.jmeno, s.prijmeni, s.email;


6. Kompletní údaje o strávníkovi – osobní info, adresa, telefon, role, pozice nebo třída.

  CREATE OR REPLACE VIEW v_stravnici_full AS 
  SELECT 
    s.id_stravnik,
    s.jmeno,
    s.prijmeni,
    s.email,
    s.role,
    s.typ_stravnik,
    s.zustatek,
    a.psc,
    a.ulice,
    a.mesto,
    p.telefon,
    po.nazev AS pozice,
    st.id_trida,
    st.datum_narozeni
FROM stravnici s
LEFT JOIN adresy a ON s.id_adresa = a.id_adresa
LEFT JOIN pracovnici p ON s.id_stravnik = p.id_stravnik
LEFT JOIN pozice po ON p.id_pozice = po.id_pozice
LEFT JOIN studenti st ON st.id_stravnik = s.id_stravnik;


7. Aktivní strávníci s přihlašovacími údaji – používá se pro login do systému.

  CREATE OR REPLACE VIEW v_stravnici_login AS 
  SELECT
    id_stravnik,
    email,
    heslo,
    role,
    typ_stravnik,
    aktivita
FROM stravnici
WHERE aktivita = '1';


8. Studenti přiřazení ke třídám – jméno, datum narození, e‑mail, číslo třídy.

  CREATE OR REPLACE VIEW v_stud_trida AS 
  SELECT 
    st.id_stravnik,
    s.jmeno || ' ' || s.prijmeni AS jmeno,
    st.datum_narozeni,
    s.email,
    t.cislo_tridy
FROM studenti st
JOIN stravnici s ON st.id_stravnik = s.id_stravnik
JOIN tridy t ON st.id_trida = t.id_trida;
