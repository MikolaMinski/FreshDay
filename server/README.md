# Сервис заявок Fresh Day (.NET 10)

Принимает заявки с формы сайта и присылает их ботом в группу Telegram:

```
🍽 Новая заявка с сайта #заявка

👤 Имя: Ольга
📞 Телефон: +375 (29) 686-66-92
🎉 Формат: Фуршет
👥 Гостей: 40
📅 Дата: 12.12.2026, сб (через 43 дн.)

💬 Комментарий:
┃ Офис на Немиге, без орехов

🔗 Со страницы: /FreshDay/uslugi/furshet/
```

Что умеет:

- принимает форму как есть (`multipart/form-data`, обычная отправка без JavaScript, JSON);
- проверяет имя, телефон и согласие на обработку данных, отсекает ботов (скрытое поле-ловушка)
  и частые отправки с одного IP;
- повторяет отправку, если Telegram временно недоступен, и сам переходит на новый ID, если
  группа стала супергруппой;
- сохраняет каждую заявку в `data/leads.jsonl` — на случай, если Telegram был недоступен;
- **сам подсказывает ID группы**: добавьте бота в группу — он напишет её ID. Или отправьте в
  группе `/chatid@имя_бота`.

Сторонних библиотек нет — только ASP.NET Core и прямые запросы к Telegram Bot API.

## Структура

```
server/
├─ FreshDay.Server.slnx              решение (открывается в VS Code с C# Dev Kit)
├─ src/FreshDay.LeadBot/
│  ├─ Program.cs                     настройка: CORS, лимит запросов, эндпоинты
│  ├─ Options.cs                     все настройки с пояснениями
│  ├─ Leads/                         разбор формы, текст сообщения, архив, POST /api/lead
│  └─ Telegram/                      клиент Bot API, отправка с повторами, помощник /chatid
├─ tests/FreshDay.LeadBot.Tests/     тесты (xUnit)
├─ Dockerfile, docker-compose.yml, Caddyfile   запуск на сервере с HTTPS
└─ .env.example                      пример настроек для сервера
```

## 1. Бот и группа

1. В Telegram откройте [@BotFather](https://t.me/BotFather) → `/newbot`, задайте имя (например,
   «Fresh Day заявки») и логин (например, `freshday_leads_bot`). Сохраните токен вида
   `123456789:AAH…`.
2. Создайте группу для заявок (или возьмите существующую) и добавьте в неё бота.
3. Запустите сервис (шаг 2 или 3) — бот сам напишет в группу её ID, например `-1001234567890`.
   Если сервис был запущен раньше, чем бота добавили, отправьте в группе `/chatid@freshday_leads_bot`.
4. Впишите ID в настройки (`Telegram:Chats:0:Id`) и перезапустите сервис.

Если в группе включены **темы**, отправьте `/chatid@…` внутри нужной темы — бот пришлёт и ID
темы (`Telegram:Chats:0:ThreadId`). Заявки можно слать сразу в несколько чатов: `Chats:1:Id` и т. д.

## 2. Запуск на своём компьютере (VS Code)

Нужен [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) и расширение
**C# Dev Kit** (VS Code предложит его сам).

Токен храните в user-secrets — так он не попадёт в git:

```bash
cd server/src/FreshDay.LeadBot
dotnet user-secrets set "Telegram:BotToken" "123456789:AAH..."
dotnet user-secrets set "Telegram:Chats:0:Id" "-1001234567890"
dotnet run
```

Сервис поднимется на `http://localhost:5080`. В VS Code то же самое: **Run and Debug →
«LeadBot: сервис заявок (.NET)»**, а **«Сайт + сервис заявок»** запускает сразу и сайт, и сервис.

Чтобы форма локального сайта отправляла в сервис, создайте в корне проекта файл `.env`:

```
PUBLIC_LEAD_API_URL=http://localhost:5080/api/lead
```

Проверка без сайта:

```bash
curl -H "Accept: application/json" -F name=Тест -F phone=+375296866692 -F consent=yes \
     http://localhost:5080/api/lead
```

