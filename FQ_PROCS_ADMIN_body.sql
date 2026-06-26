create or replace PACKAGE BODY FQ_PROCS_ADMIN AS
FUNCTION IS_SUPER_ADMIN (p_entityid VALIDQUEUES.ENTITY_ID%type, p_userid varchar2)
RETURN NUMBER
AS
v_result NUMBER(4) := 0;
BEGIN  
--    SELECT count(*) INTO v_result
--    FROM VALIDQUEUES Q
--        INNER JOIN USER_ENTITIES E
--         ON Q.entity_id = E.entity_id
--    WHERE lower(E.user_id) = lower(p_userid)
--        AND Q.QUEUE_ID = p_entityid
--        AND NVL(E.ADMINFLAG, 'N') = 'Y';
    SELECT count(*) INTO v_result
        FROM USER_ENTITIES E
        WHERE lower(E.user_id) = lower(p_userid)
        AND E.ENTITY_ID = p_entityid
        AND NVL(E.ADMINFLAG, 'N') = 'Y';
       
    IF v_result > 0 THEN
        v_result := 1;  -- FALSE:0 | TRUE: 1
    END IF;
   
    RETURN v_result;
END;

FUNCTION IS_QUEUE_ADMIN (p_queueid VALIDQUEUES.QUEUE_ID%type, p_userid varchar2)
RETURN NUMBER
AS
v_result NUMBER(4) := 0;
BEGIN
    SELECT count(*) INTO v_result
    FROM USER_PERMISSIONS
    WHERE lower(user_id) = lower(p_userid)
        AND QUEUE_ID = p_queueid
        AND NVL(QUEUEADMIN_FLAG, 'N') = 'Y';
   
    IF v_result = 0 THEN
        SELECT count(*) INTO v_result
        FROM VALIDQUEUES Q
            INNER JOIN USER_ENTITIES E
             ON Q.entity_id = E.entity_id
        WHERE lower(E.user_id) = lower(p_userid)
            AND Q.QUEUE_ID = p_queueid
            AND NVL(E.ADMINFLAG, 'N') = 'Y';
    END IF;
   
    IF v_result > 0 THEN
        v_result := 1;  -- FALSE:0 | TRUE: 1
    END IF;
   
    RETURN v_result;
END;

PROCEDURE UPSERT_QSERVICE (
    p_serviceid  IN NUMBER,
    p_queueid IN NUMBER,
    p_name IN VARCHAR2,
    p_namees IN VARCHAR2,
    p_namecp IN VARCHAR2,
    p_active IN VARCHAR2,
    p_stampuser IN VARCHAR2,
    p_out OUT VARCHAR2
)
AS
v_serviceid NUMBER;
BEGIN
    IF IS_QUEUE_ADMIN(p_queueid, p_stampuser) = 0 THEN
        RETURN;
    END IF;
   
    if p_serviceid = 0 then
        v_serviceid := QSERVICESEQ.NextVal;
        INSERT INTO VALIDQUEUE_SERVICES
            (SERVICE_ID, QUEUE_ID, ACTIVEFLAG, SERVICE_NAME, SERVICE_NAME_ES, SERVICE_NAME_CP, STAMPUSER)
        VALUES
            (v_serviceid, p_queueid, p_active, p_name, p_namees, p_namecp, p_stampuser);
    else
        UPDATE VALIDQUEUE_SERVICES
        SET service_name = p_name,
            service_name_es = p_namees,
            service_name_cp = p_namecp,
            activeflag = p_active,
            STAMPUSER = p_stampuser,
            STAMPDATE = SYSDATE
        WHERE
            service_id = p_serviceid and queue_id = p_queueid;
    end if;
EXCEPTION
    WHEN OTHERS THEN
        p_out := SQLERRM;
END;

procedure DELETE_QSERVICE (
    p_serviceid  IN NUMBER,
    p_stampuser IN VARCHAR2,
    p_out OUT VARCHAR2
)
AS
v_exists NUMBER;
BEGIN    
    SELECT CASE
        WHEN EXISTS(SELECT 1 FROM appointments WHERE service_id = p_serviceid)
            OR EXISTS(SELECT 1 FROM walkins WHERE service_id = p_serviceid)
            OR EXISTS(SELECT 1 FROM servicetransactions WHERE service_id = p_serviceid)
    THEN 1
    ELSE 0
    END INTO v_exists
    FROM DUAL;

    IF v_exists > 0 THEN
        UPDATE VALIDQUEUE_SERVICES
        SET activeflag = 'N'
        WHERE
                service_id = p_serviceid
            AND FQ_PROCS_ADMIN.is_queue_admin(queue_id, p_stampuser) = 1;
           
        --p_out := 'Service deactivated';
    ELSE
        DELETE FROM VALIDQUEUE_SERVICES        
        WHERE service_id = p_serviceid
            AND FQ_PROCS_ADMIN.is_queue_admin(queue_id, p_stampuser) = 1;
       
        IF SQL%ROWCOUNT > 0 THEN    
            UPDATE_DELETION_LOG('VALIDQUEUE_SERVICES','SERVICE_ID', p_serviceid, p_stampuser);
        END IF;
       
    END IF;

EXCEPTION
    WHEN OTHERS THEN
        p_out := 'Err: ' || SQLERRM;
END;

