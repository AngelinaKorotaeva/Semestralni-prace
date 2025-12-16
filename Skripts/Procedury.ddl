1. Vložení nové objednávky – vytvoří záznam s aktuálním datem a výchozím stavem.

create or replace PROCEDURE P_INSERT_OBJ 
(
  P_ID_STRAVNIK   IN NUMBER,
  P_CELKOVA_CENA  IN NUMBER,
  P_POZNAMKA      IN VARCHAR2
) AS 
BEGIN
  INSERT INTO objednavky 
    (id_objednavka, datum, celkova_cena, poznamka, id_stravnik, id_stav)
  VALUES 
    (S_OBJ.NEXTVAL, SYSDATE, P_CELKOVA_CENA, P_POZNAMKA, P_ID_STRAVNIK, 1);
END P_INSERT_OBJ;

2. Odstranění starých objednávek – maže objednávky starší než 12 měsíců pomocí explicitního kurzoru.
   (Explicitní kurzor – FOR UPDATE, DELETE WHERE CURRENT OF)

create or replace PROCEDURE P_ODSTR_STAR_OBJ AS 
CURSOR c_old IS
    SELECT id_objednavka
    FROM objednavky
    WHERE datum < ADD_MONTHS(SYSDATE, -12)
    FOR UPDATE;
    v_id NUMBER;
BEGIN
  OPEN c_old;

    LOOP
        FETCH c_old INTO v_id;
        EXIT WHEN c_old%NOTFOUND;

        DELETE FROM objednavky
        WHERE CURRENT OF c_old;

        DBMS_OUTPUT.PUT_LINE('Smazána objednávka: ' || v_id);
    END LOOP;

    CLOSE c_old;
END P_ODSTR_STAR_OBJ;

3. Registrace pracovníka – vloží adresu, strávníka, pracovníka a případně uloží fotografii.

create or replace PROCEDURE p_register_pracovnik (
    p_psc          IN NUMBER,
    p_mesto        IN VARCHAR2,
    p_ulice        IN VARCHAR2,
    p_jmeno        IN VARCHAR2,
    p_prijmeni     IN VARCHAR2,
    p_email        IN VARCHAR2,
    p_heslo        IN VARCHAR2,
    p_zustatek     IN NUMBER DEFAULT 0,
    p_telefon      IN NUMBER,
    p_pozice       IN NUMBER,
    p_foto         IN BLOB,
    p_foto_nazev   IN VARCHAR2,
    p_foto_pripona IN VARCHAR2,
    p_foto_typ     IN VARCHAR2
)
IS
    v_id_adresa    NUMBER;
    v_id_stravnik  NUMBER;
    v_id_pracovnik NUMBER;
BEGIN
    SELECT s_adr.NEXTVAL INTO v_id_adresa FROM dual;
    SELECT s_str.NEXTVAL INTO v_id_stravnik FROM dual;
    SELECT v_id_stravnik INTO v_id_pracovnik FROM dual;

    INSERT INTO adresy (id_adresa, psc, mesto, ulice)
    VALUES (v_id_adresa, p_psc, p_mesto, p_ulice);

    INSERT INTO stravnici (
        id_stravnik, jmeno, prijmeni, email, heslo,
        zustatek, role, aktivita, typ_stravnik, id_adresa
    ) VALUES (
        v_id_stravnik, p_jmeno, p_prijmeni, p_email, p_heslo,
        p_zustatek, 'user', '1', 'pr', v_id_adresa
    );

    INSERT INTO pracovnici (
        id_stravnik, telefon, id_pozice
    ) VALUES (
        v_id_stravnik, p_telefon, p_pozice
    );
    
    IF p_foto IS NOT NULL THEN
        INSERT INTO soubory (
            id_soubor, nazev, typ, pripona, obsah,
            datum_nahrani, tabulka, id_zaznam, id_stravnik
        ) VALUES (
            s_soub.NEXTVAL,
            p_foto_nazev,
            p_foto_typ,
            p_foto_pripona,
            p_foto,
            SYSDATE,
            'STRAVNICI',
            v_id_stravnik,
            v_id_stravnik
        );
    END IF;
