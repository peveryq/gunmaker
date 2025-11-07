# Диагностика проблемы с кнопкой закрытия

## 🔍 Пошаговая диагностика

### Шаг 1: Проверка назначения в Inspector

1. **Найдите GameObject с PurchaseConfirmationUI:**
   - В Hierarchy: найдите объект с компонентом PurchaseConfirmationUI
   - Обычно это отдельный GameObject или PurchaseModal

2. **Выберите его и проверьте Inspector:**

```
PurchaseConfirmationUI (Script)
├─ Modal Panel: [PurchaseModal] ✅
├─ Overlay: [PurchaseOverlay] ✅
├─ Part Icon Image: [ModalPartIcon] ✅
├─ Stats Header Text: [StatsHeader] ✅
├─ Stats Container: [StatsContainer] ✅
├─ Cost Text: [CostText] ✅
├─ Buy Button: [BuyButton] ✅
├─ Close Button: [ModalCloseButton] ❓ ПРОВЕРЬТЕ!
└─ Offering Generator: [ShopOfferingGenerator] ✅
```

3. **Если Close Button пустой (None):**
   - Найдите в Hierarchy: PurchaseModal → ModalCloseButton
   - Перетащите его в поле Close Button
   - Сохраните сцену (Ctrl+S)

---

### Шаг 2: Проверка кнопки в Hierarchy

1. **Найдите кнопку закрытия:**
```
Hierarchy:
Canvas
└─ ShopPanel
    └─ PurchaseOverlay
        └─ PurchaseModal
            ├─ ModalHeader
            └─ ModalCloseButton ← ЭТО ОНА!
```

2. **Выберите ModalCloseButton и проверьте:**
   - ✅ GameObject активен (галочка включена)
   - ✅ Button component есть
   - ✅ Button → Interactable = TRUE
   - ✅ Button → Navigation = None (или оставьте как есть)

3. **Проверьте Image component:**
   - ✅ Raycast Target = TRUE (ВАЖНО!)
   - Если FALSE → кнопка не будет реагировать на клики!

---

### Шаг 3: Проверка Console при запуске

1. **Запустите игру (PlayMode)**
2. **Откройте магазин**
3. **Кликните на плитку** (откроется модальное окно)

**В Console должны появиться:**
```
CloseButton listener added to ModalCloseButton ← ВАЖНО!
```

**Если появляется:**
```
CloseButton is not assigned in PurchaseConfirmationUI!
```
**→ Вернитесь к Шагу 1 и назначьте кнопку!**

---

### Шаг 4: Проверка клика

1. **Модальное окно открыто**
2. **Наведите мышь на кнопку X**
   - Кнопка должна изменить цвет (hover effect)
   - Если не меняет → проблема с Raycast Target

3. **Кликните на кнопку X**

**В Console должно появиться:**
```
Close button clicked!
```

**Если появляется:**
- ✅ Окно закрывается → **ВСЕ РАБОТАЕТ!**

**Если НЕ появляется:**
- ❌ Listener не назначен → см. Шаг 5

---

### Шаг 5: Диагностика проблем с Raycast

**Проблема:** Кнопка не реагирует на клики вообще

**Причины:**

#### A) Raycast Target отключен
```
Решение:
1. Выберите ModalCloseButton
2. Найдите Image component
3. Включите "Raycast Target"
```

#### B) Что-то перекрывает кнопку
```
Решение:
1. Проверьте в Hierarchy порядок объектов
2. PurchaseModal должен быть ПОВЕРХ всего
3. В Hierarchy он должен быть НИЖЕ (дальше в списке)
```

#### C) Canvas Group блокирует
```
Решение:
1. Проверьте родительские объекты на Canvas Group
2. Canvas Group → Block Raycasts должен быть TRUE
3. Canvas Group → Interactable должен быть TRUE
```

#### D) EventSystem отсутствует
```
Решение:
1. Найдите в Hierarchy: EventSystem
2. Если нет → создайте: GameObject → UI → Event System
3. Убедитесь что EventSystem активен
```

---

### Шаг 6: Альтернативное решение (если ничего не помогло)

**Добавьте listener через Inspector:**

1. Выберите ModalCloseButton
2. В Button component найдите "On Click ()"
3. Нажмите "+"
4. Перетащите GameObject с PurchaseConfirmationUI
5. Выберите функцию: PurchaseConfirmationUI → HideModal()

Это обойдет проблему с кодом и назначит listener напрямую!

---

## 🧪 Тестовый скрипт для диагностики

Если ничего не помогает, добавьте временный скрипт на кнопку:

```csharp
using UnityEngine;
using UnityEngine.UI;

public class ButtonTester : MonoBehaviour
{
    void Start()
    {
        Button btn = GetComponent<Button>();
        if (btn != null)
        {
            Debug.Log($"Button found on {gameObject.name}");
            Debug.Log($"Button interactable: {btn.interactable}");
            
            btn.onClick.AddListener(() => {
                Debug.Log("BUTTON CLICKED!");
            });
        }
        
        Image img = GetComponent<Image>();
        if (img != null)
        {
            Debug.Log($"Image raycastTarget: {img.raycastTarget}");
        }
    }
}
```

Добавьте на ModalCloseButton и проверьте Console:
- Должно быть: "Button interactable: True"
- Должно быть: "Image raycastTarget: True"
- При клике: "BUTTON CLICKED!"

---

## 📊 Чеклист диагностики

- [ ] Close Button назначен в PurchaseConfirmationUI Inspector
- [ ] ModalCloseButton GameObject активен
- [ ] Button component есть и Interactable = True
- [ ] Image → Raycast Target = True
- [ ] В Console: "CloseButton listener added to ModalCloseButton"
- [ ] EventSystem существует в сцене
- [ ] При клике в Console: "Close button clicked!"
- [ ] Canvas и все родители активны
- [ ] PurchaseOverlay становится активным при открытии modal
- [ ] Нет Canvas Group блокирующих raycast

---

## 🎯 Типичные решения

### Решение 1: Raycast Target
```
ModalCloseButton → Image → Raycast Target = TRUE
```

### Решение 2: Button не назначена
```
GameObject с PurchaseConfirmationUI
→ Inspector → Close Button → [ModalCloseButton]
```

### Решение 3: EventSystem
```
Hierarchy → EventSystem (должен существовать)
```

### Решение 4: Listener через Inspector
```
ModalCloseButton → Button → On Click ()
→ + → PurchaseConfirmationUI → HideModal()
```

---

## 🔧 Финальный тест

После всех исправлений:

1. ✅ Запустите игру
2. ✅ Откройте магазин
3. ✅ Кликните на плитку
4. ✅ В Console: "CloseButton listener added"
5. ✅ Кликните на X
6. ✅ В Console: "Close button clicked!"
7. ✅ Окно закрывается

**Если все шаги выполнены → кнопка работает!**

---

## 📞 Если проблема осталась

Отправьте скриншоты:
1. PurchaseConfirmationUI в Inspector (со всеми полями)
2. ModalCloseButton в Inspector (Button и Image components)
3. Console после открытия modal окна
4. Hierarchy с раскрытой структурой ShopPanel → PurchaseOverlay → PurchaseModal

Это поможет точно определить проблему!

