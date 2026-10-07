-- Run as FQOWNER after FQ_QUEUE_EMAIL exists. Application needs EXECUTE grants
-- on these procedures if its connection uses another schema.
CREATE OR REPLACE PROCEDURE FQ_GET_QUEUE_EMAILS (
 p_queue_id IN NUMBER,
 p_cur OUT SYS_REFCURSOR
) AUTHID DEFINER AS
BEGIN
 OPEN p_cur FOR SELECT email FROM FQ_QUEUE_EMAIL
 WHERE queue_id=p_queue_id ORDER BY email;
END;

CREATE OR REPLACE PROCEDURE FQ_SAVE_QUEUE_EMAILS (
 p_queue_id IN NUMBER,
 p_emails_json IN CLOB
) AUTHID DEFINER AS
 v_queue NUMBER;
 v_count NUMBER;
 v_invalid NUMBER;
BEGIN
 -- Validate the complete payload before replacing existing settings.
 IF p_emails_json IS NULL OR NOT REGEXP_LIKE(p_emails_json, '^\s*\[') THEN
  RAISE_APPLICATION_ERROR(-20001,'Expected an email JSON array; use [] to clear recipients.');
 END IF;
 SELECT COUNT(*), NVL(SUM(CASE WHEN email IS NULL OR LENGTH(email)>320
    OR NOT REGEXP_LIKE(email,'^[^[:space:]@,;<>]+@[^[:space:]@,;<>]+$')
    THEN 1 ELSE 0 END),0)
 INTO v_count,v_invalid
 FROM JSON_TABLE(p_emails_json, '$[*]' ERROR ON ERROR
      COLUMNS (email VARCHAR2(4000) PATH '$' ERROR ON ERROR));
 IF v_count>50 OR v_invalid>0 THEN
  RAISE_APPLICATION_ERROR(-20002,'Use at most 50 valid email addresses.');
 END IF;
 SELECT queue_id INTO v_queue FROM VALIDQUEUES WHERE queue_id=p_queue_id FOR UPDATE;
 SAVEPOINT fq_save_queue_emails;
 BEGIN
  DELETE FROM FQ_QUEUE_EMAIL WHERE queue_id=p_queue_id;
  INSERT INTO FQ_QUEUE_EMAIL(queue_id,email)
  SELECT p_queue_id, MIN(TRIM(email))
  FROM JSON_TABLE(p_emails_json, '$[*]' ERROR ON ERROR
       COLUMNS (email VARCHAR2(4000) PATH '$' ERROR ON ERROR))
  GROUP BY LOWER(TRIM(email));
 EXCEPTION WHEN OTHERS THEN
  ROLLBACK TO fq_save_queue_emails;
  RAISE;
 END;
 -- Caller owns the transaction. No commit here.
END;
