-- Run as FQOWNER in the same FastQ database used by the website.
CREATE OR REPLACE PROCEDURE FQ_GET_WALKIN_EMAIL_DETAILS (
 p_walkin_id IN NUMBER,
 p_cur OUT SYS_REFCURSOR
) AUTHID DEFINER AS
BEGIN
 OPEN p_cur FOR
 SELECT w.walkin_id AS appointment_id, w.customer_id, w.queue_id,
        q.entity_id, TRUNC(w.createdon) AS appt_date,
        NUMTODSINTERVAL((w.createdon-TRUNC(w.createdon))*86400,'SECOND') AS start_time,
        w.end_time, w.createdon, w.stampdate, w.status, w.service_id,
        w.ref_criteria, w.ref_value, w.contacttype, w.moreinfo,
        'W-' || TO_CHAR(w.walkin_id) AS confcode,
        w.meetingurl, w.language_pref, w.createdby, w.stampuser,
        FQ_CRYPTO_PKG.DECRYPT(c.fname) AS fname,
        FQ_CRYPTO_PKG.DECRYPT(c.lname) AS lname
 FROM WALKINS w
 JOIN VALIDQUEUES q ON q.queue_id=w.queue_id
 JOIN CUSTOMERS c ON c.customer_id=w.customer_id
 WHERE w.walkin_id=p_walkin_id;
END;

GRANT EXECUTE ON FQ_GET_WALKIN_EMAIL_DETAILS TO FQUSER;
