-- Complete specification matching FQ_PROCS_ADMIN_body.sql.
-- UPSERT_USER includes p_lobbyqueues after p_queueadminqueues.
CREATE OR REPLACE PACKAGE FQ_PROCS_ADMIN AS

  FUNCTION IS_SUPER_ADMIN (p_entityid VALIDQUEUES.ENTITY_ID%type, p_userid varchar2)
  RETURN NUMBER;

  FUNCTION IS_QUEUE_ADMIN (p_queueid VALIDQUEUES.QUEUE_ID%type, p_userid varchar2)
  RETURN NUMBER;

  PROCEDURE UPSERT_QSERVICE (
      p_serviceid  IN NUMBER,
      p_queueid IN NUMBER,
      p_name IN VARCHAR2,
      p_namees IN VARCHAR2,
      p_namecp IN VARCHAR2,
      p_active IN VARCHAR2,
      p_stampuser IN VARCHAR2,
      p_out OUT VARCHAR2
  );

  procedure DELETE_QSERVICE (
      p_serviceid  IN NUMBER,
      p_stampuser IN VARCHAR2,
      p_out OUT VARCHAR2
  );

  PROCEDURE UPSERT_QSCHEDULE (
      p_scheduleid  IN NUMBER,
      p_queueid IN NUMBER,
      p_datebegin IN VARCHAR2,
      p_dateend IN VARCHAR2,
      p_opentime IN VARCHAR2,
      p_closetime IN VARCHAR2,
      p_duration IN VARCHAR2,
      p_weekdays IN VARCHAR2,
      p_availres IN NUMBER := 1,
      p_stampuser IN VARCHAR2,
      p_out OUT VARCHAR2
  );

  PROCEDURE DELETE_QSCHEDULE (
      p_scheduleid  IN NUMBER,
      p_stampuser IN VARCHAR2,
      p_out OUT VARCHAR2
  );

  PROCEDURE DELETE_QUEUE (
      p_queueid   IN NUMBER,
      p_stampuser IN VARCHAR2,
      p_out OUT VARCHAR2
  );

  PROCEDURE UPSERT_QUEUE (
      p_queueid NUMBER,
      p_entityid IN NUMBER :=1,
      p_name IN VARCHAR2,
      p_namees IN VARCHAR2,
      p_namecp IN VARCHAR2,
      p_address IN VARCHAR2,
      p_phone IN VARCHAR2,
      p_activeflag IN VARCHAR2,
      p_emponly IN VARCHAR2,
      p_hasguidelines IN VARCHAR2,
      p_hasuploads IN VARCHAR2,
      p_hideinkiosk IN VARCHAR2,
      p_hideinmonitor IN VARCHAR2,
      p_leadtimemax IN VARCHAR2,
      p_leadtimemin IN VARCHAR2,
      p_contacttypes IN VARCHAR2,
      p_refcriterias IN VARCHAR2,
      p_stampuser IN VARCHAR2,
      p_outnewqueueid OUT NUMBER,
      p_outmsg    OUT VARCHAR2
  );

  PROCEDURE UPSERT_USER (
      p_action VARCHAR2,
      p_userid VARCHAR2,
      p_entityid IN NUMBER := 1,
      p_firstname IN VARCHAR2,
      p_lastname IN VARCHAR2,
      p_title IN VARCHAR2,
      p_email IN VARCHAR2,
      p_phone IN VARCHAR2,
      p_language IN VARCHAR2,
      p_activeflag IN VARCHAR2,
      p_configadminflag IN VARCHAR2,
      p_hostqueues IN VARCHAR2,
      p_providerqueues IN VARCHAR2,
      p_reporterqueues IN VARCHAR2,
      p_queueadminqueues IN VARCHAR2,
      p_lobbyqueues IN VARCHAR2,
      p_stampuser IN VARCHAR2,
      p_outmsg    OUT VARCHAR2
  );

  PROCEDURE UPSERT_ENTITY (
      p_entityid  IN NUMBER,
      p_name IN VARCHAR2,
      p_description IN VARCHAR2,
      p_address IN VARCHAR2,
      p_phone IN VARCHAR2,
      p_activeflag IN VARCHAR2,
      p_opensat IN VARCHAR2,
      p_closesat IN VARCHAR2,
      p_outagenotifyat IN VARCHAR2,
      p_outagebeginat IN VARCHAR2,
      p_outageendat IN VARCHAR2,
      p_outagemessage IN VARCHAR2,
      p_stampuser IN VARCHAR2,
      p_affectedappts OUT NUMBER,
      p_outmsg OUT VARCHAR2
  );

  PROCEDURE UPSERT_QACCESS (
      p_queueid IN NUMBER,
      p_hostusers IN VARCHAR2,
      p_providerusers IN VARCHAR2,
      p_reporterusers IN VARCHAR2,
      p_queueadminusers IN VARCHAR2,
      p_stampuser IN VARCHAR2,
      p_outmsg    OUT VARCHAR2
  );

  PROCEDURE DELETE_USER (
      p_userid    IN VARCHAR2,
      p_entityid  IN NUMBER,
      p_stampuser IN VARCHAR2,
      p_out OUT VARCHAR2
  );

  PROCEDURE UPSERT_HOLIDAY (
      p_holiday  IN DATE,
      p_description IN VARCHAR2,
      p_active IN VARCHAR2,
      p_stampuser IN VARCHAR2,
      p_out OUT VARCHAR2
  );

  procedure DELETE_HOLIDAY (
      p_holiday  IN VARCHAR2,
      p_stampuser IN VARCHAR2,
      p_out OUT VARCHAR2
  );

END FQ_PROCS_ADMIN;
/
SHOW ERRORS PACKAGE FQ_PROCS_ADMIN;
