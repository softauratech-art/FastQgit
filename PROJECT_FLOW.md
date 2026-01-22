App Start to Provider Today Rendering Flow

1) App boot
   - OWIN startup config sets up MVC and calls CompositionRoot.Initialize().
   - See: src/FastQ.Web/Startup.cs and src/FastQ.Web/App_Start/CompositionRoot.cs.

2) Dependency wiring (Oracle only)
   - CompositionRoot.Initialize() loads the FastQOracle connection string.
   - It constructs Oracle repositories:
     - OracleAppointmentRepository
     - OracleCustomerRepository
     - OracleQueueRepository
     - OracleLocationRepository
     - OracleProviderRepository
   - These are stored on CompositionRoot.* for later use.

3) Request routing
   - Browser requests GET /Provider/Today.
   - MVC routes map to ProviderController.Today().
   - See: src/FastQ.Web/Controllers/ProviderController.cs.

4) Controller data fetch
   - ProviderController.Today() loads all queues and customers via CompositionRoot.
   - It loads all appointments, filters to today in UTC:
     - a.ScheduledForUtc.Date == DateTime.UtcNow.Date
   - It builds AdminAppointmentRow entries (queue name, customer name/phone, status, etc.).
   - It splits rows into:
     - LiveQueue (Arrived/InService)
     - Scheduled (everything else for today)
   - It returns ProviderTodayViewModel to the view.

5) Data layer (Oracle)
   - CompositionRoot.Appointments.ListAll():
     - OracleAppointmentRepository.ListByFilter() executes:
       SELECT a.*, q.LOCATION_ID FROM APPOINTMENTS a JOIN VALIDQUEUES q ON q.QUEUE_ID = a.QUEUE_ID
   - CompositionRoot.Customers.ListAll():
     - OracleCustomerRepository.ListAll() executes:
       SELECT CUSTOMER_ID, FNAME, LNAME, EMAIL, PHONE, SMS_OPTIN, ACTIVEFLAG, STAMPDATE, STAMPUSER FROM CUSTOMERS
   - CompositionRoot.Queues.ListAll():
     - OracleQueueRepository.ListAll() executes:
       SELECT * FROM VALIDQUEUES (plus config parsing in repository)
   - Repositories map DB columns into entity objects (Appointment, Customer, Queue).

6) View rendering
   - MVC renders src/FastQ.Web/Views/Provider/Today.cshtml.
   - The view iterates LiveQueue and Scheduled lists from ProviderTodayViewModel.
   - It writes the table rows, action buttons, and page header.
