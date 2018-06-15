namespace PrepareLanding.Overlays
{
#if TAB_OVERLAYS
    public abstract class Overlay
    {
        protected GameData.GameData GameData;

        public abstract string Name { get; }

        protected Overlay(GameData.GameData gameData)
        {
            GameData = gameData;
        }
    }
#endif
}