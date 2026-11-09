# P03-A.8.2c — Comparador BASE del laboratorio

**Comparador visual implementado, evaluación visual y selección pendientes.** Ninguna variante adoptada; BASE oficial intacta. No se aprueba P03-A ni se inicia P03-A.8.3.

Commit de implementación: 18856b46ed5a9c14bceca750d9b40bd0b2d8a848, feat(game): add experimental Pangaea base variant comparison. Rama worldgeneration_pangea. [SUMMARY.md](SUMMARY.md) conserva UTC, SDK, SHA, códigos y duraciones; [base-comparator.log](base-comparator.log) contiene la evidencia de los 64 campos y 16 mundos. La ejecución es técnica, sin ventana, GL, imágenes ni evaluación gráfica.

## Uso manual exclusivo del usuario

1. Abrir el Laboratorio de Pangea y ejecutar 01 placas/movimientos, 02 superficies y 03 BASE.
2. El selector «COMPARADOR BASE - EXPERIMENTAL» aparece entre Vista y Generación manual, dentro de la columna izquierda. Antes de BASE está ausente. La columna mantiene su scroll, inspector inferior y salida fija; sus rectángulos no invaden el mapa.
3. Alternar V0 — Oficial, V1 — Sin presión, V4O — Regional y V4U — Regional moderada. Se abre ALTURAS BASE, sobre la misma superficie, placas y H04. Primer cálculo en worker; se conserva el campo anterior y se anuncia CALCULANDO. Los siguientes cambios usan caché de sesión.
4. Leer BASE OFICIAL MOTOR y BASE VISIBLE en el inspector. Colores y números BASE corresponden a la opción activa; gris absoluto altura/100, océano azul. Las etiquetas solo aparecen cuando sus glifos caben legibles y completos en cada hexágono/viewport.
5. Cambiar a otra vista muestra exclusivamente datos oficiales. Continuar 04/05 y ecología usa BASE oficial incluso si la variante visible era otra.
6. Cambiar semilla/tamaño, reiniciar, repetir superficie/BASE o salir descarta comparaciones y restaura V0. Reentrar siempre empieza con V0.

La inspección, fluidez real y aprobación visual quedan exclusivamente a cargo del usuario. Codex no ejecutó Evolve gráficamente, no creó screenshots, PNG/SVG/CSV ni automatización visual. Las comprobaciones de layout/cámara/etiquetas fueron únicamente código y geometría.

## Arquitectura y fórmulas

Engine.World.Experimental.PangaeaBaseComparison contiene únicamente la función pura optativa de cuatro campos, desde el snapshot oficial ya capturado. No hace RNG, no regenera placas/superficie, no invoca el pipeline, no devuelve un GeneratedWorld y no muta entradas. Game activa la captura existente solo para el laboratorio; el automático no cambia. No hay dependencia Game/Engine → Tests ni reflexión desde Game. Tests conserva las implementaciones experimentales originales como referencia independiente.

- V0: copia exacta de snapshot.Base oficial; en presentación se usa directamente BASE oficial sin duplicar caché V0.
- V1: P=1.3C+0.45D; cuantiles/anchors vigentes, interpolación y redondeo originales; ShapeUpperRelief con soporte de presión cero y RecoverRegionalShoulders idénticos. Retira presión tanto del potencial como del soporte superior. Se reutilizan los dos cuerpos oficiales mediante acceso internal en vez de private: sus instrucciones y llamadas oficiales permanecen idénticas.
- V4O: h=clamp(32.5+(99/3.5)P,1,100); redondeo Math.Round al par al final.
- V4U: misma h continua V4O; identidad hasta 50, luego 50+(h−50)/(1+(h−50)/30), redondeo al final.
- Océano=0 en todos los campos; sin normalización visual por variante, sin taper V4C ni nuevas calibraciones.

[Justificación y fórmulas auditadas P03-A.8.2b](../P03-A82b_20261108_234756449Z_a6c3ecc0/REPORT.md). Las cinco variantes históricas y sus archivos/resultados no se reescriben.

