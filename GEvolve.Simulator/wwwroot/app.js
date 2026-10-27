const canvas = document.getElementById('simulator-canvas');
const context = canvas.getContext('2d');

function resizeCanvas() {
  const { width, height } = canvas.getBoundingClientRect();
  const ratio = window.devicePixelRatio || 1;

  canvas.width = Math.round(width * ratio);
  canvas.height = Math.round(height * ratio);
  context.setTransform(ratio, 0, 0, ratio, 0, 0);
}

window.addEventListener('resize', resizeCanvas);
resizeCanvas();
