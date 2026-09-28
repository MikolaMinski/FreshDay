/**
 * ГЛАВНЫЙ ФАЙЛ НАСТРОЕК САЙТА.
 * Название, контакты, адрес, соцсети — всё меняется здесь и автоматически
 * подставляется в шапку, подвал, SEO-теги, микроразметку Schema.org и форму заявки.
 *
 * ⚠️ Значения с пометкой TODO — заглушки. Замените их на реальные перед запуском.
 */
export const SITE = {
  /** Домен сайта без слэша в конце. Используется для canonical, sitemap и Open Graph. */
  url: 'https://samobranka.by', // TODO: ваш домен
  /** Название бренда */
  name: 'Самобранка', // TODO: ваше название
  /** Юридическое название — выводится в подвале (требование законодательства РБ) */
  legalName: 'ООО «Самобранка», УНП 000000000', // TODO
  tagline: 'Кейтеринг в Минске',
  city: 'Минск',
  locale: 'ru_BY',
  lang: 'ru-BY',

  /** Базовые SEO-значения по умолчанию */
  defaultTitle: 'Кейтеринг в Минске — фуршеты, банкеты, кофе-брейки',
  defaultDescription:
    'Выездной кейтеринг в Минске и Минском районе: фуршеты, банкеты, свадьбы, корпоративы и кофе-брейки под ключ. Своя кухня, посуда, официанты. Смета за 15 минут.',

  contacts: {
    phone: '+375 29 000-00-00', // TODO
    phoneHref: '+375290000000', // TODO: тот же номер без пробелов
    email: 'hello@samobranka.by', // TODO
    telegram: 'https://t.me/samobranka_by', // TODO
    viber: 'viber://chat?number=%2B375290000000', // TODO
    instagram: 'https://instagram.com/samobranka.by', // TODO
    address: {
      street: 'ул. Примерная, 1', // TODO: адрес кухни/офиса
      locality: 'Минск',
      region: 'Минская область',
      postalCode: '220000', // TODO
      country: 'BY',
    },
    /** Координаты для карты и Schema.org */
    geo: { lat: 53.9023, lng: 27.5619 }, // TODO
    hours: 'Ежедневно 9:00–21:00',
    /** Формат Schema.org: https://schema.org/openingHours */
    hoursSchema: 'Mo-Su 09:00-21:00',
  },

  /** География обслуживания — важна для локального SEO */
  areaServed: [
    'Минск',
    'Минский район',
    'Боровляны',
    'Ждановичи',
    'Колодищи',
    'Заславль',
    'Фаниполь',
    'Дзержинск',
    'Логойск',
    'Смолевичи',
  ],

  /** Ключевые цифры для блока доверия */
  stats: [
    { value: 10, suffix: '+', label: 'лет на рынке' }, // TODO
    { value: 1500, suffix: '+', label: 'мероприятий' }, // TODO
    { value: 800, suffix: '', label: 'гостей — наш максимум за раз' }, // TODO
    { value: 24, suffix: ' ч', label: 'минимальный срок заказа' }, // TODO
  ],

  priceRange: '15–120 BYN за гостя',
  currency: 'BYN',

  /** Адрес обработчика формы заявки (см. README → «Форма заявки») */
  formEndpoint: '/api/lead.php',

  /** Навигация */
  nav: [
    { href: '/uslugi/', label: 'Услуги' },
    { href: '/galereya/', label: 'Галерея' },
    { href: '/#calculator', label: 'Расчёт' },
    { href: '/blog/', label: 'Статьи' },
    { href: '/kontakty/', label: 'Контакты' },
  ],
} as const;

export type Site = typeof SITE;
