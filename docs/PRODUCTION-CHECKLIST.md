# Serket: before going live, and when something goes wrong

## 1. Must do before launch

| Done | Item |
|---|---|
| [ ] | Run the app in **Production** mode (`ASPNETCORE_ENVIRONMENT=Production`; the Dockerfile sets it). Development mode shows detailed errors. |
| [ ] | Set your own admin login: `Admin__Username` and `Admin__PasswordHash` (create the hash with `dotnet run --project src/ECommerceStore.Web -- hash-password "YourPassword"`). In Production the site refuses to start with the hash that ships in `appsettings.json`. |
| [ ] | Set `ConnectionStrings__DefaultConnection` to a **new** database user with a strong password (not the development one). |
| [ ] | Set `AllowedHosts` to your domain (for example `serket.com;www.serket.com`). |
| [ ] | Set the e-mail settings (`Email__SmtpHost`, `Email__SmtpUsername`, `Email__SmtpPassword`, `Email__ToAddress`). Receipts and reminders are skipped without them. |
| [ ] | Set `DataProtection__KeysPath` to a folder that survives restarts. Without it, every restart logs everyone out and invalidates order-confirmation links. |
| [ ] | Make `wwwroot/uploads` (product photos) and `App_Data` (private payment receipts) live on a persistent disk or volume. A container's own disk is erased on redeploy. |
| [ ] | Put the site behind HTTPS and a CDN/proxy such as Cloudflare. Set `ASPNETCORE_FORWARDEDHEADERS_ENABLED=true` so the rate limiter sees the visitor's real address. |
| [ ] | Run `scripts/cleanup-test-data.sql` (read the notes at the top) and enter real stock. |
| [ ] | Put your real exchange policy in the FAQ (search for `TODO(owner)` in `Pages/Shared/_Faq.cshtml`). |
| [ ] | Place one real test order on the live site (cash on delivery and one with a receipt), check the e-mail receipt, the admin order page and the receipt image. |

## 2. Security: what is in place

* **SQL injection:** all database access goes through Entity Framework Core, which sends values as parameters. There is no hand-written SQL in the application.
* **XSS:** Razor encodes everything it prints. `Html.Raw` is used once, for JSON built by the server. No script uses `innerHTML`. The Content-Security-Policy allows scripts only from this site (no inline scripts), so an injected script would not run.
* **CSRF:** every form is protected by an anti-forgery token (checked automatically for all POSTs).
* **Passwords:** PBKDF2-SHA256, 600,000 iterations, random salt, constant-time comparison. Admin and customer logins use separate cookies (HttpOnly, Secure, SameSite=Lax).
* **Brute force / floods:** per-address rate limits (login and sign-up 20/min, checkout and reviews 60/min, everything 600/min), answered with HTTP 429.
* **Uploads:** the file's real content is checked (JPEG/PNG/WEBP signature), the stored name and extension are generated, and files are sent with `nosniff`. Payment receipts are stored outside the web root and only the admin page can show them.
* **Other customers' data:** there is no page that lists a customer's orders. The order confirmation page needs a signed, expiring link created at checkout; guessing an order number shows nothing. Guest checkouts never reuse another customer's record. Promo codes and reviews are tied to the logged-in account. All of `/Admin` needs the admin login, and status changes are checked on the server.
* **Headers:** CSP, X-Frame-Options, nosniff, Referrer-Policy, Permissions-Policy, HSTS.

### Not covered by the application (needs your hosting)
* **DDoS:** an application cannot absorb a large attack. Put Cloudflare (or similar) in front, hide the server's real address and turn on its bot/DDoS protection. The in-app limits only handle small floods and password guessing.
* **Server and database patching, firewall:** only open ports 80/443; keep the database private (not reachable from the internet).

## 3. Many visitors and crashes

* **Overselling:** the last unit can't be sold twice. Stock is taken with one atomic database statement inside a transaction with the order, and cancelling an order puts the units back.
* **Scale:** the site keeps no data in memory between requests, so more copies of it can run behind a load balancer as long as they share the database, `wwwroot/uploads`, `App_Data` and the data-protection keys. Public pages skip change tracking, the database connection is pooled, and CSS/JS are cached by browsers.
* **Database down:** pages show the friendly error page (HTTP 500) with a reference number; no stack trace is shown. Orders can't be placed until it is back.
* **If the process crashes:** make your host restart it automatically (Docker `--restart unless-stopped`, systemd `Restart=always`, or the host's own setting). `/health/live` says the process is alive; `/health` also checks the database. Point an uptime monitor (UptimeRobot, Better Stack...) at `/health` and have it e-mail or message you.

## 4. Errors and monitoring

* Unhandled errors are logged by ASP.NET Core with the same reference number the visitor sees on the error page. Keep the console/log output (Docker logs, your host's log viewer) and look up the reference when a customer reports a problem.
* Mail failures never break an order; they are logged as errors (`Failed to send email to ...`).
* Worth adding when you grow: a log service (Seq, Grafana Loki, Application Insights or Sentry) so errors alert you instead of waiting to be noticed.

## 5. Rolling back

1. **Before every release:** back up the database (`pg_dump -Fc ... -f before-release.dump`) and keep the previous build (the previous Docker image tag or a git tag).
2. **Deploy:** apply migrations (`dotnet ef database update`), then start the new version. Check `/health` and place a test order.
3. **If the new version is bad:** switch back to the previous image/build. If the release also changed the database, the old code usually still works because new columns are only added. If it doesn't, restore the backup (`pg_restore -c -d <database> before-release.dump`); this also removes any orders placed since the backup, so do it quickly or export those orders first.
4. **Avoid risky changes in one step:** add new columns first and deploy, and only remove old columns in a later release once nothing uses them.
