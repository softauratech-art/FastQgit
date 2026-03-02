CREATE OR REPLACE PACKAGE FQ_PROCS_GET AS
  PROCEDURE GET_LOCATION (
    p_locationid IN VARCHAR2,
    p_cur OUT Ref_Cursor_Types.ref_cursor
  );

  PROCEDURE GET_MYQUEUES (
    p_userid IN VARCHAR2,
    p_locationid IN NUMBER,
    p_cur OUT Ref_Cursor_Types.ref_cursor
  );

  PROCEDURE GET_QUEUE_DETAILS (
    p_queueid IN NUMBER,
    p_cur OUT Ref_Cursor_Types.ref_cursor
  );

  PROCEDURE GET_QUEUE_OPENSLOTS (
    p_queueid IN NUMBER,
    p_date IN DATE,
    p_cur OUT Ref_Cursor_Types.ref_cursor
  );

  PROCEDURE GET_APPTHIST4CUSTOMER (
    p_customerid IN NUMBER,
    p_cur OUT Ref_Cursor_Types.ref_cursor
  );

  PROCEDURE GET_APPTS4CUSTOMER (
    p_customerid IN INT,
    p_range_startdate IN DATE,
    p_range_enddate IN DATE,
    p_cur OUT Ref_Cursor_Types.ref_cursor
  );

  PROCEDURE GET_MYWALKINS (
    p_userid IN VARCHAR2,
    p_range_startdate IN DATE,
    p_range_enddate IN DATE,
    p_cur OUT Ref_Cursor_Types.ref_cursor
  );

  PROCEDURE GET_MYAPPOINTMENTS (
    p_userid IN VARCHAR2,
    p_range_startdate IN DATE,
    p_range_enddate IN DATE,
    p_cur OUT Ref_Cursor_Types.ref_cursor
  );

  PROCEDURE GET_APPT_DETAILS (
    p_apptid IN Appointments.appointment_id%type,
    p_cur OUT Ref_Cursor_Types.ref_cursor
  );

  PROCEDURE GET_MYPROFILE (
    p_userid IN VARCHAR2,
    p_cur OUT Ref_Cursor_Types.ref_cursor
  );

  FUNCTION GET_USERID (
    p_useremail IN VARCHAR2,
    p_stampuser IN VARCHAR2
  ) RETURN VARCHAR2;

  PROCEDURE GET_USERS (
    p_entityid IN NUMBER,
    p_stampuser IN VARCHAR2,
    p_message OUT VARCHAR2,
    p_cur OUT Ref_Cursor_Types.ref_cursor
  );

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
    p_location IN INT,
    p_ref_cursor OUT Ref_Cursor_Types.ref_cursor
  );

  PROCEDURE GET_SERVICES (
    p_queueid IN INT,
    p_ref_cursor OUT Ref_Cursor_Types.ref_cursor
  );

  PROCEDURE GET_QSERVICE_DETAILS (
    p_serviceid IN NUMBER,
    p_userid IN VARCHAR2,
    p_cur OUT Ref_Cursor_Types.ref_cursor
  );
END FQ_PROCS_GET;
/
