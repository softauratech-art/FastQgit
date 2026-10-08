-- Read-only checks as FQOWNER after deploying the SQL and package bodies.
SELECT name,type,line,position,text FROM user_errors
WHERE name IN ('FQ_SAVE_LOBBY_ROLES','FQ_GET_LOBBY','FQ_PROCS_GET','FQ_PROCS_ADMIN')
ORDER BY name,sequence;
SELECT user_id,queue_id,lobby_flag FROM user_permissions WHERE lobby_flag='Y';
SELECT table_name,grantee,privilege FROM user_tab_privs
WHERE table_name IN ('FQ_SAVE_LOBBY_ROLES','FQ_GET_LOBBY') AND grantee='FQUSER';
-- As FQUSER, replace both values and run with F5:
-- VARIABLE rows REFCURSOR;
-- EXEC FQOWNER.FQ_GET_LOBBY('windows_account', 1, :rows);
-- PRINT rows;
