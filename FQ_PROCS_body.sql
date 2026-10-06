create or replace PACKAGE BODY         FQ_PROCS AS
--********************************************************************
-- 2025.12.31 PREDDY Created
-- 2026.mm.yy   ASARDAR     Modified
-- 2026.08.10 PREDDY Updated INSERT_CUSTOMER to handle undisclosed
-- customer-email in staff-scheduled appts
-- 2026.08.27 PREDDY Updated TRANSFER_SOURCE removed appending
--                          Customer-Notes on TRANSFERS as per Brooke.Perry
--********************************************************************
  PROCEDURE UPDATE_APPT_STATUS (
    p_apptid    IN APPOINTMENTS.APPOINTMENT_ID%TYPE,
    p_action    IN VARCHAR2,
    p_stampuser IN VARCHAR2,
    p_notes     IN VARCHAR2,
    p_outmsg    OUT VARCHAR2
  )
  AS
  BEGIN
    p_outmsg := NULL;
    UPDATE APPOINTMENTS
       SET STATUS    = 'ARRIVED',
           STAMPUSER = NVL(p_stampuser, STAMPUSER),
           STAMPDATE = SYSDATE
     WHERE APPOINTMENT_ID = p_apptid;

    IF SQL%ROWCOUNT = 0 THEN
      p_outmsg := 'Appointment not found.';
    END IF;
  EXCEPTION
    WHEN OTHERS THEN
      p_outmsg := SQLERRM;
  END UPDATE_APPT_STATUS;

  PROCEDURE SET_SERVICE_TRANSACTION (
    p_src_type   IN  VARCHAR2,
    p_src_id     IN  NUMBER,
    p_action     IN  VARCHAR2,
    p_stampuser  IN  VARCHAR2,
    p_notes      IN  VARCHAR2,
    p_outmsg     OUT VARCHAR2
  )
  AS
    v_status_new     VARCHAR2(20);
    v_status_curr    VARCHAR2(20);
    v_queueid        NUMBER(9);
    v_serviceid      NUMBER(9);
    v_transactionid  NUMBER(9);
  BEGIN
    p_outmsg := NULL;

    BEGIN
      CASE p_src_type
        WHEN 'A' THEN
          SELECT QUEUE_ID, SERVICE_ID, STATUS
            INTO v_queueid, v_serviceid, v_status_curr
            FROM APPOINTMENTS
           WHERE APPOINTMENT_ID = p_src_id;
        WHEN 'W' THEN
          SELECT QUEUE_ID, SERVICE_ID, STATUS
            INTO v_queueid, v_serviceid, v_status_curr
            FROM WALKINS
           WHERE WALKIN_ID = p_src_id;
        ELSE
          p_outmsg := 'Invalid Source Type (' || p_src_type || ')';
          RETURN;
      END CASE;
    EXCEPTION
      WHEN NO_DATA_FOUND THEN
        p_outmsg := 'No data found';
        RETURN;
      WHEN TOO_MANY_ROWS THEN
        p_outmsg := 'Too many rows retrieved';
        RETURN;
    END;

    IF v_queueid IS NULL OR v_serviceid IS NULL THEN
      p_outmsg := 'Queue/Service ID missing';
      RETURN;
    END IF;

    IF UPPER(NVL(v_status_curr, '')) = 'DONE' THEN
      p_outmsg := 'No update performed: status is already DONE.';
      RETURN;
    END IF;

    BEGIN
      SELECT TRANSACTION_ID
        INTO v_transactionid
        FROM SERVICETRANSACTIONS
       WHERE SRC_TYPE  = UPPER(p_src_type)
         AND SRC_ID    = p_src_id
         AND QUEUE_ID  = v_queueid
         AND SERVICE_ID = v_serviceid;
    EXCEPTION
      WHEN NO_DATA_FOUND THEN
        v_transactionid := NULL;
      WHEN TOO_MANY_ROWS THEN
        SELECT MAX(TRANSACTION_ID)
          INTO v_transactionid
          FROM SERVICETRANSACTIONS
         WHERE SRC_TYPE  = UPPER(p_src_type)
           AND SRC_ID    = p_src_id
           AND QUEUE_ID  = v_queueid
           AND SERVICE_ID = v_serviceid;
    END;

    CASE
      WHEN UPPER(p_action) = 'CHECKIN' THEN
        v_status_new := 'ARRIVED';
        v_transactionid := SVCTRANSSEQ.NEXTVAL;

        INSERT INTO SERVICETRANSACTIONS
          (TRANSACTION_ID, SRC_TYPE, SRC_ID, QUEUE_ID, SERVICE_ID,
           CHECKIN_TIME, STATUS, SERVICE_NOTES, STAMPUSER, STAMPDATE)
        VALUES
          (v_transactionid, UPPER(p_src_type), p_src_id, v_queueid, v_serviceid,
           SYSDATE, v_status_new, NULL, p_stampuser, SYSDATE);

      WHEN UPPER(p_action) IN ('START','BEGIN') THEN
        v_status_new := 'IN PROGRESS';

        IF v_transactionid IS NULL THEN
          v_transactionid := SVCTRANSSEQ.NEXTVAL;

          INSERT INTO SERVICETRANSACTIONS
            (TRANSACTION_ID, SRC_TYPE, SRC_ID, QUEUE_ID, SERVICE_ID,
             CHECKIN_TIME, SERVICE_START_TIME, STATUS, SERVICE_NOTES,
             STAMPUSER, STAMPDATE)
          VALUES
            (v_transactionid, UPPER(p_src_type), p_src_id, v_queueid, v_serviceid,
             SYSDATE, SYSDATE, v_status_new, NULL,
             p_stampuser, SYSDATE);
        ELSE
          UPDATE SERVICETRANSACTIONS
             SET CHECKIN_TIME        = NVL(CHECKIN_TIME, SYSDATE),
                 SERVICE_START_TIME  = SYSDATE,
                 STATUS              = v_status_new,
                 --SERVICE_NOTES       = SERVICE_NOTES || 'Started:',
                 STAMPUSER           = p_stampuser,
                 STAMPDATE           = SYSDATE
           WHERE TRANSACTION_ID = v_transactionid;
        END IF;

      WHEN UPPER(p_action) = 'END' THEN
        v_status_new := 'DONE';

        UPDATE SERVICETRANSACTIONS
           SET SERVICE_END_TIME = SYSDATE,
               STATUS           = v_status_new,
               SERVICE_NOTES    = TRIM(NVL(SERVICE_NOTES,'') || ' ' || p_notes),
               STAMPUSER        = p_stampuser,
               STAMPDATE        = SYSDATE
         WHERE TRANSACTION_ID = v_transactionid;

      WHEN UPPER(p_action) IN ( 'REMOVE', 'CANCEL') THEN
        IF p_action = 'REMOVE' THEN
            v_status_new :=  'REMOVED';
        ELSE
            v_status_new :=  'CANCELED';
        END IF;

         IF v_transactionid IS NULL THEN
          v_transactionid := SVCTRANSSEQ.NEXTVAL;

          INSERT INTO SERVICETRANSACTIONS
            (TRANSACTION_ID, SRC_TYPE, SRC_ID, QUEUE_ID, SERVICE_ID,
             CHECKIN_TIME, SERVICE_START_TIME, STATUS, SERVICE_NOTES,
             STAMPUSER, STAMPDATE)
          VALUES
            (v_transactionid, UPPER(p_src_type), p_src_id, v_queueid, v_serviceid,
             SYSDATE, SYSDATE, v_status_new, p_notes,
             p_stampuser, SYSDATE);
        ELSE
            UPDATE SERVICETRANSACTIONS
               SET STATUS        = v_status_new,
                   SERVICE_NOTES = TRIM(SERVICE_NOTES || ' ' || p_notes),
                   STAMPUSER     = p_stampuser,
                   STAMPDATE     = SYSDATE
             WHERE TRANSACTION_ID = v_transactionid;
        END IF;

      WHEN UPPER(p_action) = 'REJOIN' THEN
        v_status_new := 'REJOINED';

        UPDATE SERVICETRANSACTIONS
           SET STATUS        = v_status_new,
               SERVICE_NOTES = NVL(SERVICE_NOTES,'') || 'Rejoined:',
               STAMPUSER     = p_stampuser,
               STAMPDATE     = SYSDATE
         WHERE TRANSACTION_ID = v_transactionid;