END;

4. Registrace studenta – vloží adresu, strávníka, studenta a případně uloží fotografii.

create or replace PROCEDURE p_register_student (
    p_psc          IN NUMBER,
    p_mesto        IN VARCHAR2,
    p_ulice        IN VARCHAR2,
    p_jmeno        IN VARCHAR2,
    p_prijmeni     IN VARCHAR2,
    p_email        IN VARCHAR2,
    p_heslo        IN VARCHAR2,
    p_zustatek     IN NUMBER DEFAULT 0,
    p_rok_narozeni IN DATE,
    p_cislo_tridy  IN NUMBER,
    p_foto         IN BLOB,
    p_foto_nazev   IN VARCHAR2,
    p_foto_pripona IN VARCHAR2,
    p_foto_typ     IN VARCHAR2
)
IS
    v_id_adresa   NUMBER;
    v_id_stravnik NUMBER;
    v_id_student  NUMBER;
BEGIN
    SELECT s_adr.NEXTVAL INTO v_id_adresa FROM dual;
    SELECT s_str.NEXTVAL INTO v_id_stravnik FROM dual;
    SELECT v_id_stravnik INTO v_id_student FROM dual;

    INSERT INTO adresy (id_adresa, psc, mesto, ulice)
    VALUES (v_id_adresa, p_psc, p_mesto, p_ulice);

    INSERT INTO stravnici (
        id_stravnik, jmeno, prijmeni, email, heslo,
        zustatek, role, aktivita, typ_stravnik, id_adresa
    ) VALUES (
        v_id_stravnik, p_jmeno, p_prijmeni, p_email, p_heslo,
        p_zustatek, 'user', '1', 'st', v_id_adresa
    );

    INSERT INTO studenti (
        id_stravnik, datum_narozeni, id_trida
    ) VALUES (
        v_id_student, p_rok_narozeni, p_cislo_tridy
    );
    
    IF p_foto IS NOT NULL THEN
        INSERT INTO soubory (
            id_soubor, nazev, typ, pripona, obsah,
            datum_nahrani, tabulka, id_zaznam, id_stravnik
        ) VALUES (
            s_soub.NEXTVAL,
            p_foto_nazev,
            p_foto_typ,
            p_foto_pripona,
            p_foto,
            SYSDATE,
            'STRAVNICI',
            v_id_stravnik,
            v_id_stravnik
        );
    END IF;
END;

5. Výpis souborů strávníka – zobrazí všechny nahrané soubory pomocí explicitního kurzoru. (Procedura obsahuje explicitní kurzor)

create or replace PROCEDURE P_SOUBORY_STRAVNIKA (
p_id_stravnik IN NUMBER 
) AS
CURSOR c_soubory IS
    SELECT id_soubor, nazev, typ, pripona, datum_nahrani
    FROM soubory
    WHERE id_stravnik = p_id_stravnik;
    
    v_id    soubory.id_soubor%TYPE;
    v_nazev soubory.nazev%TYPE;
    v_typ   soubory.typ%TYPE;
    v_prip  soubory.pripona%TYPE;
    v_dat   soubory.datum_nahrani%TYPE;
    
BEGIN
  OPEN c_soubory;

    LOOP
        FETCH c_soubory INTO v_id, v_nazev, v_typ, v_prip, v_dat;
        EXIT WHEN c_soubory%NOTFOUND;

        DBMS_OUTPUT.PUT_LINE(
            'Soubor ' || v_id || ': ' || v_nazev ||
            ' (' || v_typ || '/' || v_prip || '), nahrán: ' || v_dat
        );
    END LOOP;

    CLOSE c_soubory;
END P_SOUBORY_STRAVNIKA;

6. Statistika stavů objednávek – spočítá počet objednávek pro každý stav. (Explicitní kurzor a agregace)

