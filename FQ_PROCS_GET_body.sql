create or replace PACKAGE BODY FQ_PROCS_GET as
--*******************************************************
-- 2025.12.31   PREDDY      Created package
--
--*******************************************************
PROCEDURE GET_ENTITY (
    p_entityid         IN   NUMBER,
    p_cur              OUT  Ref_Cursor_Types.ref_cursor
)
AS
BEGIN
 OPEN p_cur FOR
    SELECT * FROM VALIDENTITIES        
    WHERE entity_id = p_entityid;
END;

PROCEDURE GET_ENTITIES_ALL (
    p_cur              OUT  Ref_Cursor_Types.ref_cursor
)
AS
BEGIN
 OPEN p_cur FOR
    SELECT * FROM VALIDENTITIES;
END;

PROCEDURE GET_MYQUEUES (
    p_userid       IN   VARCHAR2,
    p_entityid   IN   NUMBER,
    p_cur   OUT  Ref_Cursor_Types.ref_cursor
)
AS
v_isadmin NUMBER;
BEGIN
 /*OPEN p_cur FOR
    SELECT * FROM VALIDQUEUES q
        INNER JOIN USER_PERMISSIONS p ON p.queue_id = q.queue_id
    WHERE p.user_id = p_userid
        AND NVL(ACTIVEFLAG, 'N') = 'Y';

    SELECT count(user_id)
        INTO v_isadmin
    FROM FQ_USERS
    WHERE lower(user_id) = lower(p_userid) AND
        NVL(ACTIVEFLAG,'N') = 'Y' and NVL(ADMINFLAG,'N') = 'Y';
*/        
    SELECT count(user_id) INTO v_isadmin
    FROM USER_ENTITIES
    WHERE lower(user_id) = lower(p_userid) AND
        ENTITY_ID = NVL(p_entityid, ENTITY_ID) AND
        NVL(ACTIVEFLAG,'N') = 'Y' and NVL(ADMINFLAG,'N') = 'Y';  

    IF v_isadmin = 0 THEN --NOT admin or IsInactive
        OPEN p_cur FOR  
            SELECT * FROM VALIDQUEUES q
                INNER JOIN USER_PERMISSIONS p ON p.queue_id = q.queue_id
            WHERE  
                -- NVL(QUEUEADMIN_FLAG,'N') = 'Y' AND
                -- NVL(ACTIVEFLAG, 'N') = 'Y' AND
                 lower(p.user_id) = lower(p_userid)
                AND ENTITY_ID = NVL(p_entityid, ENTITY_ID)
            ORDER BY NAME;
    ELSE
        OPEN p_cur FOR  
            SELECT * FROM VALIDQUEUES q
            WHERE --p.user_id = p_userid
                --AND NVL(ACTIVEFLAG, 'N') = 'Y' AND
                 ENTITY_ID = NVL(p_entityid, ENTITY_ID)
            ORDER BY NAME;
    END IF;
END;

PROCEDURE GET_QUEUE_DETAILS (
    p_queueid     IN    NUMBER,
    p_cur  OUT  Ref_Cursor_Types.ref_cursor
)
AS
BEGIN
    OPEN p_cur FOR
        SELECT Q_SERVICES, Q_SCHEDULES, Q_DETAILS, QUEUE_ID
        FROM VW_QUEUE_DETAILS_JSON
            WHERE queue_id = p_queueid;
END;  

