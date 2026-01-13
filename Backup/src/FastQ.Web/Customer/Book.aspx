<%@ Page Title="Customer Booking" Language="C#" MasterPageFile="~/Site.master" AutoEventWireup="true" CodeBehind="Book.aspx.cs" Inherits="FastQ.Web.Customer.Book" %>
<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">
    <div class="card">
        <div class="eyebrow">Customer</div>
        <h2 class="page-title">Book a new appointment</h2>
        <p class="lead">Keep the provider console open in another tab to see the queue update immediately.</p>
    </div>

    <div class="card">
        <div class="card-header">
            <h3 class="card-title">Booking details</h3>
            <span class="badge accent">Live</span>
        </div>
        <div class="form-grid">
            <div class="field">
                <label for="queueId">Queue</label>
                <select id="queueId">
                    <option value="0153158e-0000-0000-4641-535451494430">General Queue</option>
                    <option value="01fca055-0000-0000-4641-535451494430">Secondary Queue</option>
                </select>
            </div>
            <div class="field">
                <label for="phone">Phone *</label>
                <input type="text" id="phone" placeholder="+8801..." />
            </div>
            <div class="field">
                <label for="name">Name</label>
                <input type="text" id="name" placeholder="Optional" />
            </div>
            <div class="field">
                <label for="smsOptIn">SMS Opt-in</label>
                <div class="pill">
                    <input type="checkbox" id="smsOptIn" />
                    <span>Send SMS updates</span>
                </div>
            </div>
        </div>

        <div class="row" style="margin-top:16px;">
            <button type="button" class="btn primary" onclick="FastQBook.submit()">Book First Available Slot</button>
            <span id="msg" class="muted"></span>
        </div>
    </div>

    <div class="card note">
        <h3 class="card-title">What happens live?</h3>
        <ul class="muted">
            <li>Booking triggers <code>QueueChanged</code> and <code>AppointmentChanged</code>.</li>
            <li>Provider screen refreshes the queue snapshot automatically.</li>
            <li>Customer is redirected to a live <b>Status</b> page for the appointment.</li>
        </ul>
    </div>
</asp:Content>

<asp:Content ID="Content2" ContentPlaceHolderID="ScriptsContent" runat="server">
<script>
window.FASTQ_CONTEXT = { };
var FastQBook = {
  submit: function() {
    var locationId = "00a98ac7-0000-0000-4641-535451494430";
    var queueId = document.getElementById("queueId").value;
    var phone = document.getElementById("phone").value;
    var name = document.getElementById("name").value;
    var smsOptIn = document.getElementById("smsOptIn").checked;

    $("#msg").removeClass("error ok").text("Booking...");

    $.ajax({
      url: "/Api/Book.ashx",
      method: "POST",
      data: { locationId: locationId, queueId: queueId, phone: phone, name: name, smsOptIn: smsOptIn },
      dataType: "json"
    }).done(function(res){
      if (!res || !res.ok) {
        $("#msg").addClass("error").text(res && res.error ? res.error : "Booking failed.");
        return;
      }
      $("#msg").addClass("ok").text("Booked! Redirecting to live status...");
      window.location.href = "/Customer/Status.aspx?appointmentId=" + encodeURIComponent(res.appointmentId);
    }).fail(function(xhr){
      $("#msg").addClass("error").text("Booking failed. " + (xhr.responseText || ""));
    });
  }
};
</script>
</asp:Content>