PangaeaLaboratory posee la selección, tres arrays de caché y un resultado asíncrono pendiente. El worker recibe un snapshot separado; Update publica únicamente el task vigente. Elegir otra opción o invalidar desconecta el trabajo anterior y observa sus excepciones; una terminación obsoleta no tiene acceso a publicar. El cálculo ya iniciado puede terminar en segundo plano antes de liberar sus referencias; no modifica la sesión nueva.

PangaeaMapData guarda el campo visible separadamente de Snapshot. Solo Height/Color de ElevationBase lo usan; otras vistas y BASE oficial del inspector permanecen oficiales. Snapshot.Base nunca se reemplaza. UpdateLaboratory reutiliza el renderer al conservar revisión de cuadrícula/sesión; no cambia la cámara. El DAG y orden de acciones no reciben una nueva etapa. Los tipos de mundo, configuración de generación normal y contrato persistible no incluyen la variante.

## Cobertura técnica

La prueba específica ejecuta tres semillas principales (−1387338765, −132790047, 2026) y 42 independiente en cuatro tamaños: 16 mundos, 64 campos. Cada campo se compara tres veces con el código independiente anterior y su SHA256 publicado en el log inmutable P03-A.8.2b. V0 coincide además con BASE oficial.

Se comprueba superficie/océanos y palette BASE en todas las celdas; instantáneas/etapas antes y después de alternar son idénticas. Se conservan identidad de cuadrícula y selección. Se prueban glifos de la variante activa, ausencia total de etiquetas a zoom lejano e inspector visible/oficial. Para otras vistas se prueban todos los modos disponibles en una muestra determinista de celdas cada 37 IDs, complementada por las pruebas existentes del laboratorio. No es una comparación exhaustiva de cada píxel.

Se continúa el pipeline con V1 seleccionada y se exige igualdad exacta BASE/FINAL, clima, biomas y pendientes frente al automático oficial. Guardados actuales y geológicos históricos roundtrip sin estado experimental. Se cubren BASE/superficie repetidas, cambio de semilla y tamaño con caché, reinicio, reentrada, resultados pendientes descartados y última selección V0. Dieciséis cambios calientes por mundo no crean tasks ni copias de snapshots. Layout/hit testing: 1280×720, 800×600 y 640×480; todas las acciones oficiales siguen alcanzables por scroll y el dropdown queda dentro de la columna.

Los contratos del renderer/cámara se revisan estáticamente (misma revisión evita Fit/recreación) y mediante identidad de datos/algebra de cámara; no se crea renderer GL. No se certifican aquí eventos nativos ni apariencia/fluidez.

## Resultados por mundo y variante

Cada hash coincide exactamente con P03-A.8.2b. Kernel = mediana de tres muestras tras calentamiento, solo la función pura. Dispatch = tiempo del controlador para solicitar la selección; ready incluye thread pool, cálculo y sondeo técnico de 1 ms, no latencia de un frame de juego.

