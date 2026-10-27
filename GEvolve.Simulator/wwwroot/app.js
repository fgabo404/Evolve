import { RADIUS, ROOT3, cellAt, mapHeight, periodWidth } from './grid-geometry.js';

const canvas = document.getElementById('simulator-canvas');
const context = canvas.getContext('2d');
const sizeSelect = document.getElementById('grid-size');
const generateButton = document.getElementById('generate-grid');
const status = document.getElementById('generation-status');
const errorMessage = document.getElementById('generation-error');
const COLUMN_STEP = 1.5 * RADIUS;
const ROW_STEP = ROOT3 * RADIUS;
const HALF_HEIGHT = ROW_STEP / 2;
let grid = null;
let overviewCache = null;
let selected = null;
let scale = 1;
let fitScale = 1;
let cameraX = 0;
let cameraY = 0;
let drawQueued = false;
let pointer = null;
let generating = false;

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
  const ratio = Math.min(window.devicePixelRatio || 1, 2);
  canvas.width = Math.round(bounds.width * ratio);
  canvas.height = Math.round(bounds.height * ratio);
  context.setTransform(ratio, 0, 0, ratio, 0, 0);
  overviewCache = null;
  if (grid) fitGrid();
  else queueDraw();
}

function addHexPath(target, x, y) {
  target.moveTo(x + RADIUS, y);
  target.lineTo(x + RADIUS / 2, y + HALF_HEIGHT);
  target.lineTo(x - RADIUS / 2, y + HALF_HEIGHT);
  target.lineTo(x - RADIUS, y);
  target.lineTo(x - RADIUS / 2, y - HALF_HEIGHT);
  target.lineTo(x + RADIUS / 2, y - HALF_HEIGHT);
  target.closePath();
}

// One low-resolution image is built per generated grid. Overview redraws only blit
// visible horizontal slices, so panning never traverses all 50,000 cells.
function getOverviewCache() {
  if (overviewCache) return overviewCache;
  const bitmap = document.createElement('canvas');
  const density = 0.5;
  bitmap.width = Math.ceil(periodWidth(grid.columns) * density);
  bitmap.height = Math.ceil(mapHeight(grid.rows) * density);
  const target = bitmap.getContext('2d');
  target.scale(density, density);
  target.beginPath();
  for (let row = 0; row < grid.rows; row++) {
    for (let column = -1; column <= grid.columns; column++) {
      addHexPath(target, RADIUS * (1 + 1.5 * column),
        ROW_STEP * (row + 0.5 * (column & 1) + 0.5));
    }
  }
  target.strokeStyle = '#c5cdd5';
  target.lineWidth = Math.max(0.7 / fitScale, 1.4);
  target.stroke();
  overviewCache = bitmap;
  return bitmap;
}

function drawOverview(right) {
  const bitmap = getOverviewCache();
  const period = periodWidth(grid.columns);
  const height = mapHeight(grid.rows);
  for (let copy = Math.floor(cameraX / period); copy * period < right; copy++) {
    const left = Math.max(cameraX, copy * period);
    const edge = Math.min(right, (copy + 1) * period);
    if (edge <= left) continue;
    const sourceX = (left - copy * period) * bitmap.width / period;
    const sourceWidth = (edge - left) * bitmap.width / period;
    context.drawImage(bitmap, sourceX, 0, sourceWidth, bitmap.height,
      left, 0, edge - left, height);
  }
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
  if (RADIUS * scale < 4) {
    drawOverview(right);
  } else {
    context.beginPath();
    for (let row = firstRow; row <= lastRow; row++) {
      for (let unwrapped = firstColumn; unwrapped <= lastColumn; unwrapped++) {
        addHexPath(context, RADIUS + COLUMN_STEP * unwrapped,
          ROW_STEP * (row + 0.5 * (unwrapped & 1) + 0.5));
      }
    }
    context.strokeStyle = '#c5cdd5';
    context.lineWidth = 0.7 / scale;
    context.stroke();
  }

  if (selected && selected.row >= firstRow && selected.row <= lastRow) {
    for (let unwrapped = firstColumn; unwrapped <= lastColumn; unwrapped++) {
      if (((unwrapped % grid.columns) + grid.columns) % grid.columns !== selected.column) continue;
      context.beginPath();
      addHexPath(context, RADIUS + COLUMN_STEP * unwrapped,
        ROW_STEP * (selected.row + 0.5 * (unwrapped & 1) + 0.5));
      context.fillStyle = '#e3ecf5';
      context.fill();
      context.strokeStyle = '#627e9a';
      context.lineWidth = 1.5 / scale;
      context.stroke();
    }
  }
  context.restore();
}

async function generateGrid(size) {
  const response = await fetch('/api/grid', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ size })
  });
  if (!response.ok) throw new Error(`Grid HTTP ${response.status}`);
  const next = await response.json();
  if (next.layout !== 'odd-q' || next.orientation !== 'flat-top' ||
      next.cellCount !== next.columns * next.rows ||
      !next.wrapHorizontal || next.wrapVertical) throw new Error('Unsupported grid.');
  grid = next;
  overviewCache = null;
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
  scale = Math.max(fitScale * 0.75, Math.min(Math.min(fitScale * 40, 12), scale * factor));
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

generateButton.addEventListener('click', async () => {
  if (generating) return;
  generating = true;
  generateButton.disabled = true;
  sizeSelect.disabled = true;
  status.textContent = 'Generando…';
  errorMessage.hidden = true;
  try { await generateGrid(sizeSelect.value); }
  catch (error) {
    console.error(error);
    errorMessage.textContent = `No se pudo generar el grid: ${error.message}`;
    errorMessage.hidden = false;
  } finally {
    generating = false;
    generateButton.disabled = false;
    sizeSelect.disabled = false;
    status.textContent = '';
  }
});

window.addEventListener('resize', resizeCanvas);
resizeCanvas();
