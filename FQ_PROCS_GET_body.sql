CREATE OR REPLACE PACKAGE BODY FQ_PROCS_GET AS
--*******************************************************
-- 2025.12.31   PREDDY      Created package
--*******************************************************
PROCEDURE GET_LOCATION (
    p_locationid       IN   VARCHAR2,
    p_cur              OUT  Ref_Cursor_Types.ref_cursor
)
AS
BEGIN
 OPEN p_cur FOR
    SELECT * FROM VALIDLOCATIONS
    WHERE location_id = p_locationid;
END;

PROCEDURE GET_MYQUEUES (
    p_userid       IN   VARCHAR2,
    p_locationid   IN   NUMBER,
    p_cur          OUT  Ref_Cursor_Types.ref_cursor
)
AS
BEGIN
 OPEN p_cur FOR
    SELECT * FROM VALIDQUEUES q
        INNER JOIN USER_PERMISSIONS p ON p.queue_id = q.queue_id
    WHERE p.user_id = p_userid
        AND LOCATION_ID = NVL(p_locationid, LOCATION_ID);
END;

PROCEDURE GET_QUEUE_DETAILS (
    p_queueid     IN    NUMBER,
    p_cur         OUT   Ref_Cursor_Types.ref_cursor
)
AS
BEGIN
    OPEN p_cur FOR
        SELECT * FROM VW_QUEUE_DETAILS_JSON
            WHERE queue_id = p_queueid;
END;

PROCEDURE GET_QUEUE_OPENSLOTS (
    p_queueid       IN NUMBER,
    p_date          IN DATE,
    p_cur           OUT Ref_Cursor_Types.ref_cursor
)
AS
BEGIN
    OPEN p_cur FOR
    SELECT *
     FROM (
        WITH  DuplicatedAppointments (
                    theDate, queue_id, slot_begin, slot_end,
                    interval_time, DuplicatesLevel, Available_Resources, Weekly_Sch
                    ) AS (
            SELECT  to_DATE(p_date,'YYYY-MM-DD') as theDate, queue_id,
                    slot_begin, slot_end,interval_time,
                    QS.Available_Resources as DuplicatesLevel,QS.Available_Resources,Weekly_Sch
            FROM
                QUEUE_SCHEDULES QS,
                lateral (
                    select
                        to_char(trunc(sysdate) + open_time + (level - 1) * interval_time,'HH:MI AM') as slot_begin,
                        to_char(trunc(sysdate) + open_time + (level    ) * interval_time,'HH:MI AM') as slot_end
                    from dual
                        connect by open_time + level * interval_time <= close_time
                     )
            WHERE
                     open_time + interval_time <= close_time
                 and to_date(p_date,'YYYY-MM-DD') between date_begin and date_end
                 and NOT EXISTS (select 1 from validholidays h where trunc(h.holidaydate) = trunc(to_date(p_date,'YYYY-MM-DD')))
                 and queue_id = NVL(p_queueid,queue_id)
                 and to_char(to_date(p_date,'YYYY-MM-DD'), 'd') IN (select to_char(SUBSTR(Weekly_Sch, LEVEL, 1)) as weekday from dual connect by level <= length(Weekly_Sch))
          UNION ALL
          SELECT
            theDate, queue_id,
            slot_begin, slot_end,interval_time,
            DuplicatesLevel-1, Available_Resources, Weekly_Sch
          FROM
            DuplicatedAppointments D
          WHERE
            DuplicatesLevel > 1
        )
        SELECT
                theDate, d.queue_id,
                slot_begin, slot_end, Weekly_Sch
                ,interval_time ,Available_Resources, DuplicatesLevel, a.appointment_id
        FROM
          DuplicatedAppointments d LEFT JOIN
            (Select appointment_id,
                    APPT_DATE, queue_ID,
                    to_char(trunc(sysdate) + start_time, 'HH:MI AM') slot_start_time,
                    to_char(trunc(sysdate) + end_time, 'HH:MI AM') slot_end_time,
                    ROW_NUMBER() OVER (PARTITION BY queue_ID, APPT_DATE, start_time, END_TIME order by APPT_DATE) as Duplicate_level
                    from appointments)  A
              ON  d.theDate = a.appt_date
                    and d.queue_id = a.queue_id
                    and d.slot_begin = a.slot_start_time
                    and d.slot_end = a.slot_end_time
                    and d.duplicateslevel = a.Duplicate_level
        WHERE
         DuplicatesLevel >= 1
         AND appointment_id is null
        );
END;

