1 Funkce: f_balance_check

Typ: vrací číslo/textovou hodnotu
Co dělá: kontroluje, zda má uživatel dostatek prostředků pro objednávku

CREATE OR REPLACE FUNCTION f_balance_check(
    p_id_stravnik IN INTEGER,
    p_cena IN FLOAT
) RETURN VARCHAR2
IS
    v_balance FLOAT;
BEGIN
    SELECT zustatek INTO v_balance
    FROM stravnik
    WHERE id_stravnik = p_id_stravnik;

    IF v_balance >= p_cena THEN
        RETURN 'Dostatek prostředků';
    ELSE
        RETURN 'Nedostatek prostředků';
    END IF;
EXCEPTION
    WHEN NO_DATA_FOUND THEN
        RETURN 'Uživatel nenalezen';
END;
/


Příklad volání:

SELECT f_balance_check(1, 150) FROM dual;


Účel:

Kontrola, zda je možné vytvořit objednávku

Pracuje s obchodní logikou

2 Funkce: f_total_spent

Typ: vrací číslo (FLOAT)
Co dělá: počítá součet všech plateb uživatele

CREATE OR REPLACE FUNCTION f_total_spent(
    p_id_stravnik IN INTEGER
) RETURN FLOAT
IS
    v_sum FLOAT;
BEGIN
    SELECT NVL(SUM(castka),0) INTO v_sum
    FROM platba
    WHERE id_stravnik = p_id_stravnik;

    RETURN v_sum;
END;
/


Příklad volání:

SELECT f_total_spent(1) FROM dual;


Účel:

Vypočítá, kolik peněz uživatel celkem utratil

Používá se v přehledech / DA

Liší se od funkce č. 1 typem návratové hodnoty (vrací číslo, nikoli text)

3 Funkce: f_orders_status_count

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