<%@ Page Title="Home" Language="C#" MasterPageFile="~/Site.master" AutoEventWireup="true" CodeBehind="Default.aspx.cs" Inherits="FastQ.Web.Default" %>
<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">
    <div class="hero">
        <div class="card">
            <div class="eyebrow">Live SignalR Prototype</div>
            <h1 class="page-title">FastQ keeps every queue in sync, in real time.</h1>
            <p class="lead">Book an appointment, watch it instantly appear for the provider, and follow every state change live.</p>
            <div class="row" style="margin-top:16px;">
                <a class="btn primary" href="/Customer/Book.aspx">Start a Booking</a>
                <a class="btn ghost" href="/Provider/Today.aspx">Open Provider View</a>
            </div>
        </div>

        <div class="card note">
            <div class="card-header">
                <div>
                    <div class="eyebrow">Demo IDs</div>
                    <h3 class="card-title">Use these seeded IDs</h3>
                </div>
                <span class="badge accent">Ready</span>
            </div>
            <div class="stack">
                <div class="stat">
                    <div class="stat-label">Location ID</div>
                    <div class="stat-value"><code>11111111-1111-1111-1111-111111111111</code></div>
                </div>
                <div class="stat">
                    <div class="stat-label">General Queue</div>
                    <div class="stat-value"><code>22222222-2222-2222-2222-222222222222</code></div>
                </div>
                <div class="stat">
                    <div class="stat-label">Secondary Queue</div>
                    <div class="stat-value"><code>33333333-3333-3333-3333-333333333333</code></div>
                </div>
                <div class="stat">
                    <div class="stat-label">Provider ID</div>
                    <div class="stat-value"><code>44444444-4444-4444-4444-444444444444</code></div>
                </div>
            </div>
        </div>
    </div>

    <div class="grid-2">
        <div class="card">
            <div class="card-header">
                <h3 class="card-title">Customer Journey</h3>
                <span class="pill">Live booking</span>
            </div>
            <p class="muted">Submit a new appointment and immediately receive a live status screen with queue position and updates.</p>
            <a class="btn secondary" href="/Customer/Book.aspx">Go to Customer Booking</a>
        </div>
        <div class="card">
            <div class="card-header">
                <h3 class="card-title">Provider Console</h3>
                <span class="pill">Real-time queue</span>
            </div>
            <p class="muted">Manage arrivals, service start, completion, and transfers while customers see every change.</p>
            <a class="btn secondary" href="/Provider/Today.aspx">Go to Provider Today</a>
        </div>
    </div>
</asp:Content>