PROCEDURE UPSERT_QSCHEDULE (
    p_scheduleid  IN NUMBER,
    p_queueid IN NUMBER,
    p_datebegin IN VARCHAR2,
    p_dateend IN VARCHAR2,
    p_opentime IN VARCHAR2,
    p_closetime IN VARCHAR2,
    p_duration IN VARCHAR2,
    p_weekdays IN VARCHAR2,
    p_availres IN NUMBER := 1,
    p_stampuser IN VARCHAR2,
    p_out OUT VARCHAR2
)
AS
v_scheduleid NUMBER;
BEGIN
    IF IS_QUEUE_ADMIN(p_queueid, p_stampuser) = 0 THEN
        RETURN;
    END IF;
   
    if p_scheduleid = 0 then
        v_scheduleid := QSCHEDULESEQ.NextVal;
        INSERT INTO QUEUE_SCHEDULES
            (SCHEDULE_ID, QUEUE_ID, DATE_BEGIN, DATE_END, OPEN_TIME, CLOSE_TIME,
             INTERVAL_TIME, WEEKLY_SCH, AVAILABLE_RESOURCES, STAMPUSER)
        VALUES
            (v_scheduleid, p_queueid, to_date(p_datebegin, 'MM/DD/YYYY'),
             to_date(p_dateend, 'MM/DD/YYYY'), p_opentime, p_closetime,
             p_duration, p_weekdays, p_availres, p_stampuser);
    else
        UPDATE QUEUE_SCHEDULES
        SET DATE_BEGIN = to_date(p_datebegin, 'MM/DD/YYYY'),
            DATE_END = to_date(p_dateend, 'MM/DD/YYYY'),
            OPEN_TIME = p_opentime,
            CLOSE_TIME = p_closetime,
            INTERVAL_TIME = p_duration,
            WEEKLY_SCH = p_weekdays,
            AVAILABLE_RESOURCES = p_availres,
            STAMPUSER = p_stampuser,
            STAMPDATE = SYSDATE
        WHERE
            SCHEDULE_ID = p_scheduleid and queue_id = p_queueid;
    end if;
EXCEPTION
    WHEN OTHERS THEN
        p_out := SQLERRM;
END;

PROCEDURE DELETE_QSCHEDULE (
    p_scheduleid  IN NUMBER,
    p_stampuser IN VARCHAR2,
    p_out OUT VARCHAR2
)
AS
BEGIN
      -- Delete if user has permission to this Queue
      DELETE FROM QUEUE_SCHEDULES        
        WHERE schedule_id = p_scheduleid
         AND IS_QUEUE_ADMIN(queue_id, p_stampuser) = 1;
         
        IF SQL%ROWCOUNT > 0 THEN
            UPDATE_DELETION_LOG('QUEUE_SCHEDULES','SCHEDULE_ID', p_scheduleid, p_stampuser);
        END IF;
         
EXCEPTION
    WHEN OTHERS THEN
        p_out := 'Err: ' || SQLERRM;
END;

PROCEDURE DELETE_QUEUE (
    p_queueid   IN NUMBER,
    p_stampuser IN VARCHAR2,
    p_out OUT VARCHAR2
)
AS
v_exists NUMBER;
BEGIN
    IF IS_QUEUE_ADMIN(p_queueid, p_stampuser) = 0 THEN
        RETURN;
    END IF;
   
    SELECT CASE
        WHEN EXISTS(SELECT 1 FROM appointments WHERE queue_id = p_queueid)
            OR EXISTS(SELECT 1 FROM walkins WHERE queue_id = p_queueid)
            OR EXISTS(SELECT 1 FROM servicetransactions WHERE queue_id = p_queueid)
    THEN 1
    ELSE 0
    END INTO v_exists
    FROM DUAL;

    -- If queue is in-use by historical data, throw error to deactivate
    -- the Queue from future use, else delete
    IF v_exists > 0 THEN
        /*
        UPDATE VALIDQUEUES
        SET activeflag = 'N'
        WHERE
            queue_id = p_queueid;
        */            
        p_out := 'Appointment/Walk-in records exist for this queue. Please deactivate the queue instead of deletion.';
    ELSE  
        --
        DELETE FROM USER_PERMISSIONS
        WHERE queue_id = p_queueid;
       
        IF SQL%ROWCOUNT > 0 THEN
            UPDATE_DELETION_LOG('USER_PERMISSIONS','queue_id', p_queueid, p_stampuser);
        END IF;
        --
        DELETE FROM QUEUE_SCHEDULES
        WHERE queue_id = p_queueid;
       
        IF SQL%ROWCOUNT > 0 THEN
            UPDATE_DELETION_LOG('QUEUE_SCHEDULES','p_queueid', p_queueid, p_stampuser);
        END IF;
        ---
        DELETE FROM QUEUE_CONTACTTYPES
        WHERE queue_id = p_queueid;
       
        IF SQL%ROWCOUNT > 0 THEN
            UPDATE_DELETION_LOG('QUEUE_CONTACTTYPES','queue_id', p_queueid, p_stampuser);
        END IF;
        ---
        DELETE FROM QUEUE_REFCRITERIAS
        WHERE queue_id = p_queueid;
       
        IF SQL%ROWCOUNT > 0 THEN
            UPDATE_DELETION_LOG('QUEUE_REFCRITERIAS','queue_id', p_queueid, p_stampuser);
        END IF;
        ---
        DELETE FROM VALIDQUEUE_SERVICES
        WHERE queue_id = p_queueid;
 
        IF SQL%ROWCOUNT > 0 THEN
            UPDATE_DELETION_LOG('VALIDQUEUE_SERVICES','queue_id', p_queueid, p_stampuser);
        END IF;
        ---
        DELETE FROM VALIDQUEUES        
        WHERE queue_id = p_queueid;
       
        IF SQL%ROWCOUNT > 0 THEN
            UPDATE_DELETION_LOG('VALIDQUEUES','queue_id', p_queueid, p_stampuser);
        END IF;
        ---
       
    END IF;
EXCEPTION
    WHEN OTHERS THEN
        p_out := 'Err: ' || SQLERRM;
END;

