# AntiTheftApp

[![CI](https://github.com/hyder147/Antitheft/actions/workflows/ci.yml/badge.svg)](https://github.com/hyder147/Antitheft/actions/workflows/ci.yml)

If your phone gets lost or stolen, you log into a web page from any other device, press a button, and the phone starts blaring an alarm. That is the whole idea behind this project.

I built it to practice backend development in .NET end to end: an API, a database, authentication, push notifications, a web client, a mobile client, tests and CI. It is a learning project, not a finished product, so I have tried to be honest below about what works and what does not.

## What's in the repo

- `AntiTheftApp.Api/` is the backend: ASP.NET Core 8, Entity Framework Core, PostgreSQL, JWT login and Firebase Cloud Messaging.
- `AntiTheftApp.Api.Tests/` holds the xUnit tests for the API.
- `AntiTheftApp-Portal/` is the React (Vite) web portal where the owner logs in and triggers the alarm.
- `AntiTheftApp.Mobile/` is the .NET MAUI Android app that receives the command and plays the alarm.

## How it works

1. You register with an email, a password and a short PIN. The password is for logging into the portal. The PIN is what you type on the phone itself to silence the alarm.
2. When you log in on the phone, the app sends its Firebase token to the API so the backend knows how to reach it.
3. In the portal you pick a device and press **Sound Alarm**.
4. The API first checks that the device really belongs to you, saves the command in the database (so there is a history of who triggered what and when), then sends a push message through Firebase.
5. The phone wakes up and a foreground service starts the alarm. There is also a boot receiver and Device Admin protection to make it harder to simply restart or uninstall the app.

## The API

| Method | Route | Needs login | What it does |
|--------|-------|-------------|--------------|
| POST | `/api/auth/register` | No | Creates an account and returns a token |
| POST | `/api/auth/login` | No | Logs in and returns a token |
| POST | `/api/devices/register` | Yes | Saves a phone and its Firebase token |
| GET | `/api/devices` | Yes | Lists your devices |
| POST | `/api/devices/{id}/commands` | Yes | Sends a command to one of your devices |
| POST | `/api/devices/{id}/security-event` | Yes | Lets the phone report something suspicious |

A few details I paid attention to:

- Passwords and PINs are hashed with BCrypt.
- Every device endpoint filters by the logged-in user, so nobody can trigger an alarm on someone else's phone just by guessing an ID.
- Login gives the same error for a wrong password and an unknown email, so it does not reveal which accounts exist.
- If Firebase fails to deliver, the real error goes to the server log only. The client just gets a generic message.

## Tests and CI

There are 28 automated tests. Some are unit tests for the controllers and the JWT service. The rest are integration tests that start the real ASP.NET pipeline in memory and walk through flows like register, add a device and trigger an alarm. They use an in-memory database and a mocked Firebase service, so you do not need Docker, Postgres or any credentials to run them:

```bash
dotnet test AntiTheftApp.sln
```

GitHub Actions runs on every push and pull request. It builds and tests the API, builds the portal, and checks that the API's Docker image builds. The workflow is in `.github/workflows/ci.yml`.

## Running it yourself

You will need the .NET 8 SDK, Node.js 20 or newer, a PostgreSQL database, and a Firebase project with Cloud Messaging enabled.

**1. Database.** Use any Postgres you like. If you want a local one, put a password in a `.env` file next to `docker-compose.yml` (it is gitignored) and start it:

```bash
echo "POSTGRES_PASSWORD=pick-something-strong" > .env
docker compose up -d
```

**2. API settings.** Create `AntiTheftApp.Api/appsettings.json`. It is gitignored, so it never gets committed:

```json
{
  "ConnectionStrings": { "DefaultConnection": "Host=...;Port=5432;Database=...;Username=...;Password=..." },
  "Jwt": {
    "Key": "a-random-secret-of-at-least-32-characters",
    "Issuer": "AntiTheftApp",
    "Audience": "AntiTheftApp.Client",
    "ExpiryMinutes": "1440"
  },
  "Firebase": { "CredentialsPath": "firebase-service-account.json" },
  "Cors": { "PortalOrigin": "http://localhost:5173" }
}
```

You can generate a JWT key with `openssl rand -base64 32`. For Firebase, download a service account key from Project Settings, then Service Accounts, and save it as `AntiTheftApp.Api/firebase-service-account.json` (also gitignored). The `.example` file next to it shows the shape. If you skip this, the API still starts, but sending a command will fail with a clear message.

Any setting can also come from an environment variable instead, for example `Jwt__Key`.

**3. Run the API.**

```bash
cd AntiTheftApp.Api
dotnet tool install --global dotnet-ef   # first time only
dotnet ef database update
dotnet run
```

Swagger is available at `/swagger` on the URL printed in the console.

**4. Run the portal.**

```bash
cd AntiTheftApp-Portal
cp .env.example .env     # set VITE_API_BASE_URL to your API address
npm install
npm run dev
```

**5. Mobile app.** Open `AntiTheftApp.Mobile` with the .NET MAUI workload installed, add your own Firebase `google-services.json`, set the API address in the app settings, and deploy to an Android phone (Android 8.0 or newer). Push notifications and the alarm need a real device, an emulator will not tell you much.

## Things I would not trust it with (yet)

I would rather be upfront about the limits:

- It does not survive a factory reset or a reset from recovery mode. No third-party app can do that. Google's Factory Reset Protection is the thing that actually helps there, so keep a Google account on the phone.
- Some phone makers (Xiaomi, Huawei, Oppo and others) kill background apps aggressively. On those phones you may have to allow Autostart and turn off battery optimisation for the app.
- The Device Admin warning is best effort. If a thief cuts the network first, the message never arrives.
- Commands have an `Acknowledged` status in the database, but the phone does not report back yet, so it never reaches that state.

## What I want to add next

- Let the phone confirm that it received a command
- Handle expired Firebase tokens properly
- Central error handling and structured logging
- Rate limiting on login
- Deploy the API and the portal

## Secrets

`appsettings.json`, the Firebase service account file, `google-services.json` and `.env` are all in `.gitignore`. If you fork this, use your own keys and never commit them.