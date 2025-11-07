# Shop UI Setup Guide

Complete step-by-step guide for building the shop UI hierarchy in Unity.

---

## Prerequisites

Before starting, ensure you have:
1. All shop scripts in `Assets/_InternalAssets/Scrips/`
2. TextMeshPro package installed (Window > Package Manager > TextMeshPro)
3. Main scene open (`Assets/_InternalAssets/Scenes/Main.unity`)

---

## Part 1: Create Shop UI Root

### 1.1 Create Shop Panel

1. In Hierarchy, find the existing **Canvas**
2. Right-click Canvas → UI → Panel
3. Rename to **"ShopPanel"**
4. In Inspector:
   - Anchor Preset: **Stretch-Stretch** (hold Alt+Shift and click bottom-right preset)
   - Left: 0, Right: 0, Top: 0, Bottom: 0
   - Color: Semi-transparent black `(0, 0, 0, 200)`

### 1.2 Add ShopUI Component

1. Select **ShopPanel**
2. Add Component → **ShopUI**
3. Leave references empty for now (we'll assign them later)

---

## Part 2: Left Area (Categories)

### 2.1 Create Categories Background

1. Right-click **ShopPanel** → UI → Image
2. Rename to **"CategoriesArea"**
3. Anchors:
   - Anchor Preset: **Top-Left Stretch** (left column, top row while holding Alt)
   - Pivot X: 0, Y: 1
   - Pos X: 0, Pos Y: 0
   - Width: 200
   - Height: 0 (stretches full height)
   - Color: Dark grey `(40, 40, 40, 255)`

### 2.2 Create Categories Header

1. Right-click **CategoriesArea** → UI → Image
2. Rename to **"CategoriesHeader"**
3. Anchors:
   - Anchor Preset: **Top-Left**
   - Pivot X: 0, Y: 1
   - Pos X: 0, Pos Y: 0
   - Width: 200, Height: 50
   - Color: Orange `(255, 140, 0, 255)`

4. Right-click **CategoriesHeader** → UI → Text - TextMeshPro
5. Rename to **"HeaderText"**
6. Settings:
   - Text: "CATEGORIES"
   - Font Size: 18
   - Alignment: Center + Middle
   - Color: White
   - Anchor: Stretch-Stretch (fill parent)

### 2.3 Create Category Buttons Container

1. Right-click **CategoriesArea** → Create Empty
2. Rename to **"CategoryButtons"**
3. Anchors:
   - Top-Left Stretch
   - Pos X: 0, Pos Y: -50 (below header)
   - Width: 200, Height: 0
4. Add Component → **Vertical Layout Group**
   - Child Alignment: Upper Left
   - Control Child Size: Width ✓
   - Child Force Expand: Width ✓
   - Spacing: 0

### 2.4 Create Category Buttons (Repeat for each)

Create 6 buttons: **Stocks**, **Barrels**, **Magazines**, **Scopes**, **Lasers**, **Foregrips**

**For each button:**

1. Right-click **CategoryButtons** → UI → Button - TextMeshPro
2. Rename to button name (e.g., **"StocksButton"**)
3. In Button component:
   - Navigation: None
4. In RectTransform:
   - Height: 50
5. In Image (button background):
   - Color: `(50, 50, 50, 255)` for normal
   - Color: `(35, 35, 35, 255)` for active/selected buttons
6. Delete the default Text child
7. Add children to the button:

**a) Text Label:**
- Right-click button → UI → Text - TextMeshPro
- Rename: "Label"
- Anchors: Stretch-Stretch with padding
  - Left: 15, Right: 40, Top: 0, Bottom: 0
- Text: button name (e.g., "Stocks")
- Font Size: 16
- Alignment: Middle-Left
- Color: White (Orange `255, 140, 0` for selected)

**b) Selected Indicator (Arrow):**
- Right-click button → UI → Image
- Rename: "SelectedIndicator"
- Anchors: Right-Middle
  - Pivot X: 1, Y: 0.5
  - Pos X: -10, Pos Y: 0
  - Width: 20, Height: 20
- Color: Orange `(255, 140, 0, 255)`
- Source Image: Triangle or arrow sprite
- Set Active: FALSE (only visible when selected)

