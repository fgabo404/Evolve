# P03-A.8.1–8.2 — Variantes regionales, exclusivamente en Tests

El motor, WorldGenerationService, la sesión oficial, State y Game no se modifican. PangaeaBaseVariants consume un snapshot desligado obtenido con la instrumentación P03-A.7.4 (`captureBaseFields:true`). Solo devuelve matrices de diagnóstico; no genera superficie, H04, cordilleras, FINAL, clima/biomas ni un GeneratedWorld experimental. No se puede seleccionar una variante desde el juego ni guardarla como partida.

## Cadena de hipótesis

| Variante | Padre inmediato | Único factor conceptual cambiado |
|---|---|---|
| V0 | Oficial | Ninguno: reconstrucción exacta F/G/H desde campos y transformaciones vigentes |
| V1 | V0 | Retirar la presión heredada tanto del potencial como del soporte de cumbres |
| V1T | V1 | Suprimir ShapeUpperRelief/RecoverRegionalShoulders; mantener percentiles |
| V2 | V1T | Sustituir percentiles del mundo por una escala fija de amplitud teórica |
| V3 | V2 | Añadir compresión suave monótona de la banda superior |

V1T es un control necesario: los percentiles y ShapeUpperRelief fuerzan máximos por vías distintas. Sin el puente, cambiar normalización y desactivar el reescalado de cumbres en V2 confundiría dos factores.

Se reutilizan C (regional, frecuencia 5), D (local, frecuencia 15) y B (presión suavizada) ya capturados; no hay ruido/RNG nuevo. V0 usa `P=2.5B+1.3C+.45D`. V1/V1T/V2/V3 usan `P=1.3C+.45D`. V0/V1/V1T comparten el mapeo por percentiles oficial y sus cuantiles/niveles. V0/V1 ejecutan los métodos privados originales ShapeUpperRelief y RecoverRegionalShoulders mediante delegates resueltos una sola vez por reflexión en Tests. Un cambio de firma falla explícitamente; no se copia ese algoritmo ni se amplía el API productivo. V0 se exige idéntico a F/G/H oficiales byte a byte. El pequeño mapeo por percentiles, actualmente inline en Engine, se reproduce solo en el arnés y se protege por esa igualdad exacta.

V2/V3 usan la cota matemática `A=1.3+.45=1.75`, porque C,D∈[−1,1]. Altura continua `h=1+99*clamp((P+A)/(2A),0,1)`, con tierra oficial 1..100 y océano cero. No se adapta al máximo, histograma, tamaño ni semilla del mundo. P=0 corresponde a 50.5 antes de Round: es una hipótesis de escala continental, no una altitud físicamente calibrada. Puede subir tierras bajas y costas; se debe medir ese coste, no inferir calidad por la desaparición de blancos. El máximo teórico 100 sigue siendo posible; no se obliga a alcanzarlo ni se impone una cuota.

V3 conserva h≤60. Para h>60, `e=h−60` y `g(h)=60+e/(1+e/99)`. El 60 procede del hombro existente; el 99 es el intervalo terrestre 100−1, sin ajuste a porcentajes deseados. La función es continua con derivada 1 a ambos lados de 60, monótona y no tiene recorte horizontal. En el dominio [1,100] su extremo superior teórico es 88.4892…; puede conservar alturas ≥85, pero no fuerza ninguna. No aplica el antiguo Shape/Recover después, ni suavizado espacial. Ambos casos redondean una sola vez al producir alturas; V3 opera sobre h continua, no sobre una altura ya cuantizada.

Las métricas de celdas altas no bastan para recomendar una variante. Una transformación monótona no elimina en general contornos cerrados ni máximos de la superficie continua; comparar únicamente ≥85 escondería ese hecho. La evaluación numérica cubre distintos umbrales, pendientes terrestres, saltos de costa y morfología relativa al nivel de cada campo.

## Protección y estado

El editor experimental no es parte del flujo normal. Las matrices se liberan entre configuraciones; no hay caché permanente. Las operaciones sobre campos oficiales se verifican sin mutación. Los controles verifican reproducción V0, factores comunes, continuidad/monotonía de V3, rango y océano, ocho acciones visibles del laboratorio y FINAL oficial inalterado. Las comparaciones de doce configuraciones se añaden en P03-A.8.2.

P03-A.8.1: infraestructura implementada. La comparación P03-A.8.2 se añade en la siguiente entrega; variante definitiva pendiente de selección y aprobación visual. No se aprueba P03-A ni se inicia P03-A.8.3. El usuario revisa las recomendaciones antes de cualquier adopción.
