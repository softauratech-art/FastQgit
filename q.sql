--------------------------------------------------------
--  File created - Wednesday-January-07-2026   
--------------------------------------------------------
--------------------------------------------------------
--  DDL for Sequence APPTSEQ
--------------------------------------------------------

   CREATE SEQUENCE  "APPTSEQ"  MINVALUE 1 MAXVALUE 9999999999999999999999999999 INCREMENT BY 1 START WITH 1005 CACHE 5 NOORDER  NOCYCLE  NOKEEP  NOSCALE  GLOBAL
--------------------------------------------------------
--  DDL for Sequence CUSTOMERSEQ
--------------------------------------------------------

   CREATE SEQUENCE  "CUSTOMERSEQ"  MINVALUE 1 MAXVALUE 9999999999999999999999999999 INCREMENT BY 3 START WITH 10105 CACHE 20 NOORDER  NOCYCLE  NOKEEP  NOSCALE  GLOBAL
--------------------------------------------------------
--  DDL for Sequence QSCHEDULESEQ
--------------------------------------------------------

   CREATE SEQUENCE  "QSCHEDULESEQ"  MINVALUE 1 MAXVALUE 9999999999999999999999999999 INCREMENT BY 1 START WITH 10 CACHE 20 NOORDER  NOCYCLE  NOKEEP  NOSCALE  GLOBAL
--------------------------------------------------------
--  DDL for Sequence QSERVICESEQ
--------------------------------------------------------

   CREATE SEQUENCE  "QSERVICESEQ"  MINVALUE 1 MAXVALUE 9999999999999999999999999999 INCREMENT BY 1 START WITH 10061 CACHE 20 NOORDER  NOCYCLE  NOKEEP  NOSCALE  GLOBAL
--------------------------------------------------------
--  DDL for Sequence QUEUESEQ
--------------------------------------------------------

   CREATE SEQUENCE  "QUEUESEQ"  MINVALUE 1 MAXVALUE 9999999999999999999999999999 INCREMENT BY 2 START WITH 10041 CACHE 20 NOORDER  NOCYCLE  NOKEEP  NOSCALE  GLOBAL
--------------------------------------------------------
--  DDL for Sequence SIGNINSEQ
--------------------------------------------------------

   CREATE SEQUENCE  "SIGNINSEQ"  MINVALUE 1 MAXVALUE 9999999999999999999999999999 INCREMENT BY 1 START WITH 1000 CACHE 5 NOORDER  NOCYCLE  NOKEEP  NOSCALE  GLOBAL
--------------------------------------------------------
--  DDL for Sequence SVCTRANSSEQ
--------------------------------------------------------

   CREATE SEQUENCE  "SVCTRANSSEQ"  MINVALUE 1 MAXVALUE 9999999999999999999999999999 INCREMENT BY 1 START WITH 1 CACHE 10 NOORDER  NOCYCLE  NOKEEP  NOSCALE  GLOBAL
--------------------------------------------------------
--  DDL for Table APPOINTMENTS
--------------------------------------------------------

  CREATE TABLE "APPOINTMENTS" ("APPOINTMENT_ID" NUMBER, "CUSTOMER_ID" NUMBER, "REF_CRITERIA" VARCHAR2(3), "REF_VALUE" VARCHAR2(100), "QUEUE_ID" NUMBER(9,0), "SERVICE_ID" NUMBER(9,0), "CONTACTTYPE" VARCHAR2(3), "MOREINFO" VARCHAR2(1000), "APPT_DATE" DATE, "START_TIME" INTERVAL DAY (0) TO SECOND (0), "END_TIME" INTERVAL DAY (0) TO SECOND (0), "STATUS" VARCHAR2(10), "CONFCODE" VARCHAR2(10), "MEETINGURL" VARCHAR2(150), "LANGUAGE_PREF" VARCHAR2(3), "CREATEDBY" VARCHAR2(50), "CREATEDON" DATE DEFAULT SYSDATE, "STAMPUSER" VARCHAR2(50), "STAMPDATE" DATE DEFAULT SYSDATE)   NO INMEMORY
--------------------------------------------------------
--  DDL for Table CUSTOMERS
--------------------------------------------------------

  CREATE TABLE "CUSTOMERS" ("CUSTOMER_ID" NUMBER, "FNAME" RAW(1000), "LNAME" RAW(1000), "EMAIL" RAW(1000), "PHONE" RAW(1000), "SMS_OPTIN" CHAR(1) DEFAULT NULL, "ACTIVEFLAG" CHAR(1) DEFAULT 'Y', "STAMPDATE" DATE DEFAULT SYSDATE, "STAMPUSER" VARCHAR2(50))   NO INMEMORY
--------------------------------------------------------
--  DDL for Table FQUSERS
--------------------------------------------------------

  CREATE TABLE "FQUSERS" ("USER_ID" VARCHAR2(50), "FNAME" VARCHAR2(100), "LNAME" VARCHAR2(100), "EMAIL" VARCHAR2(1000), "PHONE" VARCHAR2(15), "LANGUAGE" VARCHAR2(100), "ACTIVEFLAG" CHAR(1) DEFAULT 'Y', "PASSWORD" VARCHAR2(50), "ADMINFLAG" CHAR(1) DEFAULT 'N', "TITLE" VARCHAR2(50), "STAMPDATE" DATE DEFAULT SYSDATE, "STAMPUSER" VARCHAR2(50))   NO INMEMORY
--------------------------------------------------------
--  DDL for Table FQ_SESSIONS
--------------------------------------------------------

  CREATE TABLE "FQ_SESSIONS" ("EMAIL" VARCHAR2(100), "SESSIONID" VARCHAR2(200), "AUTHCODE" VARCHAR2(100), "STARTEDAT" DATE, "VERIFIEDAT" DATE, "EXPIRESAT" DATE, "STAMPDATE" DATE DEFAULT SYSDATE)   NO INMEMORY
--------------------------------------------------------
--  DDL for Table PR_TEST
--------------------------------------------------------

  CREATE TABLE "PR_TEST" ("CUST_NUM" NUMBER(*,0), "SORT_ORDER" NUMBER(*,0), "CATEGORY" VARCHAR2(100))   NO INMEMORY
--------------------------------------------------------
--  DDL for Table PR_TEST_FQ
--------------------------------------------------------

  CREATE TABLE "PR_TEST_FQ" ("EMAILADDRESS" VARCHAR2(200), "FNAME" VARCHAR2(100), "LNAME" VARCHAR2(100), "PHONE" VARCHAR2(50), "EMAILENC" RAW(2000), "FNAME_ENC" RAW(2000), "LNAME_ENC" RAW(2000), "PHONE_ENC" RAW(2000))   NO INMEMORY
--------------------------------------------------------
--  DDL for Table QUEUE_CONTACTTYPES
--------------------------------------------------------

  CREATE TABLE "QUEUE_CONTACTTYPES" ("QUEUE_ID" NUMBER(9,0), "CONTACT_KEY" VARCHAR2(3))   NO INMEMORY
--------------------------------------------------------
--  DDL for Table QUEUE_REFCRITERIAS
--------------------------------------------------------

  CREATE TABLE "QUEUE_REFCRITERIAS" ("QUEUE_ID" NUMBER(9,0), "CRITERIA_KEY" VARCHAR2(3))   NO INMEMORY
--------------------------------------------------------
--  DDL for Table QUEUE_SCHEDULES
--------------------------------------------------------

  CREATE TABLE "QUEUE_SCHEDULES" ("SCHEDULE_ID" NUMBER(9,0), "QUEUE_ID" NUMBER(9,0), "DATE_BEGIN" DATE, "DATE_END" DATE, "OPEN_TIME" INTERVAL DAY (0) TO SECOND (0), "CLOSE_TIME" INTERVAL DAY (0) TO SECOND (0), "INTERVAL_TIME" INTERVAL DAY (0) TO SECOND (0), "WEEKLY_SCH" VARCHAR2(7), "AVAILABLE_RESOURCES" NUMBER(1,0) DEFAULT 1, "STAMPDATE" DATE DEFAULT SYSDATE, "STAMPUSER" VARCHAR2(50))   NO INMEMORY
