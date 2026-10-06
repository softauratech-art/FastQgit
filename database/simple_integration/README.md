# Simple AMANDA lookup and queue emails

No triggers, scheduler, mapping table or tracking rows. One AMANDA table holds matching process details; a separate queue/email table configures notification recipients.

## Install

These scripts are not executed against the database by this change.

1. Create the two tables with 01_tables.sql (first installation only).
2. Compile 02_get_permit_info.sql as FQOWNER. LDMSDEV_LINK must allow reads of AMANDA FOLDER and FOLDERPROCESS.
3. Merge the FQ_STORE_AMANDA_INFO calls from FQ_PROCS_body.sql into the deployed INSERT_WALKIN and TRANSFER_SOURCE procedures. FQ_PROCS_body.sql now uses the supplied updated package body. Compile it against the matching deployed specification; the older repository specification has different procedure signatures and should not be redeployed blindly.
4. Compile FQ_EXTERNAL_body.sql against its matching deployed specification. It uses the supplied current body and calls FQ_STORE_AMANDA_INFO after appointment INSERT, before SMS. Lookup failure rolls back that creation to its savepoint and returns the error. External walk-ins delegate to FQ_PROCS.INSERT_WALKIN, so they need no second lookup.
5. Deploy FQ_PROCS_GET_body.sql. Both provider lists now read FQ_AMANDA_INFO locally and return ASSIGNEDUSER. The existing GUI shows Reviewer beside Stamp User.
6. Build/publish on Windows with Visual Studio MSBuild, including both updated assemblies and views. Set addresses in Queue → Edit → Email Notifications. Empty settings disable the send prompt. SMTP configuration stays in appSettings.

If old snapshot triggers/job are installed in the database despite the source revert, disable/remove them before deploying direct creation calls to avoid duplicate execution. These scripts do not drop existing objects automatically.

## Behavior

The lookup runs synchronously through the database link. It stores only mapped processes, including blank assignee metadata. No match means no rows. Lookup errors propagate to the creation procedure; no automatic retry is provided. Creation waits for AMANDA. The procedure does not commit. The full 20 mappings are inline in the lookup procedure. Folder type resolves multiple folders and process code resolves multiple processes, preserving the single-match fallback.

This is a creation snapshot. Updating a reference or changing AMANDA assignments does not automatically refresh it; explicitly call the procedure again if needed. No backfill is automatic.

Email settings require the selected entity's SuperAdmin or the selected queue's QueueAdmin. Both appointments and walk-ins offer Send Email after staff creation when recipients exist. Recipients are reloaded when sending. A successful send or Done closes the modal; failures allow retry. Walk-in calendar files use arrival time with no invented duration. Appointment and walk-in UIDs differ. Notification authorization is restricted to entries created in the current staff session; successful sends are deduplicated within that session.

## Verification

Local mapping fixtures: python3 tests/simple_integration/test_mapping.py

Oracle compilation, transactional error handling, live permissions, mail delivery and a Windows build still require environment validation. Test both creation types, same numeric IDs across types, no match, mapped processes, database-link failure, rollback, configured/empty emails, and cross-entity settings denial.
