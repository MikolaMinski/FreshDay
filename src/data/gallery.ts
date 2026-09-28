/**
 * Галерея. Чтобы добавить фото:
 * 1) положите .jpg в src/assets/photos/ (латиницей, через дефис: banket-v-usadbe.jpg);
 * 2) добавьте строку в массив ниже — alt (описание для поиска и незрячих) и категорию.
 * Оптимизация (AVIF/WebP, размеры под экраны) делается автоматически при сборке.
 */
import type { ImageMetadata } from 'astro';

export const CATEGORIES = {
  furshet: 'Фуршеты',
  banket: 'Банкеты',
  svadba: 'Свадьбы',
  korporativ: 'Корпоративы',
  vyezdnoj: 'За городом',
  'kofe-breik': 'Кофе-брейки',
} as const;

export type Category = keyof typeof CATEGORIES;

export interface Photo {
  file: string;
  alt: string;
  category: Category;
}

const photos: Photo[] = [
  {
    file: 'servirovka-svechi-dub',
    alt: 'Свадебная сервировка стола из дуба с чёрными свечами',
    category: 'svadba',
  },
  {
    file: 'kanape-i-tartaletki',
    alt: 'Канапе и тарталетки на фуршетных досках',
    category: 'furshet',
  },
  {
    file: 'svadba-zal-zelenye-skaterti',
    alt: 'Свадебный зал с зелёными бархатными скатертями и цветочной аркой',
    category: 'svadba',
  },
  {
    file: 'vyezdnoj-banket-v-lesu',
    alt: 'Банкет в деревянной беседке в сосновом лесу',
    category: 'vyezdnoj',
  },
  {
    file: 'banket-limony-oliva',
    alt: 'Банкетный стол с закусками, лимонами и оливковыми ветвями',
    category: 'banket',
  },
  { file: 'furshet-ananasy-ozero', alt: 'Фуршет на веранде с видом на озеро', category: 'furshet' },
  {
    file: 'kofe-breik-seminar',
    alt: 'Кофе-брейк на семинаре: термопот, чашки и выпечка',
    category: 'kofe-breik',
  },
  {
    file: 'korporativ-loft',
    alt: 'Корпоративный банкет в лофте с панорамными окнами',
    category: 'korporativ',
  },
  {
    file: 'svadba-zelenyj-barhat',
    alt: 'Круглый свадебный стол с зелёной бархатной скатертью',
    category: 'svadba',
  },
  {
    file: 'vyezdnoj-banket-v-besedke',
    alt: 'Летний банкет в белой беседке за городом',
    category: 'vyezdnoj',
  },
  {
    file: 'banket-belyj-zal',
    alt: 'Банкет в светлом зале: белые стулья и сиреневые салфетки',
    category: 'banket',
  },
  {
    file: 'furshet-v-shourume',
    alt: 'Фуршет на открытии шоурума с игристым и канапе',
    category: 'furshet',
  },
  {
    file: 'servirovka-derevyannyj-stol',
    alt: 'Вечерняя сервировка деревянного стола на террасе',
    category: 'svadba',
  },
  {
    file: 'korporativ-v-ofise',
    alt: 'Фуршет в офисе на день рождения сотрудника',
    category: 'korporativ',
  },
  {
    file: 'banket-v-restorane',
    alt: 'Банкет в ресторане с бирюзовыми диванами',
    category: 'banket',
  },
  {
    file: 'vyezdnoj-furshet-kanape',
    alt: 'Фуршетный стол на улице с тарталетками с икрой',
    category: 'vyezdnoj',
  },
  {
    file: 'servirovka-loft-okna',
    alt: 'Сервировка длинного стола в лофте с видом на сад',
    category: 'svadba',
  },
  { file: 'furshet-otkrytie-ofisa', alt: 'Фуршет на открытии офиса банка', category: 'kofe-breik' },
  {
    file: 'banket-steklyannyj-stol',
    alt: 'Круглый стеклянный стол с закусками и золотыми приборами',
    category: 'banket',
  },
  {
    file: 'svadba-cvety-kirpich',
    alt: 'Праздничный стол с цветочной композицией в зале с кирпичной стеной',
    category: 'svadba',
  },
  {
    file: 'banket-na-verande',
    alt: 'Банкет на застеклённой веранде за городом',
    category: 'banket',
  },
  {
    file: 'korporativ-hellouin',
    alt: 'Тематический фуршет на Хэллоуин в офисе',
    category: 'korporativ',
  },
  {
    file: 'vyezdnoj-stol-u-bassejna',
    alt: 'Стол под навесом у бассейна в загородном доме',
    category: 'vyezdnoj',
  },
  {
    file: 'banket-den-rozhdeniya',
    alt: 'Банкет на день рождения: салаты и мясные нарезки',
    category: 'banket',
  },
  {
    file: 'furshet-veranda-u-ozera',
    alt: 'Фуршетный стол с ананасами на веранде у озера',
    category: 'furshet',
  },
  {
    file: 'servirovka-limony-zelen',
    alt: 'Сервировка с лимонами и зеленью на деревянном столе',
    category: 'svadba',
  },
  {
    file: 'banket-v-zagorodnom-dome',
    alt: 'Банкет в загородном доме с видом на посёлок',
    category: 'banket',
  },
  { file: 'furshet-v-ofise-soki', alt: 'Фуршет в офисе: закуски и соки', category: 'korporativ' },
  {
    file: 'blyudo-steik-gribnoj-sous',
    alt: 'Стейк из говядины с грибным соусом и драником',
    category: 'banket',
  },
];

const files = import.meta.glob<{ default: ImageMetadata }>('../assets/photos/*.jpg', {
  eager: true,
});

function load(name: string): ImageMetadata {
  const mod = files[`../assets/photos/${name}.jpg`];
  if (!mod) throw new Error(`Фото не найдено: src/assets/photos/${name}.jpg`);
  return mod.default;
}

export const gallery = photos.map((p) => ({ ...p, src: load(p.file) }));
export type GalleryItem = (typeof gallery)[number];

export const byCategory = (cat: string) => gallery.filter((p) => p.category === cat);
export const photo = (name: string) => load(name);
