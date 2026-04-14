/**
 * While the pointer is over a horizontally scrollable strip, maps wheel movement to
 * scrollLeft only — the page does not scroll vertically (capture phase + preventDefault).
 */
export function attachHorizontalWheelScroll(el: HTMLElement): () => void {
  const handler = (event: WheelEvent): void => {
    if (el.scrollWidth <= el.clientWidth + 1) return;
    if (event.ctrlKey) return;

    const dx = event.deltaX;
    const dy = event.deltaY;
    const raw = Math.abs(dx) > Math.abs(dy) ? dx : dy;
    if (raw === 0) return;

    const deltaPx = normalizeWheelToPixels(raw, event, el);
    const maxScroll = el.scrollWidth - el.clientWidth;
    el.scrollLeft = Math.max(0, Math.min(maxScroll, el.scrollLeft + deltaPx));

    event.preventDefault();
    event.stopPropagation();
  };

  const opts: AddEventListenerOptions = { passive: false, capture: true };
  el.addEventListener('wheel', handler, opts);
  return () => el.removeEventListener('wheel', handler, { capture: true });
}

function normalizeWheelToPixels(raw: number, event: WheelEvent, el: HTMLElement): number {
  switch (event.deltaMode) {
    case WheelEvent.DOM_DELTA_LINE:
      return raw * 48;
    case WheelEvent.DOM_DELTA_PAGE:
      return raw * Math.max(1, el.clientWidth * 0.9);
    case WheelEvent.DOM_DELTA_PIXEL:
    default:
      return raw;
  }
}
