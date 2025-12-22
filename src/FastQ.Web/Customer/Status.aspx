<%@ Page Title="Customer Status" Language="C#" MasterPageFile="~/Site.master" AutoEventWireup="true" CodeBehind="Status.aspx.cs" Inherits="FastQ.Web.Customer.Status" %>
<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">
    <div class="card">
        <div class="eyebrow">Customer</div>
        <h2 class="page-title">Live appointment status</h2>
        <p class="lead">Stay synced while the provider moves you through the queue.</p>
    </div>

    <div class="card">
        <div class="card-header">
            <div>
                <div class="eyebrow">Appointment</div>
                <div class="stat-value"><code id="apptId"></code></div>
            </div>
            <span class="badge accent" id="statusText">-</span>
        </div>

        <div class="grid-3">
            <div class="stat">
                <div class="stat-label">Queue</div>
                <div class="stat-value" id="queueName">-</div>
            </div>
            <div class="stat">
                <div class="stat-label">Scheduled (UTC)</div>
                <div class="stat-value" id="scheduledUtc">-</div>
            </div>
            <div class="stat">
                <div class="stat-label">Updated (UTC)</div>
                <div class="stat-value" id="updatedUtc">-</div>
            </div>
            <div class="stat">
                <div class="stat-label">Queue position</div>
                <div class="stat-value"><span id="pos">-</span> / <span id="waitingCount">-</span></div>
            </div>
        </div>

        <div class="row" style="margin-top:16px;">
            <button type="button" class="btn primary" onclick="FastQStatus.refresh()">Refresh</button>
            <button type="button" class="btn ghost" onclick="FastQStatus.cancel()">Cancel</button>
            <a class="btn ghost" href="/Customer/Home.aspx">Back to Home</a>
            <span id="msg" class="muted"></span>
        </div>
    </div>

    <div class="card">
        <div class="card-header">
            <h3 class="card-title">Status timeline</h3>
            <p class="card-subtitle">Scheduled → Check-in → Start → Transfer → Remove</p>
        </div>
        <div id="statusTimeline" class="stepper"></div>
    </div>

    <div class="card note">
        <h3 class="card-title">Live updates</h3>
        <p class="muted">When the provider marks you arrived, begins service, ends service, or transfers you, this page updates instantly.</p>
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
        $("#statusText").text(FastQStatus.label(d.Status));
        $("#queueName").text(d.QueueName || d.QueueId);
        $("#scheduledUtc").text(d.ScheduledForUtc);
        $("#updatedUtc").text(d.UpdatedUtc);
        $("#waitingCount").text(d.WaitingCount);
        $("#pos").text(d.PositionInQueue ? d.PositionInQueue : "-");
        FastQStatus.renderTimeline(d.Status);

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
  },

  label: function(status) {
    return status || "-";
  },

  renderTimeline: function(status) {
    var steps = [
      { key: "Scheduled", name: "Scheduled" },
      { key: "Arrived", name: "Check-in" },
      { key: "InService", name: "Start" },
      { key: "TransferredOut", name: "Transfer" },
      { key: "Cancelled", name: "Remove" },
      { key: "Completed", name: "Done" }
    ];

    var activeIndex = steps.findIndex(function(s){ return s.key.toLowerCase() === (status || "").toLowerCase(); });
    if (activeIndex === -1) activeIndex = 0;

    var html = steps.map(function(s, idx){
      var cls = "step";
      if (idx === activeIndex) cls += " active";
      if (idx < activeIndex) cls += " done";
      return '<div class="' + cls + '"><div class="label">' + s.key + '</div><div class="name">' + s.name + '</div></div>';
    }).join("");
    $("#statusTimeline").html(html);
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
