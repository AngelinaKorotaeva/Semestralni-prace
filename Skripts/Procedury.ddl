12 пункт!

Примеры таких процедур
1)   Вставка нового заказа (objednavky)
CREATE OR REPLACE PROCEDURE pr_insert_objednavka(
    p_id_stravnik IN NUMBER,
    p_celkova_cena IN NUMBER,
    p_poznamka IN VARCHAR2
)
IS
BEGIN
    INSERT INTO objednavky (id_objednavka, datum, celkova_cena, poznamka, id_stravnik, id_stav)
    VALUES (objednavky_seq.NEXTVAL, SYSDATE, p_celkova_cena, p_poznamka, p_id_stravnik, 1); -- 1 = "vytvořeno"
END;
/




2)  Обновление статуса заказа
CREATE OR REPLACE PROCEDURE pr_update_objednavka_status(
    p_id_objednavka IN NUMBER,
    p_id_stav IN NUMBER
)
IS
BEGIN
    UPDATE objednavky
    SET id_stav = p_id_stav
    WHERE id_objednavka = p_id_objednavka;
END;
/




3)  Nova objednavka
CREATE OR REPLACE PROCEDURE vytvor_objednavku (
    p_id_objednavka OUT NUMBER,      -- OUT: вернём ID нового заказа
    p_id_stravnik   IN  NUMBER,      -- кто сделал заказ
    p_id_stav       IN  NUMBER,      -- статус заказа (например 1 = nový)
    p_celkova_cena  IN  FLOAT,       -- сумма заказа
    p_poznamka      IN  VARCHAR2,    -- примечание
    p_metoda_platby IN  VARCHAR2     -- способ оплаты ('hotove', 'kartou' и т.д.)
)
AS
    v_id_platba   NUMBER;
BEGIN
    ------------------------------------------------------------------
    -- 1️⃣ Создаём новый заказ
    ------------------------------------------------------------------
    SELECT NVL(MAX(id_objednavka), 0) + 1 INTO p_id_objednavka FROM objednavky;

    INSERT INTO objednavky (
        id_objednavka, datum, celkova_cena, poznamka, id_stav, id_stravnik
    ) VALUES (
        p_id_objednavka, SYSDATE, p_celkova_cena, p_poznamka, p_id_stav, p_id_stravnik
    );

    ------------------------------------------------------------------
    -- 2️⃣ Создаём запись в платбах (PLATBY)
    ------------------------------------------------------------------
    SELECT NVL(MAX(id_platba), 0) + 1 INTO v_id_platba FROM platby;

    INSERT INTO platby (
        id_platba, datum, castka, metoda, id_stravnik
    ) VALUES (
        v_id_platba, SYSDATE, p_celkova_cena, p_metoda_platby, p_id_stravnik
    );

    ------------------------------------------------------------------
    -- 3️⃣ Добавляем запись в лог
    ------------------------------------------------------------------
    INSERT INTO logy (
        id_log, tabulka, id_zaznam, akce, datum_cas, detail
    )
    VALUES (
        (SELECT NVL(MAX(id_log), 0) + 1 FROM logy),
        'OBJEDNAVKY',
        p_id_objednavka,
        'INSERT',
        SYSDATE,
        'Vytvořena objednávka s automatickou platbou'
    );

    COMMIT;
EXCEPTION
    WHEN OTHERS THEN
        ROLLBACK;
        raise_application_error(-20001, 'Chyba při vytváření objednávky: ' || SQLERRM);
END;



4) Celkova cena objednavky (pro vytvoreni objednavky, pdf potvrzezi 

CREATE OR REPLACE FUNCTION vypocitej_cenu_objednavky (
    p_id_objednavka IN NUMBER
) RETURN FLOAT
AS
    v_celkova FLOAT := 0;
BEGIN
    SELECT SUM(p.mnozstvi * j.cena)
    INTO v_celkova
    FROM polozky p
    JOIN jidla j ON j.id_jidlo = p.id_jidlo
    WHERE p.id_objednavka = p_id_objednavka;

    RETURN NVL(v_celkova, 0);
END;





5) Pridani penez na ucet

CREATE OR REPLACE PROCEDURE pridat_platbu (
    p_id_stravnik IN INTEGER,
    p_castka      IN FLOAT,
    p_metoda      IN VARCHAR2
)
AS
    v_id_platba INTEGER;
BEGIN

    SELECT NVL(MAX(id_platba), 0) + 1
    INTO v_id_platba
    FROM platby;

    INSERT INTO platby (id_platba, datum, castka, metoda, id_stravnik)
    VALUES (v_id_platba, SYSDATE, p_castka, p_metoda, p_id_stravnik);

    UPDATE stravnici
    SET zustatek = zustatek + p_castka
    WHERE id_stravnik = p_id_stravnik;

    INSERT INTO logy (id_log, tabulka, id_zaznam, akce, datum_cas, detail)
    VALUES (
        (SELECT NVL(MAX(id_log),0)+1 FROM logy),
        'PLATBY',
        v_id_platba,
        'INSERT',
        SYSDATE,
        'Přidána platba ' || p_castka || ' Kč pro stravníka ID=' || p_id_stravnik ||
    );

    COMMIT;
END;


EXEC pridat_platbu(5, 150.00, 'online');
EXEC pridat_platbu(3, 320.00, 'hotovost');