"use strict";
const assert = require("node:assert/strict");
const fs = require("node:fs");
const path = require("node:path");
const vm = require("node:vm");

const wiki = path.resolve(__dirname, "..");
const root = path.resolve(wiki, "..");
const files = {
  world: "Biomas/biomes.json",
  animals: "Resources/fauna.json",
  plants: "Resources/flora.json",
  minerals: "Resources/minerals.json",
  resources: "Resources/resources.json",
  technologies: "Technologies/agriculture.tech.json"
};
const data = Object.fromEntries(Object.entries(files).map(([type, file]) =>
  [type, JSON.parse(fs.readFileSync(path.join(root, file), "utf8"))]));

const ctx = vm.createContext({});
vm.runInContext(fs.readFileSync(path.join(wiki, "catalog-index.js"), "utf8"), ctx);
const index = ctx.EvolveCatalogIndex(data);
const list = (value) => Array.isArray(value) ? value : [];
const subbiomes = data.world.biomes.flatMap(biome => list(biome.subbiomes));

assert.equal(data.world.biomes.length, 8);
assert.equal(subbiomes.length, 21);
assert.equal(index.list("world").length, 29);
assert.equal(index.list("animals").length, data.animals.animals.length);
assert.equal(index.list("plants").length, data.plants.plants.length);
assert.equal(index.list("minerals").length, data.minerals.minerals.length);
assert.equal(index.list("resources").length, data.resources.resources.length);
assert.equal(index.list("technologies").length, data.technologies.technologies.length);
assert.equal(index.list("processes").length, data.resources.processingLinks.length);
assert.equal(index.list("variants").length, 0);
assert.equal(index.edges.size, new Set(index.edges.keys()).size);

for (const biome of data.world.biomes) {
  const parent = index.resolve("world", biome.id);
  assert(parent, "missing biome: " + biome.id);
  const children = index.between(parent, "subbiome");
  assert.equal(children.length, biome.subbiomes.length);
  for (const sub of biome.subbiomes) {
    const entry = index.resolve("world", sub.id);
    assert(entry, "missing subbiome: " + sub.id);
    assert.equal(entry.value.biomeId, biome.id);
    assert(children.some(edge => edge.to.id === sub.id));
    for (const [field, type] of [["fauna", "animals"], ["flora", "plants"], ["minerals", "minerals"]]) {
      for (const id of list(sub[field])) {
        assert(index.resolve(type, id), "missing linked entity: " + id);
        assert(index.between(entry, "habitat").some(edge => edge.to.id === id && edge.to.type === type));
      }
    }
  }
}
for (const [type, field] of [["animals", "fauna"], ["plants", "flora"], ["minerals", "minerals"]]) {
  for (const item of data[type][type]) {
    for (const sid of list(item.spawn)) {
      const sub = subbiomes.find(s => s.id === sid);
      assert(sub, "unknown spawn subbiome: " + sid);
      assert(list(sub[field]).includes(item.id), "missing inverse spawn link: " + item.id);
    }
  }
}
const forests = data.world.biomes.find(b => b.id === "forest");
assert(forests);
assert.equal(forests.subbiomes.length, 5);
for (const sub of forests.subbiomes) {
  assert(sub.spawn, "missing forest spawn rules: " + sub.id);
  assert(sub.spawn.required && Object.keys(sub.spawn.required).length > 0);
  assert(sub.spawn.preferred && Object.keys(sub.spawn.preferred).length > 0);
}
assert.equal(subbiomes.filter(sub => sub.spawn).length, 5);
assert.equal(subbiomes.filter(sub => !sub.spawn).length, 16);
assert.equal(data.animals.animals.length, 0, "the rebuilt fauna catalog must remain empty for now");
assert.equal(data.plants.plants.length, 0, "the rebuilt flora catalog must remain empty for now");
assert.equal(data.minerals.minerals.length, 0, "the rebuilt mineral catalog must remain empty for now");

const offlineScript = fs.readFileSync(path.join(wiki, "offline-data.js"), "utf8");
const offlineContext = vm.createContext({ window: {} });
vm.runInContext(offlineScript, offlineContext);
const cached = offlineContext.window.EvolveOfflineCatalogs;
for (const type of Object.keys(files)) {
  assert.deepEqual(JSON.parse(JSON.stringify(cached[type])), data[type], "stale offline snapshot: " + type);
}
const app = fs.readFileSync(path.join(wiki, "app.js"), "utf8");
for (const [type, file] of Object.entries(files)) {
  assert(app.includes("../" + file), "Wiki must read active catalog " + type);
}
assert(app.includes("renderSpawnRules(area, value.spawn)"), "subbiome view must render spawn rules");
assert(app.includes("rules.required"), "required spawn rules must be shown");
assert(app.includes("rules.preferred"), "preferred spawn rules must be shown");
assert(!app.includes("../old/"), "Wiki must not read archived catalogs");
const html = fs.readFileSync(path.join(wiki, "index.html"), "utf8");
assert(html.includes("offline-data.js"), "offline mode must load current catalog snapshot");
assert(index.resolve("technologies", data.technologies.rootTechnologyId));
assert(index.resolve("resources", "RES#000047"));
console.log("PASS: active catalogs, 8 biomes, 21 subbiomes, 5 forest spawn definitions, 16 pending");
console.log("PASS: references, offline snapshot, local paths and Wiki spawn UI");
