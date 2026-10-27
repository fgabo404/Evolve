import assert from 'node:assert/strict';

const canvasEvents = new Map();
const buttonEvents = new Map();
const frames = [];
const requests = [];
let cachePaths = 0;
let screenPaths = 0;
let imageBlits = 0;
let highlights = 0;
let releaseMedium;

function fakeContext(isCache) {
  return {
    setTransform() {}, scale() {}, translate() {}, save() {}, restore() {},
    fillRect() {}, beginPath() {}, lineTo() {}, closePath() {},
    fill() { if (!isCache) highlights++; }, stroke() {},
    moveTo() { if (isCache) cachePaths++; else screenPaths++; },
    drawImage() { imageBlits++; }
  };
}

const screen = fakeContext(false);
const canvas = {
  clientWidth: 1000, clientHeight: 700, width: 0, height: 0,
  getContext() { return screen; },
  getBoundingClientRect() { return { left: 0, top: 0, width: 1000, height: 700 }; },
  addEventListener(name, callback) { canvasEvents.set(name, callback); },
  setPointerCapture() {}
};
const sizeSelect = { value: 'small', disabled: false };
const button = { disabled: false, addEventListener(name, callback) { buttonEvents.set(name, callback); } };
const status = { textContent: '' };
const error = { hidden: true, textContent: '' };
const elements = { 'simulator-canvas': canvas, 'grid-size': sizeSelect,
  'generate-grid': button, 'generation-status': status, 'generation-error': error };

globalThis.document = {
  getElementById(id) { return elements[id]; },
  createElement(name) {
    assert.equal(name, 'canvas');
    return { width: 0, height: 0, getContext() { return fakeContext(true); } };
  }
};
globalThis.window = { devicePixelRatio: 2, addEventListener() {} };
globalThis.requestAnimationFrame = callback => { frames.push(callback); };
globalThis.fetch = async (url, options) => {
  requests.push({ url, options });
  if (url.startsWith('/api/grid/cells/')) {
    const [, column, row] = url.match(/cells\/(\d+)\/(\d+)/);
    return { ok: true, json: async () => ({ id: Number(row) * 100 + Number(column),
      column: Number(column), row: Number(row), neighbors: [] }) };
  }
  assert.equal(url, '/api/grid');
  assert.equal(options.method, 'POST');
  const size = JSON.parse(options.body).size;
  const [columns, rows] = { small: [100, 70], medium: [150, 100], large: [250, 200] }[size];
  const result = { ok: true, json: async () => ({ size, columns, rows, cellCount: columns * rows,
    layout: 'odd-q', orientation: 'flat-top', wrapHorizontal: true, wrapVertical: false }) };
  if (size === 'medium') return await new Promise(resolve => { releaseMedium = () => resolve(result); });
  return result;
};

function flushFrames() { while (frames.length) frames.shift()(); }

await import('../wwwroot/app.js');
flushFrames();
assert.equal(requests.length, 0, 'page load never requests a grid');
assert.equal(screenPaths, 0, 'initial canvas remains blank');

sizeSelect.value = 'large';
assert.equal(requests.length, 0, 'changing size does not generate');
await buttonEvents.get('click')();
flushFrames();
assert.equal(requests.length, 1);
assert.equal(cachePaths, 50_400, 'overview cache built once, including seam columns');
assert.equal(screenPaths, 0, 'overview does not trace 50,000 cells on screen');
const blitsBeforePan = imageBlits;
canvasEvents.get('pointerdown')({ pointerId: 1, clientX: 500, clientY: 350 });
canvasEvents.get('pointermove')({ pointerId: 1, clientX: 550, clientY: 350 });
flushFrames();
assert.equal(cachePaths, 50_400, 'pan reuses cache');
assert.equal(screenPaths, 0, 'pan does not trace cells');
assert.ok(imageBlits > blitsBeforePan && imageBlits - blitsBeforePan <= 3);
assert.equal(requests.length, 1, 'pan does not regenerate');
canvasEvents.get('pointercancel')();

for (let i = 0; i < 8; i++) {
  canvasEvents.get('wheel')({ clientX: 500, clientY: 350, deltaY: -1, preventDefault() {} });
}
flushFrames();
assert.ok(screenPaths > 0 && screenPaths < 15_000,
  'close zoom traces only the cells near the viewport');
assert.equal(requests.length, 1, 'zoom does not regenerate');

sizeSelect.value = 'small';
await buttonEvents.get('click')();
flushFrames();
assert.equal(requests.length, 2, 'button replaces the active grid');
assert.equal(sizeSelect.value, 'small');
assert.equal(error.hidden, true);
canvasEvents.get('pointerdown')({ pointerId: 2, clientX: 500, clientY: 350 });
canvasEvents.get('pointerup')({ pointerId: 2, clientX: 500, clientY: 350 });
await new Promise(resolve => setTimeout(resolve, 0));
flushFrames();
assert.ok(requests.some(request => request.url.startsWith('/api/grid/cells/')),
  'click resolves selection through the server');
assert.ok(highlights > 0, 'selected cell is highlighted');

sizeSelect.value = 'medium';
const pending = buttonEvents.get('click')();
assert.equal(status.textContent, 'Generando…');
assert.equal(button.disabled, true);
await buttonEvents.get('click')();
assert.equal(requests.filter(request => request.options?.method === 'POST').length, 3,
  'repeated clicks do not start another request');
releaseMedium();
await pending;
flushFrames();
assert.equal(button.disabled, false);
assert.equal(status.textContent, '');
console.log('PASS: blank startup, explicit generation, cache reuse, visible-cell drawing and size change');
