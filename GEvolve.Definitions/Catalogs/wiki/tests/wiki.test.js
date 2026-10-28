"use strict";
const assert = require("node:assert/strict");
const fs = require("node:fs");
const path = require("node:path");
const vm = require("node:vm");

const wiki = path.resolve(__dirname, "..");
const catalogs = path.resolve(wiki, "..");
const files = {
  world: "World/biomes.json",
  animals: "Animals/animals.json",
  plants: "Plants/plants.json",
  minerals: "Minerals/minerals.json",
  resources: "Resources/resources.catalog.guide.json",
  technologies: "Technologies/agriculture.tech.json"
};
const data = Object.fromEntries(Object.entries(files).map(([type, file]) =>
  [type, JSON.parse(fs.readFileSync(path.join(catalogs, file), "utf8"))]));
const indexContext = vm.createContext({});
vm.runInContext(fs.readFileSync(path.join(wiki, "catalog-index.js"), "utf8"), indexContext);
const index = indexContext.EvolveCatalogIndex(data);
const out = (node, kind) => index.between(node, kind);
const incoming = (node, kind) => index.between(node, kind, "in");
const findPath = (fromType, firstKind, middleType, secondKind, toType) =>
  index.list(fromType).flatMap(start => out(start, firstKind)
    .filter(edge => edge.to.type === middleType)
    .flatMap(edge => out(edge.to, secondKind)
      .filter(last => last.to.type === toType)
      .map(last => [start.id, edge.to.id, last.to.id])))[0];

for (const type of ["world", "animals", "plants", "minerals", "resources", "technologies", "processes"]) {
  assert(index.list(type).length > 0, `${type} should be indexed`);
}
assert.equal(index.list("animals").length, data.animals.animals.length);
assert.equal(index.list("plants").length,
  data.plants.plants.length);
assert.equal(index.list("resources").length, data.resources.resources.length);
assert.equal(index.list("variants").length, 0);
assert.equal(index.list("minerals").length, 45);
assert.equal(index.list("world").length, 30);
assert(index.resolve("world", "forest_boreal"));
assert(out(index.resolve("world", "forest"), "subbiome").some(e => e.to.id === "forest_boreal"));
for (const animal of data.animals.animals) for (const sid of animal.spawn) assert(out(index.resolve("world",sid), "habitat").some(e => e.to.id === animal.id));
for (const plant of data.plants.plants) for (const sid of plant.spawn) assert(out(index.resolve("world",sid), "habitat").some(e => e.to.id === plant.id));
for (const mineral of data.minerals.minerals) for (const sid of mineral.spawn) assert(out(index.resolve("world",sid), "habitat").some(e => e.to.id === mineral.id));

assert.equal(index.list("processes").length, data.resources.processingLinks.length);
assert(index.resolve("resources", "RES#000031"));
assert(index.resolve("animals", data.animals.animals[0].id));
assert(index.edges.size === new Set(index.edges.keys()).size, "edges should be unique");