**c) For Lasers and Foregrips - Add Lock Icon:**
- Right-click button → UI → Image
- Rename: "LockIcon"
- Anchors: Right-Middle
  - Pos X: -10, Width: 16, Height: 16
- Source Image: Lock icon sprite
- Add below lock icon → UI → Text - TextMeshPro
  - Text: "SOON"
  - Font Size: 10
  - Color: Grey `(150, 150, 150, 255)`

**d) Disable locked buttons:**
- Set Lasers and Foregrips Button component → Interactable: FALSE
- Set Label text color to grey `(150, 150, 150, 255)`

---

## Part 3: Top Area (Header Bar)

### 3.1 Create Header Bar

1. Right-click **ShopPanel** → UI → Image
2. Rename to **"HeaderBar"**
3. Anchors:
   - Top-Right Stretch
   - Pivot X: 1, Y: 1
   - Pos X: 0, Pos Y: 0
   - Left: 200 (to not overlap categories)
   - Height: 50
   - Color: Dark grey `(45, 45, 45, 255)`

### 3.2 Create Close Button

1. Right-click **HeaderBar** → UI → Button - TextMeshPro
2. Rename to **"CloseButton"**
3. Anchors:
   - Top-Right
   - Pos X: -10, Pos Y: -10
   - Width: 40, Height: 40
4. Button text: "X"
   - Font Size: 24
   - Alignment: Center
   - Color: White

### 3.3 Create Refresh Button

1. Right-click **HeaderBar** → UI → Button - TextMeshPro
2. Rename to **"RefreshButton"**
3. Anchors:
   - Top-Right
   - Pos X: -60, Pos Y: -10 (left of close button)
   - Width: 120, Height: 40
4. Button text: "REFRESH"
   - Font Size: 16
   - Alignment: Center

### 3.4 Create Money Display

1. Right-click **HeaderBar** → UI → Text - TextMeshPro
2. Rename to **"MoneyText"**
3. Anchors:
   - Top-Right
   - Pos X: -190, Pos Y: -10 (left of refresh)
   - Width: 150, Height: 40
4. Settings:
   - Text: "10000 $"
   - Font Size: 20
   - Alignment: Middle-Right
   - Color: Green for number part `(0, 255, 100, 255)`

---

## Part 4: Center Area (Item Grid)

### 4.1 Create Grid Background

1. Right-click **ShopPanel** → UI → Image
2. Rename to **"GridArea"**
3. Anchors:
   - Stretch-Stretch
   - Left: 200, Right: 0, Top: 50, Bottom: 0
   - Color: Darker grey `(30, 30, 30, 255)`

### 4.2 Create Scroll View

1. Right-click **GridArea** → UI → Scroll View
2. Rename to **"ItemScrollView"**
3. Anchors: Stretch-Stretch (fill parent)
   - Left: 20, Right: 20, Top: 20, Bottom: 20
4. In Scroll Rect component:
   - Horizontal: FALSE
   - Vertical: TRUE
   - Movement Type: Clamped
   - Scrollbar Visibility: Auto Hide

### 4.3 Setup Viewport and Content

1. Select **Viewport** (child of ScrollView):
   - Delete the Image component
   - Ensure it has Mask component

2. Select **Content** (child of Viewport):
   - Add Component → **Grid Layout Group**
     - Cell Size: X: 250, Y: 300
     - Spacing: X: 20, Y: 20
     - Start Corner: Upper Left
     - Start Axis: Horizontal
     - Child Alignment: Upper Center
     - Constraint: Fixed Column Count = 3
   - Add Component → **Content Size Fitter**
     - Vertical Fit: Preferred Size

3. Select **Scrollbar Vertical**:
   - Width: 15
   - Colors: Dark theme

---

## Part 5: Item Tile Prefab

### 5.1 Create Tile Base

1. In Project, navigate to `Assets/_InternalAssets/Prefabs/` (create folder if needed)
2. In Hierarchy, right-click **anywhere outside Canvas** → UI → Image
3. Rename to **"ShopItemTile"**
4. Settings:
   - Width: 250, Height: 300
   - Color: Medium grey `(60, 60, 60, 255)`