--------------------------------------------------------
--  DDL for Table SERVICETRANSACTIONS
--------------------------------------------------------

  CREATE TABLE "SERVICETRANSACTIONS" ("TRANSACTION_ID" NUMBER, "SRC_TYPE" CHAR(1), "SRC_ID" NUMBER, "QUEUE_ID" NUMBER(9,0), "SERVICE_ID" NUMBER(9,0), "CHECKIN_TIME" DATE, "CHECKOUT_TIME" DATE, "SERVICE_START_TIME" DATE, "SERVICE_END_TIME" DATE, "STATUS" VARCHAR2(10), "SERVICE_NOTES" VARCHAR2(1000), "STAMPUSER" VARCHAR2(50), "STAMPDATE" DATE DEFAULT SYSDATE)   NO INMEMORY
--------------------------------------------------------
--  DDL for Table SIGNINS
--------------------------------------------------------

  CREATE TABLE "SIGNINS" ("SIGNIN_ID" NUMBER, "CUSTOMER_ID" NUMBER(9,0), "REF_CRITERIA" VARCHAR2(3), "REF_VALUE" VARCHAR2(100), "QUEUE_ID" NUMBER(9,0), "SERVICE_ID" NUMBER(9,0), "CONTACTTYPE" VARCHAR2(3), "MOREINFO" VARCHAR2(1000), "JOIN_TIME" INTERVAL DAY (0) TO SECOND (0), "END_TIME" INTERVAL DAY (0) TO SECOND (0), "STATUS" VARCHAR2(10), "LANGUAGE_PREF" VARCHAR2(3), "CREATEDBY" VARCHAR2(50), "CREATEDON" DATE DEFAULT SYSDATE, "STAMPUSER" VARCHAR2(50), "STAMPDATE" DATE DEFAULT SYSDATE)   NO INMEMORY
--------------------------------------------------------
--  DDL for Table USER_PERMISSIONS
--------------------------------------------------------

  CREATE TABLE "USER_PERMISSIONS" ("USER_ID" VARCHAR2(50), "QUEUE_ID" NUMBER(9,0), "ROLE_ID" NUMBER(9,0), "STAMPDATE" DATE DEFAULT SYSDATE, "STAMPUSER" VARCHAR2(50))   NO INMEMORY
--------------------------------------------------------
--  DDL for Table VALIDCONTACTTYPES
--------------------------------------------------------

  CREATE TABLE "VALIDCONTACTTYPES" ("TYPE_KEY" VARCHAR2(3), "TYPE_VAL" VARCHAR2(100), "TYPE_VAL_ES" VARCHAR2(100), "TYPE_VAL_CP" VARCHAR2(100), "ACTIVEFLAG" CHAR(1) DEFAULT 'Y')   NO INMEMORY
--------------------------------------------------------
--  DDL for Table VALIDHOLIDAYS
--------------------------------------------------------

  CREATE TABLE "VALIDHOLIDAYS" ("HOLIDAYDATE" DATE, "HOLIDAYDESC" VARCHAR2(100), "STAMPDATE" DATE DEFAULT SYSDATE, "STAMPUSER" VARCHAR2(100), "ACTIVEFLAG" CHAR(1) DEFAULT 'Y')   NO INMEMORY
--------------------------------------------------------
--  DDL for Table VALIDLANGUAGES
--------------------------------------------------------

  CREATE TABLE "VALIDLANGUAGES" ("LANG_KEY" VARCHAR2(3), "LANG_VAL" VARCHAR2(100), "ACTIVEFLAG" CHAR(1) DEFAULT 'Y')   NO INMEMORY
--------------------------------------------------------
--  DDL for Table VALIDLOCATIONS
--------------------------------------------------------

  CREATE TABLE "VALIDLOCATIONS" ("LOCATION_ID" NUMBER(9,0), "LOCNAME" VARCHAR2(100), "ADDRESS" VARCHAR2(500), "PHONE" VARCHAR2(15), "OPENS_AT" DATE, "CLOSES_AT" DATE, "DESCRIPTION" VARCHAR2(100), "ACTIVEFLAG" CHAR(1) DEFAULT 'Y')   NO INMEMORY
--------------------------------------------------------
--  DDL for Table VALIDQUEUES
--------------------------------------------------------

  CREATE TABLE "VALIDQUEUES" ("QUEUE_ID" NUMBER(9,0), "NAME" VARCHAR2(100), "NAME_ES" VARCHAR2(100), "NAME_CP" VARCHAR2(100), "LOCATION_ID" NUMBER(9,0), "ACTIVEFLAG" CHAR(1) DEFAULT 'Y', "EMP_ONLY" CHAR(1), "HIDE_IN_KIOSK" CHAR(1), "HIDE_IN_MONITOR" CHAR(1), "LEAD_TIME_MIN" VARCHAR2(100), "LEAD_TIME_MAX" VARCHAR2(100), "HAS_GUIDELINES" CHAR(1), "HAS_UPLOADS" CHAR(1))   NO INMEMORY
--------------------------------------------------------
--  DDL for Table VALIDQUEUE_SERVICES
--------------------------------------------------------

  CREATE TABLE "VALIDQUEUE_SERVICES" ("SERVICE_ID" NUMBER(9,0), "QUEUE_ID" NUMBER(9,0), "ACTIVEFLAG" CHAR(1) DEFAULT 'Y', "SERVICE_NAME" VARCHAR2(100), "SERVICE_NAME_ES" VARCHAR2(100), "SERVICE_NAME_CP" VARCHAR2(100))   NO INMEMORY
--------------------------------------------------------
--  DDL for Table VALIDREFERENCECRITERIAS
--------------------------------------------------------

  CREATE TABLE "VALIDREFERENCECRITERIAS" ("REF_KEY" VARCHAR2(3), "REF_VAL" VARCHAR2(100), "REF_VAL_ES" VARCHAR2(100), "REF_VAL_CP" VARCHAR2(100), "ACTIVEFLAG" CHAR(1) DEFAULT 'Y')   NO INMEMORY
--------------------------------------------------------
--  DDL for Table VALIDROLES
--------------------------------------------------------

  CREATE TABLE "VALIDROLES" ("ROLE_ID" NUMBER(9,0), "ROLE_DESC" VARCHAR2(100), "ACTIVEFLAG" CHAR(1) DEFAULT 'Y')   NO INMEMORY
--------------------------------------------------------
--  DDL for Table VALIDSERVICESTATUS
--------------------------------------------------------

  CREATE TABLE "VALIDSERVICESTATUS" ("STATUS_KEY" VARCHAR2(3), "STATUS_VAL" VARCHAR2(100), "ACTIVEFLAG" CHAR(1) DEFAULT 'Y')   NO INMEMORY
--------------------------------------------------------
--  DDL for View VW_QUEUE_DETAILS_JSON
--------------------------------------------------------

  CREATE OR REPLACE EDITIONABLE VIEW "VW_QUEUE_DETAILS_JSON" ("QUEUE_ID", "Q_SERVICES", "Q_SCHEDULES", "Q_DETAILS") AS SELECT q.queue_id,
        JSON_OBJECT(       
        q.queue_id, 
        'services' VALUE (
                SELECT
                    JSON_ARRAYAGG(
                        JSON_OBJECT(service_id,service_name, service_name_es, service_name_cp)
                        )
                FROM
                     VALIDQUEUE_SERVICES s
                WHERE
                    s.queue_id = q.queue_id
            )) as q_services,
        JSON_OBJECT(       
            q.queue_id, 
            'schedules' VALUE (
                SELECT
                    JSON_ARRAYAGG(
                        JSON_OBJECT(schedule_id, date_begin, date_end,
                                    open_time, close_time,interval_time, 
                                    weekly_sch, available_resources)
                        )
                FROM
                    QUEUE_SCHEDULES qs
                WHERE SYSDATE BETWEEN DATE_BEGIN and DATE_END 
                    AND qs.queue_id = q.queue_id            
            )) as q_schedules,            
        JSON_OBJECT(
        q.queue_id, q.name, q.name_cp, q.name_es,
        L.locname, L.address, L.phone,  
        'configOptions' VALUE (                
                JSON_OBJECT(q.lead_time_max,q.lead_time_min, q.has_uploads, q.has_guidelines
                            ,q.emp_only, q.hide_in_monitor, q.hide_in_kiosk, q.activeflag)               
            ),
        'contactoptions' VALUE (
                SELECT
                    JSON_ARRAYAGG(
                        JSON_OBJECT(type_key,type_val,type_val_es,type_val_cp)
                        )
                FROM
                     VALIDCONTACTTYPES ct
                                inner join queue_contacttypes qct
                                 on ct.type_key = qct.contact_key                       
                WHERE
                    qct.queue_id = q.queue_id
            ),
        'refoptions' VALUE (
                SELECT
                    JSON_ARRAYAGG(
                        JSON_OBJECT(ref_key,ref_val,ref_val_es,ref_val_cp)
                        )
                FROM
                     VALIDREFERENCECRITERIAS rc
                                inner join QUEUE_REFCRITERIAS qrc
                                    on rc.ref_key = qrc.criteria_key                       
                WHERE
                    qrc.queue_id = q.queue_id
            )
        ) AS q_details --INTO p_json
    FROM
        VALIDQUEUES q
            INNER JOIN VALIDLOCATIONS l ON q.location_id = l.location_id
