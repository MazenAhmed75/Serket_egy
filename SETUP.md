# ECommerceStore — Setup

## 1. Start PostgreSQL

    docker compose up -d

pgAdmin is reachable at http://localhost:5050 (admin@store.com / admin_password_123).

## 2. Restore & build

Run from the repository root (the folder with `ECommerceStore.sln`):

    dotnet restore
    dotnet build

## 3. Apply the database migration

Only needed once initially — **and again any time the schema changes** (like the colour-options
feature added in this update). From the repository root:

    dotnet ef migrations add InitialCreate --project src/ECommerceStore.Infrastructure --startup-project src/ECommerceStore.Web
    dotnet ef database update --project src/ECommerceStore.Infrastructure --startup-project src/ECommerceStore.Web

(If `dotnet ef` isn't found: `dotnet tool install --global dotnet-ef`.)

**If you already ran `InitialCreate` before this update:** don't repeat that exact command —
it would try to create tables that already exist. Whenever a migration is added (e.g. `AddProductColors`, `AddGenderAndSizeToOrderItem`), simply apply it to your database:

    dotnet ef database update --project src/ECommerceStore.Infrastructure --startup-project src/ECommerceStore.Web

## 4. Run the Web project

    dotnet run --project src/ECommerceStore.Web

Open the printed URL (e.g. `https://localhost:60872`) — the storefront loads there directly.

**If your `Products` table already had a row from an earlier run:** the seeder only inserts
when the table is completely empty, so it won't overwrite anything automatically. You no
longer need pgAdmin for this, though — use the new **Admin → Product** page (step 5 below)
to edit the name, description, price, stock and photo directly from the browser.

## 5. Sign in to the Admin area

1. Run the app (step 4) and note the URL it prints, e.g. `https://localhost:60872`.
2. In your browser, go to that same address with `/Admin` on the end —
   e.g. `https://localhost:60872/Admin`.
3. You're not logged in yet, so it redirects you to `/Admin/Login`. Sign in with:

       Username: admin
       Password: ChangeMe123!

4. You'll land on the Dashboard. Use the top nav to reach **Orders** or **Product**.

**Change this immediately** — it's a default seeded into `appsettings.json` for this
deliverable. To set a real password:

1. Pick a new password.
2. Generate its hash with a short one-off script (uses the exact same PBKDF2 scheme as
   `Pbkdf2PasswordHasher`, so it's guaranteed compatible) — e.g. in `csharp` (`dotnet-script`)
   or a scratch Console project:

       using System.Security.Cryptography;
       var salt = RandomNumberGenerator.GetBytes(16);
       var key = Rfc2898DeriveBytes.Pbkdf2("YourNewPassword", salt, 100_000, HashAlgorithmName.SHA256, 32);
       Console.WriteLine($"100000.{Convert.ToBase64String(salt)}.{Convert.ToBase64String(key)}");

3. Paste the printed value into `Admin:PasswordHash` in `appsettings.json` (or better,
   into user-secrets / an environment variable so it isn't committed to source control).

## Configuration you'll want to fill in (src/ECommerceStore.Web/appsettings.json)

- `Email:SmtpHost` / `Email:ToAddress` (and the rest of the `Email:*` keys) — sends an order
  alert by email, and sends each customer their order receipt and back-in-stock reminders. Skipped with a warning if not set.
  A free path: a Gmail account with an
  [app password](https://myaccount.google.com/apppasswords) (not your normal Gmail
  password — Google requires 2-Step Verification to be on first) —
  `SmtpHost: smtp.gmail.com`, `SmtpPort: 587`, `UseSsl: true`, `SmtpUsername` your Gmail
  address, `SmtpPassword` the 16-character app password, `FromAddress` the same Gmail
  address, `ToAddress` wherever you want the alert delivered (can be the same address).
- `Payment:VodafoneCashNumber` / `Payment:InstaPayHandle` — shown to the customer on the
  checkout page.
- `Admin:Username` / `Admin:PasswordHash` — see step 5 above.

## Local .env file (optional convenience)

Instead of setting environment variables by hand every time you open a terminal, copy
`src/ECommerceStore.Web/.env.example` to `src/ECommerceStore.Web/.env` and fill in real
values there. It's loaded automatically on startup (`DotNetEnv`) and is already gitignored —
it never gets committed. This is for local development only; production doesn't use it at
all (see below).

## Going live: overriding config without editing appsettings.json

ASP.NET Core automatically layers environment variables over `appsettings.json`, using a
double-underscore for nesting. So instead of committing real secrets, set these on whatever
host you deploy to:

    ConnectionStrings__DefaultConnection
    Admin__Username
    Admin__PasswordHash
    Email__SmtpHost
    Email__SmtpUsername
    Email__SmtpPassword
    Email__ToAddress

Leave the matching keys in `appsettings.json` blank/default — they're just local-dev
fallbacks and a template of what exists.

## Building the Docker image locally (optional, but worth testing before deploying)

    docker build -t ecommercestore .
    docker run -p 8080:8080 --env ConnectionStrings__DefaultConnection="Host=host.docker.internal;Port=5432;Database=store_db;Username=store_user;Password=store_password_123" ecommercestore

Then open http://localhost:8080/health.

## Repository structure notes

`.gitignore` already excludes `bin/`, `obj/`, and everything under `wwwroot/uploads/`
(uploaded receipts) except a `.gitkeep` placeholder, so a fresh clone starts with empty
folders in the right place. `.dockerignore` mirrors that for image builds.

## Troubleshooting: NU1605 package downgrade error on restore/build

If `dotnet build` or `dotnet run` fails with something like:

    error NU1605: Detected package downgrade: Microsoft.Extensions.Logging.Abstractions
    from 8.0.2 to 8.0.1

a transitive dependency (EF Core, here) needs a newer patch version of a package than what's
pinned directly in `ECommerceStore.Infrastructure.csproj`. This project already pins
`Microsoft.Extensions.Configuration.Abstractions` and `Microsoft.Extensions.Logging.Abstractions`
at `8.0.2` to match, so a fresh clone of this zip shouldn't hit it. If you edited a version
number and still see the error, it's almost always a stale NuGet/build cache holding onto the
old resolution — clear it and restore again:

    dotnet clean
    dotnet nuget locals all --clear
    dotnet restore
    dotnet build

(If `dotnet clean` doesn't remove everything on Windows, manually delete the `bin/` and
`obj/` folders under each of the three project folders, then re-run the four commands above.)



**Step 1 — Core + Infrastructure**: entities, enums, repository + `IUnitOfWork` pattern,
EF Core Fluent API configurations, `AppDbContext`.

**Step 2 — Storefront**: brand palette/typography from the Serket logo; product landing
page with feature/how-it-works sections; checkout with Leaflet delivery-pin picker and
Cash on Delivery / Vodafone Cash / InstaPay (with receipt upload); order confirmation;
`IFileStorageService`, `DbSeeder`.

**Step 3 — Scrubs branding + Admin area**
- Real logo image (`wwwroot/images/logo.png`, cropped from your plaque photo) in the header.
- Home page now has a "Why Serket" feature grid and a 3-step "How ordering works" section,
  seeded with a scrub-set placeholder product (`wwwroot/images/product-placeholder.jpg`).
- `/Admin` — cookie-authenticated (single configured account, PBKDF2-hashed password, no
  Users table):
  - **Dashboard** (`/Admin`) — order counts, revenue, recent orders.
  - **Orders** (`/Admin/Orders`) — filterable by status.
  - **Order details** — customer/delivery/payment info, a read-only delivery-pin map,
    receipt screenshots, and actions: mark payment verified (→ Paid), mark shipped, cancel.
  - **Product** — edit the store's one product (name, description, price, stock, active
    flag, photo) without touching the database. Acts as a create form if no product exists.

**Step 4 — Fixes from your feedback**
- Real cropped logo image (no more CSS-text reconstruction).
- Payment-method labels are vertically centered so "Cash on delivery" (which wraps to two
  lines) lines up with the single-line options either side of it.
- The delivery map now drops a visible gold pin (drawn as inline SVG, not Leaflet's default
  marker image, which some CDNs fail to load silently), and the confirmation text below the
  map now reverse-geocodes the pin through OpenStreetMap's Nominatim service and shows the
  actual street address instead of raw coordinates (falls back to coordinates if that
  lookup fails or is rate-limited — Nominatim is a shared free public service).
- Quantity is now a proper +/- stepper instead of a bare number box, on both the product
  page and checkout (`wwwroot/js/quantity-stepper.js`).
- Order emails: `IEmailNotifier` (Infrastructure: `SmtpEmailNotifier`, via MailKit/SMTP) —
  sends the owner an alert for each order and each customer their receipt; silently skipped if not set up.
- `Dockerfile` + `.dockerignore` at the repo root, for deploying to any Docker-based host.

**Step 6 — colour options, and more remarks**
- Products can now have colour variants (`ProductColor`: name, swatch colour, its own stock).
  Manage them from **Admin → Product** → "Colours". A product with none behaves exactly as
  before (no picker, plain stock count) — this is additive, not required.
- The storefront shows round swatch buttons; picking one carries through to checkout, and
  stock/quantity limits follow the chosen colour instead of the product overall.
- Order emails now default to `Serket.egy@gmail.com`; the InstaPay number shown at checkout
  is now `01212353059`.
- Local `.env` file support (`DotNetEnv`) — see "Going live" below for how this differs from
  production secrets.

## Known simplifications

- Order numbers are `ORD-yyyyMMdd-XXXXXX` (random suffix, unique DB index, no retry loop).
- Receipts are stored on local disk, not cloud storage.
- The Admin area manages *orders*, not the product catalog yet — editing the product still
  means updating its row directly in pgAdmin.
- The placeholder scrub photo is a small (365×547) supplier/catalog-style image — swap it
  for your own product photography before going live, and confirm you have the right to use
  any sourced image commercially in the meantime.

## Next steps (not yet built)

- Nothing from the original spec is outstanding — everything in the brief (storefront,
  checkout, Admin, e-mail notifications, product management) is now in place. From here it's
  refinement: real product photography/copy, a real admin password,
  and anything else you'd like adjusted.
