<%@ Page Title="Customer Status" Language="C#" MasterPageFile="~/Site.master" AutoEventWireup="true" CodeBehind="Status.aspx.cs" Inherits="FastQ.Web.Customer.Status" %>
<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">
    <h2>Customer: Live Status</h2>

    <div class="card">
        <div><span class="badge">Appointment</span> <code id="apptId"></code></div>
        <p>
            Status: <b id="statusText">-</b><br />
            Scheduled (UTC): <span id="scheduledUtc">-</span><br />
            Updated (UTC): <span id="updatedUtc">-</span>
        </p>
        <p>
            Position in queue: <b id="pos">-</b> / Waiting: <b id="waitingCount">-</b>
        </p>
        <div style="margin-top:10px;">
            <button type="button" class="btn" onclick="FastQStatus.refresh()">Refresh</button>
            <button type="button" class="btn" onclick="FastQStatus.cancel()">Cancel</button>
            <span id="msg" class="muted"></span>
        </div>
    </div>

    <div class="card">
        <h3>Live updates</h3>
        <p class="muted">When the provider marks you arrived / begins service / ends service / transfers you, this page updates instantly.</p>
    </div>
</asp:Content>

<asp:Content ID="Content2" ContentPlaceHolderID="ScriptsContent" runat="server">
<script>
window.FASTQ_CONTEXT = {
  appointmentId: "<%= AppointmentId %>"
};

var FastQStatus = {
  refresh: function() {
    var apptId = window.FASTQ_CONTEXT.appointmentId;
    $("#msg").removeClass("error ok").text("Loading...");
    $.getJSON("/Api/AppointmentSnapshot.ashx", { appointmentId: apptId })
      .done(function(res){
        if(!res || !res.ok) {
          $("#msg").addClass("error").text(res && res.error ? res.error : "Not found");
          return;
        }
        var d = res.data;
        $("#apptId").text(d.AppointmentId);
        $("#statusText").text(d.Status);
        $("#scheduledUtc").text(d.ScheduledForUtc);
        $("#updatedUtc").text(d.UpdatedUtc);
        $("#waitingCount").text(d.WaitingCount);
        $("#pos").text(d.PositionInQueue ? d.PositionInQueue : "-");

        // Update context so fastq.live.js joins location/queue too (for more events)
        window.FASTQ_CONTEXT.locationId = d.LocationId;
        window.FASTQ_CONTEXT.queueId = d.QueueId;

        $("#msg").addClass("ok").text("Up to date.");
      })
      .fail(function(xhr){
        $("#msg").addClass("error").text("Load failed.");
      });
  },

  cancel: function() {
    var apptId = window.FASTQ_CONTEXT.appointmentId;
    $("#msg").removeClass("error ok").text("Cancelling...");
    $.ajax({ url: "/Api/Cancel.ashx", method: "POST", data: { appointmentId: apptId }, dataType: "json" })
      .done(function(res){
        if(!res || !res.ok) {
          $("#msg").addClass("error").text(res && res.error ? res.error : "Cancel failed");
          return;
        }
        $("#msg").addClass("ok").text("Cancelled.");
        FastQStatus.refresh();
      })
      .fail(function(){ $("#msg").addClass("error").text("Cancel failed."); });
  }
};

// initial load
$(function(){ FastQStatus.refresh(); });

// hooks for live updates (called from fastq.live.js)
window.onFastQAppointmentUpdated = function(appointmentId, status) {
  if (appointmentId === window.FASTQ_CONTEXT.appointmentId) {
    FastQLive.toast("Your appointment updated: " + status);
    FastQStatus.refresh();
  }
};
</script>
</asp:Content>
