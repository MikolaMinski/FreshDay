/// <reference types="astro/client" />

interface ImportMetaEnv {
  readonly PUBLIC_YANDEX_METRIKA_ID?: string;
  readonly PUBLIC_GA_ID?: string;
  readonly PUBLIC_YANDEX_VERIFICATION?: string;
  readonly PUBLIC_GOOGLE_VERIFICATION?: string;
}

interface ImportMeta {
  readonly env: ImportMetaEnv;
}

interface Window {
  ym?: (id: string | number, action: string, goal?: string) => void;
  gtag?: (...args: unknown[]) => void;
}
