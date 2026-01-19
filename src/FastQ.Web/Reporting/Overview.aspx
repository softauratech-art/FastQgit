<%@ Page Title="Reporting" Language="C#" MasterPageFile="~/Site.master" AutoEventWireup="true" CodeBehind="Overview.aspx.cs" Inherits="FastQ.Web.Reporting.Overview" %>
<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">
    <div class="card">
        <div class="eyebrow">Reporting</div>
        <h2 class="page-title">Daily summary and provider activity</h2>
        <p class="lead">Live reporting powered by the in-memory store with instant SignalR refresh.</p>
    </div>

    <div class="card">
        <div class="card-header">
            <h3 class="card-title">Filters</h3>
            <span class="badge accent">Live</span>
        </div>
        <div class="row">
            <div class="col">
                <label for="reportQueue">Queue</label>
                <select id="reportQueue"></select>
            </div>
            <div class="col" style="display:flex; gap:10px; align-items:flex-end; flex-wrap:wrap;">
                <button type="button" class="btn primary" onclick="FastQReport.refresh()">Refresh</button>
                <span id="reportMsg" class="muted"></span>
            </div>
        </div>
    </div>

    <div class="report-grid">
        <div class="stat">
            <div class="stat-label">Booked Today</div>
            <div class="stat-value" id="reportBooked">0</div>
        </div>
        <div class="stat">
            <div class="stat-label">Scheduled Today</div>
            <div class="stat-value" id="reportScheduled">0</div>
        </div>
        <div class="stat">
            <div class="stat-label">Completed</div>
            <div class="stat-value" id="reportCompleted">0</div>
        </div>
        <div class="stat">
            <div class="stat-label">Cancellations</div>
            <div class="stat-value" id="reportCancelled">0</div>
        </div>
    </div>

    <div class="grid-2">
        <div class="card">
            <div class="card-header">
                <h3 class="card-title">Provider activity</h3>
                <span class="pill">Today</span>
            </div>
            <div id="providerTable"></div>
        </div>
        <div class="card">
            <div class="card-header">
                <h3 class="card-title">Queue breakdown</h3>
                <span class="pill">Live</span>
            </div>
            <div id="queueTable"></div>
        </div>
    </div>

    <div class="card">
        <div class="card-header">
            <h3 class="card-title">7-day booking trend</h3>
            <span class="pill">UTC</span>
        </div>
        <div id="trendChart" class="stack"></div>
    </div>
</asp:Content>

<asp:Content ID="Content2" ContentPlaceHolderID="ScriptsContent" runat="server">
<script>
window.FASTQ_CONTEXT = {
  locationId: "00a98ac7-0000-0000-4641-535451494430"
};

var FastQReport = {
  refresh: function() {
    var queueId = document.getElementById("reportQueue").value;
    $("#reportMsg").removeClass("error ok").text("Loading...");
    PageMethods.ReportingSnapshot(window.FASTQ_CONTEXT.locationId, queueId, function(res){
      if (!res || !res.ok) {
        $("#reportMsg").addClass("error").text(res && res.error ? res.error : "Load failed");
        return;
      }
      $("#reportMsg").addClass("ok").text("Up to date.");
      FastQReport.render(res.data);
    }, function(){
      $("#reportMsg").addClass("error").text("Load failed.");
    });
  },

  render: function(d) {
    $("#reportBooked").text(d.BookedToday);
    $("#reportScheduled").text(d.ScheduledToday);
    $("#reportCompleted").text(d.CompletedToday);
    $("#reportCancelled").text(d.CancelledToday);

    FastQReport.renderQueues(d.Queues || []);
    FastQReport.renderProviders(d.Providers || []);
    FastQReport.renderTrend(d.DailyTrend || []);

    var queueSelect = document.getElementById("reportQueue");
    if (queueSelect && queueSelect.options.length === 0) {
      queueSelect.appendChild(new Option("All queues", ""));
      (d.Queues || []).forEach(function(q) {
        queueSelect.appendChild(new Option(q.QueueName, q.QueueId));
      });
    }
  },

  renderQueues: function(rows) {
    if (!rows || rows.length === 0) {
      $("#queueTable").html("<div class='muted'>No queue data yet.</div>");
      return;
    }
    var html = "<table class='table'><thead><tr>" +
      "<th>Queue</th><th>Waiting</th><th>In Service</th><th>Completed</th><th>Cancelled</th>" +
      "</tr></thead><tbody>";
    rows.forEach(function(r){
      html += "<tr>" +
        "<td>" + r.QueueName + "</td>" +
        "<td>" + r.Waiting + "</td>" +
        "<td>" + r.InService + "</td>" +
        "<td>" + r.Completed + "</td>" +
        "<td>" + r.Cancelled + "</td>" +
        "</tr>";
    });
    html += "</tbody></table>";
    $("#queueTable").html(html);
  },

  renderProviders: function(rows) {
    if (!rows || rows.length === 0) {
      $("#providerTable").html("<div class='muted'>No provider activity yet.</div>");
      return;
    }
    var html = "<table class='table'><thead><tr>" +
      "<th>Provider</th><th>Arrived</th><th>In Service</th><th>Completed</th><th>Cancelled</th>" +
      "</tr></thead><tbody>";
    rows.forEach(function(r){
      html += "<tr>" +
        "<td>" + r.ProviderName + "</td>" +
        "<td>" + r.Arrived + "</td>" +
        "<td>" + r.InService + "</td>" +
        "<td>" + r.Completed + "</td>" +
        "<td>" + r.Cancelled + "</td>" +
        "</tr>";
    });
    html += "</tbody></table>";
    $("#providerTable").html(html);
  },

  renderTrend: function(rows) {
    if (!rows || rows.length === 0) {
      $("#trendChart").html("<div class='muted'>No trend data yet.</div>");
      return;
    }
    var max = Math.max.apply(null, rows.map(function(r){ return r.Booked; }).concat([1]));
    var html = "";
    rows.forEach(function(r){
      var pct = Math.round((r.Booked / max) * 100);
      html += "<div class='trend-row'>" +
        "<div class='muted'>" + r.Date + "</div>" +
        "<div class='bar-track'><div class='bar-fill' style='width:" + pct + "%'></div></div>" +
        "<div>" + r.Booked + "</div>" +
        "</div>";
    });
    $("#trendChart").html(html);
  }
};

$(function(){
  FastQReport.refresh();
});

window.onFastQQueueUpdated = function() { FastQReport.refresh(); };
window.onFastQAppointmentUpdated = function() { FastQReport.refresh(); };
</script>
</asp:Content>

