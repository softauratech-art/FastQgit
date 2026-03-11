
<!DOCTYPE html>
<html>
<head>
    <title>FastQ Manager</title>
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1" />
    <link rel="preconnect" href="https://fonts.googleapis.com" />
    <link rel="preconnect" href="https://fonts.gstatic.com" crossorigin />
    <link href="https://fonts.googleapis.com/css2?family=Source+Sans+3:wght@400;500;600&family=Space+Grotesk:wght@500;600;700&display=swap" rel="stylesheet" />
  
    <link rel="stylesheet" href="https://cdn.jsdelivr.net/npm/bootstrap-icons@1.13.1/font/bootstrap-icons.min.css" />
    <link rel="stylesheet" href="Content/site.css">    
</head>
<body>
    <div class="layout">
        <aside class="drawer">
            <div class="brand">
                <div class="brand-top">
                    <div class="logo-mark"></div>
<%--                    <button class="nav-toggle" type="button" aria-label="Collapse navigation" aria-expanded="true">
                        <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                            <path d="M15 6l-6 6 6 6"></path>
                        </svg>
                    </button>--%>
                </div>
                <div class="brand-title"><!--FastQ--></div>
                <div class="brand-sub">Queue orchestration</div>
            </div>
            <nav class="drawer-nav">
                <div class="nav-section">
                    <div class="nav-title">Overview</div>
                    <a class="nav-link active" href="/" title="Dashboard" aria-label="Dashboard">
                        <i class="bi bi-house"></i>
                        <span class="nav-text">Dashboard</span>
                    </a>
            </nav>
            <div class="drawer-footer">
                <div class="status-card">
                    <div class="status-title">System Health</div>
                    <div style="display:flex; align-items:center; justify-content:space-between;">
                        <span class="pill">Online</span>
                        <span class="muted"></span>
                    </div>
                </div>
            </div>
        </aside>

        <div class="main-area">
            <header class="topbar">                
            <div>
                <div class="topbar-title">FastQ Manager</div>
                <div class="topbar-sub">
                    FastQ keeps every queue in sync, in real time.
                    Book an appointment, watch it instantly appear for the provider, and follow every state change live.
                </div>
            </div>

                <div class="user-chip">
                    <%=New FastQ.Web.Services.AuthService().GetLoggedInWindowsUser() %>
                    <!--Before:   <%=Session.Keys.Count() %>-->
                    <%Session.Clear() %>
                    <!--After: <%=Session.Keys.Count() %>-->
                </div>
            </header>

            <main class="page">
                <div class="card">
                    <h3>You have succesfully logged out.</h3>
                    <div><a href="Home/" class="btn primary">Login</a></div>
                </div>
            </main>
        </div>
    </div>
</body>
</html>
