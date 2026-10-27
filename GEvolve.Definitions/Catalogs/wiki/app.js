"use strict";

const CATALOGS = [
  { type: "world", title: { es: "Mundo (Biomas)", en: "World (Biomes)" }, file: "../World/world.catalog.guide.json" },
  { type: "animals", title: { es: "Animales", en: "Animals" }, file: "../Animals/animals.catalog.guide.json" },
  { type: "plants", title: { es: "Plantas", en: "Plants" }, file: "../Plants/plants.catalog.guide.json" },
  { type: "minerals", title: { es: "Minerales", en: "Minerals" }, file: "../Minerals/minerals.catalog.guide.json" },
  { type: "resources", title: { es: "Recursos", en: "Resources" }, file: "../Resources/resources.catalog.guide.json" },
  { type: "technologies", title: { es: "Tecnologías", en: "Technologies" }, file: "../Technologies/agriculture.technologies.json" }
];
const state = { lang: "es", query: "", type: "world", id: null, data: null, index: null,
  roadmap: { graph: null, zoom: 1, lastFocus: null } };
const $ = id => document.getElementById(id);
const list = value => Array.isArray(value) ? value : [];
const tr = (es, en) => state.lang === "es" ? es : en;
const loc = value => {
  if (value === null || value === undefined || value === "") return tr("Pendiente de definir", "Pending definition");
  if (typeof value === "boolean") return value ? tr("Sí", "Yes") : tr("No", "No");
  if (typeof value === "string" || typeof value === "number") return String(value);
  if (value[state.lang] != null || value.en != null || value.es != null)
    return String(value[state.lang] ?? value.en ?? value.es);
  return JSON.stringify(value);
};
const nameOf = entity => entity ? loc(entity.value.name ?? entity.value.processTag ?? entity.id) : "";
function element(tag, className = "", text) {
  const result = document.createElement(tag);
  if (className) result.className = className;
  if (text !== undefined) result.textContent = String(text);
  return result;
}
function append(parent, child) { parent.appendChild(child); return child; }
function line(parent, text, className = "") { return append(parent, element("p", className, text)); }
function section(parent, title) {
  const wrapper = append(parent, element("section", "section"));
  append(wrapper, element("h3", "", title));
  return wrapper;
}
function note(parent, text) { return line(parent, text, "muted"); }
function badge(parent, text, tone = "documented") { return append(parent, element("span", `badge ${tone}`, text)); }
function field(parent, label, value, showPending = false) {
  if (!showPending && (value == null || value === "")) return;
  const box = append(parent, element("div", "metabox"));
  append(box, element("small", "", label));
  append(box, element("div", "", loc(value)));
}
function fieldGrid(parent, fields) {
  const grid = append(parent, element("div", "meta"));
  for (const [label, value, showPending] of fields) field(grid, label, value, showPending);
}
function setNotice(message) { $("notice").hidden = !message; $("notice").textContent = message || ""; }

function destination(type, id) {
  const params = new URLSearchParams({ type });
  if (id) params.set("id", id);
  return `#${params}`;
}
function routeFromHash() {
  const params = new URLSearchParams(location.hash.slice(1));
  const type = params.get("type") || "world";
  return { type: CATALOGS.some(item => item.type === type) || type === "processes" ? type : "world", id: params.get("id") };
}
function navigate(type, id = null) {
  if (id && !state.index.resolve(type, id)) { setNotice(tr("Referencia no encontrada: ", "Reference not found: ") + id); return; }
  const previous = history.state?.trail || [];
  const trail = [...previous, { type, id }].slice(-8);
  history.pushState({ trail }, "", destination(type, id));
  state.query = "";
  $("search").value = "";
  syncRoute();
}
function syncRoute() {
  const next = routeFromHash();
  state.type = next.type;
  state.id = next.id;
  render();
}
function link(parent, type, id, label, className = "textlink") {
  const target = state.index.resolve(type, id);
  if (!target) {
    badge(parent, `${label || id} · ${tr("referencia no encontrada", "reference not found")}`, "missing");
    return null;
  }
  const button = append(parent, element("button", className, label || nameOf(target)));
  button.type = "button";
  button.onclick = () => navigate(target.type, target.id);
  return button;
}
function entityCard(parent, node, detail, tone = "documented") {
  const card = append(parent, element("div", "relation-card"));
  const head = append(card, element("div", "relation-head"));
  link(head, node.type, node.id, nameOf(node), "relation-link");
  badge(head, tone === "potential" ? tr("Potencial", "Potential") : tr("Documentado", "Documented"), tone);
  append(card, element("small", "code", node.id));
  if (detail) line(card, detail, "relation-detail");
  return card;
}
function edgeEvidence(edge, predicate) { return edge.evidence.find(predicate) || null; }
function bindingDetails(edge) {
  const product = edgeEvidence(edge, value => value.acquisitionMethodTags || value.availabilityConstraints || value.yield !== undefined);
  const binding = edgeEvidence(edge, value => value.acquisitionTag || value.quantityPerAction !== undefined);
  const bits = [];
  if (product?.acquisitionMethodTags?.length) bits.push(`${tr("Métodos", "Methods")}: ${product.acquisitionMethodTags.join(", ")}`);
  else if (binding?.acquisitionTag && binding.acquisitionTag !== "animal_material_methods_in_entity_catalog") bits.push(`${tr("Método", "Method")}: ${binding.acquisitionTag}`);
  if (product?.availabilityConstraints?.length) bits.push(`${tr("Restricciones", "Constraints")}: ${product.availabilityConstraints.join(", ")}`);
  const amount = product?.yield ?? binding?.quantityPerAction;
  bits.push(`${tr("Cantidad", "Quantity")}: ${amount == null ? tr("pendiente de definir", "pending definition") : amount}`);
  if (binding?.reviewStatus || product?.status) bits.push(`${tr("Estado", "Status")}: ${binding?.reviewStatus || product.status}`);
  return bits.join(" · ");
}
function sourceEdges(node, direction = "out") { return state.index.between(node, "source", direction); }
function occurrences(node, direction = "out") { return state.index.between(node, "occurrence", direction); }