--------------------------------------------------------
--  DDL for View VW_QUEUE_DETAILS_JSON_1
--------------------------------------------------------

  CREATE OR REPLACE EDITIONABLE VIEW "VW_QUEUE_DETAILS_JSON_1" ("QUEUE_ID", "JSON_RESULT") AS SELECT 
        q.queue_id,
        JSON_OBJECT(
        q.queue_id, q.name, q.name_cp, q.name_es,
        L.locname, L.address, L.phone,
        'services' VALUE (
                SELECT
                    JSON_ARRAYAGG(
                        JSON_OBJECT(service_id,service_name, service_name_es, service_name_cp)
                        )
                FROM
                     VALIDQUEUE_SERVICES s
                WHERE
                    s.queue_id = q.queue_id
            ),
            'schedules' VALUE (
                SELECT
                    JSON_ARRAYAGG(
                        JSON_OBJECT(schedule_id, date_begin, date_end,
                                    open_time, close_time,interval_time, 
                                    weekly_sch, available_resources)
                        )
                FROM
                    QUEUE_SCHEDULES qs
                WHERE SYSDATE BETWEEN DATE_BEGIN and DATE_END 
                    AND qs.queue_id = q.queue_id
            ),
        'configOptions' VALUE (                
                JSON_OBJECT(q.lead_time_max,q.lead_time_min, q.has_uploads, q.has_guidelines
                            ,q.emp_only, q.hide_in_monitor, q.hide_in_kiosk, q.activeflag)               
            ),
        'contactoptions' VALUE (
                SELECT
                    JSON_ARRAYAGG(
                        JSON_OBJECT(type_key,type_val,type_val_es,type_val_cp)
                        )
                FROM
                     VALIDCONTACTTYPES ct
                                inner join queue_contacttypes qct
                                 on ct.type_key = qct.contact_key                       
                WHERE
                    qct.queue_id = q.queue_id
            ),
        'refoptions' VALUE (
                SELECT
                    JSON_ARRAYAGG(
                        JSON_OBJECT(ref_key,ref_val,ref_val_es,ref_val_cp)
                        )
                FROM
                     VALIDREFERENCECRITERIAS rc
                                inner join QUEUE_REFCRITERIAS qrc
                                    on rc.ref_key = qrc.criteria_key                       
                WHERE
                    qrc.queue_id = q.queue_id
            )
        ) AS json_result --INTO p_json
    FROM
        VALIDQUEUES q
            INNER JOIN VALIDLOCATIONS l ON q.location_id = l.location_id
--------------------------------------------------------
--  DDL for Index QUEUE_CONTACT_UQ
--------------------------------------------------------

  CREATE UNIQUE INDEX "QUEUE_CONTACT_UQ" ON "QUEUE_CONTACTTYPES" ("QUEUE_ID", "CONTACT_KEY")
--------------------------------------------------------
--  DDL for Index QUEUE_REFCRI_UQ
--------------------------------------------------------

  CREATE UNIQUE INDEX "QUEUE_REFCRI_UQ" ON "QUEUE_REFCRITERIAS" ("QUEUE_ID", "CRITERIA_KEY")
--------------------------------------------------------
--  DDL for Index USRPERM_UQ
--------------------------------------------------------

  CREATE UNIQUE INDEX "USRPERM_UQ" ON "USER_PERMISSIONS" ("USER_ID", "QUEUE_ID", "ROLE_ID")
--------------------------------------------------------
--  DDL for Index USRSESSIONS_UQ
--------------------------------------------------------

  CREATE UNIQUE INDEX "USRSESSIONS_UQ" ON "FQ_SESSIONS" ("EMAIL", "SESSIONID", "AUTHCODE")
--------------------------------------------------------
--  DDL for Index VALIDLOCATIONS_NAME_UK
--------------------------------------------------------

  CREATE UNIQUE INDEX "VALIDLOCATIONS_NAME_UK" ON "VALIDLOCATIONS" ("LOCNAME")
--------------------------------------------------------
--  DDL for Index VALIDQUEUE_SERVICES_UQ
--------------------------------------------------------

  CREATE UNIQUE INDEX "VALIDQUEUE_SERVICES_UQ" ON "VALIDQUEUE_SERVICES" ("QUEUE_ID", "SERVICE_NAME")
--------------------------------------------------------
--  DDL for Package FQ_CRYPTO_PKG
--------------------------------------------------------

  CREATE OR REPLACE EDITIONABLE PACKAGE "FQ_CRYPTO_PKG" AS 

FUNCTION ENCRYPT (
    input_string       IN   VARCHAR2        
) RETURN RAW;

FUNCTION DECRYPT 
( 
    encrypted_raw      IN RAW        -- stores encrypted binary text
) RETURN VARCHAR2;

END FQ_CRYPTO_PKG;
--------------------------------------------------------
--  DDL for Package FQ_EXTERNAL
--------------------------------------------------------

  CREATE OR REPLACE EDITIONABLE PACKAGE "FQ_EXTERNAL" AS 
/******************************************************************************
NAME:       FQ_EXTERNAL Package
PURPOSE:    Contains all DB calls used by External DMZ application 
            (Customer-Appt-Scheduler)
----------  ---------   ------------------------------------------------------
DATE        AUTHOR       NOTES
----------  ---------   ------------------------------------------------------
2025.12.31  PREDDY      Created package
******************************************************************************/
    PROCEDURE GET_QUEUES (p_location IN INT, p_ref_cursor   OUT  Ref_Cursor_Types.ref_cursor);
    
    PROCEDURE GET_QUEUE_DETAILS_JSON (p_queueid IN NUMBER, p_ref_cursor   OUT  Ref_Cursor_Types.ref_cursor);  --TODO: Find way to return CLOB over DBlink
    PROCEDURE GET_QUEUE_DETAILS (p_queueid IN NUMBER, p_details_cursor   OUT  Ref_Cursor_Types.ref_cursor);
    PROCEDURE GET_QUEUE_OPENSLOTS (p_queueid IN NUMBER, p_date IN DATE, p_ref_cursor    OUT  Ref_Cursor_Types.ref_cursor);
    
    --TODO:
    --Pass customerid (and TOKEN?) to ALL procs from customer-specific external calls   
    PROCEDURE GET_MYHISTORY (p_customerid IN  RAW, p_ref_cursor OUT Ref_Cursor_Types.ref_cursor);    
    PROCEDURE GET_MYAPPOINTMENTS (p_customerid IN RAW, p_range_startdate IN DATE, p_range_enddate IN DATE, p_ref_cursor OUT Ref_Cursor_Types.ref_cursor);
    PROCEDURE GET_MYAPPOINTMENT  (p_customerid IN RAW, p_apptid IN RAW, p_ref_cursor OUT Ref_Cursor_Types.ref_cursor); 
    PROCEDURE GET_MYPROFILE      (p_customerid IN  RAW, p_ref_cursor OUT Ref_Cursor_Types.ref_cursor);    
  
  
    PROCEDURE CANCEL_APPT (p_customerid IN RAW, p_apptid IN RAW, p_reason IN VARCHAR2, p_outmsg OUT VARCHAR2);
    --PROCEDURE INSERT_APPT (p_customerid IN NUMBER, p_qid IN NUMBER, p_sid IN INT, p_date IN DATE, p_time IN INTERVAL, p_contact IN VARCHAR2, p_ref IN VARCHAR2, p_ref_val IN VARCHAR2, p_outid OUT INT, p_confcode OUT VARCHAR2);
    PROCEDURE INSERT_APPT (p_customerid IN RAW, p_json IN VARCHAR2, p_out OUT VARCHAR2);
    
    -- TODO: AUTH METHODS go here or in INETxxx DB?....
    -- PROCEDURE SENDEMAILCODE(????)
    -- PROCEDURE AUTHENTICATE(?????)
    -- FUNCTION ISACTIVEESSION(???)
