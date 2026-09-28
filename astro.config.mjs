// @ts-check
import { defineConfig } from 'astro/config';
import sitemap from '@astrojs/sitemap';
import { SITE } from './src/config/site.ts';

// Адрес и base-путь можно переопределить переменными окружения (так делает деплой на GitHub Pages):
//   SITE_URL=https://user.github.io  BASE_PATH=/catering-minsk
const site = process.env.SITE_URL || SITE.url;
const base = process.env.BASE_PATH || '/';

// https://docs.astro.build/en/reference/configuration-reference/
export default defineConfig({
  site,
  base,
  trailingSlash: 'always',
  build: {
    format: 'directory',
    inlineStylesheets: 'auto',
  },
  prefetch: {
    prefetchAll: true,
    defaultStrategy: 'hover',
  },
  image: {
    responsiveStyles: true,
    layout: 'constrained',
  },
  integrations: [
    sitemap({
      filter: (page) => !page.includes('/spasibo/') && !page.includes('/404'),
      changefreq: 'weekly',
      priority: 0.7,
      serialize(item) {
        const home = new URL(base.endsWith('/') ? base : `${base}/`, site).href;
        if (item.url === home) item.priority = 1.0;
        else if (item.url.includes('/uslugi/')) item.priority = 0.9;
        return item;
      },
    }),
  ],
});
