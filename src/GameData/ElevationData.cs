using Verse;

namespace PrepareLanding.GameData
{
    public class ElevationData : WorldCharacteristicData
    {
        public ElevationData(DefData defData) : base(defData)
        {
        }

        public override MostLeastCharacteristic Characteristic => MostLeastCharacteristic.Elevation;

        public override string CharacteristicMeasureUnit => "m";

        protected override float TileCharacteristicValue(int tileId)
        {
            return Find.World.grid[tileId].elevation;
        }
    }
}
