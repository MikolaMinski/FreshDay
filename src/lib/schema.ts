/**
 * Генераторы микроразметки Schema.org (JSON-LD).
 * Помогают Яндексу и Google показывать расширенные сниппеты:
 * адрес, телефон, часы работы, цены, вопросы-ответы, хлебные крошки.
 */
import { SITE } from '@/config/site';

const abs = (path: string) => new URL(path, SITE.url).href;
const orgId = `${SITE.url}/#organization`;

export function organizationSchema(logo?: string) {
  const { contacts } = SITE;
  return {
    '@context': 'https://schema.org',
    '@type': ['LocalBusiness', 'FoodEstablishment'],
    '@id': orgId,
    name: SITE.name,
    alternateName: SITE.alternateName,
    legalName: SITE.legalName,
    description: SITE.defaultDescription,
    url: SITE.url + '/',
    logo: logo ? abs(logo) : undefined,
    image: abs('/og-image.jpg'),
    telephone: contacts.phoneHref,
    email: contacts.email,
    priceRange: SITE.priceRange,
    currenciesAccepted: SITE.currency,
    paymentAccepted: 'Наличные, банковская карта, безналичный расчёт',
    servesCuisine: ['Европейская', 'Белорусская'],
    openingHours: contacts.hoursSchema,
    address: {
      '@type': 'PostalAddress',
      streetAddress: contacts.address.street,
      addressLocality: contacts.address.locality,
      addressRegion: contacts.address.region,
      postalCode: contacts.address.postalCode,
      addressCountry: contacts.address.country,
    },
    geo: { '@type': 'GeoCoordinates', latitude: contacts.geo.lat, longitude: contacts.geo.lng },
    areaServed: SITE.areaServed.map((name) => ({ '@type': 'City', name })),
    sameAs: [contacts.instagram, contacts.telegram].filter(Boolean),
  };
}

export function websiteSchema() {
  return {
    '@context': 'https://schema.org',
    '@type': 'WebSite',
    '@id': `${SITE.url}/#website`,
    url: SITE.url + '/',
    name: SITE.name,
    alternateName: SITE.alternateName,
    inLanguage: SITE.lang,
    publisher: { '@id': orgId },
  };
}

export function serviceSchema(s: {
  name: string;
  description: string;
  url: string;
  priceFrom: number;
  priceUnit: string;
  image: string;
}) {
  return {
    '@context': 'https://schema.org',
    '@type': 'Service',
    serviceType: s.name,
    name: `${s.name} в Минске`,
    description: s.description,
    url: abs(s.url),
    image: abs(s.image),
    provider: { '@id': orgId },
    areaServed: SITE.areaServed.map((name) => ({ '@type': 'City', name })),
    offers: {
      '@type': 'Offer',
      priceCurrency: SITE.currency,
      price: s.priceFrom,
      priceSpecification: {
        '@type': 'UnitPriceSpecification',
        price: s.priceFrom,
        priceCurrency: SITE.currency,
        unitText: s.priceUnit,
        minPrice: s.priceFrom,
      },
      availability: 'https://schema.org/InStock',
    },
  };
}

export function faqSchema(items: ReadonlyArray<{ q: string; a: string }>) {
  return {
    '@context': 'https://schema.org',
    '@type': 'FAQPage',
    mainEntity: items.map((i) => ({
      '@type': 'Question',
      name: i.q,
      acceptedAnswer: { '@type': 'Answer', text: i.a },
    })),
  };
}

export function breadcrumbSchema(items: ReadonlyArray<{ name: string; href: string }>) {
  return {
    '@context': 'https://schema.org',
    '@type': 'BreadcrumbList',
    itemListElement: items.map((i, idx) => ({
      '@type': 'ListItem',
      position: idx + 1,
      name: i.name,
      item: abs(i.href),
    })),
  };
}

export function articleSchema(a: {
  title: string;
  description: string;
  url: string;
  image: string;
  pubDate: Date;
  updatedDate?: Date;
}) {
  return {
    '@context': 'https://schema.org',
    '@type': 'BlogPosting',
    headline: a.title,
    description: a.description,
    image: abs(a.image),
    url: abs(a.url),
    datePublished: a.pubDate.toISOString(),
    dateModified: (a.updatedDate ?? a.pubDate).toISOString(),
    inLanguage: SITE.lang,
    author: { '@id': orgId },
    publisher: { '@id': orgId },
    mainEntityOfPage: abs(a.url),
  };
}