PROCEDURE GET_MYWALKINS (
-- For Internal Use ONLY - Get logged-in Staff's Queued Walkins
    p_entityid IN NUMBER,
    p_userid IN  VARCHAR2,
    p_range_startdate   IN  DATE,
    p_range_enddate     IN  DATE,  
    p_cur  OUT Ref_Cursor_Types.ref_cursor
)
AS
v_isSuperAdmin NUMBER;
BEGIN
 v_isSuperAdmin := FQ_PROCS_ADMIN.IS_SUPER_ADMIN(p_entityid, p_userid);
 OPEN p_cur FOR
    -- PReddy: Updated query allows SuperAdmins to implicitly inherit
    --          PROVIDER-level access to 'ALL' Queues in this Entity
    --          and eliminates granting explicit Host/Provider in FQManager UI
    WITH P AS
    (
            -- Dynamically build QPERMS for this user (including isADMIN)
            -- Left-Join to get all_Queues and then eliminate NULL perms for non-admins
                SELECT LOWER(p_userid) user_id
                    , q1.queue_id
                    , q1.name
                    , entity_id
                    , DECODE(v_isSuperAdmin, 1, 'Y', HOST_FLAG) HOST_FLAG
                    , DECODE(v_isSuperAdmin, 1, 'Y', PROVIDER_FLAG) PROVIDER_FLAG
                    , DECODE(v_isSuperAdmin, 1, 'Y', REPORTER_FLAG) REPORTER_FLAG
                    , DECODE(v_isSuperAdmin, 1, 'Y', QUEUEADMIN_FLAG) QUEUEADMIN_FLAG
                FROM VALIDQUEUES q1
                    LEFT JOIN user_permissions p1
                        ON p1.user_id = lower(p_userid)
                            AND q1.queue_id = p1.queue_id
                WHERE   entity_id = p_entityid
                    AND (    
                           DECODE(v_isSuperAdmin, 1, 'Y', HOST_FLAG)  ='Y'
                        OR DECODE(v_isSuperAdmin, 1, 'Y', PROVIDER_FLAG) ='Y'
                        OR DECODE(v_isSuperAdmin, 1, 'Y', REPORTER_FLAG) = 'Y'
                        OR DECODE(v_isSuperAdmin, 1, 'Y', QUEUEADMIN_FLAG) = 'Y'
                    )
    ),
    ST AS
    (
        SELECT src_id,
               service_notes,
               service_start_time,
               service_end_time,
               stampuser
        FROM
        (
            SELECT src_id,
                   service_notes,
                   service_start_time,
                   service_end_time,
                   stampuser,
                   ROW_NUMBER() OVER
                   (
                       PARTITION BY src_id
                       ORDER BY stampdate DESC NULLS LAST,
                                transaction_id DESC
                   ) AS rn
            FROM SERVICETRANSACTIONS
            WHERE src_type = 'W'
        )
        WHERE rn = 1
    )
    SELECT  p.queue_id, p.name, vs.service_id, vs.service_name,
            provider_flag,
            host_flag,
            queueadmin_Flag, reporter_Flag,
            u.fname, u.lname,
            FQ_PROCS_GET.GET_USERNAME(a.stampuser) stampusername,
            a.*, c.sms_optin,
            st.service_notes,
            st.service_start_time,
            st.service_end_time,
            st.stampuser service_stampuser
            , FQ_CRYPTO_PKG.DECRYPT(c.fname) cust_fname
            , FQ_CRYPTO_PKG.DECRYPT(c.lname) cust_lname
            , FQ_CRYPTO_PKG.DECRYPT(c.email) cust_email
            , FQ_CRYPTO_PKG.DECRYPT(c.phone) cust_phone
        FROM
            WALKINS a
            INNER join VALIDQUEUE_SERVICES vs ON
                vs.queue_id = a.queue_id and vs.service_id = a.service_id  
            INNER JOIN P
                   ON P.queue_id = a.Queue_id                      
            INNER JOIN FQ_USERS u ON u.user_id = p.user_id
            INNER JOIN CUSTOMERS c on c.customer_id = a.customer_id
            LEFT JOIN ST st ON st.src_id = a.walkin_id
        WHERE
                    LOWER(u.user_id) = Lower(p_userid)
            AND NVL(u.activeflag,'N') = 'Y'
            AND p.entity_id = p_entityid
            AND TRUNC(createdon)
                    BETWEEN TRUNC(p_range_startdate) AND TRUNC(p_range_enddate)
            ;        
