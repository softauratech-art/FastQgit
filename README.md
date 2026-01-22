# FastQ Prototype (ASP.NET MVC 5 on .NET Framework 4.8) - Simplified + Live SignalR

This is a **prototype** showing a simplified 2-layer architecture with an **Oracle-backed data store**, plus **live queue updates** using **SignalR (customer ? provider updates instantly)**.

## Stack
- ASP.NET MVC 5 (.NET Framework 4.8)
- SignalR 2.x (OWIN hosted)
- FastQ.Data repositories (Oracle)
- MVC controllers returning JSON for async refresh (no API handlers)

## Quick start (Visual Studio)
1. Open `FastQPrototype_LiveSignalR.sln`
2. Restore NuGet packages
3. Set `FastQ.Web` as Startup Project
4. Run (IIS Express)

## Demo flow (live)
1. Open **Provider: Today** in one tab
2. Open **Customer: Book** in another tab
3. Book with any phone - Provider screen updates immediately
4. Provider actions (Arrive / Begin / End / Transfer) - Customer Status updates immediately
5. Toast notifications appear for all users on status changes
