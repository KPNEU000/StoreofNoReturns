using UnityEngine;

/// <summary>
/// Centralized names and posting for the shop's Wwise SFX events.
/// Keeping the audio calls here means the gameplay scripts only signal the
/// moments at which a sound should play; they do not own audio assets.
/// </summary>
public static class StoreofNoReturnsSfx
{
    private const string MainSoundBank = "StoreofNoReturns_Main.bnk";
    private static bool mainSoundBankRequested;
    private static bool mainSoundBankMissing;
    public const string CustomerExcited = "PLAY_CUSTOMER_EXCITED";
    public const string CustomerDisappointed = "PLAY_CUSTOMER_DISSAPOINTED";
    public const string CustomerArrive = "PLAY_CUSTOMER_ARRIVE";
    public const string CustomerEntryDoor = "PLAY_CUSTOMER_ENTRY_DOOR";
    public const string CustomerEntryChime = "PLAY_CUSTOMER_ENTRY_CHIME";
    public const string ReputationDown = "PLAY_REPUTATION_DOWN";
    public const string ReputationUp = "PLAY_REPUTATION_UP";
    public const string AccusationWrong = "PLAY_ACCUSATION_WRONG";
    public const string AccusationCorrect = "PLAY_ACCUSATION_CORRECT";
    public const string UiConfirm = "PLAY_UI_CONFIRM";
    public const string UiDeny = "PLAY_UI_DENY";
    public const string UiChoice = "PLAY_UI_CHOICE";
    public const string UiBack = "PLAY_UI_BACK";
    public const string UiAdvance = "PLAY_UI_ADVANCE";
    public const string UiOpenDialogue = "PLAY_UI_OPEN_DIALOGUE";
    public const string FlashlightOff = "PLAY_FLASHLIGHT_OFF";
    public const string FlashlightOn = "PLAY_FLASHLIGHT_ON";
    public const string PlayerFootstep = "PLAY_PLAYER_FOOTSTEP";
    public const string CustomerFootstep = PlayerFootstep;
    public const string ShopMusic = "PLAY_SHOP_MUSIC";
    public const string ComputerInteract = "PLAY_COMPUTER_INTERACT";
    public const string GameOver = "PLAY_GAME_OVER";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void PreloadMainSoundBank()
    {
        EnsureMainSoundBankLoaded();
    }

    public static void Post(string eventName, GameObject emitter)
    {
        if (emitter != null)
        {
            EnsureMainSoundBankLoaded();
            if (mainSoundBankMissing) return;
            AkUnitySoundEngine.PostEvent(eventName, emitter);
        }
    }

    private static void EnsureMainSoundBankLoaded()
    {
        if (mainSoundBankRequested || !AkUnitySoundEngine.IsInitialized())
        {
            return;
        }

        mainSoundBankRequested = true;
        // The generated soundbanks aren't in the repo yet (2026-10-23); without this check every run logged
        // "Bank StoreofNoReturns_Main.bnk failed to load (AK_FileNotFound)". Commit the banks to bring sound back.
        string basePath = AkBasePathGetter.Get().SoundBankBasePath;
        if (string.IsNullOrEmpty(basePath) || !System.IO.File.Exists(System.IO.Path.Combine(basePath, MainSoundBank)))
        {
            mainSoundBankMissing = true;
            return;
        }
        AkBankManager.LoadBank(MainSoundBank, false, false);
    }
}
