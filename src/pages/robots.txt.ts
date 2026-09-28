import type { APIRoute } from 'astro';
import { absUrl, url } from '@/lib/url';

export const GET: APIRoute = () => {
  const lines = [
    'User-agent: *',
    'Allow: /',
    `Disallow: ${url('/api/')}`,
    `Disallow: ${url('/spasibo/')}`,
    '',
    `Sitemap: ${absUrl('/sitemap-index.xml')}`,
    '',
  ];
  return new Response(lines.join('\n'), {
    headers: { 'Content-Type': 'text/plain; charset=utf-8' },
  });
};
