-- Run as FQOWNER. Existing Windows authentication and USER_PERMISSIONS role structure.
WHENEVER SQLERROR EXIT SQL.SQLCODE ROLLBACK;
DECLARE
 v_exists NUMBER;
BEGIN
 SELECT COUNT(*) INTO v_exists FROM user_tab_columns
 WHERE table_name='USER_PERMISSIONS' AND column_name='LOBBY_FLAG';
 IF v_exists=0 THEN
  EXECUTE IMMEDIATE 'ALTER TABLE USER_PERMISSIONS ADD (LOBBY_FLAG CHAR(1) DEFAULT ''N'' NOT NULL CONSTRAINT CK_UP_LOBBY CHECK (LOBBY_FLAG IN (''Y'',''N'')))';
 END IF;
END;
/

CREATE OR REPLACE PROCEDURE FQ_SAVE_LOBBY_ROLES(
 p_userid VARCHAR2,p_entityid NUMBER,p_queueids VARCHAR2,p_stampuser VARCHAR2)
AUTHID DEFINER AS
 v_count NUMBER;
 v_queue NUMBER;
BEGIN
 SAVEPOINT lobby_role_save;
 SELECT COUNT(*) INTO v_count FROM USER_ENTITIES ue JOIN FQ_USERS u ON LOWER(u.user_id)=LOWER(ue.user_id)
 JOIN VALIDENTITIES e ON e.entity_id=ue.entity_id
 WHERE LOWER(ue.user_id)=LOWER(p_stampuser) AND ue.entity_id=p_entityid
 AND ue.adminflag='Y' AND ue.activeflag='Y' AND u.activeflag='Y' AND e.activeflag='Y';
 IF v_count=0 THEN RAISE_APPLICATION_ERROR(-20002,'Entity administrator access required'); END IF;
 SELECT COUNT(*) INTO v_count FROM USER_ENTITIES WHERE LOWER(user_id)=LOWER(p_userid) AND entity_id=p_entityid;
 IF v_count=0 THEN RAISE_APPLICATION_ERROR(-20003,'User does not belong to this entity'); END IF;
 UPDATE USER_PERMISSIONS SET lobby_flag='N',stampuser=LOWER(p_stampuser),stampdate=SYSDATE
 WHERE LOWER(user_id)=LOWER(p_userid) AND queue_id IN (SELECT queue_id FROM VALIDQUEUES WHERE entity_id=p_entityid);
 FOR r IN (SELECT REGEXP_SUBSTR(p_queueids,'[^,]+',1,LEVEL) id FROM dual
           CONNECT BY LEVEL<=REGEXP_COUNT(p_queueids,',')+1) LOOP
  IF TRIM(r.id) IS NOT NULL THEN
   v_queue:=TO_NUMBER(TRIM(r.id));
   SELECT COUNT(*) INTO v_count FROM VALIDQUEUES WHERE queue_id=v_queue AND entity_id=p_entityid AND activeflag='Y';
   IF v_count=0 THEN RAISE_APPLICATION_ERROR(-20003,'Invalid lobby queue'); END IF;
   MERGE INTO USER_PERMISSIONS p USING (SELECT LOWER(p_userid) user_id,v_queue queue_id FROM dual) x
   ON (LOWER(p.user_id)=x.user_id AND p.queue_id=x.queue_id)
   WHEN MATCHED THEN UPDATE SET lobby_flag='Y',stampuser=LOWER(p_stampuser),stampdate=SYSDATE
   WHEN NOT MATCHED THEN INSERT (user_id,queue_id,lobby_flag,stampuser,stampdate)
   VALUES (x.user_id,x.queue_id,'Y',LOWER(p_stampuser),SYSDATE);
  END IF;
 END LOOP;
 DELETE FROM USER_PERMISSIONS
 WHERE LOWER(user_id)=LOWER(p_userid)
 AND queue_id IN (SELECT queue_id FROM VALIDQUEUES WHERE entity_id=p_entityid)
 AND NVL(host_flag,'N')='N' AND NVL(provider_flag,'N')='N'
 AND NVL(reporter_flag,'N')='N' AND NVL(queueadmin_flag,'N')='N' AND lobby_flag='N';
EXCEPTION
 WHEN OTHERS THEN
  ROLLBACK TO lobby_role_save;
  RAISE;