const routes = [
  ["world", "habitat", "animals", "source", "resources"],
  ["world", "habitat", "plants", "source", "resources"],
  ["world", "habitat", "minerals", "source", "resources"],
  ["plants", "source", "resources", "process_input", "processes"],
  ["minerals", "source", "resources", "process_input", "processes"]
];
for (const route of routes) {
  const found = findPath(...route);
  console.log(`${found ? "AVAILABLE" : "NOT DOCUMENTED"}: ${route.join(" → ")}${found ? " — " + found.join(" → ") : ""}`);
}
const hide = index.resolve("resources", "RES#000031");
assert(incoming(hide, "source").filter(edge => edge.from.type === "animals").length > 1);
const tanning = out(hide, "process_input").find(edge => edge.to.id === "hide_tanning")?.to;
assert(tanning);
assert(out(tanning, "process_output").some(edge => edge.to.id === "RES#000104"));
assert(incoming(hide, "source").every(edge => out(edge.from, "source").includes(edge)));
const manure = index.resolve("resources", "RES#000006");
assert(incoming(manure, "source").length > 0, "referenced resource should retain its sources");
assert(index.resolve("technologies", "language"));
assert(index.list("technologies").length >= 1);
const olives = index.resolve("plants", "plant_wild_olive");
const oliveResource = out(olives, "source").find(edge => edge.to.id === "RES#000021")?.to;
assert(oliveResource);
const oilProcess = out(oliveResource, "process_input").find(edge => edge.to.id === "olive_oil_extraction")?.to;
assert(oilProcess && out(oilProcess, "process_output").some(edge => edge.to.id === "RES#000103"));
const breadmaking = index.resolve("processes", "breadmaking");
assert.equal(out(breadmaking, "process_output").length, 1);
assert.equal(incoming(breadmaking, "process_input").length, 2, "multi-input processes must retain every input");
const clay = index.resolve("minerals", "geological_clay");
assert.equal(out(clay, "source").find(edge => edge.to.id === "RES#000047")?.to.id, "RES#000047");
const preparedClay = index.resolve("resources", "RES#000107");
const clayBranches = out(preparedClay, "process_input").map(edge => edge.to.id);
for (const tag of ["green_brick_moulding", "adobe_moulding_drying", "ceramic_roof_tile_forming_firing", "earthenware_vessel_forming_firing"]) assert(clayBranches.includes(tag));
const adobe = index.resolve("processes", "adobe_moulding_drying");
assert(incoming(adobe, "process_input").length > 1);
assert(out(adobe, "process_output").some(edge => edge.to.id === "RES#000109"));
const bloomery = index.resolve("processes", "bloomery_prepared_ironmaking");
assert(incoming(bloomery, "process_input").length > 1);
assert(out(bloomery, "process_output").length > 1);
assert(bloomery.value.processProfile.byproductResourceIds.includes("RES#000121"));
assert(index.unresolved.size >= 0);
console.log(`Index: ${index.nodes.size} nodes, ${index.edges.size} unique edges, ${index.unresolved.size} unresolved references`);
console.log("Unresolved:", [...index.unresolved.values()].map(item => `${item.type}:${item.id} (${[...item.contexts].slice(0, 2).join(", ")})`).join("; "));
console.log("Technology resource links:", [...index.edges.values()].filter(edge => edge.from.type === "technologies" && edge.to.type === "resources").length);

class Element {
  constructor(tag = "div") { this.tag = tag; this.children = []; this.dataset = {}; this.attributes = {}; this.style = {}; this.value = ""; this._text = ""; this.hidden = false; this.scrollLeft = 0; this.scrollTop = 0; this.clientWidth = 800; this.clientHeight = 560; }
  set textContent(value) { this._text = String(value); this.children = []; }
  get textContent() { return this._text + this.children.map(child => child.textContent).join(""); }
  appendChild(child) { this.children.push(child); return child; }
  replaceChildren() { this.children = []; this._text = ""; }
  setAttribute(key, value) { this.attributes[key] = value; }
  addEventListener(type, handler) { this[`on${type}`] = handler; }
}
const ids = Object.fromEntries(["categories", "notice", "title", "subtitle", "search", "list", "detail", "count", "language", "back", "breadcrumbs", "unresolved", "unresolved-title", "unresolved-list", "workspace", "technology-view", "roadmap-heading", "roadmap-caption", "roadmap-center", "roadmap-zoom-in", "roadmap-zoom-out", "roadmap-zoom-value", "roadmap-layout", "roadmap-viewport", "roadmap-surface", "roadmap-stage", "roadmap-lines", "roadmap-nodes", "roadmap-panel", "roadmap-legend"]
  .map(id => [id, new Element()]));
const html = fs.readFileSync(path.join(wiki, "index.html"), "utf8");
for (const id of Object.keys(ids)) assert(html.includes(`id="${id}"`), `missing DOM ID in index.html: ${id}`);
const staticLabels = [{ dataset: { es: "Idioma", en: "Language" }, textContent: "Idioma" }];
const document = {
  createElement: tag => new Element(tag), createElementNS: (_namespace, tag) => new Element(tag), getElementById: id => ids[id],
  querySelectorAll: () => staticLabels, documentElement: { lang: "es" }
};
const location = { href: "http://localhost:8765/wiki/#type=resources&id=RES%23000031", hash: "#type=resources&id=RES%23000031" };
const listeners = {};
const window = { addEventListener: (type, handler) => { listeners[type] = handler; } };
const entries = [];
let position = -1;
const history = {
  state: null,
  pushState(value, _title, hash) {
    entries.splice(position + 1);
    entries.push({ value, hash }); position = entries.length - 1;
    this.state = value; location.hash = hash; location.href = `http://localhost:8765/wiki/${hash}`;
  },
  replaceState(value, _title, url) {
    const hash = url.slice(url.indexOf("#"));
    if (position < 0) { entries.push({ value, hash }); position = 0; }
    else entries[position] = { value, hash };
    this.state = value;
  },
  back() { if (position > 0) { position--; this.state = entries[position].value; location.hash = entries[position].hash; listeners.popstate(); } },
  forward() { if (position + 1 < entries.length) { position++; this.state = entries[position].value; location.hash = entries[position].hash; listeners.popstate(); } }
};
const requested = [];
const context = vm.createContext({ document, location, window, history, URLSearchParams, console,
  fetch: async relative => {
    requested.push(relative);
    const file = path.resolve(wiki, relative);
    return { ok: true, json: async () => JSON.parse(fs.readFileSync(file, "utf8")) };
  }
});
vm.runInContext(fs.readFileSync(path.join(wiki, "catalog-index.js"), "utf8"), context);
vm.runInContext(fs.readFileSync(path.join(wiki, "technology-roadmap.js"), "utf8"), context);
vm.runInContext(fs.readFileSync(path.join(wiki, "app.js"), "utf8"), context);