### 5.2 Add Button Component

1. Select **ShopItemTile**
2. Add Component → **Button**
3. Add Component → **ShopItemTile** (script)

### 5.3 Create Tile Top Section

1. Right-click **ShopItemTile** → UI → Image
2. Rename to **"TopSection"**
3. Anchors: Top-Stretch
   - Left: 0, Right: 0, Top: 0
   - Height: 230
   - Color: Slightly lighter `(65, 65, 65, 255)`

**a) Part Icon:**
- Right-click **TopSection** → UI → Image
- Rename: "PartIcon"
- Anchors: Center
- Width: 150, Height: 150
- Preserve Aspect: TRUE

**b) Manufacturer Logo:**
- Right-click **TopSection** → UI → Image
- Rename: "ManufacturerLogo"
- Anchors: Top-Right
- Pos X: -10, Pos Y: -10
- Width: 60, Height: 60
- Preserve Aspect: TRUE

**c) Stars Container:**
- Right-click **TopSection** → Create Empty
- Rename: "StarsContainer"
- Anchors: Bottom-Center
- Pos Y: 10
- Width: 150, Height: 30
- Add Component → **Horizontal Layout Group**
  - Child Alignment: Middle Center
  - Child Force Expand: Width ✓, Height ✓
  - Spacing: 5

**d) Create 5 Star Images:**
- Right-click **StarsContainer** → UI → Image (repeat 5 times)
- Rename: "Star1", "Star2", "Star3", "Star4", "Star5"
- Width: 24, Height: 24
- Preserve Aspect: TRUE

### 5.4 Create Tile Bottom Section (Price Bar)

1. Right-click **ShopItemTile** → UI → Image
2. Rename to **"BottomSection"**
3. Anchors: Bottom-Stretch
   - Left: 0, Right: 0, Bottom: 0
   - Height: 70
   - Color: Dark `(40, 40, 40, 255)`

4. Right-click **BottomSection** → UI → Text - TextMeshPro
5. Rename to **"PriceText"**
6. Anchors: Stretch-Stretch
7. Text: "123 $"
8. Font Size: 22
9. Alignment: Center-Middle
10. Color: White

### 5.5 Link References in ShopItemTile Script

Select **ShopItemTile**, in Inspector assign:
- Part Icon Image → **PartIcon**
- Manufacturer Logo Image → **ManufacturerLogo**
- Price Text → **PriceText**
- Stars Container → **StarsContainer**
- Star Images → Drag **Star1** through **Star5** (array of 5)
- Tile Button → **Button component on ShopItemTile**

### 5.6 Create Prefab

1. Drag **ShopItemTile** from Hierarchy to `Assets/_InternalAssets/Prefabs/`
2. Delete ShopItemTile from Hierarchy

---

## Part 6: Purchase Confirmation Modal

### 6.1 Create Overlay

1. Right-click **ShopPanel** → UI → Image
2. Rename to **"PurchaseOverlay"**
3. Anchors: Stretch-Stretch (full screen)
4. Color: Semi-transparent black `(0, 0, 0, 180)`
5. Raycast Target: TRUE (blocks clicks)

### 6.2 Create Modal Window

1. Right-click **PurchaseOverlay** → UI → Image
2. Rename to **"PurchaseModal"**
3. Anchors: Center
4. Pos X: 0, Pos Y: 0
5. Width: 600, Height: 500
6. Color: Dark grey `(50, 50, 50, 255)`

### 6.3 Modal Header

1. Right-click **PurchaseModal** → UI → Text - TextMeshPro
2. Rename to **"ModalHeader"**
3. Anchors: Top-Stretch
   - Left: 20, Right: 60, Top: -20
   - Height: 40
4. Text: "PURCHASE PART ?"
5. Font Size: 24
6. Alignment: Middle-Left

7. Right-click **PurchaseModal** → UI → Button - TextMeshPro
8. Rename to **"ModalCloseButton"**
9. Anchors: Top-Right
   - Pos X: -20, Pos Y: -20
   - Width: 40, Height: 40
10. Text: "X"

### 6.4 Part Icon Section

