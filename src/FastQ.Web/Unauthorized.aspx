
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
                    <a href="<%=HttpContext.Current.Request.ApplicationPath%>" class="logo-container">
                        <img src="<%=HttpContext.Current.Request.ApplicationPath%>/Content/Horzontal_HighRes.jpg" alt="Logo" class="logo">
                    </a>
                </div>
                 <div class="brand-title">FastQ Manager</div>
            </div>
            <nav class="drawer-nav">
                <div class="nav-section">
                    <div class="nav-title">Overview</div>
                    <a class="nav-link" href="Home" title="Dashboard" aria-label="Dashboard">
                        <i class="bi bi-house"></i>
                        <span class="nav-text">Dashboard</span>
                    </a>
                </div>
            </nav>
            <div class="drawer-footer">
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
                </div>
            </header>
            <main class="page">
                <div class="card">
                    <h3>You are not authorized to view this page.</h3>
                </div>
            </main>
        </div>
    </div>
</body>
</html>
