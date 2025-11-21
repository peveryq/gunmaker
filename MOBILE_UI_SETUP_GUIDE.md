# Mobile UI Setup Guide

## Overview
Этап 2 реализован: система мобильного управления с автоматическим определением устройства и адаптивным UI.

## Компоненты системы

### 1. DeviceDetectionManager
- **Путь:** `Assets/_InternalAssets/Scrips/Core/DeviceDetectionManager.cs`
- **Функция:** Определяет тип устройства (Desktop/Mobile/Tablet) через YG2 SDK
- **Singleton:** Да, с DontDestroyOnLoad
- **Настройки:** 
  - `forceDesktop` - принудительный режим десктопа для тестирования

### 2. MobileInputManager  
- **Путь:** `Assets/_InternalAssets/Scrips/Core/MobileInputManager.cs`
- **Функция:** Централизованное управление мобильным вводом
- **События:** OnShootPressed/Released, OnAimPressed/Released, OnReloadPressed, OnDropPressed, OnMovementChanged
- **Singleton:** Да, с DontDestroyOnLoad

### 3. MobileButton
- **Путь:** `Assets/_InternalAssets/Scrips/UI/MobileButton.cs`
- **Функция:** Универсальная мобильная кнопка с расширенной областью нажатия
- **Настройки:**
  - `hitAreaMultiplier` - множитель области нажатия (1.2 = на 20% больше визуального размера)
  - `supportHold` - поддержка удержания кнопки
  - Визуальная анимация нажатия и цвета

### 4. VirtualJoystick
- **Путь:** `Assets/_InternalAssets/Scrips/UI/VirtualJoystick.cs`
- **Функция:** Виртуальный джойстик для движения
- **Настройки:**
  - `handleRange` - максимальное расстояние движения ручки
  - `snapToPointer` - привязка к позиции касания
  - `hitAreaMultiplier` - расширенная область касания

### 5. MobileUIController
- **Путь:** `Assets/_InternalAssets/Scrips/UI/MobileUIController.cs`
- **Функция:** Главный контроллер мобильного UI, управляет видимостью кнопок
- **Логика видимости:**
  - Кнопки стрельбы/прицеливания - только при наличии оружия
  - Кнопка перезарядки - только при наличии оружия с магазином
  - Кнопка сброса - при наличии любого предмета в руках
  - Джойстик движения - всегда виден на мобильных устройствах

## Интеграция с существующими системами

### FirstPersonController
- Добавлена поддержка мобильного ввода для движения
- Комбинирует клавиатурный и мобильный ввод

### WeaponController  
- Добавлена поддержка мобильных кнопок стрельбы, прицеливания и перезарядки
- Мобильное прицеливание поддерживает click/hold режим как на десктопе:
  - Короткое нажатие (< 0.25с) = toggle режим прицеливания
  - Долгое удержание = hold режим прицеливания

### InteractionHandler
- Добавлена поддержка мобильной кнопки сброса предметов
- Автоматические уведомления MobileUIController о подборе/сбросе предметов

### InteractionButtonView
- Автоматически скрывает клавиатурные подсказки на мобильных устройствах
- Переключается между Button и MobileButton в зависимости от типа устройства
- Поддерживает hold-взаимодействия (например, сварка) на мобильных устройствах

### GameManager
- Добавлена инициализация DeviceDetectionManager и MobileInputManager
- Правильный порядок инициализации систем

## Настройка в Unity

### 0. Настройка InteractionButtonView префабов
Для поддержки мобильных взаимодействий нужно добавить MobileButton к префабам кнопок взаимодействия:

```
InteractionButtonView (префаб)
├── Button (существующий, активный)
├── MobileButton (НОВЫЙ, неактивный по умолчанию)
├── Background (Image)
├── ContentRow
│   ├── LabelText (TextMeshPro)
│   └── KeyHintRoot
│       └── KeyText (TextMeshPro)
```

**Настройка MobileButton:**
- `GameObject.SetActive(false)` по умолчанию
- `supportHold = true` (для поддержки сварки и других hold-взаимодействий)
- `buttonImage` → ссылка на Background Image
- `hitAreaMultiplier = 1.3` (для удобства на мобильных)

