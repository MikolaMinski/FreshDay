<?php
/**
 * Приём заявок с сайта → Telegram и/или email.
 * Настройки: скопируйте config.example.php в config.php и заполните.
 * Требования: PHP 8.0+, расширение curl (есть на любом хостинге).
 */
declare(strict_types=1);

$config = file_exists(__DIR__ . '/config.php') ? require __DIR__ . '/config.php' : [];
$wantsJson = str_contains($_SERVER['HTTP_ACCEPT'] ?? '', 'application/json');

function respond(bool $ok, string $error = ''): never {
    global $wantsJson;
    if ($wantsJson) {
        header('Content-Type: application/json; charset=utf-8');
        http_response_code($ok ? 200 : 400);
        echo json_encode(['ok' => $ok, 'error' => $error], JSON_UNESCAPED_UNICODE);
    } else {
        header('Location: ' . ($ok ? '/spasibo/' : '/kontakty/?error=1'), true, 303);
    }
    exit;
}

if ($_SERVER['REQUEST_METHOD'] !== 'POST') respond(false, 'Method not allowed');

// Ловушка для ботов
if (!empty($_POST['website'])) respond(true);

// Простая защита от частых отправок с одного IP (1 заявка / 30 сек)
$ip = $_SERVER['REMOTE_ADDR'] ?? 'unknown';
$lock = sys_get_temp_dir() . '/lead_' . md5($ip);
if (file_exists($lock) && time() - filemtime($lock) < 30) respond(false, 'Too many requests');
@touch($lock);

$clean = fn(string $key, int $max = 200): string =>
    mb_substr(trim(strip_tags((string)($_POST[$key] ?? ''))), 0, $max);

$lead = [
    'Имя'         => $clean('name', 60),
    'Телефон'     => $clean('phone', 30),
    'Формат'      => $clean('format', 60),
    'Гостей'      => $clean('guests', 10),
    'Дата'        => $clean('date', 20),
    'Комментарий' => $clean('message', 2000),
    'Страница'    => $clean('page', 200),
];

if (mb_strlen($lead['Имя']) < 2 || !preg_match('/^[+\d\s()\-]{9,20}$/', $lead['Телефон'])) {
    respond(false, 'Invalid data');
}
if (($_POST['consent'] ?? '') !== 'yes') respond(false, 'Consent required');

$lines = ["🍽 Новая заявка с сайта"];
foreach ($lead as $k => $v) if ($v !== '') $lines[] = "<b>{$k}:</b> " . htmlspecialchars($v);
$text = implode("\n", $lines);
$sent = false;

// Резервная копия каждой заявки в файл (доступ извне закрыт в .htaccess)
@file_put_contents(
    __DIR__ . '/leads.log',
    date('c') . ' ' . json_encode($lead, JSON_UNESCAPED_UNICODE) . PHP_EOL,
    FILE_APPEND | LOCK_EX
);

// Telegram
if (!empty($config['telegram_token']) && !empty($config['telegram_chat_ids'])) {
    foreach ((array)$config['telegram_chat_ids'] as $chatId) {
        $ch = curl_init("https://api.telegram.org/bot{$config['telegram_token']}/sendMessage");
        curl_setopt_array($ch, [
            CURLOPT_POST => true,
            CURLOPT_RETURNTRANSFER => true,
            CURLOPT_TIMEOUT => 10,
            CURLOPT_POSTFIELDS => ['chat_id' => $chatId, 'text' => $text, 'parse_mode' => 'HTML'],
        ]);
        $res = curl_exec($ch);
        $sent = $sent || ($res !== false && curl_getinfo($ch, CURLINFO_HTTP_CODE) === 200);
        curl_close($ch);
    }
}

// Email
if (!empty($config['email_to'])) {
    $subject = '=?UTF-8?B?' . base64_encode('Заявка с сайта: ' . $lead['Формат']) . '?=';
    $headers = implode("\r\n", [
        'MIME-Version: 1.0',
        'Content-Type: text/html; charset=UTF-8',
        'From: ' . ($config['email_from'] ?? 'noreply@' . ($_SERVER['HTTP_HOST'] ?? 'localhost')),
    ]);
    $sent = @mail($config['email_to'], $subject, nl2br($text), $headers) || $sent;
}

respond($sent, $sent ? '' : 'Delivery failed');
