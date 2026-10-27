"use strict";

// All links come from IDs or explicit tag mappings in the catalog snapshots.
// This file never interprets a design guide as a playable runtime rule.
globalThis.EvolveCatalogIndex = function createCatalogIndex(data) {
  const nodes = new Map();
  const aliases = new Map();
  const byTag = new Map();
  const edges = new Map();
  const outgoing = new Map();
  const incoming = new Map();
  const unresolved = new Map();
  const key = (type, id) => `${type}:${id}`;
  const list = value => Array.isArray(value) ? value : [];

  function register(type, rows) {
    for (const value of list(rows)) {
      if (!value?.id) continue;
      const node = { type, id: value.id, value };
      nodes.set(key(type, value.id), node);
      for (const tag of [value.tag, ...list(value.tags)]) {
        if (!tag) continue;
        const tagKey = key(type, tag);
        if (!byTag.has(tagKey)) byTag.set(tagKey, new Set());
        byTag.get(tagKey).add(node);
      }
    }
  }

  register("world", data.world.biomes);
  register("animals", data.animals.speciesCandidates);
  register("plants", [...list(data.plants.speciesCandidates), ...list(data.plants.agriculturalCropCandidates)]);
  register("minerals", data.minerals.geologicalResourceCandidates);
  register("resources", data.resources.resources);
  register("technologies", data.technologies.technologies);
  register("processes", data.resources.processingLinks.map(link => ({ id: link.processTag, ...link })));

  for (const [type, catalog] of [["animals", data.animals], ["plants", data.plants], ["minerals", data.minerals]]) {
    for (const alias of list(catalog.entityAliases)) {
      if (alias.previousId && alias.entityId) aliases.set(key(type, alias.previousId), alias.entityId);
    }
  }

  function resolve(type, id) {
    if (!id) return null;
    return nodes.get(key(type, id)) || nodes.get(key(type, aliases.get(key(type, id)))) || null;
  }

  function missing(type, id, context) {
    const ref = key(type, id);
    if (!unresolved.has(ref)) unresolved.set(ref, { type, id, contexts: new Set() });
    unresolved.get(ref).contexts.add(context);
  }

  function connect(fromType, fromId, toType, toId, kind, evidence, context) {
    const from = resolve(fromType, fromId);
    const to = resolve(toType, toId);
    if (!from) missing(fromType, fromId, context || kind);
    if (!to) missing(toType, toId, context || kind);
    if (!from || !to) return null;
    const edgeKey = `${key(from.type, from.id)}|${kind}|${key(to.type, to.id)}`;
    let edge = edges.get(edgeKey);
    if (!edge) {
      edge = { from, to, kind, evidence: [] };
      edges.set(edgeKey, edge);
      if (!outgoing.has(key(from.type, from.id))) outgoing.set(key(from.type, from.id), []);
      if (!incoming.has(key(to.type, to.id))) incoming.set(key(to.type, to.id), []);
      outgoing.get(key(from.type, from.id)).push(edge);
      incoming.get(key(to.type, to.id)).push(edge);
    }
    if (evidence && !edge.evidence.includes(evidence)) edge.evidence.push(evidence);
    return edge;
  }

  function sourceType(collection) {
    return ({ animalSpeciesCandidates: "animals", plantSpeciesCandidates: "plants",
      agriculturalCropCandidates: "plants", geologicalResourceCandidates: "minerals" })[collection];
  }

  const tagMappings = new Map();
  for (const mapping of list(data.resources.sourceTagMappings)) {
    if (mapping.targetKind !== "resource") continue;
    tagMappings.set(mapping.sourceTag, list(mapping.resourceIds));
  }
  function tagResources(tag) {
    const mapped = list(tagMappings.get(tag)).map(id => resolve("resources", id)).filter(Boolean);
    return [...new Map([...mapped, ...(byTag.get(key("resources", tag)) || [])]
      .map(node => [node.id, node])).values()];
  }

  const biomeForVariant = new Map(list(data.world.regionalVariants).map(value => [value.id, value.biomeId]));
  for (const occurrence of list(data.world.resourceOccurrences)) {
    const type = ({ animal: "animals", plant: "plants", geological: "minerals" })[occurrence.kind];
    if (!type) continue;
    connect("world", occurrence.biomeId, type, occurrence.definitionId, "occurrence", occurrence, occurrence.id);
  }
  for (const type of ["animals", "plants"]) {
    for (const entity of [...nodes.values()].filter(node => node.type === type)) {
      const value = entity.value;
      for (const variantId of list(value.biomeVariantIds)) {
        const biomeId = biomeForVariant.get(variantId) || variantId;
        if (resolve("world", biomeId)) connect("world", biomeId, type, entity.id, "habitat", value, "biomeVariantIds");
        else missing("world", biomeId, `${entity.id}.biomeVariantIds`);
      }
      for (const product of [...list(value.potentialProducts), ...list(value.products)]) {
        const resourceIds = product.resourceId ? [product.resourceId] : tagMappings.get(product.resourceTag) || [];
        for (const resourceId of resourceIds) connect(type, entity.id, "resources", resourceId, "source", product, `${entity.id}.products`);
      }
      if (value.cultivationResearch?.technologyId) {
        connect(type, entity.id, "technologies", value.cultivationResearch.technologyId,
          "cultivation", value.cultivationResearch, `${entity.id}.cultivationResearch`);
      }
    }
  }
  for (const mineral of list(data.minerals.geologicalResourceCandidates)) {
    if (mineral.extractedResourceId) connect("minerals", mineral.id, "resources", mineral.extractedResourceId, "source", mineral, "extractedResourceId");
    if (mineral.refinedOutputResourceId) connect("minerals", mineral.id, "resources", mineral.refinedOutputResourceId, "refined", mineral, "refinedOutputResourceId");
  }
  for (const binding of list(data.resources.sourceBindings)) {
    const type = sourceType(binding.sourceCollection);
    if (type) connect(type, binding.sourceId, "resources", binding.resourceId, "source", binding, "sourceBindings");
  }
  for (const process of list(data.resources.processingLinks)) {
    for (const id of list(process.inputResourceIds)) connect("resources", id, "processes", process.processTag, "process_input", process, "processingLinks.inputResourceIds");
    for (const id of list(process.processProfile?.auxiliaryResourceIds)) connect("resources", id, "processes", process.processTag, "process_auxiliary", process, "processingLinks.processProfile.auxiliaryResourceIds");
    for (const id of list(process.outputResourceIds)) connect("processes", process.processTag, "resources", id, "process_output", process, "processingLinks.outputResourceIds");
    for (const id of list(process.requiredTechnologyIds)) connect("processes", process.processTag, "technologies", id, "technology", process, "processingLinks.requiredTechnologyIds");
  }

  function visitTechnology(value, techId, path = "") {
    if (!value || typeof value !== "object") return;
    if (Array.isArray(value)) { value.forEach((item, index) => visitTechnology(item, techId, `${path}[${index}]`)); return; }
    if (typeof value.technologyId === "string") connect("technologies", techId, "technologies", value.technologyId,
      path.startsWith("prerequisites") ? "prerequisite" : "technology", { path, value }, path);
    if (typeof value.resourceId === "string") connect("technologies", techId, "resources", value.resourceId, "reference", { path, value }, path);
    for (const tag of [value.resourceTag, String(value.type || "").startsWith("resource_tag_") ? value.tag : null].filter(Boolean)) {
      const matches = tagResources(tag);
      if (!matches.length) missing("resource_tag", tag, `${techId}.${path}`);
      for (const resource of matches) connect("technologies", techId, "resources", resource.id, "tag_reference", { path, value }, `${techId}.${path}`);
    }
    for (const [field, child] of Object.entries(value)) {
      if (field !== "designNotes" && field !== "description" && field !== "name") visitTechnology(child, techId, path ? `${path}.${field}` : field);
    }
  }
  for (const tech of list(data.technologies.technologies)) visitTechnology(tech, tech.id);

  return {
    nodes, aliases, byTag, edges, unresolved,
    resolve, list: type => [...nodes.values()].filter(node => node.type === type),
    outgoing: node => outgoing.get(key(node.type, node.id)) || [],
    incoming: node => incoming.get(key(node.type, node.id)) || [],
    between: (node, kind, direction = "out") => (direction === "out" ? outgoing : incoming)
      .get(key(node.type, node.id))?.filter(edge => edge.kind === kind) || [],
    tagResources,
    sourceType
  };
};