END;

PROCEDURE GET_MYAPPOINTMENTS (
-- For Internal Use ONLY - Get logged-in Staff's Queued Appts
    p_entityid IN NUMBER,
    p_userid IN  VARCHAR2,
    p_range_startdate   IN  DATE,
    p_range_enddate     IN  DATE,  
    p_cur  OUT Ref_Cursor_Types.ref_cursor
)
AS
v_isSuperAdmin NUMBER;
BEGIN
 v_isSuperAdmin := FQ_PROCS_ADMIN.IS_SUPER_ADMIN(p_entityid, p_userid);

 OPEN p_cur FOR
    -- PReddy: Updated query allows SuperAdmins to implicitly inherit
    --          PROVIDER-level access to 'ALL' Queues in this Entity
    --          and eliminates granting explicit Host/Provider in FQManager UI
    WITH P AS
    (
            -- Dynamically build QPERMS for this user (including isADMIN)
            -- Left-Join to get all_Queues and then eliminate NULL perms for non-admins
                SELECT LOWER(p_userid) user_id
                    , q1.queue_id
                    , q1.name
                    , entity_id
                    , DECODE(v_isSuperAdmin, 1, 'Y', HOST_FLAG) HOST_FLAG
                    , DECODE(v_isSuperAdmin, 1, 'Y', PROVIDER_FLAG) PROVIDER_FLAG
                    , DECODE(v_isSuperAdmin, 1, 'Y', REPORTER_FLAG) REPORTER_FLAG
                    , DECODE(v_isSuperAdmin, 1, 'Y', QUEUEADMIN_FLAG) QUEUEADMIN_FLAG
                from VALIDQUEUES q1
                    LEFT JOIN USER_PERMISSIONS p1
                        ON p1.user_id = lower(p_userid)
                            AND q1.queue_id = p1.queue_id
                where   entity_id = p_entityid
                    AND (    
                           DECODE(v_isSuperAdmin, 1, 'Y', HOST_FLAG)  ='Y'
                        OR DECODE(v_isSuperAdmin, 1, 'Y', PROVIDER_FLAG) ='Y'
                        OR DECODE(v_isSuperAdmin, 1, 'Y', REPORTER_FLAG) = 'Y'
                        OR DECODE(v_isSuperAdmin, 1, 'Y', QUEUEADMIN_FLAG) = 'Y'
                    )
    ),
    ST AS
    (
        SELECT src_id,
               service_notes,
               service_start_time,
               service_end_time,
               stampuser
        FROM
        (
            SELECT src_id,
                   service_notes,
                   service_start_time,
                   service_end_time,
                   stampuser,
                   ROW_NUMBER() OVER
                   (
                       PARTITION BY src_id
                       ORDER BY stampdate DESC NULLS LAST,
                                transaction_id DESC
                   ) AS rn
            FROM SERVICETRANSACTIONS
            WHERE src_type = 'A'
        )
        WHERE rn = 1
    )
    SELECT  p.queue_id, p.name, vs.service_id, vs.service_name,
            provider_flag,
            host_flag,
            queueadmin_Flag, reporter_Flag,
            u.fname, u.lname,
            FQ_PROCS_GET.GET_USERNAME(a.stampuser) stampusername,
            a.*, c.sms_optin,
            st.service_notes,
            st.service_start_time,
            st.service_end_time,
            st.stampuser service_stampuser
            , FQ_CRYPTO_PKG.DECRYPT(c.fname) cust_fname
            , FQ_CRYPTO_PKG.DECRYPT(c.lname) cust_lname
            , FQ_CRYPTO_PKG.DECRYPT(c.email) cust_email
            , FQ_CRYPTO_PKG.DECRYPT(c.phone) cust_phone
        FROM
            APPOINTMENTS a
            INNER join VALIDQUEUE_SERVICES vs ON
                vs.queue_id = a.queue_id and vs.service_id = a.service_id  
            INNER JOIN P
                   ON P.queue_id = a.Queue_id                      
            INNER JOIN FQ_USERS u ON u.user_id = p.user_id
            INNER JOIN Customers c on c.customer_id = a.customer_id
            LEFT JOIN ST st ON st.src_id = a.appointment_id
        WHERE
                    LOWER(u.user_id) = LOWER(p_userid)
            AND NVL(u.activeflag,'N') = 'Y'
            AND p.entity_id = p_entityid
            AND TRUNC(appt_date)
                    BETWEEN TRUNC(p_range_startdate) AND TRUNC(p_range_enddate)
            ;
