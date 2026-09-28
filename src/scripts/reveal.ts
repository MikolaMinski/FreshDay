/**
 * Анимации при прокрутке:
 *  - [data-reveal]  — плавное появление элемента;
 *  - [data-count]   — счётчик чисел от 0 до значения;
 *  - [data-stagger] — дочерним [data-reveal] выставляется задержка по очереди.
 */
const reduced = window.matchMedia('(prefers-reduced-motion: reduce)').matches;

document.querySelectorAll<HTMLElement>('[data-stagger]').forEach((group) => {
  group.querySelectorAll<HTMLElement>('[data-reveal]').forEach((el, i) => {
    el.style.setProperty('--i', String(i));
  });
});

function animateCount(el: HTMLElement) {
  const target = Number(el.dataset.count);
  if (reduced || !Number.isFinite(target)) {
    el.textContent = target.toLocaleString('ru-RU');
    return;
  }
  const duration = 1600;
  const start = performance.now();
  const tick = (now: number) => {
    const p = Math.min((now - start) / duration, 1);
    const eased = 1 - Math.pow(1 - p, 4);
    el.textContent = Math.round(target * eased).toLocaleString('ru-RU');
    if (p < 1) requestAnimationFrame(tick);
  };
  requestAnimationFrame(tick);
}

const io = new IntersectionObserver(
  (entries) => {
    for (const entry of entries) {
      if (!entry.isIntersecting) continue;
      const el = entry.target as HTMLElement;
      el.classList.add('is-visible');
      if (el.dataset.count !== undefined) animateCount(el);
      io.unobserve(el);
    }
  },
  { rootMargin: '0px 0px -8% 0px', threshold: 0.12 },
);

document.querySelectorAll('[data-reveal], [data-count]').forEach((el) => io.observe(el));
