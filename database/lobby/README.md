# Lobby role and page

Lobby uses the **existing Windows authentication**, `FQ_USERS`, `USER_ENTITIES`, `USER_PERMISSIONS`, and FastQ authorization attribute. There is no separate login, account type, or entity-level Lobby setting.

## Deploy

1. Run `01_lobby.sql` as **FQOWNER**. It safely adds `USER_PERMISSIONS.LOBBY_FLAG` beside the existing role flags. It creates no procedures.
2. Compile the complete root package files in this order: **FQ_PROCS_ADMIN_spec.sql**, **FQ_PROCS_GET_spec.sql**, **FQ_PROCS_ADMIN_body.sql**, **FQ_PROCS_GET_body.sql**. Both specifications match the corresponding tracked bodies. `UPSERT_USER` now accepts `p_lobbyqueues` after `p_queueadminqueues`, saving Lobby like Reporter. `GET_USER_QUEUES_ROLES` returns the flag; `GET_MYAPPOINTMENTS` and `GET_MYWALKINS` include Lobby queue permissions. The GET specification also replaces the outdated historical declarations with the current body's signatures.
3. Rebuild and publish **FastQ.Data and FastQ.Web**, including the Lobby view, CSS and JavaScript. Recycle the application to reload session permissions. Deploy database changes before application code; coordinate the user-save signature change with the application deployment. After all instances are updated, run optional `03_remove_legacy_procedures.sql` to remove the old standalone Lobby procedures. Run `02_verify.sql` for read-only checks.
4. In **Users → Create/Edit**, check **Lobby** for the desired queues in the existing Host/Provider/Reporter permissions grid, then Save. For a display-only user leave other roles and Super Admin unchecked.
5. Sign in through the existing Windows login. A Lobby-only user is redirected to `/Lobby`; staff with Lobby plus another role retain their existing access. Super Admin can preview the display via the navigation link.

If the earlier entity-level implementation was deployed, its `USER_ENTITIES.LOBBYFLAG` and access procedures are no longer used. Assign the new queue-level Lobby permissions in User Settings; existing entity flags are not automatically converted.

## Behavior

- Lobby-only users can open the display but cannot open other FastQ MVC pages or execute staff actions. Staff notification subscriptions require a staff role.
- Like existing roles, application authorization uses the authenticated FastQ session. Sign out/reload sessions after changing roles. Invalid entity selections cannot bypass the Lobby-only restriction. Each display refresh reloads current active membership and Lobby queue permissions through the existing user procedures, then filters the existing appointment/walk-in results to assigned visible queues. Revoked access returns 403 and clears the display.
- Only assigned active queues appear; Super Admin sees all active queues in the selected entity. **Hide in Monitor** always applies.
- Today's date uses America/New_York; existing stored times are treated as local business times.
- Waiting = ARRIVED; Now serving = IN PROGRESS. Unchecked-in, completed and cancelled entries are excluded.
- Names and scheduled appointment times / walk-in arrival times appear. Missing appointment start times display “Not scheduled”. No contact details, notes or permits are exposed.
- Refresh every 15 seconds after a request completes. Queue groups rotate every 10 seconds. Waiting has three fixed slots showing the first three entries; unused slots remain blank. Waiting names do not rotate. Previous, Next, Pause and Full screen are available.

## DEV verification

- Create a Windows-authenticated FastQ user with only Lobby checked for two queues. Verify automatic redirect, both assigned queues, names and scheduled/arrival times.
- Verify other queues, hidden queues and inactive queues are absent.
- Try a staff page, staff POST, staff AJAX and notification subscription with that account; verify access denied/redirect without staff data or mutations.
- Verify Host-only and Reporter-only accounts cannot open Lobby; adding Lobby grants display access without removing their existing roles.
- Verify administrator user/queue saves preserve Lobby flags. Change/revoke Lobby and reload the user's session.
- Verify queue slider, overflow names, empty queues, database failure and Eastern date boundary.

Local checks: `node tests/lobby/display.test.cjs` and `dotnet run --project tests/lobby/RoleChecks.csproj` (role policy and existing-repository display integration checks). Oracle compilation, IIS Windows authentication and full .NET Framework web build require the DEV/Windows environment.
