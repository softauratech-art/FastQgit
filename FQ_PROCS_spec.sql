CREATE OR REPLACE PACKAGE FQ_PROCS AS
  PROCEDURE VALIDATE_PERMIT (
    p_queueid        IN NUMBER,
    p_permit_number  IN VARCHAR2,
    p_is_valid       OUT NUMBER,
    p_outmsg         OUT VARCHAR2
  );

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
  );

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
  );

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
  );

  PROCEDURE UPDATE_APPOINTMENT (
    p_apptid         IN NUMBER,
    p_customer_id    IN NUMBER,
    p_queue_id       IN NUMBER,
    p_service_id     IN NUMBER,
    p_ref_criteria   IN VARCHAR2,
    p_ref_value      IN VARCHAR2,
    p_contacttype    IN VARCHAR2,
    p_moreinfo       IN VARCHAR2,
    p_appt_date      IN DATE,
    p_start_time     IN VARCHAR2,
    p_end_time       IN VARCHAR2,
    p_status         IN VARCHAR2,
    p_confcode       IN VARCHAR2,
    p_meetingurl     IN VARCHAR2,
    p_language_pref  IN VARCHAR2,
    p_stampuser      IN VARCHAR2,
    p_outmsg         OUT VARCHAR2
  );

  PROCEDURE UPDATE_APPT_STATUS (
    p_apptid    IN APPOINTMENTS.APPOINTMENT_ID%TYPE,
    p_action    IN VARCHAR2,
    p_stampuser IN VARCHAR2,
    p_notes     IN VARCHAR2,
    p_outmsg    OUT VARCHAR2
  );

  PROCEDURE SET_SERVICE_TRANSACTION (
    p_src_type   IN  VARCHAR2,
    p_src_id     IN  NUMBER,
    p_action     IN  VARCHAR2,
    p_stampuser  IN  VARCHAR2,
    p_notes      IN  VARCHAR2,
    p_outmsg     OUT VARCHAR2
  );

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
  );

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
    p_servicenotes      IN VARCHAR2,
    p_stampuser         IN VARCHAR2,
    p_new_src_id        OUT NUMBER,
    p_outmsg            OUT VARCHAR2
  );

  PROCEDURE SAVE_SERVICE_INFO(
    p_src_type   IN CHAR,
    p_src_id     IN NUMBER,
    p_webex_url  IN VARCHAR2,
    p_notes      IN VARCHAR2,
    p_stampuser  IN VARCHAR2
  );
END FQ_PROCS;
/
