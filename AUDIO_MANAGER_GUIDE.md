# AudioManager - Руководство (Упрощенная версия)

## Что это и зачем?

**AudioManager** - это упрощенная централизованная система управления звуками в игре. Использует **два AudioSource**: один для SFX (звуковых эффектов), один для музыки.

### Преимущества:

1. **Простота:**
   - Нет пулинга - один AudioSource для всех SFX
   - Два AudioSource: SFX и Music
   - Все звуки 2D (без пространственной локализации)

2. **Централизованное управление:**
   - Одна точка для настройки громкости (SFX, Music, Master)
   - Сохранение настроек в PlayerPrefs
   - Легко добавить паузу/стоп всех звуков

3. **Раздельная настройка громкости:**
   - Master Volume - общая громкость
   - SFX Volume - громкость звуковых эффектов
   - Music Volume - громкость музыки

## Использование

### 1. Настройка на сцене

1. Создайте пустой GameObject на сцене (можно в `===SYSTEMS===`)
2. Добавьте компонент `AudioManager`
3. **Опционально**: Назначьте AudioSource в поля:
   - **SFX Source** - для звуковых эффектов (если не назначен, создастся автоматически)
   - **Music Source** - для музыки (если не назначен, создастся автоматически)
4. Настройте начальные значения громкости в Inspector

### 2. Проигрывание звуков

#### Звуковые эффекты (SFX):
```csharp
AudioManager.Instance.PlaySFX(hitSound, volume: 0.8f);
```

#### Музыка:
```csharp
// Воспроизвести музыку
AudioManager.Instance.PlayMusic(backgroundMusic, volume: 0.7f, loop: true);

// Остановить музыку
AudioManager.Instance.StopMusic();

// Пауза/возобновление
AudioManager.Instance.PauseMusic();
AudioManager.Instance.ResumeMusic();
```

### 3. Управление громкостью

```csharp
// Установить громкость (автоматически сохраняется)
AudioManager.Instance.MasterVolume = 0.8f;
AudioManager.Instance.SFXVolume = 0.9f;
AudioManager.Instance.MusicVolume = 0.6f;

// Получить текущую громкость
float currentSFX = AudioManager.Instance.SFXVolume;
```

### 4. Интеграция с UI (настройки)

```csharp
// В UI скрипте для слайдеров громкости:
public Slider masterVolumeSlider;
public Slider sfxVolumeSlider;
public Slider musicVolumeSlider;

void Start()
{
    // Загрузить текущие значения
    masterVolumeSlider.value = AudioManager.Instance.MasterVolume;
    sfxVolumeSlider.value = AudioManager.Instance.SFXVolume;
    musicVolumeSlider.value = AudioManager.Instance.MusicVolume;
    
    // Подписаться на изменения
    masterVolumeSlider.onValueChanged.AddListener(value => 
        AudioManager.Instance.MasterVolume = value);
    sfxVolumeSlider.onValueChanged.AddListener(value => 
        AudioManager.Instance.SFXVolume = value);
    musicVolumeSlider.onValueChanged.AddListener(value => 
        AudioManager.Instance.MusicVolume = value);
}
```

## Миграция существующего кода

### Было (на мишени):
```csharp
[SerializeField] private AudioSource audioSource;

private void PlayHitSound()
{
    audioSource.PlayOneShot(hitClip);
}
```

### Стало:
```csharp
// Удалить AudioSource из префаба мишени (или оставить для fallback)
// В коде:
private void PlayHitSound()
{
    if (AudioManager.Instance != null)
    {
        AudioManager.Instance.PlaySFX(hitClip, volume: 0.8f);
    }
}
```

## Настройки громкости

Настройки автоматически сохраняются в `PlayerPrefs`:
- `Audio_MasterVolume`
- `Audio_SFXVolume`
- `Audio_MusicVolume`

Значения загружаются при старте игры.

## Архитектура

### Два AudioSource:

1. **SFX Source** - для всех звуковых эффектов
   - Использует `PlayOneShot()` для одновременного воспроизведения нескольких звуков
   - 2D звук (spatialBlend = 0)
   - Громкость контролируется через SFX Volume и Master Volume

2. **Music Source** - для фоновой музыки
   - Один трек за раз
   - Зацикливание (loop = true)
   - 2D звук (spatialBlend = 0)
   - Громкость контролируется через Music Volume и Master Volume

## Преимущества упрощенной версии

- ✅ **Проще в использовании** - не нужно управлять пулом
- ✅ **Меньше кода** - легче поддерживать
- ✅ **Достаточно для 2D звуков** - все звуки в игре 2D
- ✅ **Раздельная настройка громкости** - SFX и Music отдельно
- ✅ **Автоматическое создание** - AudioSource создаются автоматически, если не назначены

## Ограничения

- ⚠️ **Одновременные звуки**: `PlayOneShot()` может воспроизводить несколько звуков одновременно, но если звуки очень длинные, они могут перекрываться
- ⚠️ **Нет 3D звука**: Все звуки 2D (но это не проблема, так как в игре не нужен 3D звук)

## Рекомендации

1. **Для всех звуков**: Используйте `PlaySFX()` или `PlayOneShot()`
2. **Для музыки**: Используйте `PlayMusic()` - автоматически зацикливается
3. **Поля AudioSource**: Можно оставить пустыми - создадутся автоматически
4. **Fallback**: Оставьте локальные AudioSource на объектах для fallback (если AudioManager не настроен)
