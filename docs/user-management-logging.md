# User management diagnostics

User detail/edit lookup failures and save/reload failures use the existing NLog file target and logger `FastQ.UserManagement`. Missing lookup results are warnings; exceptions are errors with the full stack trace and inner exceptions.

Configured server location (from `src/FastQ.Web/nlog.config`):

```text
D:\inetpub\logs\LogFiles\FastQ.Manager\yyyy-MM-dd_FastQ-app.log
```

The date in the filename follows the server clock. Search for the error reference shown in the GUI. The JSON context includes the operation, target user ID, Windows account, selected entity ID, and MVC action. Request fields, tokens, and passwords are not logged by this diagnostic code.

Deploy the updated web assembly, views, and `nlog.config`. Ensure the IIS application-pool identity has Modify permission on the log directory. If the deployed config overrides `logDirectory`, use that directory instead. Trigger a failed lookup and match the GUI reference to its log entry to verify logging on the server.
