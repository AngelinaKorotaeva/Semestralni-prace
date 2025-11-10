CREATE OR REPLACE TRIGGER trg_alergie_log
AFTER INSERT OR UPDATE OR DELETE ON alergie
FOR EACH ROW
BEGIN
    IF INSERTING THEN
        INSERT INTO logy (id_log, tabulka, id_zaznam, akce, datum_cas, detail)
	VALUES (log_seq.NEXTVAL, 'ALERGIE', :NEW.id_alergie, 'INSERT', SYSDATE, NULL);
    ELSIF UPDATING THEN
        INSERT INTO logy (id_log, tabulka, id_zaznam, akce, datum_cas, detail)
	VALUES (log_seq.NEXTVAL, 'ALERGIE', :NEW.id_alergie, 'UPDATE', SYSDATE, NULL);
    ELSIF DELETING THEN
        INSERT INTO logy (id_log, tabulka, id_zaznam, akce, datum_cas, detail)
	VALUES (log_seq.NEXTVAL, 'ALERGIE', :OLD.id_alergie, 'DELETE', SYSDATE, NULL);
    END IF;
END;


CREATE OR REPLACE TRIGGER trg_str_alerg_log
AFTER INSERT OR UPDATE OR DELETE ON stravnici_alergie
FOR EACH ROW
DECLARE
    v_id VARCHAR2(100);
BEGIN
    v_id := TO_CHAR(NVL(:NEW.id_alergie, :OLD.id_alergie)) || '-' || TO_CHAR(NVL(:NEW.id_stravnik, :OLD.id_stravnik));

    IF INSERTING THEN
        INSERT INTO logy (id_log, tabulka, id_zaznam, akce, datum_cas, detail)
	VALUES (log_seq.NEXTVAL, 'STRAVNICI_ALERGIE', v_id, 'INSERT', SYSDATE, NULL);
    ELSIF UPDATING THEN
        INSERT INTO logy (id_log, tabulka, id_zaznam, akce, datum_cas, detail)
	VALUES (log_seq.NEXTVAL, 'STRAVNICI_ALERGIE', v_id, 'UPDATE', SYSDATE, NULL);
    ELSIF DELETING THEN
        INSERT INTO logy (id_log, tabulka, id_zaznam, akce, datum_cas, detail)
	VALUES (log_seq.NEXTVAL, 'STRAVNICI_ALERGIE', v_id, 'DELETE', SYSDATE, NULL);
    END IF;
END;


CREATE OR REPLACE TRIGGER trg_dietni_omezeni_log
AFTER INSERT OR UPDATE OR DELETE ON dietni_omezeni
FOR EACH ROW
BEGIN
    IF INSERTING THEN
        INSERT INTO logy (id_log, tabulka, id_zaznam, akce, datum_cas, detail)
	VALUES (log_seq.NEXTVAL, 'DIETNI_OMEZENI', :NEW.id_omezeni, 'INSERT', SYSDATE, NULL);
    ELSIF UPDATING THEN
        INSERT INTO logy (id_log, tabulka, id_zaznam, akce, datum_cas, detail)
	VALUES (log_seq.NEXTVAL, 'DIETNI_OMEZENI', :NEW.id_omezeni, 'UPDATE', SYSDATE, NULL);
    ELSIF DELETING THEN
        INSERT INTO logy (id_log, tabulka, id_zaznam, akce, datum_cas, detail)
	VALUES (log_seq.NEXTVAL, 'DIETNI_OMEZENI', :OLD.id_omezeni, 'DELETE', SYSDATE, NULL);
    END IF;
END;


CREATE OR REPLACE TRIGGER trg_str_omezeni_log
AFTER INSERT OR UPDATE OR DELETE ON stravnici_omezeni
FOR EACH ROW
DECLARE
    v_id VARCHAR2(100);
BEGIN
    v_id := TO_CHAR(NVL(:NEW.id_omezeni, :OLD.id_omezeni)) || '-' || TO_CHAR(NVL(:NEW.id_stravnik, :OLD.id_stravnik));

    IF INSERTING THEN
        INSERT INTO logy (id_log, tabulka, id_zaznam, akce, datum_cas, detail)
	VALUES (log_seq.NEXTVAL, 'STRAVNICI_OMEZENI', v_id, 'INSERT', SYSDATE, NULL);
    ELSIF UPDATING THEN
        INSERT INTO logy (id_log, tabulka, id_zaznam, akce, datum_cas, detail)
	VALUES (log_seq.NEXTVAL, 'STRAVNICI_OMEZENI', v_id, 'UPDATE', SYSDATE, NULL);
    ELSIF DELETING THEN
        INSERT INTO logy (id_log, tabulka, id_zaznam, akce, datum_cas, detail)
	VALUES (log_seq.NEXTVAL, 'STRAVNICI_OMEZENI', v_id, 'DELETE', SYSDATE, NULL);
    END IF;
