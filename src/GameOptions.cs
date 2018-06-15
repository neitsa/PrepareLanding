using HugsLib.Settings;
using Verse;

namespace PrepareLanding
{
    public class GameOptions
    {
        private readonly ModSettingsPack _settingsPack;

        public GameOptions(ModSettingsPack settingsPack, RimWorldEventHandler eventHandler)
        {
            _settingsPack = settingsPack;

            eventHandler.DefsLoaded += OnDefLoaded;
        }

        private void OnDefLoaded()
        {
            Log.Message("[PrepareLanding] GameOptions.OnDefLoaded().");

            DisableWorldData = _settingsPack.GetHandle("DisableWorldData", "PLGOPT_DisableWorldDataTitle".Translate(),
                "PLGOPT_DisableWorldDataDescription".Translate(), false);
        }

        public SettingHandle<bool> DisableWorldData { get; private set; }
    }
}