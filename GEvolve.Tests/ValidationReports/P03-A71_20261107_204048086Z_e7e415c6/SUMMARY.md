# World generation validation report

- Run: `P03-A71_20261107_204048086Z_e7e415c6`
- Started UTC: 2026-11-07T20:40:48.0862373Z
- Finished UTC: 2026-11-07T20:57:00.8432537Z
- Branch: `worldgeneration_pangea`
- Validated commit: `46483ca299175c80ea2be0890bda8455f93305f7` - refactor(worldgen): introduce staged Pangaea generation session
- .NET SDK: `10.0.402`
- Validation set: `StagedPangaea`
- PowerShell: 5.1.26100.9711; OS: Microsoft Windows NT 10.0.26200.0; 64-bit OS: True
- Tracked source differs from validated HEAD: False
- The report/script changes are operational and excluded from the tracked-source comparison.

| Command | Status | Native exit code | Duration (seconds) | Complete log |
|---|---|---:|---:|---|
| `dotnet build` | PASS | 0 | 22.055 | [build.log](build.log) |
| `dotnet run --project GEvolve.Tests -- --pangaea-staged` | PASS | 0 | 134.734 | [pangaea-staged.log](pangaea-staged.log) |
| `dotnet run --project GEvolve.Tests -- --pangaea-elevations` | PASS | 0 | 56.682 | [pangaea-elevations.log](pangaea-elevations.log) |
| `dotnet run --project GEvolve.Tests -- --mountain-ranges` | PASS | 0 | 50.212 | [mountain-ranges.log](mountain-ranges.log) |
| `dotnet run --project GEvolve.Tests -- --pangaea-tectonic-audit` | PASS | 0 | 22.138 | [tectonic-audit.log](tectonic-audit.log) |
| `dotnet run --project GEvolve.Tests -- --pangaea-uplift` | PASS | 0 | 43.232 | [pangaea-uplift.log](pangaea-uplift.log) |
| `dotnet run --project GEvolve.Tests -- --all-suites` | FAIL | 1 | 641.101 | [all-suites.log](all-suites.log) |

## Reported suite totals
- all-suites: 15 suites passed; 2 suites failed.
Individual assertion/test totals are not inferred from PASS messages; this console runner does not report a framework test count.

## Compilation errors, failed suites and exception reasons
```text
SUITE FAIL: TectonicMotion: System.InvalidOperationException: Pre-motion terrain baselines differ in 4 scenario(s): Small/Pangaea/2026, Medium/Pangaea/2026, Large/Pangaea/2026, Enormous/Pangaea/2026
SUITE FAIL: ContinentalGeology: System.InvalidOperationException: Approved non-continental world baselines differ in 12 scenario(s): Small/Pangaea/2026, Small/Pangaea/42, Small/Pangaea/-731, Medium/Pangaea/2026, Medium/Pangaea/42, Medium/Pangaea/-731, Large/Pangaea/2026, Large/Pangaea/42, Large/Pangaea/-731, Enormous/Pangaea/2026, Enormous/Pangaea/42, Enormous/Pangaea/-731
```

## Warnings emitted
```text
    0 Warning(s)
```

