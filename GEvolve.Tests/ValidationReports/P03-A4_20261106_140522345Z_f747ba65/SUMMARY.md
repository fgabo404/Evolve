# World generation validation report

- Run: `P03-A4_20261106_140522345Z_f747ba65`
- Started UTC: 2026-11-06T14:05:22.3450106Z
- Finished UTC: 2026-11-06T14:14:50.4626363Z
- Branch: `worldgeneration_pangea`
- Validated commit: `d0658fe889edb5c0754893e19d770e8d9ed1eb03` - refactor(worldgen): restore Pangaea mountain relief balance
- .NET SDK: `10.0.402`
- Tracked source differs from validated HEAD: False
- The report/script changes are operational and excluded from the tracked-source comparison.

| Command | Status | Native exit code | Duration (seconds) | Complete log |
|---|---|---:|---:|---|
| `dotnet build` | PASS | 0 | 6.380 | [build.log](build.log) |
| `dotnet run --project GEvolve.Tests -- --pangaea-elevations` | PASS | 0 | 39.124 | [pangaea-elevations.log](pangaea-elevations.log) |
| `dotnet run --project GEvolve.Tests -- --mountain-ranges` | PASS | 0 | 38.659 | [mountain-ranges.log](mountain-ranges.log) |
| `dotnet run --project GEvolve.Tests -- --all-suites` | FAIL | 1 | 482.917 | [all-suites.log](all-suites.log) |

## Reported suite totals
- all-suites: 13 suites passed; 2 suites failed.
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

