1. Vrací celkovou cenu objednávky podle položek a jejich množství.

CREATE OR REPLACE FUNCTION F_CELKOVA_CENA 
(
  P_ID_OBJEDNAVKA IN INTEGER 
) RETURN FLOAT AS 
v_sum FLOAT := 0;
BEGIN
SELECT SUM(cena_polozky * mnozstvi)
INTO v_sum
FROM polozky
WHERE id_objednavka = P_ID_OBJEDNAVKA;
  RETURN NVL(v_sum, 0);
END F_CELKOVA_CENA;


2. Ukazuje přehled stavů objednávek daného strávníka (v procesu, dokončeno, zrušeno, nezaplaceno).

CREATE OR REPLACE FUNCTION F_OBJEDNAVKA_STAV 
(
  P_ID_STRAVNIK IN INTEGER 
) RETURN VARCHAR2 AS 
v_nezap INTEGER;
v_vproc INTEGER;
v_dokon INTEGER;
v_zrus INTEGER;
BEGIN
SELECT COUNT(*) INTO v_nezap FROM objednavky WHERE id_stravnik = p_id_stravnik AND id_stav = 4;
    SELECT COUNT(*) INTO v_vproc FROM objednavky WHERE id_stravnik = p_id_stravnik AND id_stav = 3;
    SELECT COUNT(*) INTO v_dokon FROM objednavky WHERE id_stravnik = p_id_stravnik AND id_stav = 1;
    SELECT COUNT(*) INTO v_zrus  FROM objednavky WHERE id_stravnik = p_id_stravnik AND id_stav = 2;
  RETURN 'V procesu: ' || v_vproc || ', Dokončeno: ' || v_dokon || 
           ', Zrušené: ' || v_zrus || ', Nezaplaceny: ' || v_nezap;
END F_OBJEDNAVKA_STAV;


3. Určuje aktuální stav menu (čeká na start, aktivní, ukončeno) podle časového období.

CREATE OR REPLACE FUNCTION F_STAV_MENU 
(
  P_ID_MENU IN INTEGER 
) RETURN VARCHAR2 AS 
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
END F_STAV_MENU;


4. Kontroluje, zda má strávník dostatek zůstatku na zaplacení částky; jinak vrací nedostatečný nebo neexistuje.

CREATE OR REPLACE FUNCTION F_ZUSTATEK 
(
  P_ID_STRAVNIK IN NUMBER 
, P_CASTKA IN FLOAT 
) RETURN VARCHAR2 AS
v_zustatek FLOAT;
BEGIN
SELECT zustatek INTO v_zustatek 
FROM stravnici 
WHERE id_stravnik = P_ID_STRAVNIK;

IF v_zustatek >= P_CASTKA THEN
RETURN 'OK';
ELSE 
RETURN 'NEDOSTATECNY';
END IF;

EXCEPTION 
WHEN NO_DATA_FOUND THEN
RETURN 'NEEXISTUJE';
END F_ZUSTATEK;


5. Vypočítá MD5 hash zadaného řetězce a vrátí ho jako hexadecimální hodnotu.

CREATE OR REPLACE FUNCTION md5hash (str IN VARCHAR2)
RETURN VARCHAR2
IS v_checksum VARCHAR2(32);
BEGIN
v_checksum := LOWER( RAWTOHEX( UTL_RAW.CAST_TO_RAW(
sys.dbms_obfuscation_toolkit.md5(input_string => str) ) ) );
RETURN v_checksum;
EXCEPTION WHEN NO_DATA_FOUND THEN NULL;
WHEN OTHERS THEN 
RAISE;
END md5hash;


6. Vrací počet znaků v zadaném řetězci.

CREATE OR REPLACE FUNCTION num_characters (p_string IN VARCHAR2)
    RETURN INTEGER 
AS
    v_num_characters INTEGER;
BEGIN
    SELECT LENGTH(p_string) INTO v_num_characters
    FROM dual;

    RETURN v_num_characters;
END;
