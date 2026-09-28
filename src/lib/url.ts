/**
 * Адреса с учётом base-пути.
 * На своём домене base = '/', на GitHub Pages без домена — '/имя-репозитория/'.
 * Всегда используйте url('/путь/') для внутренних ссылок — тогда сайт работает в обоих вариантах.
 */
import { SITE } from '@/config/site';

const BASE = import.meta.env.BASE_URL.replace(/\/$/, ''); // '' или '/repo'

/** Адрес сайта без слэша на конце (из настроек сборки или из site.ts) */
export const siteUrl = (import.meta.env.SITE ?? SITE.url).replace(/\/$/, '');

/** Внутренняя ссылка с префиксом base: url('/uslugi/') → '/repo/uslugi/' */
export function url(path = '/'): string {
  if (/^([a-z]+:|#|\/\/)/i.test(path)) return path; // внешние, tel:, mailto:, якоря
  const p = path.startsWith('/') ? path : `/${path}`;
  if (BASE && (p === BASE || p.startsWith(`${BASE}/`))) return p; // уже с префиксом
  return BASE + p;
}

/** Полный адрес: absUrl('/og-image.jpg') → 'https://домен/og-image.jpg' */
export function absUrl(path = '/'): string {
  return new URL(url(path), `${siteUrl}/`).href;
}
