using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace PrepareLanding.GameData
{
    public class RainfallData : WorldCharacteristicData
    {
        public RainfallData(DefData defData) : base(defData)
        {
        }

        public override MostLeastCharacteristic Characteristic => MostLeastCharacteristic.Rainfall;
        public override string CharacteristicMeasureUnit => "mm";

        public Texture2D RainfallGradientTexure => CharacteristicGradientTexture;

        public Dictionary<BiomeDef, Dictionary<int, float>> RainFallsByBiomes => CharacteristicByBiomes;

        public List<KeyValuePair<int, float>> WorldTilesRainfalls => WorldTilesCharacteristics;

        protected override float TileCharacteristicValue(int tileId)
        {
            return Find.World.grid[tileId].rainfall;
        }
    }
}