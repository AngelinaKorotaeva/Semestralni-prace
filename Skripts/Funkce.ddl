1 Cena objednavky

CREATE OR REPLACE FUNCTION get_celkova_cena(p_id_objednavka IN INTEGER)
RETURN FLOAT IS
    v_sum FLOAT := 0;
BEGIN
    SELECT SUM(cena_polozky * mnozstvi)
    INTO v_sum
    FROM polozky
    WHERE id_objednavka = p_id_objednavka;

    RETURN NVL(v_sum, 0);
END;



2 Funkce: f_orders_status_count

Typ: vrací VARCHAR2 s agregovanými informacemi
Co dělá: počítá počet objednávek uživatele v různých stavech a vrací textové shrnutí

CREATE OR REPLACE FUNCTION f_orders_status_count(
    p_id_stravnik IN INTEGER
) RETURN VARCHAR2
IS
    v_nezap  INTEGER;
    v_vproc  INTEGER;
    v_dokon  INTEGER;
    v_zrus   INTEGER;
BEGIN
    SELECT COUNT(*) INTO v_nezap FROM objednavky WHERE id_stravnik = p_id_stravnik AND id_stav = 4;
    SELECT COUNT(*) INTO v_vproc FROM objednavky WHERE id_stravnik = p_id_stravnik AND id_stav = 3;
    SELECT COUNT(*) INTO v_dokon FROM objednavky WHERE id_stravnik = p_id_stravnik AND id_stav = 1;
    SELECT COUNT(*) INTO v_zrus  FROM objednavky WHERE id_stravnik = p_id_stravnik AND id_stav = 2;

    RETURN 'V procesu: ' || v_vproc || ', Dokončeno: ' || v_dokon || 
           ', Zrušené: ' || v_zrus || ', Nezaplaceny: ' || v_nezap;
END;
/


Příklad volání:

SELECT f_orders_status_count(1) FROM dual;


Účel:

Poskytuje přehled o počtu objednávek uživatele podle stavu

Umožňuje rychle zobrazit souhrn v DA


3 Funkce pro určení stavu menu podle času


CREATE OR REPLACE FUNCTION stav_menu(p_id_menu IN INTEGER)
RETURN VARCHAR2 IS
    v_od DATE;
    v_do DATE;
    v_stav VARCHAR2(20);
BEGIN
    SELECT time_od, time_do
    INTO v_od, v_do
    FROM menu
    WHERE id_menu = p_id_menu;

    IF SYSDATE < v_od THEN
        v_stav := 'ČEKÁ NA START';
    ELSIF SYSDATE BETWEEN v_od AND v_do THEN
        v_stav := 'AKTIVNÍ';
    ELSE
        v_stav := 'UKONČENO';
    END IF;

    RETURN v_stav;
EXCEPTION
    WHEN NO_DATA_FOUND THEN
        RETURN 'MENU NEEXISTUJE';
END;


4 Funkce overeni hesla 

CREATE OR REPLACE FUNCTION check_heslo(
    p_email IN VARCHAR2,
    p_password IN VARCHAR2
) RETURN NUMBER
AS
    v_stored_hash VARCHAR2(200);
    v_input_hash  VARCHAR2(200);
BEGIN
    SELECT heslo INTO v_stored_hash
    FROM stravnici
    WHERE email = p_email
    AND aktivita = 'A';

    v_input_hash := LOWER(
        RAWTOHEX(DBMS_CRYPTO.HASH(
            UTL_I18N.STRING_TO_RAW(p_password, 'AL32UTF8'),
            DBMS_CRYPTO.HASH_SH256
        ))
    );

    IF v_input_hash = v_stored_hash THEN
        RETURN 1;
    END IF;

    RETURN 0;

EXCEPTION
    WHEN NO_DATA_FOUND THEN
        RETURN 0;
END;