## Actual diagnostic and regression evidence
Selected actual records; full decisions, distributions, prominence and H04 metrics are in pangaea-staged.log.
```text
STAGED_GRAPH_PASS Small: H04, surface, plates, grid transitive invalidations and save/load
STAGED_CASE_PASS Small/-1387338765: automatic=manual=original; BASE repeat preserves geometry; fullState=E10739D08DC71995BB6CF72D7B119090376D2563B98C84085B98D78ECC879B5F
STAGED_CASE_PASS Small/-132790047: automatic=manual=original; BASE repeat preserves geometry; fullState=7D3DF1BA3F6AD1C6A83C0B65E76C47900BA95BAD9DC369708D9440EBAABEBAE9
STAGED_CASE_PASS Small/-714216669: automatic=manual=original; BASE repeat preserves geometry; fullState=74D65E351E099FFA42A0B85CA865D60F6A2925915661991A00B43A36E0B1D9AB
STAGED_CASE_PASS Small/-2024691550: automatic=manual=original; BASE repeat preserves geometry; fullState=E0D6BEEFAF26C534F1069C944D458F1373FBCC6BE4F1B1E29A549E8973F4488A
STAGED_CASE_PASS Small/-174327693: automatic=manual=original; BASE repeat preserves geometry; fullState=093EE9ABAA01333B9DF3A3D97D4585E8B7035B9B9771955E44097E356068DC6E
STAGED_CASE_PASS Small/-1979952984: automatic=manual=original; BASE repeat preserves geometry; fullState=209A381DF68706304C1AF2EE0AD03C42FC8CA6F52CB5A88CCC227ECD562C9F70
STAGED_CASE_PASS Small/2076374193: automatic=manual=original; BASE repeat preserves geometry; fullState=208116414B80709145DC91DCB3FC12384FE36C2C90753FF5EFF53D840A1EE030
STAGED_OTHER_PASS Small/Continents/2026: fullState=EEDDCA55F8F7CBFC23FCF954D2EBFE22EB262CF8D31E83CD7F695AEC5F911F94
STAGED_OTHER_PASS Small/Archipelagos/2026: fullState=B70CB3B87D627B66B1DEC038E23709085C382E6413747104CFB2911419B37809
STAGED_OTHER_PASS Small/SmallContinents/2026: fullState=15998006E0E4DB9CDB80890F7D4DE3F2B8255BEBBB49ADDD1B04AA2E8E6151C9
STAGED_GRAPH_PASS Medium: H04, surface, plates, grid transitive invalidations and save/load
STAGED_CASE_PASS Medium/-1387338765: automatic=manual=original; BASE repeat preserves geometry; fullState=AF582F34DA90B4060EE19321C3A93D2824901F917A2FFA18CD0E8A23F9783324
STAGED_CASE_PASS Medium/-132790047: automatic=manual=original; BASE repeat preserves geometry; fullState=F1783F97DF71BE0EB5F94E4DE1E1AF201B5FA6C70616F243FAD261F0273D0055
STAGED_CASE_PASS Medium/-714216669: automatic=manual=original; BASE repeat preserves geometry; fullState=3A8C014CC9226EE3405969E66CBDBE0D15B967E0B5113C26806B269568716B26
STAGED_CASE_PASS Medium/-2024691550: automatic=manual=original; BASE repeat preserves geometry; fullState=DAF33B7855383E6C920237B67A62512FEA1B10CA93919AA8C443B58D6485E80D
STAGED_CASE_PASS Medium/-174327693: automatic=manual=original; BASE repeat preserves geometry; fullState=69EAD7048B9AD44C8BD521CE4914BDE0307B3BA63BC023AA74C96354352B5AFC
STAGED_CASE_PASS Medium/-1979952984: automatic=manual=original; BASE repeat preserves geometry; fullState=2E54CE928DFF4ACB3C9FC43C7C8CBD0346C5084F3D7F4D650838F489B8CBAC3E
STAGED_CASE_PASS Medium/2076374193: automatic=manual=original; BASE repeat preserves geometry; fullState=6FD320471E3FA0E8F41B03C4145FECAED7A08960BB95AD5353DE86DF14585267
STAGED_OTHER_PASS Medium/Continents/2026: fullState=7B141B5FDB744847BC04FABF3B95DF317F3285184FB0EECC8F9E8712DCF55EF4
STAGED_OTHER_PASS Medium/Archipelagos/2026: fullState=D7DB7D8E004042C7F422CB1E1EEC80482D5DCBD96D1C0A3674BB0A5E94097F7F
STAGED_OTHER_PASS Medium/SmallContinents/2026: fullState=9802CE0532F744CE706CA041C2053AC1E9E0C1FE1CD7788E1AF5930222419194
STAGED_GRAPH_PASS Large: H04, surface, plates, grid transitive invalidations and save/load
STAGED_CASE_PASS Large/-1387338765: automatic=manual=original; BASE repeat preserves geometry; fullState=FC6AA5BF55D2213C4B7EAA689FF035E4B00169BE7BCDBEA59B0D3F437F825C9C
STAGED_CASE_PASS Large/-132790047: automatic=manual=original; BASE repeat preserves geometry; fullState=EC512A51FE2F3951A32F2A3B212F00A04B6EE8E95A5AF9F8B5239279535B5D91
STAGED_CASE_PASS Large/-714216669: automatic=manual=original; BASE repeat preserves geometry; fullState=4DE99925BD53AC0CEA826913F3F1AD424D90B817353543A6BE8FA3452352D92D
STAGED_CASE_PASS Large/-2024691550: automatic=manual=original; BASE repeat preserves geometry; fullState=4343C718DD1A4504CE61B3E23837A45A1590BEC4917A2671D5DDF0DF197F6EA2
STAGED_CASE_PASS Large/-174327693: automatic=manual=original; BASE repeat preserves geometry; fullState=221507D0FB27E8364268FFF3C85FD752217AD18AFF7D2E34B2A9E5BDE45F2229
STAGED_CASE_PASS Large/-1979952984: automatic=manual=original; BASE repeat preserves geometry; fullState=C986886BB7F4D2EBDBCD628F85D305556D88819A79FE277A92CD869C43099C31
STAGED_CASE_PASS Large/2076374193: automatic=manual=original; BASE repeat preserves geometry; fullState=6AFABA060F9FBB890A82EB0540C28DE6EC0A0D744CAA3CC84765407312286BC4
STAGED_OTHER_PASS Large/Continents/2026: fullState=342BE17F2314CC4014C7E77E9F3F4030075C3F647425F8C7E214E27EAE09BC95
STAGED_OTHER_PASS Large/Archipelagos/2026: fullState=E893F8965A18BE8D7B2BC68025CFAE737097372E2DA966CBF11FB6BA315EF0B7
STAGED_OTHER_PASS Large/SmallContinents/2026: fullState=53EB98F8D1011F656E311AAB8A33871A3E604D1812F70438BE4665FA7ACBFB44
STAGED_GRAPH_PASS Enormous: H04, surface, plates, grid transitive invalidations and save/load
STAGED_CASE_PASS Enormous/-1387338765: automatic=manual=original; BASE repeat preserves geometry; fullState=F91B85E3A0BE51BA2EF11D1749684FE0D6F0247EC37FBD722781D4BF24FF5044
STAGED_CASE_PASS Enormous/-132790047: automatic=manual=original; BASE repeat preserves geometry; fullState=D43775D0EE80CAC777ED60E59A4887CA7DC41690E5F299D5F0E6A7B30409DF6F
STAGED_CASE_PASS Enormous/-714216669: automatic=manual=original; BASE repeat preserves geometry; fullState=1F277BF68A5C93A90A5C279EEF4C9176A0EBD9F31127C605E745BAD528090A93
STAGED_CASE_PASS Enormous/-2024691550: automatic=manual=original; BASE repeat preserves geometry; fullState=C4667A3D19EFEC348EEC44AE4AED092C7898B8E79DC82A5F5CBB98932AACC6B9
STAGED_CASE_PASS Enormous/-174327693: automatic=manual=original; BASE repeat preserves geometry; fullState=083A21E50C9CD18D51715D8AD880973DB98D8C25ACB1ADCB916BFE7849DADAC0
STAGED_CASE_PASS Enormous/-1979952984: automatic=manual=original; BASE repeat preserves geometry; fullState=33BF5D06539F199027D30C48CE598C0044BE7B303103C19D4C4B07BAE94E89D6
STAGED_CASE_PASS Enormous/2076374193: automatic=manual=original; BASE repeat preserves geometry; fullState=AD214965AD767DA69DA33738D50C5B585652D025CCE91E8EC6620D0E98D10C38
STAGED_OTHER_PASS Enormous/Continents/2026: fullState=DD5BA1E75DDE65AB68A1C470693D9E62F523099ABF03803581D4CFE44ED1113B
STAGED_OTHER_PASS Enormous/Archipelagos/2026: fullState=686EAAD1DF867EBCD5DAC23E8AE8ADD5F7BE1953D12632766B0982398380EB0F
STAGED_OTHER_PASS Enormous/SmallContinents/2026: fullState=6FEB1C7E3391FD53FA7424DC20A7B3A3F1118C4D410C971FF1A1B2E7C47528EC
STAGED_ASYNC_PASS: detached snapshots/exports; invalid requests atomic; cancellation; seed+size reset; obsolete results discarded; async stages complete
STAGED_PASS: 28 Pangaea acceptance worlds across four sizes + 12 unchanged other-type controls; exact original/automatic/manual equivalence, invalidation, persistence, snapshot isolation and async reset/cancellation
```
```text
P03-A Small/2026: 13 ranges, 268 axis cells, 6 ms P03-A, 542 ms total
P03-A Small/42: 7 ranges, 135 axis cells, 2 ms P03-A, 459 ms total
P03-A Small/-731: 17 ranges, 293 axis cells, 3 ms P03-A, 409 ms total
P03-A Medium/2026: 16 ranges, 347 axis cells, 5 ms P03-A, 728 ms total
P03-A Medium/42: 3 ranges, 61 axis cells, 2 ms P03-A, 704 ms total
P03-A Medium/-731: 6 ranges, 171 axis cells, 3 ms P03-A, 574 ms total
P03-A Large/2026: 5 ranges, 168 axis cells, 3 ms P03-A, 758 ms total
P03-A Large/42: 13 ranges, 346 axis cells, 4 ms P03-A, 868 ms total
P03-A Large/-731: 14 ranges, 383 axis cells, 9 ms P03-A, 1084 ms total
P03-A Enormous/2026: 4 ranges, 209 axis cells, 5 ms P03-A, 1651 ms total
P03-A Enormous/42: 11 ranges, 344 axis cells, 9 ms P03-A, 1798 ms total
P03-A Enormous/-731: 6 ranges, 214 axis cells, 8 ms P03-A, 1940 ms total
```
```text
SUITE PASS: World
SUITE PASS: Grid
SUITE PASS: Landmass
SUITE PASS: Biomes
SUITE PASS: Slope
SUITE PASS: Temperature
SUITE PASS: Precipitation
SUITE PASS: PlateBoundary
SUITE PASS: TectonicPlate
SUITE PASS: StagedLandmass
SUITE PASS: Game
BASELINE MISMATCH Small/Pangaea/2026
BASELINE MISMATCH Medium/Pangaea/2026
BASELINE MISMATCH Large/Pangaea/2026
BASELINE MISMATCH Enormous/Pangaea/2026
SUITE FAIL: TectonicMotion: System.InvalidOperationException: Pre-motion terrain baselines differ in 4 scenario(s): Small/Pangaea/2026, Medium/Pangaea/2026, Large/Pangaea/2026, Enormous/Pangaea/2026
APPROVED BASELINE MISMATCH Small/Pangaea/2026
APPROVED BASELINE MISMATCH Small/Pangaea/42
APPROVED BASELINE MISMATCH Small/Pangaea/-731
APPROVED BASELINE MISMATCH Medium/Pangaea/2026
APPROVED BASELINE MISMATCH Medium/Pangaea/42
APPROVED BASELINE MISMATCH Medium/Pangaea/-731
APPROVED BASELINE MISMATCH Large/Pangaea/2026
APPROVED BASELINE MISMATCH Large/Pangaea/42
APPROVED BASELINE MISMATCH Large/Pangaea/-731
APPROVED BASELINE MISMATCH Enormous/Pangaea/2026
APPROVED BASELINE MISMATCH Enormous/Pangaea/42
APPROVED BASELINE MISMATCH Enormous/Pangaea/-731
SUITE FAIL: ContinentalGeology: System.InvalidOperationException: Approved non-continental world baselines differ in 12 scenario(s): Small/Pangaea/2026, Small/Pangaea/42, Small/Pangaea/-731, Medium/Pangaea/2026, Medium/Pangaea/42, Medium/Pangaea/-731, Large/Pangaea/2026, Large/Pangaea/42, Large/Pangaea/-731, Enormous/Pangaea/2026, Enormous/Pangaea/42, Enormous/Pangaea/-731
SUITE PASS: PangaeaMountainRanges
SUITE PASS: PangaeaElevations
SUITE PASS: PangaeaUplift
SUITE PASS: PangaeaStaged
```

## Capture, privacy and retention
- Both streams are complete in separate labeled sections; cross-stream chronology is not asserted.
- Personal paths, URL credentials, recognizable secret assignments and emails are replaced by explicit markers; errors and numbers are retained.
- Sensitive-substring replacements across captured logs: 13.
- Review all files for additional sensitive data before publishing; this script does not commit or push.
- Keep milestone/failure reports and one canonical report per commit/SDK. Review routine successes manually after five; remove only redundant successes after review, never automatically. Git retains published history. See [retention policy](../README.md).

## Manual game validation and repeat command
The Game entry point creates EvolveWindow and calls window.Run(); it is interactive and was not launched. After a successful build, run `dotnet run --project GEvolve.Game`, inspect the required worlds and BASE/APORTE/FINAL/RELIEVE/PLACAS views, then close the window normally. Visual approval is not implied by these reports.

From the repository root, repeat this exact script snapshot:
```powershell
.\GEvolve.Tests\ValidationReports\P03-A71_20261107_204048086Z_e7e415c6\commands.ps1 -ValidationSet StagedPangaea -Label P03-A71
```
Each invocation creates a new UTC directory. Individual native exit codes are in each log and the table; this script exits 1 if any validation failed, otherwise 0.