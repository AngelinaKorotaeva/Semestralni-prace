a) Triggery pro id: 

1. Automatické doplnění id strávníka při vložení

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


2. Automatické doplnění id platby

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


3. Automatické doplnění id objednávky

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


4.
Automatické doplnění id jídla
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


5. Automatické doplnění id menu

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


6. Automatické doplnění id adresy

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


7. Automatické doplnění id alergie

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


8. Automatické doplnění id omezení

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


9. Automatické doplnění id složky

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


10. Automatické doplnění id souboru

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


11. Automatické doplnění id logu

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


b) Triggery splňující požadavky:

12. Zákaz vytvoření objednávky bez dostatečného kreditu (bod 14 – kontrola zůstatku)

CREATE OR REPLACE TRIGGER t_obj_zustatek
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


13. Automatické stržení peněz při objednávce (bod 14 – práce s transakcemi)

CREATE OR REPLACE TRIGGER t_obj_platba
AFTER INSERT ON objednavky
FOR EACH ROW
BEGIN
    UPDATE stravnici
    SET zustatek = zustatek - :NEW.celkova_cena
    WHERE id_stravnik = :NEW.id_stravnik;
END;


14. Automatické přičtení peněz při platbě (bod 14 – práce s transakcemi)

CREATE OR REPLACE TRIGGER t_platba_zustatek
AFTER INSERT ON platby
FOR EACH ROW
BEGIN
    UPDATE stravnici
    SET zustatek = zustatek + :NEW.castka
    WHERE id_stravnik = :NEW.id_stravnik;
END;


15. Automatické nastavení datumu objednávky (bod 6 – automatické doplnění hodnot)

CREATE OR REPLACE TRIGGER t_obj_date
BEFORE INSERT ON objednavky
FOR EACH ROW
BEGIN
    IF :NEW.datum IS NULL THEN
        :NEW.datum := SYSDATE;
    END IF;
END;

 
16. Automatické nastavení výchozího stavu objednávky (bod 6 – doplnění výchozí hodnoty)

CREATE OR REPLACE TRIGGER t_obj_status
BEFORE INSERT ON objednavky
FOR EACH ROW
BEGIN
    IF :NEW.id_stav IS NULL THEN
        :NEW.id_stav := 3; -- "V procesu"
    END IF;
END;
