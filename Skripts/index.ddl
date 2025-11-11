CREATE INDEX obj_str_fk_idx ON objednavky(id_stravnik);
CREATE INDEX obj_stav_fk_idx ON objednavky(id_stav);

CREATE INDEX plat_str_fk_idx ON platby(id_stravnik);

CREATE INDEX jdl_menu_fk_idx ON jidla(id_menu);

CREATE INDEX slozky_jdl_fk_idx ON slozky_jidla(id_jidlo);
CREATE INDEX jdl_slozk_fk_idx ON slozky_jidla(id_slozka);

CREATE INDEX omez_str_fk_idx ON stravnici_omezeni(id_omezeni);
CREATE INDEX str_omez_fk_idx ON stravnici_omezeni(id_stravnik);

CREATE INDEX alerg_str_fk_idx ON stravnici_alergie(id_alergie);
CREATE INDEX str_alerg_fk_idx ON stravnici_alergie(id_stravnik);

CREATE INDEX st_trid_fk_idx ON studenti(id_trida);
CREATE INDEX st_str_fk_idx ON studenti(id_stravnik);

CREATE INDEX pr_pozice_fk_idx ON pracovnici(id_pozice);
CREATE INDEX pr_str_fk_idx ON pracovnici(id_stravnik);
