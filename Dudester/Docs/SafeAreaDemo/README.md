# Практика: автоматическая адаптация UI под мобильные устройства

Готовая сцена: `Assets/_Presentation/Scenes/SafeAreaDemo.unity`.
Ветка: `feature-safe-area`. Проверено 16 ноября 2026 года в Unity **6000.3.8f1**.

## Запуск после клонирования

1. Открыть проект Dudester в Unity 6000.3.8f1, дождаться импорта пакетов.
2. Открыть `Assets/_Presentation/Scenes/SafeAreaDemo.unity`.
3. Открыть **Window → General → Device Simulator**, выбрать устройство и включить **Safe Area**.
4. Нажать Play. Если Unity переключит фокус на обычный Game, выбрать вкладку Simulator: активным может быть только одно окно симуляции.
5. Переключать устройства и нажимать Rotate в Simulator без остановки Play Mode.

Сцена, локальный PNG и все `.meta` сохранены. Генерация перед запуском не требуется. Список игровых сцен в Build Settings не изменён; для отдельного билда демонстрации следует добавить SafeAreaDemo в собственный Build Profile.

## Реализация

```text
Demo Camera
Canvas (Screen Space - Overlay)
├── Background (полноэкранные якоря)
│   └── Image (локальный Background.png, Raycast Target выключен)
└── SafeAreaPanel (SafeAreaFitter)
    ├── TopBar
    │   └── Title
    ├── LeftAction
    │   └── Label
    └── RightAction
        └── Label
EventSystem (InputSystemUIInputModule)
```

CanvasScaler: **Scale With Screen Size**, **1080 × 1920**, Match **0.5**. У элементов HUD отступ 28 единиц Canvas; кнопки 280 × 116, с привязкой к нижним углам SafeAreaPanel. TopBar растянут по верхней стороне панели. На кнопках нет игровых обработчиков. Фон не подчиняется SafeAreaFitter и заполняет весь экран, включая область под вырезом.

`Assets/_Source/UiSystem/SafeAreaFitter.cs`, namespace `UiSystem`, использует `UnityEngine.Device.Screen`, поддерживающий симуляцию в редакторе и обычное выполнение в билде. Якоря рассчитываются так:

```csharp
anchorMin = new Vector2(safeArea.xMin / width, safeArea.yMin / height);
anchorMax = new Vector2(safeArea.xMax / width, safeArea.yMax / height);
offsetMin = offsetMax = Vector2.zero;
```

При включении компонента происходит первое применение; далее проверяются и размеры экрана, и сам Rect Safe Area. Неизменившиеся данные не вызывают запись в RectTransform или новый лог. Кадры с нулевым размером пропускаются и не сохраняются в кэш. Лог содержит `Screen.safeArea`, разрешение и якоря.

