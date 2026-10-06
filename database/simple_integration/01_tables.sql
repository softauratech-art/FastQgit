-- First installation, as FQOWNER.
CREATE TABLE FQ_AMANDA_INFO (
 src_type CHAR(1) NOT NULL CHECK (src_type IN ('A','W')),
 src_id NUMBER NOT NULL, queue_id NUMBER NOT NULL,
 permit_reference VARCHAR2(1000 CHAR),
 folderrsn NUMBER NOT NULL, foldertype VARCHAR2(50 CHAR),
 processrsn NUMBER NOT NULL, processcode NUMBER,
 assigneduser VARCHAR2(320 CHAR), signoffuser VARCHAR2(320 CHAR),
 scheduledate DATE, scheduleenddate DATE, startdate DATE, enddate DATE,
 statuscode NUMBER, passedflag VARCHAR2(10 CHAR), captured_at DATE DEFAULT SYSDATE NOT NULL,
 CONSTRAINT PK_FQ_AMANDA_INFO PRIMARY KEY(src_type,src_id,processrsn)
);
CREATE TABLE FQ_QUEUE_EMAIL (
 queue_id NUMBER NOT NULL REFERENCES VALIDQUEUES(queue_id),
 email VARCHAR2(320 CHAR) NOT NULL,
 CONSTRAINT PK_FQ_QUEUE_EMAIL PRIMARY KEY(queue_id,email)
);
