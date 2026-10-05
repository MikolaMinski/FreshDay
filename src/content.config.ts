/**
 * Схемы контента. Каждая услуга и статья — отдельный Markdown-файл в src/content/.
 * Схема проверяет поля при сборке: опечатка в поле или забытая картинка = понятная ошибка,
 * а не сломанная страница на проде.
 */
import { defineCollection } from 'astro:content';
import { glob } from 'astro/loaders';
import { z } from 'astro/zod';

const faqItem = z.object({ q: z.string(), a: z.string() });

const services = defineCollection({
  loader: glob({ pattern: '**/*.md', base: './src/content/services' }),
  schema: ({ image }) =>
    z.object({
      /** Короткое название для карточек и меню */
      name: z.string(),
      /** H1 страницы — с главным ключом, например «Фуршет в Минске» */
      h1: z.string(),
      /** <title> без названия бренда (добавится само) — до ~55 символов */
      seoTitle: z.string().max(60),
      /** meta description — 120–160 символов */
      description: z.string().min(80).max(180),
      /** Короткий анонс для карточки на главной */
      excerpt: z.string(),
      order: z.number().default(100),
      icon: z.enum(['glass', 'table', 'cup', 'rings', 'briefcase', 'tree', 'cake']),
      priceFrom: z.number().positive(),
      priceUnit: z.string().default('за гостя'),
      cover: image(),
      coverAlt: z.string(),
      /** Категория галереи, фото из которой покажутся на странице услуги */
      galleryCategory: z.string(),
      faq: z.array(faqItem).default([]),
      /** Не показывать страницу, пока услуга в работе */
      draft: z.boolean().default(false),
    }),
});

const blog = defineCollection({
  loader: glob({ pattern: '**/*.md', base: './src/content/blog' }),
  schema: ({ image }) =>
    z.object({
      title: z.string(),
      seoTitle: z.string().max(60).optional(),
      description: z.string().min(80).max(180),
      pubDate: z.coerce.date(),
      updatedDate: z.coerce.date().optional(),
      cover: image(),
      coverAlt: z.string(),
      tags: z.array(z.string()).default([]),
      draft: z.boolean().default(false),
    }),
});

export const collections = { services, blog };
