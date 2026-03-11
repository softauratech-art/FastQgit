CREATE OR REPLACE PACKAGE BODY FQ_PROCS AS
  PROCEDURE VALIDATE_PERMIT (
    p_queueid        IN NUMBER,
    p_permit_number  IN VARCHAR2,
    p_is_valid       OUT NUMBER,
    p_outmsg         OUT VARCHAR2
  )
  AS
    v_permit VARCHAR2(100);
    v_count  NUMBER := 0;
  BEGIN
    p_is_valid := 0;
    p_outmsg := NULL;
    v_permit := TRIM(p_permit_number);

    IF p_queueid IS NULL OR p_queueid <= 0 THEN
      p_outmsg := 'Queue is required.';
      RETURN;
    END IF;

    IF v_permit IS NULL THEN
      p_outmsg := 'Permit number is required.';
      RETURN;
    END IF;

    SELECT COUNT(1)
      INTO v_count
      FROM Folder@LDMSDEV_LINK f
     WHERE TRIM(UPPER(f.ReferrenceFile)) = TRIM(UPPER(v_permit));

    IF v_count > 0 THEN
      p_is_valid := 1;
    ELSE
      p_outmsg := 'Permit number not found.';
    END IF;
  EXCEPTION
    WHEN OTHERS THEN
      p_is_valid := 0;
      p_outmsg := SQLERRM;
  END VALIDATE_PERMIT;

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
       p_status,
       p_meetingurl,
       p_language_pref,
       NVL(p_createdby, 'fastq'),
       SYSDATE,
       NVL(p_stampuser, 'fastq'),
       SYSDATE);
  EXCEPTION
    WHEN OTHERS THEN
      p_walkin_id := NULL;
      p_outmsg := SQLERRM;
  END INSERT_WALKIN;

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
           SYSDATE, v_status_new, 'CheckedIn:', p_stampuser, SYSDATE);

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
             SYSDATE, SYSDATE, v_status_new, 'Started(auto-checkin):',
             p_stampuser, SYSDATE);
        ELSE
          UPDATE SERVICETRANSACTIONS
             SET CHECKIN_TIME        = NVL(CHECKIN_TIME, SYSDATE),
                 SERVICE_START_TIME  = SYSDATE,
                 STATUS              = v_status_new,
                 SERVICE_NOTES       = 'Started:',
                 STAMPUSER           = p_stampuser,
                 STAMPDATE           = SYSDATE
           WHERE TRANSACTION_ID = v_transactionid;
        END IF;

      WHEN UPPER(p_action) = 'END' THEN
        v_status_new := 'DONE';

        UPDATE SERVICETRANSACTIONS
           SET SERVICE_END_TIME = SYSDATE,
               STATUS           = v_status_new,
               SERVICE_NOTES    = NVL(SERVICE_NOTES,'') || 'Ended:',
               STAMPUSER        = p_stampuser,
               STAMPDATE        = SYSDATE
         WHERE TRANSACTION_ID = v_transactionid;

      WHEN UPPER(p_action) = 'REMOVE' THEN
        v_status_new := 'REMOVED';

        UPDATE SERVICETRANSACTIONS
           SET STATUS        = v_status_new,
               SERVICE_NOTES = NVL(SERVICE_NOTES,'') || 'Removed:',
               STAMPUSER     = p_stampuser,
               STAMPDATE     = SYSDATE
         WHERE TRANSACTION_ID = v_transactionid;

      WHEN UPPER(p_action) = 'REJOIN' THEN
        v_status_new := 'REJOINED';

        UPDATE SERVICETRANSACTIONS
           SET STATUS        = v_status_new,
               SERVICE_NOTES = NVL(SERVICE_NOTES,'') || 'Rejoined:',
               STAMPUSER     = p_stampuser,
               STAMPDATE     = SYSDATE
         WHERE TRANSACTION_ID = v_transactionid;

      WHEN UPPER(p_action) = 'CANCEL' THEN
        v_status_new := 'CANCELED';

        UPDATE SERVICETRANSACTIONS
           SET STATUS        = v_status_new,
               SERVICE_NOTES = NVL(SERVICE_NOTES,'') || 'Canceled:',
               STAMPUSER     = p_stampuser,
               STAMPDATE     = SYSDATE
         WHERE TRANSACTION_ID = v_transactionid;

      WHEN UPPER(p_action) = 'TRANSFER' THEN
        v_status_new := 'TRANSFERRED';

        UPDATE SERVICETRANSACTIONS
           SET STATUS        = v_status_new,
               SERVICE_NOTES = NVL(SERVICE_NOTES,'') || 'Transferred:',
               STAMPUSER     = p_stampuser,
               STAMPDATE     = SYSDATE
         WHERE TRANSACTION_ID = v_transactionid;
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
    p_ref_value          IN VARCHAR2,
    p_notes              IN VARCHAR2,
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
    v_ref_value    VARCHAR2(4000);
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

    IF v_src_type = 'A' THEN
      SELECT customer_id, queue_id, service_id, contacttype, moreinfo, ref_criteria, ref_value
        INTO v_customer_id, v_queue_id, v_service_id, v_contacttype, v_moreinfo, v_ref_criteria, v_ref_value
        FROM appointments
       WHERE appointment_id = p_src_id
       FOR UPDATE;
    ELSE
      SELECT customer_id, queue_id, service_id, contacttype, moreinfo, ref_criteria, ref_value
        INTO v_customer_id, v_queue_id, v_service_id, v_contacttype, v_moreinfo, v_ref_criteria, v_ref_value
        FROM walkins
       WHERE walkin_id = p_src_id
       FOR UPDATE;
    END IF;

    SET_SERVICE_TRANSACTION(
      p_src_type  => v_src_type,
      p_src_id    => p_src_id,
      p_action    => p_source_action,
      p_stampuser => p_stampuser,
      p_notes     => p_notes,
      p_outmsg    => p_outmsg
    );

    IF p_outmsg IS NOT NULL THEN
      ROLLBACK;
      RETURN;
    END IF;

    IF v_target_kind = 'W' THEN
      p_new_src_id := WALKINSEQ.NEXTVAL;
      INSERT INTO walkins (
        walkin_id, customer_id, queue_id, service_id,
        ref_criteria, ref_value, contacttype, moreinfo,
        join_time, status, createdby, createdon, stampuser, stampdate,
        source_type, source_id
      ) VALUES (
        p_new_src_id,
        v_customer_id,
        p_target_queue_id,
        NVL(p_target_service_id, v_service_id),
        v_ref_criteria,
        NVL(p_ref_value, v_ref_value),
        v_contacttype,
        v_moreinfo,
        NUMTODSINTERVAL(0, 'SECOND'),
        'ARRIVED',
        p_stampuser,
        SYSDATE,
        p_stampuser,
        SYSDATE,
        v_src_type,
        p_src_id
      );

      SET_SERVICE_TRANSACTION(
        p_src_type  => 'W',
        p_src_id    => p_new_src_id,
        p_action    => 'CHECKIN',
        p_stampuser => p_stampuser,
        p_notes     => 'Auto-checkin after transfer',
        p_outmsg    => p_outmsg
      );
    ELSE
      p_new_src_id := APPTSEQ.NEXTVAL;
      INSERT INTO appointments (
        appointment_id, customer_id, queue_id, service_id,
        ref_criteria, ref_value, contacttype, moreinfo,
        appt_date, status, createdby, createdon, stampuser, stampdate,
        source_type, source_id
      ) VALUES (
        p_new_src_id,
        v_customer_id,
        p_target_queue_id,
        NVL(p_target_service_id, v_service_id),
        v_ref_criteria,
        NVL(p_ref_value, v_ref_value),
        v_contacttype,
        v_moreinfo,
        TRUNC(p_target_date),
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
    p_ref_value         IN VARCHAR2,
    p_notes             IN VARCHAR2,
    p_stampuser         IN VARCHAR2,
    p_new_src_id        OUT NUMBER,
    p_outmsg            OUT VARCHAR2
  )
  AS
    v_additional VARCHAR2(1) := UPPER(TRIM(p_additional));
  BEGIN
    p_outmsg := NULL;
    p_new_src_id := NULL;

    IF v_additional <> 'Y' THEN
      SET_SERVICE_TRANSACTION(
        p_src_type  => UPPER(TRIM(p_src_type)),
        p_src_id    => p_src_id,
        p_action    => 'END',
        p_stampuser => p_stampuser,
        p_notes     => p_notes,
        p_outmsg    => p_outmsg
      );

      IF p_outmsg IS NOT NULL THEN
        ROLLBACK;
        RETURN;
      END IF;

      COMMIT;
      RETURN;
    END IF;

    TRANSFER_SOURCE(
      p_src_type        => UPPER(TRIM(p_src_type)),
      p_src_id          => p_src_id,
      p_target_queue_id => p_target_queue_id,
      p_target_service_id => p_target_service_id,
      p_target_kind     => UPPER(TRIM(p_target_kind)),
      p_target_date     => p_target_date,
      p_ref_value       => p_ref_value,
      p_notes           => 'Additional service after close',
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
    p_webex_url  IN VARCHAR2,
    p_notes      IN VARCHAR2,
    p_stampuser  IN VARCHAR2
  )
  AS
    v_notes VARCHAR2(1000);
  BEGIN
    v_notes := SUBSTR(
      CASE WHEN p_webex_url IS NOT NULL THEN 'WEBEX_URL=' || p_webex_url || '; ' ELSE '' END ||
      CASE WHEN p_notes IS NOT NULL THEN 'NOTES=' || p_notes ELSE '' END,
      1, 1000
    );

    UPDATE SERVICETRANSACTIONS st
       SET st.SERVICE_NOTES = v_notes,
           st.STAMPUSER     = NVL(p_stampuser, 'web'),
           st.STAMPDATE     = SYSDATE
     WHERE st.TRANSACTION_ID = (
       SELECT transaction_id
         FROM (
           SELECT transaction_id
             FROM SERVICETRANSACTIONS
            WHERE SRC_TYPE = p_src_type
              AND SRC_ID   = p_src_id
            ORDER BY STAMPDATE DESC, TRANSACTION_ID DESC
         )
       WHERE ROWNUM = 1
     );

    IF SQL%ROWCOUNT = 0 THEN
      RAISE_APPLICATION_ERROR(-20001, 'No service transaction found for source.');
    END IF;
  END SAVE_SERVICE_INFO;
END FQ_PROCS;
/
