# YG2 SDK Integration - Этапы 1-2: Обзор реализации

## Общая информация

Этот документ описывает реализацию первых двух этапов интеграции YG2 SDK в проект. Документ предназначен для контекста в новых чатах при продолжении работы над этапом 3 (Interstitial Ads).

---

## Этап 1: Общий Game Manager система для инициализации систем и загрузки уровня

### Статус: ✅ Полностью реализован

### Файлы
- `Assets/_InternalAssets/Scrips/Core/GameManager.cs`

### Реализованная функциональность

#### 1.1. Singleton Pattern с DontDestroyOnLoad
- `GameManager` существует в единственном экземпляре
- Сохраняется между сценами через `DontDestroyOnLoad`
- Автоматически находит `LoadingScreen` если не назначен в инспекторе

#### 1.2. Последовательная инициализация систем
Корoutine `InitializeGame()` инициализирует системы в следующем порядке:

1. **DeviceDetectionManager** - определение типа устройства
2. **LocalizationManager** - система локализации
3. **SettingsManager** - настройки игры (создается автоматически если не существует)
4. **MobileInputManager** - мобильный ввод
5. **WeldingController** - система сварки (создается автоматически если не существует)
6. **AdManager** - менеджер рекламы (опционально, через рефлексию)

#### 1.3. Ожидание загрузки данных
- **LocationManager**: Ожидает инициализации `LocationManager.Instance` (таймаут: `maxLocationLoadWaitTime`)
- **SaveSystemManager**: Ожидает завершения загрузки сохранений через событие `OnLoadComplete` (таймаут: `maxSaveLoadWaitTime`)
- **Location Fully Loaded**: Ожидает полной загрузки локации (несколько кадров + 0.2 секунды)

#### 1.4. Loading Screen интеграция
- Показывает `LoadingScreen` в начале инициализации
- Скрывает через `FadeOut()` после завершения всех инициализаций
- Настраиваемые параметры: `initializationDelay`, `loadingScreenFadeDuration`

#### 1.5. События
- `OnGameInitialized` - вызывается после полной инициализации
- `IsInitialized` - публичное свойство для проверки статуса

### Ключевые особенности
- Все системы опциональны - если система не найдена, инициализация пропускается с предупреждением
- Таймауты предотвращают бесконечное ожидание
- Поддержка рефлексии для опциональных систем (AdManager)
- Автоматическое создание некоторых систем (SettingsManager, WeldingController)

---

## Этап 2: Device Detection (Определение устройства)

### Статус: ✅ Полностью реализован

### Файлы
- `Assets/_InternalAssets/Scrips/Core/DeviceDetectionManager.cs`
- `Assets/_InternalAssets/Scrips/Core/MobileInputManager.cs`
- `Assets/_InternalAssets/Scrips/UI/MobileUIController.cs`

### Реализованная функциональность

#### 2.1. DeviceDetectionManager - Определение типа устройства

**Источники определения:**
1. **YG2 SDK** (приоритетный):
   - Использует `YG2.envir.deviceType`
   - Проверяет `YG2.envir.isMobile` → `DeviceType.Mobile`
   - Проверяет `YG2.envir.isTablet` → `DeviceType.Tablet`
   - Иначе → `DeviceType.Desktop`

2. **Fallback через Unity SystemInfo**:
   - Если YG2 SDK недоступен, использует `Application.isMobilePlatform`
   - Эвристика по диагонали экрана: < 7 дюймов = Mobile, иначе = Tablet

**Типы устройств:**
```csharp
public enum DeviceType
{
    Desktop,
    Mobile,
    Tablet
}
```

**Публичные свойства:**
- `CurrentDeviceType` - текущий тип устройства
- `IsMobile` - true если Mobile
- `IsTablet` - true если Tablet
- `IsMobileOrTablet` - true если Mobile или Tablet
- `IsDesktop` - true если Desktop

**События:**
- `OnDeviceTypeChanged` - вызывается при изменении типа устройства

**Особенности:**
- Подписка на `YG2.onGetSDKData` для обновления при готовности SDK
- Метод `RefreshDeviceDetection()` для принудительного обновления
- Метод `SetDeviceTypeForTesting()` для тестирования в редакторе
- Флаг `forceDesktop` для принудительного desktop режима в редакторе

#### 2.2. MobileInputManager - Централизованный мобильный ввод

**Функциональность:**
- Singleton с `DontDestroyOnLoad`
- Управляет состоянием мобильных кнопок и джойстика
- Предоставляет события для других систем

**Входные данные:**
- `MovementInput` (Vector2) - от виртуального джойстика
- `IsShootPressed` - состояние кнопки стрельбы
- `IsAimPressed` - состояние кнопки прицеливания
- `IsReloadPressed` - триггер перезарядки
- `IsDropPressed` - триггер сброса предмета

**События:**
- `OnShootPressed` / `OnShootReleased`
- `OnAimPressed` / `OnAimReleased`
- `OnReloadPressed`
- `OnDropPressed`
- `OnMovementChanged` (Vector2)