END;


CREATE OR REPLACE TRIGGER trg_adresy_log
AFTER INSERT OR UPDATE OR DELETE ON adresy
FOR EACH ROW
BEGIN
    IF INSERTING THEN
        INSERT INTO logy (id_log, tabulka, id_zaznam, akce, datum_cas, detail)
	VALUES (log_seq.NEXTVAL, 'ADRESY', :NEW.id_adresa, 'INSERT', SYSDATE, NULL);
    ELSIF UPDATING THEN
        INSERT INTO logy (id_log, tabulka, id_zaznam, akce, datum_cas, detail)
	VALUES (log_seq.NEXTVAL, 'ADRESY', :NEW.id_adresa, 'UPDATE', SYSDATE, NULL);
    ELSIF DELETING THEN
        INSERT INTO logy (id_log, tabulka, id_zaznam, akce, datum_cas, detail)
	VALUES (log_seq.NEXTVAL, 'ADRESY', :OLD.id_adresa, 'DELETE', SYSDATE, NULL);
    END IF;
END;


CREATE OR REPLACE TRIGGER trg_stravnici_log
AFTER INSERT OR UPDATE OR DELETE ON stravnici
FOR EACH ROW
BEGIN
    IF INSERTING THEN
        INSERT INTO logy (id_log, tabulka, id_zaznam, akce, datum_cas, detail)
	VALUES (log_seq.NEXTVAL, 'STRAVNICI', :NEW.id_stravnik, 'INSERT', SYSDATE, NULL);
    ELSIF UPDATING THEN
        INSERT INTO logy (id_log, tabulka, id_zaznam, akce, datum_cas, detail)
	VALUES (log_seq.NEXTVAL, 'STRAVNICI', :NEW.id_stravnik, 'UPDATE', SYSDATE, NULL);
    ELSIF DELETING THEN
        INSERT INTO logy (id_log, tabulka, id_zaznam, akce, datum_cas, detail)
	VALUES (log_seq.NEXTVAL, 'STRAVNICI', :OLD.id_stravnik, 'DELETE', SYSDATE, NULL);
    END IF;
END;


CREATE OR REPLACE TRIGGER trg_jidla_log
AFTER INSERT OR UPDATE OR DELETE ON jidla
FOR EACH ROW
BEGIN
    IF INSERTING THEN
        INSERT INTO logy (id_log, tabulka, id_zaznam, akce, datum_cas, detail)
	VALUES (log_seq.NEXTVAL, 'JIDLA', :NEW.id_jidlo, 'INSERT', SYSDATE, NULL);
    ELSIF UPDATING THEN
        INSERT INTO logy (id_log, tabulka, id_zaznam, akce, datum_cas, detail)
	VALUES (log_seq.NEXTVAL, 'JIDLA', :NEW.id_jidlo, 'UPDATE', SYSDATE, NULL);
    ELSIF DELETING THEN
        INSERT INTO logy (id_log, tabulka, id_zaznam, akce, datum_cas, detail)
	VALUES (log_seq.NEXTVAL, 'JIDLA', :OLD.id_jidlo, 'DELETE', SYSDATE, NULL);
    END IF;
END;


CREATE OR REPLACE TRIGGER trg_objednavky_log
AFTER INSERT OR UPDATE OR DELETE ON objednavky
FOR EACH ROW
BEGIN
    IF INSERTING THEN
        INSERT INTO logy (id_log, tabulka, id_zaznam, akce, datum_cas, detail)
	VALUES (log_seq.NEXTVAL, 'OBJEDNAVKY', :NEW.id_objednavka, 'INSERT', SYSDATE, NULL);
    ELSIF UPDATING THEN
        INSERT INTO logy (id_log, tabulka, id_zaznam, akce, datum_cas, detail)
	VALUES (log_seq.NEXTVAL, 'OBJEDNAVKY', :NEW.id_objednavka, 'UPDATE', SYSDATE, NULL);
    ELSIF DELETING THEN
        INSERT INTO logy (id_log, tabulka, id_zaznam, akce, datum_cas, detail)
	VALUES (log_seq.NEXTVAL, 'OBJEDNAVKY', :OLD.id_objednavka, 'DELETE', SYSDATE, NULL);
    END IF;
END;


