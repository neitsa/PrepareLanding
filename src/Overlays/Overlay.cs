namespace PrepareLanding.Overlays
{
    public abstract class Overlay
    {
        protected GameData.GameData GameData;

        public abstract string Name { get; }

        protected Overlay(GameData.GameData gameData)
        {
            GameData = gameData;
        }
    }
}