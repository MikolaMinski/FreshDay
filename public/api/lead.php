<?php
/**
 * Приём заявок с сайта → Telegram и/или email.
 * Требования: PHP 8.1+, расширение curl (есть на любом хостинге).
 * Настройки ищутся в freshday-config.php уровнем выше папки сайта или в api/config.php.
 */
declare(strict_types=1);

// Настройки ищутся сначала вне папки сайта (Plesk: /var/www/vhosts/<домен>/freshday-config.php —
// туда не попадает деплой из Git и нет доступа из браузера), затем рядом (api/config.php).
$privateDir = dirname(__DIR__, 2);
$configFile = null;
foreach ([$privateDir . '/freshday-config.php', __DIR__ . '/config.php'] as $f) {
    if (@is_file($f)) { $configFile = $f; break; }
}
$config = $configFile ? require $configFile : [];
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

// Оформление — то же, что у сервиса .NET (server/…/LeadFormatter.cs)
$e = fn(string $s): string => htmlspecialchars($s, ENT_QUOTES | ENT_HTML5, 'UTF-8');

/** +375 (29) 686-66-92 для белорусских номеров, иначе как ввели */
$formatPhone = function (string $raw): string {
    $d = preg_replace('/\D/', '', $raw);
    if (strlen($d) === 11 && str_starts_with($d, '80')) $d = '375' . substr($d, 2);
    elseif (strlen($d) === 9 && !str_starts_with(ltrim($raw), '+')) $d = '375' . $d;
    return strlen($d) === 12 && str_starts_with($d, '375')
        ? sprintf('+375 (%s) %s-%s-%s', substr($d, 3, 2), substr($d, 5, 3), substr($d, 8, 2), substr($d, 10, 2))
        : trim($raw);
};

/** 12.12.2026, сб (через 43 дн.) — по минскому времени (UTC+3) */
$formatDate = function (string $raw): string {
    $tz = new DateTimeZone('Europe/Minsk');
    $date = DateTimeImmutable::createFromFormat('!Y-m-d', $raw, $tz);
    if (!$date) return $raw;
    $today = new DateTimeImmutable('today', $tz);
    $days = (int)$today->diff($date)->format('%r%a');
    $rel = match (true) {
        $days === 0 => 'сегодня',
        $days === 1 => 'завтра',
        $days > 1   => "через {$days} дн.",
        default     => 'дата уже прошла',
    };
    $wd = ['вс', 'пн', 'вт', 'ср', 'чт', 'пт', 'сб'][(int)$date->format('w')];
    return $date->format('d.m.Y') . ", {$wd} ({$rel})";
};

$lines = ['🍽 <b>Новая заявка с сайта</b> #заявка', ''];
$lines[] = '👤 Имя: <b>' . $e($lead['Имя']) . '</b>';
$lines[] = '📞 Телефон: ' . $e($formatPhone($lead['Телефон']));
if ($lead['Формат'] !== '') $lines[] = '🎉 Формат: ' . $e($lead['Формат']);
if ($lead['Гостей'] !== '') $lines[] = '👥 Гостей: ' . $e($lead['Гостей']);
if ($lead['Дата'] !== '')   $lines[] = '📅 Дата: ' . $e($formatDate($lead['Дата']));
if ($lead['Комментарий'] !== '') {
    array_push($lines, '', '💬 Комментарий:', '<blockquote>' . $e($lead['Комментарий']) . '</blockquote>');
}
$page = $lead['Страница'];
if (str_starts_with($page, '/') && !str_starts_with($page, '//')) {
    $host = preg_replace('/[^a-z0-9.\-]/i', '', $_SERVER['HTTP_HOST'] ?? '');
    $lines[] = '';
    $lines[] = $host !== ''
        ? '🔗 Со страницы: <a href="' . $e("https://{$host}{$page}") . '">' . $e($page) . '</a>'
        : '🔗 Со страницы: ' . $e($page);
}
$text = implode("\n", $lines);
$sent = false;

// Резервная копия каждой заявки в файл (доступ извне закрыт в .htaccess)
$logDir = $configFile && str_starts_with($configFile, $privateDir . '/freshday-') ? $privateDir : __DIR__;
@file_put_contents(
    $logDir . '/' . ($logDir === __DIR__ ? 'leads.log' : 'freshday-leads.log'),
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