END;

PROCEDURE GET_APPT_DETAILS (
    p_apptid IN Appointments.appointment_id%type,
    p_cur OUT Ref_Cursor_Types.ref_cursor
)
AS
BEGIN
 OPEN p_cur FOR
    SELECT --FQ_CRYPTO_PKG.ENCRYPT(A.Appointment_Id),
        A.*, q.entity_id,
        q.Address QUEUE_ADDRESS, q.Phone QUEUE_PHONE
    FROM APPOINTMENTS A
        INNER JOIN VALIDQUEUES Q ON a.queue_id = q.queue_id
    WHERE appointment_id = p_apptid;
    --TODO: Add additional conditions and Join-Tables
END;

PROCEDURE GET_MYPROFILE (p_userid IN VARCHAR2, p_cur OUT Ref_Cursor_Types.ref_cursor)
AS
BEGIN
    OPEN p_cur FOR
     SELECT * FROM FQ_USERS
      WHERE lower(user_id) = lower(p_userid)
        AND NVL(activeflag, 'N') = 'Y';    
END;

FUNCTION GET_USERNAME (p_userid IN VARCHAR2)
RETURN VARCHAR2
AS
p_ret VARCHAR2(200);
BEGIN
    SELECT FNAME || ' ' || LNAME INTO p_ret
    FROM fq_users
        WHERE lower(user_id) = lower(p_userid);  

    RETURN p_ret;
EXCEPTION
    WHEN NO_DATA_FOUND THEN
        RETURN p_userid;
END;

-- GETUSERS and GETUSER are used by/for ADMIN-USER-ACCOUNTS
PROCEDURE GET_USERS (p_entityid IN NUMBER, p_stampuser IN VARCHAR2, p_message OUT VARCHAR2, p_cur OUT Ref_Cursor_Types.ref_cursor)
AS
--isAdmin CHAR(1);
isAdmin NUMBER(3) := 1;
BEGIN    
--    SELECT NVL(adminflag,'N') INTO isAdmin
--    FROM FQ_USERS WHERE LOWER(user_id) = LOWER(p_stampuser);

    SELECT count(1) INTO isAdmin
    FROM USER_ENTITIES WHERE LOWER(user_id) = LOWER(p_stampuser)
        AND ENTITY_ID = p_entityid
        AND NVL(adminflag,'N') = 'Y';

    IF isAdmin = 0 THEN
        p_message := 'You do not have access to User Accouts';
        RETURN;
    END IF;

    OPEN p_cur FOR
     SELECT u.*, e.AdminFlag
      FROM fq_users u
        INNER JOIN user_entities e ON u.user_id = e.user_id
     WHERE e.entity_id = p_entityid;
END;