## P03-A.4 elevation, mountain and regression evidence
Selected actual records; full distributions, prominence and H04 metrics are in pangaea-elevations.log.
```text
P03-A.4 Small/2026: generation=330.605 ms
  recovered base cells=1286, final cells crossing85 vs P03-A.3=31
  P03-A.3 BASE correlations: approximate tectonics=0.3663, regional noise=0.8021; composition limited=3
  SHA256 base=E62EB0A75091A91B1D52D54CE82EAA3D8A0310D96DE382F27E3D6005C9C49A9F, final=BFB903B89FE1547D7242A4C1A8A22DEB36E6BB81FFC0DDCA8923588258D65DA8
  SHA256 previous base=744E5DAB1FA285D42350AB7753DD41F0D31C49155C94E7FF038F02BDB5EFE334, previous final=175D54985EB4413E80F8435A9541B40234004D1AD05B244B7660B6736E1CD61C
  SHA256 P03-A.3 base=602E846BF0BDFB89ED038FE53A4CA082113F2E4D644CAD5D196C629A960761E2, P03-A.3 final=49A30C2EDC998E4780812522591DED44D43C931FF50EC375EA12278DFCAC6B95
  SYSTEMS P03-A.2: detected=13, persisted=13, axis cells=268, influence cells=1863 (17.493% land), final raised cells metadata=1659
  SYSTEMS P03-A.3: detected=13, persisted=13, axis cells=268, influence cells=1863 (17.493% land), final raised cells metadata=1663
  SYSTEMS P03-A.4: detected=13, persisted=13, axis cells=268, influence cells=1863 (17.493% land), final raised cells metadata=1663
  MOUNTAIN P03-A.4 FINAL: bands <40/40-60/61-75/76-84/85-94/95-100=6889/2191/959/512/95/4; corridor=978/437/178/195/75/0; outside=5911/1754/781/317/20/4
P03-A.4 Small/42: generation=257.533 ms
  recovered base cells=1270, final cells crossing85 vs P03-A.3=6
  P03-A.3 BASE correlations: approximate tectonics=0.4640, regional noise=0.8583; composition limited=0
  SHA256 base=C199218E01D950F2DB860C17CBC6FBBF9903960D0F6670C4F435D18BA0BF3A47, final=A03798A7B943C2426EDAA66253EBA204098368EDEAE7F8F917E49857A64BDD61
  SHA256 previous base=E174AEE0750490AE8A400C4581D71A38AE8BEDA75ADB510A1E986D403EB080E2, previous final=57851244BB31C7C635C64CBCA3E4C1566098CC7CD9C74F6C16167DCB339B41FB
  SHA256 P03-A.3 base=8B25BA7DA93602ED2B17B330C62F3D8FCFFC9252C56D9CE1D216C28C875651A4, P03-A.3 final=9052F0646CDC0C483DC925CCCBDC7811775BA784BF50AA4C9FC1CF6BDD08F829
  SYSTEMS P03-A.2: detected=7, persisted=7, axis cells=135, influence cells=1184 (10.308% land), final raised cells metadata=1089
  SYSTEMS P03-A.3: detected=7, persisted=7, axis cells=135, influence cells=1184 (10.308% land), final raised cells metadata=1089
  SYSTEMS P03-A.4: detected=7, persisted=7, axis cells=135, influence cells=1184 (10.308% land), final raised cells metadata=1089
  MOUNTAIN P03-A.4 FINAL: bands <40/40-60/61-75/76-84/85-94/95-100=7444/2344/1138/367/158/35; corridor=895/138/97/48/6/0; outside=6549/2206/1041/319/152/35
P03-A.4 Small/-731: generation=258.788 ms
  recovered base cells=884, final cells crossing85 vs P03-A.3=13
  P03-A.3 BASE correlations: approximate tectonics=0.4321, regional noise=0.8481; composition limited=29
  SHA256 base=CCC2E16277D3181CB9BFAC7A799AC81D354B0DA8CCE59FDCCCE428114C771C5F, final=2E286A59CA0C4E52B5ABAF0C50B3F7BF9A3C656005032689BDC72045BC5B62C6
  SHA256 previous base=8D0E36672F819613637361997237B18E8FC277D173DB1B8DDD75C4A5DC509C78, previous final=78A029B15134E78AC6109B591689D04C315D71FD2A9B2B53A35BC1F81C9D37D2
  SHA256 P03-A.3 base=4FBA3B67D98D961073BCF950AAC9DB64A22C82F6AB0810E03B2932A6927621AB, P03-A.3 final=58060FBEA9E729AD904CEE4A3AB22AFB21FF95FDF87CE05B03AF6F2BC7D646D4
  SYSTEMS P03-A.2: detected=17, persisted=17, axis cells=293, influence cells=2087 (19.615% land), final raised cells metadata=1904
  SYSTEMS P03-A.3: detected=17, persisted=17, axis cells=293, influence cells=2087 (19.615% land), final raised cells metadata=1919
  SYSTEMS P03-A.4: detected=17, persisted=17, axis cells=293, influence cells=2087 (19.615% land), final raised cells metadata=1919
  MOUNTAIN P03-A.4 FINAL: bands <40/40-60/61-75/76-84/85-94/95-100=6724/2564/800/382/159/11; corridor=1173/454/199/125/132/4; outside=5551/2110/601/257/27/7
P03-A.4 Medium/2026: generation=420.550 ms
  recovered base cells=1808, final cells crossing85 vs P03-A.3=35
  P03-A.3 BASE correlations: approximate tectonics=0.3974, regional noise=0.8485; composition limited=79
  SHA256 base=22381BDA7029CA3EC1FDFE6BD6CF5C9BB371117E718286900AC7D394CC9FF98B, final=83FF2B68A0BF44EB44896B0C83207DD8B44AC6C74DB3369006BB7A4D1013C0B6
  SHA256 previous base=E5A3E05A71DB898154D13228FEDE879F87DACC41FBD76FA901758153CB692A2F, previous final=8DD70A02E62E90C56922F0327A7D1FAA3CC0B0E1C5EA792B3D5EF37508DE465A
  SHA256 P03-A.3 base=1BB94431AC9ED826B3D044F20DCF77412FE096FF6F3A0CEE5F274D5390CF45CF, P03-A.3 final=2638D47A26F13F68CED59BBBA683532D961063A47270FD2F3A10C5CA399553AA
  SYSTEMS P03-A.2: detected=16, persisted=16, axis cells=347, influence cells=2971 (17.790% land), final raised cells metadata=2518
  SYSTEMS P03-A.3: detected=16, persisted=16, axis cells=347, influence cells=2971 (17.790% land), final raised cells metadata=2535
  SYSTEMS P03-A.4: detected=16, persisted=16, axis cells=347, influence cells=2971 (17.790% land), final raised cells metadata=2535
  MOUNTAIN P03-A.4 FINAL: bands <40/40-60/61-75/76-84/85-94/95-100=10659/3646/1259/793/336/7; corridor=1377/771/225/255/336/7; outside=9282/2875/1034/538/0/0
P03-A.4 Medium/42: generation=348.383 ms
  recovered base cells=1538, final cells crossing85 vs P03-A.3=2
  P03-A.3 BASE correlations: approximate tectonics=0.4832, regional noise=0.8915; composition limited=20
  SHA256 base=FCF3646F8E90771073B18E742AF861BC8F3755880D98CCFA593E5B6874090BFF, final=387BD246EFF6DA9E471433474363E7D35E8F00020E67C4B7AF211F55B5288FFA
  SHA256 previous base=01E66CFE8ECEDB927E77D13BD1916DB116CC54333D1D82047652E71870C6BC4B, previous final=E0C85374FAEBCE2C5EC484CE339410DAE8C85828255346262622D5E35B00CF6A
  SHA256 P03-A.3 base=1320E75E6E801A467D511D4F83ECB17F66B33E291B0DB9CE89618AFBD0A2132C, P03-A.3 final=D848085CEB631C5A953C9215E223B4F3B6F8CE1E649A2063782B9119FAC05CBD
  SYSTEMS P03-A.2: detected=3, persisted=3, axis cells=61, influence cells=863 (5.168% land), final raised cells metadata=665
  SYSTEMS P03-A.3: detected=3, persisted=3, axis cells=61, influence cells=863 (5.168% land), final raised cells metadata=676
  SYSTEMS P03-A.4: detected=3, persisted=3, axis cells=61, influence cells=863 (5.168% land), final raised cells metadata=676
  MOUNTAIN P03-A.4 FINAL: bands <40/40-60/61-75/76-84/85-94/95-100=10853/3648/1309/657/212/21; corridor=161/260/215/155/66/6; outside=10692/3388/1094/502/146/15
P03-A.4 Medium/-731: generation=387.592 ms
  recovered base cells=1958, final cells crossing85 vs P03-A.3=4
  P03-A.3 BASE correlations: approximate tectonics=0.3500, regional noise=0.9042; composition limited=18
  SHA256 base=DC1F35061A234AA1DA7BD5DC23AC6B9CA6AA1F1E14D6C08ACBE292D85E674866, final=9F69F20B0132F55784C0DBC37588EE68E3C296423AA0CF17DA60936E31C117DB
  SHA256 previous base=574911DE1F60EF8DB6E1F492DE663D96ACF3C6AD660A18ECDE20C42463BB177D, previous final=A96B735C17012E10BD22A1800323C6F1C535CB321D7367EC8C36357AD769C43F
  SHA256 P03-A.3 base=7AF8FCA322B264DAB35CDF9DD593157853A387941EBA52C1087D203B6869C174, P03-A.3 final=9D51FB267D1B25B385DE16AFDC938F6F92ACA09C16AE46556156665958E68410
  SYSTEMS P03-A.2: detected=6, persisted=6, axis cells=171, influence cells=1580 (9.268% land), final raised cells metadata=1407
  SYSTEMS P03-A.3: detected=6, persisted=6, axis cells=171, influence cells=1580 (9.268% land), final raised cells metadata=1422
  SYSTEMS P03-A.4: detected=6, persisted=6, axis cells=171, influence cells=1580 (9.268% land), final raised cells metadata=1422
  MOUNTAIN P03-A.4 FINAL: bands <40/40-60/61-75/76-84/85-94/95-100=11284/3439/1554/670/88/13; corridor=871/452/138/65/49/5; outside=10413/2987/1416/605/39/8
P03-A.4 Large/2026: generation=805.606 ms
  recovered base cells=2923, final cells crossing85 vs P03-A.3=20
  P03-A.3 BASE correlations: approximate tectonics=0.1613, regional noise=0.9479; composition limited=53
  SHA256 base=33646922AA766E29A561FAAB190C2BA297208C2B94A17F2D61B3D2CA608A106D, final=B567F380AF02DBCD5A9FFAE8EF4D5D4F62F260C3904F6474725A1D83C77791A7
  SHA256 previous base=4C755C9610C3959065B0A2D5918670A5B943C7FF709176D0FA4B0C3ECBE7D399, previous final=3D4D9BD8FD926C415B57273848B46FC57FC0B6B14091D430ABFC68EDFF284FE5
  SHA256 P03-A.3 base=496448F746BA259C84F9689655E65383D37169CC0AFDCD3091329B7400A48C0B, P03-A.3 final=7286BA1F22217EED18989E169A215D3AE3ED89780173A86BD659C23CC4C73A15
  SYSTEMS P03-A.2: detected=5, persisted=5, axis cells=168, influence cells=2402 (8.721% land), final raised cells metadata=1921
  SYSTEMS P03-A.3: detected=5, persisted=5, axis cells=168, influence cells=2402 (8.721% land), final raised cells metadata=1923
  SYSTEMS P03-A.4: detected=5, persisted=5, axis cells=168, influence cells=2402 (8.721% land), final raised cells metadata=1923
  MOUNTAIN P03-A.4 FINAL: bands <40/40-60/61-75/76-84/85-94/95-100=17959/5602/2694/1105/164/19; corridor=1284/461/321/185/132/19; outside=16675/5141/2373/920/32/0
P03-A.4 Large/42: generation=786.236 ms
  recovered base cells=2055, final cells crossing85 vs P03-A.3=95
  P03-A.3 BASE correlations: approximate tectonics=0.4918, regional noise=0.8698; composition limited=161
  SHA256 base=1D64730F60F1DFB2A9F477E4E41866D5960F6DB16F428BD916D2DEC1C08B91F3, final=D785193FF7855E0F4ACB9CFEF0A71B0CE01D15F3420402EBF55EDC46A1CAA713
  SHA256 previous base=19A3E5F0E85E6756D23096A52EBD1B4E01B716D2D26CC5BC84CA993A65E0CED1, previous final=CE507C397AC91FAB59E2B131469C2E12C3AED2FCDC6A7E7E3711D417E379B2E4
  SHA256 P03-A.3 base=33894DE8F55C9E475C4F3D729004F17103F5B8C273F6945E04EF9418A4BEFC0D, P03-A.3 final=973DBF31B260CE691F78180C6F379862DC7165255622B02929DC217D706F6586
  SYSTEMS P03-A.2: detected=13, persisted=13, axis cells=346, influence cells=4864 (20.267% land), final raised cells metadata=4020
  SYSTEMS P03-A.3: detected=13, persisted=13, axis cells=346, influence cells=4864 (20.267% land), final raised cells metadata=4076
  SYSTEMS P03-A.4: detected=13, persisted=13, axis cells=346, influence cells=4864 (20.267% land), final raised cells metadata=4076
  MOUNTAIN P03-A.4 FINAL: bands <40/40-60/61-75/76-84/85-94/95-100=15291/5729/1659/846/455/20; corridor=2223/1234/490/476/432/9; outside=13068/4495/1169/370/23/11
P03-A.4 Large/-731: generation=796.572 ms
  recovered base cells=2798, final cells crossing85 vs P03-A.3=20
  P03-A.3 BASE correlations: approximate tectonics=0.2289, regional noise=0.9523; composition limited=36
  SHA256 base=7D6848EC90B8C078C245A16AC8AB2DC4FAEB1002F2BDFBAECB48E23E85C6964F, final=F641BFC1742F5C8BA2D6B442C7E2ADB5724BD72724F751F4D3C06DA608BB1EB7
  SHA256 previous base=BC617D30B85F8FFFE2D6D6827834C7398AAE653CC57945E0C5A0135B9BB1E9B8, previous final=B20FBA85C7004080DCC61243C43B2A1C089FC7A01E1A56C608EEAC466912355B
  SHA256 P03-A.3 base=BA44561BBE2AD6771E03B9DACBD3429E2FAE69D5EEC9B2405E2E8F75334A15CE, P03-A.3 final=9315FDCAD08090B1DECCF9BC08C3AF800472A74EA91BAE89BA424B0E9F8C867C
  SYSTEMS P03-A.2: detected=14, persisted=14, axis cells=383, influence cells=5065 (21.104% land), final raised cells metadata=4265
  SYSTEMS P03-A.3: detected=14, persisted=14, axis cells=383, influence cells=5065 (21.104% land), final raised cells metadata=4274
  SYSTEMS P03-A.4: detected=14, persisted=14, axis cells=383, influence cells=5065 (21.104% land), final raised cells metadata=4274
  MOUNTAIN P03-A.4 FINAL: bands <40/40-60/61-75/76-84/85-94/95-100=16387/3809/2398/1236/152/18; corridor=2915/1056/659/309/111/15; outside=13472/2753/1739/927/41/3
P03-A.4 Enormous/2026: generation=1628.665 ms
  recovered base cells=5284, final cells crossing85 vs P03-A.3=86
  P03-A.3 BASE correlations: approximate tectonics=0.3312, regional noise=0.9121; composition limited=274
  SHA256 base=E5D47324C8C1C32992B7F04691D81A8519AB603D5AEF45CE7486E06B656597B8, final=E7DD80D5E51EB84464AC829BBE1B9C11E53C772982BDC971C58F58C2E0654D59
  SHA256 previous base=559C805E4B476698A5A49F68D7A6406769C4CF5CAC9399D36FD61EBC7C33A20D, previous final=FACE94A9B6180C1016F1B242D1D95813CD6B4EE18FFA4FEDA053161B9A5380D9
  SHA256 P03-A.3 base=43B1BC014BB92ECBB46A6230BA7936EC0983B56D3DEDBE4378A9669A6BA0CF9E, P03-A.3 final=918D0BE0840BCB121339BF51416530281B9E293B436C39D6E3373701F0C15BC6
  SYSTEMS P03-A.2: detected=4, persisted=4, axis cells=209, influence cells=3567 (8.350% land), final raised cells metadata=2911
  SYSTEMS P03-A.3: detected=4, persisted=4, axis cells=209, influence cells=3567 (8.350% land), final raised cells metadata=2957
  SYSTEMS P03-A.4: detected=4, persisted=4, axis cells=209, influence cells=3567 (8.350% land), final raised cells metadata=2957
  MOUNTAIN P03-A.4 FINAL: bands <40/40-60/61-75/76-84/85-94/95-100=27362/9289/3760/1674/618/17; corridor=1235/594/600/570/564/4; outside=26127/8695/3160/1104/54/13
P03-A.4 Enormous/42: generation=1646.675 ms
  recovered base cells=4089, final cells crossing85 vs P03-A.3=92
  P03-A.3 BASE correlations: approximate tectonics=0.4375, regional noise=0.8606; composition limited=53
  SHA256 base=15DCDB42D3F26CB11CBAB55D248EA6F873785019D482438EFDF6608DA91BCF73, final=9EBFA1CF39EBE23953B6219247BFF9E4C3668B47D1E2FCE75F74407D334A1B74
  SHA256 previous base=F1B49C66A27A5864636D61FF4E7BAAC8B3BB6E289DFEFA7C15849CCA3371FE02, previous final=366F88AE618007C413E7B6DDAD1F636D398FF29DA10DF7E49F0FB9ECD6665AA7
  SHA256 P03-A.3 base=1F43BA0010637B65CF364F217A567A2703BD4995E8148C19AE12FFA074826250, P03-A.3 final=18B36AE8F0C121E3BF02AECD44D212605160C111019A77248798C3935D1FA9E6
  SYSTEMS P03-A.2: detected=11, persisted=11, axis cells=344, influence cells=5434 (12.720% land), final raised cells metadata=4399
  SYSTEMS P03-A.3: detected=11, persisted=11, axis cells=344, influence cells=5434 (12.720% land), final raised cells metadata=4420
  SYSTEMS P03-A.4: detected=11, persisted=11, axis cells=344, influence cells=5434 (12.720% land), final raised cells metadata=4420
  MOUNTAIN P03-A.4 FINAL: bands <40/40-60/61-75/76-84/85-94/95-100=26443/10912/3266/1516/579/5; corridor=1754/1616/717/779/563/5; outside=24689/9296/2549/737/16/0
P03-A.4 Enormous/-731: generation=1625.153 ms
  recovered base cells=5415, final cells crossing85 vs P03-A.3=7
  P03-A.3 BASE correlations: approximate tectonics=0.2515, regional noise=0.9255; composition limited=10
  SHA256 base=6B4494BEAB3300BA6D84DDE4E0D65701BDB0C16D4C2EA747877B1A307B7C869A, final=4F938FE60969F9182375B2663434DCEDE1D2929A0AC7B655B22C7A8FF717348C
  SHA256 previous base=63BB11C163DE3F753E3A82864F4F676D9B9A9EA919FF9E4C0C8966F675E2B969, previous final=1236B1A93F384FB712F9EEE5E14CAD72C201BF7DBBF37E3FF617085504E1C131
  SHA256 P03-A.3 base=C06466D3F0D7CDA895AFFDE8CD4E29439CEE1EB160CB311AF343E7B7A790455D, P03-A.3 final=D184BE89A727EE26662EF4E5628FB04090E891116F112BF8621D15A144F3B01E
  SYSTEMS P03-A.2: detected=6, persisted=6, axis cells=214, influence cells=4051 (9.483% land), final raised cells metadata=3209
  SYSTEMS P03-A.3: detected=6, persisted=6, axis cells=214, influence cells=4051 (9.483% land), final raised cells metadata=3220
  SYSTEMS P03-A.4: detected=6, persisted=6, axis cells=214, influence cells=4051 (9.483% land), final raised cells metadata=3220
  MOUNTAIN P03-A.4 FINAL: bands <40/40-60/61-75/76-84/85-94/95-100=28680/8143/4112/1700/81/4; corridor=2942/609/295/133/72/0; outside=25738/7534/3817/1567/9/4
P03-A.4 Enormous/1324918567: generation=1599.291 ms
  recovered base cells=4153, final cells crossing85 vs P03-A.3=38
  P03-A.3 BASE correlations: approximate tectonics=0.2421, regional noise=0.9051; composition limited=189
  SHA256 base=D596BEC57408ADD3417BC3EB883765626606509B57E4C6BD2682D263DAF3BABA, final=DCCC9871600005714509C0B6272BB19C714AAE433401C603C6D071687C1F0CC6
  SHA256 previous base=C57F9882137BD1EF281FA0F47780D18D96040EDCE49E88F648CCD7E489E3DC3F, previous final=C951CC99B2E2A046D901F73E5BAB78BE790680E7537B2FAB56DD8519141424D8
  SHA256 P03-A.3 base=2A2FC6E6BB5AC83D68F47851FC111F2A0527FC9EFF6199EF1AD815FB600AC4F2, P03-A.3 final=D6DA7B030623CFBDF665B7CCF21B5A54F4935C31F5778A42058E0CA5C4B819B3
  SYSTEMS P03-A.2: detected=8, persisted=8, axis cells=326, influence cells=5825 (13.635% land), final raised cells metadata=4970
  SYSTEMS P03-A.3: detected=8, persisted=8, axis cells=326, influence cells=5825 (13.635% land), final raised cells metadata=5005
  SYSTEMS P03-A.4: detected=8, persisted=8, axis cells=326, influence cells=5825 (13.635% land), final raised cells metadata=5005
  MOUNTAIN P03-A.4 FINAL: bands <40/40-60/61-75/76-84/85-94/95-100=27914/9030/3576/1680/473/47; corridor=3977/957/457/102/285/47; outside=23937/8073/3119/1578/188/0
P03-A.4 Enormous/418579367: generation=1523.832 ms
  recovered base cells=4957, final cells crossing85 vs P03-A.3=22
  P03-A.3 BASE correlations: approximate tectonics=0.3625, regional noise=0.9158; composition limited=132
  SHA256 base=16A22CA0996EBB44F0DA77CF24B254EDB1C2B5F451CEC8DF1A9C45823B7E3FA7, final=0C2E7ABE231F83E0AE2B0E455F91D60BEF32E02EFB0CC253545A4F618E084359
  SHA256 previous base=B7FA6FD76B800DB78819CE08C62C1B931CA722449A615246D98783A0F0C6ED80, previous final=E2B1C060FDB37EC0FCB6CE34804836F22AB61A228FE21D61DD89C4DBAA2529BE
  SHA256 P03-A.3 base=14AAE04775175D0D827D5E69994011FBD577877515101DA802709C596CE703A2, P03-A.3 final=E96242561343423CA445C22BD799C5BE48118EABE405746841C4F003F370E2E8
  SYSTEMS P03-A.2: detected=13, persisted=13, axis cells=513, influence cells=9109 (21.138% land), final raised cells metadata=7335
  SYSTEMS P03-A.3: detected=13, persisted=13, axis cells=513, influence cells=9109 (21.138% land), final raised cells metadata=7399
  SYSTEMS P03-A.4: detected=13, persisted=13, axis cells=513, influence cells=9109 (21.138% land), final raised cells metadata=7399
  MOUNTAIN P03-A.4 FINAL: bands <40/40-60/61-75/76-84/85-94/95-100=27630/9327/4421/1259/406/49; corridor=5716/2200/575/338/255/25; outside=21914/7127/3846/921/151/24
P03-A.4 Enormous/1697481406: generation=1529.886 ms
  recovered base cells=4869, final cells crossing85 vs P03-A.3=125
  P03-A.3 BASE correlations: approximate tectonics=0.4139, regional noise=0.8772; composition limited=58
  SHA256 base=13C1C4122B0BADC2D9DE4CB121E178E2ECDCFD6BF19FC0CD7BAA9695B90CC58D, final=57EF8E0A73664EA7D0C117647FFD6AD2DB8975BB90048576A14C9BAB9861DA19
  SHA256 previous base=9E07D44BD9D84B2DC425AE5B48E48D529D020C87302F45DC5260E8FE072AAD3B, previous final=4A07E4ACEC907CD60A4EBA16F932781E5526701F0D3A8258074375BDED72CBF9
  SHA256 P03-A.3 base=1C4AB23E42F301F8961C0AB1F716D3AD87A7AC66D6B38B57BA75613B80DBD8D4, P03-A.3 final=BB19EA9575C673C18844FC3EA2CFCB11C91EB63651DFD200B9D13D23405ADAA9
  SYSTEMS P03-A.2: detected=18, persisted=18, axis cells=696, influence cells=12233 (28.635% land), final raised cells metadata=9986
  SYSTEMS P03-A.3: detected=18, persisted=18, axis cells=696, influence cells=12233 (28.635% land), final raised cells metadata=10014
  SYSTEMS P03-A.4: detected=18, persisted=18, axis cells=696, influence cells=12233 (28.635% land), final raised cells metadata=10014
  MOUNTAIN P03-A.4 FINAL: bands <40/40-60/61-75/76-84/85-94/95-100=27612/8903/3803/1927/469/6; corridor=6334/2973/1283/1168/469/6; outside=21278/5930/2520/759/0/0
P03-A.4 Enormous/1242668747: generation=1576.775 ms
  recovered base cells=5936, final cells crossing85 vs P03-A.3=64
  P03-A.3 BASE correlations: approximate tectonics=0.3261, regional noise=0.9164; composition limited=137
  SHA256 base=265C59DF7AD8F1D425EA2F3FB339D2190C0364982121FD0963A307097E3324D2, final=E01CB9E8684E1CE3CA48B9132E39BB458D91664E38F155510E8D9717BF45BE4C
  SHA256 previous base=455D062D64DFBB9FE60F84E506538F3FE7721289C8BDCE4A8D924632E25FF518, previous final=F93BD93942085E0206296E27300B5EB4B158D256DDAE7913D7D8855D3E2CAECC
  SHA256 P03-A.3 base=F72F580637F822F8C54FFBF836B870480B3C3566D17EF37D372CA6ACD1D29A4F, P03-A.3 final=F542B8AAFF44372C455A90230189F313ED2778B6BF15ED83AF59AEEB551A3290
  SYSTEMS P03-A.2: detected=4, persisted=4, axis cells=197, influence cells=3936 (8.893% land), final raised cells metadata=3210
  SYSTEMS P03-A.3: detected=4, persisted=4, axis cells=197, influence cells=3936 (8.893% land), final raised cells metadata=3259
  SYSTEMS P03-A.4: detected=4, persisted=4, axis cells=197, influence cells=3936 (8.893% land), final raised cells metadata=3259
  MOUNTAIN P03-A.4 FINAL: bands <40/40-60/61-75/76-84/85-94/95-100=28935/8562/4733/1534/481/16; corridor=1847/935/427/336/381/10; outside=27088/7627/4306/1198/100/6
P03-A.4 Large/-234020843: generation=789.725 ms
  recovered base cells=1656, final cells crossing85 vs P03-A.3=32
  P03-A.3 BASE correlations: approximate tectonics=0.4906, regional noise=0.7750; composition limited=24
  SHA256 base=AF710D3073F3C783BE33A12761BF797B905B5A37E718058262B5607A94759208, final=1438D064D0F309730D5D1CB6805C5055785C286625C0DB8788AF16FD0B2D68BB
  SHA256 previous base=A179647154F61C02A28D43A7D7E7E462350C2865C76F32E7F1F0CAF3DDF29F86, previous final=4104A00E25844AADD521FC0D4EE40E054CF34D5148BE6B30EB68071745352D37
  SHA256 P03-A.3 base=9EA279D75F9BB630ED50C3C8BE7C76427A998A5976E5EF434046B69F114703BC, P03-A.3 final=E5422E6C0910A562C718EA5D2A3A1039AE2A2A8418FD91A4386EF2011B4EF631
  SYSTEMS P03-A.2: detected=12, persisted=12, axis cells=323, influence cells=3914 (16.303% land), final raised cells metadata=3232
  SYSTEMS P03-A.3: detected=12, persisted=12, axis cells=323, influence cells=3914 (16.303% land), final raised cells metadata=3247
  SYSTEMS P03-A.4: detected=12, persisted=12, axis cells=323, influence cells=3914 (16.303% land), final raised cells metadata=3247
  MOUNTAIN P03-A.4 FINAL: bands <40/40-60/61-75/76-84/85-94/95-100=15836/5579/1447/867/271/8; corridor=1792/1049/374/428/264/7; outside=14044/4530/1073/439/7/1
P03-A.4 Large/-690013300: generation=802.758 ms
  recovered base cells=1836, final cells crossing85 vs P03-A.3=96
  P03-A.3 BASE correlations: approximate tectonics=0.5987, regional noise=0.7789; composition limited=160
  SHA256 base=92A73CE1C603FC509FE0D87EA8889D8471DE7C50A20513F8313E56858ABE739E, final=82EB81BA038006A3A456481D75C50AAE219266565830977E9B77D63709137CF0
  SHA256 previous base=CCF3AC0741F80BBCF3A3ABC00775016B2DD9231EB57037FAF7556415545BD93B, previous final=86C980B3062AA8130BEB260B38109B747CA1D06CF639A8A237874282AEB559F6
  SHA256 P03-A.3 base=5CC1E0FB991D731D4837A4938C908B27456C6CF7580EC0FD1D8749743AF1AD2F, P03-A.3 final=839649F46149B7B91E3709FA02932C762EDC9F4451BF3A79BD63EFA1FB94E54D
  SYSTEMS P03-A.2: detected=19, persisted=19, axis cells=642, influence cells=6356 (26.475% land), final raised cells metadata=5499
  SYSTEMS P03-A.3: detected=19, persisted=19, axis cells=642, influence cells=6356 (26.475% land), final raised cells metadata=5524
  SYSTEMS P03-A.4: detected=19, persisted=19, axis cells=642, influence cells=6356 (26.475% land), final raised cells metadata=5524
  MOUNTAIN P03-A.4 FINAL: bands <40/40-60/61-75/76-84/85-94/95-100=15480/5519/1485/922/594/8; corridor=2604/1642/824/738/540/8; outside=12876/3877/661/184/54/0
P03-A.4 Large/689804754: generation=789.776 ms
  recovered base cells=2506, final cells crossing85 vs P03-A.3=34
  P03-A.3 BASE correlations: approximate tectonics=0.4578, regional noise=0.8588; composition limited=13
  SHA256 base=86FCC5DF92A7AA520013CE7EE5D4C1B2D8AF291F473F950F6467FF17E5523404, final=1817E0580CEC0FF145EDB3C632E1D5F51C4DF3C2D63D6ABD1A7419EBB7EB1E5B
  SHA256 previous base=0AFDE5F8D916E3565090B1F30CD388CA4598942CCF0CBB901A566515E6B1CB51, previous final=353507A0778712944386F128B67FD37CA004EA9865ED15E9349E04C68A5E3CAC
  SHA256 P03-A.3 base=98BA2EEE4312BFC4174C9A1177BC1BE704F134A510FE1411B42F529B815E2D3C, P03-A.3 final=F7610C5B481C9319B93F95FC3FEBB89E5124C1B1EBBB5E564ABA5B52D2BFD954
  SYSTEMS P03-A.2: detected=20, persisted=20, axis cells=552, influence cells=6436 (25.802% land), final raised cells metadata=5283
  SYSTEMS P03-A.3: detected=20, persisted=20, axis cells=552, influence cells=6436 (25.802% land), final raised cells metadata=5328
  SYSTEMS P03-A.4: detected=20, persisted=20, axis cells=552, influence cells=6436 (25.802% land), final raised cells metadata=5328
  MOUNTAIN P03-A.4 FINAL: bands <40/40-60/61-75/76-84/85-94/95-100=16454/5046/2258/989/191/6; corridor=3315/1774/759/398/184/6; outside=13139/3272/1499/591/7/0
P03-A.4 Large/1195510322: generation=874.384 ms
  recovered base cells=3063, final cells crossing85 vs P03-A.3=24
  P03-A.3 BASE correlations: approximate tectonics=0.2950, regional noise=0.9366; composition limited=93
  SHA256 base=E05887CFE267F380E99F030763401C1C390D422DD6B072464DDDD04E650AD58C, final=D54EECDF06ACE53A5F1A364FFCC8A3FD4349DE0E9489557870E1029986CE9BC1
  SHA256 previous base=FA1118C3EDB877AD0D8B4ED58F598DA9B8520C598C3DF80DBBD5C07A84C053B5, previous final=852416FC2EEB2901BBB5F0FE51A33D0880185026764D5D52388B82F982EA32D9
  SHA256 P03-A.3 base=FE292A0661D557DAA82B70B38795F818614756BECC3F91B6687D3BC8571C34B0, P03-A.3 final=66E95AB27188FB48349E1B56269F8F8C0BD18060D834EEEAE1249E6068829EBA
  SYSTEMS P03-A.2: detected=8, persisted=8, axis cells=171, influence cells=2500 (10.417% land), final raised cells metadata=1970
  SYSTEMS P03-A.3: detected=8, persisted=8, axis cells=171, influence cells=2500 (10.417% land), final raised cells metadata=1992
  SYSTEMS P03-A.4: detected=8, persisted=8, axis cells=171, influence cells=2500 (10.417% land), final raised cells metadata=1992
  MOUNTAIN P03-A.4 FINAL: bands <40/40-60/61-75/76-84/85-94/95-100=15867/4572/2361/980/180/40; corridor=1116/630/290/288/141/35; outside=14751/3942/2071/692/39/5
P03-A.4 Large/1443340172: generation=788.702 ms
  recovered base cells=3067, final cells crossing85 vs P03-A.3=9
  P03-A.3 BASE correlations: approximate tectonics=0.3200, regional noise=0.9539; composition limited=30
  SHA256 base=40E7184AF58A830E3A541DB3833E14F07467FC7F9EE5497D88E408E679B0A5B5, final=1D7F863A79E4413BC8C3DABFF6E24F34517206BC2421E053BB323E109371B27D
  SHA256 previous base=CF04692F6533AF8A8FB800A26F3245B387D46F3DFFEB3FEA8CE818D53940AF20, previous final=44941BA408B005E25F763693A1762F9457DD4969A6CEA890B5115BD733304C80
  SHA256 P03-A.3 base=599E0249EBDA890CDC99B3987EEFB7018C6B7E1C5BAE04EFDD17B1018511DFF8, P03-A.3 final=A9721971B5665B0B9A348D8A7DD12B5565580E3FE37A6AE284F5C6549963E2A7
  SYSTEMS P03-A.2: detected=7, persisted=7, axis cells=131, influence cells=1882 (7.842% land), final raised cells metadata=1484
  SYSTEMS P03-A.3: detected=7, persisted=7, axis cells=131, influence cells=1882 (7.842% land), final raised cells metadata=1516
  SYSTEMS P03-A.4: detected=7, persisted=7, axis cells=131, influence cells=1882 (7.842% land), final raised cells metadata=1516
  MOUNTAIN P03-A.4 FINAL: bands <40/40-60/61-75/76-84/85-94/95-100=15708/4819/2504/791/172/6; corridor=1076/357/139/191/115/4; outside=14632/4462/2365/600/57/2
P03-A.4 Large/-185224545: generation=822.014 ms
  recovered base cells=2366, final cells crossing85 vs P03-A.3=2
  P03-A.3 BASE correlations: approximate tectonics=0.3014, regional noise=0.9313; composition limited=55
  SHA256 base=C4EF7BD64E19E46F9557A0EC6AAD050C2C0B1EE2716E98AF5909BE71F0BA4A9A, final=68F5621FC2C541CB113D492135E05CAF11D7E4505D4E9E3BE1E0B4AEEA48548E
  SHA256 previous base=AF03A7586F5138EA3CB3ADE97C34E4C86AF01869CD8711A2989B82F6319943D7, previous final=8B6C89985054AE921630E947211B14577EDABD7DA49D3873BAB4241311E19053
  SHA256 P03-A.3 base=A2783DD473FF24247CAD527F27A3922D829AB29AD2C1387FB43C21863EEC9F05, P03-A.3 final=0943073C385925EEB9848A9D63D315800DDDA5BB0A227061F081E8B70400729B
  SYSTEMS P03-A.2: detected=7, persisted=7, axis cells=137, influence cells=1995 (8.312% land), final raised cells metadata=1538
  SYSTEMS P03-A.3: detected=7, persisted=7, axis cells=137, influence cells=1995 (8.312% land), final raised cells metadata=1586
  SYSTEMS P03-A.4: detected=7, persisted=7, axis cells=137, influence cells=1995 (8.312% land), final raised cells metadata=1586
  MOUNTAIN P03-A.4 FINAL: bands <40/40-60/61-75/76-84/85-94/95-100=15684/5405/1740/963/187/21; corridor=1012/276/186/316/184/21; outside=14672/5129/1554/647/3/0
```
```text
P03-A Small/2026: 13 ranges, 268 axis cells, 2 ms P03-A, 304 ms total
P03-A Small/42: 7 ranges, 135 axis cells, 1 ms P03-A, 212 ms total
P03-A Small/-731: 17 ranges, 293 axis cells, 1 ms P03-A, 198 ms total
P03-A Medium/2026: 16 ranges, 347 axis cells, 2 ms P03-A, 345 ms total
P03-A Medium/42: 3 ranges, 61 axis cells, 1 ms P03-A, 547 ms total
P03-A Medium/-731: 6 ranges, 171 axis cells, 3 ms P03-A, 545 ms total
P03-A Large/2026: 5 ranges, 168 axis cells, 2 ms P03-A, 820 ms total
P03-A Large/42: 13 ranges, 346 axis cells, 3 ms P03-A, 816 ms total
P03-A Large/-731: 14 ranges, 383 axis cells, 5 ms P03-A, 799 ms total
P03-A Enormous/2026: 4 ranges, 209 axis cells, 5 ms P03-A, 1540 ms total
P03-A Enormous/42: 11 ranges, 344 axis cells, 7 ms P03-A, 1691 ms total
P03-A Enormous/-731: 6 ranges, 214 axis cells, 5 ms P03-A, 1591 ms total
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
.\GEvolve.Tests\ValidationReports\P03-A4_20261106_140522345Z_f747ba65\commands.ps1
```
Each invocation creates a new UTC directory. Individual native exit codes are in each log and the table; this script exits 1 if any validation failed, otherwise 0.

## Post-run publication review

- The complete all-suites output reports 16 elevation-field mismatches (4 in TectonicMotion and 12 in ContinentalGeology), including overlapping scenarios. It reports zero attempt, surface or plate-field mismatches. No baseline was updated and no generation code was changed.
- Residual-path/email/recognizable-credential scans found no matches in the four logs or this summary. The commands.ps1 snapshot contains generic redaction regexes, not personal paths or credentials.
- Only the six required report text files, the reusable script and the retention/procedure README are included. Build output and the pre-existing unrelated swap file are excluded.
- Build and test success does not approve P03-A.4 visually. The interactive game was not launched.
