CREATE OR REPLACE PROCEDURE FQ_STORE_AMANDA_INFO (
 p_type VARCHAR2, p_id NUMBER, p_queue NUMBER, p_criteria VARCHAR2, p_reference VARCHAR2
) AS
BEGIN
 IF p_type IS NULL OR p_type NOT IN ('A','W') THEN
  RAISE_APPLICATION_ERROR(-20001,'Invalid FastQ source type');
 END IF;
 DELETE FROM FQ_AMANDA_INFO WHERE src_type=p_type AND src_id=p_id;
 IF NVL(UPPER(TRIM(p_criteria)),'-') NOT IN ('P','PERMIT') OR TRIM(p_reference) IS NULL THEN
  RETURN;
 END IF;
 INSERT INTO FQ_AMANDA_INFO
 (src_type,src_id,queue_id,permit_reference,folderrsn,foldertype,processrsn,processcode,
  assigneduser,signoffuser,scheduledate,scheduleenddate,startdate,enddate,statuscode,passedflag)
 WITH QUEUE_PROCESS_MAP (queue_id, permit_type, folder_match_kind, foldertypes, processcode) AS
    (
        SELECT 10321, 'any', 'ANY', NULL, NULL FROM dual
        UNION ALL
        SELECT 10283, 'any permits under structure permitting group in LDMS', 'TYPES', 'COM,CS,CT,DEMO,ELEC,FENC,FIR,GAS,LV,MECH,PLUM,RES,ROOF,SIGN,SUN,SWD,SWP,TENT,USE', NULL FROM dual
        UNION ALL
        SELECT 10420, 'none', 'NONE', NULL, NULL FROM dual
        UNION ALL
        SELECT 10333, 'RES Permit', 'TYPES', 'RES', NULL FROM dual
        UNION ALL
        SELECT 10001, 'COM Permit', 'TYPES', 'COM', 50099 FROM dual
        UNION ALL
        SELECT 10003, 'RES Permit', 'TYPES', 'RES', 50101 FROM dual
        UNION ALL
        SELECT 10005, 'ROOF, ELEC, PLUM, GAS, SUN, MECH', 'TYPES', 'ROOF,ELEC,PLUM,GAS,SUN,MECH', NULL FROM dual
        UNION ALL
        SELECT 10007, 'CL folder type', 'TYPES', 'CL', NULL FROM dual
        UNION ALL
        SELECT 10009, 'COM Permit', 'TYPES', 'COM', NULL FROM dual
        UNION ALL
        SELECT 10013, 'CEL, CIL, DEMI, CVRC, PCA, PSA, SCA, TCA, ARIF, IFC, NPG, SCRC, TCRC', 'TYPES', 'CEL,CIL,DEMI,CVRC,PCA,PSA,SCA,TCA,ARIF,IFC,NPG,SCRC,TCRC', NULL FROM dual
        UNION ALL
        SELECT 10015, 'any', 'ANY', NULL, NULL FROM dual
        UNION ALL
        SELECT 10403, 'COM Permit', 'TYPES', 'COM', 50170 FROM dual
        UNION ALL
        SELECT 10361, 'any permits under structure permitting group in LDMS', 'TYPES', 'COM,CS,CT,DEMO,ELEC,FENC,FIR,GAS,LV,MECH,PLUM,RES,ROOF,SIGN,SUN,SWD,SWP,TENT,USE', NULL FROM dual
        UNION ALL
        SELECT 10405, 'SE, VA, ZM', 'TYPES', 'SE,VA,ZM', NULL FROM dual
        UNION ALL
        SELECT 10407, 'COM Permit', 'TYPES', 'COM', 50100 FROM dual
        UNION ALL
        SELECT 10409, 'ZP Addressing', 'TYPES', 'ZP', NULL FROM dual
        UNION ALL
        SELECT 10411, 'multiple cases under DRC group in LDMS', 'TYPES', 'APF,CDR,DISC,DO,DP,DRCA,DVR,EXT,HHA,LUP,LUPA,PRI,PSP', NULL FROM dual
        UNION ALL
        SELECT 10413, 'RES Permit', 'TYPES', 'RES', 50100 FROM dual
        UNION ALL
        SELECT 10415, 'BTR, USE', 'TYPES', 'BTR,USE', NULL FROM dual
        UNION ALL
        SELECT 10417, 'LS, ABA', 'TYPES', 'LS,ABA', NULL FROM dual
    ),
 folders AS (
  SELECT f.folderrsn,f.foldertype,COUNT(*) OVER () folder_count
  FROM folder@LDMSDEV_LINK f WHERE UPPER(TRIM(f.referencefile))=UPPER(TRIM(p_reference))
 ), processes AS (
  SELECT fp.*,COUNT(*) OVER (PARTITION BY fp.folderrsn) process_count
  FROM folderprocess@LDMSDEV_LINK fp WHERE fp.folderrsn IN (SELECT folderrsn FROM folders)
 )
 SELECT p_type,p_id,p_queue,UPPER(TRIM(p_reference)),f.folderrsn,f.foldertype,
 fp.processrsn,fp.processcode,TRIM(fp.assigneduser),fp.signoffuser,
 fp.scheduledate,fp.scheduleenddate,fp.startdate,fp.enddate,fp.statuscode,fp.passedflag
 FROM folders f JOIN processes fp ON fp.folderrsn=f.folderrsn
 JOIN queue_process_map m ON m.queue_id=p_queue
 WHERE m.folder_match_kind<>'NONE'
 AND (f.folder_count=1 OR m.folder_match_kind='ANY' OR
  (m.folder_match_kind='TYPES' AND INSTR(','||m.foldertypes||',',','||UPPER(TRIM(f.foldertype))||',')>0))
 AND (fp.process_count=1 OR fp.processcode=m.processcode);
 -- Caller owns commit/rollback. Lookup failure propagates to the creation procedure.
END;
/
SHOW ERRORS PROCEDURE FQ_STORE_AMANDA_INFO;
