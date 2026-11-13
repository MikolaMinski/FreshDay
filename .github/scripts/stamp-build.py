"""Дописывает метку сборки в конец каждого .html и .php в папке (аргумент).

Plesk при развёртывании из Git копирует только изменившиеся файлы; метка гарантирует,
что каждая сборка перезаписывает все страницы на хостинге.
"""
import os
import sys
from datetime import datetime, timezone
from pathlib import Path

root = Path(sys.argv[1])
stamp = f"build {os.environ.get('GITHUB_SHA', 'local')[:7]} {datetime.now(timezone.utc):%Y-%m-%dT%H:%M:%SZ}"
count = 0
for path in root.rglob("*"):
    if path.suffix == ".html":
        line = f"\n<!-- {stamp} -->\n"
    elif path.suffix == ".php":
        line = f"\n// {stamp}\n"
    else:
        continue
    with path.open("a", encoding="utf-8") as f:
        f.write(line)
    count += 1
print(f"Метка «{stamp}» добавлена в {count} файлов")