create or replace PROCEDURE P_STATISTIKA_STAVU AS
    CURSOR  c_stavy IS
        SELECT id_stav, nazev FROM stavy;
        v_id   stavy.id_stav%TYPE;
        v_nazev stavy.nazev%TYPE;
        v_pocet NUMBER;
BEGIN
   OPEN c_stavy;

    LOOP
        FETCH c_stavy INTO v_id, v_nazev;
        EXIT WHEN c_stavy%NOTFOUND;

        SELECT COUNT(*) INTO v_pocet
        FROM objednavky
        WHERE id_stav = v_id;

        DBMS_OUTPUT.PUT_LINE(v_nazev || ': ' || v_pocet);
    END LOOP;

    CLOSE c_stavy;
END P_STATISTIKA_STAVU;

7. Výpis jídel – kurzor FOR LOOP pro zobrazení nabídky jídel (Implicitně řízený kurzor – CURSOR FOR LOOP)

create or replace PROCEDURE P_VYPIS_JIDEL AS 
BEGIN
  FOR r IN (SELECT id_jidlo, nazev, cena FROM jidla ORDER BY nazev)
    LOOP
        DBMS_OUTPUT.PUT_LINE(r.id_jidlo || ' - ' || r.nazev || ' (' || r.cena || ' Kč)');
    END LOOP;
END P_VYPIS_JIDEL;

8. Transakční registrace pracovníka – provede registraci pracovníka v rámci jedné transakce.

create or replace PROCEDURE trans_register_pracovnik (
    p_psc          IN NUMBER,
    p_mesto        IN VARCHAR2,
    p_ulice        IN VARCHAR2,
    p_jmeno        IN VARCHAR2,
    p_prijmeni     IN VARCHAR2,
    p_email        IN VARCHAR2,
    p_heslo        IN VARCHAR2,
    p_zustatek     IN NUMBER DEFAULT 0,
    p_telefon      IN NUMBER,
    p_pozice       IN NUMBER,
    p_foto         IN BLOB,
    p_foto_nazev   IN VARCHAR2,
    p_foto_pripona IN VARCHAR2,
    p_foto_typ     IN VARCHAR2
)
IS
BEGIN
    SAVEPOINT sp_reg;

    BEGIN
        p_register_pracovnik(
            p_psc, p_mesto, p_ulice,
            p_jmeno, p_prijmeni, p_email, p_heslo,
            p_zustatek, p_telefon, p_pozice,
            p_foto,
            p_foto_nazev,
            p_foto_pripona,
            p_foto_typ
        );

        COMMIT;

    EXCEPTION
        WHEN OTHERS THEN
            ROLLBACK TO sp_reg;
            RAISE;
    END;
END;

9. Transakční registrace studenta – provede registraci studenta v rámci jedné transakce.

create or replace PROCEDURE trans_register_student (
    p_psc          IN NUMBER,
    p_mesto        IN VARCHAR2,
    p_ulice        IN VARCHAR2,
    p_jmeno        IN VARCHAR2,
    p_prijmeni     IN VARCHAR2,
    p_email        IN VARCHAR2,
    p_heslo        IN VARCHAR2,
    p_zustatek     IN NUMBER DEFAULT 0,
    p_rok_narozeni IN DATE,
    p_cislo_tridy  IN NUMBER,
    p_foto         IN BLOB,
    p_foto_nazev   IN VARCHAR2,
    p_foto_pripona IN VARCHAR2,
    p_foto_typ     IN VARCHAR2
)
IS
BEGIN
    SAVEPOINT sp_reg;

    BEGIN
        p_register_student(
            p_psc, p_mesto, p_ulice,
            p_jmeno, p_prijmeni, p_email, p_heslo,
            p_zustatek, p_rok_narozeni, p_cislo_tridy,
            p_foto,
            p_foto_nazev,
            p_foto_pripona,
            p_foto_typ
        );

        COMMIT;

    EXCEPTION
        WHEN OTHERS THEN
            ROLLBACK TO sp_reg;
            RAISE;
    END;
END;
