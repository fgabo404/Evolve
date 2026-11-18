# Menu interaction checks

Run from the repository root with the .NET 10 SDK:

```powershell
dotnet run --project .\tests\menu\Evolve.Menu.Checks.csproj --configuration Release
$LASTEXITCODE
```

The lightweight console suite prints each named result and returns `0` for a
passing suite or `1` when any check fails. It uses `dotnet run`, not `dotnet test`.

`Evolve.Menu.Checks.csproj` links the production `MainMenuInteraction`,
`MainMenuLayout`, `MainMenuButton` and `MainMenuActivationKey` source files.
There is no copied menu implementation, graphics dependency or test package.
`MenuCheckEntryPoint.cs` launches the suite; `MenuInteractionChecks.cs` contains
the input scenarios and assertions.

Coverage includes mouse ownership and invalid releases, pointer drag feedback,
selection and wrapping, Enter/Space matching and repeat suppression, cancellation
and focus reset, input takeover, unavailable notices, exit intent and real layout
hit bounds at compact, normal and high-DPI framebuffer dimensions.

Tests do not create a window, synthesize OS events or validate GPU resources.
See `../../game/audit.md` for build evidence, manual validation and limitations.