END FQ_EXTERNAL;
--------------------------------------------------------
--  DDL for Package FQ_PROCS
--------------------------------------------------------

  CREATE OR REPLACE EDITIONABLE PACKAGE "FQ_PROCS" as          
    PROCEDURE GET_QUEUES (p_location IN INT, p_ref_cursor   OUT  Ref_Cursor_Types.ref_cursor);
    PROCEDURE GET_QUEUE_DETAILS (p_queueid IN INT, p_ref_cursor   OUT  Ref_Cursor_Types.ref_cursor);
    PROCEDURE GET_QUEUE_DETAILS (p_queueid IN INT, p_json OUT  CLOB);
    PROCEDURE GET_QUEUE_OPENSLOTS (p_queueid IN INT, p_date IN DATE, p_ref_cursor    OUT  Ref_Cursor_Types.ref_cursor);
    --
    PROCEDURE GET_APPTS4STAFF (p_userid IN VARCHAR2, p_range_startdate IN DATE, p_range_enddate IN DATE, p_ref_cursor OUT Ref_Cursor_Types.ref_cursor);
    PROCEDURE GET_APPT_DETAILS (p_apptid IN Appointments.appointment_id%type, p_ref_cursor OUT Ref_Cursor_Types.ref_cursor); 
    
    --TODO:
    --Pass stampuser to ALL procs from internal calls
    --Pass customerid (and token?) to ALL procs from external calls
    
END FQ_PROCS;
--------------------------------------------------------
--  DDL for Package REF_CURSOR_TYPES
--------------------------------------------------------

  CREATE OR REPLACE EDITIONABLE PACKAGE "REF_CURSOR_TYPES" 
AS TYPE ref_cursor IS REF CURSOR;
end;
--------------------------------------------------------
--  DDL for Package Body FQ_CRYPTO_PKG
--------------------------------------------------------

  CREATE OR REPLACE EDITIONABLE PACKAGE BODY "FQ_CRYPTO_PKG" AS
    key_bytes_raw      RAW(2000)             -- stores 256-bit encryption key
                        := 'C38798011FC27A756D615CB8082A6BF8F5537D2D25ECCF8DA26596A6146E408D';
    iv_raw             RAW(16)   
                        :=  '95BB6A9952026C9FB0B597C2175B00B3';
    encryption_type    PLS_INTEGER :=          -- total encryption type
                        DBMS_CRYPTO.ENCRYPT_AES256
                      + DBMS_CRYPTO.CHAIN_CBC
                      + DBMS_CRYPTO.PAD_PKCS5;

    FUNCTION ENCRYPT (
        input_string    IN   VARCHAR2      
    ) RETURN RAW
    AS
        encrypted_raw      RAW(2000);            -- stores encrypted binary text 
    BEGIN
        -- Encrypt and return RAW
        encrypted_raw := DBMS_CRYPTO.ENCRYPT(
                             src => UTL_I18N.STRING_TO_RAW (input_string,  'AL32UTF8'),
                             typ => encryption_type,
                             key => key_bytes_raw,
                             iv  => iv_raw
                          );        
        RETURN encrypted_raw;        
        --TODO: Handle exceptions
    END ENCRYPT;

    FUNCTION DECRYPT
    ( 
        encrypted_raw      IN RAW        -- stores encrypted binary text         
    ) RETURN VARCHAR2 
    AS
        decrypted_raw      RAW(2000);           -- stores decrypted binary text
        output_string     VARCHAR2(200);
    BEGIN    
        decrypted_raw := DBMS_CRYPTO.DECRYPT
          (
             src => encrypted_raw,
             typ => encryption_type,
             key => key_bytes_raw,
             iv  => iv_raw
          );        
        output_string := UTL_I18N.RAW_TO_CHAR (decrypted_raw, 'AL32UTF8');        
        RETURN output_string;
        --TODO: Handle exceptions
    END DECRYPT;

END FQ_CRYPTO_PKG;
--------------------------------------------------------
--  DDL for Package Body FQ_EXTERNAL
--------------------------------------------------------

  CREATE OR REPLACE EDITIONABLE PACKAGE BODY "FQ_EXTERNAL" as
--*******************************************************
-- 2025.12.31   PREDDY      Created package
-- 
--*******************************************************
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

PROCEDURE GET_QUEUE_DETAILS_JSON (
    p_queueid     IN NUMBER,    
    p_ref_cursor   OUT  Ref_Cursor_Types.ref_cursor
)
AS
--p_json   VARCHAR2(4000);  --TODO: Find way to return CLOB over DBlink
BEGIN
    OPEN p_ref_cursor FOR    
      SELECT queue_id, Q_services, Q_Schedules, Q_Details
      FROM VW_QUEUE_DETAILS_JSON Q
      WHERE  q.queue_id = p_queueid;    
END;   

PROCEDURE GET_QUEUE_DETAILS (
    p_queueid       IN NUMBER, 
    p_details_cursor    OUT  Ref_Cursor_Types.ref_cursor
    --,p_services_cursor    OUT  Ref_Cursor_Types.ref_cursor    
    --,p_schedules_cursor    OUT  Ref_Cursor_Types.ref_cursor
    )
AS
BEGIN
 -- This query excludes Schedule details
 OPEN p_details_cursor FOR    
    SELECT 
        Q.*,S.SERVICE_NAME, S.SERVICE_NAME_ES, S.SERVICE_NAME_CP,
        L.LOCNAME, L.ADDRESS, L.PHONE,
        contactmethod_list,criteria_list, Wkly_Sch
    FROM VALIDQUEUES Q
            INNER JOIN VALIDQUEUE_SERVICES S
                ON Q.QUEUE_ID = S.QUEUE_ID
            INNER JOIN VALIDLOCATIONS L
                ON Q.LOCATION_ID = L.LOCATION_ID
            INNER JOIN (SELECT queue_id,
                            LISTAGG(type_key, ',') WITHIN GROUP (ORDER BY type_key) AS contactmethod_list
                        from validcontacttypes ct
                            inner join queue_contacttypes qct
                             on ct.type_key = qct.contact_key
                        group by queue_id) C
                ON  Q.QUEUE_ID = C.QUEUE_ID
            INNER JOIN (SELECT queue_id,
                            LISTAGG(ref_key, ',') WITHIN GROUP (ORDER BY ref_key) AS criteria_list
                        from VALIDREFERENCECRITERIAS rc
                            inner join QUEUE_REFCRITERIAS qrc
                                on rc.ref_key = qrc.criteria_key
                        group by queue_id) R
                ON  Q.QUEUE_ID = R.QUEUE_ID
            LEFT OUTER JOIN  (SELECT queue_id, 
                                LISTAGG(Weekly_sch) WITHIN GROUP (ORDER BY schedule_id) AS Wkly_Sch 
                              FROM QUEUE_SCHEDULES 
                              Where SYSDATE BETWEEN DATE_BEGIN and DATE_END 
                              group by queue_id) QS
                ON Q.QUEUE_ID = qs.QUEUE_ID          
    WHERE NVL(Q.ACTIVEFLAG, 'N') = 'Y'
            and q.queue_id = p_queueid;
            
END;    