PROCEDURE UPSERT_QUEUE (
    p_queueid NUMBER,
    p_entityid IN NUMBER :=1,
    p_name IN VARCHAR2,
    p_namees IN VARCHAR2,
    p_namecp IN VARCHAR2,
    p_address IN VARCHAR2,
    p_phone IN VARCHAR2,  
    p_activeflag IN VARCHAR2,    
    p_emponly IN VARCHAR2,
    p_hasguidelines IN VARCHAR2,
    p_hasuploads IN VARCHAR2,
    p_hideinkiosk IN VARCHAR2,
    p_hideinmonitor IN VARCHAR2,
    p_leadtimemax IN VARCHAR2,
    p_leadtimemin IN VARCHAR2,
    p_contacttypes IN VARCHAR2, -- Comma-separated list of key_vals
    p_refcriterias IN VARCHAR2, -- Comma-separated list of key_vals
    p_stampuser IN VARCHAR2,
    p_outnewqueueid OUT NUMBER,
    p_outmsg    OUT VARCHAR2
)
AS
v_queueid NUMBER;
BEGIN
-- TO DO: How to validate for new_queue
--    IF IS_QUEUE_ADMIN(p_queueid, p_stampuser) = 0 THEN
--        RETURN;
--    END IF;
   
    IF p_queueid = 0 THEN
        v_queueid := QUEUESEQ.NextVal;
        INSERT INTO VALIDQUEUES (            
            QUEUE_ID,NAME,NAME_ES,NAME_CP,ENTITY_ID,ACTIVEFLAG,
            ADDRESS,PHONE,
            EMP_ONLY,HIDE_IN_KIOSK,HIDE_IN_MONITOR,HAS_GUIDELINES,HAS_UPLOADS,
            LEAD_TIME_MIN,LEAD_TIME_MAX,STAMPDATE,STAMPUSER)
        VALUES
           (v_queueid, p_name, p_namees, p_namecp, p_entityid,p_activeflag,
            p_address, p_phone,
            p_emponly,p_hideinkiosk, p_hideinmonitor,p_hasguidelines,p_hasuploads,
            p_leadtimemin,p_leadtimemax,SYSDATE,p_stampuser);
        p_outnewqueueid := v_queueid;
       
        -- Grant this user QADMIN role implicity: TODO: check if this is necessary
        INSERT INTO USER_PERMISSIONS (user_id, queue_id, queueadmin_flag, stampuser)
            VALUES (p_stampuser,v_queueid, 'Y', p_stampuser);
           
    ELSE
        v_queueid := p_queueid; --used later for ContactMethods and RefCriterias
        UPDATE VALIDQUEUES
        SET
            NAME = p_name,
            NAME_ES = p_namees,
            NAME_CP = p_namecp,
            ADDRESS = p_address,
            PHONE = p_phone,
            ENTITY_ID = p_entityid,
            ACTIVEFLAG = p_activeflag,
            EMP_ONLY = p_emponly,
            HIDE_IN_KIOSK = p_hideinkiosk,
            HIDE_IN_MONITOR = p_hideinmonitor,
            HAS_GUIDELINES = p_hasguidelines,
            HAS_UPLOADS = p_hasuploads,
            LEAD_TIME_MAX = p_leadtimemax,
            LEAD_TIME_MIN = p_leadtimemin,
            STAMPUSER = p_stampuser,
            STAMPDATE = SYSDATE
        WHERE
            queue_id = p_queueid;
    END IF;

    -- Set ContactMethods
    IF (length(TRIM(p_contacttypes)) > 0) then
      DELETE FROM QUEUE_CONTACTTYPES WHERE queue_id = v_queueid;
      BEGIN
        FOR i IN
           (SELECT trim(regexp_substr(p_contacttypes, '[^,]+', 1, LEVEL)) item
              FROM dual
                CONNECT BY LEVEL <= regexp_count(p_contacttypes, ',')+1
              )
         LOOP
           if(length(i.item)>0)then
            INSERT INTO QUEUE_CONTACTTYPES VALUES (v_queueid,i.item);          
           end if;
         END LOOP;
      END;
    END IF;

    -- Set RefCriterias
    DELETE FROM QUEUE_REFCRITERIAS WHERE queue_id = v_queueid;
    IF (length(TRIM(p_refcriterias)) > 0) then
      BEGIN
        FOR i IN
           (SELECT trim(regexp_substr(p_refcriterias, '[^,]+', 1, LEVEL)) item
              FROM dual
                CONNECT BY LEVEL <= regexp_count(p_refcriterias, ',')+1
              )
         LOOP
           if(length(i.item)>0)then
            INSERT INTO QUEUE_REFCRITERIAS VALUES (v_queueid,i.item);          
           end if;
         END LOOP;
      END;
    END IF;


EXCEPTION
    WHEN OTHERS THEN
      p_outmsg := SQLERRM;
END;

