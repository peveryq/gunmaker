# Location Transition System Setup Guide

This guide provides step-by-step instructions for setting up the location transition system in Unity.

## Overview

The location transition system allows players to move between the workshop and testing range locations. It includes:
- Location selection UI
- Loading screen with progress bar
- Fade screen system
- Testing range with countdown and shooting timer
- Results screen with earnings display
- Weapon preservation system

---

## 1. Location Manager Setup

### 1.1 Create Location Manager GameObject

1. In the scene hierarchy, create an empty GameObject named `LocationManager`
2. Add the `LocationManager` component to it
3. Configure the following fields:

**Location Roots:**
- `Workshop Root`: Drag the root GameObject of the workshop location (contains all workshop objects)
- `Testing Range Root`: Drag the root GameObject of the testing range location (contains all testing range objects)

**References:**
- `Loading Screen`: Will be assigned after creating LoadingScreen (see section 3)
- `Fade Screen`: Will be assigned after creating FadeScreen (see section 2)
- `Interaction Handler`: Drag the player's InteractionHandler component
- `First Person Controller`: Drag the player's FirstPersonController component
- `Workshop Spawn Point`: Create an empty GameObject at the workshop spawn position, drag it here
- `Testing Range Spawn Point`: Create an empty GameObject at the testing range spawn position, drag it here

**Fade Settings:**
- `Fade In Speed`: Duration for fade in animation (default: 0.5)
- `Fade Out Speed`: Duration for fade out animation (default: 0.5)

### 1.2 Location Root Setup

**Workshop Root:**
- Should contain all workshop-related GameObjects (shop, workbench, targets, etc.)
- Initially active in the scene
- Will be deactivated when transitioning to testing range

**Testing Range Root:**
- Should contain all testing range-related GameObjects (targets, platform, door, etc.)
- Initially inactive in the scene
- Will be activated when transitioning from workshop

---

## 2. Fade Screen Setup

### 2.1 Create Fade Screen UI

1. In the Canvas hierarchy, create a new Image GameObject named `FadeScreen`
2. Set it as a child of the Canvas (or a dedicated UI layer)
3. Configure the Image:
   - **RectTransform**: Stretch to fill entire screen (Anchor: stretch-stretch, Left/Right/Top/Bottom: 0)
   - **Color**: Black (or desired fade color) with Alpha: 0
   - **Raycast Target**: Enabled (to block input during fade)

4. Add the `FadeScreen` component to the GameObject
5. Assign the Image component to the `Fade Image` field

### 2.2 Connect to Location Manager

- In LocationManager, assign the FadeScreen GameObject to the `Fade Screen` field

---

## 3. Loading Screen Setup

### 3.1 Create Loading Screen UI

1. In the Canvas hierarchy, create a new GameObject named `LoadingScreen`
2. Add the `LoadingScreen` component to it

**Background:**
- Create a child Image named `Background`
- Set RectTransform to fill screen (Anchor: stretch-stretch, Left/Right/Top/Bottom: 0)
- Set Color to desired background color (e.g., black) with Alpha: 1
- Assign to `Background Image` field in LoadingScreen component

**Progress Bar:**
- Create a child GameObject named `ProgressBarContainer`
- Inside it, create two Image children:
  - `ProgressBarBackground`: Full bar background
  - `ProgressBarFill`: The filling bar
- Configure ProgressBarFill:
  - Image Type: `Filled`
  - Fill Method: `Horizontal`
  - Fill Amount: 0
- Assign both to `Progress Bar Background` and `Progress Bar Fill` fields

**Loading Icon:**
- Create a child GameObject with an Image or SpriteRenderer for the loading icon
- Assign to `Loading Icon` field

**Settings:**
- `Fake Minimum Wait Time`: Minimum time to show loading (e.g., 2 seconds)
- `Required Objects`: Array of GameObjects that must be active for loading to complete (optional)

### 3.2 Connect to Location Manager

- In LocationManager, assign the LoadingScreen GameObject to the `Loading Screen` field

---

## 4. Location Selection UI Setup

### 4.1 Create UI Structure

1. In the Canvas hierarchy, create a GameObject named `LocationSelectionUI`
2. Add the `LocationSelectionUI` component to it

**Main Panel:**
- Create a child GameObject named `LocationPanel`
- Add Image component for background (optional)
- Assign to `Location Panel` field

**Left Sidebar (Decorative):**
- Create a child GameObject named `LeftSidebar`
- Add header text and location preview images (decorative, non-clickable for now)
- Position on the left side of screen