PROCEDURE GET_QUEUE_OPENSLOTS (
    p_queueid       IN NUMBER,
    p_date          IN DATE,
    p_ref_cursor    OUT  Ref_Cursor_Types.ref_cursor
)
AS
BEGIN
 OPEN p_ref_cursor FOR    
   SELECT * FROM ( 
    --- Recursive CTE to get avaialable-open-slots based on 
    --  SelectedDate, ServiceID (IN params)
    ---   AND Available_Resources setting
    ---   AND WeeklySch - dayOfWeek
    ---   AND existing ScheduledAppointments
    --    AND HolidaySchedule
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
             and to_char(to_date(p_date,'YYYY-MM-DD'), 'd') IN (select to_char(SUBSTR(Weekly_Sch, LEVEL, 1)) as weekday from dual connect by level <= length(Weekly_Sch))
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
            slot_begin, slot_end, Weekly_Sch
            ,interval_time ,Available_Resources, DuplicatesLevel, a.appointment_id
            --,count(*) avail_spots
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
     -- Use GROUP-BY to get totals for each begin-end slots (toggle lines 46,47 ogf this query)
     -- group by theDate, d.queue_id, slot_begin, slot_end, Weekly_Sch
    );            
END; 

PROCEDURE GET_MYHISTORY (
    p_customerid        IN  RAW,   
    p_ref_cursor        OUT Ref_Cursor_Types.ref_cursor
)
AS
BEGIN
 OPEN p_ref_cursor FOR
    SELECT  FQ_CRYPTO_PKG.ENCRYPT(A.Appointment_Id) Appointment_Id,
            FQ_CRYPTO_PKG.ENCRYPT(A.CUSTOMER_ID) Customer_Id, 
            A.REF_CRITERIA, A.REF_VALUE, A.QUEUE_ID, A.SERVICE_ID,
            A.CONTACTTYPE, A.MOREINFO, A.APPT_DATE,
            A.START_TIME, A.END_TIME, A.STATUS,
            A.CONFCODE, A.MEETINGURL, A.LANGUAGE_PREF 
    FROM APPOINTMENTS  A
    WHERE CUSTOMER_ID = FQ_CRYPTO_PKG.DECRYPT(p_customerid)
        AND TO_DATE(TO_CHAR(TRUNC(appt_date) + start_time, 'HH:MI AM'),'MM-DD-YYYY HH:MI AM') < SYSDATE
        AND TRUNC(appt_date) > ADD_MONTHS(TRUNC(SYSDATE), -6);
    --TODO: Add additional conditions and Join-Tables
END;

PROCEDURE GET_MYAPPOINTMENTS (
    p_customerid        IN  RAW,
    p_range_startdate   IN  DATE,
    p_range_enddate     IN  DATE,
    p_ref_cursor        OUT Ref_Cursor_Types.ref_cursor
)
AS
BEGIN
 OPEN p_ref_cursor FOR
    SELECT  FQ_CRYPTO_PKG.ENCRYPT(A.Appointment_Id) Appointment_Id,
            FQ_CRYPTO_PKG.ENCRYPT(A.CUSTOMER_ID) Customer_Id, 
            A.REF_CRITERIA, A.REF_VALUE, A.QUEUE_ID, A.SERVICE_ID,
            A.CONTACTTYPE, A.MOREINFO, A.APPT_DATE,
            A.START_TIME, A.END_TIME, A.STATUS,
            A.CONFCODE, A.MEETINGURL, A.LANGUAGE_PREF 
    FROM APPOINTMENTS A
    WHERE CUSTOMER_ID = FQ_CRYPTO_PKG.DECRYPT(p_customerid)
        AND trunc(appt_date) BETWEEN trunc(p_range_startdate) AND trunc(p_range_enddate);
    --TODO: Add additional conditions and Join-Tables
END;

PROCEDURE GET_MYAPPOINTMENT  (
    p_customerid IN RAW,
    p_apptid IN RAW, 
    --p_apptidenc IN Varchar2,
    p_ref_cursor OUT Ref_Cursor_Types.ref_cursor
)
AS
BEGIN
 OPEN p_ref_cursor FOR
    SELECT  FQ_CRYPTO_PKG.ENCRYPT(A.Appointment_Id) Appointment_Id,
            FQ_CRYPTO_PKG.ENCRYPT(A.CUSTOMER_ID) Customer_Id, 
            A.REF_CRITERIA, A.REF_VALUE, A.QUEUE_ID, A.SERVICE_ID,
            A.CONTACTTYPE, A.MOREINFO, A.APPT_DATE,
            A.START_TIME, A.END_TIME, A.STATUS,
            A.CONFCODE, A.MEETINGURL, A.LANGUAGE_PREF 
    FROM APPOINTMENTS A
    WHERE appointment_id = FQ_CRYPTO_PKG.DECRYPT(p_apptid)
        AND customer_id = FQ_CRYPTO_PKG.DECRYPT(p_customerid);
    --TODO: Add additional conditions and Join-Tables
END; 

PROCEDURE GET_MYPROFILE (
    p_customerid IN  RAW, 
    p_ref_cursor OUT Ref_Cursor_Types.ref_cursor
)
AS
BEGIN
 OPEN p_ref_cursor FOR
    SELECT FQ_CRYPTO_PKG.ENCRYPT(C.customer_id) customer_id, SMS_OPTIN
        ,FQ_CRYPTO_PKG.DECRYPT(fname) firstname, FQ_CRYPTO_PKG.DECRYPT(lname) lastname
        ,FQ_CRYPTO_PKG.DECRYPT(email) email, FQ_CRYPTO_PKG.DECRYPT(phone) phone    
    FROM 
        CUSTOMERS C
    WHERE C.customer_id = FQ_CRYPTO_PKG.DECRYPT(p_customerid);
    --TODO: Add any additional conditions and Join-Tables
END;    

PROCEDURE CANCEL_APPT (p_customerid IN RAW, p_apptid IN RAW, p_reason IN VARCHAR2, p_outmsg OUT VARCHAR2)
AS
BEGIN
    --TODO: Finalize proc
    UPDATE APPOINTMENTS 
        SET status = 'C'
        WHERE customer_id = FQ_CRYPTO_PKG.DECRYPT(p_customerid)
            AND appointment_id = FQ_CRYPTO_PKG.DECRYPT(p_apptid)
            AND TRUNC(appt_date) > TRUNC(sysdate) -- Past appointments CANNOT be cancelled
            AND status <> 'C';
    p_outmsg := 'Success';  -- or null => check Siva's pref
EXCEPTION
    WHEN OTHERS THEN
        p_outmsg := 'Err: ' || SQLCODE || ' - ' || SQLERRM;
END;    
    
--PROCEDURE INSERT_APPT (p_customerid IN INT, p_qid IN INT, p_sid IN INT, p_date IN DATE, p_time IN INTERVAL, p_contact IN VARCHAR2, p_ref IN VARCHAR2, p_ref_val IN VARCHAR2, p_outid OUT INT, p_confcode OUT VARCHAR2);
PROCEDURE INSERT_APPT (
    p_customerid IN RAW, 
    p_json IN VARCHAR2, 
    p_out OUT VARCHAR2
)
AS
/*
p_json VARCHAR2(1000) := '[
            {"REF_CRITERIA": "G", 
             "REF_VALUE": "Questions",
             "QUEUE_ID": 10003,  
             "SERVICE_ID": 10004, 
             "CONTACTTYPE": "PC",  
             "MOREINFO": "none", 
             "APPT_DATE": "19-JAN-26 12.00.00 AM", 
             "START_TIME": "+00 13:00:00.000000", 
             "END_TIME": "+00 14:00:00.000000", 
             "STATUS": "SCHEDULED", 
             "CONFCODE": "c3s7g9g7", 
             "MEETINGURL": null, 
             "LANGUAGE_PREF": null,
             "FNAME": "First",
             "LNAME": "Last",
             "EMAIL": "someemail@domain.com",
             "PHONE": "123-456-7890",
             "SMSOPTIN": "Y"
         }]';
*/

BEGIN

    -- 1. INSERT into CUSTOMER if NOT Exists with ENCRYPTED PII
    --TODO: 1. Check if customerEXISTS -->
    
    -- 2. INSERT into APPOINTMENTS
    -- 3. GENERATE CONF_CODE and email customer
    
    INSERT INTO appointments (
        APPOINTMENT_ID, CUSTOMER_ID, 
        REF_CRITERIA, REF_VALUE, 
        QUEUE_ID,  SERVICE_ID, 
        CONTACTTYPE,  MOREINFO, 
        APPT_DATE, START_TIME, END_TIME, 
        STATUS, CONFCODE, MEETINGURL, LANGUAGE_PREF, 
        CREATEDBY, CREATEDON, STAMPUSER, STAMPDATE
    )
    SELECT APPTSEQ.NEXTVAL, FQ_CRYPTO_PKG.DECRYPT(p_customerid), J.*,
          'WEBAPP', sysdate, 'WEBAPP', sysdate
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
            CONFCODE PATH '$.CONFCODE',
            MEETINGURL PATH '$.MEETINGURL',
            LANGUAGE_PREF PATH '$.LANGUAGE_PREF'
        )
    ) J;

