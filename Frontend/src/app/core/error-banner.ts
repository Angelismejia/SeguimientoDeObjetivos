/**
 * Muestra los errores directamente en la pantalla (fuera de Angular, con DOM
 * plano) para poder diagnosticar bugs en dispositivos donde no hay forma de
 * conectar las devtools, ej: el celular de un familiar. El usuario puede
 * simplemente sacarle una foto al mensaje en vez de necesitar la consola.
 */
export function showErrorBanner(message: string): void {
  let el = document.getElementById('__global-error-banner');
  if (!el) {
    el = document.createElement('div');
    el.id = '__global-error-banner';
    el.style.cssText = [
      'position:fixed', 'left:0', 'right:0', 'bottom:0', 'z-index:2147483647',
      'background:#7f1d1d', 'color:#fff', 'font-family:monospace',
      'font-size:11px', 'line-height:1.4', 'padding:10px 12px',
      'max-height:45vh', 'overflow-y:auto', 'white-space:pre-wrap',
      'word-break:break-word', 'box-shadow:0 -2px 12px rgba(0,0,0,0.4)'
    ].join(';');

    const closeBtn = document.createElement('button');
    closeBtn.textContent = 'Cerrar aviso de error ✕';
    closeBtn.style.cssText = [
      'display:block', 'margin-bottom:8px', 'padding:6px 10px',
      'background:#fff', 'color:#7f1d1d', 'border:none', 'border-radius:6px',
      'font-weight:700', 'font-size:11px', 'cursor:pointer'
    ].join(';');
    closeBtn.onclick = () => el?.remove();
    el.appendChild(closeBtn);

    document.body.appendChild(el);
  }

  const line = document.createElement('div');
  line.style.cssText = 'border-top:1px solid rgba(255,255,255,0.25);padding-top:6px;margin-top:6px;';
  line.textContent = `[${new Date().toLocaleTimeString()}] ${message}`;
  el.appendChild(line);
}