PROCEDURE UPSERT_USER (
    p_action VARCHAR2,  -- Allowed values A:Add  U:Update
    p_userid VARCHAR2,
    p_entityid IN NUMBER := 1,
    p_firstname IN VARCHAR2,
    p_lastname IN VARCHAR2,
    p_title IN VARCHAR2,
    p_email IN VARCHAR2,
    p_phone IN VARCHAR2,
    p_language IN VARCHAR2,
    p_activeflag IN VARCHAR2,
    p_configadminflag IN VARCHAR2,
    p_hostqueues IN VARCHAR2,       -- Comma-separated list of queueIds
    p_providerqueues IN VARCHAR2,   -- Comma-separated list of queueIds
    p_reporterqueues IN VARCHAR2,   -- Comma-separated list of queueIds
    p_queueadminqueues IN VARCHAR2, -- Comma-separated list of queueIds
    p_stampuser IN VARCHAR2,
    p_outmsg    OUT VARCHAR2
)
AS
--v_queueid NUMBER;    
v_result NUMBER(4) := 0;
v_queueslist VARCHAR2(2000);
v_addentity CHAR(1) := 'N';
BEGIN
    -- Pre-check: Check if stampuser is Config_Admin for this business entity    
    SELECT count(*) INTO v_result
    FROM  USER_ENTITIES E            
    WHERE lower(E.user_id) = lower(p_stampuser)            
        AND NVL(E.adminflag, 'N') = 'Y'
        --TODO: AND NVL(E.activeflag, 'N') = 'Y'
        AND entity_id = p_entityid;

    IF v_result = 0 THEN
        p_outmsg := 'Access denied';
        RETURN;
    END IF;    

    v_result := 0; -- re-initalize for user-exists-check

    SELECT count(1) INTO v_result
    FROM  FQ_USERS
    WHERE lower(user_id) = lower(p_userid);

    IF  p_action = 'A' AND v_result > 0 THEN
        SELECT COUNT(1) INTO v_result
        FROM USER_ENTITIES E
            WHERE lower(E.user_id) = lower(p_userid)
                AND entity_id = p_entityid;
 
        IF v_result > 0 THEN
            p_outmsg := 'UserId already exists';
            RETURN;
        ELSIF v_result = 0 THEN
            v_addentity := 'Y';
        END IF;
       
    END IF;
   
    -- Add or Update User info
    IF p_action = 'A' AND v_result = 0 AND v_addentity = 'N' THEN
        -- 1. Base record
        INSERT INTO FQ_USERS (            
            USER_ID, FNAME, LNAME, EMAIL, PHONE, LANGUAGE,
            ACTIVEFLAG, TITLE, STAMPDATE, STAMPUSER)
        VALUES
           (lower(p_userid), p_firstname, p_lastname, p_email, p_phone, p_language,
             p_activeflag, p_title, SYSDATE, lower(p_stampuser));

        -- 2. User-Entity record
        INSERT INTO USER_ENTITIES (
            USER_ID, ENTITY_ID, ACTIVEFLAG,
            ADMINFLAG, STAMPDATE, STAMPUSER)
        VALUES (
            lower(p_userid), p_entityid, p_activeflag,
            p_configadminflag, SYSDATE, lower(p_stampuser));
           
    ELSIF p_action = 'U' OR v_addentity = 'Y' THEN
        -- 1. If user is new for entity Then insert Else update        
        MERGE INTO USER_ENTITIES tgt
            USING (SELECT lower(p_userid) AS user_id,
                          p_entityid      AS entity_id,
                          p_configadminflag AS admin_flag,
                          p_activeflag    AS active_flag
                    FROM dual) src
            ON (    lower(tgt.user_id) = src.user_id
                and tgt.entity_id = src.entity_id)
            WHEN MATCHED THEN
                UPDATE SET
                    tgt.STAMPUSER = lower(p_stampuser),
                    tgt.STAMPDATE = SYSDATE,
                    tgt.ADMINFLAG = src.admin_flag,
                    tgt.ACTIVEFLAG = src.active_flag
            WHEN NOT MATCHED THEN
                INSERT (user_id, entity_id, activeflag, adminflag,
                        stampuser, stampdate)
                VALUES (src.user_id, src.entity_id, src.active_flag,
                        src.admin_flag, lower(p_stampuser), SYSDATE);
                       
        -- 2. Update Base-record
        SELECT count(user_id) INTO v_result
         FROM USER_ENTITIES
         WHERE lower(USER_ID) = lower(p_userid)
            AND NVL(activeflag,'N') = 'Y';
       
        UPDATE FQ_USERS SET
            FNAME = p_firstname,
            LNAME = p_lastname,
            EMAIL = p_email,
            PHONE = p_phone,
            LANGUAGE = p_language,
            -- Deactivate base-record - ONLY when all entities are Inactive
            ACTIVEFLAG = DECODE(v_result, 0, p_activeflag, ACTIVEFLAG),
            TITLE = p_title,
            STAMPUSER = lower(p_stampuser)
        WHERE lower(user_id) = lower(p_userid);
    ELSE
        p_outmsg := 'Invalid action ' || p_action;
        RETURN;
    END IF;

    -- Set HostQueues
    v_queueslist := p_hostqueues;
    IF (length(TRIM(v_queueslist)) > 0) then
      BEGIN    
        FOR i IN
               (SELECT trim(regexp_substr(v_queueslist, '[^,]+', 1, LEVEL)) item
                  FROM dual
                    CONNECT BY LEVEL <= regexp_count(v_queueslist, ',')+1
                  )
             LOOP
               if(length(i.item) > 0) then
                    MERGE INTO USER_PERMISSIONS tgt
                    USING (SELECT lower(p_userid) AS user_id,
                                  i.item   AS queue_id,
                                  'Y'      AS flag
                            FROM dual) src
                    ON (tgt.user_id = src.user_id and tgt.queue_id = src.queue_id)
                    WHEN MATCHED THEN
                        UPDATE SET
                            tgt.host_flag = src.flag,
                            tgt.stampuser = lower(p_stampuser),
                            tgt.stampdate = SYSDATE
                    WHEN NOT MATCHED THEN
                        INSERT (user_id, queue_id, host_flag, stampuser)
                        VALUES (src.user_id, src.queue_id, src.flag, lower(p_stampuser));
               end if;          
       END LOOP;
      END;
    END IF;

    -- Disable any older HOST perms
    UPDATE USER_PERMISSIONS SET
        host_flag = 'N',
        stampuser = p_stampuser,
        stampdate = SYSDATE
        WHERE lower(user_id) = lower(p_userid)
            and host_flag = 'Y'
            and queue_id IN (
                select queue_id from validqueues
                  where entity_id = p_entityid
                minus
                select to_number(trim(regexp_substr(v_queueslist, '[^,]+', 1, level))) queue_id
                  from dual
                   connect by level <= regexp_count(v_queueslist, ',')+1
            );
           
           
    -- Set ProviderQueues
    v_queueslist := p_providerqueues;
    IF (length(TRIM(v_queueslist)) > 0) then
      BEGIN    
        FOR i IN
               (SELECT trim(regexp_substr(v_queueslist, '[^,]+', 1, LEVEL)) item
                  FROM dual
                    CONNECT BY LEVEL <= regexp_count(v_queueslist, ',')+1
                  )
             LOOP
               if(length(i.item) > 0) then
                    MERGE INTO USER_PERMISSIONS tgt
                    USING (SELECT lower(p_userid) AS user_id,
                                  i.item   AS queue_id,
                                  'Y'      AS flag
                            FROM dual) src
                    ON (tgt.user_id = src.user_id and tgt.queue_id = src.queue_id)
                    WHEN MATCHED THEN
                        UPDATE SET
                            tgt.provider_flag = src.flag,
                            tgt.stampuser = lower(p_stampuser),
                            tgt.stampdate = SYSDATE
                    WHEN NOT MATCHED THEN
                        INSERT (user_id, queue_id, provider_flag, stampuser)
                        VALUES (src.user_id, src.queue_id, src.flag, lower(p_stampuser));
               end if;          
       END LOOP;
      END;
    END IF;
   
    -- Disable any older PROVIDER perms
    UPDATE USER_PERMISSIONS SET
        provider_flag = 'N',
        stampuser = p_stampuser,
        stampdate = SYSDATE
        WHERE lower(user_id) = lower(p_userid)
            and provider_flag = 'Y'
            and queue_id IN (
                select queue_id from validqueues
                  where entity_id = p_entityid
                minus
                select to_number(trim(regexp_substr(v_queueslist, '[^,]+', 1, level))) queue_id
                  from dual
                   connect by level <= regexp_count(v_queueslist, ',')+1
            );
           
    -- Set ReporterQueues
    v_queueslist := p_reporterqueues;
    IF (length(TRIM(v_queueslist)) > 0) then
      BEGIN
        -- b. Enable all current perms            
        FOR i IN
               (SELECT trim(regexp_substr(v_queueslist, '[^,]+', 1, LEVEL)) item
                  FROM dual
                    CONNECT BY LEVEL <= regexp_count(v_queueslist, ',')+1
                  )
             LOOP
               if(length(i.item) > 0) then
                    MERGE INTO USER_PERMISSIONS tgt
                    USING (SELECT lower(p_userid) AS user_id,
                                  i.item   AS queue_id,
                                  'Y'      AS flag
                            FROM dual) src
                    ON (tgt.user_id = src.user_id and tgt.queue_id = src.queue_id)
                    WHEN MATCHED THEN
                        UPDATE SET
                            tgt.reporter_flag = src.flag,
                            tgt.stampuser = lower(p_stampuser),
                            tgt.stampdate = SYSDATE
                    WHEN NOT MATCHED THEN
                        INSERT (user_id, queue_id, reporter_flag, stampuser)
                        VALUES (src.user_id, src.queue_id, src.flag, lower(p_stampuser));
               end if;          
       END LOOP;
      END;
    END IF;
   
    -- Disable any older REPORTER  perms
    UPDATE USER_PERMISSIONS SET
        reporter_flag = 'N',
        stampuser = p_stampuser,
        stampdate = SYSDATE
        WHERE lower(user_id) = lower(p_userid)
            and reporter_flag = 'Y'
            and queue_id IN (
                select queue_id from validqueues
                  where entity_id = p_entityid
                minus
                select to_number(trim(regexp_substr(v_queueslist, '[^,]+', 1, level))) queue_id
                  from dual
                   connect by level <= regexp_count(v_queueslist, ',')+1
            );
   
    -- Set QueueAdminQueues
    v_queueslist := p_queueadminqueues;
    IF (length(TRIM(v_queueslist)) > 0) then
      BEGIN    
        FOR i IN
               (SELECT trim(regexp_substr(v_queueslist, '[^,]+', 1, LEVEL)) item
                  FROM dual
                    CONNECT BY LEVEL <= regexp_count(v_queueslist, ',')+1
                  )
             LOOP
               if(length(i.item) > 0) then
                    MERGE INTO USER_PERMISSIONS tgt
                    USING (SELECT lower(p_userid) AS user_id,
                                  i.item   AS queue_id,
                                  'Y'      AS flag
                            FROM dual) src
                    ON (tgt.user_id = src.user_id and tgt.queue_id = src.queue_id)
                    WHEN MATCHED THEN
                        UPDATE SET
                            tgt.queueadmin_flag = src.flag,
                            tgt.stampuser = lower(p_stampuser),
                            tgt.stampdate = SYSDATE
                    WHEN NOT MATCHED THEN
                        INSERT (user_id, queue_id, queueadmin_flag, stampuser)
                        VALUES (src.user_id, src.queue_id, src.flag, lower(p_stampuser));
               end if;          
       END LOOP;
      END;
    END IF;  
   
    -- Disable any older QUEUEADMIN perms
    UPDATE USER_PERMISSIONS SET
        queueadmin_flag = 'N',
        stampuser = lower(p_stampuser),
        stampdate = SYSDATE
    WHERE lower(user_id) = lower(p_userid)
            and queueadmin_flag = 'Y'
            and queue_id IN (
                select queue_id from validqueues
                  where entity_id = p_entityid
                minus
                select to_number(trim(regexp_substr(v_queueslist, '[^,]+', 1, level))) queue_id
                  from dual
                   connect by level <= regexp_count(v_queueslist, ',')+1
            );
           
           
    -- Finally delete any perms where all 4 flags are disabled
    DELETE FROM USER_PERMISSIONS
        WHERE lower(user_id) = lower(p_userid)
            and NVL(host_flag, 'N') = 'N'
            and NVL(provider_flag, 'N') = 'N'
            and NVL(reporter_flag, 'N') = 'N'
            and NVL(queueadmin_flag, 'N') = 'N'
            and queue_id
                IN (select queue_id from validqueues
                    where entity_id = p_entityid);
   