PROCEDURE GET_USER_QUEUES_ROLES (
    p_userid IN VARCHAR2,
    p_stampuser IN VARCHAR2,
    p_message OUT VARCHAR2,
    p_cur OUT Ref_Cursor_Types.ref_cursor
)
AS
--isAdmin CHAR(1) := 'Y';
iCount NUMBER(9) := 1;
BEGIN    
    -- Check if stampuser has access to requested User-Profile  
    IF p_stampuser = 'AUTHSERVICE' OR lower(p_stampuser) = lower(p_userid) THEN
        iCount := 1;
    ELSE
        SELECT COUNT(1) INTO iCount
            FROM user_entities
            WHERE lower(user_id) = lower(p_userid)
                AND entity_id IN (SELECT entity_id FROM user_entities
                            WHERE lower(user_id) = lower(p_stampuser)
                                AND NVL(AdminFlag, 'N') = 'Y'
                            );
    END IF;

    IF --UPPER(isAdmin) = 'Y' AND
        iCount > 0 THEN    
         -- Get user-queues-permissions
         OPEN p_cur FOR
             SELECT p.USER_ID, q.QUEUE_ID, q.NAME, q.ENTITY_ID, q.ACTIVEFLAG,
                    -- r.ROLE_ID, r.ROLE_DESC
                    e.adminflag configadmin,
                    provider_flag, host_flag, queueadmin_Flag, reporter_Flag
                FROM validqueues q
                    INNER JOIN user_permissions p on p.queue_id = q.queue_id
                    INNER JOIN user_entities e on
                                p.user_id = e.user_id
                            and e.entity_id = q.entity_id
                WHERE lower(p.user_id) = lower(p_userid)
                ORDER BY q.NAME;        
    ELSE
        p_message := 'You do not have access to this user account';
    END IF;      
END;

PROCEDURE GET_USER (    
    p_userid IN VARCHAR2,
    p_stampuser IN VARCHAR2,
    p_message OUT VARCHAR2,
    p_cur OUT Ref_Cursor_Types.ref_cursor
)
AS
isAdmin CHAR(1) := 'Y';
iCount NUMBER(9);
BEGIN
    --Check if stampuser has access to requested User-Profile
--    SELECT NVL(adminflag,'N') INTO isAdmin
--    FROM FQ_USERS WHERE LOWER(user_id) = LOWER(p_stampuser)
--        and NVL(ActiveFlag, 'N') = 'Y';


    SELECT COUNT(entity_id) INTO iCount
    FROM user_entities
    WHERE lower(user_id) = lower(p_userid)
        AND entity_id IN (SELECT entity_id FROM user_entities WHERE lower(user_id) = lower(p_stampuser));

    IF UPPER(isAdmin) = 'Y' AND iCount > 0 THEN    
         OPEN p_cur FOR
             SELECT u.*,
             'N' ADMINFLAG -- AdminFlag is set in UserEntities
             FROM FQ_USERS u
              WHERE lower(user_id) = lower(p_userid);
    ELSE
        p_message := 'You do not have access to this user account';
    END IF;
--EXCEPTION
    --WHEN NO_DATA_FOUND THEN
    --    p_message := 'User not found';
    --WHEN OTHERS THEN
    --    p_message := SQLERRM;
END;

PROCEDURE GET_QUEUES (
    p_entity     IN   INT,
    p_ref_cursor   OUT  Ref_Cursor_Types.ref_cursor
)
AS
BEGIN
 OPEN p_ref_cursor FOR
    SELECT * FROM VALIDQUEUES
    WHERE ENTITY_ID = NVL(p_entity, ENTITY_ID)
        AND NVL(ACTIVEFLAG, 'N') = 'Y'
    ORDER BY NAME;
END;

PROCEDURE GET_SERVICES (
    p_queueid    IN INT,
    p_ref_cursor OUT Ref_Cursor_Types.ref_cursor
)
AS
BEGIN
    OPEN p_ref_cursor FOR
        SELECT
            s.service_id,
            s.queue_id,
            s.service_name,
            s.service_name_es,
            s.service_name_cp,
            s.activeflag
        FROM validqueue_services s
        WHERE s.queue_id = p_queueid
          AND NVL(s.activeflag, 'N') = 'Y'
        ORDER BY s.service_name;
END GET_SERVICES;

PROCEDURE GET_QSERVICE_DETAILS(
 p_serviceid IN number,
 p_userid IN varchar2,
 p_cur  OUT  Ref_Cursor_Types.ref_cursor)