PROCEDURE GET_APPTHIST4CUSTOMER (
    p_customerid        IN  NUMBER,
    p_cur               OUT Ref_Cursor_Types.ref_cursor
)
AS
BEGIN
 OPEN p_cur FOR
    SELECT FQ_CRYPTO_PKG.ENCRYPT(A.Appointment_Id), A.*
    FROM APPOINTMENTS  A
    WHERE CUSTOMER_ID = p_customerid
        AND TO_DATE(TO_CHAR(TRUNC(appt_date) + start_time, 'HH:MI AM'),'MM-DD-YYYY HH:MI AM') < SYSDATE;
END;

PROCEDURE GET_APPTS4CUSTOMER (
    p_customerid        IN  INT,
    p_range_startdate   IN  DATE,
    p_range_enddate     IN  DATE,
    p_cur               OUT Ref_Cursor_Types.ref_cursor
)
AS
BEGIN
 OPEN p_cur FOR
    SELECT FQ_CRYPTO_PKG.ENCRYPT(A.Appointment_Id),A.*
    FROM APPOINTMENTS A
    WHERE CUSTOMER_ID = p_customerid
        AND trunc(appt_date) BETWEEN trunc(p_range_startdate) AND trunc(p_range_enddate);
END;

PROCEDURE GET_MYWALKINS (
    p_userid IN  VARCHAR2,
    p_range_startdate   IN  DATE,
    p_range_enddate     IN  DATE,
    p_cur               OUT Ref_Cursor_Types.ref_cursor
)
AS
BEGIN
 OPEN p_cur FOR
    SELECT q.queue_id, q.name, vs.service_id, vs.service_name,
        p.role_id, r.role_desc,
        u.fname, u.lname,
        a.*, c.sms_optin
        , fq_crypto_pkg.decrypt(c.fname) cust_fname, fq_crypto_pkg.decrypt(c.lname) cust_lname
        , fq_crypto_pkg.decrypt(c.email) cust_email, fq_crypto_pkg.decrypt(c.phone) cust_phone
    FROM validqueues q
        INNER JOIN walkins a ON q.queue_id = a.queue_id
        INNER join validqueue_services vs ON
            vs.queue_id = a.queue_id and vs.service_id = a.service_id
        INNER JOIN user_permissions p ON q.queue_id = p.queue_id
        INNER JOIN validroles r ON r.role_id = p.role_id
        INNER JOIN fq_users u ON u.user_id = p.user_id
        INNER JOIN customers c on c.customer_id = a.customer_id
    WHERE
            NVL(q.activeflag,'N') = 'Y'
            AND u.user_id = p_userid AND NVL(u.activeflag,'N') = 'Y'
            AND trunc(createdon) BETWEEN trunc(p_range_startdate) AND trunc(p_range_enddate);
END;

PROCEDURE GET_MYAPPOINTMENTS (
    p_userid IN  VARCHAR2,
    p_range_startdate   IN  DATE,
    p_range_enddate     IN  DATE,
    p_cur               OUT Ref_Cursor_Types.ref_cursor
)
AS
BEGIN
 OPEN p_cur FOR
    SELECT q.queue_id, q.name, vs.service_id, vs.service_name,
        p.role_id, r.role_desc, u.fname, u.lname,
        a.*, c.sms_optin
        , fq_crypto_pkg.decrypt(c.fname) cust_fname, fq_crypto_pkg.decrypt(c.lname) cust_lname
        , fq_crypto_pkg.decrypt(c.email) cust_email, fq_crypto_pkg.decrypt(c.phone) cust_phone
    FROM validqueues q
        INNER JOIN appointments a ON q.queue_id = a.queue_id
        INNER join validqueue_services vs ON
            vs.queue_id = a.queue_id and vs.service_id = a.service_id
        INNER JOIN user_permissions p ON q.queue_id = p.queue_id
        INNER JOIN validroles r ON r.role_id = p.role_id
        INNER JOIN fq_users u ON u.user_id = p.user_id
        INNER JOIN customers c on c.customer_id = a.customer_id
    WHERE NVL(q.activeflag,'N') = 'Y'
        AND u.user_id = p_userid AND NVL(u.activeflag,'N') = 'Y'
        AND trunc(appt_date) BETWEEN trunc(p_range_startdate) AND trunc(p_range_enddate);
END;

PROCEDURE GET_APPT_DETAILS (
    p_apptid IN Appointments.appointment_id%type,
    p_cur OUT Ref_Cursor_Types.ref_cursor
)
AS
BEGIN
 OPEN p_cur FOR
    SELECT FQ_CRYPTO_PKG.ENCRYPT(A.Appointment_Id), A.*
    FROM APPOINTMENTS A
    WHERE appointment_id = p_apptid;
