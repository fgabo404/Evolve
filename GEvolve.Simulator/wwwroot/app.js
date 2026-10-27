import { RADIUS, ROOT3, cellAt, center, mapHeight, periodWidth } from './grid-geometry.js';

const canvas = document.getElementById('simulator-canvas');
const context = canvas.getContext('2d');
const sizeSelect = document.getElementById('grid-size');
let grid = null;
let selected = null;
let scale = 1;
let fitScale = 1;
let cameraX = 0;
let cameraY = 0;
let drawQueued = false;
let pointer = null;

function queueDraw() {
  if (drawQueued) return;
  drawQueued = true;
  requestAnimationFrame(draw);
}

function normalizeCamera() {
  const period = periodWidth(grid.columns);
  cameraX = ((cameraX % period) + period) % period;
  const height = mapHeight(grid.rows);
  const visibleHeight = canvas.clientHeight / scale;
  cameraY = visibleHeight >= height
    ? (height - visibleHeight) / 2
    : Math.max(-RADIUS, Math.min(height - visibleHeight + RADIUS, cameraY));
}

function fitGrid() {
  if (!grid || !canvas.clientWidth || !canvas.clientHeight) return;
  const period = periodWidth(grid.columns);
  const height = mapHeight(grid.rows);
  fitScale = Math.min(canvas.clientWidth / period, canvas.clientHeight / height) * 0.94;
  scale = fitScale;
  cameraX = (period - canvas.clientWidth / scale) / 2;
  cameraY = (height - canvas.clientHeight / scale) / 2;
  normalizeCamera();
  queueDraw();
}

function resizeCanvas() {
  const bounds = canvas.getBoundingClientRect();
  const ratio = window.devicePixelRatio || 1;
  canvas.width = Math.round(bounds.width * ratio);
  canvas.height = Math.round(bounds.height * ratio);
  context.setTransform(ratio, 0, 0, ratio, 0, 0);
  fitGrid();
}

function addHexPath(x, y) {
  const halfHeight = ROOT3 * RADIUS / 2;
  context.moveTo(x + RADIUS, y);
  context.lineTo(x + RADIUS / 2, y + halfHeight);
  context.lineTo(x - RADIUS / 2, y + halfHeight);
  context.lineTo(x - RADIUS, y);
  context.lineTo(x - RADIUS / 2, y - halfHeight);
  context.lineTo(x + RADIUS / 2, y - halfHeight);
  context.closePath();
}

function draw() {
  drawQueued = false;
  const width = canvas.clientWidth;
  const height = canvas.clientHeight;
  context.fillStyle = '#ffffff';
  context.fillRect(0, 0, width, height);
  if (!grid) return;

  const right = cameraX + width / scale;
  const bottom = cameraY + height / scale;
  const firstColumn = Math.floor((cameraX - 2 * RADIUS) / (1.5 * RADIUS));
  const lastColumn = Math.ceil((right + RADIUS) / (1.5 * RADIUS));
  const firstRow = Math.max(0, Math.floor(cameraY / (ROOT3 * RADIUS)) - 2);
  const lastRow = Math.min(grid.rows - 1, Math.ceil(bottom / (ROOT3 * RADIUS)) + 1);

  context.save();
  context.translate(-cameraX * scale, -cameraY * scale);
  context.scale(scale, scale);
  context.beginPath();
  for (let row = firstRow; row <= lastRow; row++) {
    for (let unwrapped = firstColumn; unwrapped <= lastColumn; unwrapped++) {
      const column = ((unwrapped % grid.columns) + grid.columns) % grid.columns;
      if (grid.cells[row * grid.columns + column] === undefined) continue;
      const point = center(unwrapped, row);
      addHexPath(point.x, point.y);
    }
  }
  context.strokeStyle = '#c5cdd5';
  context.lineWidth = 0.7 / scale;
  context.stroke();

  if (selected && selected.row >= firstRow && selected.row <= lastRow) {
    for (let unwrapped = firstColumn; unwrapped <= lastColumn; unwrapped++) {
      if (((unwrapped % grid.columns) + grid.columns) % grid.columns !== selected.column) continue;
      const point = center(unwrapped, selected.row);
      context.beginPath();
      addHexPath(point.x, point.y);
      context.fillStyle = '#e3ecf5';
      context.fill();
      context.strokeStyle = '#627e9a';
      context.lineWidth = 1.5 / scale;
      context.stroke();
    }
  }
  context.restore();
}

async function loadGrid(size) {
  const response = await fetch('/api/grid', size ? {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ size })
  } : undefined);
  if (!response.ok) throw new Error(`Grid HTTP ${response.status}`);
  const next = await response.json();
  if (next.layout !== 'odd-q' || next.orientation !== 'flat-top' ||
      next.cells.length !== next.columns * next.rows) throw new Error('Unsupported grid.');
  grid = next;
  selected = null;
  sizeSelect.value = next.size;
  fitGrid();
}

async function selectAt(clientX, clientY) {
  if (!grid) return;
  const bounds = canvas.getBoundingClientRect();
  const address = cellAt(cameraX + (clientX - bounds.left) / scale,
    cameraY + (clientY - bounds.top) / scale, grid.columns, grid.rows);
  if (!address) return;
  const currentGrid = grid;
  const response = await fetch(`/api/grid/cells/${address.column}/${address.row}`);
  if (!response.ok || grid !== currentGrid) return;
  const cell = await response.json();
  if (grid !== currentGrid) return;
  selected = cell;
  queueDraw();
}

canvas.addEventListener('wheel', event => {
  if (!grid) return;
  event.preventDefault();
  const bounds = canvas.getBoundingClientRect();
  const x = event.clientX - bounds.left;
  const y = event.clientY - bounds.top;
  const worldX = cameraX + x / scale;
  const worldY = cameraY + y / scale;
  const factor = event.deltaY < 0 ? 1.16 : 1 / 1.16;
  scale = Math.max(fitScale * 0.5, Math.min(fitScale * 40, scale * factor));
  cameraX = worldX - x / scale;
  cameraY = worldY - y / scale;
  normalizeCamera();
  queueDraw();
}, { passive: false });

canvas.addEventListener('pointerdown', event => {
  pointer = { id: event.pointerId, x: event.clientX, y: event.clientY,
    startX: event.clientX, startY: event.clientY, moved: false };
  canvas.setPointerCapture(event.pointerId);
});
canvas.addEventListener('pointermove', event => {
  if (!pointer || pointer.id !== event.pointerId || !grid) return;
  const dx = event.clientX - pointer.x;
  const dy = event.clientY - pointer.y;
  if (Math.abs(event.clientX - pointer.startX) +
      Math.abs(event.clientY - pointer.startY) > 3) pointer.moved = true;
  cameraX -= dx / scale;
  cameraY -= dy / scale;
  pointer.x = event.clientX;
  pointer.y = event.clientY;
  normalizeCamera();
  queueDraw();
});
canvas.addEventListener('pointerup', event => {
  if (!pointer || pointer.id !== event.pointerId) return;
  const moved = pointer.moved;
  pointer = null;
  if (!moved) selectAt(event.clientX, event.clientY).catch(console.error);
});
canvas.addEventListener('pointercancel', () => { pointer = null; });

sizeSelect.addEventListener('change', async () => {
  sizeSelect.disabled = true;
  try { await loadGrid(sizeSelect.value); }
  catch (error) { console.error(error); if (grid) sizeSelect.value = grid.size; }
  finally { sizeSelect.disabled = false; }
});

window.addEventListener('resize', resizeCanvas);
resizeCanvas();
loadGrid().catch(console.error);
