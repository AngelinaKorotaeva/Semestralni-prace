triggery pro id: 

CREATE OR REPLACE TRIGGER trg_str_id 
BEFORE INSERT ON stravnici 
FOR EACH ROW 
BEGIN  
    if(:new.id_stravnik is null) then 
    SELECT s_str.nextval 
    INTO :new.id_stravnik
    FROM dual; 
  end if; 
END; 


CREATE OR REPLACE TRIGGER t_plat_id
BEFORE INSERT ON platby 
FOR EACH ROW 
BEGIN  
    if(:new.id_platba is null) then 
    SELECT s_plat.nextval 
    INTO :new.id_platba 
    FROM dual; 
  end if; 
END; 


CREATE OR REPLACE TRIGGER t_obj_id 
BEFORE INSERT ON objednavky 
FOR EACH ROW 
BEGIN  
    if(:new.id_objednavka is null) then 
    SELECT s_obj.nextval 
    INTO :new.id_objednavka 
    FROM dual; 
  end if; 
END; 


CREATE OR REPLACE TRIGGER t_jid_id 
BEFORE INSERT ON jidla 
FOR EACH ROW 
BEGIN  
    if(:new.id_jidlo is null) then 
    SELECT s_jid.nextval 
    INTO :new.id_jidlo 
    FROM dual; 
  end if; 
END; 


CREATE OR REPLACE TRIGGER t_menu_id 
BEFORE INSERT ON menu 
FOR EACH ROW 
BEGIN  
    if(:new.id_menu is null) then 
    SELECT s_menu.nextval 
    INTO :new.id_menu 
    FROM dual; 
  end if; 
END; 


CREATE OR REPLACE TRIGGER t_adr_id 
BEFORE INSERT ON adresy 
FOR EACH ROW 
BEGIN  
    if(:new.id_adresa is null) then 
    SELECT s_adr.nextval 
    INTO :new.id_adresa
    FROM dual; 
  end if; 
END; 


CREATE OR REPLACE TRIGGER t_aler_id 
BEFORE INSERT ON alergie 
FOR EACH ROW 
BEGIN  
    if(:new.id_alergie is null) then 
    SELECT s_aler.nextval 
    INTO :new.id_alergie
    FROM dual; 
  end if; 
END; 


CREATE OR REPLACE TRIGGER t_omez_id 
BEFORE INSERT ON dietni_omezeni
FOR EACH ROW 
BEGIN  
    if(:new.id_omezeni is null) then 
    SELECT s_omez.nextval 
    INTO :new.id_omezeni
    FROM dual; 
  end if; 
END; 


CREATE OR REPLACE TRIGGER t_sloz_id 
BEFORE INSERT ON slozky
FOR EACH ROW 
BEGIN  
    if(:new.id_slozka is null) then 
    SELECT s_sloz.nextval 
    INTO :new.id_slozka
    FROM dual; 
  end if; 
END; 


CREATE OR REPLACE TRIGGER t_soub_id 
BEFORE INSERT ON soubory
FOR EACH ROW 
BEGIN  
    if(:new.id_soubor is null) then 
    SELECT s_soub.nextval 
    INTO :new.id_soubor
    FROM dual; 
  end if; 
END; 


CREATE OR REPLACE TRIGGER t_log_id 
BEFORE INSERT ON logy
FOR EACH ROW 
BEGIN  
    if(:new.id_log is null) then 
    SELECT s_log.nextval 
    INTO :new.id_log
    FROM dual; 
  end if; 
END; 


6 a 14 zadaní triggery:

14 - Kontrola zůstatku při objednávce

Neumožňuje vytvořit objednávku, pokud klient nemá dostatek peněz

CREATE OR REPLACE TRIGGER objednavky_check_balance
BEFORE INSERT ON objednavky
FOR EACH ROW
DECLARE
    v_balance FLOAT;
BEGIN
    SELECT zustatek INTO v_balance
    FROM stravnici
    WHERE id_stravnik = :NEW.id_stravnik;

    IF v_balance < :NEW.celkova_cena THEN
        RAISE_APPLICATION_ERROR(-20001, 'Nedostatečný zůstatek pro objednávku.');
    END IF;
END;

Automatické stržení peněz při objednávce

CREATE OR REPLACE TRIGGER objednavky_deduct_balance
AFTER INSERT ON objednavky
FOR EACH ROW
BEGIN
    UPDATE stravnici
    SET zustatek = zustatek - :NEW.celkova_cena
    WHERE id_stravnik = :NEW.id_stravnik;
END;

14 - Automatické přičtení peněz při platbě

CREATE OR REPLACE TRIGGER platby_add_balance
AFTER INSERT ON platby
FOR EACH ROW
BEGIN
    UPDATE stravnici
    SET zustatek = zustatek + :NEW.castka
    WHERE id_stravnik = :NEW.id_stravnik;
END;


Kontrola přítomnosti ingrediencí před přidáním jídla

CREATE OR REPLACE TRIGGER jidlo_check_ingredients
BEFORE INSERT ON jidla
FOR EACH ROW
DECLARE
    v_count INTEGER;
BEGIN
    SELECT COUNT(*) INTO v_count
    FROM slozky_jidla
    WHERE id_jidlo = :NEW.id_jidlo;

    IF v_count = 0 THEN
        RAISE_APPLICATION_ERROR(-20003, 'Jídlo musí mít alespoň jednu složku.');
    END IF;
END;

Automatické nastavení datumu objednávky

CREATE OR REPLACE TRIGGER obj_auto_date
BEFORE INSERT ON objednavky
FOR EACH ROW
BEGIN
    IF :NEW.datum IS NULL THEN
        :NEW.datum := SYSDATE;
    END IF;
END;

 Logování změn (audit)


CREATE TABLE audit_log (
    user_name VARCHAR2(30),
    table_name VARCHAR2(30),
    action VARCHAR2(10),
    action_date DATE
);


14 - 

CREATE OR REPLACE TRIGGER audit_objednavky
AFTER INSERT OR UPDATE OR DELETE ON objednavky
BEGIN
    INSERT INTO audit_log (user_name, table_name, action, action_date)
    VALUES (USER, 'OBJEDNAVKY',
            CASE
                WHEN INSERTING THEN 'INSERT'
                WHEN UPDATING THEN 'UPDATE'
                WHEN DELETING THEN 'DELETE'
            END,
            SYSDATE);
END;
/


Proč: Záznam změn - kdo co udělal s příkazy a kdy.


6 - Automatické nastavení stavu objednávky

CREATE OR REPLACE TRIGGER obj_set_default_status
BEFORE INSERT ON objednavky
FOR EACH ROW
BEGIN
    IF :NEW.id_stav IS NULL THEN
        :NEW.id_stav := 3; -- "V procesu"
    END IF;
END;

6 - Automatické mazání starých neuhrazených objednávek ??