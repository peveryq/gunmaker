# Руководство по миграции на AudioManager

## Что было сделано

✅ **Автоматически мигрированы:**
- `ShootingTarget` - звуки попаданий и падения мишени
- `WeaponController` - звуки выстрела, перезарядки, пустого магазина

## Поле Music Source в AudioManager

**Можно оставить пустым!** AudioManager автоматически создаст AudioSource для музыки при старте, если поле не назначено.

**Или назначить свой AudioSource**, если нужны особые настройки (например, специальный эффект реверберации).

## Остальные системы для миграции

### Приоритет 1 (важные для геймплея):

1. **WeaponLockerSystem** - звуки открытия/закрытия, складывания/извлечения
2. **Workbench** - звук установки детали
3. **ItemPickup** - звуки подбора и удара

### Приоритет 2 (UI звуки):

4. **ShopUI** - звуки кликов кнопок
5. **PurchaseConfirmationUI** - звуки покупки и кликов
6. **WeaponSellModal** - звук клика
7. **GunNameModal** - звук клика
8. **WeaponSlotSelectionUI** - звук клика

### Приоритет 3 (опциональные):

9. **FirstPersonController** - звуки шагов (можно оставить локальный AudioSource)
10. **Blowtorch** - звук горелки (можно оставить локальный AudioSource)
11. **PartSpawner** - звук спавна детали

## Паттерн миграции

### Было:
```csharp
[SerializeField] private AudioSource audioSource;

private void PlaySound(AudioClip clip)
{
    if (clip != null && audioSource != null)
    {
        audioSource.PlayOneShot(clip);
    }
}
```

### Стало:
```csharp
// Поле audioSource можно оставить для fallback, но пометить как опциональное
[Tooltip("Optional local AudioSource for fallback (if AudioManager not available). Can be left empty.")]
[SerializeField] private AudioSource audioSource; // Fallback only

private void PlaySound(AudioClip clip)
{
    if (clip == null) return;
    
    // Use AudioManager if available, otherwise fallback to local AudioSource
    if (AudioManager.Instance != null)
    {
        AudioManager.Instance.PlaySFX2D(clip, volume: 1f); // Для UI звуков
        // или
        AudioManager.Instance.PlaySFX(clip, transform.position, volume: 1f); // Для 3D звуков
    }
    else if (audioSource != null)
    {
        audioSource.PlayOneShot(clip);
    }
}
```

## Типы звуков

### 2D звуки (UI, оружие):
```csharp
AudioManager.Instance.PlaySFX2D(clip, volume: 1f);
```

### 3D звуки (мишени, предметы):
```csharp
AudioManager.Instance.PlaySFX(clip, transform.position, volume: 0.8f);
```

## Что делать с AudioSource на объектах

**Вариант 1 (рекомендуется):** Оставить для fallback, но пометить как опциональное
- Система будет работать даже если AudioManager не настроен
- Плавная миграция без поломок

**Вариант 2:** Удалить полностью
- Только если уверены, что AudioManager всегда будет на сцене
- Более чистая структура

## Проверка миграции

После миграции проверьте:
1. ✅ Звуки проигрываются через AudioManager
2. ✅ Настройки громкости работают (Master, SFX, Music)
3. ✅ Fallback работает, если AudioManager не настроен

## Примеры для разных систем

### UI звуки (2D):
```csharp
if (AudioManager.Instance != null)
{
    AudioManager.Instance.PlaySFX2D(clickSound, volume: 0.8f);
}
```

### 3D звуки (предметы, мишени):
```csharp
if (AudioManager.Instance != null)
{
    AudioManager.Instance.PlaySFX(pickupSound, transform.position, volume: 0.7f);
}
```

### Музыка:
```csharp
if (AudioManager.Instance != null)
{
    AudioManager.Instance.PlayMusic(backgroundMusic, volume: 0.6f, loop: true);
}
```

