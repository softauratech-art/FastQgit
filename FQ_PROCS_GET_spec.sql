-- Complete specification matching FQ_PROCS_GET_body.sql.
CREATE OR REPLACE PACKAGE FQ_PROCS_GET AS

  PROCEDURE GET_ENTITY (
      p_entityid         IN   NUMBER,
      p_cur              OUT  Ref_Cursor_Types.ref_cursor
  );

  PROCEDURE GET_ENTITIES_ALL (
      p_cur              OUT  Ref_Cursor_Types.ref_cursor
  );

  PROCEDURE GET_MYQUEUES (
      p_userid       IN   VARCHAR2,
      p_entityid   IN   NUMBER,
      p_cur   OUT  Ref_Cursor_Types.ref_cursor
  );

  PROCEDURE GET_QUEUE_DETAILS (
      p_queueid     IN    NUMBER,
      p_cur  OUT  Ref_Cursor_Types.ref_cursor
  );

  PROCEDURE GET_MYWALKINS (
      p_entityid IN NUMBER,
      p_userid IN  VARCHAR2,
      p_range_startdate   IN  DATE,
      p_range_enddate     IN  DATE,
      p_cur  OUT Ref_Cursor_Types.ref_cursor
  );

  PROCEDURE GET_MYAPPOINTMENTS (
      p_entityid IN NUMBER,
      p_userid IN  VARCHAR2,
      p_range_startdate   IN  DATE,
      p_range_enddate     IN  DATE,
      p_cur  OUT Ref_Cursor_Types.ref_cursor
  );

  PROCEDURE GET_APPT_DETAILS (
      p_apptid IN Appointments.appointment_id%type,
      p_cur OUT Ref_Cursor_Types.ref_cursor
  );

  PROCEDURE GET_MYPROFILE (p_userid IN VARCHAR2, p_cur OUT Ref_Cursor_Types.ref_cursor);

  FUNCTION GET_USERNAME (p_userid IN VARCHAR2)
  RETURN VARCHAR2;

  PROCEDURE GET_USERS (p_entityid IN NUMBER, p_stampuser IN VARCHAR2, p_message OUT VARCHAR2, p_cur OUT Ref_Cursor_Types.ref_cursor);

  PROCEDURE GET_USER_QUEUES_ROLES (
      p_userid IN VARCHAR2,
      p_stampuser IN VARCHAR2,
      p_message OUT VARCHAR2,
      p_cur OUT Ref_Cursor_Types.ref_cursor
  );

  PROCEDURE GET_USER (
      p_userid IN VARCHAR2,
      p_stampuser IN VARCHAR2,
      p_message OUT VARCHAR2,
      p_cur OUT Ref_Cursor_Types.ref_cursor
  );

  PROCEDURE GET_QUEUES (
      p_entity     IN   INT,
      p_ref_cursor   OUT  Ref_Cursor_Types.ref_cursor
  );

  PROCEDURE GET_SERVICES (
      p_queueid    IN INT,
      p_ref_cursor OUT Ref_Cursor_Types.ref_cursor
  );

  PROCEDURE GET_QSERVICE_DETAILS(
   p_serviceid IN number,
   p_userid IN varchar2,
   p_cur  OUT  Ref_Cursor_Types.ref_cursor);

  PROCEDURE GET_QSCHEDULE_DETAILS(
   p_scheduleid IN number,
   p_userid IN varchar2,
   p_cur  OUT  Ref_Cursor_Types.ref_cursor);

  PROCEDURE GET_CUSTOMERS (p_fldname IN VARCHAR2, p_fldvalue IN VARCHAR2, p_cur OUT Ref_Cursor_Types.ref_cursor);

  PROCEDURE GET_PROVIDERS_BY_ENTITY (p_entityid NUMBER, p_cur OUT Ref_Cursor_Types.ref_cursor);

  PROCEDURE GET_APPT_ID_BY_CONF (
    p_confcode IN VARCHAR2,
    p_apptid OUT NUMBER
  );

  PROCEDURE GET_QUEUE_ID_FOR_SOURCE (
    p_src_type IN VARCHAR2,
    p_src_id IN NUMBER,
    p_queue_id OUT NUMBER
  );

  PROCEDURE GET_APPTS_BY_QUEUE (
    p_queueid IN NUMBER,
    p_cur OUT Ref_Cursor_Types.ref_cursor
  );

  PROCEDURE GET_APPTS_BY_CUSTOMER (
    p_customerid IN NUMBER,
    p_cur OUT Ref_Cursor_Types.ref_cursor
  );

  PROCEDURE GET_APPTS_BY_ENTITY (
    p_entityid IN NUMBER,
    p_cur OUT Ref_Cursor_Types.ref_cursor
  );

  PROCEDURE GET_ALL_APPTS (
    p_cur OUT Ref_Cursor_Types.ref_cursor
  );

  PROCEDURE GET_USER_ACTION_QUEUE_ACCESS (
      p_userid IN VARCHAR2,
      p_cur OUT Ref_Cursor_Types.ref_cursor
  );

  PROCEDURE GET_QUEUE_ACCESS (
      p_queueid IN NUMBER,
      p_cur OUT Ref_Cursor_Types.ref_cursor
  );

  PROCEDURE GET_USER_ENTITIES (
      p_userid IN VARCHAR2,
      p_stampuser IN VARCHAR2,
      p_message OUT VARCHAR2,
      p_cur OUT Ref_Cursor_Types.ref_cursor
  );

  PROCEDURE GET_VALID_LOOKUPS (p_lookuptable IN VARCHAR2, p_cur OUT Ref_Cursor_Types.Ref_Cursor);

  PROCEDURE GET_HOLIDAY (
      p_holiday         IN   VARCHAR2,
      p_cur              OUT  Ref_Cursor_Types.ref_cursor
  );

  PROCEDURE GET_HOLIDAYS (p_cur OUT  Ref_Cursor_Types.ref_cursor);

  PROCEDURE GET_CUSTOMER_ID_FOR_SOURCE (
    p_src_type IN VARCHAR2,
    p_src_id IN NUMBER,
    p_customer_id OUT NUMBER
  );

END FQ_PROCS_GET;
/
SHOW ERRORS PACKAGE FQ_PROCS_GET;
