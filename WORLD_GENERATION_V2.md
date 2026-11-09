# Evolve — Generación Mundial V2

**Rama de desarrollo:** `worldgeneration_v2`  
**Origen V1:** `worldgeneration_pangea`, commit `fd6fca73dd11d6a63dfa9a4ae6df086374561688`  
**Estado:** V2.0 — estructura inicial; algoritmos V2 aún no implementados.

## Propósito
Construir un pipeline mundial claro, modular y determinista, sin heredar los problemas de la elevación BASE. El resultado oficial tendrá **una sola elevación definitiva por hexágono**. Los campos intermedios de geología son diagnósticos, no una segunda elevación persistente.

## Principios
1. Una única fuente tectónica para V2: el H04 oficial (placas, movimientos e interacciones).
2. Superficie solo decide tierra/océano: no asigna alturas ni genera su propia cinemática tectónica en la arquitectura definitiva.
3. La geología define regiones y accidentes antes de componer elevaciones.
4. Un único orquestador público del juego. V2 es experimental y aislado hasta aprobación.
5. Cada etapa declara entradas, salidas y dependencias; ejecutar de nuevo una etapa invalida únicamente sus descendientes.
6. Mismos tamaños hexagonales, wrap E/O, límites N/S y semilla determinista.
7. No modificar esquemas de partidas ni baselines históricos durante la preparación.
8. No adoptar V2 sin compilación, regresión, equivalencia y revisión visual humana.

## Secuencia objetivo
| Etapa | Responsabilidad | Dependencias principales |
|---|---|---|
| 00 Configuración | Tamaño, tipo, semilla, reglas | — |
| 01 Cuadrícula | Celdas y topología hexagonal | 00 |
| 02 Placas | Partición de 16 placas | 01 |
| 03 Dinámica | H04: velocidades, convergencia, divergencia, cizalla | 02 |
| 04 Superficie | Tierra, océano, costas, estrategia por tipo | 01, 02, 03 si procede |
| 05 Regiones geológicas | Llanuras, cuencas, mesetas, macizos antiguos | 04, 03 |
| 06 Accidentes tectónicos | Cordilleras, fallas, rifts, volcanismo | 03, 04, 05 |
| 07 Elevación unificada | Un valor final de altura por celda | 04, 05, 06 |
| 08 Terreno y pendientes | Pendiente y derivados topográficos | 07 |
| 09 Temperatura | Latitud y altura definitiva | 07 |
| 10 Precipitaciones y humedad | Circulación/relieve/temperatura | 04, 07, 09 |
| 11 Hidrología | Drenaje, ríos, lagos | 07, 10 |
| 12 Biomas y subbiomas | Clasificación ecológica | 08–11 |
| 13 Recursos naturales | Minerales, flora y fauna | 04–12, catálogos |

El orden es el de la planificación; las dependencias finales serán un DAG explícito. No implica que todas las fases estén implementadas.

## Hitos de migración
- [ ] **V2.0** Auditoría completa, definición de contratos y estructura. Carpeta creada, auditoría pendiente.
- [ ] **V2.1** Pipeline experimental de etapas y estados, sin algoritmo nuevo.
- [ ] **V2.2** Reutilización de cuadrícula y H03 con equivalencia.
- [ ] **V2.3** Reutilización de H04 como única fuente tectónica V2.
- [ ] **V2.4** Estrategia de superficie de Pangea; preservar costas aprobadas, aislar legado.
- [ ] **V2.5** Regiones geológicas.
- [ ] **V2.6** Accidentes tectónicos; empezar por cordilleras.
- [ ] **V2.7** Elevación unificada sin BASE oficial.
- [ ] **V2.8** Pendientes y temperatura.
- [ ] **V2.9** Precipitación, hidrología y biomas.
- [ ] **V2.10** Recursos, persistencia, aprobación visual y retirada de V1.

## Restricciones inmediatas
- No tocar `master`, `worldgenerator` ni `worldgeneration_pangea`.
- V1 permanece funcional durante la migración; archivo histórico en `old/worldgeneration_v1/`.
- No reintroducir H05 cortical retirado.
- La superficie V1 utiliza velocidades internas heredadas: **no sustituirlas abruptamente**, porque alteraría costas aprobadas. Usar adaptador temporal hasta completar una comparación y aprobación.
- `Continents` sigue con superficie pendiente; no declararlo completo.
- No usar los dos fallos históricos de la suite como motivo para reajustar baselines.
- Ningún archivo de código V1 se elimina de su lugar operativo antes de que exista reemplazo, refactorización de referencias y pruebas ejecutables.

## Próximo paso
**V2.0a — Auditoría arquitectónica con Codex**: mapear dependencias reales y referencias por ruta, clasificar archivos para mover, reutilizar o eliminar, proponer contratos mínimos; **sin implementar nueva geografía**. Después revisar el plan con Chat Dev y aprobar V2.1.