**Top Bar:**
- Create a child GameObject named `TopBar`
- Add an exit button in the top-right corner
- Assign to `Exit Button` field
- Position from left sidebar to right edge

**Main Area:**
- Create a child GameObject named `MainArea`
- Add the following children:
  - `LocationNameText`: TextMeshProUGUI for location name
  - `LocationDescriptionText`: TextMeshProUGUI for location description
  - `StartButton`: Button for starting location
  - `GrabGunFirstNotification`: GameObject with text "grab a gun first"
- Assign all to respective fields in LocationSelectionUI component

### 4.2 Configure References

- `Location Manager`: Assign LocationManager GameObject
- `Loading Screen`: Assign LoadingScreen GameObject
- `Interaction Handler`: Assign player's InteractionHandler
- `First Person Controller`: Assign player's FirstPersonController
- `Button Click Sound`: Assign audio clip for button clicks (optional)

---

## 5. Location Door Setup

### 5.1 Configure LocationDoor

1. Find or create the door GameObject in the workshop
2. Ensure it has the `LocationDoor` component
3. Configure:
   - `Interaction Range`: Distance for interaction (default: 3)
   - `Door Name`: Name displayed (e.g., "Training Range")
   - `Unlocked Label`: Label for interaction button (default: "to testing")
4. Assign `Location Selection UI` to the `Location Selection UI` field

---

## 6. Testing Range Controller Setup

### 6.1 Create Testing Range Controller

1. In the Testing Range root GameObject, create an empty child named `TestingRangeController`
2. Add the `TestingRangeController` component to it

**Countdown:**
- Create a TextMeshProUGUI GameObject for countdown display
- Position in center of screen
- Assign to `Countdown Text` field
- Configure `Countdown Interval`: Time between countdown numbers (default: 1 second)
- Configure `Shoot Text`: Text to show after countdown (default: "shoot!")

**Shooting Timer:**
- Create a TextMeshProUGUI GameObject for timer display
- Position where timer should appear (e.g., top-center)
- Assign to `Shooting Timer Text` field
- Configure `Shooting Duration`: Total shooting time in seconds (default: 60)

**Door Animations:**
- Find the door GameObject on the platform
- Add two `DOTweenAnimation` components:
  - One for opening (e.g., move/rotate door open)
  - One for closing (e.g., move/rotate door closed)
- Configure both animations:
  - `Auto Play`: false
  - `Auto Generate`: true (or manually create tweens)
- Assign to `Door Open Animation` and `Door Close Animation` fields
- Assign audio clips to `Door Open Sound` and `Door Close Sound` fields

**References:**
- `Location Manager`: Assign LocationManager GameObject
- `Results Screen`: Will be assigned after creating ResultsScreen (see section 7)
- `Fade Screen`: Assign FadeScreen GameObject
- `First Person Controller`: Assign player's FirstPersonController

**Fade Settings:**
- `Fade Out Speed`: Duration for fade out when timer ends (default: 0.5)

### 6.2 Door Animation Configuration

**Door Open Animation:**
1. Select the door GameObject
2. Add `DOTweenAnimation` component
3. Configure:
   - `Animation Type`: Move, Rotate, or Scale (depending on door design)
   - `Target Type`: Transform
   - `End Value`: Target position/rotation/scale when open
   - `Duration`: Animation duration
   - `Ease Type`: Desired easing (e.g., OutQuad)
   - `Auto Play`: false
   - `Auto Generate`: true

**Door Close Animation:**
1. Add second `DOTweenAnimation` component (or use separate GameObject)
2. Configure similarly but with closed position/rotation/scale
3. Use different easing if desired (e.g., InQuad for closing)

---

## 7. Results Screen UI Setup

### 7.1 Create Results Screen UI

1. In the Canvas hierarchy, create a GameObject named `ResultsScreenUI`
2. Add the `ResultsScreenUI` component to it

**Main Panel:**
- Create a child GameObject named `ResultsPanel`
- Add Image component for background
- Assign to `Results Panel` field

**Header:**
- Create child GameObject named `TimeIsUpHeader`
- Add TextMeshProUGUI with text "time is up"
- Create child GameObject named `TimeIsUpBackground`
- Add Image component for header background
- Assign both to respective fields

**Earnings Display:**
- Create a container GameObject for earnings
- Inside it, create two separate TextMeshProUGUI children:
  - `DollarSignText`: Text "$" (configure color and size)
  - `EarningsAmountText`: Text for amount (configure different color and size)
- Assign both to respective fields
- Also assign a label TextMeshProUGUI to `Earnings Label` (optional, for "earnings" text)

