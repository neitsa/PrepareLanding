using Verse;

namespace PrepareLanding.GameData
{
    public class ElevationData : WorldFeatureData
    {
        public ElevationData(DefData defData) : base(defData)
        {
        }

        public override MostLeastFeature Feature => MostLeastFeature.Elevation;

        public override string FeatureMeasureUnit => "m";

        protected override float TileFeatureValue(int tileId)
        {
            return Find.World.grid[tileId].elevation;
        }
    }
}
