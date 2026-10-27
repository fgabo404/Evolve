"use strict";

// The diagram projects technology prerequisites and separately declared visual branches.
globalThis.EvolveTechnologyRoadmap = function createTechnologyRoadmap(catalog) {
  const technologies = catalog.technologies || [];
  const byId = new Map(technologies.map(value => [value.id, value]));
  if (byId.size !== technologies.length) throw Error("Duplicate technology IDs");
  const nodes = new Map(technologies.map(value => [value.id, { id: value.id, kind: "technology", value }]));
  const edges = [];
  const dependencies = new Map(technologies.map(value => [value.id, new Set()]));
  const dependents = new Map(technologies.map(value => [value.id, new Set()]));
  const declaredExternal = new Set(catalog.externalReferences?.technologyIds || []);

  function connect(from, to, owner, scope = null, kind = "prerequisite") {
    if (!edges.some(edge => edge.from === from && edge.to === to && edge.owner === owner))
      edges.push({ from, to, owner, scope, kind });
  }
  function expression(value, owner, path) {
    if (!value || typeof value !== "object") return null;
    const operator = Object.hasOwn(value, "all") ? "all" : Object.hasOwn(value, "any") ? "any" : null;
    if (operator) {
      if (!Array.isArray(value[operator]) || operator === "any" && value[operator].length === 0)
        throw Error(`Invalid ${operator} prerequisite for ${owner}`);
      const inputs = value[operator]
        .map((child, index) => expression(child, owner, `${path}.${operator}.${index}`)).filter(Boolean);
      if (!inputs.length) return null;
      if (inputs.length === 1) return inputs[0];
      const id = `gate:${owner}:${path}`;
      nodes.set(id, { id, kind: "gate", operator, owner });
      for (const input of inputs) connect(input.id, id, owner, input.scope);
      return { id };
    }
    if (value.type && value.type !== "technology_completed") {
      const id = `condition:${owner}:${path}`;
      nodes.set(id, { id, kind: "condition", value, owner });
      return { id };
    }
    if (value.type !== "technology_completed" || typeof value.technologyId !== "string") return null;
    const id = value.technologyId;
    if (!byId.has(id)) {
      if (!declaredExternal.has(id)) throw Error(`Undeclared technology prerequisite: ${id}`);
      if (!nodes.has(id)) nodes.set(id, { id, kind: "external" });
    }
    dependencies.get(owner).add(id);
    if (byId.has(id)) dependents.get(id).add(owner);
    return { id, scope: value.scope || null };
  }
  for (const technology of technologies) {
    const prerequisite = expression(technology.prerequisites, technology.id, "root");
    if (prerequisite) connect(prerequisite.id, technology.id, technology.id, prerequisite.scope);
  }
  const branches = catalog.branches || [];
  if (branches.length && !byId.has(catalog.rootTechnologyId)) throw Error("Visual branches require an existing root technology");
  for (const [order, branch] of branches.entries()) {
    if (!branch.id || !branch.name?.en || !branch.name?.es) throw Error("Incomplete visual branch");
    const id = `branch:${branch.id}`;
    if (nodes.has(id)) throw Error(`Duplicate visual branch: ${branch.id}`);
    nodes.set(id, { id, kind: "branch", value: branch, order });
    connect(catalog.rootTechnologyId, id, catalog.rootTechnologyId, null, "branch");
  }
  const incoming = new Map([...nodes.keys()].map(id => [id, []]));
  const outgoing = new Map([...nodes.keys()].map(id => [id, []]));
  for (const edge of edges) { incoming.get(edge.to).push(edge); outgoing.get(edge.from).push(edge); }
  const degree = new Map([...nodes.keys()].map(id => [id, incoming.get(id).length]));
  const levels = new Map([...nodes.keys()].map(id => [id, 0]));
  const queue = [...nodes.keys()].filter(id => degree.get(id) === 0);
  let processed = 0;
  while (queue.length) {
    const id = queue.shift(); processed++;
    for (const edge of outgoing.get(id)) {
      levels.set(edge.to, Math.max(levels.get(edge.to), levels.get(id) + 1));
      degree.set(edge.to, degree.get(edge.to) - 1);
      if (degree.get(edge.to) === 0) queue.push(edge.to);
    }
  }
  if (processed !== nodes.size) throw Error("Technology prerequisite cycle");
  if (catalog.rootTechnologyId) {
    if (!byId.has(catalog.rootTechnologyId)) throw Error(`Missing root technology: ${catalog.rootTechnologyId}`);
    const reachable = new Set([catalog.rootTechnologyId]);
    const queue = [catalog.rootTechnologyId];
    while (queue.length) {
      for (const edge of outgoing.get(queue.shift())) {
        if (reachable.has(edge.to)) continue;
        reachable.add(edge.to);
        queue.push(edge.to);
      }
    }
    for (const id of byId.keys()) if (!reachable.has(id)) throw Error(`Technology does not descend from ${catalog.rootTechnologyId}: ${id}`);
  }
  const columns = [];
  for (const [id, level] of levels) (columns[level] ||= []).push(id);
  const order = id => nodes.get(id).order ?? nodes.get(id).value?.order ?? Number.MAX_SAFE_INTEGER;
  for (const column of columns) column.sort((a, b) => order(a) - order(b) || a.localeCompare(b));
  // Barycentric passes reduce crossings while keeping every level in its DAG column.
  for (let pass = 0; pass < 4; pass++) {
    for (let level = 1; level < columns.length; level++) {
      const previous = new Map(columns[level - 1].map((id, index) => [id, index]));
      const score = id => {
        const parents = incoming.get(id).map(edge => previous.get(edge.from)).filter(index => index !== undefined);
        return parents.length ? parents.reduce((sum, index) => sum + index, 0) / parents.length : Number.MAX_SAFE_INTEGER;
      };
      columns[level].sort((a, b) => score(a) - score(b) || order(a) - order(b) || a.localeCompare(b));
    }
  }
  const maxRows = Math.max(...columns.map(column => column.length));
  const openingBranches = branches.length > 0 && technologies.length === 1;
  const columnGap = openingBranches ? 360 : 270;
  const rowGap = openingBranches ? 180 : 88;
  const width = columns.length * columnGap + 90;
  const height = Math.max(640, maxRows * rowGap + 120);
  for (let level = 0; level < columns.length; level++) {
    const column = columns[level];
    for (let row = 0; row < column.length; row++) {
      const node = nodes.get(column[row]);
      node.x = 46 + level * columnGap;
      node.y = 48 + (row + (maxRows - column.length) / 2) * rowGap;
      node.level = level;
    }
  }
  if (branches.length) {
    const root = nodes.get(catalog.rootTechnologyId);
    root.y = height / 2 - 24;
    const middle = (branches.length - 1) / 2;
    branches.forEach((branch, index) => { nodes.get(`branch:${branch.id}`).y = root.y + (index - middle) * 180; });
  }
  return { nodes, edges, dependencies, dependents, incoming, outgoing, width, height, columns };
};