END;

PROCEDURE GET_MYPROFILE (p_userid IN VARCHAR2, p_cur OUT Ref_Cursor_Types.ref_cursor)
AS
BEGIN
    OPEN p_cur FOR
     SELECT * FROM fq_users
      WHERE lower(user_id) = lower(p_userid)
        AND NVL(activeflag, 'N') = 'Y';
END;

FUNCTION GET_USERID (p_useremail IN VARCHAR2, p_stampuser IN VARCHAR2)
RETURN VARCHAR2
AS
p_ret VARCHAR2(200);
BEGIN
    SELECT user_id INTO p_ret
    FROM fq_users
        WHERE lower(email) = lower(p_useremail);
    RETURN p_ret;
END;

PROCEDURE GET_USERS (p_entityid IN NUMBER, p_stampuser IN VARCHAR2, p_message OUT VARCHAR2, p_cur OUT Ref_Cursor_Types.ref_cursor)
AS
isAdmin CHAR(1);
BEGIN
    SELECT NVL(adminflag,'N') INTO isAdmin
    FROM FQ_USERS WHERE LOWER(user_id) = LOWER(p_stampuser);

    IF UPPER(isAdmin) = 'Y' THEN
     OPEN p_cur FOR
     SELECT u.* FROM fq_users u
        INNER JOIN USER_LOCATIONS l ON u.user_id = l.user_id
     WHERE l.location_id IN
        (select distinct Location_id
            from user_locations ul where user_id = lower(p_stampuser)
        );
    ELSE
        p_message := 'You do not have access to User Accouts';
    END IF;
END;

PROCEDURE GET_USER_QUEUES_ROLES (
    p_userid IN VARCHAR2,
    p_stampuser IN VARCHAR2,
    p_message OUT VARCHAR2,
    p_cur OUT Ref_Cursor_Types.ref_cursor
)
AS
isAdmin CHAR(1);
iCount NUMBER(9);
BEGIN
    SELECT NVL(adminflag,'N') INTO isAdmin
    FROM FQ_USERS WHERE LOWER(user_id) = LOWER(p_stampuser)
        and NVL(ActiveFlag, 'N') = 'Y';

    SELECT COUNT(location_id) INTO iCount
    FROM user_locations
    WHERE lower(user_id) = lower(p_userid)
        AND location_id IN (SELECT location_id FROM user_locations WHERE lower(user_id) = lower(p_stampuser));

    IF UPPER(isAdmin) = 'Y' AND iCount > 0 THEN
         OPEN p_cur FOR
             SELECT p.USER_ID, q.QUEUE_ID, q.NAME, LOCATION_ID, q.ACTIVEFLAG,
                        r.ROLE_ID, r.ROLE_DESC
                FROM validqueues q
                    INNER JOIN user_permissions p on p.queue_id = q.queue_id
                    INNER JOIN validroles r on p.role_id = r.role_id
                WHERE lower(user_id) = lower(p_userid);
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
isAdmin CHAR(1);
iCount NUMBER(9);
BEGIN
    SELECT NVL(adminflag,'N') INTO isAdmin
    FROM FQ_USERS WHERE LOWER(user_id) = LOWER(p_stampuser)
        and NVL(ActiveFlag, 'N') = 'Y';

    SELECT COUNT(location_id) INTO iCount
    FROM user_locations
    WHERE lower(user_id) = lower(p_userid)
        AND location_id IN (SELECT location_id FROM user_locations WHERE lower(user_id) = lower(p_stampuser));

    IF UPPER(isAdmin) = 'Y' AND iCount > 0 THEN
         OPEN p_cur FOR
             SELECT * FROM fq_users
              WHERE lower(user_id) = lower(p_userid);
    ELSE
        p_message := 'You do not have access to this user account';
    END IF;
END;

PROCEDURE GET_QUEUES (
    p_location     IN   INT,
    p_ref_cursor   OUT  Ref_Cursor_Types.ref_cursor
)
AS
BEGIN
 OPEN p_ref_cursor FOR
    SELECT * FROM VALIDQUEUES
    WHERE LOCATION_ID = NVL(p_location, LOCATION_ID)
        AND NVL(ACTIVEFLAG, 'N') = 'Y';
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
  OPEN p_cur FOR
    SELECT
       service_id, queue_id, activeflag,
       service_name, service_name_es, service_name_cp
   FROM VALIDQUEUE_SERVICES
   WHERE service_id = p_serviceid;
END;
END FQ_PROCS_GET;
/
