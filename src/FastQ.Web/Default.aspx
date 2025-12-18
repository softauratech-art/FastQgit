<%@ Page Title="Home" Language="C#" MasterPageFile="~/Site.master" AutoEventWireup="true" CodeBehind="Default.aspx.cs" Inherits="FastQ.Web.Default" %>
<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">
    <h1>FastQ Prototype (Layered + Live SignalR)</h1>

    <div class="card">
        <div><span class="badge">Demo IDs</span></div>
        <p class="muted">These IDs are pre-seeded (in-memory) so you can start immediately.</p>
        <ul>
            <li>LocationId: <code>11111111-1111-1111-1111-111111111111</code></li>
            <li>General QueueId: <code>22222222-2222-2222-2222-222222222222</code></li>
            <li>Secondary QueueId: <code>33333333-3333-3333-3333-333333333333</code></li>
            <li>ProviderId: <code>44444444-4444-4444-4444-444444444444</code></li>
        </ul>
    </div>

    <div class="row">
        <div class="col card">
            <h3>Customer</h3>
            <p>Book an appointment and watch it appear live on the Provider screen.</p>
            <a class="btn" href="/Customer/Book.aspx">Go to Customer Booking</a>
        </div>
        <div class="col card">
            <h3>Provider</h3>
            <p>Manage the queue (Arrive / Begin / End / Transfer). Customers see updates instantly.</p>
            <a class="btn" href="/Provider/Today.aspx">Go to Provider Today</a>
        </div>
    </div>
</asp:Content>
