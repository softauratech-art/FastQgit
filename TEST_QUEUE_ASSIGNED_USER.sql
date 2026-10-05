-- Run in FastQ as FQOWNER. Read-only. Change these two values.
WITH params AS (
    SELECT 10001 AS queue_id, UPPER(TRIM('B25901449')) AS permit_number
    FROM dual
),
    QUEUE_PROCESS_MAP (queue_id, permit_type, folder_match_kind, foldertypes, processcode) AS
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
folder_candidates AS (
    SELECT f.folderrsn, f.foldertype,
           COUNT(*) OVER () AS folder_count
    FROM folder@LDMSDEV_LINK f
    CROSS JOIN params i
    WHERE UPPER(TRIM(f.referencefile)) = i.permit_number
),
process_candidates AS (
    SELECT fp.folderrsn, fp.processrsn, fp.processcode, fp.assigneduser,
           COUNT(*) OVER (PARTITION BY fp.folderrsn) AS process_count
    FROM folderprocess@LDMSDEV_LINK fp
    WHERE fp.folderrsn IN (SELECT folderrsn FROM folder_candidates)
)
SELECT i.queue_id, i.permit_number,
       m.permit_type AS excel_permit_type,
       m.foldertypes AS mapped_folder_types,
       m.processcode AS mapped_processcode,
       f.folderrsn, f.foldertype, f.folder_count,
       fp.processrsn, fp.processcode, fp.process_count,
       TRIM(fp.assigneduser) AS assigned_user,
       CASE
           WHEN m.queue_id IS NULL THEN 'QUEUE NOT MAPPED'
           WHEN m.folder_match_kind = 'NONE' THEN 'LOOKUP DISABLED BY MAP'
           WHEN f.folderrsn IS NULL THEN 'PERMIT NOT FOUND'
           WHEN NOT (
               f.folder_count = 1 OR m.folder_match_kind = 'ANY'
               OR (m.folder_match_kind = 'TYPES' AND
                   INSTR(',' || m.foldertypes || ',',
                         ',' || TRIM(UPPER(f.foldertype)) || ',') > 0)
           ) THEN 'FOLDER TYPE EXCLUDED'
           WHEN fp.processrsn IS NULL THEN 'NO PROCESS ROWS'
           WHEN fp.process_count > 1 AND m.processcode IS NULL
               THEN 'MULTIPLE PROCESSES; NO MAPPED CODE'
           WHEN fp.process_count > 1 AND fp.processcode <> m.processcode
               THEN 'PROCESS CODE EXCLUDED'
           WHEN fp.process_count > 1 AND fp.processcode IS NULL
               THEN 'PROCESS CODE EXCLUDED'
           WHEN TRIM(fp.assigneduser) IS NULL THEN 'ASSIGNED USER IS BLANK'
           ELSE 'MATCH - EXPECT IN FASTQ'
       END AS test_result
FROM params i
LEFT JOIN queue_process_map m ON m.queue_id = i.queue_id
LEFT JOIN folder_candidates f ON 1 = 1
LEFT JOIN process_candidates fp ON fp.folderrsn = f.folderrsn
ORDER BY f.folderrsn, fp.processcode, fp.processrsn;