| Tamaño | Semilla | Variante | SHA256 alturas | Kernel mediana ms | Asignación mediana bytes | Dispatch ms | Ready ms |
|---|---:|---|---|---:|---:|---:|---:|
| Small | -1387338765 | V0 | 0068BB72CB8AA3B526702C274A7262BC32DCFF4C0D6028DD70B50513BBE43428 | 0.0833 | 26696 | 0.1632 | 0.1640 |
| Small | -1387338765 | V1 | 402EB9AD9F61629C72AEFCE9A085C01E85D061FAE9D7FA6FE129F5F3B6C8DD02 | 4.0433 | 912464 | 0.2123 | 27.5588 |
| Small | -1387338765 | V4O | 510B54068910978695B0EBE164D41EB6A91D46DCBA6C4C528D206672EFDDBEF5 | 1.3392 | 239520 | 0.0157 | 23.0092 |
| Small | -1387338765 | V4U | 19FE29E2956D21004956810D86E48BEDF9DA847AB8537944F62C86B593078DFE | 1.5091 | 239520 | 0.0202 | 20.5735 |
| Small | -132790047 | V0 | 5D201EDFEF4E68610B4181C2782B13BB3C5E46D3503643F40950CB4E5E4088D4 | 0.0817 | 26944 | 0.0123 | 0.0129 |
| Small | -132790047 | V1 | D649D2EF2190FCBB5CD29C2DB5221E462CBC90EA7DA384960400D227A5C50E3A | 7.4311 | 905360 | 0.0158 | 25.9399 |
| Small | -132790047 | V4O | 31A8B18DEC93DC47ADE6A0380BE58018288CE33206CA6BC081B995C74A04E15D | 1.1391 | 239520 | 0.0184 | 22.5986 |
| Small | -132790047 | V4U | E6F75C80EF35C539C7F645ED97C7D0F71925F9598D652424AF63F396C575221C | 1.1214 | 239520 | 0.0255 | 23.0312 |
| Small | 2026 | V0 | E62EB0A75091A91B1D52D54CE82EAA3D8A0310D96DE382F27E3D6005C9C49A9F | 0.0705 | 26696 | 0.0097 | 0.0106 |
| Small | 2026 | V1 | F7766259D4262114D18CB3FA496DCB5F2EA0865614D828084FDA3E0980003216 | 2.6765 | 905560 | 0.0124 | 19.4678 |
| Small | 2026 | V4O | F75A3E654327714B78733334B51D70DFE39262EE9C3BCCD4612D0F9F7F6E450A | 1.1920 | 239768 | 0.0186 | 19.3352 |
| Small | 2026 | V4U | 38CA54A40470D4729AD46371B93A935BC7A349440E1E9C185409793831BADA94 | 1.2252 | 239520 | 0.0129 | 19.4494 |
| Small | 42 | V0 | C199218E01D950F2DB860C17CBC6FBBF9903960D0F6670C4F435D18BA0BF3A47 | 0.0560 | 26696 | 0.0112 | 0.0121 |
| Small | 42 | V1 | 194F6F6F641A297921541A8027671883C6183A9E5EDC7AC851AC92BB66A1BC4C | 2.3329 | 922280 | 0.0096 | 19.3516 |
| Small | 42 | V4O | FA3E46240CEF294E7EAE9820898BCA7A90371FBE17EB395A57A27D4D1D062172 | 1.0396 | 239520 | 0.0087 | 22.7303 |
| Small | 42 | V4U | 4EEDA8D98DC60B53D6FCDF46C4BB038638A393A98F2611EA4B4B4A65BB523702 | 1.2777 | 239520 | 0.0092 | 24.3900 |
| Medium | -1387338765 | V0 | 03CA3ACE527FCE3BB75FF158FC1D75C1C3294211BBD48B375337A65EAD42A5DF | 0.0899 | 41848 | 0.0084 | 0.0098 |
| Medium | -1387338765 | V1 | E2E519CE22D499F903CCC255F6CEA4894E707EA3A46970AB95710C3D13DD79A0 | 3.9295 | 1420464 | 0.0146 | 32.4453 |
| Medium | -1387338765 | V4O | B33DCE0554CCDF809912DE4E8C14807981B1A98555A2614B651BAFEDC5E54666 | 1.6350 | 375872 | 0.0119 | 30.8038 |
| Medium | -1387338765 | V4U | 5DFAE5DCD7F92F59833733B3D1F2DCFB2E1FB475DBDD911F94047D2087A8197B | 1.7119 | 375872 | 0.0102 | 31.2217 |
| Medium | -132790047 | V0 | 74A08014CDEE823BDB9435012150617B24583FC64B6997159432740222AE58EB | 0.0850 | 41848 | 0.0091 | 0.0097 |
| Medium | -132790047 | V1 | DBCC0F57A1F13C910F01C8F147A7B1BC64322A94644F183990EEF33FF5F61EFC | 4.1025 | 1420464 | 0.0094 | 34.3192 |
| Medium | -132790047 | V4O | 9F24BEE48B3114AB7EB920FD1B2F9A84F489183B8E444C8D1319DA211BE58DEE | 1.9046 | 375872 | 0.0108 | 22.2546 |
| Medium | -132790047 | V4U | 4AA189C32B9FD01659AD1917DAAC16CEA91EC9F3940095E857E4CFFDD77A8F97 | 6.8103 | 375872 | 0.0130 | 27.3365 |
| Medium | 2026 | V0 | 22381BDA7029CA3EC1FDFE6BD6CF5C9BB371117E718286900AC7D394CC9FF98B | 0.1242 | 41848 | 0.0133 | 0.0143 |
| Medium | 2026 | V1 | 479C21367FDBF28D709633A9DAFADD4638203DD6DBC37DA391434C69B950A3AD | 3.8951 | 1420464 | 0.0212 | 25.0247 |
| Medium | 2026 | V4O | 71B8319CFFD29119A1FBFC3B46D2CD8C0B1994FC1121161174F87CBAD007E944 | 1.7144 | 375872 | 0.0130 | 24.2248 |
| Medium | 2026 | V4U | AA0F8160346325768BFB7CDE95712081DAF16DF7F8C1B937744003E02F3DF1D1 | 1.7513 | 375872 | 0.0122 | 27.6971 |
| Medium | 42 | V0 | FCF3646F8E90771073B18E742AF861BC8F3755880D98CCFA593E5B6874090BFF | 0.0752 | 41848 | 0.0095 | 0.0101 |
| Medium | 42 | V1 | 0C1860025027FB6C987B9D6AE6C860C9B7FF12B6ADD617D6BBD1DB67156A4C46 | 4.0697 | 1420464 | 0.0144 | 30.8848 |
| Medium | 42 | V4O | CE8DD5E408E3B8CE3F53EFD5EA1748EF5279C3FA806984EFDDD5539B007077C8 | 1.8459 | 375872 | 0.0151 | 30.3289 |
| Medium | 42 | V4U | F1D845D584980C483C8628CE57665447BD6189D06DE1B4FE13B838A942534FD9 | 1.7335 | 375872 | 0.0087 | 29.2921 |
| Large | -1387338765 | V0 | 1CE9C56E62F178E915C88864258ECAC6958926214A12D9B703E665015628BFE0 | 0.1373 | 60096 | 0.0091 | 0.0099 |
| Large | -1387338765 | V1 | 69F479905514E3CF895D754B5B57BFC0C8233BBF6D9F2BD9071D34270345C877 | 5.4364 | 2041400 | 0.0125 | 28.1996 |
| Large | -1387338765 | V4O | A69E870EA2EFB6F4F429B4AE2FDAC2098F16DDDA00589BCDE21D76A282E65511 | 2.7468 | 540120 | 0.0131 | 22.1740 |
| Large | -1387338765 | V4U | 6567398143EF10811CA2A9D037529817EFA311E24E1E9036F5DF3161FBF79B11 | 2.5596 | 540120 | 0.0152 | 29.8664 |
| Large | -132790047 | V0 | 2DF30795672AFCE53C9CAFF2A37B689660B74D34931E6E88215CE23BD8F8E9F9 | 0.1191 | 60096 | 0.0102 | 0.0108 |
| Large | -132790047 | V1 | 19BC57304ACD3F1893D21896220C519F076EE4C52EA3BBA896D7C55573634898 | 5.4888 | 2060440 | 0.0131 | 26.3654 |
| Large | -132790047 | V4O | 7FD7B38BE2BC3741D8FE65CDB885AD30346C14ECD6BFD72AE7C53CC4E1B13D3F | 2.5625 | 540120 | 0.0136 | 32.7655 |
| Large | -132790047 | V4U | EE5FF45F66629F54BF518DCF69FC24B04F8E329EBEC481BCD81ACFCBCDB47976 | 2.4774 | 540120 | 0.0094 | 20.2402 |
| Large | 2026 | V0 | 33646922AA766E29A561FAAB190C2BA297208C2B94A17F2D61B3D2CA608A106D | 0.1427 | 60096 | 0.0102 | 0.0109 |
| Large | 2026 | V1 | C9E31DC3A3473DA87C2241ABA248F1A3F75013856B514258B3212BE632265D44 | 6.8148 | 2111824 | 0.0156 | 34.3731 |
| Large | 2026 | V4O | 90D3EBC1D3E4F38442EC1C65C7539C64BAB052A2450953BA888D4ECACE4AC511 | 2.7537 | 540120 | 0.0110 | 30.7284 |
| Large | 2026 | V4U | B60B45DC48353D2484509D8DBF7BFF8AEE26508859114F3DFAF7A68B4B1426F9 | 2.6191 | 540120 | 0.0093 | 33.0233 |
| Large | 42 | V0 | 1D64730F60F1DFB2A9F477E4E41866D5960F6DB16F428BD916D2DEC1C08B91F3 | 0.2300 | 60096 | 0.0137 | 0.0144 |
| Large | 42 | V1 | A2CE740AAD1FEDC9A2DF8131DFE29F1882F980EA85B49BB0079BFCEB95A0BD00 | 6.5551 | 2040960 | 0.0147 | 27.5839 |
| Large | 42 | V4O | C1707142396AA708EF8F7B99719ABD6DBBB2862F31771386ED7E0C401D2FB957 | 2.5936 | 540120 | 0.0129 | 24.6437 |
| Large | 42 | V4U | 83053B573A3B1DA0A7091C63131617D42F4A9ADE876CB9A7033DB21547E25720 | 3.1345 | 540120 | 0.0168 | 31.7822 |
| Enormous | -1387338765 | V0 | 4CB2B3416C8C8D443D72EAC18439591E81EE02FBDC45F9697B02B9846A94C178 | 0.3762 | 106896 | 0.0116 | 0.0123 |
| Enormous | -1387338765 | V1 | A2C26E36F34A9207301EE30D217653220146049D142125FC57014240ED5D0E80 | 10.1906 | 3632200 | 0.0130 | 29.1145 |
| Enormous | -1387338765 | V4O | CF884ED8A97C01841F6E39981D8D08A414A6F63986D26DE048FAB8DA23087645 | 4.3242 | 961320 | 0.0120 | 27.3655 |
| Enormous | -1387338765 | V4U | AFF7AAF7646F7C4E4D8282100DFA469C76CBC497C170E0BCF27EE4E346301C9D | 5.1746 | 961320 | 0.0137 | 21.5450 |
| Enormous | -132790047 | V0 | 3B4E5208EB6444EE47A5F87D091B43E11349B703FD18963D329BBAB32E92219F | 0.2946 | 106896 | 0.0114 | 0.0121 |
| Enormous | -132790047 | V1 | AFE6C24E3185B4E7523097F7BB0938F4D92550EC415982F3DE1D499E54CE062E | 9.9365 | 3632360 | 0.0127 | 36.9782 |
| Enormous | -132790047 | V4O | D67C589AAE2A2F2A9322B3A006CB127E06234B5B828EA31569F1B471E1104E0B | 4.3237 | 961320 | 0.0109 | 31.7907 |
| Enormous | -132790047 | V4U | 8FB35E2A8F6C56C839EB30006BA5325A73B02B089FAEB0510FE273C3B3BCB166 | 4.6232 | 961320 | 0.0115 | 24.1494 |
| Enormous | 2026 | V0 | E5D47324C8C1C32992B7F04691D81A8519AB603D5AEF45CE7486E06B656597B8 | 0.2546 | 106896 | 0.0110 | 0.0116 |
| Enormous | 2026 | V1 | 05F2A07F40B5F2E782540C0E50DF2C62FE9206ABCC779C132EE0B909A11B714D | 10.4220 | 3632160 | 0.0146 | 34.6343 |
| Enormous | 2026 | V4O | 931BDBFB4EF4B71D46F03E8C7B5F411C03AECF5A045A1BCEA49EB2A6A0B2096F | 4.3586 | 961320 | 0.0111 | 27.9690 |
| Enormous | 2026 | V4U | 61E7703BA9CE5CB6B9D5B7307D3DED84B9DC2912E84A017F10F7834E2ACDFF08 | 4.6809 | 961320 | 0.0163 | 24.4900 |
| Enormous | 42 | V0 | 15DCDB42D3F26CB11CBAB55D248EA6F873785019D482438EFDF6608DA91BCF73 | 0.3093 | 106896 | 0.0118 | 0.0125 |
| Enormous | 42 | V1 | DBE11A005A28656745AD15C90F2EE32D4CB8E1C907DD68C21F4FFCFB0F87D8C1 | 10.4064 | 3632184 | 0.0125 | 28.3222 |
| Enormous | 42 | V4O | 46D9646CAFBB64983DA5CE1863DCE43DFFED5E32950595F699C12A1CE4F3CB35 | 7.0108 | 961320 | 0.0205 | 22.3925 |
| Enormous | 42 | V4U | 04CF1C6AA10D7670F9E7D6A74B99833FCB2460086008272184A883A5864CCDB8 | 4.3545 | 961320 | 0.0129 | 25.1939 |

