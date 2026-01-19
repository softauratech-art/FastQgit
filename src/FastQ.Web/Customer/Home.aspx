<%@ Page Title="Customer Home" Language="C#" MasterPageFile="~/Site.master" AutoEventWireup="true" CodeBehind="Home.aspx.cs" Inherits="FastQ.Web.Customer.Home" %>
<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">
    <div class="card">
        <div class="eyebrow">My Appointments</div>
        <h2 class="page-title">Manage and cancel in one place.</h2>
        <p class="lead">Upcoming, History, and Profile tabs. Cancel inline with confirmation.</p>
        <div class="row" style="margin-top:12px;">
            <a class="btn primary" href="/Customer/Book.aspx">Create Appointment</a>
            <button type="button" class="btn ghost" onclick="FastQHome.refreshAll()">Refresh all</button>
            <span id="homeMsg" class="muted"></span>
        </div>
    </div>

    <div class="card">
        <div class="card-header">
            <div>
                <div class="eyebrow">Tabs</div>
                <h3 class="card-title" id="homeProfileTitle">You</h3>
                <p class="card-subtitle" id="homeProfileSubtitle">Email: -, Contact: -</p>
            </div>
            <div class="tabs" id="homeTabs">
                <button type="button" data-tab="upcoming" class="active" onclick="FastQHome.switchTab('upcoming')">Upcoming</button>
                <button type="button" data-tab="history" onclick="FastQHome.switchTab('history')">History</button>
                <button type="button" data-tab="profile" onclick="FastQHome.switchTab('profile')">Profile</button>
            </div>
        </div>

        <div id="tab-upcoming" class="table-card">
            <table class="table">
                <thead>
                    <tr><th>Date &amp; Time</th><th>Queue</th><th>Type</th><th>Status</th><th>Cancel?</th></tr>
                </thead>
                <tbody id="upcomingTable"></tbody>
            </table>
        </div>
        <div id="tab-history" class="table-card" style="display:none;">
            <table class="table">
                <thead>
                    <tr><th>Date &amp; Time</th><th>Queue</th><th>Type</th><th>Status</th><th>Cancel?</th></tr>
                </thead>
                <tbody id="historyTable"></tbody>
            </table>
        </div>
        <div id="tab-profile" style="display:none;">
            <div class="grid-2">
                <div class="field">
                    <label for="addAppointmentId">Track an appointment</label>
                    <div class="row">
                        <input type="text" id="addAppointmentId" placeholder="Appointment ID" style="flex:1;" />
                        <button type="button" class="btn secondary" onclick="FastQHome.addFromInput()">Add</button>
                    </div>
                </div>
                <div class="field">
                    <label>Quick links</label>
                    <div class="row">
                        <a class="btn ghost" href="/Customer/Book.aspx">Book flow</a>
                        <a class="btn ghost" href="/Customer/Status.aspx">Status lookup</a>
                    </div>
                </div>
            </div>
        </div>
    </div>

    <div class="modal-backdrop" id="cancelModal">
        <div class="modal">
            <div class="card-header" style="padding:0; margin-bottom:8px;">
                <h3 class="card-title">Cancel Appointment</h3>
            </div>
            <div class="stack">
                <div class="stat-label" id="cancelSummary"></div>
                <div class="field">
                    <label for="cancelReason">Reason</label>
                    <select id="cancelReason">
                        <option value="">Select from list</option>
                        <option value="conflict">Schedule conflict</option>
                        <option value="no_longer_needed">No longer needed</option>
                        <option value="reschedule">Need to reschedule</option>
                        <option value="other">Other</option>
                    </select>
                </div>
                <div class="row">
                    <button type="button" class="btn primary" onclick="FastQHome.confirmCancel()">Yes</button>
                    <button type="button" class="btn ghost" onclick="FastQHome.closeModal()">No</button>
                </div>
                <div id="cancelMsg" class="muted"></div>
            </div>
        </div>
    </div>
</asp:Content>

