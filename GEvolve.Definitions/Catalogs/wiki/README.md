# Evolve — Wiki de catálogos

Enciclopedia local de solo lectura. Usa HTML, CSS y JavaScript nativo para consultar los seis catálogos JSON de `Catalogs/`. No carga las referencias biológicas ni modifica el motor o las definiciones.

## Abrir

Desde `Evolve/GEvolve.Definitions/Catalogs/`:

```powershell
node wiki/serve.js
```

Abre `http://127.0.0.1:8765/wiki/`. También puedes servir esa carpeta con `python -m http.server 8765` o VS Code Live Server. La lectura de JSON mediante `fetch()` requiere HTTP local; `file://` suele bloquearla.

## Navegación

El menú muestra Mundo, Animales, Plantas, Minerales, Recursos y Tecnologías. Las fichas enlazan los biomas generales con apariciones documentadas, fuentes con recursos, recursos con procesos de transformación y referencias tecnológicas explícitas. Los procesos tienen ficha accesible desde sus enlaces; las variantes regionales no tienen ficha ni navegación. Los cultivos jugables proceden de `agriculturalCropCandidates` del catálogo principal de plantas.

Tecnologías abre directamente el roadmap de Agricultura. `technology-roadmap.js` crea los 23 nodos actuales mediante el JSON cargado por `fetch()`; no mantiene una lista de IDs en el código. Recorre únicamente `prerequisites` para conectar tecnologías y coloca nodos auxiliares diferenciados para `all`, `any` y las referencias externas declaradas. `order` solo desempata la posición dentro de una columna; nunca crea una dependencia. La disposición calcula niveles del grafo y separa puntos y etiquetas. El mapa conserva desplazamiento, zoom, búsqueda, URLs con hash y el historial. Un punto seleccionado abre un panel de solo lectura con los datos originales de investigación y resalta requisitos y dependientes directos. Las referencias externas no tienen ficha agrícola inventada.

`catalog-index.js` construye un índice común por tipo e ID, resuelve alias históricos de animales, plantas y minerales, indexa tags de recursos y genera enlaces entrantes y salientes sin listas manuales duplicadas. Las relaciones proceden de `resourceOccurrences`, compatibilidades de bioma, `sourceBindings`, productos declarados, `sourceTagMappings`, `processingLinks` y referencias directas de tecnologías. Una ficha de recurso muestra fuentes, procesos en ambos sentidos y ramas posteriores; los nodos de procesos muestran entradas, auxiliares, salidas y subproductos. Las referencias sin destino aparecen en un panel al pie. Ningún proceso candidato se ejecuta como receta.

Las apariciones regionales documentadas no garantizan generación; la compatibilidad de hábitat se presenta aparte. Los productos y procesos que siguen como guías se etiquetan como potenciales. Los valores `null` se muestran como pendientes. Ninguna relación se convierte en receta o regla del juego desde la Wiki.

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
