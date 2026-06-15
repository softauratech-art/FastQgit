
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
    <style>
        .logout-page {
            min-height: calc(100vh - 104px);
            display: grid;
            place-items: center;
            padding: 48px 20px;
            position: relative;
            overflow: hidden;
        }

        .logout-page::before {
            content: "";
            position: absolute;
            inset: 10% auto auto 50%;
            width: min(560px, 80vw);
            height: min(560px, 80vw);
            border-radius: 999px;
            background: radial-gradient(circle, rgba(31, 122, 140, 0.16), rgba(31, 122, 140, 0));
            transform: translateX(-50%);
            pointer-events: none;
        }

        .logout-card {
            width: min(520px, 100%);
            padding: 44px 42px;
            text-align: center;
            border: 1px solid rgba(15, 23, 42, 0.08);
            border-radius: 28px;
            background:
                linear-gradient(145deg, rgba(255, 255, 255, 0.96), rgba(248, 250, 252, 0.92)),
                #fff;
            box-shadow: 0 28px 70px rgba(15, 23, 42, 0.14);
            position: relative;
        }

        .logout-icon {
            width: 72px;
            height: 72px;
            display: inline-grid;
            place-items: center;
            margin-bottom: 22px;
            border-radius: 24px;
            color: #0f766e;
            background: linear-gradient(135deg, #d9f99d, #ccfbf1);
            box-shadow: inset 0 0 0 1px rgba(15, 118, 110, 0.12);
            font-size: 34px;
        }

        .logout-title {
            margin: 0 0 10px;
            color: #0f172a;
            font-family: "Space Grotesk", sans-serif;
            font-size: clamp(1.7rem, 4vw, 2.35rem);
            line-height: 1.05;
            letter-spacing: -0.04em;
        }

        .logout-message {
            max-width: 380px;
            margin: 0 auto 28px;
            color: #64748b;
            font-size: 1rem;
            line-height: 1.65;
        }

        .logout-actions {
            display: flex;
            justify-content: center;
        }

        .logout-login {
            min-width: 160px;
            justify-content: center;
            border-radius: 999px;
            padding-inline: 24px;
        }

        @media (max-width: 720px) {
            .main-area {
                min-width: 0;
            }

            .logout-page {
                min-height: calc(100vh - 88px);
                padding: 32px 14px;
            }

            .logout-card {
                padding: 34px 24px;
                border-radius: 24px;
            }
        }
    </style>
</head>
<body>
    <div class="layout">
        <aside class="drawer">
            <div class="brand">
                <div class="brand-top">
                   

                    <a href="<%=HttpContext.Current.Request.ApplicationPath%>" class="logo-container">
                        <img src="<%=HttpContext.Current.Request.ApplicationPath%>/Content/Horzontal_HighRes.jpg" alt="Logo" class="logo">
                    </a>
                   
<%--                    <button class="nav-toggle" type="button" aria-label="Collapse navigation" aria-expanded="true">
                        <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                            <path d="M15 6l-6 6 6 6"></path>
                        </svg>
                    </button>--%>
                </div>
                 <div class="brand-title">FastQ Manager</div>
            </div>
            <nav class="drawer-nav">
                <div class="nav-section">
                    <div class="nav-title">Overview</div>
                    <a class="nav-link active" href="Home" title="Dashboard" aria-label="Dashboard">
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
                    <%Session.Abandon() %>
                    <!--After: <%=Session.Keys.Count() %>-->
                </div>
            </header>

            <main class="page logout-page">
                <div class="logout-card">
                    <div class="logout-icon" aria-hidden="true">
                        <i class="bi bi-check2-circle"></i>
                    </div>
                    <h1 class="logout-title">You are signed out</h1>
                    <p class="logout-message">
                        Your FastQ Manager session has been closed securely. Sign in again whenever you need to continue managing queues.
                    </p>
                    <div class="logout-actions">
                        <a href="Home/" class="btn primary logout-login">Sign in again</a>
                    </div>
                </div>
            </main>
        </div>
    </div>
</body>
</html>
