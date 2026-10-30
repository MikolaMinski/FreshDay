#!/usr/bin/env bash
# Кладёт содержимое папки в отдельную ветку репозитория отдельным коммитом поверх прежнего
# (без force-push — так Plesk спокойно подтягивает обновления).
# Использование: publish-branch.sh <ветка> <папка>
set -euo pipefail

branch="$1"
src="$(cd "$2" && pwd)"
repo="https://x-access-token:${GH_TOKEN}@github.com/${GITHUB_REPOSITORY}.git"
work="$(mktemp -d)"
sha="${GITHUB_SHA:0:7}"

git config --global user.name "github-actions[bot]"
git config --global user.email "41898282+github-actions[bot]@users.noreply.github.com"

if git ls-remote --exit-code --heads "$repo" "$branch" >/dev/null 2>&1; then
  git clone --quiet --depth 1 --branch "$branch" "$repo" "$work"
else
  git init --quiet -b "$branch" "$work"
  git -C "$work" remote add origin "$repo"
fi

# Заменить всё содержимое ветки, кроме самой папки .git
find "$work" -mindepth 1 -maxdepth 1 ! -name .git -exec rm -rf {} +
cp -a "$src/." "$work/"

cd "$work"
git add -A
if git diff --cached --quiet; then
  echo "Ветка $branch уже актуальна"
  exit 0
fi
git commit --quiet -m "Сборка из main@${sha}"
git push --quiet origin "HEAD:$branch"
echo "Ветка $branch обновлена (main@${sha})"