В проекте используются uGUI **2.0.0** и Input System **1.18.0**. Device Simulator встроен в эту версию редактора; добавлен официальный пакет профилей `com.unity.device-simulator.devices` **1.0.1**, зафиксированный в manifest и packages-lock. Совместимость пакета описана в [документации Unity 6000.3](https://docs.unity.com/en-us/engine/6000.3/manual/packages-list/packages-all/pack-safe/com-unity-device-simulator-devices); API — в [UnityEngine.Device.Screen](https://docs.unity.com/en-us/engine/6000.3/script-reference/unityengine/device/screen).

## Фактически выполненная проверка

Все пять снимков сделаны из настоящего окна Device Simulator в **одном непрерывном запуске Play Mode**. На каждом видны название выбранного устройства, HUD и включённый жёлтый контур Safe Area. Это профили Simulator, не испытания на физических телефонах.

| Устройство / ориентация | Особенность | Разрешение | Safe Area: x, y, width, height | Скриншот |
| --- | --- | --- | --- | --- |
| Apple iPhone X, портрет | Широкий верхний вырез, нижний безопасный отступ | 1125 × 2436 | 0, 102, 1125, 2202 | [01](Screenshots/01-iphone-x-portrait.png) |
| Apple iPhone X, ландшафт после поворота | Боковые отступы вокруг выреза | 2436 × 1125 | 132, 63, 2172, 1062 | [02](Screenshots/02-iphone-x-landscape.png) |
| Google Pixel 5, портрет | Круглая камера слева | 1080 × 2340 | 0, 0, 1080, 2204 | [03](Screenshots/03-pixel-5-portrait.png) |
| Samsung Galaxy S10+, портрет | Продолговатый вырез справа | 1440 × 3040 | 0, 0, 1440, 2898 | [04](Screenshots/04-galaxy-s10-plus-portrait.png) |
| Apple iPhone 8, портрет | Толстые рамки без выреза | 750 × 1334 | 0, 0, 750, 1334 | [05](Screenshots/05-iphone-8-portrait.png) |

У iPhone X после поворота якоря изменились с `(0, 0.041872)–(1, 0.945813)` на `(0.054187, 0.056000)–(0.945813, 1)`. TopBar и обе кнопки остались внутри безопасного прямоугольника и сохранили привязки. Для каждого снимка рядом сохранён `.txt` с разрешением, безопасным прямоугольником, якорями, нулевыми offsets, координатами углов HUD и номером кадра. Границы элементов также проверены численно относительно Safe Area. Фон во всех пяти состояниях сохранил якоря `(0, 0)–(1, 1)` и нулевые offsets.

[runtime.txt](runtime.txt) содержит исходные сообщения одного проверочного запуска, включая стартовый лог. Промежуточное `537×302` соответствует переключению фокуса в обычный Game, после возврата в Simulator вновь применился профиль iPhone X. При трёхсекундном наблюдении неизменного экрана новых сообщений не появилось.

## Автоматические проверки

Unity EditMode: **6 / 6 пройдено, 0 ошибок, 0 пропусков**. Исходный результат Unity Test Runner: [EditMode-results.xml](EditMode-results.xml).

- Нормализация всех четырёх координат и обнуление offsets.
- Изменение Safe Area при неизменном разрешении.
- Изменение разрешения при неизменном Rect Safe Area.
- Отсутствие повторной записи и лога при неизменных данных.
- Пропуск нулевой ширины / высоты с применением следующего корректного кадра.
- Пересчёт портретных якорей в ландшафтные.

Повторный запуск: **Window → General → Test Runner → EditMode → Dudester.SafeArea.Tests**. Проверка структуры сохранённой сцены доступна в меню **Dudester → Safe Area → Validate demo scene**. Дополнительный пункт **Create demo scene** пересоздаёт только демонстрационную сцену и её фон; для обычного просмотра он не нужен.

## Состав изменений

- `Assets/_Source/UiSystem/SafeAreaFitter.cs` и `.meta` — рабочий компонент.
- `Assets/_Presentation/Scenes/SafeAreaDemo.unity` и `.meta` — готовая сцена.
- `Assets/_Presentation/SafeAreaDemo/Background.png` и `.meta` — локальный фоновый спрайт.
- `Assets/_Source/UiSystem/Editor/SafeAreaDemoBuilder.cs`, `Dudester.SafeArea.Editor.asmdef` и `.meta` — создание и валидация демо в редакторе.
- `Assets/_Source/UiSystem/Tests/Editor/SafeAreaFitterTests.cs`, `Dudester.SafeArea.Tests.asmdef` и `.meta` — EditMode-тесты.
- `.meta` новых каталогов — стабильные GUID для импорта.
- `Packages/manifest.json`, `Packages/packages-lock.json` — официальный пакет профилей устройств.
- `Docs/SafeAreaDemo/` — этот отчёт, пять PNG, пять файлов состояния, лог и XML тестов.

Игровые сцены, HUD Dudester и настройки Player/Build не менялись. Unity Editor и Simulator были доступны: обязательных ручных проверок или недостающих скриншотов для этой практики не осталось.