## Caché, rendimiento y memoria

| Tamaño | Semilla | Payload caché bytes | Máximo cambio caliente ms | Mediana cambio caliente ms | Copias de snapshot añadidas |
|---|---:|---:|---:|---:|---:|
| Small | -1387338765 | 79800 | 0.0127 | 0.0009 | 0 |
| Small | -132790047 | 79800 | 0.0122 | 0.0007 | 0 |
| Small | 2026 | 79800 | 0.0098 | 0.0004 | 0 |
| Small | 42 | 79800 | 0.0077 | 0.0003 | 0 |
| Medium | -1387338765 | 125250 | 0.0078 | 0.0003 | 0 |
| Medium | -132790047 | 125250 | 0.0083 | 0.0003 | 0 |
| Medium | 2026 | 125250 | 0.0315 | 0.0003 | 0 |
| Medium | 42 | 125250 | 0.0074 | 0.0003 | 0 |
| Large | -1387338765 | 180000 | 0.0116 | 0.0006 | 0 |
| Large | -132790047 | 180000 | 0.0141 | 0.0004 | 0 |
| Large | 2026 | 180000 | 0.0087 | 0.0003 | 0 |
| Large | 42 | 180000 | 0.0085 | 0.0003 | 0 |
| Enormous | -1387338765 | 320400 | 0.0097 | 0.0003 | 0 |
| Enormous | -132790047 | 320400 | 0.0094 | 0.0004 | 0 |
| Enormous | 2026 | 320400 | 0.0118 | 0.0006 | 0 |
| Enormous | 42 | 320400 | 0.0122 | 0.0007 | 0 |

