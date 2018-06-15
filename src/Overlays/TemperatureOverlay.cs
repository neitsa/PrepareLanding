namespace PrepareLanding.Overlays
{
#if TAB_OVERLAYS
    public class TemperatureOverlay : Overlay
    {
        public override string Name => "Temperature";

        public TemperatureOverlay(GameData.GameData gameData) : base(gameData)
        {
            
        }
    }
#endif
}