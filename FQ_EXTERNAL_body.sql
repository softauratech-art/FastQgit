create or replace PACKAGE BODY         "FQ_EXTERNAL" as
--********************************************************************
-- 2025.12.31   PREDDY      Created package
-- 2026.xx.xx SVANAR01 Added procs
-- 2026.07.31 PREDDY Added IS_OPEN_FOR_WALKIN
-- Added Exception to GET_MYPROFILE
-- 2026.08.10 PREDDY Updated INSERT_APPT to account for undisclosed
-- customer-email in staff-scheduled appts
--********************************************************************
PROCEDURE GET_QUEUE_DETAILS_JSON (
    p_queueid         IN NUMBER,
    p_queue_details   OUT  VARCHAR2,
    p_queue_services  OUT  VARCHAR2,
    p_queue_schedules OUT  VARCHAR2
    )
AS
BEGIN
      SELECT Q_services, Q_Schedules, Q_Details
        INTO p_queue_services, p_queue_schedules, p_queue_details
      FROM VW_QUEUE_DETAILS_JSON Q
      WHERE  q.queue_id = p_queueid;
END;

PROCEDURE GET_QUEUE_OPENSLOTS (
    p_queueid IN NUMBER,
    p_thedate IN DATE,
    p_json    OUT  VARCHAR2
)
AS
p_date varchar2(20) := to_char(p_thedate,'YYYY-MM-DD');
v_outage_begin_dt DATE;
v_outage_end_dt DATE;
v_json CLOB;
BEGIN

    SELECT NVL(E.outage_begins_at,SYSDATE), NVL(E.outage_ends_at,SYSDATE)
        INTO v_outage_begin_dt, v_outage_end_dt
     FROM VALIDENTITIES E
        INNER JOIN VALIDQUEUES Q ON e.entity_id = q.entity_id
     WHERE q.queue_id = p_queueid;


 SELECT  JSON_ARRAYAGG(
                            JSON_OBJECT(
                            thedate, 'qid' value queue_id,
                            slot_begin, slot_end, weekly_sch,
                            'duration' value interval_time,
                            'prov' value available_resources,
                            'open' value avail_spots
                            --,duplicateslevel, appointment_id --*Uncomment this if NOT grouping by
                            )
                            ORDER BY TO_TIMESTAMP(slot_begin, 'HH:MI AM')
                            RETURNING CLOB
                    )
    INTO p_json
 FROM (
    --- Recursive CTE to get available-open-slots based on
    --  SelectedDate, ServiceID (IN params)
    ---   AND Available_Resources setting
    ---   AND WeeklySch - dayOfWeek
    ---   AND existing ScheduledAppointments
    --    AND HolidaySchedule
    --    AND OutageSchedule
    -- This CTE is a recursive anchor to generate rows
    WITH  DuplicatedAppointments (
                theDate, queue_id, slot_begin, slot_end,
                interval_time, DuplicatesLevel, Available_Resources, Weekly_Sch
                ) AS (
        -- Anchor member: Select all appointments
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
             and to_char(to_date(p_date,'YYYY-MM-DD'), 'd')-1 IN (select to_char(SUBSTR(Weekly_Sch, LEVEL, 1)) as weekday from dual connect by level <= length(Weekly_Sch))
      -- Recursive member: Add a new row for each remaining duplicate
      UNION ALL
      SELECT
        theDate, queue_id,
        slot_begin, slot_end,interval_time,
        DuplicatesLevel-1, Available_Resources, Weekly_Sch
      FROM
        DuplicatedAppointments D
      WHERE
        DuplicatesLevel > 1 -- Continue until all duplicates are generated
    )
    -- Select from the final result, filtering out the original
    -- "Available_Resources" appointments if they are not needed in the result.
    SELECT --d.*,to_char(to_date(p_date,'YYYY-MM-DD'), 'd') as DayofWeek,
            theDate, d.queue_id,
            slot_begin, slot_end, Weekly_Sch,interval_time ,Available_Resources
            --,DuplicatesLevel, a.appointment_id  --*Uncomment this if NOT grouping by
            ,count(*) avail_spots
    FROM
      DuplicatedAppointments d LEFT JOIN
        (Select appointment_id,
                APPT_DATE, queue_ID,    --start_time, END_TIME ,
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
    -- (Available_Resources = 1 OR (Available_Resources > 1 AND DuplicatesLevel >= 1))
     DuplicatesLevel >= 1
     AND appointment_id is null
     AND TO_DATE(TO_CHAR(thedate, 'YYYY-MM-DD') || ' ' || slot_begin, 'YYYY-MM-DD HH:MI AM')
                NOT BETWEEN v_outage_begin_dt AND v_outage_end_dt
     AND TO_DATE(TO_CHAR(thedate, 'YYYY-MM-DD') || ' ' || slot_end, 'YYYY-MM-DD HH:MI AM')
                NOT BETWEEN v_outage_begin_dt AND v_outage_end_dt
     -- Use GROUP-BY to get totals for each begin-end slots (toggle lines * commented this query)
      group by theDate, d.queue_id, slot_begin, slot_end, Weekly_Sch,interval_time ,Available_Resources
    ORDER BY TO_TIMESTAMP(slot_begin, 'HH:MI AM')
    )
    --GROUP BY THEDATE, QUEUE_ID
    ;
    dbms_output.put_line (length(p_json));
END;

PROCEDURE GET_QUEUE_FIRST_AVAIL_DATE(
    p_queueid IN NUMBER,
    p_json    OUT  VARCHAR2
)
AS
v_df varchar2(50) := 'YYYY-MM-DD';
v_lead_time_min INTERVAL DAY(3) TO SECOND(0);
v_lead_time_max INTERVAL DAY(3) TO SECOND(0);
v_max_converted_datetime DATE;
v_begin_dt DATE;
v_end_dt DATE;

BEGIN
    SELECT NVL(LEAD_TIME_MIN, INTERVAL '1' DAY),
            NVL(LEAD_TIME_MAX, INTERVAL '90' DAY)
        INTO v_lead_time_min, v_lead_time_max
    FROM validqueues
    WHERE queue_id = p_queueid;

    v_begin_dt := to_date(to_char((SYSDATE) + v_lead_time_min, 'YYYY-MM-DD HH24:MI:SS'), 'YYYY-MM-DD HH24:MI:SS');
    v_end_dt := SYSDATE + v_lead_time_max;

    WHILE v_begin_dt <= v_end_dt LOOP
        p_json := '';

        FQ_EXTERNAL.GET_QUEUE_OPENSLOTS(p_queueid, trunc(v_begin_dt), p_json);

        IF TRUNC(v_begin_dt) = TRUNC(SYSDATE) AND length(p_json) > 0 THEN
               -- For same-day slots, if MAX time-slot-avail is NOT greater than current-day
               --       MIN_LEAD_TIME then jump to next-day appts
               SELECT
                MAX(to_date(
                    to_char(
                        TO_DATE(thedate , 'YYYY-MM-DD"T"HH24:MI:SS'), 'YYYY-MM-DD')
                        || ' ' || slot_begin,
                        'YYYY-MM-DD HH:MI AM'))
                    converted_datetime INTO v_max_converted_datetime
                FROM JSON_TABLE(
                   p_json,
                   '$[*]' -- Path to the array elements
                    COLUMNS (thedate VARCHAR2(100), slot_begin VARCHAR2(100))
                 );

                EXIT WHEN v_max_converted_datetime > v_begin_dt;

                -- Otherwise Reset JSON so we can continue checking for Next-day availability
                p_json := '';
        END IF;

        EXIT WHEN length(p_json) > 0;

        v_begin_dt := TRUNC(v_begin_dt) + 1;

    END LOOP;
END;

PROCEDURE GET_MYPROFILE (
    p_email      IN  VARCHAR2,
    p_json       OUT VARCHAR2
)
AS
BEGIN

    SELECT
        JSON_OBJECT (
        'customer_id' value FQ_CRYPTO_PKG.ENCRYPT(C.customer_id)
        ,'SMS_OPTIN' value  SMS_OPTIN
        ,'fname' value FQ_CRYPTO_PKG.DECRYPT(fname)
        ,'lname' value FQ_CRYPTO_PKG.DECRYPT(lname)
        ,'email' value FQ_CRYPTO_PKG.DECRYPT(email)
        ,'phone' value  FQ_CRYPTO_PKG.DECRYPT(phone)
        ) INTO p_json
    FROM
        CUSTOMERS C
    WHERE FQ_CRYPTO_PKG.DECRYPT(C.email) = LOWER(p_email);

  EXCEPTION
    WHEN NO_DATA_FOUND THEN
        p_json := NULL;

END;

PROCEDURE CANCEL_APPT (p_email IN  VARCHAR2, p_apptid IN NUMBER, p_reason IN VARCHAR2, p_outmsg OUT VARCHAR2)
AS
v_customerid NUMBER;
v_customerphone VARCHAR2(15);
v_smsoptin   CHAR(1);
v_smsoutput VARCHAR2(1000);
BEGIN

    SELECT   customer_id
            ,FQ_CRYPTO_PKG.DECRYPT(PHONE)
        INTO v_customerid, v_customerphone
    FROM CUSTOMERS
    WHERE lower(FQ_CRYPTO_PKG.DECRYPT(email)) = lower(p_email);

    UPDATE APPOINTMENTS
        SET STATUS = 'CANCELLED',
            REASONFORCANCEL = p_reason
        WHERE customer_id = v_customerid
            AND APPOINTMENT_ID = p_apptid --FQ_CRYPTO_PKG.DECRYPT(p_apptid)
            AND TRUNC(APPT_DATE) > TRUNC(sysdate) -- Past appointments CANNOT be cancelled
            AND STATUS <> 'C';
    p_outmsg := 'Success';  -- or null => check Siva's pref

    -- Check user's SMS_OPTIN flag. If 'Y' then send SMS
    -- Ignore errors
    BEGIN
        SELECT NVL(SMS_OPTIN, 'N') INTO v_smsoptin
        FROM APPOINTMENTS
            WHERE appointment_id = p_apptid
                AND customer_id = v_customerid;

        IF v_smsoptin = 'Y' THEN
            AMANDA.SEND_SMS@fqlink(v_customerphone,  f_AppointmentSMSBody('A', p_apptid, 'C') , '', 'FASTQ', 'APPOINTMENT_ID', p_apptid, 'FastQ-Scheduler', v_smsoutput);
        END IF;
    EXCEPTION
        WHEN OTHERS THEN
            RETURN;
    END;

EXCEPTION
    WHEN OTHERS THEN
        p_outmsg := 'Err: ' || SQLCODE || ' - ' || SQLERRM;
END;

PROCEDURE INSERT_APPT (
    p_email IN  VARCHAR2,
    p_json IN VARCHAR2,
    p_stampuser IN VARCHAR2,
    p_confcode OUT VARCHAR2,
    p_out OUT VARCHAR2
)
AS
/* Example:
p_json VARCHAR2(1000) := '[
            {"REF_CRITERIA": "G",
             "REF_VALUE": "Questions",
             "QUEUE_ID": 10003,
             "SERVICE_ID": 10004,
             "CONTACTTYPE": "PC",
             "MOREINFO": "comments are here",
             "APPT_DATE": "19-JAN-26 12.00.00 AM",
             "START_TIME": "+00 13:00:00.000000",
             "END_TIME": "+00 14:00:00.000000",
             "STATUS": "SCHEDULED",
             "LANGUAGE_PREF": null,
             "FNAME": "First",
             "LNAME": "Last",
             "EMAIL": "someemail@domain.com",
             "PHONE": "123-456-7890",
             "SMSOPTIN": "Y"
         }]';
*/
v_customerid NUMBER;
v_confcode VARCHAR2(10);
v_exists_flag NUMBER(1);
v_apptid NUMBER;
v_smsoptin  VARCHAR2(5);
v_customerphone VARCHAR2(15);
v_customeremail VARCHAR2(100);
v_smsoutput VARCHAR2(1000);

BEGIN
    p_out := NULL;
    p_confcode := NULL;
    SAVEPOINT fq_create_appointment;
    -- Read info from Json - for SMS use later
    SELECT J.PHONE, J.SMSOPTIN
        INTO v_customerphone, v_smsoptin
    FROM JSON_TABLE(
        p_json,
        '$[*]' COLUMNS (
            PHONE PATH '$.PHONE',
            SMSOPTIN PATH '$.SMSOPTIN'
        )
    ) J;

    -- 1. INSERT into CUSTOMER if NOT Exists with ENCRYPTED PII
    BEGIN
        SELECT C.customer_id, FQ_CRYPTO_PKG.DECRYPT(C.phone)
            INTO v_customerid, v_customerphone
        FROM CUSTOMERS C
            WHERE lower(FQ_CRYPTO_PKG.DECRYPT(email)) = lower(p_email);
    EXCEPTION
        WHEN NO_DATA_FOUND THEN
            SELECT CUSTOMERSEQ.nextval INTO v_customerid FROM dual;

            -- Build placeholder email for ONLY:
            --  staff-scheduled appts with UNDISCLOSED customer-email
            IF UPPER(TRIM(p_email)) = 'NA' AND UPPER(p_stampuser) <> 'FASTQEXT' THEN
                v_customeremail := v_customerid || '@fastq.ocfl.net';
            ELSE
                v_customeremail := TRIM(p_email);
            END IF;

            INSERT INTO CUSTOMERS (CUSTOMER_ID, FNAME, LNAME,
                                    EMAIL, PHONE, SMS_OPTIN,
                                    ACTIVEFLAG, STAMPDATE, STAMPUSER)
                SELECT v_customerid,
                    FQ_CRYPTO_PKG.ENCRYPT(J.FNAME), FQ_CRYPTO_PKG.ENCRYPT(J.LNAME),
                    FQ_CRYPTO_PKG.ENCRYPT(LOWER(v_customeremail)), FQ_CRYPTO_PKG.ENCRYPT(J.PHONE),
                    J.SMSOPTIN, 'Y', SYSDATE, p_stampuser
                FROM JSON_TABLE(
                    p_json,
                    '$[*]' COLUMNS (
                        REF_CRITERIA PATH '$.REF_CRITERIA',
                        FNAME PATH '$.FNAME',
                        LNAME PATH '$.LNAME',
                        EMAIL PATH '$.EMAIL',
                        PHONE PATH '$.PHONE',
                        SMSOPTIN PATH '$.SMSOPTIN'
                    )
                ) J;
    END;

    -- 2. GENERATE CONF_CODE (to be used in customer confirmation & url for virtual-appt)
    v_confcode := FQ_EXTERNAL.GET_NEW_CONF_CODE;

    -- 3. INSERT into APPOINTMENTS
    v_apptid :=  APPTSEQ.NEXTVAL;

    INSERT INTO appointments (
        APPOINTMENT_ID, CUSTOMER_ID,
        REF_CRITERIA, REF_VALUE,
        QUEUE_ID,  SERVICE_ID,
        CONTACTTYPE,  MOREINFO,
        APPT_DATE, START_TIME, END_TIME,
        STATUS, LANGUAGE_PREF, SMS_OPTIN, CONFCODE, MEETINGURL,
        CREATEDBY, CREATEDON, STAMPUSER, STAMPDATE
    )
    SELECT v_apptid, v_customerid, J.*,
          v_confcode, null,p_stampuser, sysdate, p_stampuser, sysdate
    FROM JSON_TABLE(
        p_json,
        '$[*]' COLUMNS (
            REF_CRITERIA PATH '$.REF_CRITERIA',
            REF_VALUE PATH '$.REF_VALUE',
            QUEUE_ID PATH '$.QUEUE_ID',
            SERVICE_ID PATH '$.SERVICE_ID',
            CONTACTTYPE PATH '$.CONTACTTYPE',
            MOREINFO PATH '$.MOREINFO',
            APPT_DATE PATH '$.APPT_DATE',
            START_TIME PATH '$.START_TIME',
            END_TIME PATH '$.END_TIME',
            STATUS PATH '$.STATUS',
            LANGUAGE_PREF PATH '$.LANGUAGE_PREF',
            SMSOPTIN PATH '$.SMSOPTIN'
        )
    ) J;

    -- Read the values actually saved, and capture AMANDA details before SMS.
    BEGIN
        FOR r IN (
            SELECT queue_id, ref_criteria, ref_value
            FROM appointments
            WHERE appointment_id = v_apptid
              AND UPPER(TRIM(ref_criteria)) IN ('P','PERMIT')
              AND TRIM(ref_value) IS NOT NULL
        ) LOOP
            FQ_STORE_AMANDA_INFO('A', v_apptid, r.queue_id, r.ref_criteria, r.ref_value);
        END LOOP;
    EXCEPTION WHEN OTHERS THEN
        p_out := SQLERRM;
        ROLLBACK TO fq_create_appointment;
        RETURN;
    END;

    p_confcode := v_confcode;

    -- Check user's SMS_OPTIN flag. If 'Y' then send SMS
    IF v_smsoptin = 'Y' THEN
        AMANDA.SEND_SMS@fqlink(v_customerphone,  f_AppointmentSMSBody('A', v_apptid, 'S') , '', 'FASTQ', 'APPOINTMENT_ID', v_apptid, 'FastQ-Scheduler', v_smsoutput);
    END IF;

---COMMIT;
EXCEPTION
    WHEN OTHERS THEN
        p_out := SQLERRM;
        --ROLLBACK;
END;

PROCEDURE INSERT_LOGIN (p_email IN VARCHAR2, p_sessionid IN VARCHAR2, p_authcode OUT VARCHAR2, p_out OUT VARCHAR2)
AS
BEGIN
    -- Step 1. Generate authcode
    SELECT LPAD(TRUNC(DBMS_RANDOM.VALUE(0, 999999)), 6, '0') into p_authcode FROM dual;

    -- Step 2. Insert into FQ_SESSIONS
    INSERT INTO FQ_SESSIONS (email, sessionid, authcode, startedat, verifiedat, expiresat, stampdate)
    VALUES (LOWER(p_email), p_sessionid, p_authcode, SYSDATE, null, SYSDATE + interval '10' minute, SYSDATE);

EXCEPTION
    WHEN OTHERS THEN
        p_authcode := null;
        p_out := 'Err: ' || SQLCODE || ' - ' || SQLERRM;
END;

PROCEDURE IS_ACTIVE_SESSION(p_email IN VARCHAR2, p_sessionid IN VARCHAR2, p_success OUT NUMBER)
AS
p_expiresat DATE;
BEGIN
    p_success := 0; -- assume False (Inactive session)

    SELECT  expiresat into p_expiresat
    FROM FQ_SESSIONS
    WHERE LOWER(email) = LOWER(p_email) AND sessionid = p_sessionid and verifiedat IS NOT NULL;

    IF p_expiresat > sysdate THEN
        p_success := 1;
    END IF;

EXCEPTION
    WHEN NO_DATA_FOUND THEN
        p_success := 0;
END;

--***** SP - VALIDATE_LOGIN - Siva - Begin
PROCEDURE VALIDATE_LOGIN (
    p_email IN VARCHAR2,
    p_sessionid IN VARCHAR2,
    p_authcode IN VARCHAR2,
    p_out OUT VARCHAR2,
    p_success OUT NUMBER
)
AS
    p_upd_flag BOOLEAN := false;
BEGIN
    p_out := NULL;
    p_success := 0;

    -- If authcode is valid AND expiresAt is Valid then set verifiedat=sysdate and expiresAt=sysdate+60M
    -- Using cursor in case there were multiple attempts to generate authcode
    FOR item IN (SELECT authcode, expiresat
                     FROM FQ_SESSIONS
                     WHERE LOWER(email) = LOWER(p_email)
                        AND sessionid = p_sessionid
                        AND verifiedat IS NULL)
    LOOP
        IF item.authcode = p_authcode AND item.expiresat > SYSDATE THEN
            p_upd_flag := true;
            EXIT;
        ELSIF item.authcode = p_authcode AND item.expiresat <= SYSDATE THEN
            p_out := 'Auth Code has expired. Please enter your email to generate new auth code.';
        ELSIF item.authcode <> p_authcode AND item.expiresat > SYSDATE THEN
            p_out := 'Auth code is invalid. Please re-enter the correct code.';
        END IF;
    END LOOP;
    -- TODO: trap %notfound exception

    IF p_upd_flag = true THEN
        UPDATE FQ_SESSIONS
        SET  verifiedat = SYSDATE,
             expiresat = SYSDATE + interval '60' minute
        WHERE LOWER(email) = LOWER(p_email) AND sessionid = p_sessionid;
            p_success := 1;
        ELSE
            p_success := 0;
    END IF;

END;
--***** SP - VALIDATE_LOGIN - Siva - End

--***** SP - Update Profile - Siva - Begin
PROCEDURE UPDATE_PROFILE (p_firstname IN VARCHAR2, p_lastname IN VARCHAR2, p_email IN VARCHAR2, p_phone IN VARCHAR2, p_smsoptin IN VARCHAR2, p_success OUT NUMBER, p_out OUT VARCHAR2)
AS
BEGIN
    UPDATE_PROFILE (
        p_firstname,
        p_lastname,
        p_email,
        p_phone,
        p_smsoptin,
        'FASTQEXT',
        p_success,
        p_out
        );

END;

PROCEDURE UPDATE_PROFILE (
    p_firstname IN VARCHAR2,
    p_lastname IN VARCHAR2,
    p_email IN VARCHAR2,
    p_phone IN VARCHAR2,
    p_smsoptin IN VARCHAR2,
    p_stampuser IN VARCHAR2,
    p_success OUT NUMBER,
    p_out OUT VARCHAR2)
AS
BEGIN
    p_success := 0;

    UPDATE CUSTOMERS
    SET FNAME =FQ_CRYPTO_PKG.ENCRYPT(p_firstname),
        LNAME = FQ_CRYPTO_PKG.ENCRYPT(p_lastname),
        PHONE = FQ_CRYPTO_PKG.ENCRYPT(p_phone),
        SMS_OPTIN = p_smsoptin,
        STAMPDATE = SYSDATE,
        STAMPUSER = p_stampuser
    WHERE FQ_CRYPTO_PKG.DECRYPT(EMAIL) = p_email AND ACTIVEFLAG = 'Y';
    p_success := 1;

EXCEPTION
    WHEN OTHERS THEN
        p_success := 0;
        p_out := 'Err: ' || SQLCODE || ' - ' || SQLERRM;
END;
--***** SP - Update Profile - Siva - End

--***** SP - Get MeetingURL by ConfCode - Siva - Begin
PROCEDURE GET_MEETING_URL_BY_CONFCODE (
    p_confcode      IN  VARCHAR2,
    p_meetingurl       OUT VARCHAR2,
    p_out OUT VARCHAR2
)
AS
BEGIN

    SELECT
        A.MeetingURL into p_meetingurl
    FROM
        APPOINTMENTS A
    WHERE LOWER(A.CONFCODE) = LOWER(p_confcode);

EXCEPTION
    WHEN OTHERS THEN
        p_out := 'Err: ' || SQLCODE || ' - ' || SQLERRM;

END;
--***** SP - Get MeetingURL by ConfCode - Siva - End

--***** SP - Get Verify the session expires - Siva - Begin
PROCEDURE GET_VERIFIED_SESSION_EXPIRES (
    P_EMAIL      IN  VARCHAR2,
    P_SESSIONID  IN  VARCHAR2,
    P_EXPIRESAT  OUT DATE,
    P_OUT        OUT VARCHAR2
)
AS
BEGIN
    SELECT EXPIRESAT
      INTO P_EXPIRESAT
      FROM (
            SELECT EXPIRESAT
              FROM FQ_SESSIONS
             WHERE LOWER(EMAIL) = LOWER(P_EMAIL)
               AND SESSIONID = P_SESSIONID
               AND VERIFIEDAT IS NOT NULL
             ORDER BY VERIFIEDAT DESC
           )
     WHERE ROWNUM = 1;

    P_OUT := NULL;
EXCEPTION
    WHEN OTHERS THEN
        P_EXPIRESAT := NULL;
        P_OUT := 'Err: ' || SQLCODE || ' - ' || SQLERRM;
END GET_VERIFIED_SESSION_EXPIRES;

--***** SP - Get Verify the session expires - Siva - End

--***** SP - Get valid entity - Siva - Begin
PROCEDURE GET_VALIDENTITY (
    p_entity_id  IN  NUMBER,
    p_json       OUT VARCHAR2
)
AS
BEGIN
    SELECT
        JSON_OBJECT (
             'entity_id'           VALUE ENTITY_ID
            ,'entity_name'         VALUE ENTITY_NAME
            ,'address'             VALUE ADDRESS
            ,'phone'               VALUE PHONE
            ,'opens_at'            VALUE TO_CHAR(OPENS_AT,            'YYYY-MM-DD"T"HH24:MI:SS')
            ,'closes_at'           VALUE TO_CHAR(CLOSES_AT,           'YYYY-MM-DD"T"HH24:MI:SS')
            ,'description'         VALUE DESCRIPTION
            ,'activeflag'          VALUE ACTIVEFLAG
            ,'outage_notify_begin' VALUE TO_CHAR(OUTAGE_NOTIFY_BEGIN, 'YYYY-MM-DD"T"HH24:MI:SS')
            ,'outage_begins_at'    VALUE TO_CHAR(OUTAGE_BEGINS_AT,    'YYYY-MM-DD"T"HH24:MI:SS')
            ,'outage_ends_at'      VALUE TO_CHAR(OUTAGE_ENDS_AT,      'YYYY-MM-DD"T"HH24:MI:SS')
            ,'outage_message'      VALUE OUTAGE_MESSAGE
            ,'stampdate'           VALUE TO_CHAR(STAMPDATE,           'YYYY-MM-DD"T"HH24:MI:SS')
            ,'stampuser'           VALUE STAMPUSER
            ABSENT ON NULL
        ) INTO p_json
    FROM FQOWNER.VALIDENTITIES
    WHERE ENTITY_ID = p_entity_id;

EXCEPTION
    WHEN NO_DATA_FOUND THEN
        p_json := NULL;
END GET_VALIDENTITY;
--***** SP - Get valid entity - Siva - End

FUNCTION GET_NEW_CONF_CODE RETURN VARCHAR2
AS
v_confcode VARCHAR2(10);
v_exists_flag NUMBER(1);
BEGIN
    LOOP
        -- Generate a random alpha characters only (mixed case) string of length 6
        v_confcode := DBMS_RANDOM.STRING('A', 6);

        -- Check if the generated string is already used
        SELECT COUNT(1) INTO v_exists_flag
        FROM appointments
        WHERE CONFCODE = v_confcode;

        -- Exit the loop if the string does not exist
        EXIT WHEN v_exists_flag = 0;
    END LOOP;

    RETURN v_confcode;
END;

PROCEDURE INSERT_WALKIN (
    p_email IN  VARCHAR2,
    p_json IN VARCHAR2,
    p_stampuser IN VARCHAR2,
    p_confcode OUT VARCHAR2,
    p_out OUT VARCHAR2
)
AS
    v_fname             VARCHAR2(50);
    v_lname             VARCHAR2(50);
    v_customerid        NUMBER;
    v_customerphone     VARCHAR2(15);
    v_queue_id          NUMBER;
    v_service_id        NUMBER;
    v_ref_criteria      VARCHAR2(3);
    v_ref_value         VARCHAR2(100);
    v_contacttype       VARCHAR2(3);
    v_moreinfo          VARCHAR2(100);
    v_language_pref     VARCHAR2(50);
    v_confcode          VARCHAR2(10);
    v_smsoptin          VARCHAR2(5);
    v_exists_flag       NUMBER(1);
    v_walkinid          NUMBER;
BEGIN
    -- Read info from Json - for SMS use later
    SELECT J.PHONE, J.SMSOPTIN
        INTO v_customerphone, v_smsoptin
    FROM JSON_TABLE(
        p_json,
        '$[*]' COLUMNS (
            PHONE PATH '$.PHONE',
            SMSOPTIN PATH '$.SMSOPTIN'
        )
    ) J;

    -- 1. INSERT into CUSTOMER if NOT Exists with ENCRYPTED PII
    BEGIN
        SELECT C.customer_id, FQ_CRYPTO_PKG.DECRYPT(C.phone)
            INTO v_customerid, v_customerphone
        FROM CUSTOMERS C
            WHERE lower(FQ_CRYPTO_PKG.DECRYPT(email)) = lower(p_email);
    EXCEPTION
        WHEN NO_DATA_FOUND THEN
            SELECT
                    J.FNAME, J.LNAME, J.PHONE
                INTO
                    v_fname, v_lname, v_customerphone
                FROM JSON_TABLE(
                    p_json,
                    '$[*]' COLUMNS (
                        FNAME PATH '$.FNAME',
                        LNAME PATH '$.LNAME',
                        PHONE PATH '$.PHONE'
                    )
                ) J;

            FQ_PROCS.INSERT_CUSTOMER(
                v_fname, v_lname, lower(p_email), v_customerphone,
                NULL, 'Y', p_stampuser,
                v_customerid, p_out);

            IF LENGTH(NVL(p_out, '')) > 0 OR v_customerid IS NULL THEN
                p_out := 'ERR adding customer. ' || p_out;
                RETURN;
            END IF;
    END;

     -- 2. INSERT into WALKINS
    SELECT
        J.*
    INTO
        v_queue_id,
        v_service_id,
        v_ref_criteria,
        v_ref_value,
        v_contacttype,
        v_moreinfo,
        v_language_pref
    FROM JSON_TABLE(
        p_json,
        '$[*]' COLUMNS (
            QUEUE_ID PATH '$.QUEUE_ID',
            SERVICE_ID PATH '$.SERVICE_ID',
            REF_CRITERIA PATH '$.REF_CRITERIA',
            REF_VALUE PATH '$.REF_VALUE',
            CONTACTTYPE PATH '$.CONTACTTYPE',
            MOREINFO PATH '$.MOREINFO',
            LANGUAGE_PREF PATH '$.LANGUAGE_PREF'
        )
    ) J;

    -- 3. INSERT into WALKINS
    FQ_PROCS.INSERT_WALKIN (
        v_customerid,
        v_queue_id,
        v_service_id,
        v_ref_criteria,
        v_ref_value,
        v_contacttype,
        v_moreinfo,
        '00 ' || to_char(sysdate, 'HH24:MI:SS'),
        NULL,
        'ARRIVED',
        NULL,
        v_language_pref,
        p_stampuser,
        p_stampuser,
        v_walkinid,
        p_out
    );

    -- INSERT_WALKIN already stores AMANDA details; do not look them up twice.
    IF p_out IS NOT NULL OR v_walkinid IS NULL THEN
        p_confcode := NULL;
        RETURN;
    END IF;
    p_confcode := 'W-' || v_walkinid;

EXCEPTION
    WHEN OTHERS THEN
        p_out := SQLERRM;
END;

PROCEDURE GET_ACTIVE_COUNTS (
p_email     IN  VARCHAR2,
p_appts     OUT NUMBER,
p_walkins   OUT NUMBER,
p_out       OUT VARCHAR2
)
AS
v_customerid NUMBER;
BEGIN
    p_appts := NULL;
    p_walkins := NULL;

    BEGIN
        SELECT customer_id INTO v_customerid
        FROM CUSTOMERS
        WHERE lower(FQ_CRYPTO_PKG.DECRYPT(email)) = lower(p_email);
    EXCEPTION
        WHEN NO_DATA_FOUND THEN
            -- Likely a New Customer, so send 0 to allow 'add new appt/walkin'
            p_appts := 0;
            p_walkins := 0;
        WHEN OTHERS THEN
            p_out := SQLERRM;
            RETURN;
    END;

    -- Get Current Active Appointments
    SELECT count(1) INTO p_appts
    FROM APPOINTMENTS
    WHERE   customer_id = v_customerid
        AND TRUNC(APPT_DATE) >= TRUNC(SYSDATE)
        AND UPPER(STATUS) = 'SCHEDULED';

    -- Get Today's Active Walkins
    SELECT count(1) INTO p_walkins
    FROM WALKINS
    WHERE   customer_id = v_customerid
        AND TRUNC(CREATEDON) = TRUNC(SYSDATE)
        AND UPPER(STATUS) = 'ARRIVED';
EXCEPTION
    WHEN OTHERS THEN
        p_out := SQLERRM;
END;

PROCEDURE IS_OPEN_FOR_WALKIN( p_entityid number, p_datetime DATE, p_isopen OUT NUMBER)
AS
v_open NUMBER :=0 ; -- assume False | 0 (False), 1 (True)
BEGIN
    p_isopen := 0;
    SELECT count(*) INTO v_open
        FROM DUAL
        WHERE
            TO_CHAR(p_datetime, 'DY') NOT IN ('SUN', 'SAT')
        AND
            NOT EXISTS (
                SELECT HolidayDate FROM VALIDHOLIDAYS
                        WHERE TRUNC(HolidayDate) = TRUNC(p_datetime))
        AND
            NOT EXISTS (
                    select 1 from validentities
                    WHERE entity_id = p_entityid
                        AND
                        p_datetime BETWEEN OUTAGE_BEGINS_AT AND  OUTAGE_ENDS_AT);

    -- If it is a working-day then check for operating-hours
    IF v_open = 1 THEN
        SELECT count(*) INTO v_open
            FROM validentities
            WHERE entity_id = p_entityid
                AND (p_datetime - trunc(p_datetime))
                        between (opens_at - trunc(opens_at))
                            and (closes_at - trunc(closes_at)) ;
    END IF;
    -- Finally return  0 (False), 1 (True)
    p_isopen := v_open;
END;

END FQ_EXTERNAL;
/
SHOW ERRORS PACKAGE BODY FQ_EXTERNAL;