--      WHEN UPPER(p_action) = 'CANCEL' THEN
--        v_status_new := 'CANCELED';
--
--        UPDATE SERVICETRANSACTIONS
--           SET STATUS        = v_status_new,
--               SERVICE_NOTES = NVL(SERVICE_NOTES,'') || 'Canceled:',
--               STAMPUSER     = p_stampuser,
--               STAMPDATE     = SYSDATE
--         WHERE TRANSACTION_ID = v_transactionid;

      WHEN UPPER(p_action) = 'TRANSFER' THEN
        v_status_new := 'TRANSFERRED';

        IF v_transactionid IS NULL THEN
          v_transactionid := SVCTRANSSEQ.NEXTVAL;

          INSERT INTO SERVICETRANSACTIONS
            (TRANSACTION_ID, SRC_TYPE, SRC_ID, QUEUE_ID, SERVICE_ID,
             CHECKIN_TIME, SERVICE_START_TIME, STATUS, SERVICE_NOTES,
             STAMPUSER, STAMPDATE)
          VALUES
            (v_transactionid, UPPER(p_src_type), p_src_id, v_queueid, v_serviceid,
             SYSDATE, SYSDATE, v_status_new, p_notes,
             p_stampuser, SYSDATE);
        ELSE
            UPDATE SERVICETRANSACTIONS
               SET STATUS        = v_status_new,
                   SERVICE_NOTES = TRIM(NVL(SERVICE_NOTES,'') || ' ' || p_notes),
                   STAMPUSER     = p_stampuser,
                   STAMPDATE     = SYSDATE
             WHERE TRANSACTION_ID = v_transactionid;
        END IF;
      ELSE
        p_outmsg := 'Invalid action (' || p_action || ')';
        RETURN;
    END CASE;

    BEGIN
      CASE p_src_type
        WHEN 'A' THEN
          UPDATE APPOINTMENTS
             SET STATUS    = v_status_new,
                 STAMPUSER = NVL(p_stampuser, STAMPUSER),
                 STAMPDATE = SYSDATE
           WHERE APPOINTMENT_ID = p_src_id;
        WHEN 'W' THEN
          UPDATE WALKINS
             SET STATUS    = v_status_new,
                 END_TIME  =
                    DECODE(UPPER(p_action),'END',
                        TO_DSINTERVAL('00 ' || TO_CHAR(SYSDATE, 'HH24:MI:SS')),
                        END_TIME),
                 STAMPUSER = NVL(p_stampuser, STAMPUSER),
                 STAMPDATE = SYSDATE
           WHERE WALKIN_ID = p_src_id;
        ELSE
          p_outmsg := 'Invalid Source Type (' || p_src_type || ')';
          ROLLBACK;
          RETURN;
      END CASE;

      IF SQL%ROWCOUNT = 0 THEN
        p_outmsg := 'Appointment/Walkin not found.';
        RETURN;
      END IF;
    END;
  EXCEPTION
    WHEN OTHERS THEN
      p_outmsg := SQLERRM;
  END;

