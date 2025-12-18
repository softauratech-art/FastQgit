using System;

namespace FastQ.Web.Customer
{
    public partial class Status : System.Web.UI.Page
    {
        protected string AppointmentId { get; private set; }

        protected void Page_Load(object sender, EventArgs e)
        {
            AppointmentId = (Request.QueryString["appointmentId"] ?? string.Empty).Trim();
        }
    }
}
