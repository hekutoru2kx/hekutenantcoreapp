# Tasks

## Pending

- [ ] **Move `BootstrapAdminEmail` out of `appsettings.json` into user-secrets** — `backend/Hekutenantcoreapp.Api/appsettings.json:10` holds a real personal email address, and that file is tracked and not gitignored. Fix: `dotnet user-secrets set "BootstrapAdminEmail" "<email>" --project backend/Hekutenantcoreapp.Api`, then set the value in `appsettings.json` to `""` (matching how `Jwt:Key`, `SmtpUser` and `SmtpPassword` are already handled there). `Program.cs` skips bootstrap-admin recovery when the value is empty, so a blank default is safe. Note the address is already in git history — blanking it stops further exposure but does not retroactively remove it. Same issue found in ludemia, gestamind and hekucoreapp.
- [ ] **Rename `backend/Hekutenantcoreapp.Infrastructure/Respositories/` → `Repositories/`** — the folder name is misspelled while the namespace declared inside the files already reads `Hekutenantcoreapp.Infrastructure.Repositories`. Do it before adding more repositories there, otherwise one namespace ends up split across two folders. Pure folder rename, no code change (SDK-style csproj globs `.cs`, so nothing references the path). Inherited through the fork chain — ludemia, hekucoreapp and gestamind all had the same typo.
- [ ] **Validate email during registration** — registration creates the Identity user from whatever address is typed (format-checked client-side only) and never confirms it's reachable/owned: no confirmation token, no `EmailConfirmed` gate, no "verify your email" step. ASP.NET Identity already exposes `GenerateEmailConfirmationTokenAsync`/`ConfirmEmailAsync`; the existing transactional-email pipeline (welcome/password-reset) can send the confirmation link too. Same gap found in ludemia, hekucoreapp and gestamind.

## Done

- [x] **Default tenant login** — admin-configurable setting so a user with a personal default tenant skips the post-login tenant-picker and logs straight into that tenant; admins can set/change a user's default tenant.
- [x] **Disable multitenant functionality** — admin-configurable setting to hide tenant-selection UI (registration, nav-bar) app-wide and route login/registration straight to a single default tenant, without locking out users who already belong to other tenants.