CREATE OR REPLACE TRIGGER trg_stavy_log
AFTER INSERT OR UPDATE OR DELETE ON stavy
FOR EACH ROW
BEGIN
    IF INSERTING THEN
        INSERT INTO logy (id_log, tabulka, id_zaznam, akce, datum_cas, detail)
	VALUES (log_seq.NEXTVAL, 'STAVY', :NEW.id_stav, 'INSERT', SYSDATE, NULL);
    ELSIF UPDATING THEN
        INSERT INTO logy (id_log, tabulka, id_zaznam, akce, datum_cas, detail)
	VALUES (log_seq.NEXTVAL, 'STAVY', :NEW.id_stav, 'UPDATE', SYSDATE, NULL);
    ELSIF DELETING THEN
        INSERT INTO logy (id_log, tabulka, id_zaznam, akce, datum_cas, detail)
	VALUES (log_seq.NEXTVAL, 'STAVY', :OLD.id_stav, 'DELETE', SYSDATE, NULL);
    END IF;
END;


CREATE OR REPLACE TRIGGER trg_polozky_log
AFTER INSERT OR UPDATE OR DELETE ON polozky
FOR EACH ROW
DECLARE
    v_id VARCHAR2(100);
BEGIN
    v_id := TO_CHAR(NVL(:NEW.id_jidlo, :OLD.id_jidlo)) || '-' || TO_CHAR(NVL(:NEW.id_objednavka, :OLD.id_objednavka));

    IF INSERTING THEN
        INSERT INTO logy (id_log, tabulka, id_zaznam, akce, datum_cas, detail)
	VALUES (log_seq.NEXTVAL, 'POLOZKY', v_id, 'INSERT', SYSDATE, NULL);
    ELSIF UPDATING THEN
        INSERT INTO logy (id_log, tabulka, id_zaznam, akce, datum_cas, detail)
	VALUES (log_seq.NEXTVAL, 'POLOZKY', v_id, 'UPDATE', SYSDATE, NULL);
    ELSIF DELETING THEN
        INSERT INTO logy (id_log, tabulka, id_zaznam, akce, datum_cas, detail)
	VALUES (log_seq.NEXTVAL, 'POLOZKY', v_id, 'DELETE', SYSDATE, NULL);
    END IF;
END;


CREATE OR REPLACE TRIGGER trg_menu_log
AFTER INSERT OR UPDATE OR DELETE ON menu
FOR EACH ROW
BEGIN
    IF INSERTING THEN
        INSERT INTO logy (id_log, tabulka, id_zaznam, akce, datum_cas, detail)
	VALUES (log_seq.NEXTVAL, 'MENU', :NEW.id_menu, 'INSERT', SYSDATE, NULL);
    ELSIF UPDATING THEN
        INSERT INTO logy (id_log, tabulka, id_zaznam, akce, datum_cas, detail)
	VALUES (log_seq.NEXTVAL, 'MENU', :NEW.id_menu, 'UPDATE', SYSDATE, NULL);
    ELSIF DELETING THEN
        INSERT INTO logy (id_log, tabulka, id_zaznam, akce, datum_cas, detail)
	VALUES (log_seq.NEXTVAL, 'MENU', :OLD.id_menu, 'DELETE', SYSDATE, NULL);
    END IF;
END;


CREATE OR REPLACE TRIGGER trg_platby_log
AFTER INSERT OR UPDATE OR DELETE ON platby
FOR EACH ROW
BEGIN
    IF INSERTING THEN
        INSERT INTO logy (id_log, tabulka, id_zaznam, akce, datum_cas, detail)
	VALUES (log_seq.NEXTVAL, 'PLATBY', :NEW.id_platba, 'INSERT', SYSDATE, NULL);
    ELSIF UPDATING THEN
        INSERT INTO logy (id_log, tabulka, id_zaznam, akce, datum_cas, detail)
	VALUES (log_seq.NEXTVAL, 'PLATBY', :NEW.id_platba, 'UPDATE', SYSDATE, NULL);
    ELSIF DELETING THEN
        INSERT INTO logy (id_log, tabulka, id_zaznam, akce, datum_cas, detail)
	VALUES (log_seq.NEXTVAL, 'PLATBY', :OLD.id_platba, 'DELETE', SYSDATE, NULL);
    END IF;
END;


CREATE OR REPLACE TRIGGER trg_slozky_log
AFTER INSERT OR UPDATE OR DELETE ON slozky
FOR EACH ROW
BEGIN
    IF INSERTING THEN
        INSERT INTO logy (id_log, tabulka, id_zaznam, akce, datum_cas, detail)
	VALUES (log_seq.NEXTVAL, 'SLOZKY', :NEW.id_slozka, 'INSERT', SYSDATE, NULL);
    ELSIF UPDATING THEN
        INSERT INTO logy (id_log, tabulka, id_zaznam, akce, datum_cas, detail)
	VALUES (log_seq.NEXTVAL, 'SLOZKY', :NEW.id_slozka, 'UPDATE', SYSDATE, NULL);
    ELSIF DELETING THEN
        INSERT INTO logy (id_log, tabulka, id_zaznam, akce, datum_cas, detail)
	VALUES (log_seq.NEXTVAL, 'SLOZKY', :OLD.id_slozka, 'DELETE', SYSDATE, NULL);
    END IF;
