using System;
using System.Web.Script.Services;
using System.Web.Services;
using FastQ.Web.App_Start;

namespace FastQ.Web.Customer
{
    public partial class Status : System.Web.UI.Page
    {
        protected string AppointmentId { get; private set; }

        protected void Page_Load(object sender, EventArgs e)
        {
            AppointmentId = (Request.QueryString["appointmentId"] ?? string.Empty).Trim();
        }

        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public static object GetAppointmentSnapshot(string appointmentId)
        {
            if (!Guid.TryParse(appointmentId, out var apptId))
                return new { ok = false, error = "appointmentId is required" };

            var dto = CompositionRoot.Queries.GetAppointmentSnapshot(apptId);
            if (dto == null)
                return new { ok = false, error = "appointment not found" };

            return new { ok = true, data = dto };
        }

        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public static object CancelAppointment(string appointmentId)
        {
            if (!Guid.TryParse(appointmentId, out var apptId))
                return new { ok = false, error = "appointmentId is required" };

            var res = CompositionRoot.Booking.Cancel(apptId);
            if (!res.Ok)
                return new { ok = false, error = res.Error };

            return new { ok = true };
        }
    }
}