**Методы:**
- `SetMovementInput(Vector2)` - установка движения от джойстика
- `SetShootPressed(bool)` - установка состояния стрельбы
- `SetAimPressed(bool)` - установка состояния прицеливания
- `TriggerReload()` - триггер перезарядки (одноразовое событие)
- `TriggerDrop()` - триггер сброса (одноразовое событие)
- `SetMobileInputEnabled(bool)` - включение/выключение мобильного ввода
- `IsMobileInputActive()` - проверка активности мобильного ввода

#### 2.3. MobileUIController - Управление мобильным UI

**Функциональность:**
- Автоматическое включение/выключение мобильных элементов UI
- Подписка на `DeviceDetectionManager.OnDeviceTypeChanged`
- Управление видимостью кнопок в зависимости от состояния игры

**Мобильные элементы:**
- `VirtualJoystick` - джойстик движения
- `MobileButton shootButton` - кнопка стрельбы (основная)
- `MobileButton shootButton2` - кнопка стрельбы (дополнительная, для удобства)
- `MobileButton aimButton` - кнопка прицеливания
- `MobileButton reloadButton` - кнопка перезарядки
- `MobileButton dropButton` - кнопка сброса предмета

**Интеграция:**
- Подключение к `MobileInputManager` для передачи событий
- Настройка `MobileCameraController` с exclusion areas для кнопок
- Управление видимостью кнопок в зависимости от наличия оружия в руках

**Методы:**
- `OnWeaponEquipped()` - показ кнопок действия при экипировке оружия
- `OnWeaponUnequipped()` - скрытие кнопок действия при снятии оружия
- `OnItemDropped()` - скрытие кнопки сброса при сбросе предмета

### Интеграция с другими системами

**WeaponController:**
- Проверяет `DeviceDetectionManager.Instance.IsMobileOrTablet`
- Использует `MobileInputManager` для мобильного ввода
- Игнорирует мышиный ввод на мобильных устройствах

**FirstPersonController:**
- Интегрирован с `MobileCameraController` для мобильного управления камерой
- Поддержка внешнего ввода от мобильного контроллера камеры

**GameManager:**
- Инициализирует `DeviceDetectionManager` и `MobileInputManager` в начале игры
- Принудительно обновляет определение устройства если YG2 SDK готов

---

## Этап 3: Interstitial Ads (Межстраничная реклама)

### Статус: ❌ Еще не начат

### Текущее состояние

#### 3.1. Интеграция в GameManager
- В `GameManager.InitializeGame()` есть заготовка `InitializeAdManager()` через рефлексию
- Метод проверяет наличие `AdManager` и инициализирует его, если он существует
- На данный момент `AdManager` не создан

#### 3.2. YG2 SDK модуль InterstitialAdv
- Модуль YG2 SDK `InterstitialAdv` установлен в проекте
- Доступны примеры использования в `Assets/PluginYourGames/Modules/InterstitialAdv/Example/`
- Доступен таймер: `YG2.timerInterAdv`
- API готов к использованию: `YG2.InterstitialAdvShow()`

### Что нужно реализовать

#### 3.3. AdManager (требуется создание)
Нужно создать `AdManager` со следующей функциональностью:

1. **Таймер рекламы:**
   - Использовать `YG2.timerInterAdv` для отслеживания времени до показа рекламы
   - Настраиваемый интервал показа (например, каждые 3 минуты)

2. **Условия показа:**
   - НЕ показывать рекламу в Testing Range (стрельбище)
   - Показывать только в Workshop
   - Проверка через `LocationManager.CurrentLocation`

3. **Полноэкранное окно с таймером:**
   - UI окно, которое появляется перед показом рекламы
   - Отображение обратного отсчета (например, "Реклама через 3... 2... 1...")
   - Кнопка "Пропустить" (если разрешено) или автоматический показ

4. **Интеграция паузы игры:**
   - При показе окна с таймером → `Time.timeScale = 0`
   - При показе рекламы → `Time.timeScale = 0`
   - После закрытия рекламы → `Time.timeScale = 1`
   - Блокировка управления игроком во время рекламы

5. **Вызов рекламы:**
   - Использовать `YG2.InterstitialAdvShow()` для показа рекламы
   - Обработка событий `YG2.onInterstitialAdvOpen` и `YG2.onInterstitialAdvClose`

### Рекомендуемая структура AdManager

```csharp
public class AdManager : MonoBehaviour
{
    public static AdManager Instance { get; private set; }
    
    [Header("Ad Settings")]
    [SerializeField] private float adIntervalMinutes = 3f; // Интервал между рекламами
    [SerializeField] private bool showInTestingRange = false; // Показывать ли в стрельбище
    
    [Header("UI References")]
    [SerializeField] private GameObject adTimerWindow; // Окно с таймером
    [SerializeField] private TextMeshProUGUI timerText; // Текст таймера
    
    private float timeSinceLastAd = 0f;
    private bool isAdShowing = false;
    
    // Методы:
    // - CheckAdTimer() - проверка таймера YG2.timerInterAdv
    // - ShowAdTimerWindow() - показ окна с таймером
    // - ShowInterstitialAd() - показ рекламы через YG2.InterstitialAdvShow()
    // - PauseGame() / ResumeGame() - управление паузой
}
```

