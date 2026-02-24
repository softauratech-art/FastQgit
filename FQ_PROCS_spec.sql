CREATE OR REPLACE PACKAGE FQ_PROCS AS
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
