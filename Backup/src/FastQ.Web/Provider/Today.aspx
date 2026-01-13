<%@ Page Title="Provider Today" Language="C#" MasterPageFile="~/Site.master" AutoEventWireup="true" CodeBehind="Today.aspx.cs" Inherits="FastQ.Web.Provider.Today" %>
<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">
    <h2>Provider: Today (Live)</h2>

    <div class="card">
        <div class="row">
            <div class="col">
                <label>Queue</label><br />
                <select id="queueId" onchange="FastQProvider.changeQueue()">
                    <option value="0153158e-0000-0000-4641-535451494430">General Queue</option>
                    <option value="01fca055-0000-0000-4641-535451494430">Secondary Queue</option>
                </select>
                <div class="muted">LocationId: <code>00a98ac7-0000-0000-4641-535451494430</code></div>
            </div>
            <div class="col">
                <button type="button" class="btn" onclick="FastQProvider.refresh()">Refresh</button>
                <button type="button" class="btn" onclick="FastQProvider.systemClose()">System Close Stale</button>
                <span id="msg" class="muted"></span>
            </div>
        </div>
    </div>

    <div class="row">
        <div class="col card">
            <h3>Waiting <span class="badge" id="waitingCount">0</span></h3>
            <div id="waitingTable"></div>
        </div>

        <div class="col card">
            <h3>In Service <span class="badge" id="inServiceCount">0</span></h3>
            <div id="inServiceTable"></div>
        </div>
    </div>

    <div class="card">
        <h3>Recent Done <span class="badge" id="doneCount">0</span></h3>
        <div id="doneTable"></div>
    </div>
</asp:Content>

<asp:Content ID="Content2" ContentPlaceHolderID="ScriptsContent" runat="server">
<script>
window.FASTQ_CONTEXT = {
  locationId: "00a98ac7-0000-0000-4641-535451494430",
  queueId: document.getElementById("queueId") ? document.getElementById("queueId").value : "0153158e-0000-0000-4641-535451494430",
  providerId: "02a62b1c-0000-0000-4641-535451494430"
};

var FastQProvider = {
  changeQueue: function() {
    window.FASTQ_CONTEXT.queueId = document.getElementById("queueId").value;
    FastQLive.toast("Switched queue");
    FastQProvider.refresh();
    // join queue group too
    FastQLive.tryJoinGroups();
  },

  refresh: function() {
    var ctx = window.FASTQ_CONTEXT;
    $("#msg").removeClass("error ok").text("Loading...");
    $.getJSON("/Api/QueueSnapshot.ashx", { locationId: ctx.locationId, queueId: ctx.queueId })
      .done(function(res){
        if(!res || !res.ok) {
          $("#msg").addClass("error").text(res && res.error ? res.error : "Load failed");
          return;
        }
        $("#msg").addClass("ok").text("Up to date.");
        FastQProvider.render(res.data);
      })
      .fail(function(){ $("#msg").addClass("error").text("Load failed."); });
  },

  render: function(d) {
    $("#waitingCount").text(d.WaitingCount);
    $("#inServiceCount").text(d.InServiceCount);
    $("#doneCount").text(d.CompletedCount);

    $("#waitingTable").html(FastQProvider.renderTable(d.Waiting, "waiting"));
    $("#inServiceTable").html(FastQProvider.renderTable(d.InService, "inservice"));
    $("#doneTable").html(FastQProvider.renderTable(d.Done, "done"));
  },

  renderTable: function(rows, mode) {
    if(!rows || rows.length === 0) return "<div class='muted'>No rows</div>";
    var html = "<table><thead><tr>" +
      "<th>Customer</th><th>Status</th><th>Scheduled (UTC)</th><th>Updated</th><th>Actions</th>" +
      "</tr></thead><tbody>";

    rows.forEach(function(r){
      var actions = "";
      var apptId = r.AppointmentId;

      if(mode === "waiting") {
        actions += "<button class='btn' onclick=\"FastQProvider.act('arrive','" + apptId + "')\">Arrive</button> ";
        actions += "<button class='btn' onclick=\"FastQProvider.act('begin','" + apptId + "')\">Begin</button> ";
        actions += "<button class='btn' onclick=\"FastQProvider.transfer('" + apptId + "')\">Transfer</button> ";
      }
      if(mode === "inservice") {
        actions += "<button class='btn' onclick=\"FastQProvider.act('end','" + apptId + "')\">End</button> ";
        actions += "<button class='btn' onclick=\"FastQProvider.transfer('" + apptId + "')\">Transfer</button> ";
      }
      if(mode === "done") {
        actions += "<a class='btn' href='/Customer/Status.aspx?appointmentId=" + apptId + "' target='_blank'>Open Status</a>";
      }

      // allow open status for any row
      actions += " <a class='btn' href='/Customer/Status.aspx?appointmentId=" + apptId + "' target='_blank'>Status</a>";

      html += "<tr>" +
        "<td>" + (r.CustomerPhone || "") + "</td>" +
        "<td>" + r.Status + "</td>" +
        "<td>" + r.ScheduledForUtc + "</td>" +
        "<td>" + r.UpdatedUtc + "</td>" +
        "<td>" + actions + "</td>" +
        "</tr>";
    });

    html += "</tbody></table>";
    return html;
  },

  act: function(action, appointmentId) {
    var ctx = window.FASTQ_CONTEXT;
    FastQLive.toast("Action: " + action);
    $.ajax({
      url: "/Api/ProviderAction.ashx",
      method: "POST",
      data: { action: action, appointmentId: appointmentId, providerId: ctx.providerId },
      dataType: "json"
    }).done(function(res){
      if(!res || !res.ok) {
        FastQLive.toast("Failed: " + (res && res.error ? res.error : "unknown"));
        return;
      }
      // push will refresh; keep a local refresh as fallback
      setTimeout(FastQProvider.refresh, 200);
    });
  },

  transfer: function(appointmentId) {
    var ctx = window.FASTQ_CONTEXT;
    var target = (ctx.queueId === "0153158e-0000-0000-4641-535451494430")
      ? "01fca055-0000-0000-4641-535451494430"
      : "0153158e-0000-0000-4641-535451494430";

    FastQLive.toast("Transfer to other queue");
    $.ajax({
      url: "/Api/Transfer.ashx",
      method: "POST",
      data: { appointmentId: appointmentId, targetQueueId: target },
      dataType: "json"
    }).done(function(res){
      if(!res || !res.ok) {
        FastQLive.toast("Transfer failed: " + (res && res.error ? res.error : "unknown"));
        return;
      }
      setTimeout(FastQProvider.refresh, 200);
    });
  },

  systemClose: function() {
    FastQLive.toast("System close stale...");
    $.getJSON("/Api/SystemClose.ashx", { staleHours: 12 }).done(function(res){
      if(res && res.ok) {
        FastQLive.toast("Closed stale: " + res.closed);
        setTimeout(FastQProvider.refresh, 200);
      }
    });
  }
};

// initial refresh
$(function(){
  window.FASTQ_CONTEXT.queueId = $("#queueId").val();
  FastQProvider.refresh();
});

// hooks for live updates (called from fastq.live.js)
window.onFastQQueueUpdated = function(locationId, queueId) {
  if (locationId === window.FASTQ_CONTEXT.locationId) {
    // if the update is for the active queue or same location, refresh
    if (!queueId || queueId === window.FASTQ_CONTEXT.queueId) {
      FastQProvider.refresh();
    }
  }
};
</script>
</asp:Content>