### 1. Создание мобильного UI
```
Canvas (Screen Space - Overlay)
├── MobileUIRoot (GameObject)
    ├── MovementJoystick (VirtualJoystick)
    │   ├── Background (Image)
    │   └── Knob (Image)
    ├── ActionButtons (GameObject - Bottom Right)
    │   ├── ShootButton (MobileButton)
    │   ├── AimButton (MobileButton)
    │   └── ReloadButton (MobileButton)
    └── UtilityButtons (GameObject - Bottom Left)
        └── DropButton (MobileButton)
```

### 2. Настройка MobileUIController
- Перетащить все UI элементы в соответствующие поля
- Настроить MobileUIRoot как родительский объект
- Иконки настраиваются прямо на каждой MobileButton

### 3. Настройка кнопок
- **Shoot button:** `supportHold = true` (для удержания стрельбы)
- **Aim button:** `supportHold = true` (для click/hold прицеливания как на десктопе)
- **Reload/Drop buttons:** `supportHold = false` (одиночные нажатия)
- **Hit Area Multiplier:** рекомендуется 1.2-1.5 для удобства
- **Icon Image:** назначить Sprite иконки прямо в MobileButton.iconImage

#### Настройка иконок на каждой кнопке:
```
ShootButton (GameObject + MobileButton)
├── Button Image (фон кнопки)
└── Icon (GameObject + Image) ← назначить Sprite здесь
```
В MobileButton компоненте:
- `buttonImage` → ссылка на Image фона
- `iconImage` → ссылка на Image иконки (уже с назначенным Sprite)

### 4. Настройка джойстика
- **Handle Range:** 50-80 пикселей
- **Snap to Pointer:** true для лучшего UX
- **Hit Area Multiplier:** 1.2-1.3
- **Return Speed:** скорость возврата ручки в центр (только визуально)
- **Поведение:** движение останавливается сразу при отпускании, ручка возвращается плавно

## Расположение кнопок

### Нижний правый угол (Action Buttons):
1. **Кнопка стрельбы** - появляется при наличии оружия
2. **Кнопка прицеливания** - появляется при наличии оружия  
3. **Кнопка перезарядки** - появляется при наличии оружия с магазином

### Нижний левый угол (Utility Buttons):
1. **Кнопка сброса** - появляется при наличии любого предмета в руках
2. **Стик движения** - всегда виден на мобильных устройствах

## Тестирование

### В редакторе:
1. Установить `DeviceDetectionManager.forceDesktop = false`
2. Использовать `DeviceDetectionManager.SetDeviceTypeForTesting(DeviceType.Mobile)`
3. **Для тестирования мышью:** Нажать ESC для разблокировки курсора, затем кликать по мобильным кнопкам
4. **Движение и стрельба работают** даже при разблокированном курсоре (для удобства тестирования)

### На устройстве:
- Мобильный UI автоматически активируется на мобильных устройствах и планшетах
- Определение происходит через YG2.envir.isMobile/isTablet

### Отладка:
- Включите Console в Unity для просмотра сообщений:
  - "Movement input: (x, y)" при движении джойстика
  - "Finger lifted, movement stopped immediately" при отпускании джойстика
  - "Aim button pressed/released" при нажатии прицеливания

## Производительность
- Все кнопки используют DOTween для анимаций (если доступен)
- Минимальные аллокации памяти
- Оптимизировано для WebGL

## Особенности работы с интерактивными элементами

### Сварка (Welding)
Система сварки теперь полностью поддерживает мобильные устройства:

1. **WeldingController** - синглтон, управляющий процессом сварки
2. **Hold-интерактивность** - кнопка сварки требует удержания:
   - На мобильных устройствах: удерживайте кнопку для сварки
   - На десктопе: удерживайте клавишу E (или настроенную клавишу)
3. **Интеграция с InteractionButtonView**:
   - Автоматически определяет hold-интерактивность через `RequiresHold` свойство
   - Показывает соответствующие кнопки для мобильных устройств