function renderNav() {
  const nav = $("categories"); nav.replaceChildren();
  for (const catalog of CATALOGS) {
    const button = append(nav, element("button", "navbtn" + (catalog.type === state.type ? " active" : ""), loc(catalog.title)));
    button.type = "button";
    if (catalog.type === state.type) button.setAttribute("aria-current", "page");
    button.onclick = () => navigate(catalog.type);
  }
}
function renderTrail(node) {
  const crumbs = $("breadcrumbs"); crumbs.replaceChildren();
  const trail = history.state?.trail || [];
  const paths = trail.length ? trail : [{ type: state.type, id: state.id }];
  for (let i = 0; i < paths.length; i++) {
    const path = paths[i];
    const entity = path.id ? state.index.resolve(path.type, path.id) : null;
    const title = entity ? nameOf(entity) : loc(CATALOGS.find(item => item.type === path.type)?.title || { es: "Proceso", en: "Process" });
    if (i) append(crumbs, element("span", "crumb-separator", "/"));
    if (i === paths.length - 1) append(crumbs, element("span", "crumb-current", title));
    else link(crumbs, path.type, path.id, title, "crumb-link");
  }
  $("back").hidden = paths.length < 2;
  $("back").textContent = tr("Atrás", "Back");
}
function renderList(rows, selected) {
  const q = state.query.trim().toLocaleLowerCase();
  const filtered = rows.filter(node => `${nameOf(node)} ${node.id} ${node.value.tag || ""} ${list(node.value.tags).join(" ")} ${node.value.cropUseTag || ""} ${[...state.index.aliases].filter(([, id]) => id === node.id).map(([key]) => key.startsWith(`${node.type}:`) ? key.slice(node.type.length + 1) : "").join(" ")}`.toLocaleLowerCase().includes(q));
  $("count").textContent = `${filtered.length} / ${rows.length}`;
  const parent = $("list"); parent.replaceChildren();
  if (!filtered.length) append(parent, element("div", "empty", tr("Sin resultados", "No results")));
  for (const node of filtered) {
    const button = append(parent, element("button", "item" + (selected?.type === node.type && selected.id === node.id ? " active" : "")));
    button.type = "button";
    append(button, element("strong", "", nameOf(node)));
    append(button, element("small", "", node.id));
    button.onclick = () => navigate(node.type, node.id);
  }
}
function render() {
  if (!state.index) return;
  renderNav();
  const selected = state.id ? state.index.resolve(state.type, state.id) : null;
  const catalogType = state.type === "processes" ? "resources" : state.type;
  const rows = state.index.list(catalogType);
  const title = CATALOGS.find(item => item.type === catalogType)?.title;
  $("title").textContent = loc(title);
  $("subtitle").textContent = tr("Explora conexiones documentadas entre catálogos.", "Explore documented links between catalogs.");
  renderTrail(selected);
  $("technology-view").hidden = state.type !== "technologies";
  $("workspace").hidden = state.type === "technologies";
  if (state.type === "technologies") {
    renderTechnologyRoadmap(selected);
    return;
  }
  renderList(rows, selected);
  const area = $("detail"); area.replaceChildren();
  if (!selected) {
    if (state.id) { setNotice(tr("Referencia no encontrada: ", "Reference not found: ") + state.id); note(area, tr("Selecciona otra definición.", "Select another definition.")); }
    else note(area, tr("Selecciona una definición de la lista.", "Select a definition from the list."));
    return;
  }
  setNotice("");
  const head = append(area, element("div", "detail-head"));
  append(head, element("h2", "", nameOf(selected)));
  append(head, element("div", "code", selected.id));
  if (selected.value.description) line(area, loc(selected.value.description), "description");
  ({ world: renderWorld, animals: renderCreature, plants: renderCreature,
    minerals: renderMineral, resources: renderResource, technologies: renderTechnology,
    processes: renderProcess })[selected.type](area, selected);
  renderRelated(area, selected);
}

