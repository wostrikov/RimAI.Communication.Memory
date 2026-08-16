# Список команд Git для надсилання комітів

## Швидкі кроки для надсилання коміту

### 1. Переглянути зміни
```bash
git status
```

### 2. Додати всі розділені файли
```bash
git add Source/Memory/UI/MainTabWindow_Memory.cs
git add Source/Memory/UI/MainTabWindow_Memory_Actions.cs
git add Source/Memory/UI/MainTabWindow_Memory_Controls.cs
git add Source/Memory/UI/MainTabWindow_Memory_ImportExport.cs
git add Source/Memory/UI/MainTabWindow_Memory_Timeline.cs
git add Source/Memory/UI/MainTabWindow_Memory_TopBar.cs
git add Source/Memory/UI/MainTabWindow_Memory_Utilities.cs
git add Docs/拆分*.md
```

### 3. Створити коміт (рекомендовано)
```bash
git commit -m "refactor: 拆分 MainTabWindow_Memory 为 8 个 partial class 文件

主要改进:
- 主文件从 1590 行减少到 130 行 (减少 92%)
- 按功能模块拆分为 7 个部分类文件
- 提高代码可维护性和可读性

文件列表:
- TopBar (145行): Pawn选择器和统计信息
- Controls (376行): 过滤器和批量操作按钮  
- Timeline (440行): 时间线和记忆卡片绘制
- Actions (176行): 批量操作逻辑
- ImportExport (230行): 导入导出功能
- Utilities (210行): 辅助方法和对话框
- Helpers (280行): 记忆聚合算法

Breaking Changes: 无 (完全向后兼容)
"
```

### 4. Надіслати до віддаленого репозиторію
```bash
# 推送到分支 1
git push origin 1

# 或推送到 main
git push origin HEAD:main
```

---

## Необов’язково: створити окрему гілку для розділення

### Створити нову гілку
```bash
git checkout -b refactor/split-maintabwindow-memory
```

### Створити коміт і надіслати
```bash
git add Source/Memory/UI/MainTabWindow_Memory*.cs Docs/拆分*.md
git commit -m "refactor: 拆分 MainTabWindow_Memory 为 8 个 partial class 文件"
git push origin refactor/split-maintabwindow-memory
```

### Створення Pull Request
Потім створіть Pull Request на GitHub і злийте його до гілки main

---

## Обробка резервних файлів

### Варіант 1: Зберегти резервну копію (рекомендовано для першого коміту)
```bash
# 备份文件也提交，以便回滚
git add Source/Memory/UI/MainTabWindow_Memory_OLD_BACKUP.cs
```

### Варіант 2: Видалити резервну копію (подальше очищення)
```bash
# 删除备份文件
rm Source/Memory/UI/MainTabWindow_Memory_OLD_BACKUP.cs

# 或移到其他位置
mv Source/Memory/UI/MainTabWindow_Memory_OLD_BACKUP.cs ../backup/
```

---

## Контрольний список перевірки

Перед надсиланням підтвердьте:

- [ ] Усі нові файли додано до Git
- [ ] Основний файл правильно замінено
- [ ] Файл документації додано
- [ ] Повідомлення коміту чітко описує зміни
- [ ] (необов’язково) Код успішно скомпільовано
- [ ] (необов’язково) Функціональність успішно протестовано

---

## План відкату (якщо потрібно)

### Відкат до стану до розділення
```bash
# 恢复旧文件
git restore Source/Memory/UI/MainTabWindow_Memory.cs

# 或使用备份
cp Source/Memory/UI/MainTabWindow_Memory_OLD_BACKUP.cs Source/Memory/UI/MainTabWindow_Memory.cs

# 删除新文件
git rm Source/Memory/UI/MainTabWindow_Memory_*.cs
```

---

**Швидка довідка**: просто скопіюйте наведену вище команду й виконайте її!