PROCEDURE TRANSFER_SOURCE (
    p_src_type           IN VARCHAR2,
    p_src_id             IN NUMBER,
    p_target_queue_id    IN NUMBER,
    p_target_service_id  IN NUMBER,
    p_target_kind        IN VARCHAR2,
    p_target_date        IN DATE,
    p_target_enddate     IN DATE,
    p_target_notes       IN VARCHAR2,
    p_servicenotes       IN VARCHAR2,
    p_stampuser          IN VARCHAR2,
    p_new_src_id         OUT NUMBER,
    p_outmsg             OUT VARCHAR2,
    p_source_action      IN VARCHAR2 DEFAULT 'TRANSFER'
  )
  AS
    v_src_type     VARCHAR2(1) := UPPER(TRIM(p_src_type));
    v_target_kind  VARCHAR2(1) := UPPER(TRIM(p_target_kind));
    v_customer_id  NUMBER;
    v_queue_id     NUMBER;
    v_service_id   NUMBER;
    v_contacttype  VARCHAR2(50);
    v_moreinfo     VARCHAR2(4000);
    v_ref_criteria VARCHAR2(100);
    v_ref_value    VARCHAR2(100);
    v_conf_code    VARCHAR2(10);
    v_meetingurl   VARCHAR2(1000);
    v_meetingurl_host VARCHAR2(1000);
  BEGIN
    p_outmsg := NULL;
    p_new_src_id := NULL;

    IF v_src_type NOT IN ('A','W') THEN
      p_outmsg := 'Invalid source type';
      RETURN;
    END IF;

    IF v_target_kind NOT IN ('A','W') THEN
      p_outmsg := 'Invalid target kind';
      RETURN;
    END IF;

    IF p_target_queue_id IS NULL THEN
      p_outmsg := 'Target queue required';
      RETURN;
    END IF;

    IF v_target_kind = 'A' AND p_target_date IS NULL THEN
      p_outmsg := 'Target date required for appointment target';
      RETURN;
    END IF;

