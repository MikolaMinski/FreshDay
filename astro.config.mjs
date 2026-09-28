// @ts-check
import { defineConfig } from 'astro/config';
import sitemap from '@astrojs/sitemap';
import { SITE } from './src/config/site.ts';

// https://docs.astro.build/en/reference/configuration-reference/
export default defineConfig({
  site: SITE.url,
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
        if (item.url === SITE.url + '/') item.priority = 1.0;
        else if (item.url.includes('/uslugi/')) item.priority = 0.9;
        return item;
      },
    }),
  ],
});
