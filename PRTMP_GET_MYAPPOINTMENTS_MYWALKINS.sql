create or replace PROCEDURE PRTMP_GET_MYAPPOINTMENTS (
-- For Internal Use ONLY - Get logged-in Staff's Queued Appts
    p_entityid IN NUMBER,
    p_userid IN  VARCHAR2,
    p_range_startdate   IN  DATE,
    p_range_enddate     IN  DATE,  
    p_cur  OUT Ref_Cursor_Types.ref_cursor
)
AS
v_isSuperAdmin NUMBER;
BEGIN
 v_isSuperAdmin := FQ_PROCS_ADMIN.IS_SUPER_ADMIN(p_entityid, p_userid);

 OPEN p_cur FOR
--    SELECT q.queue_id, q.name, vs.service_id, vs.service_name,
--        --p.role_id, r.role_desc,
--        provider_flag, host_flag, queueadmin_Flag, reporter_Flag,
--        u.fname, u.lname,
--        GET_USERNAME(a.stampuser) stampusername,
--        a.*, c.sms_optin
--        , fq_crypto_pkg.decrypt(c.fname) cust_fname, fq_crypto_pkg.decrypt(c.lname) cust_lname
--        , fq_crypto_pkg.decrypt(c.email) cust_email, fq_crypto_pkg.decrypt(c.phone) cust_phone
--    FROM validqueues q
--        INNER JOIN appointments a ON q.queue_id = a.queue_id
--        INNER join validqueue_services vs ON
--            vs.queue_id = a.queue_id and vs.service_id = a.service_id  
--        INNER JOIN user_permissions p ON q.queue_id = p.queue_id
--        INNER JOIN fq_users u ON u.user_id = p.user_id
--        INNER JOIN customers c on c.customer_id = a.customer_id        
--    WHERE
--            lower(u.user_id) = lower(p_userid)
--        AND NVL(u.activeflag,'N') = 'Y'          
--        AND q.entity_id = p_entityid
--        AND trunc(appt_date)
--            BETWEEN trunc(p_range_startdate) AND trunc(p_range_enddate);


-- PReddy: Use the below if allowing SuperAdmins to automatically inherit
--          Provider access to 'ALL' Queues in this Entity
WITH P AS
(
        -- Dynamically build QPERMS for this user (including isADMIN)
        -- Left-Join to get all_Queues and then eliminate NULL perms for non-admins
            select lower(p_userid) user_id
                , q1.queue_id
                , q1.name
                , entity_id
                , DECODE(v_isSuperAdmin, 1, 'Y', HOST_FLAG) HOST_FLAG
                , DECODE(v_isSuperAdmin, 1, 'Y', PROVIDER_FLAG) PROVIDER_FLAG
                , DECODE(v_isSuperAdmin, 1, 'Y', REPORTER_FLAG) REPORTER_FLAG
                , DECODE(v_isSuperAdmin, 1, 'Y', QUEUEADMIN_FLAG) QUEUEADMIN_FLAG
            from validqueues q1
                LEFT JOIN user_permissions p1
                    ON p1.user_id = lower(p_userid)
                and q1.queue_id = p1.queue_id
            where   entity_id = p_entityid
                and (    
                       DECODE(v_isSuperAdmin, 1, 'Y', HOST_FLAG)  ='Y'
                    or DECODE(v_isSuperAdmin, 1, 'Y', PROVIDER_FLAG) ='Y'
                    or DECODE(v_isSuperAdmin, 1, 'Y', REPORTER_FLAG) = 'Y'
                    or DECODE(v_isSuperAdmin, 1, 'Y', QUEUEADMIN_FLAG) = 'Y'
                )
)
SELECT  p.queue_id, p.name, vs.service_id, vs.service_name,
        provider_flag,
        host_flag,
        queueadmin_Flag, reporter_Flag,
        u.fname, u.lname,
        FQ_PROCS_GET.GET_USERNAME(a.stampuser) stampusername,
        a.*, c.sms_optin,
        st.service_notes, st.service_start_time, st.service_end_time
        , fq_crypto_pkg.decrypt(c.fname) cust_fname, fq_crypto_pkg.decrypt(c.lname) cust_lname
        , fq_crypto_pkg.decrypt(c.email) cust_email, fq_crypto_pkg.decrypt(c.phone) cust_phone
    FROM
        appointments a
        INNER join validqueue_services vs ON
            vs.queue_id = a.queue_id and vs.service_id = a.service_id  
        INNER JOIN P
               ON P.queue_id = a.Queue_id                      
        INNER JOIN fq_users u ON u.user_id = p.user_id
        INNER JOIN customers c on c.customer_id = a.customer_id
        LEFT JOIN (
            SELECT src_id,
                   MAX(service_notes) KEEP (DENSE_RANK LAST ORDER BY stampdate NULLS FIRST, transaction_id) service_notes,
                   MAX(service_start_time) KEEP (DENSE_RANK LAST ORDER BY stampdate NULLS FIRST, transaction_id) service_start_time,
                   MAX(service_end_time) KEEP (DENSE_RANK LAST ORDER BY stampdate NULLS FIRST, transaction_id) service_end_time
              FROM servicetransactions
             WHERE src_type = 'A'
             GROUP BY src_id
        ) st ON st.src_id = a.appointment_id
    WHERE
                lower(u.user_id) = lower(p_userid)
        AND NVL(u.activeflag,'N') = 'Y'
        AND p.entity_id = p_entityid
        AND trunc(appt_date)
                BETWEEN trunc(p_range_startdate) AND trunc(p_range_enddate)
        ;