AS
  BEGIN
    --TODO: Check if user has permissions to the Queue for this serviceID
  OPEN p_cur FOR
    SELECT --JSON_OBJECT(
       service_id, queue_id, activeflag,
       service_name, service_name_es, service_name_cp
   FROM VALIDQUEUE_SERVICES
   WHERE service_id = p_serviceid;
 END;

PROCEDURE GET_QSCHEDULE_DETAILS(
 p_scheduleid IN number,
 p_userid IN varchar2,
 p_cur  OUT  Ref_Cursor_Types.ref_cursor)
AS
  BEGIN
    --TODO: Check if user has permissions to the Queue for this serviceID
  OPEN p_cur FOR
    SELECT --JSON_OBJECT(
       SCHEDULE_ID, QUEUE_ID, DATE_BEGIN, DATE_END, OPEN_TIME, CLOSE_TIME,
       INTERVAL_TIME, WEEKLY_SCH, AVAILABLE_RESOURCES
   FROM queue_schedules
   WHERE SCHEDULE_ID = p_scheduleid;
 END;

-- Multi-functional proc to get Customer(s) based on column data
PROCEDURE GET_CUSTOMERS (p_fldname IN VARCHAR2, p_fldvalue IN VARCHAR2, p_cur OUT Ref_Cursor_Types.ref_cursor)
AS
v_where VARCHAR2(100);
BEGIN
    CASE UPPER(p_fldname)
        WHEN 'CUSTOMER_ID' THEN
            v_where := 'CUSTOMER_ID = ' || to_number(p_fldvalue);
        WHEN 'PHONE' THEN
            v_where := 'FQ_CRYPTO_PKG.DECRYPT(PHONE) = ''' || p_fldvalue || '''';
        WHEN 'EMAIL' THEN
            v_where := 'lower(FQ_CRYPTO_PKG.DECRYPT(EMAIL)) = ''' || lower(p_fldvalue) || '''';
        WHEN 'ALL' THEN
            v_where := '1=1';
        ELSE
            v_where := '1=2';
    END CASE;

    OPEN p_cur FOR
    'SELECT CUSTOMER_ID,
             FQ_CRYPTO_PKG.DECRYPT(FNAME) AS FNAME,
             FQ_CRYPTO_PKG.DECRYPT(LNAME) AS LNAME,
             FQ_CRYPTO_PKG.DECRYPT(EMAIL) AS EMAIL,
             FQ_CRYPTO_PKG.DECRYPT(PHONE) AS PHONE,
             SMS_OPTIN, ACTIVEFLAG, STAMPDATE, STAMPUSER
      FROM CUSTOMERS
      WHERE ' || v_where;
END;

PROCEDURE GET_PROVIDERS_BY_ENTITY (p_entityid NUMBER, p_cur OUT Ref_Cursor_Types.ref_cursor)
AS
BEGIN
 OPEN p_cur FOR
    SELECT DISTINCT u.USER_ID, u.FNAME, u.LNAME, u.EMAIL, u.PHONE, u.LANGUAGE,
        u.ACTIVEFLAG, ue.ADMINFLAG, u.TITLE, u.STAMPDATE, u.STAMPUSER
      FROM fqowner.FQ_USERS u
      JOIN fqowner.USER_PERMISSIONS p ON p.USER_ID = u.USER_ID
      JOIN fqowner.VALIDQUEUES q ON q.QUEUE_ID = p.QUEUE_ID
      JOIN USER_ENTITIES ue on ue.user_id = u.user_id and ue.entity_id = q.entity_id
      WHERE q.ENTITY_ID = p_entityid;
END;    

PROCEDURE GET_APPT_ID_BY_CONF (
  p_confcode IN VARCHAR2,
  p_apptid OUT NUMBER
)
AS
BEGIN
  p_apptid := NULL;

  SELECT APPOINTMENT_ID
    INTO p_apptid
    FROM APPOINTMENTS
   WHERE UPPER(CONFCODE) = UPPER(TRIM(p_confcode));

EXCEPTION
  WHEN NO_DATA_FOUND THEN
    p_apptid := NULL;
END GET_APPT_ID_BY_CONF;

PROCEDURE GET_QUEUE_ID_FOR_SOURCE (
  p_src_type IN VARCHAR2,
  p_src_id IN NUMBER,
  p_queue_id OUT NUMBER
)
AS
  v_src_type VARCHAR2(1);
BEGIN
  p_queue_id := NULL;
  v_src_type := UPPER(TRIM(p_src_type));

  IF v_src_type = 'A' THEN
    SELECT QUEUE_ID
      INTO p_queue_id
      FROM APPOINTMENTS
     WHERE APPOINTMENT_ID = p_src_id;
  ELSIF v_src_type = 'W' THEN
    SELECT QUEUE_ID
      INTO p_queue_id
      FROM WALKINS
     WHERE WALKIN_ID = p_src_id;
  ELSE
    RAISE_APPLICATION_ERROR(-20002, 'Invalid source type');
  END IF;

EXCEPTION
  WHEN NO_DATA_FOUND THEN
    p_queue_id := NULL;
END GET_QUEUE_ID_FOR_SOURCE;

PROCEDURE GET_APPTS_BY_QUEUE (
  p_queueid IN NUMBER,
  p_cur OUT Ref_Cursor_Types.ref_cursor
)
AS
BEGIN
  OPEN p_cur FOR
    SELECT *
    FROM APPOINTMENTS
    WHERE QUEUE_ID = p_queueid
    ORDER BY APPT_DATE, START_TIME, APPOINTMENT_ID;
END GET_APPTS_BY_QUEUE;

PROCEDURE GET_APPTS_BY_CUSTOMER (
  p_customerid IN NUMBER,
  p_cur OUT Ref_Cursor_Types.ref_cursor
)
AS
BEGIN
  OPEN p_cur FOR
    SELECT A.*, q.entity_id
    FROM APPOINTMENTS A
        INNER JOIN VALIDQUEUES Q ON a.queue_id = q.queue_id    
    WHERE CUSTOMER_ID = p_customerid
    ORDER BY APPT_DATE, START_TIME, APPOINTMENT_ID;
END GET_APPTS_BY_CUSTOMER;

PROCEDURE GET_APPTS_BY_ENTITY (
  p_entityid IN NUMBER,
  p_cur OUT Ref_Cursor_Types.ref_cursor
)
AS
BEGIN
  OPEN p_cur FOR
    SELECT a.*, q.entity_id
    FROM APPOINTMENTS a
    INNER JOIN VALIDQUEUES q
      ON q.QUEUE_ID = a.QUEUE_ID
    WHERE q.ENTITY_ID = p_entityid
    ORDER BY a.APPT_DATE, a.START_TIME, a.APPOINTMENT_ID;
END GET_APPTS_BY_ENTITY;

PROCEDURE GET_ALL_APPTS (
  p_cur OUT Ref_Cursor_Types.ref_cursor
)
AS
BEGIN
  OPEN p_cur FOR
    SELECT A.*, q.entity_id
    FROM APPOINTMENTS A
        INNER JOIN VALIDQUEUES Q ON a.queue_id = q.queue_id    
    ORDER BY APPT_DATE, START_TIME, APPOINTMENT_ID;
END GET_ALL_APPTS;

PROCEDURE GET_USER_ACTION_QUEUE_ACCESS (
    p_userid IN VARCHAR2,
    p_cur OUT Ref_Cursor_Types.ref_cursor
)
AS
BEGIN
 OPEN p_cur FOR
    SELECT DISTINCT
        q.queue_id,
        q.name,
        q.entity_id,
        q.activeflag,
        p.provider_flag,
        p.host_flag,
        p.queueadmin_flag,
        p.reporter_flag
    FROM validqueues q
        INNER JOIN user_permissions p ON q.queue_id = p.queue_id
        INNER JOIN fq_users u ON u.user_id = p.user_id
    WHERE NVL(q.activeflag,'N') = 'Y'
        AND lower(u.user_id) = lower(p_userid)
        AND NVL(u.activeflag,'N') = 'Y'
        AND (p.provider_flag = 'Y' OR p.host_flag = 'Y' OR p.queueadmin_flag = 'Y' OR p.reporter_flag = 'Y');      
END;

PROCEDURE GET_QUEUE_ACCESS (
    p_queueid IN NUMBER,
    p_cur OUT Ref_Cursor_Types.ref_cursor
)
AS
BEGIN
 OPEN p_cur FOR
    SELECT DISTINCT
        u.USER_ID, FNAME, LNAME, EMAIL, u.ACTIVEFLAG,
        NVL(QUEUE_ID, p_queueid) QUEUE_ID,
        HOST_FLAG, PROVIDER_FLAG,
        REPORTER_FLAG, QUEUEADMIN_FLAG,
        e.adminflag ConfigAdminFlag, e.entity_id
    FROM fq_users U
    LEFT OUTER JOIN user_permissions P
     ON u.user_id = p.user_id And p.queue_id = p_queueid
    INNER JOIN user_entities e ON e.user_id = u.user_id
        AND e.entity_id = (select entity_id from validqueues where queue_id = p_queueid)
    ORDER BY u.lname, u.fname;
END;

PROCEDURE GET_USER_ENTITIES (    
    p_userid IN VARCHAR2,
    p_stampuser IN VARCHAR2,
    p_message OUT VARCHAR2,
    p_cur OUT Ref_Cursor_Types.ref_cursor
)
AS
BEGIN
    -- Get requested users' Active entities (used for Authentication)
     OPEN p_cur FOR
        SELECT e.entity_id as EntityId, e.entity_name EntityName,
                ue.ACTIVEFLAG, ue.ADMINFLAG
        FROM validentities e                  
            INNER JOIN user_entities ue on e.entity_id = ue.entity_id
        WHERE NVL(e.ACTIVEFLAG, 'N') = 'Y'
            AND lower(ue.user_id) = lower(p_userid);
END;

PROCEDURE GET_VALID_LOOKUPS (p_lookuptable IN VARCHAR2, p_cur OUT Ref_Cursor_Types.Ref_Cursor)
AS
BEGIN
    CASE UPPER(p_lookuptable)
        WHEN 'VALIDREFERENCECRITERIAS' THEN
            OPEN p_cur FOR
                SELECT A.REF_KEY key, A.REF_VAL val
                 FROM VALIDREFERENCECRITERIAS A
                  WHERE NVL(ACTIVEFLAG,'N') = 'Y';
        WHEN 'VALIDCONTACTTYPES' THEN
            OPEN p_cur FOR
                SELECT A.TYPE_KEY key, A.TYPE_VAL val
                 FROM VALIDCONTACTTYPES A
                  WHERE NVL(ACTIVEFLAG,'N') = 'Y';
        ELSE
            NULL;
        END CASE;
END;

PROCEDURE GET_HOLIDAY (
    p_holiday         IN   VARCHAR2,
    p_cur              OUT  Ref_Cursor_Types.ref_cursor
)
AS
BEGIN
 OPEN p_cur FOR
    SELECT HOLIDAYDATE, HOLIDAYDESC,ACTIVEFLAG, STAMPDATE, STAMPUSER
      FROM VALIDHOLIDAYS        
        WHERE TRUNC(HOLIDAYDATE) = TO_DATE(p_holiday, 'MM/DD/YYYY');
END;

PROCEDURE GET_HOLIDAYS (p_cur OUT  Ref_Cursor_Types.ref_cursor)
AS
BEGIN
 OPEN p_cur FOR
    SELECT HOLIDAYDATE, HOLIDAYDESC,ACTIVEFLAG, STAMPDATE, STAMPUSER
        FROM VALIDHOLIDAYS;
END;


END FQ_PROCS_GET;
