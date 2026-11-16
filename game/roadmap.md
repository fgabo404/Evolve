# Evolve — Roadmap de la interfaz base

**Rama:** `architecture`  
**Área:** `game/`  
**Estado general:** PROPUESTA — pendiente de aprobación del responsable del proyecto  
**Fecha:** 2026-11-16

## 1. Objetivo

Construir una aplicación gráfica mínima, ejecutable y mantenible para Evolve, con un **menú principal azul de estilo pixelado y botones con relieve**, inspirado en la estética de los menús de Minecraft pero con recursos y diseño propios.

Esta fase es **solo la base de la interfaz**. No incluye generación de mundos, cuadrícula, mapas, simulación, guardado ni lógica del juego.

### Resultado visual esperado

- Fondo en tonos azules oscuros, con detalles discretos.
- Título **EVOLVE** grande y destacado.
- **Versión del juego en texto pequeño inmediatamente debajo del título**, leída de los metadatos de compilación; nunca duplicar la versión en un texto fijo del menú.
- Cuatro botones, en este orden y con estas etiquetas exactas: **INICIAR**, **LAB**, **OPCIONES**, **SALIR**.
- Botones rectangulares con relieve/bordes marcados, tipografía legible de estilo pixelado y estados de interacción.
- Diseño centrado que se adapte al cambio de tamaño de ventana.
- Sin copiar texturas, fuentes ni otros recursos protegidos de Minecraft.

## 2. Elección técnica pendiente

**Propuesta del Arquitecto:** utilizar **C# / .NET 10 + OpenTK 4.x**, porque la versión anterior de Evolve (`WorldGenerationOLD`) ya usaba OpenTK 4.9.4 para la ventana y la representación 2D.

Esta elección requiere **aprobación explícita del responsable** antes de empezar a programar. El código anterior podrá servir como referencia conceptual, pero **no se migrarán componentes antiguos ni se reintroducirán dependencias de Engine/State/Definitions sin autorización**.

El sistema gráfico debe estar encapsulado en `game/`, con una estructura interna mínima, sin abstraer prematuramente un motor gráfico completo.

## 3. Roadmap por etapas

### UI-01 — Proyecto gráfico mínimo

**Estado:** Pendiente.

**Dev / Codex:**
1. Tras la aprobación de la biblioteca gráfica, crear el proyecto C# ejecutable dentro de `game/`.
2. Configurar los metadatos de versión en un único lugar; inicialmente se propone **0.0.1**, sujeto a confirmación.
3. Abrir una ventana titulada `Evolve`, con un fondo azul sólido.
4. Manejar correctamente el cierre de la ventana y la liberación de recursos.

**Criterios de aceptación:** la aplicación inicia, muestra la ventana y termina limpiamente; no depende de `engine/`, `state/`, `saves/` ni `definitions/`.

### UI-02 — Presentación del menú

**Estado:** Pendiente.

**Dev / Codex:**
1. Dibujar el título `EVOLVE` en grande.
2. Mostrar la versión real del ensamblado inmediatamente debajo, en tamaño menor.
3. Dibujar los cuatro botones, centrados y en el orden aprobado.
4. Aplicar una identidad visual en tonos azules, relieve rectangular y estilo pixelado original.
5. Garantizar alineación, legibilidad y escalado al redimensionar la ventana.

**Criterios de aceptación:** título, versión y las cuatro opciones se muestran correctamente, sin recortes ni solapamientos en tamaños de ventana razonables.

### UI-03 — Interacción básica

**Estado:** Pendiente.

**Dev / Codex:**
1. Implementar estados visuales normales, al pasar el cursor y al pulsar.
2. Implementar clic y navegación básica con teclado, sin colocar lógica de simulación en la interfaz.
3. `INICIAR`, `LAB` y `OPCIONES` deben informar que aún no están disponibles; **no** simular funcionalidades inexistentes ni crear pantallas adicionales.
4. `SALIR` debe cerrar la aplicación de forma ordenada.

**Criterios de aceptación:** las cuatro opciones responden de forma coherente, no desencadenan lógica de juego y `SALIR` funciona.

### UI-04 — Verificación y entrega

**Estado:** Pendiente.

**Dev / Codex:**
1. Incorporar verificaciones automatizadas sencillas de cualquier lógica de navegación comprobable sin GPU, si la implementación lo permite sin añadir complejidad artificial.
2. Documentar cómo compilar y ejecutar el menú mediante comandos PowerShell para el responsable del proyecto.
3. Entregar el resumen de archivos cambiados, mensaje y SHA completo del commit, así como limitaciones conocidas.

**Auditor:**
1. Revisar que el alcance se limita a la interfaz base.
2. Verificar que no hay reglas de simulación en `game/` ni cambios innecesarios en otros módulos.
3. Revisar evidencia de funcionamiento, aspecto visual, comportamiento de botones, escalado y cierre, sin afirmar pruebas que no hayan sido ejecutadas.

**Criterios de aceptación:** el Auditor emite su valoración y el responsable del proyecto decide si aprueba la etapa.

## 4. Límites y gobierno

- **Arquitecto:** define y ajusta el diseño y roadmap conforme a decisiones aprobadas.
- **Dev / Codex:** implementa solo etapas autorizadas.
- **Auditor:** comprueba los criterios de aceptación y presenta hallazgos.
- **Responsable del proyecto:** aprueba el diseño, autoriza las etapas y acepta los resultados.
- Se trabaja **una etapa a la vez**.
- No modificar `architecture.MD`, otros módulos o este roadmap sin la autorización correspondiente.
- **No avanzar hacia la generación de mundos** dentro de este roadmap.

## 5. Decisiones que deben cerrarse antes de UI-01

- [ ] Biblioteca gráfica: aprobar OpenTK u otra alternativa.
- [ ] Versión inicial visible: aprobar `0.0.1` u otro valor.
- [ ] Aprobar el diseño y dar autorización al Dev para comenzar UI-01.

**Esta documentación es un plan de trabajo; su publicación no autoriza implementar las etapas.**
