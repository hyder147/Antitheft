# AntiTheftApp — Week 1: Backend Foundation

What's in this folder maps directly to Week 1 of the roadmap: Firebase + Postgres
set up, EF Core models, auth (register/login/JWT), and a working FCM push send.
No mobile app yet — that's Week 2.

## What you're getting

```
AntiTheftApp/
├── AntiTheftApp.sln
├── docker-compose.yml              ← local Postgres, no install needed
├── .gitignore
└── AntiTheftApp.Api/
    ├── AntiTheftApp.Api.csproj
    ├── Program.cs                  ← wires up DB, JWT auth, Firebase, Swagger
    ├── appsettings.json            ← config (placeholders — you'll edit this)
    ├── appsettings.Development.json
    ├── firebase-service-account.json.example
    ├── AntiTheftApp.Api.http       ← ready-made test requests
    ├── Models/                     ← User, Device, Command
    ├── Data/                       ← EF Core DbContext
    ├── DTOs/                       ← request/response shapes
    ├── Services/                   ← JWT issuing, Firebase push sending
    └── Controllers/                ← AuthController, DevicesController
```

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/) (for Postgres — or install Postgres natively if you prefer)
- A free [Firebase](https://console.firebase.google.com/) account

## Setup steps (do these in order)

### 1. Start Postgres
```bash
docker compose up -d
```
This spins up Postgres on `localhost:5432` with database `antitheftapp`.

**Before running this**, open `docker-compose.yml` and `AntiTheftApp.Api/appsettings.json`
and change `CHANGE_ME` to the same real password in **both** files — they need to match.

### 2. Create your Firebase project
1. Go to the [Firebase Console](https://console.firebase.google.com/) → **Add project**.
2. Once created, go to **Project Settings (gear icon) → Cloud Messaging** and confirm the
   Firebase Cloud Messaging API is enabled (it is by default on new projects).
3. Go to **Project Settings → Service Accounts → Generate New Private Key**. This downloads
   a JSON file.
4. Rename that downloaded file to `firebase-service-account.json` and place it directly in
   `AntiTheftApp.Api/` (next to `Program.cs`). It's already gitignored, so it's safe there.

   *(`firebase-service-account.json.example` in this folder shows you the expected shape —
   don't use it directly, it has fake values.)*

### 3. Set a real JWT signing key
In `appsettings.json`, replace `Jwt:Key` with any random 32+ character string. Quick way to
generate one:
```bash
openssl rand -base64 32
```

### 4. Restore packages and build
```bash
cd AntiTheftApp.Api
dotnet restore
dotnet build
```
If this is your first time — `dotnet restore` needs internet access to NuGet.org to download
the packages listed in the `.csproj` (EF Core, JWT auth, BCrypt, FirebaseAdmin, Swagger).

### 5. Create the database schema
```bash
dotnet tool install --global dotnet-ef   # first time only
dotnet ef migrations add InitialCreate
dotnet ef database update
```
This reads your three models (`User`, `Device`, `Command`) and creates matching tables in
Postgres.

### 6. Run it
```bash
dotnet run
```
Swagger UI opens automatically (or visit `https://localhost:7001/swagger`) — you can test
every endpoint from the browser, or open `AntiTheftApp.Api.http` in VS Code (with the
**REST Client** extension) and click "Send Request" above each numbered request.

## What "done" looks like for Week 1

- [ ] `POST /api/auth/register` creates a user and returns a JWT
- [ ] `POST /api/auth/login` returns a JWT for existing credentials
- [ ] `POST /api/devices/register` (with a valid JWT) creates a device row
- [ ] `GET /api/devices` lists it back
- [ ] `POST /api/devices/{id}/commands` attempts an FCM push — **it's fine if this
      returns a 502** right now, since there's no real phone with a real FCM token yet.
      A 502 here specifically means "everything is wired correctly, delivery just failed,"
      which is exactly what Week 2 (the mobile app) will fix by producing a real token.

## A note on why some choices were made

- **Data-only FCM message, not a "notification" message** — this lets the mobile app's own
  code decide what happens on receipt (start the alarm service) instead of Android showing
  a generic tray notification first, which a thief could just swipe away.
- **PIN is separate from the login password** — the password logs you into the *portal* on a
  second device; the PIN is what's typed *on the lost phone itself* to silence the alarm.
  Keeping them separate means a shoulder-surfed PIN doesn't also compromise the account.
- **Every `Command` is logged**, not just fired-and-forgotten — this becomes your audit trail
  ("someone tried to trigger my alarm at 3am") and lets a future retry/polling mechanism work
  if a push gets dropped.

## Next: Week 2
Week 2 builds the actual .NET MAUI mobile app that registers against `/api/devices/register`
with a real FCM token, and implements the foreground service + `STREAM_ALARM` audio that
actually makes noise when a command arrives.

## Reboot persistence & Device Admin (Android)

If the phone is powered off and back on **while the alarm/lock screen is active**, the app
now brings the lock screen straight back on boot instead of the thief getting a clean,
unlocked device. This needed three pieces, all on the Android side:

- `BootReceiver` — listens for `BOOT_COMPLETED`, checks a locally-saved "was this device
  armed?" flag, and re-launches `AlarmActivity` immediately (skipping the grace-period
  countdown, since the phone was already armed before it powered off).
- `AntiTheftDeviceAdminReceiver` — a Device Admin (not Device Owner) that lets the app resist
  casual uninstall/force-stop, and best-effort-pings `POST /api/devices/{id}/security-event`
  if someone starts removing admin protection.
- The user must tap **"Enable Device Admin Protection"** on the home screen once — this is a
  system permission dialog Android requires; it can't be silently auto-enabled.

**⚠️ New DB migration needed** — `Device` gained `LastSecurityEvent` / `LastSecurityEventAt`
columns. Run this before starting the API:
```bash
cd AntiTheftApp.Api
dotnet ef migrations add AddDeviceSecurityEvents
dotnet ef database update
```

**What this does *not* do — read this before you rely on it:**
- **It does not survive a factory reset.** A factory reset wipes the app itself, so there is
  nothing left on the device to bring the lock screen back. This is true for every
  third-party app on Android, not a limitation specific to this project — only Google's
  built-in Factory Reset Protection (tied to the device's Google account) survives a reset,
  and that's independent of this app entirely. Tell users to keep a Google account signed in
  on the device; that's what actually makes a stolen, reset phone unusable to resell.
- **It does not block a hardware recovery-mode reset.** Device Admin has no power over the
  bootloader/recovery menu.
- **OEM battery managers can still kill it.** On Xiaomi (MIUI), Huawei, Oppo, and some other
  OEM skins, the user also needs to manually allow "Autostart" for the app in that phone's
  battery/security settings, or `BOOT_COMPLETED` may never fire. No code fix exists for this.