function groupedEntities(parent, edges, heading, relationTone = "documented") {
  const group = section(parent, `${heading} (${edges.length})`);
  if (!edges.length) { note(group, tr("Sin referencias documentadas.", "No documented references.")); return; }
  const cards = append(group, element("div", "relation-grid"));
  for (const edge of edges) {
    const entity = edge.to;
    const sources = edge.evidence.map(item => item.sourceText).filter(Boolean);
    const detail = sources.length ? `${tr("Fuentes documentadas", "Documented sources")}: ${sources.join("; ")}` : "";
    entityCard(cards, entity, detail, relationTone);
  }
}
function renderWorld(area, node) {
  fieldGrid(section(area, tr("Bioma", "Biome")), [
    [tr("ID", "ID"), node.id],
    [tr("Categoría de interfaz", "Interface category"), node.value.interfaceCategoryOnly]
  ]);
  const found = occurrences(node);
  groupedEntities(area, found.filter(edge => edge.to.type === "animals"), tr("Animales con aparición documentada", "Animals with documented occurrence"));
  groupedEntities(area, found.filter(edge => edge.to.type === "plants"), tr("Plantas con aparición documentada", "Plants with documented occurrence"));
  groupedEntities(area, found.filter(edge => edge.to.type === "minerals"), tr("Candidatos geológicos documentados", "Documented geological candidates"));
  const compatible = state.index.between(node, "habitat");
  const unique = [...new Map(compatible.map(edge => [`${edge.to.type}:${edge.to.id}`, edge])).values()];
  if (unique.length) {
    const s = section(area, tr("Compatibilidad ambiental indicada", "Indicated environmental compatibility"));
    note(s, tr("Estas referencias de hábitat no garantizan presencia ni generación.", "These habitat references do not guarantee presence or spawning."));
    const grid = append(s, element("div", "relation-grid"));
    for (const edge of unique) entityCard(grid, edge.to, "", "potential");
  }
  note(area, tr("Los rangos ambientales y la probabilidad de aparición no están definidos para este bioma.", "Environmental ranges and spawn probabilities are not defined for this biome."));
}
function renderEntityHabitats(area, node) {
  const refs = [...state.index.incoming(node).filter(edge => edge.kind === "occurrence" || edge.kind === "habitat")];
  if (!refs.length) return;
  const s = section(area, tr("Mundo y hábitat", "World and habitat"));
  const grid = append(s, element("div", "relation-grid"));
  const seen = new Set();
  for (const edge of refs) {
    const source = edge.from;
    const signature = `${source.type}:${source.id}:${edge.kind}`;
    if (seen.has(signature)) continue;
    seen.add(signature);
    const originNotes = [...new Set(edge.evidence.map(item => item.sourceText).filter(Boolean))];
    entityCard(grid, source, edge.kind === "occurrence"
      ? `${tr("Aparición documentada; presencia no garantizada", "Documented occurrence; presence not guaranteed")}${originNotes.length ? ` · ${tr("Fuentes", "Sources")}: ${originNotes.join("; ")}` : ""}`
      : tr("Compatibilidad indicada; presencia no garantizada", "Indicated compatibility; presence not guaranteed"),
      edge.kind === "occurrence" ? "documented" : "potential");
  }
}
function renderSourceLinks(area, node) {
  const edges = sourceEdges(node);
  const s = section(area, `${tr("Recursos asociados", "Associated resources")} (${edges.length})`);
  if (!edges.length) { note(s, tr("Aún no hay vínculos de recursos documentados.", "No resource links are documented yet.")); return edges; }
  const grid = append(s, element("div", "relation-grid"));
  for (const edge of edges) entityCard(grid, edge.to, bindingDetails(edge), "potential");
  note(s, tr("Los vínculos son referencias de diseño; las cantidades y acciones ejecutables pueden seguir pendientes.", "These are design references; quantities and executable actions may still be pending."));
  return edges;
}
function renderCreature(area, node) {
  const value = node.value;
  fieldGrid(section(area, tr("Identidad", "Identity")), [
    ["Tag", value.tag ?? value.cropUseTag],
    [tr("Tipo", "Type"), value.cropUseTag ? tr("Cultivo", "Crop") : node.type === "animals" ? tr("Animal", "Animal") : tr("Planta", "Plant")],
    [tr("Estado", "Status"), value.definitionStatus]
  ]);
  renderEntityHabitats(area, node);
  const sources = renderSourceLinks(area, node);
  if (value.cultivationResearch?.technologyId) {
    const s = section(area, tr("Investigación relacionada", "Related research"));
    link(s, "technologies", value.cultivationResearch.technologyId);
    note(s, tr("La referencia no confirma que el cultivo esté disponible en una partida.", "This reference does not confirm crop availability in a game."));
  }
  if (node.type === "plants") {
    const processes = [...new Map(sources.flatMap(edge => state.index.between(edge.to, "process_input"))
      .map(edge => [edge.to.id, edge.to])).values()];
    if (processes.length) {
      const s = section(area, tr("Transformaciones candidatas de sus recursos", "Candidate transformations of its resources"));
      for (const process of processes) processCard(s, process);
    }
  }
}
function renderMineral(area, node) {
  const value = node.value;
  fieldGrid(section(area, tr("Condiciones geológicas", "Geological conditions")), [
    [tr("Condiciones", "Conditions"), value.geologyConstraints, true],
    [tr("Estado", "Status"), value.definitionStatus],
    [tr("Distinción superficie / mena", "Surface / ore distinction"), value.surfaceAndOreDistinction]
  ]);
  renderEntityHabitats(area, node);
  const extracted = sourceEdges(node).filter(edge => edge.evidence.some(value => value.extractedResourceId || value.sourceCollection));
  const refined = state.index.between(node, "refined");
  for (const [edges, title] of [[extracted, tr("Recursos extraíbles", "Extractable resources")], [refined, tr("Productos refinados relacionados", "Related refined products")]]) {
    const s = section(area, `${title} (${edges.length})`);
    for (const edge of edges) entityCard(s, edge.to, bindingDetails(edge), "potential");
  }
  const processes = [...new Map([...extracted, ...refined].flatMap(edge => state.index.between(edge.to, "process_input"))
    .map(edge => [edge.to.id, edge.to])).values()];
  if (processes.length) {
    const s = section(area, tr("Transformaciones posteriores documentadas", "Documented later transformations"));
    for (const process of processes) processCard(s, process);
  }
}

