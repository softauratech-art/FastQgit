<%@ Page Title="Provider Today" Language="C#" MasterPageFile="~/Site.master" AutoEventWireup="true" CodeBehind="Today.aspx.cs" Inherits="FastQ.Web.Provider.Today" %>
<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">
    <div class="card">
        <div class="eyebrow">Provider</div>
        <h2 class="page-title">Today's live queue</h2>
        <p class="lead">Manage arrivals, service, and transfers while customers see changes instantly.</p>
    </div>

    <div class="card">
        <div class="card-header">
            <h3 class="card-title">Queue controls</h3>
            <span class="pill">Location <code>00a98ac7-0000-0000-4641-535451494430</code></span>
        </div>
        <div class="row">
            <div class="col">
                <label for="queueId">Queue</label>
                <select id="queueId" onchange="FastQProvider.changeQueue()">
                    <option value="0153158e-0000-0000-4641-535451494430">General Queue</option>
                    <option value="01fca055-0000-0000-4641-535451494430">Secondary Queue</option>
                </select>
            </div>
            <div class="col" style="display:flex; gap:10px; align-items:flex-end; flex-wrap:wrap;">
                <button type="button" class="btn primary" onclick="FastQProvider.refresh()">Refresh</button>
                <button type="button" class="btn ghost" onclick="FastQProvider.systemClose()">System Close Stale</button>
                <span id="msg" class="muted"></span>
            </div>
        </div>
    </div>

    <div class="report-grid">
        <div class="stat">
            <div class="stat-label">Waiting</div>
            <div class="stat-value" id="waitingCount">0</div>
        </div>
        <div class="stat">
            <div class="stat-label">In Service</div>
            <div class="stat-value" id="inServiceCount">0</div>
        </div>
        <div class="stat">
            <div class="stat-label">Recent Done</div>
            <div class="stat-value" id="doneCount">0</div>
        </div>
        <div class="stat">
            <div class="stat-label">Live Queue</div>
            <div class="stat-value">Synced via SignalR</div>
        </div>
    </div>

    <div class="card">
        <div class="card-header">
            <h3 class="card-title">Today's schedule (UTC)</h3>
            <span class="badge accent">Calendar View</span>
        </div>
        <div id="calendarView" class="calendar"></div>
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

    FastQProvider.renderCalendar(d);
  },

  renderCalendar: function(d) {
    var all = [];
    function addRows(rows, mode) {
      if (!rows) return;
      rows.forEach(function(r) {
        all.push({
          Mode: mode,
          AppointmentId: r.AppointmentId,
          CustomerPhone: r.CustomerPhone,
          Status: r.Status,
          ScheduledForUtc: r.ScheduledForUtc,
          UpdatedUtc: r.UpdatedUtc
        });
      });
    }

    addRows(d.Waiting, "waiting");
    addRows(d.InService, "inservice");
    addRows(d.Done, "done");

    if (all.length === 0) {
      $("#calendarView").html("<div class='muted'>No appointments scheduled yet.</div>");
      return;
    }

    all.sort(function(a, b) {
      return new Date(a.ScheduledForUtc) - new Date(b.ScheduledForUtc);
    });

    var hours = all.map(function(r) { return FastQProvider.getHour(r.ScheduledForUtc); }).filter(function(v) { return v !== null; });
    var minHour = hours.length ? Math.min.apply(null, hours) : 8;
    var maxHour = hours.length ? Math.max.apply(null, hours) : 18;
    minHour = Math.max(0, minHour - 1);
    maxHour = Math.min(23, maxHour + 1);

    var html = "";
    for (var h = minHour; h <= maxHour; h++) {
      var label = FastQProvider.formatHour(h);
      var slotRows = all.filter(function(r) { return FastQProvider.getHour(r.ScheduledForUtc) === h; });
      html += "<div class='time-row'>" +
        "<div class='time-label'>" + label + "</div>" +
        "<div class='slot-stack'>" + (slotRows.length ? slotRows.map(FastQProvider.renderSlot).join("") : "<div class='muted'>No bookings</div>") + "</div>" +
        "</div>";
    }

    $("#calendarView").html(html);
  },

  renderSlot: function(r) {
    var statusClass = FastQProvider.statusClass(r.Status, r.Mode);
    var actions = FastQProvider.renderActions(r);
    var scheduled = r.ScheduledForUtc || "-";
    var updated = r.UpdatedUtc || "-";

    return "<div class='slot-card'>" +
      "<div class='slot-header'>" +
      "<div class='slot-title'>" + (r.CustomerPhone || "Walk-in") + "</div>" +
      "<div class='slot-meta'>" +
      "<span class='status-tag " + statusClass + "'>" + r.Status + "</span>" +
      "<span class='muted'>Scheduled: " + scheduled + "</span>" +
      "<span class='muted'>Updated: " + updated + "</span>" +
      "</div>" +
      "</div>" +
      "<div class='slot-actions'>" + actions + "</div>" +
      "</div>";
  },

  renderActions: function(r) {
    var apptId = r.AppointmentId;
    var actions = "";

    if (r.Mode === "waiting") {
      actions += "<button class='btn small' onclick=\"FastQProvider.act('arrive','" + apptId + "')\">Arrive</button> ";
      actions += "<button class='btn small' onclick=\"FastQProvider.act('begin','" + apptId + "')\">Begin</button> ";
      actions += "<button class='btn small ghost' onclick=\"FastQProvider.transfer('" + apptId + "')\">Transfer</button> ";
    }
    if (r.Mode === "inservice") {
      actions += "<button class='btn small' onclick=\"FastQProvider.act('end','" + apptId + "')\">End</button> ";
      actions += "<button class='btn small ghost' onclick=\"FastQProvider.transfer('" + apptId + "')\">Transfer</button> ";
    }

    actions += "<a class='btn small ghost' href='/Customer/Status.aspx?appointmentId=" + apptId + "' target='_blank'>Status</a>";
    return actions;
  },

  statusClass: function(status, mode) {
    var s = (status || "").toLowerCase();
    if (s.indexOf("cancel") >= 0 || s.indexOf("closed") >= 0) return "cancelled";
    if (mode === "inservice" || s.indexOf("service") >= 0) return "inservice";
    if (mode === "done" || s.indexOf("completed") >= 0 || s.indexOf("transferred") >= 0) return "done";
    return "waiting";
  },

  getHour: function(dt) {
    var d = new Date(dt);
    if (isNaN(d.getTime())) return null;
    return d.getUTCHours();
  },

  formatHour: function(h) {
    var label = (h < 10 ? "0" + h : h) + ":00 UTC";
    return label;
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