### Интеграция с LocationManager
- Подписка на `LocationManager.OnLocationChangedEvent`
- Проверка `LocationManager.CurrentLocation` перед показом рекламы
- Показ рекламы только при переходе из Testing Range в Workshop (опционально)

---

## Следующий этап: Этап 3 - Interstitial Ads

### План реализации

1. **Создание AdManager:**
   - Singleton менеджер для управления межстраничной рекламой
   - Интеграция с `GameManager` (уже есть заготовка)
   - Использование `YG2.timerInterAdv` для отслеживания таймера

2. **Условия показа рекламы:**
   - НЕ показывать в Testing Range (стрельбище)
   - Показывать только в Workshop
   - Проверка через `LocationManager.CurrentLocation`

3. **Полноэкранное окно с таймером:**
   - UI окно, появляющееся перед показом рекламы
   - Отображение обратного отсчета (например, "Реклама через 3... 2... 1...")
   - Автоматический показ рекламы после отсчета

4. **Интеграция паузы игры:**
   - При показе окна с таймером → `Time.timeScale = 0`
   - При показе рекламы → `Time.timeScale = 0`
   - После закрытия рекламы → `Time.timeScale = 1`
   - Блокировка управления игроком во время рекламы

5. **Вызов рекламы:**
   - Использовать `YG2.InterstitialAdvShow()` для показа рекламы
   - Обработка событий `YG2.onInterstitialAdvOpen` и `YG2.onInterstitialAdvClose`

### Файлы для работы
- `Assets/_InternalAssets/Scrips/Core/GameManager.cs` - интеграция AdManager
- `Assets/_InternalAssets/Scrips/LocationManager.cs` - проверка текущей локации
- `Assets/PluginYourGames/Modules/InterstitialAdv/Example/` - примеры использования YG2 SDK
- Создать: `Assets/_InternalAssets/Scrips/Ads/AdManager.cs`
- Создать: UI окно с таймером для рекламы

---

## Технические детали

### Зависимости между системами

```
GameManager
  ├── DeviceDetectionManager (Singleton, DontDestroyOnLoad)
  │   └── Использует YG2.envir.deviceType
  ├── MobileInputManager (Singleton, DontDestroyOnLoad)
  │   └── Зависит от DeviceDetectionManager
  ├── MobileUIController (на сцене)
  │   ├── Подписывается на DeviceDetectionManager.OnDeviceTypeChanged
  │   └── Использует MobileInputManager
  └── AdManager (планируется)
      └── Зависит от LocationManager
```

### YG2 SDK интеграция

**Используемые модули:**
- `YG2.envir` - информация об окружении (deviceType, isMobile, isTablet)
- `YG2.isSDKEnabled` - проверка готовности SDK
- `YG2.onGetSDKData` - событие готовности SDK
- `YG2.timerInterAdv` - таймер межстраничной рекламы (для этапа 3)
- `YG2.InterstitialAdvShow()` - показ межстраничной рекламы (для этапа 3)
- `YG2.RewardedAdvShow()` - показ вознаграждаемой рекламы (для этапа 4)

**События YG2:**
- `YG2.onInterstitialAdvOpen` - открытие межстраничной рекламы
- `YG2.onInterstitialAdvClose` - закрытие межстраничной рекламы
- `YG2.onRewardAdv` - получение награды за просмотр рекламы (для этапа 4)

---

## Примечания для продолжения работы

1. **Этап 3 (Interstitial Ads)** - следующий этап работы:
   - Создать `AdManager` как singleton с DontDestroyOnLoad
   - Реализовать проверку таймера через `YG2.timerInterAdv`
   - Создать UI окно с таймером (полноэкранное)
   - Интегрировать паузу игры через `Time.timeScale`
   - Подписаться на события YG2 для управления состоянием
   - Проверять локацию через `LocationManager.CurrentLocation`

2. **Этап 4 (Rewarded Ads)** - после завершения этапа 3:
   - Найти или создать экран результатов
   - Добавить кнопку "Удвоить награду"
   - Интегрировать с `EarningsTracker`
   - Реализовать логику удвоения дохода через `YG2.RewardedAdvShow()`

3. **Тестирование:**
   - В редакторе YG2 SDK может быть недоступен
   - Использовать `DeviceDetectionManager.SetDeviceTypeForTesting()` для тестирования
   - Проверять готовность SDK через `YG2.isSDKEnabled`
   - YG2 SDK модули могут требовать билд для полного тестирования

---

_Документ создан: Декабрь 2025_
_Последнее обновление: Декабрь 2025_
_Статус: Этапы 1-2 завершены, Этап 3 - следующий_