function sourceGroup(area, edges, type, es, en) {
  const subset = edges.filter(edge => edge.from.type === type);
  if (!subset.length) return;
  const s = section(area, `${tr(es, en)} (${subset.length})`);
  const grid = append(s, element("div", "relation-grid"));
  for (const edge of subset) entityCard(grid, edge.from, bindingDetails(edge), "potential");
}
function processCard(parent, node) {
  const value = node.value;
  const card = append(parent, element("div", "process-card"));
  const body = append(card, element("div", "process-flow"));
  const inputs = append(body, element("div", "process-side"));
  append(inputs, element("small", "flow-label", tr("Entradas", "Inputs")));
  for (const id of list(value.inputResourceIds)) link(inputs, "resources", id, null, "flow-node");
  for (const id of list(value.processProfile?.auxiliaryResourceIds)) {
    const row = append(inputs, element("div", "flow-auxiliary"));
    append(row, element("small", "flow-label", tr("Auxiliar", "Auxiliary")));
    link(row, "resources", id, null, "flow-node");
  }
  const middle = append(body, element("div", "process-middle"));
  append(middle, element("small", "flow-label", tr("Proceso candidato", "Candidate process")));
  link(middle, "processes", node.id, node.id, "flow-process");
  const outputs = append(body, element("div", "process-side"));
  append(outputs, element("small", "flow-label", tr("Salidas", "Outputs")));
  for (const id of list(value.outputResourceIds)) {
    const row = append(outputs, element("div", "flow-output"));
    link(row, "resources", id, null, "flow-node");
    if (list(value.processProfile?.byproductResourceIds).includes(id)) append(row, element("small", "flow-label", tr("Subproducto", "Byproduct")));
  }
  note(card, value.description ? loc(value.description) : tr("Proceso documentado como guía; receta pendiente.", "Documented as a guide; recipe pending."));
  badge(card, value.runtimeEnabled ? tr("Regla operativa", "Operational rule") : tr("Relación de diseño; receta pendiente", "Design link; recipe pending"), value.runtimeEnabled ? "operational" : "potential");
  if (value.processProfile) fieldGrid(card, [
    [tr("Preparación", "Preparation"), value.processProfile.preparation],
    [tr("Transformación", "Transformation"), value.processProfile.transformation],
    [tr("Instalaciones sugeridas", "Suggested facilities"), value.processProfile.suggestedFacilities],
    [tr("Restricciones conocidas", "Known constraints"), value.processProfile.knownConstraints]
  ]);
}
function renderProductionTree(area, root) {
  const sectionNode = section(area, tr("Ramas de producción documentadas", "Documented production branches"));
  const seen = new Set();
  let frontier = [root];
  let count = 0;
  for (let depth = 0; depth < 5 && frontier.length && count < 80; depth++) {
    const next = [];
    for (const resource of frontier) {
      for (const edge of state.index.between(resource, "process_input")) {
        const process = edge.to;
        if (seen.has(process.id)) continue;
        seen.add(process.id);
        processCard(sectionNode, process);
        count++;
        next.push(...state.index.between(process, "process_output").map(output => output.to));
      }
    }
    frontier = next;
  }
  if (!count) note(sectionNode, tr("No hay derivaciones documentadas.", "No documented derived products."));
}
function renderResource(area, node) {
  const value = node.value;
  fieldGrid(section(area, tr("Identidad", "Identity")), [
    ["Tag", value.tag], [tr("Categoría", "Category"), value.category],
    [tr("Etapa", "Stage"), value.stage], [tr("Estado", "Status"), value.definitionStatus]
  ]);
  if (value.tags?.length) {
    const s = section(area, tr("Etiquetas", "Tags"));
    for (const tag of value.tags) badge(s, tag);
  }
  if (value.inventory) fieldGrid(section(area, tr("Inventario", "Inventory")), [
    [tr("Almacenable", "Storable"), value.inventory.storable, true],
    [tr("Unidad", "Unit"), value.inventory.quantityUnit, true],
    [tr("Perecedero", "Perishable"), value.inventory.perishable, true],
    [tr("Almacenamiento", "Storage"), value.inventory.storageRules, true]
  ]);
  const sources = sourceEdges(node, "in");
  const sourceSection = section(area, `${tr("Fuentes de obtención", "Acquisition sources")} (${sources.length})`);
  if (!sources.length) note(sourceSection, tr("No hay fuente vinculada mediante ID.", "No source linked by ID."));
  sourceGroup(sourceSection, sources, "animals", "Animales", "Animals");
  sourceGroup(sourceSection, sources, "plants", "Plantas y cultivos", "Plants and crops");
  sourceGroup(sourceSection, sources, "minerals", "Minerales", "Minerals");
  const refined = state.index.between(node, "refined", "in");
  if (refined.length) sourceGroup(sourceSection, refined, "minerals", "Referencias de refinado", "Refining references");
  const produced = state.index.between(node, "process_output", "in").map(edge => edge.from);
  const used = state.index.between(node, "process_input").map(edge => edge.to);
  const auxiliary = state.index.between(node, "process_auxiliary").map(edge => edge.to);
  for (const [processes, title] of [[produced, tr("Puede producirse a partir de", "Can be produced from")]]) {
    const s = section(area, `${title} (${processes.length})`);
    if (!processes.length) note(s, tr("No hay proceso vinculado.", "No linked process."));
    for (const process of processes) processCard(s, process);
  }
  if (used.length) renderProductionTree(area, node);
  if (auxiliary.length) {
    const auxSection = section(area, tr("Uso como material auxiliar", "Use as auxiliary material"));
    for (const process of auxiliary) processCard(auxSection, process);
  }
  const techEdges = state.index.incoming(node).filter(edge => ["reference", "tag_reference"].includes(edge.kind));
  if (techEdges.length) {
    const s = section(area, tr("Tecnologías que lo referencian", "Technologies referencing it"));
    note(s, tr("Una mención no implica por sí sola un requisito obligatorio.", "A mention alone does not imply a mandatory requirement."));
    const grid = append(s, element("div", "relation-grid"));
    for (const edge of techEdges) entityCard(grid, edge.from, edge.evidence.map(item => item.path).filter(Boolean).join(", "), "documented");
  }
}
function renderProcess(area, node) {
  processCard(area, node);
  const value = node.value;
  fieldGrid(section(area, tr("Estado de la receta", "Recipe status")), [
    [tr("ID de receta", "Recipe ID"), value.recipeId, true],
    [tr("Cantidades de entrada", "Input quantities"), value.inputQuantities, true],
    [tr("Cantidades de salida", "Output quantities"), value.outputQuantities, true],
    [tr("Instalaciones", "Facilities"), value.facilities, true],
    [tr("Habilitado en el motor", "Runtime enabled"), value.runtimeEnabled]
  ]);
  if (value.processProfile) fieldGrid(section(area, tr("Estado documental", "Documentation status")), [
    [tr("Práctica histórica", "Historical practice"), value.processProfile.historicalStatus],
    [tr("Estado en Evolve", "Evolve status"), value.processProfile.gameplayStatus]
  ]);
  const s = section(area, tr("Tecnologías requeridas explícitamente", "Explicitly required technologies"));
  if (!value.requiredTechnologyIds?.length) note(s, tr("Pendiente de definir.", "Pending definition."));
  for (const id of list(value.requiredTechnologyIds)) link(s, "technologies", id);
}