EXCEPTION
    WHEN OTHERS THEN
      p_outmsg := SQLERRM;
END;

PROCEDURE UPSERT_ENTITY (
    p_entityid  IN NUMBER,
    p_name IN VARCHAR2,
    p_description IN VARCHAR2,
    p_address IN VARCHAR2,
    p_phone IN VARCHAR2,
    p_activeflag IN VARCHAR2,    
    p_opensat IN VARCHAR2,
    p_closesat IN VARCHAR2,
    p_outagenotifyat IN VARCHAR2,
    p_outagebeginat IN VARCHAR2,
    p_outageendat IN VARCHAR2,
    p_outagemessage IN VARCHAR2,
    p_stampuser IN VARCHAR2,
    p_affectedappts OUT NUMBER,
    p_outmsg OUT VARCHAR2
)
AS
v_entityid NUMBER;
BEGIN
--    IF IS_QUEUE_ADMIN(p_queueid, p_stampuser) = 0 THEN
--        RETURN;
--    END IF;
   
    if p_entityid = 0 then
        /*
        v_entityid := 999; --EntitySEQ.NextVal;
        INSERT INTO QUEUE_SCHEDULES
            (SCHEDULE_ID, QUEUE_ID, DATE_BEGIN, DATE_END, OPEN_TIME, CLOSE_TIME,
             INTERVAL_TIME, WEEKLY_SCH, AVAILABLE_RESOURCES, STAMPUSER)
        VALUES
            (v_scheduleid, p_queueid, to_date(p_datebegin, 'MM/DD/YYYY'),
             to_date(p_dateend, 'MM/DD/YYYY'), p_opentime, p_closetime,
             p_duration, p_weekdays, p_availres, p_stampuser);
             */
             null;
    else
        UPDATE VALIDENTITIES
          SET ENTITY_NAME = p_name,
                ADDRESS = p_address,
                PHONE = p_phone,
                OPENS_AT = to_date(p_opensAt, 'mm/dd/yyyy hh:mi:ss AM'),
                CLOSES_AT = to_date(p_closesat, 'mm/dd/yyyy hh:mi:ss AM'),
                DESCRIPTION = p_description,
                ACTIVEFLAG = 'Y',
                OUTAGE_BEGINS_AT = to_date(p_outagebeginat, 'mm/dd/yyyy hh:mi:ss AM'),
                OUTAGE_ENDS_AT = to_date(p_outageendat, 'mm/dd/yyyy hh:mi:ss AM'),
                OUTAGE_NOTIFY_BEGIN = to_date(p_outagenotifyat, 'mm/dd/yyyy hh:mi:ss AM'),
                OUTAGE_MESSAGE = p_outagemessage,
                STAMPUSER = p_stampuser
          WHERE ENTITY_ID = p_entityid;
         
          SELECT count(1) INTO p_affectedappts
          FROM APPOINTMENTS
          WHERE
            STATUS = 'SCHEDULED' AND
            APPT_DATE + START_TIME BETWEEN
            to_date(p_outagebeginat, 'mm/dd/yyyy hh:mi:ss AM')
            AND
            to_date(p_outageendat, 'mm/dd/yyyy hh:mi:ss AM');
    end if;