END;
/
SHOW ERRORS PROCEDURE FQ_SAVE_LOBBY_ROLES;
GRANT EXECUTE ON FQ_SAVE_LOBBY_ROLES TO FQUSER;

CREATE OR REPLACE PROCEDURE FQ_GET_LOBBY(p_userid VARCHAR2,p_entityid NUMBER,p_cur OUT SYS_REFCURSOR)
AUTHID DEFINER AS
 v_count NUMBER;
 v_today DATE := TRUNC(CAST(SYSTIMESTAMP AT TIME ZONE 'America/New_York' AS DATE));
BEGIN
 SELECT COUNT(*) INTO v_count FROM USER_ENTITIES ue JOIN FQ_USERS u ON LOWER(u.user_id)=LOWER(ue.user_id)
 JOIN VALIDENTITIES e ON e.entity_id=ue.entity_id
 WHERE LOWER(ue.user_id)=LOWER(p_userid) AND ue.entity_id=p_entityid
 AND ue.activeflag='Y' AND u.activeflag='Y' AND e.activeflag='Y'
 AND (ue.adminflag='Y' OR EXISTS (SELECT 1 FROM USER_PERMISSIONS p JOIN VALIDQUEUES q ON q.queue_id=p.queue_id WHERE LOWER(p.user_id)=LOWER(p_userid) AND p.lobby_flag='Y' AND q.entity_id=p_entityid AND q.activeflag='Y'));
 IF v_count=0 THEN RAISE_APPLICATION_ERROR(-20002,'Lobby access required'); END IF;
 OPEN p_cur FOR
 WITH entries AS (
  SELECT 'A' src_type,a.appointment_id src_id,a.queue_id,a.customer_id,a.status,
   CAST(TRUNC(a.appt_date) + a.start_time AS DATE) visit_time
  FROM APPOINTMENTS a WHERE a.appt_date>=v_today AND a.appt_date<v_today+1
  AND UPPER(TRIM(a.status)) IN ('ARRIVED','IN PROGRESS')
  UNION ALL
  SELECT 'W',w.walkin_id,w.queue_id,w.customer_id,w.status,w.createdon
  FROM WALKINS w WHERE w.createdon>=v_today AND w.createdon<v_today+1
  AND UPPER(TRIM(w.status)) IN ('ARRIVED','IN PROGRESS')
 )
 SELECT q.queue_id,q.name queue_name,x.src_type,x.src_id,
  CASE WHEN x.src_id IS NOT NULL THEN TRIM(FQ_CRYPTO_PKG.DECRYPT(c.fname)||' '||FQ_CRYPTO_PKG.DECRYPT(c.lname)) END customer_name,
  UPPER(TRIM(x.status)) status,CASE WHEN x.src_id IS NOT NULL THEN NVL(TO_CHAR(x.visit_time,'HH12:MI AM'),'Not scheduled') END time_text
 FROM VALIDQUEUES q LEFT JOIN entries x ON x.queue_id=q.queue_id
 LEFT JOIN CUSTOMERS c ON c.customer_id=x.customer_id
 WHERE q.entity_id=p_entityid AND q.activeflag='Y' AND NVL(q.hide_in_monitor,'N')<>'Y'
 AND (EXISTS (SELECT 1 FROM USER_ENTITIES ue WHERE LOWER(ue.user_id)=LOWER(p_userid) AND ue.entity_id=p_entityid AND ue.adminflag='Y' AND ue.activeflag='Y')
 OR EXISTS (SELECT 1 FROM USER_PERMISSIONS p WHERE LOWER(p.user_id)=LOWER(p_userid) AND p.queue_id=q.queue_id AND p.lobby_flag='Y'))
 ORDER BY q.name,x.visit_time,x.src_type,x.src_id;
END;
/
SHOW ERRORS PROCEDURE FQ_GET_LOBBY;
GRANT EXECUTE ON FQ_GET_LOBBY TO FQUSER;

-- CREATE PROCEDURE can finish with compilation warnings; fail deployment explicitly.
DECLARE
 v_errors NUMBER;
BEGIN
 SELECT COUNT(*) INTO v_errors FROM user_errors
 WHERE name IN ('FQ_SAVE_LOBBY_ROLES','FQ_GET_LOBBY') AND attribute='ERROR';
 IF v_errors>0 THEN RAISE_APPLICATION_ERROR(-20005,'Lobby procedures have compilation errors; inspect USER_ERRORS'); END IF;
END;
/
