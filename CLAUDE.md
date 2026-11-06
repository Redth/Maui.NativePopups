# CLAUDE.md

Maui.NativePopups is a .NET MAUI plugin that provides native menus, action sheets, alerts, sheets, popovers, pickers and date/time pickers. It uses UIKit on iOS / Mac Catalyst, Material 3 on Android and WinUI on Windows.

**Start here: [docs/PLAN.md](docs/PLAN.md).** It covers the decisions, the source map from the Pool Math reference implementation, the API design, the platform mapping, the hard-won gotchas, the milestone checklists and the dev and CI commands. Keep it up to date: tick checklist items and record new lessons as you go.

## Conventions

- **Targets:** `net10.0` and `net11.0` for portable, Android, iOS and Mac Catalyst, plus Windows when building on Windows. Use APIs available in both; guard newer OS APIs with `OperatingSystem.Is*VersionAtLeast`.
- **Platform code:** goes in `src/Maui.NativePopups/Platforms/Apple` (iOS + Mac Catalyst), `Platforms/Android` and `Platforms/Windows`. These are excluded per TFM in the csproj. Shared code stays portable.
- **Features:** every feature is on by default and can be switched off in `NativePopupsOptions`.
- **Look:** use each platform's own controls and animations; don't re-skin them to look alike.
- **Trimming / AOT:** the library is `IsAotCompatible` and `IsTrimmable`. Avoid reflection, and keep trim/AOT warnings at zero.
- **Resources:** hosted MAUI content must be a logical child of the window or presenting page, so DynamicResources resolve.
- **Android:** reuse native views and dialogs rather than rebuilding them on every show.
- **Do not modify `global.json`** unless asked. The SDK is pinned exactly because CI must match the MAUI workload set.

## Workflow

- One milestone per PR (see PLAN.md §7), targeting `main`.
- Each PR adds sample-app screens for its feature and screenshots on iOS, Mac Catalyst, Android and Windows, and needs green CI (GitHub-hosted runners).
- **Build:** `dotnet build src/Maui.NativePopups/Maui.NativePopups.csproj`.
- **Sample:** `dotnet build samples/NativePopups.Sample -f net11.0-<platform>`. Apple targets need `-p:ValidateXcodeVersion=false`.
