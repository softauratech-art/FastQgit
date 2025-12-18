<%@ Page Title="Customer Booking" Language="C#" MasterPageFile="~/Site.master" AutoEventWireup="true" CodeBehind="Book.aspx.cs" Inherits="FastQ.Web.Customer.Book" %>
<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">
    <h2>Customer: Book (Live)</h2>

    <div class="card">
        <p class="muted">Open <b>Provider: Today</b> in another tab. When you book, the provider queue updates immediately.</p>
        <div class="row">
            <div class="col">
                <label>Queue</label><br />
                <select id="queueId">
                    <option value="22222222-2222-2222-2222-222222222222">General Queue</option>
                    <option value="33333333-3333-3333-3333-333333333333">Secondary Queue</option>
                </select>
            </div>
            <div class="col">
                <label>Phone *</label><br />
                <input type="text" id="phone" placeholder="+8801..." />
            </div>
            <div class="col">
                <label>Name</label><br />
                <input type="text" id="name" placeholder="Optional" />
            </div>
            <div class="col">
                <label><input type="checkbox" id="smsOptIn" /> SMS Opt-in</label>
            </div>
        </div>

        <div style="margin-top:12px;">
            <button type="button" class="btn" onclick="FastQBook.submit()">Book First Available Slot</button>
            <span id="msg" class="muted"></span>
        </div>
    </div>

    <div class="card">
        <h3>What happens live?</h3>
        <ul>
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
    var locationId = "11111111-1111-1111-1111-111111111111";
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
