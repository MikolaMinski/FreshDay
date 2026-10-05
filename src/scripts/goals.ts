/**
 * Цели аналитики для кликов по контактам: звонок, Telegram, Viber, email, Instagram.
 * Работает на всех страницах автоматически — тип цели определяется по ссылке,
 * либо задаётся вручную атрибутом data-goal="имя_цели".
 *
 * Яндекс Метрика: создайте JavaScript-цели call, telegram, viber, email, instagram.
 * Google Analytics 4: событие contact_click с параметром method; отметьте его
 * как ключевое событие (Admin → Events → Mark as key event).
 */
function goalFor(link: HTMLAnchorElement): string | null {
  if (link.dataset.goal) return link.dataset.goal;
  const href = link.getAttribute('href') ?? '';
  if (href.startsWith('tel:')) return 'call';
  if (href.startsWith('viber:')) return 'viber';
  if (href.startsWith('mailto:')) return 'email';
  if (/^https?:\/\/(t\.me|telegram\.me)\//.test(href)) return 'telegram';
  if (/instagram\.com\//.test(href)) return 'instagram';
  return null;
}

document.addEventListener(
  'click',
  (event) => {
    const link = (event.target as Element | null)?.closest?.('a');
    if (!(link instanceof HTMLAnchorElement)) return;
    const goal = goalFor(link);
    if (!goal) return;

    const metrikaId = import.meta.env.PUBLIC_YANDEX_METRIKA_ID;
    if (metrikaId && window.ym) window.ym(metrikaId, 'reachGoal', goal);
    window.gtag?.('event', 'contact_click', { method: goal, link_url: link.href });
  },
  { capture: true },
);
