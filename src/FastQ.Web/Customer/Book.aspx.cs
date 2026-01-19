using System;
using FastQ.Web.App_Start;

namespace FastQ.Web.Customer
{
    public partial class Book : System.Web.UI.Page
    {
        private static readonly Guid DefaultLocationId = new Guid("00a98ac7-0000-0000-4641-535451494430");

        protected void Page_Load(object sender, EventArgs e) { }

        protected void CreateAppointment_Click(object sender, EventArgs e)
        {
            var queueIdRaw = Request.Form["queueId"];
            var phone = (Request.Form["phone"] ?? string.Empty).Trim();
            var firstName = (Request.Form["firstName"] ?? string.Empty).Trim();
            var lastName = (Request.Form["lastName"] ?? string.Empty).Trim();
            var name = $"{firstName} {lastName}".Trim();

            if (!Guid.TryParse(queueIdRaw, out var queueId))
            {
                msg.Attributes["class"] = "muted error";
                msg.InnerText = "Queue is required.";
                return;
            }

            if (string.IsNullOrWhiteSpace(phone) || string.IsNullOrWhiteSpace(firstName) || string.IsNullOrWhiteSpace(lastName))
            {
                msg.Attributes["class"] = "muted error";
                msg.InnerText = "First name, last name, and mobile number are required.";
                return;
            }

            var res = CompositionRoot.Booking.BookFirstAvailable(DefaultLocationId, queueId, phone, true, name);
            if (!res.Ok)
            {
                msg.Attributes["class"] = "muted error";
                msg.InnerText = res.Error;
                return;
            }

            msg.Attributes["class"] = "muted ok";
            msg.InnerText = "Booked! Redirecting to My Appointments...";
            Response.Redirect($"/Customer/Home.aspx?appointmentId={Uri.EscapeDataString(res.Value.Id.ToString())}", true);
        }
    }
}

