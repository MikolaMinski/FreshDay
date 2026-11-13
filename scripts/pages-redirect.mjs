// Превращает сборку сайта в набор страниц-переадресаций на основной домен.
// Нужен для копии на GitHub Pages: чтобы Google не считал её дублем и
// индексировал только основной сайт (https://freshday.by).
//
// Использование: node scripts/pages-redirect.mjs <папка сборки> <папка результата> <https://домен>
import { readdir, mkdir, writeFile } from 'node:fs/promises';
import { join, relative, dirname } from 'node:path';

const [src, out, target] = process.argv.slice(2);
if (!src || !out || !target) {
  console.error('Usage: pages-redirect.mjs <dist> <out> <https://domain>');
  process.exit(1);
}
const origin = target.replace(/\/$/, '');

const page = (url) => `<!doctype html>
<html lang="ru">
<head>
<meta charset="utf-8">
<title>Fresh Day переехал на ${origin.replace('https://', '')}</title>
<link rel="canonical" href="${url}">
<meta http-equiv="refresh" content="0; url=${url}">
<script>location.replace(${JSON.stringify(url)} + location.search + location.hash)</script>
</head>
<body>
<p>Сайт переехал: <a href="${url}">${url}</a></p>
</body>
</html>
`;

async function* htmlFiles(dir) {
  for (const entry of await readdir(dir, { withFileTypes: true })) {
    const p = join(dir, entry.name);
    if (entry.isDirectory()) yield* htmlFiles(p);
    else if (entry.name.endsWith('.html')) yield p;
  }
}

let count = 0;
for await (const file of htmlFiles(src)) {
  const rel = relative(src, file).split('\\').join('/');
  let path;
  if (rel === '404.html') path = '/';
  else if (rel === 'index.html') path = '/';
  else if (rel.endsWith('/index.html')) path = '/' + rel.slice(0, -'index.html'.length);
  else path = '/' + rel;
  const dest = join(out, rel);
  await mkdir(dirname(dest), { recursive: true });
  await writeFile(dest, page(origin + path));
  count++;
}
await writeFile(
  join(out, 'robots.txt'),
  `User-agent: *\nAllow: /\n\nSitemap: ${origin}/sitemap-index.xml\n`,
);
console.log(`${count} redirect pages → ${origin}`);