-- Get source-record's data to copy over to new record; LOCK table-row for UPDATE
    IF v_src_type = 'A' THEN
      SELECT customer_id, queue_id, service_id, contacttype,
             moreinfo, ref_criteria, ref_value,
             meetingurl, meetingurl_host
        INTO v_customer_id, v_queue_id, v_service_id, v_contacttype,
             v_moreinfo, v_ref_criteria, v_ref_value,
             v_meetingurl, v_meetingurl_host
        FROM appointments
       WHERE appointment_id = p_src_id
       FOR UPDATE;
    ELSE
      SELECT customer_id, queue_id, service_id, contacttype,
             moreinfo, ref_criteria, ref_value,
             meetingurl, meetingurl_host
        INTO v_customer_id, v_queue_id, v_service_id, v_contacttype,
             v_moreinfo, v_ref_criteria, v_ref_value,
             v_meetingurl, v_meetingurl_host
        FROM walkins
       WHERE walkin_id = p_src_id
       FOR UPDATE;
    END IF;

    FQ_PROCS.SET_SERVICE_TRANSACTION(
      p_src_type  => v_src_type,
      p_src_id    => p_src_id,
      p_action    => p_source_action,
      p_stampuser => p_stampuser,
      p_notes     => p_servicenotes,
      p_outmsg    => p_outmsg
    );

    IF p_outmsg IS NOT NULL THEN
      ROLLBACK;
      RETURN;
    END IF;

    -- removed on 8.27.2026 as per Brooke.Perry[Tolbert].Zoning:
    -- Donot append original-rec notes. Just save the current-rec Notes
    --    IF LENGTH(NVL(v_moreinfo,'')) > 0
    --        AND LENGTH(NVL(p_target_notes,'')) > 0 THEN
    --        v_moreinfo := v_moreinfo || CHR(13) || p_target_notes;
    --    ELSE
    --        v_moreinfo := v_moreinfo || p_target_notes;
    --    END IF;

    v_moreinfo := p_target_notes;

    IF v_target_kind = 'W' THEN
      p_new_src_id := WALKINSEQ.NEXTVAL;

      INSERT INTO walkins (
        walkin_id, customer_id, queue_id, service_id,
        ref_criteria, ref_value, contacttype, moreinfo,
        join_time, status, createdby, createdon, stampuser, stampdate,
        source_type, source_id,
        meetingurl, meetingurl_host
      ) VALUES (
        p_new_src_id,
        v_customer_id,
        p_target_queue_id,
        NVL(p_target_service_id, v_service_id),
        v_ref_criteria,
        v_ref_value,
        v_contacttype,
        v_moreinfo,
        TO_DSINTERVAL('00 ' || TO_CHAR(SYSDATE, 'HH24:MI:SS')),
        'ARRIVED',
        p_stampuser,
        SYSDATE,
        p_stampuser,
        SYSDATE,
        v_src_type,
        p_src_id,
        v_meetingurl,
        v_meetingurl_host
      );

      FQ_PROCS.SET_SERVICE_TRANSACTION(
        p_src_type  => 'W',
        p_src_id    => p_new_src_id,
        p_action    => 'CHECKIN',
        p_stampuser => p_stampuser,
        p_notes     => 'Auto-checkin after transfer',
        p_outmsg    => p_outmsg
      );
    ELSE
      p_new_src_id := APPTSEQ.NEXTVAL;
      v_conf_code := FQ_EXTERNAL.GET_NEW_CONF_CODE;

      INSERT INTO appointments (
        APPOINTMENT_ID, CUSTOMER_ID,
        QUEUE_ID, SERVICE_ID,
        REF_CRITERIA, REF_VALUE, CONTACTTYPE, MOREINFO,
        APPT_DATE, START_TIME, END_TIME, CONFCODE, STATUS,
        CREATEDBY, CREATEDON, STAMPUSER, STAMPDATE,
        SOURCE_TYPE, SOURCE_ID
        )
      VALUES (
        p_new_src_id,
        v_customer_id,
        p_target_queue_id,
        NVL(p_target_service_id, v_service_id),
        v_ref_criteria,
         --PR: NVL(p_ref_value, v_ref_value), -- use Original/Src-Rec data
        v_ref_value,
        v_contacttype,
        v_moreinfo,
        TRUNC(p_target_date),
        TO_DSINTERVAL('00 ' || TO_CHAR(p_target_date, 'HH24:MI:SS')),
        TO_DSINTERVAL('00 ' || TO_CHAR(p_target_enddate, 'HH24:MI:SS')),
        v_conf_code,
        'SCHEDULED',
        p_stampuser,
        SYSDATE,
        p_stampuser,
        SYSDATE,
        v_src_type,
        p_src_id
      );
    END IF;

    IF p_outmsg IS NOT NULL THEN
      ROLLBACK;
      RETURN;
    END IF;

    -- Use the same reference values inserted into the new source above.
    IF UPPER(TRIM(v_ref_criteria)) IN ('P','PERMIT') THEN
      FQ_STORE_AMANDA_INFO(v_target_kind, p_new_src_id, p_target_queue_id,
                          v_ref_criteria, v_ref_value);
    END IF;

    COMMIT;
  EXCEPTION
    WHEN NO_DATA_FOUND THEN
      p_outmsg := 'Source record not found';
      ROLLBACK;
    WHEN OTHERS THEN
      p_outmsg := SQLERRM;
      ROLLBACK;
  END;