END;

create or replace PROCEDURE PRTMP_GET_MYWALKINS (
-- For Internal Use ONLY - Get logged-in Staff's Queued Walkins
    p_entityid IN NUMBER,
    p_userid IN  VARCHAR2,
    p_range_startdate   IN  DATE,
    p_range_enddate     IN  DATE,  
    p_cur  OUT Ref_Cursor_Types.ref_cursor
)
AS
v_isSuperAdmin NUMBER;
BEGIN
 v_isSuperAdmin := FQ_PROCS_ADMIN.IS_SUPER_ADMIN(p_entityid, p_userid);
 OPEN p_cur FOR
--    SELECT q.queue_id, q.name, vs.service_id, vs.service_name,
--        --p.role_id, r.role_desc,
--        provider_flag, host_flag, queueadmin_Flag, reporter_Flag,
--        u.fname, u.lname,
--        GET_USERNAME(a.stampuser) stampusername,
--        a.*, c.sms_optin
--        , fq_crypto_pkg.decrypt(c.fname) cust_fname, fq_crypto_pkg.decrypt(c.lname) cust_lname
--        , fq_crypto_pkg.decrypt(c.email) cust_email, fq_crypto_pkg.decrypt(c.phone) cust_phone
--    FROM validqueues q
--        INNER JOIN walkins a ON q.queue_id = a.queue_id
--        INNER join validqueue_services vs ON
--            vs.queue_id = a.queue_id and vs.service_id = a.service_id  
--        INNER JOIN user_permissions p ON q.queue_id = p.queue_id
--        INNER JOIN fq_users u ON u.user_id = p.user_id
--        INNER JOIN customers c on c.customer_id = a.customer_id
--    WHERE (lower(u.user_id) = lower(p_userid))
--            AND NVL(u.activeflag,'N') = 'Y'            
--            AND q.entity_id = p_entityid
--            AND trunc(createdon)
--                BETWEEN trunc(p_range_startdate) AND trunc(p_range_enddate);

-- PReddy: Use the below if allowing SuperAdmins to automatically inherit
--          Provider access to 'ALL' Queues in this Entity
WITH P AS
(
        -- Dynamically build QPERMS for this user (including isADMIN)
        -- Left-Join to get all_Queues and then eliminate NULL perms for non-admins
            select lower(p_userid) user_id
                , q1.queue_id
                , q1.name
                , entity_id
                , DECODE(v_isSuperAdmin, 1, 'Y', HOST_FLAG) HOST_FLAG
                , DECODE(v_isSuperAdmin, 1, 'Y', PROVIDER_FLAG) PROVIDER_FLAG
                , DECODE(v_isSuperAdmin, 1, 'Y', REPORTER_FLAG) REPORTER_FLAG
                , DECODE(v_isSuperAdmin, 1, 'Y', QUEUEADMIN_FLAG) QUEUEADMIN_FLAG
            from validqueues q1
                LEFT JOIN user_permissions p1
                    ON p1.user_id = lower(p_userid)
                and q1.queue_id = p1.queue_id
            where   entity_id = p_entityid
                and (    
                       DECODE(v_isSuperAdmin, 1, 'Y', HOST_FLAG)  ='Y'
                    or DECODE(v_isSuperAdmin, 1, 'Y', PROVIDER_FLAG) ='Y'
                    or DECODE(v_isSuperAdmin, 1, 'Y', REPORTER_FLAG) = 'Y'
                    or DECODE(v_isSuperAdmin, 1, 'Y', QUEUEADMIN_FLAG) = 'Y'
                )
)
SELECT  p.queue_id, p.name, vs.service_id, vs.service_name,
        provider_flag,
        host_flag,
        queueadmin_Flag, reporter_Flag,
        u.fname, u.lname,
        FQ_PROCS_GET.GET_USERNAME(a.stampuser) stampusername,
        a.*, c.sms_optin,
        st.service_notes, st.service_start_time, st.service_end_time
        , fq_crypto_pkg.decrypt(c.fname) cust_fname, fq_crypto_pkg.decrypt(c.lname) cust_lname
        , fq_crypto_pkg.decrypt(c.email) cust_email, fq_crypto_pkg.decrypt(c.phone) cust_phone
    FROM
        walkins a
        INNER join validqueue_services vs ON
            vs.queue_id = a.queue_id and vs.service_id = a.service_id  
        INNER JOIN P
               ON P.queue_id = a.Queue_id                      
        INNER JOIN fq_users u ON u.user_id = p.user_id
        INNER JOIN customers c on c.customer_id = a.customer_id
        LEFT JOIN (
            SELECT src_id,
                   MAX(service_notes) KEEP (DENSE_RANK LAST ORDER BY stampdate NULLS FIRST, transaction_id) service_notes,
                   MAX(service_start_time) KEEP (DENSE_RANK LAST ORDER BY stampdate NULLS FIRST, transaction_id) service_start_time,
                   MAX(service_end_time) KEEP (DENSE_RANK LAST ORDER BY stampdate NULLS FIRST, transaction_id) service_end_time
              FROM servicetransactions
             WHERE src_type = 'W'
             GROUP BY src_id
        ) st ON st.src_id = a.walkin_id
    WHERE
                lower(u.user_id) = lower(p_userid)
        AND NVL(u.activeflag,'N') = 'Y'
        AND p.entity_id = p_entityid
        AND trunc(createdon)
                BETWEEN trunc(p_range_startdate) AND trunc(p_range_enddate)
        ;

END; 


