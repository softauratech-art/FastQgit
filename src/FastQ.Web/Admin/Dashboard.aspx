<%@ Page Title="Admin" Language="C#" MasterPageFile="~/Site.master" AutoEventWireup="true" CodeBehind="Dashboard.aspx.cs" Inherits="FastQ.Web.Admin.Dashboard" %>
<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">
    <div class="card">
        <div class="eyebrow">Admin</div>
        <h2 class="page-title">Queue configuration and system controls</h2>
        <p class="lead">Manage queue policies and monitor provider roster. Changes are stored in-memory for now.</p>
    </div>

    <div class="grid-2">
        <div class="card">
            <div class="card-header">
                <h3 class="card-title">Queue configuration</h3>
                <span class="badge accent">In-memory</span>
            </div>
            <div class="stack">
                <div class="field">
                    <label for="adminQueue">Queue</label>
                    <select id="adminQueue"></select>
                </div>
                <div class="grid-3">
                    <div class="field">
                        <label for="maxUpcoming">Max upcoming</label>
                        <input type="text" id="maxUpcoming" />
                    </div>
                    <div class="field">
                        <label for="maxDaysAhead">Max days ahead</label>
                        <input type="text" id="maxDaysAhead" />
                    </div>
                    <div class="field">
                        <label for="minHoursLead">Min hours lead</label>
                        <input type="text" id="minHoursLead" />
                    </div>
                </div>
                <div class="row">
                    <button type="button" class="btn primary" onclick="FastQAdmin.saveQueue()">Save Changes</button>
                    <span id="adminMsg" class="muted"></span>
                </div>
            </div>
        </div>

        <div class="card">
            <div class="card-header">
                <h3 class="card-title">System actions</h3>
                <span class="pill">Maintenance</span>
            </div>
            <p class="muted">Run administrative actions against the live in-memory store.</p>
            <div class="row">
                <button type="button" class="btn ghost" onclick="FastQAdmin.systemClose()">System Close Stale (12h)</button>
                <span id="systemMsg" class="muted"></span>
            </div>
        </div>
    </div>

    <div class="card">
        <div class="card-header">
            <h3 class="card-title">Provider roster</h3>
            <span class="pill">Location</span>
        </div>
        <div id="adminProviders"></div>
    </div>
</asp:Content>

<asp:Content ID="Content2" ContentPlaceHolderID="ScriptsContent" runat="server">
<script>
window.FASTQ_CONTEXT = {
  locationId: "00a98ac7-0000-0000-4641-535451494430"
};

var FastQAdmin = {
  data: null,

  load: function() {
    $("#adminMsg").removeClass("error ok").text("Loading...");
    PageMethods.AdminSnapshot(window.FASTQ_CONTEXT.locationId, function(res){
      if (!res || !res.ok) {
        $("#adminMsg").addClass("error").text(res && res.error ? res.error : "Load failed");
        return;
      }
      $("#adminMsg").addClass("ok").text("Ready.");
      FastQAdmin.data = res.data;
      FastQAdmin.render();
    }, function(){
      $("#adminMsg").addClass("error").text("Load failed.");
    });
  },

  render: function() {
    var queues = (FastQAdmin.data && FastQAdmin.data.Queues) || [];
    var providers = (FastQAdmin.data && FastQAdmin.data.Providers) || [];
    var select = document.getElementById("adminQueue");
    if (select.options.length === 0) {
      queues.forEach(function(q) {
        select.appendChild(new Option(q.QueueName, q.QueueId));
      });
    }
    FastQAdmin.applyQueue(select.value || (queues[0] && queues[0].QueueId));

    if (!providers.length) {
      $("#adminProviders").html("<div class='muted'>No providers found.</div>");
      return;
    }
    var html = "<table class='table'><thead><tr>" +
      "<th>Provider</th><th>Location</th><th>Provider Id</th>" +
      "</tr></thead><tbody>";
    providers.forEach(function(p){
      html += "<tr>" +
        "<td>" + p.ProviderName + "</td>" +
        "<td>" + p.LocationName + "</td>" +
        "<td><code>" + p.ProviderId + "</code></td>" +
        "</tr>";
    });
    html += "</tbody></table>";
    $("#adminProviders").html(html);
  },

  applyQueue: function(queueId) {
    var queues = (FastQAdmin.data && FastQAdmin.data.Queues) || [];
    var q = queues.filter(function(x){ return x.QueueId === queueId; })[0];
    if (!q) return;
    $("#maxUpcoming").val(q.MaxUpcomingAppointments);
    $("#maxDaysAhead").val(q.MaxDaysAhead);
    $("#minHoursLead").val(q.MinHoursLead);
  },

  saveQueue: function() {
    var queueId = $("#adminQueue").val();
    var payload = {
      queueId: queueId,
      maxUpcoming: $("#maxUpcoming").val(),
      maxDaysAhead: $("#maxDaysAhead").val(),
      minHoursLead: $("#minHoursLead").val()
    };

    $("#adminMsg").removeClass("error ok").text("Saving...");
    PageMethods.AdminUpdate(payload.queueId, payload.maxUpcoming, payload.maxDaysAhead, payload.minHoursLead, function(res){
      if (!res || !res.ok) {
        $("#adminMsg").addClass("error").text(res && res.error ? res.error : "Save failed");
        return;
      }
      $("#adminMsg").addClass("ok").text("Saved.");
      FastQAdmin.load();
    }, function(){
      $("#adminMsg").addClass("error").text("Save failed.");
    });
  },

  systemClose: function() {
    $("#systemMsg").removeClass("error ok").text("Closing stale...");
    PageMethods.SystemClose(12, function(res){
      if (res && res.ok) {
        $("#systemMsg").addClass("ok").text("Closed stale: " + res.closed);
      } else {
        $("#systemMsg").addClass("error").text("System close failed.");
      }
    }, function(){
      $("#systemMsg").addClass("error").text("System close failed.");
    });
  }
};

$(function(){
  FastQAdmin.load();
  $("#adminQueue").on("change", function(){ FastQAdmin.applyQueue(this.value); });
});

window.onFastQQueueUpdated = function() { FastQAdmin.load(); };
window.onFastQAppointmentUpdated = function() { FastQAdmin.load(); };
</script>
</asp:Content>