Тесты: `dotnet test server/FreshDay.Server.slnx` (или задача **leadbot: test** в VS Code).

## 3. Запуск на сервере

Сайт на GitHub Pages работает по HTTPS, поэтому и сервису нужен HTTPS-адрес, например
`https://api.freshday.by`. Подойдёт любой VPS с Docker (от ~5 $ в месяц) и поддомен, A-запись
которого указывает на IP сервера.

```bash
git clone https://github.com/MikolaMinski/FreshDay.git && cd FreshDay/server
cp .env.example .env && nano .env      # токен, ID группы, LEADBOT_DOMAIN
docker compose up -d --build
docker compose logs -f leadbot         # «Telegram-бот @… подключён»
```

Caddy сам получит и будет продлевать сертификат Let's Encrypt. Обновление после изменений в
репозитории: `git pull && docker compose up -d --build`.

Без Docker: `dotnet publish -c Release`, запуск как службы systemd и любой обратный прокси
(nginx, Caddy) с HTTPS; в этом случае задайте `LeadBot__BehindProxy=true`.

## 4. Подключить сайт

На GitHub: **Settings → Secrets and variables → Actions → Variables → New repository variable**:

| Имя                   | Значение                             |
| --------------------- | ------------------------------------ |
| `PUBLIC_LEAD_API_URL` | `https://api.freshday.by/api/lead`   |

Затем **Actions → Deploy to GitHub Pages → Run workflow** (или любой push в `main`). Форма начнёт
отправлять заявки в сервис; Web3Forms и `lead.php` при этом не используются.

Когда у сайта появится свой домен, добавьте его в `LeadBot__AllowedOrigins__…` и поменяйте
`LeadBot__SiteUrl` (см. `.env.example`), иначе браузер не даст форме получить ответ.

## Настройки

| Ключ (в `.env` — через `__`)    | По умолчанию                              | Зачем                                    |
| ------------------------------- | ----------------------------------------- | ---------------------------------------- |
| `Telegram:BotToken`             | —                                         | токен бота, обязателен                   |
| `Telegram:Chats:N:Id`           | —                                         | куда слать заявки                        |
| `Telegram:Chats:N:ThreadId`     | —                                         | тема в группе с темами                   |
| `Telegram:ChatIdHelper`         | `true`                                    | бот отвечает на `/chatid`                |
| `LeadBot:SiteUrl`               | `https://mikolaminski.github.io/FreshDay` | «Спасибо» без JS и ссылка на страницу    |
| `LeadBot:AllowedOrigins:N`      | `https://mikolaminski.github.io`          | с каких сайтов принимать форму (CORS)    |
| `LeadBot:ArchivePath`           | `data/leads.jsonl`                        | архив заявок; пусто — не хранить         |
| `LeadBot:RequestsPerMinutePerIp`| `3`                                       | защита от флуда                          |
| `LeadBot:BehindProxy`           | `false`                                   | `true` за nginx / Caddy                  |

## Ответы сервиса

`POST /api/lead` с `Accept: application/json`:

| Код | Ответ                                                      | Когда                                   |
| --- | ---------------------------------------------------------- | --------------------------------------- |
| 200 | `{"ok":true}`                                              | заявка доставлена                       |
| 400 | `{"ok":false,"error":"invalid_name,invalid_phone,…"}`      | не заполнены обязательные поля          |
| 429 | `{"ok":false,"error":"too_many_requests"}`                 | слишком часто с одного IP               |
| 502 | `{"ok":false,"error":"delivery_failed"}`                   | Telegram не принял (заявка в архиве)    |
| 503 | `{"ok":false,"error":"not_configured"}`                    | не указан чат (заявка в архиве)         |

Без `Accept: application/json` (форма без JavaScript) — переход 303 на `/spasibo/` или на
`/kontakty/?error=1`. `GET /health` — проверка, что сервис жив.

Имя и телефон в журнал не пишутся — только формат и число гостей. Персональные данные остаются
лишь в сообщении Telegram и в архиве `data/leads.jsonl` на вашем сервере.
