/// <summary>
/// Enum for tutorial quest types
/// </summary>
public enum TutorialQuest
{
    None = -1,
    CreateGun = 0,          // Quest 1: Create a new gun at the workbench
    BuyBarrel = 1,         // Quest 2: Buy a barrel at the computer
    TakeBarrel = 2,        // Quest 3: Take the barrel
    AttachBarrel = 3,      // Quest 4: Attach the barrel to the gun
    TakeBlowtorch = 4,     // Quest 5: Take blowtorch
    WeldBarrel = 5,        // Quest 6: Weld the barrel with blowtorch
    BuyMag = 6,           // Quest 7: Buy a mag at the computer
    TakeMag = 7,          // Quest 8: Take the mag
    AttachMag = 8,        // Quest 9: Attach the mag to the gun
    TakeGun = 9,          // Quest 10: Take the gun from workbench
    ShootTargets = 10,    // Quest 11: Shoot some targets
    EnterRange = 11,      // Quest 12: Go to the door and enter shooting range
    Completed = 12        // All quests completed
}

