<?php
// Скопируйте этот файл и заполните. В git настройки не попадают.
// Plesk (hoster.by): положите его на уровень выше папки httpdocs под именем freshday-config.php —
//   /var/www/vhosts/<домен>/freshday-config.php (в Диспетчере файлов это корень домена).
//   Там его не тронет деплой из Git и не откроет браузер.
// Другой хостинг: можно рядом, под именем api/config.php.
return [
    // Telegram: создайте бота через @BotFather, получите токен.
    // chat_id: напишите боту, затем откройте https://api.telegram.org/bot<TOKEN>/getUpdates
    'telegram_token'    => '',
    'telegram_chat_ids' => [/* 123456789 */],

    // Email (необязательно)
    'email_to'   => '',
    'email_from' => '',
];