En Enormous, 106800 celdas: payload de tres arrays =320400 bytes (sin cabeceras/diccionario). El máximo observado del núcleo entre las cuatro semillas fue V0 0.3762 ms, V1 10.4220 ms, V4O 7.0108 ms y V4U 5.1746 ms. Asignaciones máximas de una llamada: V0 106896 bytes; V1 3632360; V4O/V4U 961320. Máximo despacho del controlador 0.0205 ms; cambio caliente máximo 0.0122 ms. Estos son datos de esta máquina/ejecución, no límites físicos ni aprobación visual.

V1 ordena valores terrestres: O(N log N); V4O/V4U son O(N). La caché es O(3N) bytes y se elimina al invalidar. La medición del núcleo excluye generación oficial, captura, snapshot/copia, hashes de pruebas, JSON, GL y etiquetas. Las asignaciones GC no son memoria retenida ni pico RSS. El controlador no copia snapshots al seleccionar una variante. Sí existe un coste adicional optativo de captura en el laboratorio: PangaeaBaseFields contiene once arrays double (payload 88N =9398400 bytes por copia en Enormous), aparte de arrays oficiales y diagnósticos existentes. Las copias posteriores de snapshots y una operación abandonada aún terminando pueden retener más de una copia. No se confunde esa memoria con los 320400 bytes de caché ni se asegura un pico total medido.