EXCEPTION
    WHEN OTHERS THEN
        p_outmsg := 'Err:' || SQLERRM;
END;

PROCEDURE UPSERT_QACCESS (
    p_queueid IN NUMBER,  
    p_hostusers IN VARCHAR2,       -- Comma-separated list of userIds
    p_providerusers IN VARCHAR2,   -- Comma-separated list of userIds
    p_reporterusers IN VARCHAR2,   -- Comma-separated list of userIds
    p_queueadminusers IN VARCHAR2, -- Comma-separated list of userIds
    p_stampuser IN VARCHAR2,
    p_outmsg    OUT VARCHAR2
)
AS
v_userslist VARCHAR2(2000);
v_linemarker number := 100;
BEGIN  
    -- Set hostusers
    v_userslist := lower(p_hostusers);
    dbms_output.put_line('Hosts: ' || p_hostusers);
    IF (length(TRIM(v_userslist)) > 0) then
      BEGIN    
        FOR i IN
               (SELECT trim(regexp_substr(v_userslist, '[^,]+', 1, LEVEL)) item
                  FROM dual
                    CONNECT BY LEVEL <= regexp_count(v_userslist, ',')+1
                  )
             LOOP
                v_linemarker := v_linemarker + 1;
               dbms_output.put_line('item: ' || i.item);
               if(length(i.item) > 0) then
                    MERGE INTO USER_PERMISSIONS tgt
                    USING (SELECT lower(i.item) AS user_id,
                                  p_queueid AS queue_id,
                                  'Y'       AS flag
                            FROM dual) src
                    ON (tgt.user_id = src.user_id and tgt.queue_id = src.queue_id)
                    WHEN MATCHED THEN
                        UPDATE SET
                            tgt.host_flag = src.flag,
                            tgt.stampuser = p_stampuser,
                            tgt.stampdate = SYSDATE
                    WHEN NOT MATCHED THEN
                        INSERT (user_id, queue_id, host_flag, stampuser)
                        VALUES (src.user_id, src.queue_id, src.flag, p_stampuser);
               end if;        
       END LOOP;
      END;
    END IF;
   
    v_linemarker := 199;
    -- Disable any older HOST perms
    UPDATE USER_PERMISSIONS SET
        host_flag = 'N',
        stampuser = p_stampuser,
        stampdate = SYSDATE
        WHERE queue_id = p_queueid      
            and host_flag = 'Y'
            and lower(user_id) IN (    
                select lower(user_id) from fq_users
                MINUS    
                select trim(regexp_substr(v_userslist, '[^,]+', 1, level)) user_id
                  from dual
                   connect by level <= regexp_count(v_userslist, ',')+1
            );
           
    v_linemarker := 200;        
    -- Set providerusers
    v_userslist := lower(p_providerusers);
    IF (length(TRIM(v_userslist)) > 0) then
      BEGIN    
        FOR i IN
               (SELECT trim(regexp_substr(v_userslist, '[^,]+', 1, LEVEL)) item
                  FROM dual
                    CONNECT BY LEVEL <= regexp_count(v_userslist, ',')+1
                  )
             LOOP
                v_linemarker := v_linemarker + 1;
               if(length(i.item) > 0) then
                    MERGE INTO USER_PERMISSIONS tgt
                    USING (SELECT lower(i.item) AS user_id,
                                  p_queueid     AS queue_id,
                                  'Y'           AS flag
                            FROM dual) src
                    ON (tgt.user_id = src.user_id and tgt.queue_id = src.queue_id)
                    WHEN MATCHED THEN
                        UPDATE SET
                            tgt.provider_flag = src.flag,
                            tgt.stampuser = p_stampuser,
                            tgt.stampdate = SYSDATE
                    WHEN NOT MATCHED THEN
                        INSERT (user_id, queue_id, provider_flag, stampuser)
                        VALUES (src.user_id, src.queue_id, src.flag, p_stampuser);
               end if;          
       END LOOP;
      END;
    END IF;
    v_linemarker := 299;
    -- Disable any older PROVIDER perms
    UPDATE USER_PERMISSIONS SET
        provider_flag = 'N',
        stampuser = p_stampuser,
        stampdate = SYSDATE
        WHERE  queue_id =  p_queueid
            and provider_flag = 'Y'
            and lower(user_id) IN (    
                select lower(user_id) from fq_users
                MINUS    
                select trim(regexp_substr(v_userslist, '[^,]+', 1, level)) user_id
                  from dual
                   connect by level <= regexp_count(v_userslist, ',')+1
            );
           
    -- Set reporterusers
    v_linemarker := 300;
    v_userslist := lower(p_reporterusers);
    IF (length(TRIM(v_userslist)) > 0) then
      BEGIN
        -- b. Enable all current perms            
        FOR i IN
               (SELECT trim(regexp_substr(v_userslist, '[^,]+', 1, LEVEL)) item
                  FROM dual
                    CONNECT BY LEVEL <= regexp_count(v_userslist, ',')+1
                  )
             LOOP
               v_linemarker := v_linemarker + 1;
               if(length(i.item) > 0) then
                    MERGE INTO USER_PERMISSIONS tgt
                    USING (SELECT lower(i.item) AS user_id,
                                  p_queueid     AS queue_id,
                                  'Y'           AS flag
                            FROM dual) src
                    ON (tgt.user_id = src.user_id and tgt.queue_id = src.queue_id)
                    WHEN MATCHED THEN
                        UPDATE SET
                            tgt.reporter_flag = src.flag,
                            tgt.stampuser = p_stampuser,
                            tgt.stampdate = SYSDATE
                    WHEN NOT MATCHED THEN
                        INSERT (user_id, queue_id, reporter_flag, stampuser)
                        VALUES (src.user_id, src.queue_id, src.flag, p_stampuser);
               end if;          
       END LOOP;
      END;
    END IF;
    v_linemarker := 399;
    -- Disable any older REPORTER  perms
    UPDATE USER_PERMISSIONS SET
        reporter_flag = 'N',
        stampuser = p_stampuser,
        stampdate = SYSDATE
        WHERE queue_id =  p_queueid
            and reporter_flag = 'Y'
            and lower(user_id) IN (    
                select lower(user_id) from fq_users
                MINUS            
                select trim(regexp_substr(v_userslist, '[^,]+', 1, level)) user_id
                  from dual
                   connect by level <= regexp_count(v_userslist, ',')+1
            );
    v_linemarker := 400;
    -- Set queueadminusers
    v_userslist := lower(p_queueadminusers);
    IF (length(TRIM(v_userslist)) > 0) then
      BEGIN    
        FOR i IN
               (SELECT trim(regexp_substr(v_userslist, '[^,]+', 1, LEVEL)) item
                  FROM dual
                    CONNECT BY LEVEL <= regexp_count(v_userslist, ',')+1
                  )
             LOOP
               v_linemarker := v_linemarker + 1;
               if(length(i.item) > 0) then
                    MERGE INTO USER_PERMISSIONS tgt
                    USING (SELECT lower(i.item) AS user_id,
                                  p_queueid     AS queue_id,
                                  'Y'           AS flag
                            FROM dual) src
                    ON (tgt.user_id = src.user_id and tgt.queue_id = src.queue_id)
                    WHEN MATCHED THEN
                        UPDATE SET
                            tgt.queueadmin_flag = src.flag,
                            tgt.stampuser = p_stampuser,
                            tgt.stampdate = SYSDATE
                    WHEN NOT MATCHED THEN
                        INSERT (user_id, queue_id, queueadmin_flag, stampuser)
                        VALUES (src.user_id, src.queue_id, src.flag, p_stampuser);
               end if;          
       END LOOP;
      END;
    END IF;  
    v_linemarker := 499;
    -- Disable any older QUEUEADMIN perms
    UPDATE USER_PERMISSIONS SET
        queueadmin_flag = 'N',
        stampuser = p_stampuser,
        stampdate = SYSDATE
    WHERE queue_id =  p_queueid
            and queueadmin_flag = 'Y'
            and lower(user_id) IN (    
                select lower(user_id) from fq_users
                MINUS                
                select trim(regexp_substr(v_userslist, '[^,]+', 1, level)) user_id
                  from dual
                   connect by level <= regexp_count(v_userslist, ',')+1
            );
           
   v_linemarker := 500;        
    -- Finally delete any perms where all 4 flags are disabled
    DELETE FROM USER_PERMISSIONS
        WHERE
                NVL(host_flag, 'N') = 'N'
            and NVL(provider_flag, 'N') = 'N'
            and NVL(reporter_flag, 'N') = 'N'
            and NVL(queueadmin_flag, 'N') = 'N'
            and queue_id = p_queueid;
   
