using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace PrepareLanding.GameData
{
    public class RainfallData : WorldFeatureData
    {
        public RainfallData(DefData defData) : base(defData)
        {
        }

        public override MostLeastFeature Feature => MostLeastFeature.Rainfall;
        public override string FeatureMeasureUnit => "mm";

        public Texture2D RainfallGradientTexure => FeatureGradientTexure;

        public Dictionary<BiomeDef, Dictionary<int, float>> RainFallsByBiomes => FeatureByBiomes;

        public List<KeyValuePair<int, float>> WorldTilesRainfalls => WorldTilesFeatures;

        protected override float TileFeatureValue(int tileId)
        {
            return Find.World.grid[tileId].rainfall;
        }
    }
}