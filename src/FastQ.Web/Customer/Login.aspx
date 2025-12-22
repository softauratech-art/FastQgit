<%@ Page Title="Customer Login" Language="C#" MasterPageFile="~/Site.master" AutoEventWireup="true" CodeBehind="Login.aspx.cs" Inherits="FastQ.Web.Customer.Login" %>
<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">
    <div class="hero">
        <div class="card">
            <div class="eyebrow">Passwordless Authentication</div>
            <h2 class="page-title">Please provide email address to get started.</h2>
            <p class="lead">We’ll send a one-time code. No passwords needed.</p>

            <div class="stack" style="margin-top:14px;">
                <div id="loginStepEmail">
                    <div class="field">
                        <label for="loginEmail">Email Address *</label>
                        <input type="email" id="loginEmail" placeholder="you@example.com" />
                    </div>
                    <div class="row">
                        <button type="button" class="btn primary" onclick="FastQLogin.sendCode()">Continue</button>
                        <span id="loginMsg" class="muted"></span>
                    </div>
                </div>

                <div id="loginStepCode" style="display:none;">
                    <p class="muted" id="codeSentText">We sent a code to …</p>
                    <div class="row" style="gap:8px; flex-wrap:wrap;">
                        <input type="text" maxlength="1" class="code-box" />
                        <input type="text" maxlength="1" class="code-box" />
                        <input type="text" maxlength="1" class="code-box" />
                        <input type="text" maxlength="1" class="code-box" />
                        <input type="text" maxlength="1" class="code-box" />
                        <input type="text" maxlength="1" class="code-box" />
                    </div>
                    <div class="row">
                        <button type="button" class="btn primary" onclick="FastQLogin.verify()">Continue</button>
                        <button type="button" class="btn ghost" onclick="FastQLogin.reset()">Back</button>
                    </div>
                </div>
            </div>
        </div>

        <div class="card note">
            <div class="card-header">
                <div>
                    <div class="eyebrow">Flow</div>
                    <h3 class="card-title">/manage → My Appointments</h3>
                    <p class="card-subtitle">Upcoming, History, and Profile tabs; cancel inline.</p>
                </div>
                <span class="badge ink">Prototype</span>
            </div>
            <p class="muted">On verify, we store your email locally and take you to My Appointments.</p>
        </div>
    </div>
</asp:Content>

<asp:Content ID="Content2" ContentPlaceHolderID="ScriptsContent" runat="server">
<script>
var FastQLogin = {
  sendCode: function() {
    $("#loginMsg").removeClass("error ok").text("");
    var email = $("#loginEmail").val().trim();
    if (!email) {
      $("#loginMsg").addClass("error").text("Email is required.");
      return;
    }
    $("#codeSentText").text("We sent a code to " + email.replace(/(.{3}).+(@.+)/, '$1***$2'));
    $("#loginStepEmail").hide();
    $("#loginStepCode").show();
    $(".code-box").val("").first().focus();
  },
  verify: function() {
    var code = $(".code-box").map(function(){ return $(this).val(); }).get().join("").trim();
    if (code.length < 4) {
      $("#loginMsg").addClass("error").text("Enter the code to continue.");
      return;
    }
    var email = $("#loginEmail").val().trim();
    var profile = { email: email };
    localStorage.setItem("fastq_customer_profile", JSON.stringify(profile));
    $("#loginMsg").addClass("ok").text("Verified! Redirecting to My Appointments...");
    setTimeout(function(){ window.location.href = "/Customer/Home.aspx"; }, 400);
  },
  reset: function() {
    $("#loginStepCode").hide();
    $("#loginStepEmail").show();
    $("#loginMsg").removeClass("error ok").text("");
  },
  bind: function() {
    $(".code-box").on("keyup", function(e){
      if (this.value && this.nextElementSibling) this.nextElementSibling.focus();
      if (e.key === "Backspace" && !this.value && this.previousElementSibling) this.previousElementSibling.focus();
    });
  },
  hydrate: function() {
    var raw = localStorage.getItem("fastq_customer_profile");
    if (!raw) return;
    try {
      var profile = JSON.parse(raw);
      if (profile.email) $("#loginEmail").val(profile.email);
    } catch (e) { /* ignore */ }
  }
};

$(function(){ FastQLogin.bind(); FastQLogin.hydrate(); });
</script>
</asp:Content>
