# FastQ JMeter Test Plans (GUI-first)

Primary GUI plans:
- 08-gui-realtime-mixed-all.jmx (recommended: mixed GET + POST, realtime monitoring)
- 07-authenticated-post-workflows.jmx (focused auth + anti-forgery POST workflow)
- 01-smoke.jmx / 02-load.jmx / 03-stress.jmx / 04-soak.jmx / 05-scalability.jmx / 06-spike.jmx

GUI usage:
1. Open JMeter GUI.
2. File -> Open -> `08-gui-realtime-mixed-all.jmx`.
3. In Thread Group, set users/ramp/duration for your run.
4. Update CSV data files before run:
- `data/auth_users.csv`
- `data/realtime_scenarios.csv`
5. Run and monitor in listeners:
- View Results Tree
- Summary Report

What 08 covers:
- Authenticated session bootstrap (`debug=USR`, `debuguserid`, `eid`)
- Anti-forgery token extraction from `/Admin/Dashboard`
- GET flows: Home + Provider snapshot context
- POST flows: Admin EditEntity, ProviderAction, AddWalkin, AddAppointment

Important:
- Requires valid users/roles and valid queue/service/appointment IDs in CSV.
- This uses your app’s debug impersonation path from Web.config (`debugkey=USR`).

WebDriver GUI plan (Chromium):
- 09-gui-webdriver-chromium.jmx

Plugin prerequisites (JMeter GUI):
1. Options -> Plugins Manager.
2. Install `jpgc-webdriver` (WebDriver Set).
3. Ensure Chrome and matching ChromeDriver are available.
4. Restart JMeter.

How to run in GUI:
1. Open `09-gui-webdriver-chromium.jmx`.
2. In `Test Plan -> Variables`, set `baseUrl`, `debugUser`, `entityId`.
3. In `Chrome Driver Config`, set `driver_path` if ChromeDriver is not on PATH.
4. Run and observe `View Results Tree` and browser actions.

Note:
- WebDriver tests are heavy; use small thread counts for browser realism.
- Keep high-scale load in HTTP sampler plans.
