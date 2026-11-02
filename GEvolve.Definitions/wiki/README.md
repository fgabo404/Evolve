# Evolve — Wiki de catálogos

Enciclopedia local de solo lectura. Usa HTML, CSS y JavaScript nativo para consultar los seis catálogos JSON de `Catalogs/`. No carga las referencias biológicas ni modifica el motor o las definiciones.

## Abrir

Desde `Evolve/GEvolve.Definitions/Catalogs/`:

```powershell
node wiki/serve.js
```

Abre `http://127.0.0.1:8765/wiki/`. También puedes servir esa carpeta con `python -m http.server 8765` o VS Code Live Server. La lectura de JSON mediante `fetch()` requiere HTTP local; `file://` suele bloquearla.

## Navegación

El menú muestra Mundo, Animales, Plantas, Minerales, Recursos y Tecnologías. Las fichas enlazan biomas con subbiomas y sus listas de fauna, flora y minerales, y las fuentes con recursos, recursos con procesos de transformación y referencias tecnológicas explícitas. Los procesos tienen ficha accesible desde sus enlaces; las variantes regionales no tienen ficha ni navegación. Las plantas proceden de `Plants/plants.json`; los candidatos agrícolas históricos no se cargan automáticamente.

Tecnologías abre el árbol desde `Technologies/agriculture.tech.json`. Esta versión contiene una sola tecnología: **Lenguaje** (`language`, orden 0). Desde ella salen tres ramas visuales planificadas: **Agricultura**, **Domesticación** y **Otras tecnologías**. Las cajas de rama no son tecnologías, requisitos ni capacidades jugables. Las demás tecnologías se incorporarán una por una con requisitos explícitos que desciendan de Lenguaje. El archivo anterior, `agriculture.technologies.json`, permanece intacto como referencia.

`technology-roadmap.js` crea puntos y ramas desde el JSON cargado por `fetch()`. Las líneas hacia las cajas indican clasificación visual; las conexiones entre tecnologías futuras procederán solo de `prerequisites`. `order` desempata posiciones y nunca crea dependencias. El mapa conserva desplazamiento, zoom, búsqueda, URLs con hash y el historial. Un punto seleccionado abre un panel de solo lectura; los campos sin decisión de diseño se muestran como pendientes.

`catalog-index.js` construye un índice común por tipo e ID, resuelve alias históricos de animales, plantas y minerales, indexa tags de recursos y genera enlaces entrantes y salientes sin listas manuales duplicadas. Las relaciones proceden de los nuevos `spawn[]`, `fauna[]`, `flora[]`, `minerals[]`, `resources[]`, además de `sourceBindings`, `processingLinks` y referencias explícitas de tecnologías. Una ficha de recurso muestra fuentes, procesos en ambos sentidos y ramas posteriores; los nodos de procesos muestran entradas, auxiliares, salidas y subproductos. Las referencias sin destino aparecen en un panel al pie. Ningún proceso candidato se ejecuta como receta.

La compatibilidad por subbioma no garantiza generación; no se cargan variantes regionales. Los productos y procesos que siguen como guías se etiquetan como potenciales. Los valores `null` se muestran como pendientes. Ninguna relación se convierte en receta o regla del juego desde la Wiki.

Una URL como `#type=resources&id=RES%23000031` abre una ficha concreta. Se admite Atrás y Adelante del navegador, además del botón Atrás de la Wiki. El carácter `#` de los IDs se codifica en el hash.

## Comprobaciones

Desde `Catalogs/`:

```powershell
node --check wiki/catalog-index.js
node --check wiki/app.js
node --check wiki/technology-roadmap.js
node --check wiki/serve.js
node wiki/tests/wiki.test.js
node wiki/tests/technology-roadmap.test.js
```

La prueba usa los JSON reales y las API nativas de Node.js. Comprueba el índice, rutas documentadas, alias, enlaces inversos, acceso directo por hash, navegación Atrás/Adelante, búsqueda, idioma y exclusión de los catálogos biológicos.

## Nuevos catálogos de juego

La Wiki consume `World/biomes.json`, `Animals/animals.json`, `Plants/plants.json` y `Minerals/minerals.json`. Mundo incluye ocho biomas principales con fichas para sus 22 subbiomas. Los enlaces funcionan en ambas direcciones: subbioma → entidad, entidad → subbioma y entidad → recurso → proceso. No se carga ningún JSON de `Old/` ni los catálogos biológicos auxiliares.
