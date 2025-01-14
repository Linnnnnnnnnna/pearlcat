using System.Linq;

namespace Pearlcat;

public static class ModCompat_Helpers
{
    public static void InitModCompat()
    {
        if (IsModEnabled_ImprovedInputConfig)
        {
            IICCompat.InitCompat();
        }

        if (IsModEnabled_ChasingWind)
        {
            CWCompat.InitCompat();
        }

        if (IsModEnabled_RainMeadow)
        {
            MeadowCompat.InitCompat();
        }
    }


    // Warp
    public static bool IsWarpAllowed(this RainWorldGame game)
    {
        return game.IsStorySession && (!ModManager.MSC || !game.rainWorld.safariMode);
    }


    // Mira Installation
    public static bool IsModEnabled_MiraInstallation => ModManager.ActiveMods.Any(x => x.id == "mira");
    public static bool ShowMiraVersionWarning => IsModEnabled_MiraInstallation; // TODO


    // Chasing Wind
    public static bool IsModEnabled_ChasingWind => ModManager.ActiveMods.Any(x => x.id == "myr.chasing_wind");


    // Improved Input Config
    public static bool IsModEnabled_ImprovedInputConfig => ModManager.ActiveMods.Any(x => x.id == "improved-input-config");
    public static bool IsIICActive => IsModEnabled_ImprovedInputConfig && !ModOptions.DisableImprovedInputConfig.Value;


    // Rain Meadow
    public static bool IsModEnabled_RainMeadow => ModManager.ActiveMods.Any(x => x.id == "henpemaz_rainmeadow");

    public static bool RainMeadow_IsLobbyOwner => !IsModEnabled_RainMeadow || MeadowCompat.IsLobbyOwner;
    public static bool RainMeadow_IsOnline => IsModEnabled_RainMeadow && MeadowCompat.IsOnline;
    public static bool RainMeadow_FriendlyFire => IsModEnabled_RainMeadow && MeadowCompat.FriendlyFire;

    public static bool RainMeadow_IsMine(AbstractPhysicalObject obj)
    {
        return !IsModEnabled_RainMeadow || MeadowCompat.IsLocal(obj);
    }
}