**Buttons:**
- Create two Button GameObjects:
  - `NextButton`: Button with text "next"
  - `GetX2Button`: Button with text "get x2"
- Assign both to respective fields

**References:**
- `Location Manager`: Assign LocationManager GameObject
- `Fade Screen`: Assign FadeScreen GameObject
- `First Person Controller`: Assign player's FirstPersonController
- `Button Click Sound`: Assign audio clip (optional)

**Fade Settings:**
- `Fade Out Speed`: Duration for fade out (default: 0.5)

### 7.2 Connect to Testing Range Controller

- In TestingRangeController, assign ResultsScreenUI GameObject to the `Results Screen` field

---

## 8. Earnings Tracker Setup

The EarningsTracker component is automatically added to LocationManager. No manual setup required.

---

## 9. Testing Checklist

### 9.1 Basic Functionality

- [ ] Door interaction shows "to testing" button
- [ ] Location selection UI opens when interacting with door
- [ ] "Start" button is disabled when player has no weapon
- [ ] "Grab a gun first" notification shows when no weapon
- [ ] Loading screen appears when clicking "start"
- [ ] Progress bar fills during loading
- [ ] Fade in occurs after loading completes
- [ ] Player spawns at testing range spawn point
- [ ] Countdown sequence plays (5-4-3-2-1-shoot!)
- [ ] Door opens after countdown
- [ ] Shooting timer starts and counts down
- [ ] Earnings are tracked during shooting
- [ ] Door closes when timer ends
- [ ] Results screen shows correct earnings
- [ ] "Next" button returns to workshop
- [ ] "Get x2" button adds earnings again and returns to workshop
- [ ] Weapon is restored to player's hands when returning to workshop (even if dropped)

### 9.2 Weapon Preservation Test

1. Enter testing range with weapon
2. Drop weapon on testing range (press G)
3. Complete shooting session
4. Return to workshop
5. Verify weapon is back in player's hands

### 9.3 Edge Cases

- [ ] Test with no weapon (should not allow starting)
- [ ] Test dropping weapon during countdown
- [ ] Test dropping weapon during shooting
- [ ] Test rapid location transitions
- [ ] Test ESC key closes all UIs

---

## 10. Common Issues and Troubleshooting

### Issue: Loading screen doesn't complete

**Solution:**
- Check that all `Required Objects` in LoadingScreen are active
- Increase `Fake Minimum Wait Time` if loading is too fast
- Verify LocationManager is properly configured

### Issue: Weapon not restored after transition

**Solution:**
- Verify InteractionHandler is assigned in LocationManager
- Check that weapon has WeaponBody or WeaponController component
- Ensure weapon GameObject is not destroyed (should be in scene)

### Issue: Door doesn't animate

**Solution:**
- Verify DOTweenAnimation components are configured
- Check that `Auto Generate` is true or manually call `CreateTween()`
- Ensure animations are assigned in TestingRangeController
- Check that door GameObject is active

### Issue: Countdown/timer not showing

**Solution:**
- Verify TextMeshProUGUI components are assigned
- Check that GameObjects are active
- Ensure TestingRangeController is enabled
- Verify OnEnable is called (component must be on active GameObject)

### Issue: Earnings not tracked

**Solution:**
- Verify MoneySystem.Instance exists
- Check that EarningsTracker.StartTracking() is called
- Ensure EarningsTracker.StopTracking() is called when timer ends
- Verify MoneySystem.OnMoneyChanged event is firing

### Issue: Fade screen not working

**Solution:**
- Verify Image component is assigned in FadeScreen
- Check that Image RectTransform fills screen
- Ensure Image color alpha can change (not locked)
- Verify DOTween is imported and working

---

## 11. Performance Considerations

- Loading screen fake minimum wait time prevents jarring instant transitions
- Weapon state is saved as reference, not deep copy (efficient)
- Location roots are enabled/disabled, not destroyed/recreated
- Fade animations use DOTween for efficient interpolation
- Progress bar uses Image.fillAmount (GPU-friendly)

---

## 12. Future Enhancements

- Add more locations (extend LocationType enum)
- Implement ad system for "get x2" button
- Add location unlock system
- Add location-specific settings (different timer durations, etc.)
- Add location preview images in selection UI
- Add location-specific music/ambience

---

## Notes

- All UI components should be children of a Canvas
- Ensure Canvas has proper sorting order (above gameplay, below other fullscreen UIs if needed)
- Audio clips should be assigned for better UX
- Test on target platform (WebGL) for performance
- Consider adding loading screen tips/hints during wait time

---

_Last updated: November 2025_