1. Right-click **PurchaseModal** → UI → Image
2. Rename to **"ModalPartIcon"**
3. Anchors: Left-Middle
   - Pos X: 150, Pos Y: 0
   - Width: 200, Height: 200
4. Preserve Aspect: TRUE

### 6.5 Stats Section

1. Right-click **PurchaseModal** → Create Empty
2. Rename to **"StatsContainer"**
3. Anchors: Right-Stretch
   - Left: 320, Right: -20, Top: -80, Bottom: 100
4. Add Component → **Vertical Layout Group**
   - Child Alignment: Upper Left
   - Spacing: 10

5. Right-click **StatsContainer** → UI → Text - TextMeshPro
6. Rename to **"StatsHeader"**
7. Text: "STATS"
8. Font Size: 20
9. Alignment: Top-Left

### 6.6 Create Stat Line Prefab

1. In Hierarchy (outside Canvas) → Create Empty
2. Rename to **"StatLine"**
3. Add Component → **RectTransform**
4. Width: 200, Height: 30
5. Right-click **StatLine** → UI → Text - TextMeshPro
6. Rename: "StatText"
7. Anchors: Stretch-Stretch
8. Text: "Accuracy: +50"
9. Font Size: 16
10. Alignment: Middle-Left
11. Drag **StatLine** to Prefabs folder
12. Delete from Hierarchy

### 6.7 Purchase Section

1. Right-click **PurchaseModal** → UI → Text - TextMeshPro
2. Rename to **"CostText"**
3. Anchors: Bottom-Center
   - Pos Y: 60
   - Width: 300, Height: 40
4. Text: "COST: 123 $"
5. Font Size: 20
6. Alignment: Center

7. Right-click **PurchaseModal** → UI → Button - TextMeshPro
8. Rename to **"BuyButton"**
9. Anchors: Bottom-Center
   - Pos Y: 20
   - Width: 200, Height: 50
10. Button color: Orange `(255, 140, 0, 255)`
11. Text: "BUY"
12. Font Size: 24

### 6.8 Add PurchaseConfirmationUI Component

1. Select **PurchaseModal** or create new GameObject for it
2. Add Component → **PurchaseConfirmationUI**
3. Assign references:
   - Modal Panel → **PurchaseModal**
   - Overlay → **PurchaseOverlay**
   - Part Icon Image → **ModalPartIcon**
   - Stats Header Text → **StatsHeader**
   - Stats Container → **StatsContainer**
   - Stat Line Prefab → **StatLine prefab**
   - Cost Text → **CostText**
   - Buy Button → **BuyButton**
   - Close Button → **ModalCloseButton**

### 6.9 Hide Modal Initially

1. Select **PurchaseOverlay**
2. In Inspector, uncheck the checkbox at top to disable it

---

## Part 7: Wire Up ShopUI Component

### 7.1 Assign All References

Select **ShopPanel**, in **ShopUI** component assign:

**Main Panel:**
- Shop Panel → **ShopPanel**

**Category Buttons:**
- Stocks Button → **StocksButton**
- Barrels Button → **BarrelsButton**
- Magazines Button → **MagazinesButton**
- Scopes Button → **ScopesButton**
- Lasers Button → **LasersButton**
- Foregrips Button → **ForegripsButton**

**Category Visuals:**
- Stocks Selected Indicator → **StocksButton/SelectedIndicator**
- Barrels Selected Indicator → **BarrelsButton/SelectedIndicator**
- Magazines Selected Indicator → **MagazinesButton/SelectedIndicator**
- Scopes Selected Indicator → **ScopesButton/SelectedIndicator**

**Header:**
- Money Text → **MoneyText**
- Refresh Button → **RefreshButton**
- Close Button → **CloseButton**

**Grid:**
- Item Grid Container → **Content** (inside ScrollView)
- Item Tile Prefab → **ShopItemTile prefab**

**References:**
- Purchase Confirmation UI → **PurchaseConfirmationUI component**

### 7.2 Leave ShopPanel ENABLED

⚠️ **IMPORTANT:** Do NOT disable ShopPanel!