function descendants(root) { return [root, ...root.children.flatMap(descendants)]; }
async function ready() {
  await new Promise(resolve => setImmediate(resolve));
  assert.equal(requested.length, 6);
  assert(requested.includes("../Technologies/agriculture.tech.json"));
  assert(!requested.includes("../Technologies/agriculture.technologies.json"));
  assert(requested.every(file => !file.includes("biological")));
  assert.equal(ids.categories.children.length, 6);
  assert.equal(ids.categories.children[0].textContent, "Mundo (Biomas)");
  assert.equal(ids.categories.children[4].textContent, "Recursos");
  assert.equal(ids.count.textContent, "121 / 121");
  assert.match(ids.detail.textContent, /RES#000031/);
  assert.match(ids.detail.textContent, /hide_tanning/);
  assert.match(ids.detail.textContent, /Pendiente de definir/);
  assert.match(ids["unresolved-title"].textContent, /\d+/);
  assert.doesNotMatch(ids.detail.textContent, /taxonomy|taxonResolution|scientificName/i);
  const sourceButton = descendants(ids.detail).find(item => item.className === "relation-link" && item.textContent);
  assert(sourceButton, "a resource source must be clickable");
  sourceButton.onclick();
  assert.equal(new URLSearchParams(location.hash.slice(1)).get("type"), "animals");
  assert.match(ids.detail.textContent, /Recursos asociados/);
  history.back();
  assert.equal(new URLSearchParams(location.hash.slice(1)).get("id"), "RES#000031");
  history.forward();
  assert.equal(new URLSearchParams(location.hash.slice(1)).get("type"), "animals");
  ids.language.value = "en"; ids.language.onchange({ target: ids.language });
  assert.equal(document.documentElement.lang, "en");
  assert.match(ids.detail.textContent, /Associated resources/);
  const animalId = new URLSearchParams(location.hash.slice(1)).get("id");
  ids.search.value = animalId; ids.search.oninput({ target: ids.search });
  assert.equal(ids.count.textContent, "1 / 101");
  assert.equal(ids.search.value, animalId);
  vm.runInContext("navigate('resources')", context);
  ids.language.value = "es"; ids.language.onchange({ target: ids.language });
  ids.search.value = "madera"; ids.search.oninput({ target: ids.search });
  assert.match(ids.list.textContent, /Madera/);
  ids.language.value = "en"; ids.language.onchange({ target: ids.language });
  ids.search.value = "wood"; ids.search.oninput({ target: ids.search });
  assert.match(ids.list.textContent, /Wood/);
  ids.search.value = "fuel_candidate"; ids.search.oninput({ target: ids.search });
  assert.match(ids.list.textContent, /Wood/);
  ids.search.value = "RES#000001"; ids.search.oninput({ target: ids.search });
  assert.equal(ids.count.textContent, "1 / 121");
  for (const [type, id, expected] of [
    ["world", "forest_boreal", "Fauna"],
    ["plants", "plant_wild_olive", "Associated resources"],
    ["minerals", "geological_clay", "Extractable resources"],
    ["processes", "breadmaking", "breadmaking"],
    ["technologies", "language", "Language"]
  ]) {
    vm.runInContext(`navigate(${JSON.stringify(type)}, ${JSON.stringify(id)})`, context);
    assert.match((type === "technologies" ? ids["roadmap-panel"] : ids.detail).textContent, new RegExp(expected), `${type}:${id}`);
  }
  assert.match(ids["roadmap-panel"].textContent, /Pending definition/);
  assert(ids["roadmap-nodes"].children.filter(node => node.className.includes(" technology")).length >= 1);
  assert.equal(ids["roadmap-nodes"].children.filter(node => node.className.includes(" branch")).length, 3);
  assert.equal(ids["roadmap-nodes"].children.filter(node => node.className.includes(" external")).length, 0);
  assert.equal(ids["roadmap-lines"].children.filter(node => node.tag === "path").length, 3);
  assert.match(ids["roadmap-nodes"].textContent, /Agriculture|Domestication|Other technologies/);
  const agricultureBranch = ids["roadmap-nodes"].children.find(node => node.className.includes(" branch") && node.textContent.includes("Agriculture"));
  assert(agricultureBranch);
  const routeBeforeBranch = location.hash;
  agricultureBranch.onclick();
  assert.equal(location.hash, routeBeforeBranch);
  assert.match(ids.notice.textContent, /future agriculture technologies/);
  assert.equal(ids.workspace.hidden, true);
  assert.equal(ids["technology-view"].hidden, false);
  ids["roadmap-zoom-in"].onclick();
  assert.equal(ids["roadmap-zoom-value"].textContent, "115%");
  ids["roadmap-zoom-out"].onclick();
  assert.equal(ids["roadmap-zoom-value"].textContent, "100%");
  ids["roadmap-viewport"].clientWidth = 200;
  ids["roadmap-center"].onclick();
  assert(ids["roadmap-viewport"].scrollLeft > 0);
  ids.search.value = "language"; ids.search.oninput({ target: ids.search });
  assert.equal(ids.count.textContent, "1 / 1");
  const languagePoint = descendants(ids["roadmap-nodes"]).find(node => node.title === "language");
  assert(languagePoint);
  languagePoint.onclick();
  assert.equal(new URLSearchParams(location.hash.slice(1)).get("id"), "language");
  assert.match(ids["roadmap-panel"].textContent, /Language/);
  const closeRoadmap = descendants(ids["roadmap-panel"]).find(node => node.className === "roadmap-close");
  closeRoadmap.onclick();
  assert.equal(new URLSearchParams(location.hash.slice(1)).get("type"), "technologies");
  assert.equal(ids["roadmap-panel"].hidden, true);
  history.back();
  assert.equal(new URLSearchParams(location.hash.slice(1)).get("id"), "language");
  assert.equal(ids["roadmap-panel"].hidden, false);
  ids.language.value = "es"; ids.language.onchange({ target: ids.language });
  assert.match(ids["roadmap-panel"].textContent, /Lenguaje/);
  ids.search.value = "lenguaje"; ids.search.oninput({ target: ids.search });
  assert.equal(ids.count.textContent, "1 / 1");
  location.hash = "#type=technologies&id=language";
  listeners.hashchange();
  assert.match(ids["roadmap-panel"].textContent, /Lenguaje/);
  ids.language.value = "en"; ids.language.onchange({ target: ids.language });
  vm.runInContext("navigate('world', 'tropical_forest')", context);
  assert.doesNotMatch(ids.detail.textContent, /Regional variants|regional variant IDs/i);
  vm.runInContext("navigate('minerals')", context);
  ids.search.value = "geological_clay"; ids.search.oninput({ target: ids.search });
  assert.equal(ids.count.textContent, "1 / 45");
  assert.match(ids.list.textContent, /Clay/);
  vm.runInContext("navigate('resources', 'RES#000047')", context);
  assert.match(ids.detail.textContent, /Documented production branches/);
  assert.match(ids.detail.textContent, /adobe_moulding_drying/);
  assert.match(ids.detail.textContent, /brick_firing/);
  location.hash = "#type=resources&id=RES%23DOES_NOT_EXIST";
  listeners.hashchange();
  assert.match(ids.notice.textContent, /Reference not found/);
  assert(!ids.detail.textContent.includes("RES#000031"));
  const css = fs.readFileSync(path.join(wiki, "style.css"), "utf8");
  assert.match(css, /@media\(max-width:900px\)/);
  assert.match(css, /@media\(max-width:450px\)/);
  assert(!requested.some(file => file.includes("biological")));
  console.log("UI: direct encoded URL, links, browser back/forward, six sections, search, language and no biology fetch passed");
}
ready().catch(error => { console.error(error); process.exitCode = 1; });
