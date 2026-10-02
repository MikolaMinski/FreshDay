/**
 * ГЛАВНЫЙ ФАЙЛ НАСТРОЕК САЙТА.
 * Название, контакты, адрес, соцсети — всё меняется здесь и автоматически
 * подставляется в шапку, подвал, SEO-теги, микроразметку Schema.org и форму заявки.
 *
 * ⚠️ Значения с пометкой TODO — заглушки. Замените их на реальные перед запуском.
 */
export const SITE = {
  /** Домен сайта без слэша в конце. Используется для canonical, sitemap и Open Graph. */
  url: 'https://freshday.by', // TODO: проверьте домен
  /** Название бренда */
  name: 'Fresh Day',
  /** Написание кириллицей — люди ищут и так, и так */
  alternateName: 'Фреш Дэй',
  /** Юридическое название — выводится в подвале (требование законодательства РБ) */
  legalName: 'ООО «…», УНП 000000000', // TODO: юрлицо и УНП
  tagline: 'Кейтеринг в Минске',
  city: 'Минск',
  locale: 'ru_BY',
  lang: 'ru-BY',

  /** Базовые SEO-значения по умолчанию */
  defaultTitle: 'Кейтеринг в Минске — фуршеты, банкеты, кофе-брейки',
  defaultDescription:
    'Выездной кейтеринг в Минске и по всей Беларуси: фуршеты, банкеты, свадьбы, корпоративы и кофе-брейки под ключ. Своя кухня, посуда, официанты.',

  contacts: {
    phone: '+375 29 000-00-00', // TODO
    phoneHref: '+375290000000', // TODO: тот же номер без пробелов
    /** Дополнительный номер. Оставьте phone2: '' чтобы скрыть */
    phone2: '+375 29 111-11-11', // TODO: дополнительный номер
    phone2Href: '+375291111111', // TODO: тот же номер без пробелов
    email: 'hello@freshday.by', // TODO
    telegram: 'https://t.me/freshday_by', // TODO
    viber: 'viber://chat?number=%2B375290000000', // TODO
    instagram: 'https://instagram.com/freshday.by', // TODO
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
    'Беларусь',
    'Минск',
    'Минская область',
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

  priceRange: 'от 35 BYN за гостя',
  currency: 'BYN',

  /**
   * Обработчик формы заявки (см. README → «Форма заявки»):
   *  - обычный хостинг с PHP → '/api/lead.php' (заявки в Telegram);
   *  - GitHub Pages и другой хостинг без PHP → задайте ключ PUBLIC_WEB3FORMS_KEY,
   *    тогда заявки пойдут через web3forms.com на ваш email, это поле игнорируется.
   */
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