**Why?**
- ShopUI.Awake() needs to run for initialization
- The code will hide the panel automatically: `shopPanel.SetActive(false)`
- If you disable it in Inspector, Awake() won't run and the shop won't work

**ShopPanel should be ENABLED (checkbox ON) in Inspector!**

---

## Part 8: Create Supporting GameObjects

### 8.1 Create MoneySystem GameObject

1. In Hierarchy, right-click → Create Empty
2. Rename to **"MoneySystem"**
3. Add Component → **MoneySystem**
4. Set Starting Money: 10000

### 8.2 Create ShopOfferingGenerator GameObject

1. In Hierarchy, right-click → Create Empty
2. Rename to **"ShopOfferingGenerator"**
3. Add Component → **ShopOfferingGenerator**

### 8.3 Create ShopPartConfig ScriptableObject

1. In Project, navigate to `Assets/_InternalAssets/Config/` (create if needed)
2. Right-click → Create → Gunmaker → Shop Part Config
3. Rename to **"ShopPartConfig"**
4. Configure each part type (Barrels, Magazines, Stocks, Scopes):
   - Add 5 RarityTiers (1-5 stars)
   - For each tier: set rarity, price ranges, stat ranges, ammo ranges
   - Add part prefabs for each tier
   - Add stat influences

**Example Configuration for Barrels:**

**Tier 1 (1 star):**
- Rarity: 1
- Price: 20-100
- Stat Range: 0-19
- Part Prefabs: [Add barrel prefabs for tier 1]
- Stat Influences: Power, Accuracy

**Tier 2 (2 stars):**
- Rarity: 2
- Price: 101-500
- Stat Range: 20-39
- etc.

**Continue for all tiers and part types...**

5. Add manufacturer logos to the list
6. Add star icons (filled and empty)

### 8.4 Assign Config to Generator

1. Select **ShopOfferingGenerator**
2. Drag **ShopPartConfig** to Config field

### 8.5 Create PartSpawner GameObject

1. In Hierarchy, right-click → Create Empty
2. Rename to **"PartSpawner"**
3. Add Component → **PartSpawner**
4. Create child → Create Empty, rename to **"SpawnPoint"**
5. Position SpawnPoint where you want parts to spawn
6. Drag **SpawnPoint** to Part Spawner's Spawn Point field

### 8.6 Assign Generator to Purchase UI

1. Select the GameObject with **PurchaseConfirmationUI**
2. Drag **ShopOfferingGenerator** to Offering Generator field

---

## Part 9: Final Touches

### 9.1 Add ShopComputer to Scene

1. Create or find shop computer object in scene
2. Add/ensure it has **ShopComputer** component
3. Drag **ShopPanel** to Shop UI field
4. Set interaction range

### 9.2 Test Configuration

1. Play the scene
2. Interact with shop computer
3. Verify:
   - Shop opens, cursor unlocks
   - Categories switch
   - Items display with prices and stars
   - Clicking items shows purchase modal
   - Stats calculate correctly
   - Purchasing works and spawns parts
   - Money deducts properly

---

## Quick Reference: Anchor Presets

- **Stretch-Stretch**: Hold Alt+Shift, click bottom-right preset
- **Top-Left Stretch**: Hold Alt, click top-left preset
- **Top-Right Stretch**: Hold Alt, click top-right preset
- **Bottom-Stretch**: Hold Alt, click bottom-center preset
- **Center**: Click center preset

---

## Troubleshooting

**Shop doesn't open:**
- Ensure ShopComputer has ShopUI reference
- Check ShopPanel starts disabled
- Verify Canvas has EventSystem

**Items don't display:**
- Check ShopPartConfig is assigned to ShopOfferingGenerator
- Ensure part prefabs are assigned in config
- Verify Item Tile Prefab is assigned in ShopUI

**Purchase doesn't work:**
- Ensure MoneySystem exists in scene
- Verify PartSpawner has spawn point assigned
- Check PurchaseConfirmationUI has all references

**Stats don't calculate:**
- Verify stat influences are set in ShopPartConfig
- Check rarity tier ranges are correct
- Ensure ShopOfferingGenerator has config reference

---

## Complete! 

Your shop system is now fully set up and ready to use.

