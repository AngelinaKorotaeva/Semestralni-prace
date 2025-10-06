CREATE SEQUENCE s_str START WITH 1 INCREMENT BY 1 NOCACHE; 

CREATE OR REPLACE TRIGGER t_str_id 
BEFORE INSERT ON stravnici 
REFERENCING NEW AS NEW FOR EACH ROW 
BEGIN  
    if(:new.id_stravnik is null) then 
    SELECT s_str.nextval 
    INTO :new.id_stravnik
    FROM dual; 
  end if; 
END; 

ALTER TRIGGER T_STR_ID ENABLE


CREATE SEQUENCE s_plat START WITH 1 INCREMENT BY 1 NOCACHE; 

CREATE OR REPLACE TRIGGER t_plat_id
BEFORE INSERT ON platby 
REFERENCING NEW AS NEW FOR EACH ROW 
BEGIN  
    if(:new.id_platba is null) then 
    SELECT s_plat.nextval 
    INTO :new.id_platba 
    FROM dual; 
  end if; 
END; 

ALTER TRIGGER t_plat_id ENABLE


CREATE SEQUENCE s_obj START WITH 1 INCREMENT BY 1 NOCACHE; 

CREATE OR REPLACE TRIGGER t_obj_id 
BEFORE INSERT ON objednavky 
REFERENCING NEW AS NEW FOR EACH ROW 
BEGIN  
    if(:new.id_objednavka is null) then 
    SELECT s_obj.nextval 
    INTO :new.id_objednavka 
    FROM dual; 
  end if; 
END; 

ALTER TRIGGER t_obj_id ENABLE


CREATE SEQUENCE s_jid START WITH 1 INCREMENT BY 1 NOCACHE; 

CREATE OR REPLACE TRIGGER t_jid_id 
BEFORE INSERT ON jidla 
REFERENCING NEW AS NEW FOR EACH ROW 
BEGIN  
    if(:new.id_jidlo is null) then 
    SELECT s_jid.nextval 
    INTO :new.id_jidlo 
    FROM dual; 
  end if; 
END; 

ALTER TRIGGER t_jid_id ENABLE


CREATE SEQUENCE s_menu START WITH 1 INCREMENT BY 1 NOCACHE; 

CREATE OR REPLACE TRIGGER t_menu_id 
BEFORE INSERT ON menu 
REFERENCING NEW AS NEW FOR EACH ROW 
BEGIN  
    if(:new.id_menu is null) then 
    SELECT s_menu.nextval 
    INTO :new.id_menu 
    FROM dual; 
  end if; 
END; 

ALTER TRIGGER t_menu_id ENABLE


CREATE SEQUENCE s_adr START WITH 1 INCREMENT BY 1 NOCACHE; 

CREATE OR REPLACE TRIGGER t_adr_id 
BEFORE INSERT ON adresy 
REFERENCING NEW AS NEW FOR EACH ROW 
BEGIN  
    if(:new.id_adresa is null) then 
    SELECT s_adr.nextval 
    INTO :new.id_adresa
    FROM dual; 
  end if; 
END; 

ALTER TRIGGER t_adr_id ENABLE


CREATE SEQUENCE s_aler START WITH 1 INCREMENT BY 1 NOCACHE; 

CREATE OR REPLACE TRIGGER t_aler_id 
BEFORE INSERT ON alergie 
REFERENCING NEW AS NEW FOR EACH ROW 
BEGIN  
    if(:new.id_alergie is null) then 
    SELECT s_aler.nextval 
    INTO :new.id_alergie
    FROM dual; 
  end if; 
END; 

ALTER TRIGGER t_aler_id ENABLE


CREATE SEQUENCE s_omez START WITH 1 INCREMENT BY 1 NOCACHE; 

CREATE OR REPLACE TRIGGER t_omez_id 
BEFORE INSERT ON dietni_omezeni
REFERENCING NEW AS NEW FOR EACH ROW 
BEGIN  
    if(:new.id_omez is null) then 
    SELECT s_omez.nextval 
    INTO :new.id_omezeni
    FROM dual; 
  end if; 
END; 

ALTER TRIGGER t_omez_id ENABLE


CREATE SEQUENCE s_sloz START WITH 1 INCREMENT BY 1 NOCACHE; 

CREATE OR REPLACE TRIGGER t_sloz_id 
BEFORE INSERT ON slozky
REFERENCING NEW AS NEW FOR EACH ROW 
BEGIN  
    if(:new.id_slozka is null) then 
    SELECT s_sloz.nextval 
    INTO :new.id_slozka
    FROM dual; 
  end if; 
END; 

ALTER TRIGGER t_sloz_id ENABLE

CREATE INDEX str_id_idx ON stravnici(id_stravnik);  
CREATE INDEX plat_id_idx ON platby(id_platba);  
CREATE INDEX obj_id_idx ON objednavky(id_odjednavka);  
CREATE INDEX jid_id_idx ON jidla(id_jidlo);  
CREATE INDEX menu_id_idx ON menu(id_menu); 
CREATE INDEX adr_id_idx ON adresy(id_adres); 
CREATE INDEX aler_id_idx ON alergie(id_alergie); 
CREATE INDEX omez_id_idx ON dietni_omezeni(id_omezeni); 
CREATE INDEX sloz_id_idx ON slozky(id_slozka); 