/*
    p_qid IN NUMBER, 
    p_sid IN NUMBER, 
    p_date IN DATE, 
    p_starttime IN INTERVAL,
    p_endtime IN INTERVAL,
    p_contactby IN VARCHAR2, 
    p_ref IN VARCHAR2, 
    p_ref_val IN VARCHAR2,
    p_notes IN VARCHAR2,
    p_fname IN VARCHAR2,
    p_lname IN VARCHAR2,
    p_email IN VARCHAR2,
    p_phone IN VARCHAR2,
    p_SMSoptin IN VARCHAR2,
    p_outid OUT INT, 
    p_confcode OUT VARCHAR2,
    p_apptid OUT VARCHAR2
    
    
    (APPTSEQ.NETVAL, 
     1,
     'G',
     'Questions', 
     10003, 
     10004, 
     'PC', 
     'none', 
     '19-JAN-26 12.00.00 AM',	
     '+00 14:00:00.000000', 
     '+00 15:00:00.000000', 
     'SCHEDULED', 
     'c3s7g9',
     null,
     null,
     'SQL',
     sysdate,
     'PREDDY01', 
     sysdate);							
*/
--TODO: Finalize proc
    COMMIT;
EXCEPTION
    WHEN OTHERS THEN
        p_out := 'Proc not finalized';
        ROLLBACK;
END;
    
END FQ_EXTERNAL;
--------------------------------------------------------
--  DDL for Package Body FQ_PROCS
--------------------------------------------------------

  CREATE OR REPLACE EDITIONABLE PACKAGE BODY "FQ_PROCS" as
--*******************************************************
-- 2025.12.31   PREDDY      Created package
-- 
--*******************************************************
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

PROCEDURE GET_QUEUE_DETAILS (
    p_queueid     IN INT, 
    p_json        OUT  CLOB
)
AS
BEGIN
    SELECT
        JSON_OBJECT(
        q.queue_id, q.name, q.name_cp, q.name_es,
        L.locname, L.address, L.phone,
        'services' VALUE (
                SELECT
                    JSON_ARRAYAGG(
                        JSON_OBJECT(service_id,service_name, service_name_es, service_name_cp)
                        )
                FROM
                     VALIDQUEUE_SERVICES s
                WHERE
                    s.queue_id = q.queue_id
            ),
        'schedules' VALUE (
                SELECT
                    JSON_ARRAYAGG(
                        JSON_OBJECT(schedule_id, date_begin, date_end,
                                    open_time, close_time,interval_time, 
                                    weekly_sch, available_resources)
                        )
                FROM
                    QUEUE_SCHEDULES qs
                WHERE SYSDATE BETWEEN DATE_BEGIN and DATE_END 
                    AND qs.queue_id = q.queue_id
            ),
        'configOptions' VALUE (                
                JSON_OBJECT(q.lead_time_max,q.lead_time_min, q.has_uploads, q.has_guidelines
                            ,q.emp_only, q.hide_in_monitor, q.hide_in_kiosk, q.activeflag)               
            ),
        'contactoptions' VALUE (
                SELECT
                    JSON_ARRAYAGG(
                        JSON_OBJECT(type_key,type_val,type_val_es,type_val_cp)
                        )
                FROM
                     VALIDCONTACTTYPES ct
                                inner join queue_contacttypes qct
                                 on ct.type_key = qct.contact_key                       
                WHERE
                    qct.queue_id = q.queue_id
            ),
        'refoptions' VALUE (
                SELECT
                    JSON_ARRAYAGG(
                        JSON_OBJECT(ref_key,ref_val,ref_val_es,ref_val_cp)
                        )
                FROM
                     VALIDREFERENCECRITERIAS rc
                                inner join QUEUE_REFCRITERIAS qrc
                                    on rc.ref_key = qrc.criteria_key                       
                WHERE
                    qrc.queue_id = q.queue_id
            )
        ) AS json_result INTO p_json
    FROM
        VALIDQUEUES q
            INNER JOIN VALIDLOCATIONS l ON q.location_id = l.location_id  
    WHERE  q.queue_id = p_queueid;
END;   

PROCEDURE GET_QUEUE_DETAILS (
    p_queueid     IN INT, 
    p_ref_cursor   OUT  Ref_Cursor_Types.ref_cursor
)
AS
BEGIN
 OPEN p_ref_cursor FOR    
    SELECT Q.*, 
        S.SERVICE_NAME, S.SERVICE_NAME_ES, S.SERVICE_NAME_CP,
        L.LOCNAME, L.ADDRESS, L.PHONE,
        contactmethod_list,criteria_list, Wkly_Sch
    FROM VALIDQUEUES Q
            INNER JOIN VALIDQUEUE_SERVICES S
                ON Q.QUEUE_ID = S.QUEUE_ID
            INNER JOIN VALIDLOCATIONS L
                ON Q.LOCATION_ID = L.LOCATION_ID
            INNER JOIN (SELECT queue_id,
                            LISTAGG(type_key, ',') WITHIN GROUP (ORDER BY type_key) AS contactmethod_list
                        from validcontacttypes ct
                            inner join queue_contacttypes qct
                             on ct.type_key = qct.contact_key
                        group by queue_id) C
                ON  Q.QUEUE_ID = C.QUEUE_ID
            INNER JOIN (SELECT queue_id,
                            LISTAGG(ref_key, ',') WITHIN GROUP (ORDER BY ref_key) AS criteria_list
                        from VALIDREFERENCECRITERIAS rc
                            inner join QUEUE_REFCRITERIAS qrc
                                on rc.ref_key = qrc.criteria_key
                        group by queue_id) R
                ON  Q.QUEUE_ID = R.QUEUE_ID
            LEFT OUTER JOIN  (SELECT queue_id, 
                                LISTAGG(Weekly_sch) WITHIN GROUP (ORDER BY schedule_id) AS Wkly_Sch 
                              FROM QUEUE_SCHEDULES 
                              Where SYSDATE BETWEEN DATE_BEGIN and DATE_END 
                              group by queue_id) QS
                ON Q.QUEUE_ID = qs.QUEUE_ID          
    WHERE NVL(Q.ACTIVEFLAG, 'N') = 'Y'
            and q.queue_id = p_queueid;
            
END;    

PROCEDURE GET_QUEUE_OPENSLOTS (
    --TODO: Exclude Holidays from resultset
    p_queueid       IN INT,
    p_date          IN DATE,
    p_ref_cursor    OUT  Ref_Cursor_Types.ref_cursor
)
AS
BEGIN
 OPEN p_ref_cursor FOR    
   SELECT * FROM ( 
    --- Recursive CTE to get avaialable-open-slots based on 
    --  SelectedDate, ServiceID (IN params)
    ---   AND Available_Resources setting
    ---   AND WeeklySch - dayOfWeek
    ---   AND existing ScheduledAppointments
    --
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
             and queue_id = NVL(p_queueid,queue_id)
             and to_char(to_date(p_date,'YYYY-MM-DD'), 'd') IN (select to_char(SUBSTR(Weekly_Sch, LEVEL, 1)) as weekday from dual connect by level <= length(Weekly_Sch))
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
            slot_begin, slot_end, Weekly_Sch
            ,interval_time ,Available_Resources, DuplicatesLevel, a.appointment_id
            --,count(*) avail_spots
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
     -- Use GROUP-BY to get totals for each begin-end slots (toggle lines 46,47 ogf this query)
     -- group by theDate, d.queue_id, slot_begin, slot_end, Weekly_Sch
    );            
END; 

