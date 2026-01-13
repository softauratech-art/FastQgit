# FastQ Prototype (ASP.NET WebForms 4.8) — Layered + Live SignalR

This is a **prototype** showing a layered architecture with an **in-memory DB** that is structured to be swapped to a real DB later, plus **live queue updates** using **SignalR (customer ↔ provider updates instantly)**.

## Stack
- ASP.NET WebForms (.NET Framework 4.8)
- SignalR 2.x (OWIN hosted)
- In-memory repositories (Infrastructure layer)

## Quick start (Visual Studio)
1. Open `FastQPrototype_LiveSignalR.sln`
2. Restore NuGet packages
3. Set `FastQ.Web` as Startup Project
4. Run (IIS Express)

## Demo flow (live)
1. Open **Provider: Today** in one tab  
2. Open **Customer: Book** in another tab  
3. Book with any phone → Provider screen updates immediately  
4. Provider actions (Arrive / Begin / End / Transfer) → Customer Status updates immediately

## Pre-seeded IDs (in-memory)
- LocationId: `00a98ac7-0000-0000-4641-535451494430`
- General QueueId: `0153158e-0000-0000-4641-535451494430`
- Secondary QueueId: `01fca055-0000-0000-4641-535451494430`
- ProviderId: `02a62b1c-0000-0000-4641-535451494430`