La evidencia respalda un coste técnico acotado en las configuraciones ensayadas: cálculos fríos fuera del hilo de ventana y selección caliente sin nuevo pipeline. La fluidez real, frames, consumo GPU y memoria máxima de una sesión interactiva no se midieron y corresponden a revisión del usuario.

## Archivos de implementación

- GEvolve.Engine/World/Experimental/PangaeaBaseComparison.cs
- GEvolve.Engine/World/LandmassGeneration/PangaeaFromPlatesGenerator.cs (solo private→internal en dos funciones, sin cambios de cuerpos)
- GEvolve.Engine/WORLD_GENERATION_ARCHITECTURE.md
- GEvolve.Game/Graphics/PangaeaMapData.cs
- GEvolve.Game/PangaeaLaboratory.cs
- GEvolve.Game/PangaeaLaboratoryWindow.cs
- GEvolve.Game/Screens/PangaeaLaboratoryLayout.cs
- GEvolve.Game/Screens/ScreenLayout.cs
- GEvolve.Game/PANGAEA_LABORATORY.md
- GEvolve.Tests/PangaeaBaseComparatorTests.cs
- GEvolve.Tests/Program.cs
- GEvolve.Tests/ValidationReports/commands.ps1

HexGridRenderer, MapViewMode y PangaeaHeightLabels se reutilizan sin cambios. No se alteran WorldGenerationService, PangaeaGenerationSession, State, Definitions, JSON, fórmulas oficiales, H04, máscara, sistemas, FINAL, ecología ni baselines. El commit documental añade informes, índice, ambos roadmaps y memoria operativa. El swap ajeno no se toca ni versiona.