PROCEDURE GET_APPTHIST4CUSTOMER (
    p_customerid        IN  INT,   
    p_ref_cursor        OUT Ref_Cursor_Types.ref_cursor
)
AS
BEGIN
 OPEN p_ref_cursor FOR
    SELECT FQ_CRYPTO_PKG.ENCRYPT(A.Appointment_Id), A.* 
    FROM APPOINTMENTS  A
    WHERE CUSTOMER_ID = p_customerid        
        AND TO_DATE(TO_CHAR(TRUNC(appt_date) + start_time, 'HH:MI AM'),'MM-DD-YYYY HH:MI AM') < SYSDATE;
    --TODO: Add additional conditions and Join-Tables
END;

PROCEDURE GET_APPTS4CUSTOMER (
    p_customerid        IN  INT,
    p_range_startdate   IN  DATE,
    p_range_enddate     IN  DATE,
    p_ref_cursor        OUT Ref_Cursor_Types.ref_cursor
)
AS
BEGIN
 OPEN p_ref_cursor FOR
    SELECT FQ_CRYPTO_PKG.ENCRYPT(A.Appointment_Id),A.* 
    FROM APPOINTMENTS A
    WHERE CUSTOMER_ID = p_customerid
        AND trunc(appt_date) BETWEEN trunc(p_range_startdate) AND trunc(p_range_enddate);
    --TODO: Add additional conditions and Join-Tables
END;

PROCEDURE GET_APPTS4STAFF ( 
-- For Internal Use ONLY
    p_userid IN  VARCHAR2,
    p_range_startdate   IN  DATE,
    p_range_enddate     IN  DATE,   
    p_ref_cursor        OUT Ref_Cursor_Types.ref_cursor
)
AS
BEGIN
 OPEN p_ref_cursor FOR
    SELECT * FROM APPOINTMENTS
    WHERE trunc(appt_date) BETWEEN trunc(p_range_startdate) AND trunc(p_range_enddate);
    --TODO: Add additional conditions and User-Queue-Permissions Join-Tables
END; 

PROCEDURE GET_APPT_DETAILS (
    p_apptid IN Appointments.appointment_id%type, 
    --p_apptidenc IN Varchar2,
    p_ref_cursor OUT Ref_Cursor_Types.ref_cursor
)
AS
BEGIN
 OPEN p_ref_cursor FOR
    SELECT FQ_CRYPTO_PKG.ENCRYPT(A.Appointment_Id), A.* 
    FROM APPOINTMENTS A
    WHERE appointment_id = p_apptid;
    --TODO: Add additional conditions and Join-Tables
END; 


END FQ_PROCS;
--------------------------------------------------------
--  Constraints for Table VALIDREFERENCECRITERIAS
--------------------------------------------------------

  ALTER TABLE "VALIDREFERENCECRITERIAS" ADD PRIMARY KEY ("REF_KEY") USING INDEX  ENABLE
--------------------------------------------------------
--  Constraints for Table SIGNINS
--------------------------------------------------------

  ALTER TABLE "SIGNINS" ADD PRIMARY KEY ("SIGNIN_ID") USING INDEX  ENABLE
--------------------------------------------------------
--  Constraints for Table VALIDSERVICESTATUS
--------------------------------------------------------

  ALTER TABLE "VALIDSERVICESTATUS" ADD PRIMARY KEY ("STATUS_KEY") USING INDEX  ENABLE
--------------------------------------------------------
--  Constraints for Table VALIDLANGUAGES
--------------------------------------------------------

  ALTER TABLE "VALIDLANGUAGES" ADD PRIMARY KEY ("LANG_KEY") USING INDEX  ENABLE
--------------------------------------------------------
--  Constraints for Table FQUSERS
--------------------------------------------------------

  ALTER TABLE "FQUSERS" MODIFY ("FNAME" NOT NULL ENABLE)
  ALTER TABLE "FQUSERS" MODIFY ("LNAME" NOT NULL ENABLE)
  ALTER TABLE "FQUSERS" MODIFY ("EMAIL" NOT NULL ENABLE)
  ALTER TABLE "FQUSERS" ADD PRIMARY KEY ("USER_ID") USING INDEX  ENABLE
  ALTER TABLE "FQUSERS" ADD UNIQUE ("EMAIL") USING INDEX  ENABLE
--------------------------------------------------------
--  Constraints for Table APPOINTMENTS
--------------------------------------------------------

  ALTER TABLE "APPOINTMENTS" ADD PRIMARY KEY ("APPOINTMENT_ID") USING INDEX  ENABLE
--------------------------------------------------------
--  Constraints for Table VALIDQUEUE_SERVICES
--------------------------------------------------------

  ALTER TABLE "VALIDQUEUE_SERVICES" MODIFY ("SERVICE_NAME" NOT NULL ENABLE)
  ALTER TABLE "VALIDQUEUE_SERVICES" ADD PRIMARY KEY ("SERVICE_ID") USING INDEX  ENABLE
  ALTER TABLE "VALIDQUEUE_SERVICES" ADD CONSTRAINT "VALIDQUEUE_SERVICES_UQ" UNIQUE ("QUEUE_ID", "SERVICE_NAME") USING INDEX  ENABLE
--------------------------------------------------------
--  Constraints for Table SERVICETRANSACTIONS
--------------------------------------------------------

  ALTER TABLE "SERVICETRANSACTIONS" ADD CHECK (SRC_TYPE in ('A', 'S')) ENABLE
  ALTER TABLE "SERVICETRANSACTIONS" ADD PRIMARY KEY ("TRANSACTION_ID") USING INDEX  ENABLE
--------------------------------------------------------
--  Constraints for Table VALIDHOLIDAYS
--------------------------------------------------------

  ALTER TABLE "VALIDHOLIDAYS" ADD PRIMARY KEY ("HOLIDAYDATE") USING INDEX  ENABLE
--------------------------------------------------------
--  Constraints for Table VALIDLOCATIONS
--------------------------------------------------------

  ALTER TABLE "VALIDLOCATIONS" ADD PRIMARY KEY ("LOCATION_ID") USING INDEX  ENABLE
  ALTER TABLE "VALIDLOCATIONS" ADD CONSTRAINT "VALIDLOCATIONS_NAME_UK" UNIQUE ("LOCNAME") USING INDEX  ENABLE
--------------------------------------------------------
--  Constraints for Table FQ_SESSIONS
--------------------------------------------------------

  ALTER TABLE "FQ_SESSIONS" MODIFY ("STARTEDAT" NOT NULL ENABLE)
  ALTER TABLE "FQ_SESSIONS" MODIFY ("VERIFIEDAT" NOT NULL ENABLE)
  ALTER TABLE "FQ_SESSIONS" MODIFY ("EXPIRESAT" NOT NULL ENABLE)
  ALTER TABLE "FQ_SESSIONS" ADD CONSTRAINT "USRSESSIONS_UQ" UNIQUE ("EMAIL", "SESSIONID", "AUTHCODE") USING INDEX  ENABLE
--------------------------------------------------------
--  Constraints for Table QUEUE_SCHEDULES
--------------------------------------------------------

  ALTER TABLE "QUEUE_SCHEDULES" MODIFY ("QUEUE_ID" NOT NULL ENABLE)
  ALTER TABLE "QUEUE_SCHEDULES" ADD PRIMARY KEY ("SCHEDULE_ID") USING INDEX  ENABLE
--------------------------------------------------------
--  Constraints for Table CUSTOMERS
--------------------------------------------------------

  ALTER TABLE "CUSTOMERS" MODIFY ("FNAME" NOT NULL ENABLE)
  ALTER TABLE "CUSTOMERS" MODIFY ("LNAME" NOT NULL ENABLE)
  ALTER TABLE "CUSTOMERS" MODIFY ("EMAIL" NOT NULL ENABLE)
  ALTER TABLE "CUSTOMERS" MODIFY ("PHONE" NOT NULL ENABLE)
  ALTER TABLE "CUSTOMERS" ADD PRIMARY KEY ("CUSTOMER_ID") USING INDEX  ENABLE
  ALTER TABLE "CUSTOMERS" ADD UNIQUE ("EMAIL") USING INDEX  ENABLE