function structured(parent, value, label = "") {
  if (value == null) { note(parent, `${label ? label + ": " : ""}${tr("Pendiente de definir", "Pending definition")}`); return; }
  if (Array.isArray(value)) {
    if (!value.length) { note(parent, `${label ? label + ": " : ""}${tr("Sin condiciones", "No conditions")}`); return; }
    const group = append(parent, element("div", "expression-list"));
    for (const item of value) structured(group, item);
    return;
  }
  if (typeof value !== "object") { line(parent, `${label ? label + ": " : ""}${loc(value)}`, "expression-line"); return; }
  if (Object.hasOwn(value, "all") || Object.hasOwn(value, "any")) {
    const op = Object.hasOwn(value, "all") ? "all" : "any";
    const group = append(parent, element("div", "expression-group"));
    badge(group, op === "all" ? tr("TODAS", "ALL") : tr("CUALQUIERA", "ANY"));
    structured(group, value[op]);
    for (const [field, child] of Object.entries(value)) if (field !== op) structured(group, child, field);
    return;
  }
  const card = append(parent, element("div", "expression-leaf"));
  if (label) append(card, element("small", "flow-label", label));
  if (value.type) append(card, element("strong", "", value.type));
  for (const [field, child] of Object.entries(value)) {
    if (field === "type") continue;
    if (field === "technologyId" || field === "resourceId") {
      const row = append(card, element("div", "expression-line", `${field}: `));
      link(row, field === "technologyId" ? "technologies" : "resources", child, child);
    } else if (field === "speciesId" && typeof child === "string" && child !== "$speciesId" && state.index.resolve("plants", child)) {
      const row = append(card, element("div", "expression-line", `${field}: `)); link(row, "plants", child, child);
    } else if ((field === "tag" && value.type?.startsWith("resource_tag_")) || field === "resourceTag") {
      const row = append(card, element("div", "expression-line", `${field}: ${child} `));
      const matches = state.index.tagResources(child);
      for (const resource of matches) link(row, "resources", resource.id, nameOf(resource));
      if (!matches.length) badge(row, tr("Referencia no encontrada en estos catálogos", "Reference not found in these catalogs"), "missing");
    } else if (typeof child === "object" && child !== null) structured(card, child, field);
    else line(card, `${field}: ${loc(child)}`, "expression-line");
  }
}
function renderTechnology(area, node) {
  const value = node.value;
  fieldGrid(section(area, tr("Investigación", "Research")), [
    [tr("Ámbito", "Scope"), value.scope], [tr("Orden", "Order"), value.order],
    [tr("Modo", "Mode"), value.research?.mode], [tr("Costo documentado", "Documented cost"), value.research?.cost]
  ]);
  for (const [label, expression] of [
    [tr("Requisitos previos", "Prerequisites"), value.prerequisites],
    [tr("Condiciones de inicio", "Start conditions"), value.research?.startConditions],
    [tr("Condiciones de continuación", "Ongoing conditions"), value.research?.ongoingConditions],
    [tr("Condiciones de uso", "Usage conditions"), value.usageConditions],
    [tr("Eurekas", "Eurekas"), value.eurekas],
    [tr("Efectos y desbloqueos", "Effects and unlocks"), value.effects]
  ]) {
    const s = section(area, label);
    structured(s, expression);
    const raw = append(s, element("details", "rawsummary"));
    append(raw, element("summary", "", tr("Ver estructura original", "Show original structure")));
    append(raw, element("pre", "", JSON.stringify(expression, null, 2)));
  }
  const prereqs = state.index.between(node, "prerequisite");
  const later = state.index.between(node, "prerequisite", "in");
  for (const [edges, title, direction] of [[prereqs, tr("Tecnologías anteriores", "Earlier technologies"), "to"], [later, tr("Tecnologías posteriores", "Later technologies"), "from"]]) {
    if (!edges.length) continue;
    const s = section(area, title); const grid = append(s, element("div", "relation-grid"));
    for (const edge of edges) entityCard(grid, edge[direction], "", "documented");
  }
  const related = state.index.outgoing(node).filter(edge => ["reference", "tag_reference"].includes(edge.kind));
  if (related.length) {
    const s = section(area, tr("Recursos mencionados explícitamente", "Explicitly mentioned resources"));
    note(s, tr("El tipo de condición original determina el significado de cada mención.", "The original condition type determines what each mention means."));
    const grid = append(s, element("div", "relation-grid"));
    for (const edge of related) entityCard(grid, edge.to, edge.evidence.map(item => item.path).filter(Boolean).join(", "));
  }
  const entities = state.index.between(node, "cultivation", "in");
  if (entities.length) {
    const s = section(area, tr("Entidades con referencia a esta investigación", "Entities referencing this research"));
    const grid = append(s, element("div", "relation-grid"));
    for (const edge of entities) entityCard(grid, edge.from, "", "potential");
  }
}
function roadmapMatches(node, query) {
  if (!query || node.kind !== "technology") return false;
  return `${node.id} ${node.value.name?.es || ""} ${node.value.name?.en || ""}`.toLocaleLowerCase().includes(query);
}
function centerRoadmapNode(id) {
  const node = state.roadmap.graph?.nodes.get(id);
  if (!node) return;
  const viewport = $("roadmap-viewport");
  const scale = state.roadmap.zoom;
  viewport.scrollLeft = Math.max(0, (node.x + 100) * scale - viewport.clientWidth / 2);
  viewport.scrollTop = Math.max(0, (node.y + 24) * scale - viewport.clientHeight / 2);
}
function applyRoadmapZoom() {
  const { graph, zoom } = state.roadmap;
  if (!graph) return;
  $("roadmap-surface").style.width = `${graph.width * zoom}px`;
  $("roadmap-surface").style.height = `${graph.height * zoom}px`;
  $("roadmap-stage").style.transform = `scale(${zoom})`;
  $("roadmap-zoom-value").textContent = `${Math.round(zoom * 100)}%`;
}
function setRoadmapZoom(delta) {
  const viewport = $("roadmap-viewport");
  const previous = state.roadmap.zoom;
  const next = Math.min(1.6, Math.max(0.8, Math.round((previous + delta) * 100) / 100));
  const centerX = (viewport.scrollLeft + viewport.clientWidth / 2) / previous;
  const centerY = (viewport.scrollTop + viewport.clientHeight / 2) / previous;
  state.roadmap.zoom = next;
  applyRoadmapZoom();
  viewport.scrollLeft = Math.max(0, centerX * next - viewport.clientWidth / 2);
  viewport.scrollTop = Math.max(0, centerY * next - viewport.clientHeight / 2);
}
function centerRoadmap() {
  const viewport = $("roadmap-viewport");
  const { graph, zoom } = state.roadmap;
  viewport.scrollLeft = Math.max(0, (graph.width * zoom - viewport.clientWidth) / 2);
  viewport.scrollTop = Math.max(0, (graph.height * zoom - viewport.clientHeight) / 2);
}
function roadmapHighlightEdges(graph, selectedId) {
  const result = new Set();
  if (!selectedId) return result;
  for (const edge of graph.edges) if (edge.owner === selectedId) result.add(edge);
  for (const dependent of graph.dependents.get(selectedId) || []) {
    const queue = [selectedId], visited = new Set();
    while (queue.length) {
      const id = queue.shift();
      if (visited.has(id)) continue;
      visited.add(id);
      for (const edge of graph.outgoing.get(id) || []) {
        if (edge.owner !== dependent) continue;
        result.add(edge);
        queue.push(edge.to);
      }
    }
  }
  return result;
}
function renderRoadmapPanel(node) {
  const panel = $("roadmap-panel"); panel.replaceChildren();
  panel.hidden = !node;
  $("roadmap-layout").className = `roadmap-layout${node ? " has-panel" : ""}`;
  if (!node) return;
  const top = append(panel, element("div", "roadmap-panel-head"));
  badge(top, tr("Guía de investigación", "Research guide"), "potential");
  const close = append(top, element("button", "roadmap-close", tr("Cerrar", "Close")));
  close.type = "button";
  close.onclick = () => navigate("technologies");
  const head = append(panel, element("div", "detail-head"));
  append(head, element("h2", "", nameOf(node)));
  append(head, element("div", "code", node.id));
  line(panel, loc(node.value.description), "description");
  if (state.data.technologies.balance?.status === "provisional")
    note(panel, tr("Los costes y valores numéricos son provisionales.", "Costs and numeric values are provisional."));
  renderTechnology(panel, node);
  const capabilities = list(node.value.effects).filter(effect => effect.type === "unlock_capability");
  if (capabilities.length) {
    const area = section(panel, tr("Capacidades desbloqueadas", "Unlocked capabilities"));
    for (const effect of capabilities) badge(area, effect.capabilityId);
    note(area, tr("Su uso depende de las condiciones indicadas; no se crean recursos automáticamente.", "Use depends on the listed conditions; no resources are created automatically."));
  }
  const refs = state.roadmap.graph.dependencies.get(node.id) || new Set();
  const external = [...refs].filter(id => state.roadmap.graph.nodes.get(id)?.kind === "external");
  if (external.length) {
    const area = section(panel, tr("Conocimientos externos no integrados", "External knowledge not integrated"));
    for (const id of external) badge(area, id, "potential");
  }
  renderRelated(panel, node);
}
function renderTechnologyRoadmap(selected) {
  const graph = state.roadmap.graph;
  const query = state.query.trim().toLocaleLowerCase();
  const matches = [...graph.nodes.values()].filter(node => roadmapMatches(node, query));
  $("count").textContent = `${query ? matches.length : state.data.technologies.technologies.length} / ${state.data.technologies.technologies.length}`;
  $("roadmap-heading").textContent = loc(state.data.technologies.name);
  $("roadmap-caption").textContent = tr("Puntos azules: tecnologías agrícolas · líneas: requisitos explícitos", "Blue points: agriculture technologies · lines: explicit prerequisites");
  $("roadmap-center").textContent = tr("Centrar mapa", "Center map");
  $("roadmap-zoom-in").setAttribute("aria-label", tr("Acercar", "Zoom in"));
  $("roadmap-zoom-out").setAttribute("aria-label", tr("Alejar", "Zoom out"));
  $("roadmap-viewport").setAttribute("aria-label", tr("Mapa tecnológico desplazable", "Scrollable technology map"));
  const legend = $("roadmap-legend"); legend.replaceChildren();
  for (const [shape, text] of [["legend-dot", tr("Tecnología agrícola", "Agriculture technology")],
    ["legend-dot external", tr("Referencia externa", "External reference")],
    ["legend-gate", tr("TODAS / CUALQUIERA", "ALL / ANY")]]) {
    const item = append(legend, element("span"));
    append(item, element("i", shape, shape === "legend-gate" ? tr("Y/O", "A/O") : ""));
    append(item, element("span", "", text));
  }
  if (state.id && !selected) setNotice(tr("Referencia no encontrada: ", "Reference not found: ") + state.id);
  else setNotice("");
  renderRoadmapPanel(selected);
  const viewport = $("roadmap-viewport");
  const oldLeft = viewport.scrollLeft, oldTop = viewport.scrollTop;
  const svg = $("roadmap-lines"); svg.replaceChildren();
  svg.setAttribute("viewBox", `0 0 ${graph.width} ${graph.height}`);
  svg.setAttribute("width", graph.width);
  svg.setAttribute("height", graph.height);
  const activeEdges = roadmapHighlightEdges(graph, selected?.id);
  const related = new Set([...(graph.dependencies.get(selected?.id) || []), ...(graph.dependents.get(selected?.id) || [])]);
  for (const edge of graph.edges) {
    const from = graph.nodes.get(edge.from), to = graph.nodes.get(edge.to);
    const sx = from.x + 24, sy = from.y + 24, tx = to.x + 6, ty = to.y + 24;
    const halfway = sx + Math.max(35, (tx - sx) / 2);
    const path = document.createElementNS("http://www.w3.org/2000/svg", "path");
    path.setAttribute("d", `M ${sx} ${sy} C ${halfway} ${sy}, ${halfway} ${ty}, ${tx} ${ty}`);
    path.setAttribute("class", `roadmap-path${activeEdges.has(edge) ? " is-active" : selected ? " is-dim" : ""}`);
    svg.appendChild(path);
  }
  const nodeLayer = $("roadmap-nodes"); nodeLayer.replaceChildren();
  for (const node of graph.nodes.values()) {
    const matched = roadmapMatches(node, query);
    const isRelated = related.has(node.id) || node.kind === "gate" && activeEdges.size && graph.edges.some(edge => activeEdges.has(edge) && (edge.from === node.id || edge.to === node.id));
    const dim = selected ? node.id !== selected.id && !isRelated && !matched : query && !matched && node.kind === "technology";
    const button = append(nodeLayer, element("button", `roadmap-node ${node.kind}${node.id === selected?.id ? " is-selected" : ""}${isRelated ? " is-related" : ""}${dim ? " is-dim" : ""}${matched ? " is-match" : ""}`));
    button.type = "button";
    button.style.left = `${node.x}px`; button.style.top = `${node.y}px`;
    if (node.kind === "technology") {
      append(button, element("span", "point"));
      append(button, element("span", "node-label", loc(node.value.name)));
      button.title = node.id;
      if (node.id === selected?.id) button.setAttribute("aria-current", "true");
      button.onclick = () => navigate("technologies", node.id);
    } else if (node.kind === "external") {
      append(button, element("span", "point"));
      append(button, element("span", "node-label", node.id.replaceAll("_", " ")));
      button.title = tr("Conocimiento externo no integrado: ", "External knowledge not integrated: ") + node.id;
      button.setAttribute("aria-label", button.title);
      button.onclick = () => setNotice(button.title);
    } else if (node.kind === "gate") {
      append(button, element("span", "point", node.operator === "all" ? tr("Y", "AND") : tr("O", "OR")));
      button.title = node.operator === "all" ? tr("Deben cumplirse todas las alternativas", "All requirements must be met") : tr("Una alternativa es suficiente", "Any one alternative is sufficient");
      button.setAttribute("aria-label", button.title);
      button.disabled = true;
    } else {
      append(button, element("span", "point"));
      append(button, element("span", "node-label", node.value.type));
      button.title = tr("Condición del requisito: ", "Prerequisite condition: ") + node.value.type;
      button.disabled = true;
    }
  }
  $("roadmap-stage").style.width = `${graph.width}px`;
  $("roadmap-stage").style.height = `${graph.height}px`;
  applyRoadmapZoom();
  viewport.scrollLeft = oldLeft; viewport.scrollTop = oldTop;
  const focus = query ? matches[0]?.id || null : selected?.id || null;
  if (focus && focus !== state.roadmap.lastFocus) centerRoadmapNode(focus);
  state.roadmap.lastFocus = focus;
}
function renderRelated(area, node) {
  const neighbors = [...state.index.outgoing(node), ...state.index.incoming(node)]
    .map(edge => edge.from === node ? edge.to : edge.from)
    .filter(other => other !== node);
  const unique = [...new Map(neighbors.map(other => [`${other.type}:${other.id}`, other])).values()];
  if (!unique.length) return;
  const s = section(area, `${tr("Relacionado con", "Related to")} (${unique.length})`);
  const grid = append(s, element("div", "related-list"));
  for (const related of unique.slice(0, 12)) link(grid, related.type, related.id, nameOf(related), "related-link");
  if (unique.length > 12) {
    const more = append(s, element("details", "related-more"));
    append(more, element("summary", "", `${tr("Ver más relaciones", "Show more links")} (${unique.length - 12})`));
    const extra = append(more, element("div", "related-list"));
    for (const related of unique.slice(12)) link(extra, related.type, related.id, nameOf(related), "related-link");
  }
}
function renderUnresolved() {
  const refs = [...state.index.unresolved.values()];
  $("unresolved-title").textContent = `${tr("Referencias sin ficha en los catálogos actuales", "References without an entry in the current catalogs")} (${refs.length})`;
  const container = $("unresolved-list"); container.replaceChildren();
  note(container, tr("Se conservan como referencias de diseño. No se crean definiciones ni enlaces ficticios.", "They remain design references. No invented definitions or links are created."));
  for (const ref of refs) {
    const row = append(container, element("div", "unresolved-row"));
    append(row, element("span", "code", `${ref.type}: ${ref.id}`));
    append(row, element("small", "", [...ref.contexts].slice(0, 3).join(", ")));
  }
}

