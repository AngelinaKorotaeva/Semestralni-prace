12 пункт!

Примеры таких процедур
1️⃣ Вставка нового заказа (objednavky)
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

2️⃣ Обновление статуса заказа
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

3️⃣ Вставка фотографии пользователя (fotky_stravnici)
CREATE OR REPLACE PROCEDURE pr_insert_fotka_stravnik(
    p_id_stravnik IN NUMBER,
    p_obsah BLOB,
    p_nazev VARCHAR2
)
IS
BEGIN
    INSERT INTO fotky_stravnici (id_stravnik, obsah, nazev_souboru, datum_nahrani)
    VALUES (p_id_stravnik, p_obsah, p_nazev, SYSDATE);
END;