--------------------------------------------------------
--  Constraints for Table QUEUE_REFCRITERIAS
--------------------------------------------------------

  ALTER TABLE "QUEUE_REFCRITERIAS" MODIFY ("QUEUE_ID" NOT NULL ENABLE)
  ALTER TABLE "QUEUE_REFCRITERIAS" MODIFY ("CRITERIA_KEY" NOT NULL ENABLE)
  ALTER TABLE "QUEUE_REFCRITERIAS" ADD CONSTRAINT "QUEUE_REFCRI_UQ" UNIQUE ("QUEUE_ID", "CRITERIA_KEY") USING INDEX  ENABLE
--------------------------------------------------------
--  Constraints for Table VALIDCONTACTTYPES
--------------------------------------------------------

  ALTER TABLE "VALIDCONTACTTYPES" ADD PRIMARY KEY ("TYPE_KEY") USING INDEX  ENABLE
--------------------------------------------------------
--  Constraints for Table VALIDROLES
--------------------------------------------------------

  ALTER TABLE "VALIDROLES" ADD PRIMARY KEY ("ROLE_ID") USING INDEX  ENABLE
--------------------------------------------------------
--  Constraints for Table VALIDQUEUES
--------------------------------------------------------

  ALTER TABLE "VALIDQUEUES" MODIFY ("NAME" NOT NULL ENABLE)
  ALTER TABLE "VALIDQUEUES" MODIFY ("NAME_ES" NOT NULL ENABLE)
  ALTER TABLE "VALIDQUEUES" MODIFY ("NAME_CP" NOT NULL ENABLE)
  ALTER TABLE "VALIDQUEUES" ADD PRIMARY KEY ("QUEUE_ID") USING INDEX  ENABLE
--------------------------------------------------------
--  Constraints for Table USER_PERMISSIONS
--------------------------------------------------------

  ALTER TABLE "USER_PERMISSIONS" MODIFY ("USER_ID" NOT NULL ENABLE)
  ALTER TABLE "USER_PERMISSIONS" MODIFY ("QUEUE_ID" NOT NULL ENABLE)
  ALTER TABLE "USER_PERMISSIONS" MODIFY ("ROLE_ID" NOT NULL ENABLE)
  ALTER TABLE "USER_PERMISSIONS" ADD CONSTRAINT "USRPERM_UQ" UNIQUE ("USER_ID", "QUEUE_ID", "ROLE_ID") USING INDEX  ENABLE
--------------------------------------------------------
--  Constraints for Table QUEUE_CONTACTTYPES
--------------------------------------------------------

  ALTER TABLE "QUEUE_CONTACTTYPES" MODIFY ("QUEUE_ID" NOT NULL ENABLE)
  ALTER TABLE "QUEUE_CONTACTTYPES" MODIFY ("CONTACT_KEY" NOT NULL ENABLE)
  ALTER TABLE "QUEUE_CONTACTTYPES" ADD CONSTRAINT "QUEUE_CONTACT_UQ" UNIQUE ("QUEUE_ID", "CONTACT_KEY") USING INDEX  ENABLE
--------------------------------------------------------
--  Ref Constraints for Table APPOINTMENTS
--------------------------------------------------------

  ALTER TABLE "APPOINTMENTS" ADD CONSTRAINT "APPT_CONTTYPE_FK" FOREIGN KEY ("CONTACTTYPE") REFERENCES "VALIDCONTACTTYPES" ("TYPE_KEY") ENABLE
  ALTER TABLE "APPOINTMENTS" ADD CONSTRAINT "APPT_REF_FK" FOREIGN KEY ("REF_CRITERIA") REFERENCES "VALIDREFERENCECRITERIAS" ("REF_KEY") ENABLE
  ALTER TABLE "APPOINTMENTS" ADD CONSTRAINT "APPT_CUSTID_FK" FOREIGN KEY ("CUSTOMER_ID") REFERENCES "CUSTOMERS" ("CUSTOMER_ID") ENABLE
  ALTER TABLE "APPOINTMENTS" ADD CONSTRAINT "APPT_QID_FK" FOREIGN KEY ("QUEUE_ID") REFERENCES "VALIDQUEUES" ("QUEUE_ID") ENABLE
  ALTER TABLE "APPOINTMENTS" ADD CONSTRAINT "APPT_SID_FK" FOREIGN KEY ("SERVICE_ID") REFERENCES "VALIDQUEUE_SERVICES" ("SERVICE_ID") ENABLE
--------------------------------------------------------
--  Ref Constraints for Table QUEUE_CONTACTTYPES
--------------------------------------------------------

  ALTER TABLE "QUEUE_CONTACTTYPES" ADD CONSTRAINT "QUEUE_CONTACT_CKEY_FK" FOREIGN KEY ("CONTACT_KEY") REFERENCES "VALIDCONTACTTYPES" ("TYPE_KEY") ENABLE
--------------------------------------------------------
--  Ref Constraints for Table QUEUE_REFCRITERIAS
--------------------------------------------------------

  ALTER TABLE "QUEUE_REFCRITERIAS" ADD CONSTRAINT "QUEUE_REFCRI_CKEY_FK" FOREIGN KEY ("CRITERIA_KEY") REFERENCES "VALIDREFERENCECRITERIAS" ("REF_KEY") ENABLE
--------------------------------------------------------
--  Ref Constraints for Table SERVICETRANSACTIONS
--------------------------------------------------------

  ALTER TABLE "SERVICETRANSACTIONS" ADD CONSTRAINT "SVCTRAN_QID_FK" FOREIGN KEY ("QUEUE_ID") REFERENCES "VALIDQUEUES" ("QUEUE_ID") ENABLE
  ALTER TABLE "SERVICETRANSACTIONS" ADD CONSTRAINT "SVCTRAN_SID_FK" FOREIGN KEY ("SERVICE_ID") REFERENCES "VALIDQUEUE_SERVICES" ("SERVICE_ID") ENABLE
--------------------------------------------------------
--  Ref Constraints for Table SIGNINS
--------------------------------------------------------

  ALTER TABLE "SIGNINS" ADD CONSTRAINT "SIGNIN_CUSTID_FK" FOREIGN KEY ("CUSTOMER_ID") REFERENCES "CUSTOMERS" ("CUSTOMER_ID") ENABLE
  ALTER TABLE "SIGNINS" ADD CONSTRAINT "SIGNIN_QID_FK" FOREIGN KEY ("QUEUE_ID") REFERENCES "VALIDQUEUES" ("QUEUE_ID") ENABLE
  ALTER TABLE "SIGNINS" ADD CONSTRAINT "SIGNIN_SID_FK" FOREIGN KEY ("SERVICE_ID") REFERENCES "VALIDQUEUE_SERVICES" ("SERVICE_ID") ENABLE
  ALTER TABLE "SIGNINS" ADD CONSTRAINT "SIGNIN_CONTTYPE_FK" FOREIGN KEY ("CONTACTTYPE") REFERENCES "VALIDCONTACTTYPES" ("TYPE_KEY") ENABLE
  ALTER TABLE "SIGNINS" ADD CONSTRAINT "SIGNIN_REF_FK" FOREIGN KEY ("REF_CRITERIA") REFERENCES "VALIDREFERENCECRITERIAS" ("REF_KEY") ENABLE
--------------------------------------------------------
--  Ref Constraints for Table USER_PERMISSIONS
--------------------------------------------------------

  ALTER TABLE "USER_PERMISSIONS" ADD CONSTRAINT "USRPERM_USRID_FK" FOREIGN KEY ("USER_ID") REFERENCES "FQUSERS" ("USER_ID") ENABLE
  ALTER TABLE "USER_PERMISSIONS" ADD CONSTRAINT "USRPERM_QID_FK" FOREIGN KEY ("QUEUE_ID") REFERENCES "VALIDQUEUES" ("QUEUE_ID") ENABLE
  ALTER TABLE "USER_PERMISSIONS" ADD CONSTRAINT "USRPERM_RID_FK" FOREIGN KEY ("ROLE_ID") REFERENCES "VALIDROLES" ("ROLE_ID") ENABLE
--------------------------------------------------------
--  Ref Constraints for Table VALIDQUEUES
--------------------------------------------------------

  ALTER TABLE "VALIDQUEUES" ADD CONSTRAINT "VALIDQUEUE_LOCID_FK" FOREIGN KEY ("LOCATION_ID") REFERENCES "VALIDLOCATIONS" ("LOCATION_ID") ENABLE
