-- Read-only checks as FQOWNER after compiling the existing package specification/body.
SELECT name,type,line,position,text FROM user_errors
WHERE name IN ('FQ_PROCS_GET','FQ_PROCS_ADMIN')
ORDER BY name,sequence;
SELECT user_id,queue_id,lobby_flag FROM user_permissions WHERE lobby_flag='Y';
SELECT argument_name,position,data_type,in_out FROM user_arguments
WHERE package_name='FQ_PROCS_ADMIN' AND object_name='UPSERT_USER'
ORDER BY sequence;
-- As FQUSER, replace values and run with F5. End date is inclusive.
-- VARIABLE rows REFCURSOR;
-- EXEC FQOWNER.FQ_PROCS_GET.GET_MYAPPOINTMENTS(1, 'windows_account', TRUNC(SYSDATE), TRUNC(SYSDATE), :rows);
-- PRINT rows;
-- EXEC FQOWNER.FQ_PROCS_GET.GET_MYWALKINS(1, 'windows_account', TRUNC(SYSDATE), TRUNC(SYSDATE), :rows);
-- PRINT rows;

-- Diagnose user-edit result columns (SQL Developer: Run Script/F5).
-- Replace the two user IDs. PRINT output must include LOBBY_FLAG along with
-- QUEUE_ID, NAME, ENTITY_ID, ACTIVEFLAG and the existing role flags.
-- VARIABLE user_roles REFCURSOR;
-- VARIABLE lookup_message VARCHAR2(2000);
-- EXEC FQOWNER.FQ_PROCS_GET.GET_USER_QUEUES_ROLES('target_user', 'admin_user', :lookup_message, :user_roles);
-- PRINT lookup_message;
-- PRINT user_roles;
