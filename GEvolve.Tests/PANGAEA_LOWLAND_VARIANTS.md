# P03-A.8.2b — Presión factorial, escala regional y costa

Todo reside en Tests. El motor, sus etapas, superficie/H04, FINAL, clima/biomas, persistencia y UI no cambian. V0/V1/V1T/V2/V3 conservan fórmulas y resultados: el arnés contrasta los 60 hashes de altura/potencial y los objetos de métricas originales de P03-A.8.1–8.2. Los logs históricos no se reescriben.

## Diseño fijado antes de medir

Se conservan los cinco controles y se añaden solo seis variantes:

| ID | Referencia | Cambio | Hipótesis | Riesgo |
|---|---|---|---|---|
| VP | V0 | B en potencial, no en soporte | Separar presión del potencial manteniendo regla de cumbres | Persisten bandas y máximo forzado |
| VS | V1 | B solo en soporte | Aislar soporte con potencial sin presión | Puede remarcar contactos heredados |
| V4O | V2 | Centro vertical de 50.5 a 32.5 | Referencia baja/intermedia fija | Suelo 1 puede producir mesetas |
| V4S | V4O | Escala de 99/3.5 a 25 | Menor amplitud sin ajustar cada mundo | Puede perder tierras altas útiles |
| V4U | V4O | Compresión superior continua >50 | Conservar llanuras y moderar cumbres | No elimina contornos cerrados por sí sola |
| V4C | V4U | Taper costero contextual | Bajar solo costas bajas/suaves, proteger altas/abruptas | Terrazas por distancia y cuantización; proxies no equivalen a geomorfología real |

No es una búsqueda de parámetros. Las constantes se fijan desde los anclajes y coeficientes ya existentes; la semilla 42 se reserva como control independiente, sin reajustes después de observarla. Se ejecutan las tres semillas principales en cuatro tamaños (12) y 42 en cuatro tamaños (4): 16 configuraciones ×11 variantes =176, de las cuales 132 corresponden al conjunto obligatorio.

## Presión: factorial 2×2

V0=(potencial sí, soporte sí), VP=(sí,no), VS=(no,sí), V1=(no,no). Todos usan las reglas percentiles, ShapeUpperRelief y RecoverRegionalShoulders originales mediante los delegates existentes. Se comprueba que VP mantiene potencial/anclas/F de V0 y VS los de V1. Los extremos del factorial reproducen exactamente V0/V1. El contraste de potencial incluye el reajuste de anclas que su propia distribución provoca bajo la misma regla Q; no confundirlo con retirar B manteniendo anclas congeladas.

## Escala, desplazamiento y compresión

P=1.3C+.45D, con los mismos campos capturados. En V2, h=50.5+(99/3.5)P para el dominio teórico original. En V4O, h=clamp(32.5+(99/3.5)P,1,100): solo se desplaza 18 niveles respecto a V2. El centro 32.5 es (15+50)/2, mitad del intervalo bajo/intermedio vigente, no la media ajustada de las semillas.

V4S conserva centro 32.5 y usa h=clamp(32.5+25P,1,100). La escala 25=(80−15)/(2×1.3) asigna el ancho 65 del intervalo regional vigente a la amplitud primaria regional ±1.3. El detalle .45 permanece añadido; no se cambia su peso. Es una hipótesis de escala vertical, no una cota física. Tanto el desplazamiento como la pendiente son iguales para todas las semillas y tamaños; no se adapta el máximo a 100.

V4U parte de h continua V4O: identidad hasta 50; para e=h−50>0, u=50+e/(1+e/30). El 50 es el ancla intermedia; 30=80−50 es el ancho superior regional vigente. Es C1 y monótona, sin recorte horizontal. Se redondea al final; no se aplica Shape/Recover después. V4S y V4U son dos ramas desde V4O, no un cambio simultáneo de escala y compresión.

## Transición costera V4C

Se usa d=distancia hexagonal a la primera celda terrestre costera (d=0 allí). La distancia al océano es d+1. R=min(columnas,filas)/(2×15), media longitud de onda del ruido local primario D existente: 4.433/5.567/6.667/8.9 pasos en los cuatro tamaños. No se toma una muestra de ruido nueva.

Sea h la altura continua V4U y r=max|h−h_vecino| sobre vecinos terrestres reales. s(x)=t²(3−2t), t=clamp(x,0,1). Factores:

- w=1−s(d/R), soporte compacto; fuera de R no se cambia nada.
- a=1−s((h−15)/(49−15)), protección por altura. 49=50−1 deja margen de cuantización y preserva exactamente cualquier altura redondeada ≥50.
- b=1−s(r/35), protección por relieve; 35=50−15, intervalo bajo/intermedio. Si r≥35 no se cambia nada.
- h_c=1+(h−1)*(1−0.5*w*a*b), con salida idéntica a h si d≥R, h≥49 o r≥35.

El factor 0.5 es un límite experimental explícito: retener al menos la mitad del exceso sobre nivel terrestre 1 incluso en el caso más activo. No fija una franja a altura 1 ni impone la misma reducción a todos los litorales. Las curvas de protección son suaves y el taper termina con derivada cero. La altura y el salto terrestre son proxies: no certifican que toda costa montañosa real quede clasificada correctamente. Se miden nueva igualdad a lo largo de líneas de distancia, corridas de altura/depth constante y distribuciones por profundidad; el redondeo/distancia discreta puede aumentar terrazas y debe evaluarse, sin declarar aprobación visual.

Las franjas principales usan distancia real al océano: primera celda=1, agregado 1–3 (incluye deliberadamente la primera), 4–8 e interior≥9. También se conserva el campo de distancia a costa de P03-A.8 anterior; no se cambia su convenio ni sus métricas históricas. Se publican niveles exactos 1..8 y saltos de aristas terrestres cercanas al litoral, separados de saltos tierra–océano.

## Validación y uso

Se reutilizan métricas previas de altura/morfología/H04/pendientes y se añaden bandas de elevación, mesetas de nivel 1 y proxies de terrazas. Los parámetros no se ajustan con los resultados; si fallan se documenta el riesgo. Ninguna variante se guarda ni se ejecuta en Evolve.

Desde la raíz:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\GEvolve.Tests\ValidationReports\commands.ps1 -ValidationSet BaseLowlandVariants -Label P03-A82b -IsolatedArtifacts
```

BaseLowlandControls ejecuta build+controles; BaseLowlandComparison build+comparación; BaseLowlandVariants añade equivalencia, laboratorio y general. Las mediciones del núcleo excluyen captura y análisis; se registran aparte costes costeros, muestras de memoria y entradas oficiales. No se infiere memoria retenida de las asignaciones GC.

**P03-A.8.2b: calibración experimental completada; selección y aprobación visual pendientes.** El código está preparado para validar; la evidencia real se publica separadamente al terminar. No se sustituye BASE oficial ni se inicia P03-A.8.3.