END;


CREATE OR REPLACE TRIGGER trg_slozky_jidla_log
AFTER INSERT OR UPDATE OR DELETE ON slozky_jidla
FOR EACH ROW
DECLARE
    v_id VARCHAR2(100);
BEGIN
    v_id := TO_CHAR(NVL(:NEW.id_slozka, :OLD.id_slozka)) || '-' || TO_CHAR(NVL(:NEW.id_jidlo, :OLD.id_jidlo));

    IF INSERTING THEN
        INSERT INTO logy (id_log, tabulka, id_zaznam, akce, datum_cas, detail)
	VALUES (log_seq.NEXTVAL, 'SLOZKY_JIDLA', v_id, 'INSERT', SYSDATE, NULL);
    ELSIF UPDATING THEN
        INSERT INTO logy (id_log, tabulka, id_zaznam, akce, datum_cas, detail)
	VALUES (log_seq.NEXTVAL, 'SLOZKY_JIDLA', v_id, 'UPDATE', SYSDATE, NULL);
    ELSIF DELETING THEN
        INSERT INTO logy (id_log, tabulka, id_zaznam, akce, datum_cas, detail)
	VALUES (log_seq.NEXTVAL, 'SLOZKY_JIDLA', v_id, 'DELETE', SYSDATE, NULL);
    END IF;
END;


CREATE OR REPLACE TRIGGER trg_soubory_log
AFTER INSERT OR UPDATE OR DELETE ON soubory
FOR EACH ROW
BEGIN
    IF INSERTING THEN
        INSERT INTO logy (id_log, tabulka, id_zaznam, akce, datum_cas, detail)
	VALUES (log_seq.NEXTVAL, 'SOUBORY', :NEW.id_soubor, 'INSERT', SYSDATE, NULL);
    ELSIF UPDATING THEN
        INSERT INTO logy (id_log, tabulka, id_zaznam, akce, datum_cas, detail)
	VALUES (log_seq.NEXTVAL, 'SOUBORY', :NEW.id_soubor, 'UPDATE', SYSDATE, NULL);
    ELSIF DELETING THEN
        INSERT INTO logy (id_log, tabulka, id_zaznam, akce, datum_cas, detail)
	VALUES (log_seq.NEXTVAL, 'SOUBORY', :OLD.id_soubor, 'DELETE', SYSDATE, NULL);
    END IF;
END;


CREATE OR REPLACE TRIGGER trg_studenti_log
AFTER INSERT OR UPDATE OR DELETE ON studenti
FOR EACH ROW
BEGIN
    IF INSERTING THEN
        INSERT INTO logy (id_log, tabulka, id_zaznam, akce, datum_cas, detail)
        VALUES (log_seq.NEXTVAL, 'STUDENTI', :NEW.id_stravnik, 'INSERT', SYSDATE, NULL);
    ELSIF UPDATING THEN
        INSERT INTO logy (id_log, tabulka, id_zaznam, akce, datum_cas, detail)
        VALUES (log_seq.NEXTVAL, 'STUDENTI', :NEW.id_stravnik, 'UPDATE', SYSDATE, NULL);
    ELSIF DELETING THEN
        INSERT INTO logy (id_log, tabulka, id_zaznam, akce, datum_cas, detail)
        VALUES (log_seq.NEXTVAL, 'STUDENTI', :OLD.id_stravnik, 'DELETE', SYSDATE, NULL);
    END IF;
END;


CREATE OR REPLACE TRIGGER trg_pracovnici_log
AFTER INSERT OR UPDATE OR DELETE ON pracovnici
FOR EACH ROW
BEGIN
    IF INSERTING THEN
        INSERT INTO logy (id_log, tabulka, id_zaznam, akce, datum_cas, detail)
        VALUES (log_seq.NEXTVAL, 'PRACOVNICI', :NEW.id_stravnik, 'INSERT', SYSDATE, NULL);
    ELSIF UPDATING THEN
        INSERT INTO logy (id_log, tabulka, id_zaznam, akce, datum_cas, detail)
        VALUES (log_seq.NEXTVAL, 'PRACOVNICI', :NEW.id_stravnik, 'UPDATE', SYSDATE, NULL);
    ELSIF DELETING THEN
        INSERT INTO logy (id_log, tabulka, id_zaznam, akce, datum_cas, detail)
        VALUES (log_seq.NEXTVAL, 'PRACOVNICI', :OLD.id_stravnik, 'DELETE', SYSDATE, NULL);
    END IF;
END;