EXCEPTION
    WHEN OTHERS THEN
      p_outmsg := v_linemarker || '--' || SQLERRM;
END;

PROCEDURE DELETE_USER (
    p_userid    IN VARCHAR2,
    p_entityid  IN NUMBER,
    p_stampuser IN VARCHAR2,
    p_out OUT VARCHAR2
)
AS
v_result NUMBER(4) := 0;
BEGIN
    -- 1. Pre-check: Check if stampuser is Config_Admin for this Business-entity    
    SELECT count(*) INTO v_result
    FROM  USER_ENTITIES E            
    WHERE lower(E.user_id) = lower(p_stampuser)            
            AND entity_id = p_entityid
            AND NVL(E.adminflag, 'N') = 'Y'
            AND NVL(E.activeflag, 'N') = 'Y';

    IF v_result = 0 THEN
        p_out := 'Access denied';
        RETURN;
    END IF;    
   
    -- 2. Remove Queue-Permissions  
    DELETE FROM USER_PERMISSIONS
    WHERE lower(user_id) = lower(p_userid)
        AND queue_id IN (SELECT queue_id
                            FROM VALIDQUEUES Q
                            WHERE Q.entity_id = p_entityid
                        );
       
    -- 3. Remove Business-entity permission
    DELETE FROM USER_ENTITIES
    WHERE lower(user_id) = lower(p_userid)
        AND entity_id = p_entityid;    
   
    -- 3. Check if user exists in other business-entities    
    --     Delete base record ONLY-IF the user has no other membership    
    v_result := 0; -- re-initalize for user-exists-check
   
    SELECT COUNT(*) INTO v_result
    FROM USER_ENTITIES E
    WHERE lower(E.user_id) = lower(p_userid);
       

    -- Log DELETION for auditing purposes
    INSERT INTO LOG_DELETED_USERS (
        USER_ID, FNAME, LNAME, EMAIL, PHONE, LANGUAGE,
        ACTIVEFLAG, PASSWORD, TITLE, ENTITY_ID,
        STAMPDATE, STAMPUSER, DELETED_BY, DELETED_ON
        )
        (SELECT USER_ID, FNAME, LNAME, EMAIL, PHONE, LANGUAGE,
                ACTIVEFLAG, PASSWORD, TITLE, p_entityid,
                STAMPDATE, STAMPUSER, p_stampuser, SYSDATE  
         FROM FQ_USERS U
            WHERE lower(user_id) = lower(p_userid)
        );
       
    IF v_result = 0 THEN
        DELETE FROM FQ_USERS WHERE lower(user_id) = lower(p_userid);
    END IF;
           