<asp:Content ID="Content2" ContentPlaceHolderID="ScriptsContent" runat="server">
<script>
var FastQHome = {
  storageKey: "fastq_customer_appts",
  list: [],
  profile: { name: "Guest", phone: "", email: "" },
  cancelId: null,

  loadProfile: function() {
    var raw = localStorage.getItem("fastq_customer_profile");
    if (!raw) return;
    try {
      var p = JSON.parse(raw);
      if (p.name) this.profile.name = p.name;
      if (p.phone) this.profile.phone = p.phone;
      if (p.email) this.profile.email = p.email;
    } catch(e) { /* ignore */ }
  },

  renderProfile: function() {
    $("#homeProfileTitle").text(this.profile.name || "Guest");
    var contactText = "Email: " + (this.profile.email || "not set") + " • Phone: " + (this.profile.phone || "not set");
    $("#homeProfileSubtitle").text(contactText);
  },

  loadList: function() {
    var raw = localStorage.getItem(this.storageKey);
    if (!raw) { this.list = []; return; }
    try { this.list = JSON.parse(raw) || []; } catch(e) { this.list = []; }
  },

  saveList: function() { localStorage.setItem(this.storageKey, JSON.stringify(this.list)); },

  addFromQuery: function() {
    var params = new URLSearchParams(window.location.search);
    var apptId = params.get("appointmentId");
    if (apptId) {
      this.upsert({ id: apptId });
    }
  },

  upsert: function(item) {
    if (!item || !item.id) return;
    var exists = this.list.some(function(x){ return x.id === item.id; });
    if (!exists) {
      this.list.unshift({ id: item.id });
      this.saveList();
    }
  },

  addFromInput: function() {
    var val = $("#addAppointmentId").val().trim();
    if (!val) return;
    this.upsert({ id: val });
    $("#addAppointmentId").val("");
    this.render();
    this.refreshOne(val);
  },

  refreshOne: function(id) {
    var self = this;
    $("#homeMsg").removeClass("error ok").text("Refreshing...");
    return new Promise(function(resolve){
      PageMethods.GetAppointmentSnapshot(id, function(res){
        if (!res || !res.ok) {
          $("#homeMsg").addClass("error").text(res && res.error ? res.error : "Not found");
          resolve(res);
          return;
        }
        self.list = self.list.map(function(x){
          if (x.id === id) {
            var d = res.data;
            return {
              id: d.AppointmentId,
              queueId: d.QueueId,
              queueName: d.QueueName || "Queue",
              locationId: d.LocationId,
              status: d.Status,
              scheduledUtc: d.ScheduledForUtc,
              updatedUtc: d.UpdatedUtc,
              position: d.PositionInQueue,
              waitingCount: d.WaitingCount,
              contactMethod: x.contactMethod
            };
          }
          return x;
        });
        self.saveList();
        self.render();
        $("#homeMsg").addClass("ok").text("Updated.");
        resolve(res);
      }, function(){
        $("#homeMsg").addClass("error").text("Load failed.");
        resolve(null);
      });
    });
  },

  refreshAll: function() {
    var self = this;
    if (!this.list.length) { $("#homeMsg").text("No appointments tracked yet."); return; }
    $("#homeMsg").removeClass("error ok").text("Refreshing all...");
    var queue = this.list.map(function(x){ return self.refreshOne(x.id); });
    return Promise.all(queue);
  },

  promptCancel: function(id) {
    this.cancelId = id;
    $("#cancelReason").val("");
    $("#cancelMsg").text("");
    $("#cancelSummary").text("Are you sure you want to cancel appointment " + id + "?");
    $("#cancelModal").css("display", "flex");
  },

  closeModal: function() {
    $("#cancelModal").hide();
    this.cancelId = null;
  },

  confirmCancel: function() {
    if (!this.cancelId) { this.closeModal(); return; }
    var self = this;
    var reason = $("#cancelReason").val();
    $("#cancelMsg").removeClass("error ok").text("Cancelling...");
    PageMethods.CancelAppointment(self.cancelId, function(res){
      if (!res || !res.ok) {
        $("#cancelMsg").addClass("error").text(res && res.error ? res.error : "Cancel failed");
        return;
      }
      $("#cancelMsg").addClass("ok").text("Cancelled.");
      self.refreshOne(self.cancelId);
      setTimeout(function(){ self.closeModal(); }, 300);
    }, function(){
      $("#cancelMsg").addClass("error").text("Cancel failed.");
    });
  },

  remove: function(id) {
    this.list = this.list.filter(function(x){ return x.id !== id; });
    this.saveList();
    this.render();
  },

  formatStatusTag: function(status) {
    var cls = "neutral";
    if (!status) status = "Unknown";
    if (status.toLowerCase().indexOf("cancel") >= 0 || status.toLowerCase().indexOf("closed") >= 0) cls = "danger";
    else if (status.toLowerCase().indexOf("complete") >= 0 || status.toLowerCase().indexOf("done") >= 0) cls = "past";
    else cls = "soon";
    return '<span class="tag ' + cls + '">' + status + '</span>';
  },

  isPast: function(status) {
    if (!status) return false;
    var s = status.toLowerCase();
    return s.indexOf("complete") >= 0 || s.indexOf("cancel") >= 0 || s.indexOf("closed") >= 0 || s.indexOf("transfer") >= 0;
  },

  render: function() {
    var upcoming = [];
    var past = [];
    this.list.forEach(function(x){
      (FastQHome.isPast(x.status) ? past : upcoming).push(x);
    });

    function row(item) {
      var statusTag = FastQHome.formatStatusTag(item.status || "Scheduled");
      var type = item.contactMethod || "Online";
      var cancelLink = FastQHome.isPast(item.status) ? "<span class='muted'>-</span>" : "<a href='javascript:void(0)' class='cancel-link' onclick='FastQHome.promptCancel(\"" + item.id + "\")'>Cancel</a>";
      return "<tr>" +
        "<td>" + (item.scheduledUtc || "xx-xx-xxxx xxxx AM") + "</td>" +
        "<td>" + (item.queueName || "Queue") + "</td>" +
        "<td>" + type + "</td>" +
        "<td>" + statusTag + "</td>" +
        "<td>" + cancelLink + "</td>" +
      "</tr>";
    }

    $("#upcomingTable").html(upcoming.length ? upcoming.map(row).join("") : "<tr><td colspan='5' class='muted'>Nothing upcoming.</td></tr>");
    $("#historyTable").html(past.length ? past.map(row).join("") : "<tr><td colspan='5' class='muted'>No history.</td></tr>");
  },

  switchTab: function(tab) {
    $("#homeTabs button").removeClass("active");
    $("#homeTabs button[data-tab='" + tab + "']").addClass("active");
    ["upcoming","history","profile"].forEach(function(t){
      $("#tab-" + t).hide();
    });
    $("#tab-" + tab).show();
  },

  init: function() {
    this.loadProfile();
    this.renderProfile();
    this.loadList();
    this.addFromQuery();
    this.render();
    this.refreshAll();
  }
};

$(function(){ FastQHome.init(); });
</script>
</asp:Content>
