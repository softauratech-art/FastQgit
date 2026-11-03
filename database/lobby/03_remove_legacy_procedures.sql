-- Optional cleanup as FQOWNER ONLY AFTER all web instances use the existing packages.
WHENEVER SQLERROR EXIT SQL.SQLCODE;
BEGIN
 FOR r IN (SELECT object_name FROM user_objects WHERE object_type='PROCEDURE'
           AND object_name IN ('FQ_SAVE_LOBBY_ROLES','FQ_GET_LOBBY')) LOOP
  EXECUTE IMMEDIATE 'DROP PROCEDURE ' || r.object_name;
 END LOOP;
END;
/