PROCEDURE CLOSE_AND_ADD_SOURCE (
    p_src_type          IN VARCHAR2,
    p_src_id            IN NUMBER,
    p_additional        IN VARCHAR2,
    p_target_queue_id   IN NUMBER,
    p_target_service_id IN NUMBER,
    p_target_kind       IN VARCHAR2,
    p_target_date       IN DATE,
    p_target_enddate    IN DATE,
    p_target_notes      IN VARCHAR2,
    p_servicenotes      IN VARCHAR2,
    p_stampuser         IN VARCHAR2,
    p_new_src_id        OUT NUMBER,
    p_outmsg            OUT VARCHAR2
  )
  AS
    v_additional VARCHAR2(1) := UPPER(TRIM(p_additional));
  BEGIN
    p_outmsg := NULL;
    p_new_src_id := NULL;

    -- If no additional services needed, then END this service and exit
    IF v_additional <> 'Y' THEN
      FQ_PROCS.SET_SERVICE_TRANSACTION(
        p_src_type  => UPPER(TRIM(p_src_type)),
        p_src_id    => p_src_id,
        p_action    => 'END',
        p_stampuser => p_stampuser,
        p_notes     => p_servicenotes,
        p_outmsg    => p_outmsg
      );

      IF p_outmsg IS NOT NULL THEN
        ROLLBACK;
        RETURN;
      END IF;

      COMMIT;
      RETURN;
    END IF;

    -- If additional service is needed then copy base-record's data to new record
    -- TRANSFER_SOURCE will also END source-service-record as END
    TRANSFER_SOURCE(
      p_src_type        => UPPER(TRIM(p_src_type)),
      p_src_id          => p_src_id,
      p_target_queue_id => p_target_queue_id,
      p_target_service_id => p_target_service_id,
      p_target_kind     => UPPER(TRIM(p_target_kind)),
      p_target_date     => p_target_date,
      p_target_enddate  => p_target_enddate,
      p_target_notes    => p_target_notes,
      p_servicenotes    => p_servicenotes,
      p_stampuser       => p_stampuser,
      p_new_src_id      => p_new_src_id,
      p_outmsg          => p_outmsg,
      p_source_action   => 'END'
    );

    IF p_outmsg IS NOT NULL THEN
      ROLLBACK;
      RETURN;
    END IF;

    COMMIT;
  EXCEPTION
    WHEN OTHERS THEN
      p_outmsg := SQLERRM;
      ROLLBACK;
  END;

  PROCEDURE SAVE_SERVICE_INFO(
    p_src_type   IN CHAR,
    p_src_id     IN NUMBER,
    p_guest_url  IN VARCHAR2,
    p_host_url   IN VARCHAR2,
    p_notes      IN VARCHAR2,
    p_stampuser  IN VARCHAR2
  )
  AS
    v_notes          VARCHAR2(1000);
  BEGIN

    v_notes := SUBSTR(
      CASE WHEN p_notes IS NOT NULL THEN
        CHR(13) || TO_CHAR(SYSDATE, 'YYYY.MM.DD HH:MI AM')
        || ' (' || p_stampuser || '): ' || p_notes ELSE '' END,
      1, 1000
    );

    IF p_src_type = 'A' THEN
      UPDATE APPOINTMENTS
         SET MEETINGURL_HOST = p_host_url,
             MEETINGURL = p_guest_url,
             MOREINFO = MOREINFO || v_notes,
             STAMPUSER = NVL(p_stampuser, 'fastq'),
             STAMPDATE = SYSDATE
       WHERE APPOINTMENT_ID = p_src_id;
    ELSIF p_src_type = 'W' THEN
      UPDATE WALKINS
         SET MEETINGURL = p_guest_url,
             MOREINFO = MOREINFO || v_notes,
             STAMPUSER = NVL(p_stampuser, 'fastq'),
             STAMPDATE = SYSDATE
       WHERE WALKIN_ID = p_src_id;
    END IF;
  END SAVE_SERVICE_INFO;

  PROCEDURE INSERT_CUSTOMER (
    p_fname       IN VARCHAR2,
    p_lname       IN VARCHAR2,
    p_email       IN VARCHAR2,
    p_phone       IN VARCHAR2,
    p_sms_optin   IN VARCHAR2,
    p_activeflag  IN VARCHAR2,
    p_stampuser   IN VARCHAR2,
    p_customer_id OUT NUMBER,
    p_outmsg      OUT VARCHAR2
  )
  AS
  v_customer_email VARCHAR2(100);
  BEGIN
    p_outmsg := NULL;
    p_customer_id := CUSTOMERSEQ.NEXTVAL;

    -- Build placeholder email for UNDISCLOSED customer-email
    IF UPPER(p_email) = 'NA' THEN
        v_customer_email := TO_CHAR(p_customer_id) || '@fastq.ocfl.net';
    ELSE
        v_customer_email := p_email;
    END IF;

    INSERT INTO CUSTOMERS
      (CUSTOMER_ID, FNAME, LNAME, EMAIL, PHONE, SMS_OPTIN, ACTIVEFLAG, STAMPUSER, STAMPDATE)
    VALUES
      (p_customer_id,
       FQ_CRYPTO_PKG.ENCRYPT(p_fname),
       FQ_CRYPTO_PKG.ENCRYPT(p_lname),
       FQ_CRYPTO_PKG.ENCRYPT(LOWER(v_customer_email)),
       FQ_CRYPTO_PKG.ENCRYPT(p_phone),
       p_sms_optin,
       p_activeflag,
       NVL(p_stampuser, 'fastq'),
       SYSDATE);
  EXCEPTION
    WHEN OTHERS THEN
      p_customer_id := NULL;
      p_outmsg := SQLERRM;
  END INSERT_CUSTOMER;

  PROCEDURE UPDATE_CUSTOMER (
    p_customer_id IN NUMBER,
    p_fname       IN VARCHAR2,
    p_lname       IN VARCHAR2,
    p_email       IN VARCHAR2,
    p_phone       IN VARCHAR2,
    p_sms_optin   IN VARCHAR2,
    p_activeflag  IN VARCHAR2,
    p_stampuser   IN VARCHAR2,
    p_outmsg      OUT VARCHAR2
  )
  AS
  BEGIN
    p_outmsg := NULL;

    UPDATE CUSTOMERS
       SET FNAME = FQ_CRYPTO_PKG.ENCRYPT(p_fname),
           LNAME = FQ_CRYPTO_PKG.ENCRYPT(p_lname),
           EMAIL = FQ_CRYPTO_PKG.ENCRYPT(LOWER(p_email)),
           PHONE = FQ_CRYPTO_PKG.ENCRYPT(p_phone),
           SMS_OPTIN = p_sms_optin,
           ACTIVEFLAG = p_activeflag,
           STAMPUSER = NVL(p_stampuser, 'fastq'),
           STAMPDATE = SYSDATE
     WHERE CUSTOMER_ID = p_customer_id;

    IF SQL%ROWCOUNT = 0 THEN
      p_outmsg := 'Customer not found.';
    END IF;
  EXCEPTION
    WHEN OTHERS THEN
      p_outmsg := SQLERRM;
  END UPDATE_CUSTOMER;

  PROCEDURE INSERT_WALKIN (
    p_customer_id    IN NUMBER,
    p_queue_id       IN NUMBER,
    p_service_id     IN NUMBER,
    p_ref_criteria   IN VARCHAR2,
    p_ref_value      IN VARCHAR2,
    p_contacttype    IN VARCHAR2,
    p_moreinfo       IN VARCHAR2,
    p_join_time      IN VARCHAR2,
    p_end_time       IN VARCHAR2,
    p_status         IN VARCHAR2,
    p_meetingurl     IN VARCHAR2,
    p_language_pref  IN VARCHAR2,
    p_createdby      IN VARCHAR2,
    p_stampuser      IN VARCHAR2,
    p_walkin_id      OUT NUMBER,
    p_outmsg         OUT VARCHAR2
  )
  AS
  BEGIN
    p_outmsg := NULL;
    p_walkin_id := NULL;

    IF p_customer_id IS NULL OR p_customer_id <= 0 THEN
      p_outmsg := 'CustomerId is required.';
      RETURN;
    END IF;

    IF p_queue_id IS NULL OR p_queue_id <= 0 THEN
      p_outmsg := 'QueueId is required.';
      RETURN;
    END IF;

    SAVEPOINT fq_create_walkin;
    p_walkin_id := WALKINSEQ.NEXTVAL;

    INSERT INTO WALKINS
      (WALKIN_ID, CUSTOMER_ID, QUEUE_ID, SERVICE_ID, REF_CRITERIA, REF_VALUE, CONTACTTYPE, MOREINFO,
       JOIN_TIME, END_TIME, STATUS, MEETINGURL, LANGUAGE_PREF, CREATEDBY, CREATEDON, STAMPUSER, STAMPDATE)
    VALUES
      (p_walkin_id,
       p_customer_id,
       p_queue_id,
       p_service_id,
       p_ref_criteria,
       p_ref_value,
       p_contacttype,
       p_moreinfo,
       CASE WHEN p_join_time IS NULL THEN NULL ELSE TO_DSINTERVAL(p_join_time) END,
       CASE WHEN p_end_time IS NULL THEN NULL ELSE TO_DSINTERVAL(p_end_time) END,
       UPPER(p_status),
       p_meetingurl,
       p_language_pref,
       NVL(p_createdby, 'fastq'),
       SYSDATE,
       NVL(p_stampuser, 'fastq'),
       SYSDATE);

    -- Capture the permit reviewer before check-in; keep the caller's transaction.
    IF UPPER(TRIM(p_ref_criteria)) IN ('P','PERMIT') THEN
      BEGIN
        FQ_STORE_AMANDA_INFO('W', p_walkin_id, p_queue_id, p_ref_criteria, p_ref_value);
      EXCEPTION WHEN OTHERS THEN
        p_outmsg := SQLERRM;
        ROLLBACK TO fq_create_walkin;
        p_walkin_id := NULL;
        RETURN;
      END;
    END IF;

    -- PREDDY 07.30.2026 added: INSERT TRANSACTION record
    SET_SERVICE_TRANSACTION (
        p_src_type   => 'W',
        p_src_id     => p_walkin_id,
        p_action     => 'CHECKIN',
        p_stampuser  => p_stampuser,
        p_notes      => NULL,
        p_outmsg     => p_outmsg);

  EXCEPTION
    WHEN OTHERS THEN
      p_walkin_id := NULL;
      p_outmsg := SQLERRM;
  END INSERT_WALKIN;

  PROCEDURE UPDATE_APPOINTMENT (
    p_apptid         IN NUMBER,
    p_customer_id    IN NUMBER,
    p_queue_id       IN NUMBER,
    p_service_id     IN NUMBER,
    p_ref_criteria   IN VARCHAR2,
    p_ref_value      IN VARCHAR2,
    p_contacttype    IN VARCHAR2,
    p_moreinfo_new   IN VARCHAR2,
    p_appt_date      IN DATE,
    p_start_time     IN VARCHAR2,
    p_end_time       IN VARCHAR2,
    p_status         IN VARCHAR2,
    p_confcode       IN VARCHAR2,
    p_meetingurl     IN VARCHAR2,
    p_language_pref  IN VARCHAR2,
    p_stampuser      IN VARCHAR2,
    p_outmsg         OUT VARCHAR2
  )
  AS
  BEGIN
    p_outmsg := NULL;

    UPDATE APPOINTMENTS
       SET CUSTOMER_ID = p_customer_id,
           QUEUE_ID = p_queue_id,
           SERVICE_ID = p_service_id,
           REF_CRITERIA = p_ref_criteria,
           REF_VALUE = p_ref_value,
           CONTACTTYPE = p_contacttype,
           MOREINFO = MOREINFO || p_moreinfo_new,
           APPT_DATE = p_appt_date,
           START_TIME = CASE WHEN p_start_time IS NULL THEN NULL ELSE TO_DSINTERVAL(p_start_time) END,
           END_TIME = CASE WHEN p_end_time IS NULL THEN NULL ELSE TO_DSINTERVAL(p_end_time) END,
           STATUS = p_status,
           CONFCODE = p_confcode,
           MEETINGURL = p_meetingurl,
           LANGUAGE_PREF = p_language_pref,
           STAMPUSER = NVL(p_stampuser, 'fastq'),
           STAMPDATE = SYSDATE
     WHERE APPOINTMENT_ID = p_apptid;

    IF SQL%ROWCOUNT = 0 THEN
      p_outmsg := 'Appointment not found.';
    END IF;
  EXCEPTION
    WHEN OTHERS THEN
      p_outmsg := SQLERRM;
  END UPDATE_APPOINTMENT;

END FQ_PROCS;
/
SHOW ERRORS PACKAGE BODY FQ_PROCS;