## Repetición técnica

Desde la raíz, rama worldgeneration_pangea:

~~~powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .GEvolve.TestsValidationReportscommands.ps1 -ValidationSet BaseVisualComparator -Label P03-A82c -IsolatedArtifacts
~~~

BaseComparatorControls hace build+comparador solamente. BaseVisualComparator añade equivalencia 28+12, laboratorio y general. Cada ejecución crea su directorio UTC, conserva ambos streams/códigos completos y nunca inicia Game. Se aplican las reglas de privacidad/conservación existentes. Los resultados previos, incluido el fallo de compilación inicial, se preservan y distinguen de la ejecución canónica.
## Validación completa y fallos históricos

Ejecución UTC 2026-11-09 01:20:37–01:33:19, SDK 10.0.402; fuente versionada limpia al inicio, commit 18856b46ed5a9c14bceca750d9b40bd0b2d8a848. Códigos y streams completos en [SUMMARY.md](SUMMARY.md).

| Ejecución | Estado | Código | Segundos |
|---|---|---:|---:|
| Compilación | PASS, 0 errores /2 advertencias CS8602 en pruebas nuevas | 0 | 3.642 |
| Comparador: 64 campos /16 mundos | PASS | 0 | 77.352 |
| Equivalencia por etapas/sesiones: 28 Pangea +12 controles | PASS | 0 | 90.395 |
| Laboratorio: cuatro tamaños | PASS | 0 | 15.410 |
| General: 17 suites PASS /2 FAIL históricos | FAIL | 1 | 573.843 |

TectonicMotion conserva cuatro escenarios discrepantes; ContinentalGeology conserva doce. Los 32 registros «BASELINE MISMATCH»/«elevation» se compararon con P03-A.8.2b: 32 frente a 32, cero diferencias. Los 40 STAGED_EQ/STAGED_OTHER_PASS también son idénticos. No se actualizan ni ocultan baselines, excepciones o códigos. La suite general tiene código agregado 1; no se afirma que toda la suite pasó.

Las dos advertencias CS8602 estaban en lecturas Surface/Base de la prueba nueva, no en Game/Engine. Se corrigieron con guardas explícitas en el commit 8d20b2522713b71dddaceede7376fdef02c9cb78, fix(tests): validate completed comparator snapshot fields. La misma corrección refuerza la aserción final: se captura el hash previo del snapshot completo antes de alternar vistas, en vez de comparar dos referencias al mismo snapshot. No cambia código productivo, fórmulas, interfaz o baselines. [Validación focalizada posterior](../P03-A82c-final-controls_20261109_013431407Z_fc5ae8b0/REPORT.md).

La ejecución focalizada posterior identifica HEAD 8d20b25; el indicador de fuente modificada se debe únicamente a los roadmaps Markdown aún sin commit (WORLD_GENERATION_ROADMAP y PANGAEA_ROADMAP). Los .cs coinciden con HEAD; memoria e índice de informes son también documentación. La suite general no se repite después de esta corrección exclusiva de la prueba específica: su código productivo y sus 19 suites son los mismos ya ejecutados; se recompila y repite el comparador modificado. No se atribuye aquella ejecución general a otro SHA.

Se conservan [build inicial fallido](../P03-A82c-precheck_20261109_011724368Z_91580f80/REPORT.md) y [controles preliminares](../P03-A82c-controls_20261109_011751294Z_d3ce5758/REPORT.md), ambos sobre fuente todavía no comprometida del HEAD anterior. Ninguno sustituye el informe completo ni el precheck posterior a la corrección.
Validación final del commit de pruebas 8d20b2522713b71dddaceede7376fdef02c9cb78: build PASS, 8.985 s, cero errores/advertencias; comparador reforzado PASS, 85.813 s. Resultados íntegros en el informe focalizado enlazado arriba.