EXCEPTION
    WHEN OTHERS THEN
        p_out := v_result ||  'Err: ' || SQLERRM;
END;

PROCEDURE UPSERT_HOLIDAY (
    p_holiday  IN DATE,
    p_description IN VARCHAR2,
    p_active IN VARCHAR2,
    p_stampuser IN VARCHAR2,
    p_out OUT VARCHAR2
)
AS
v_superadmin NUMBER := 0;
BEGIN    
    -- Check if stampuser is SuperAdmin
   SELECT count(1) INTO v_superadmin
    FROM USER_ENTITIES u
     WHERE lower(user_id) = lower(p_stampuser)
        AND NVL(u.activeflag, 'N') = 'Y'
        AND NVL(u.adminflag, 'N') = 'Y';
   
    IF v_superadmin = 0 THEN
        p_out := 'You do not have permissions to manage Holiday schedule.';
        RETURN;
    END IF;
   
    MERGE INTO VALIDHOLIDAYS tgt
        USING (SELECT
                TRUNC(p_holiday) AS holiday_date,
                p_description AS holiday_desc,
                p_active AS holiday_status
                FROM dual
               ) src
        ON (    
                TRUNC(tgt.holidaydate) = src.holiday_date
           )
        WHEN MATCHED THEN
            UPDATE SET
                tgt.STAMPUSER = lower(p_stampuser),
                tgt.STAMPDATE = SYSDATE,                    
                tgt.ACTIVEFLAG = src.holiday_status,
                tgt.HOLIDAYDESC =  src.holiday_desc
        WHEN NOT MATCHED THEN
            INSERT (holidaydate, holidaydesc, activeflag,
                    stampuser, stampdate)
            VALUES (src.holiday_date, src.holiday_desc, src.holiday_status,
                    lower(p_stampuser), SYSDATE);
EXCEPTION
    WHEN OTHERS THEN
        p_out := SQLERRM;
END;

procedure DELETE_HOLIDAY (
    p_holiday  IN VARCHAR2,
    p_stampuser IN VARCHAR2,
    p_out OUT VARCHAR2
)
AS
v_superadmin NUMBER := 0;
BEGIN    
    -- TODO: Check if stampuser is SuperAdmin
   SELECT count(1) INTO v_superadmin
    FROM USER_ENTITIES u
     WHERE lower(user_id) = lower(p_stampuser)
        AND NVL(u.activeflag, 'N') = 'Y'
        AND NVL(u.adminflag, 'N') = 'Y';
   
    IF v_superadmin > 0 THEN
        DELETE FROM VALIDHOLIDAYS
            WHERE TRUNC(holidayDate) = to_date(p_holiday, 'MM/DD/YYYY');
       
        IF SQL%ROWCOUNT > 0 THEN
            UPDATE_DELETION_LOG('VALIDHOLIDAYS','TRUNC(holidayDate)',
            'to_date(' || p_holiday || ', ''MM/DD/YYYY'')', p_stampuser);
        END IF;
    ELSE
        p_out := 'You do not have permissions to delete Holiday schedule.';
    END IF;

EXCEPTION
    WHEN OTHERS THEN
        p_out := 'Err: ' || SQLERRM;
END;

END FQ_PROCS_ADMIN;