async function start() {
  try {
    const pairs = await Promise.all(CATALOGS.map(async item => {
      const response = await fetch(item.file);
      if (!response.ok) throw Error(`${item.file}: HTTP ${response.status}`);
      return [item.type, await response.json()];
    }));
    state.data = Object.fromEntries(pairs);
    state.index = EvolveCatalogIndex(state.data);
    state.roadmap.graph = EvolveTechnologyRoadmap(state.data.technologies);
    renderUnresolved();
    const route = routeFromHash();
    if (!history.state?.trail) history.replaceState({ trail: [route] }, "", location.href);
    syncRoute();
  } catch (error) {
    const serverHint = location.protocol === "file:" ? ` ${tr("Abre la Wiki desde un servidor HTTP local.", "Open the Wiki from a local HTTP server.")}` : "";
    setNotice(`${tr("No se pudo iniciar la Wiki", "Could not start the Wiki")}: ${error.message}.${serverHint}`);
  }
}
$("search").addEventListener("input", event => { state.query = event.target.value; render(); });
$("language").addEventListener("change", event => {
  state.lang = event.target.value;
  document.documentElement.lang = state.lang;
  document.querySelectorAll("[data-es][data-en]").forEach(node => { node.textContent = node.dataset[state.lang]; });
  $("search").placeholder = tr("Buscar por nombre, ID o etiqueta…", "Search by name, ID or tag…");
  if (state.index) renderUnresolved();
  render();
});
$("back").addEventListener("click", () => history.back());
$("roadmap-zoom-in").addEventListener("click", () => setRoadmapZoom(0.15));
$("roadmap-zoom-out").addEventListener("click", () => setRoadmapZoom(-0.15));
$("roadmap-center").addEventListener("click", centerRoadmap);
window.addEventListener("popstate", () => { state.query = ""; $("search").value = ""; syncRoute(); });
window.addEventListener("hashchange", () => { if (state.index) syncRoute(); });
start();