### Настройка hold-интерактивности
Для создания новых hold-интерактивностей:
```csharp
options.Add(InteractionOption.Primary(
    id: "custom.hold",
    label: "Hold Action",
    key: KeyCode.E,
    isAvailable: true,
    callback: StartHoldAction,
    requiresHold: true  // Включает hold-логику
));
```

### Диагностика проблем со сваркой
При проблемах со сваркой проверьте консоль на наличие следующих логов:

**При нажатии кнопки сварки:**
- `HandleMobilePressed called for: weld, RequiresHold: True, IsAvailable: True`
- `Starting welding via mobile button press`
- `StartWeldingInteraction called`
- `Welding components - blowtorch: True, unweldedBarrel: True, WeldingController: True`
- `StartWelding called - isWelding: False, workbench: True, blowtorch: True, target: True`
- `Starting blowtorch with work position: True`
- `Welding started via interaction button`

**При отпускании кнопки сварки:**
- `HandleMobileReleased called for: weld, RequiresHold: True`
- `Stopping welding via mobile button release`
- `StopWeldingInteraction called`
- `StopWelding called - isWelding: True`
- `Stopping blowtorch`
- `Welding stopped`

**Возможные проблемы:**
1. `MobileButton.SupportHold = false` - кнопка не настроена для hold-интерактивности
2. `RequiresHold: False` - опция не помечена как hold-интерактивность
3. `WeldingController: False` - WeldingController не инициализирован
4. `unweldedBarrel: False` - нет ствола, требующего сварки

## Hold Interactions (Обновлено)

**НОВЫЙ ПОДХОД:** `InteractionButtonView` теперь имеет встроенную поддержку hold-интерактивностей без использования `MobileButton`.

### Как работает Hold Logic

1. **Автоматическое определение:**
   - `InteractionButtonView` проверяет свойство `InteractionOption.RequiresHold`
   - Если `true`, использует hold-логику вместо click-логики

2. **Hold Logic:**
   - **OnPointerDown:** Начинает интерактивность (например, начинает сварку)
   - **OnPointerUp:** Останавливает интерактивность (например, останавливает сварку)  
   - **OnPointerExit:** Также останавливает, если пользователь увел курсор/палец с кнопки

3. **Интеграция с Workbench:**
   - `Workbench.PopulateInteractionOptions()` устанавливает `RequiresHold = true` для опций сварки
   - `InteractionButtonView` автоматически обрабатывает остальное

**Преимущества нового подхода:**
- Нет необходимости в дополнительных компонентах (`MobileButton`)
- Работает одинаково на desktop и mobile
- Проще в настройке и отладке
- Меньше потенциальных конфликтов между компонентами

## Управление камерой на мобильных устройствах

### MobileCameraController
Автоматически создается `MobileUIController` и обеспечивает:

**Функции:**
- Фильтрация касаний по областям экрана
- Исключение UI элементов (кнопки, джойстик) из управления камерой
- Поддержка мультитача (один палец - движение, другой - камера)
- Автоматическое переключение между desktop и mobile режимами
- Симуляция касаний мышью в Unity Editor для тестирования

**Настройки:**
- `touchSensitivity` - чувствительность касаний для камеры
- `invertY` - инвертировать вертикальную ось
- `exclusionAreas` - области, где касания не управляют камерой (автоматически настраивается)
- `cameraControlArea` - область для управления камерой (если null, весь экран)

### Тестирование в Unity Editor
**Управление камерой:**
- В mobile режиме: мышка симулирует касания для поворота камеры
- Работает только в областях, не занятых UI элементами
- Безопасно: не вызывает стрельбу (защищено директивами компилятора)

**Управление оружием:**
- Кнопки мобильного UI работают при клике мышью
- Джойстик движения работает при перетаскивании мышью
- Можно тестировать весь мобильный функционал без реального устройства

### Предотвращение случайной стрельбы
На мобильных устройствах:
- Мышиный ввод полностью игнорируется
- Стрельба происходит только через мобильные кнопки
- Прицеливание и перезарядка также только через кнопки
- На desktop сохраняется обычное управление + поддержка мобильных кнопок для тестирования

## Следующие этапы
- Этап 3: Interstitial и Rewarded реклама
- Этап 4: Pause control
- Этап 5: Финальная интеграция и тестирование
