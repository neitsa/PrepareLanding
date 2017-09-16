using System.Collections.Generic;
using System.Linq;
using PrepareLanding.Core;
using PrepareLanding.Core.Extensions;
using PrepareLanding.Filters;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace PrepareLanding.GameData
{
    public abstract class WorldFeatureData
    {
        public const int DefaultNumberOfColorSamples = 100;
        private readonly Gradient _colorGradient;

        protected readonly Material _defaultMaterial = WorldMaterials.SelectedTile;

        public readonly Dictionary<BiomeDef, List<Color>> ColorSamplesByBiomes =
            new Dictionary<BiomeDef, List<Color>>();

        protected readonly DefData DefData;

        public readonly Dictionary<BiomeDef, List<Material>> MaterialSamplesByBiomes =
            new Dictionary<BiomeDef, List<Material>>();

        private Texture2D _featureGradientTexture;

        protected WorldFeatureData(DefData defData)
        {
            DefData = defData;

            PrepareLanding.Instance.EventHandler.WorldGeneratedOrLoaded += ExecuteOnWorldGeneratedOrLoaded;

            // Cocorico!
            GradientColors = new List<Color> {Color.blue, Color.white, Color.red};

            _colorGradient = ColorUtils.CreateSolidGradient(GradientColors);
        }

        public BiomeDef Biome { get; set; }

        public abstract MostLeastFeature Feature { get; }

        public Dictionary<BiomeDef, Dictionary<int, float>> FeatureByBiomes { get; } =
            new Dictionary<BiomeDef, Dictionary<int, float>>();

        public Texture2D FeatureGradientTexture
        {
            get
            {
                if (_featureGradientTexture == null)
                    _featureGradientTexture = ColorUtils.CreateGradientTexture(_colorGradient);

                return _featureGradientTexture;
            }

            private set
            {
                if (_featureGradientTexture != null && value != _featureGradientTexture)
                    _featureGradientTexture = value;
            }
        }

        public abstract string FeatureMeasureUnit { get; }

        public virtual string FeatureName => Feature.ToString();

        public Dictionary<BiomeDef, List<float>> FeatureQuantaByBiomes { get; } =
            new Dictionary<BiomeDef, List<float>>();

        public List<Color> GradientColors { get; }

        /// <summary>
        ///     Whole world ordered list of <see cref="KeyValuePair{TKey,TValue}" /> where the key is a tile ID and the value is
        ///     the feature value.
        /// </summary>
        public List<KeyValuePair<int, float>> WorldTilesFeatures { get; } = new List<KeyValuePair<int, float>>();

        protected abstract float TileFeatureValue(int tileId);

        private void ExecuteOnWorldGeneratedOrLoaded()
        {
            // clear dictionaries and lists
            FeatureByBiomes.Clear();
            FeatureQuantaByBiomes.Clear();
            ColorSamplesByBiomes.Clear();
            MaterialSamplesByBiomes.Clear();
            WorldTilesFeatures.Clear();

            // and now fetch all feature related data
            FetchAllFeatureData();
        }

        public void FetchAllFeatureData()
        {
            foreach (var biomeDef in DefData.BiomeDefs)
            {
                if (!PrepareLanding.Instance.GameData.WorldData.BiomeHasTiles(biomeDef))
                    continue;

                FetchAllFeatureDataForBiome(biomeDef);

                var tilesAndFeatureValues = FeatureByBiomes[biomeDef];
                WorldTilesFeatures.AddRange(tilesAndFeatureValues.ToList());
            }

            // sort world feature
            WorldTilesFeatures.Sort((x, y) => x.Value.CompareTo(y.Value));
        }

        public void FetchAllFeatureDataForBiome(BiomeDef biomeDef)
        {
            TilesFeatureByBiome(biomeDef);
            QuantizeFeatureForBiome(biomeDef);
            GenerateColorSamples(biomeDef);
            // Can't do the following here on some occasions (i.e Load a save without going to the select landing page).
            // because this would load materials on another thread than the main thread.
            //GenerateMaterialByBiome(biomeDef); 
        }

        public void TilesFeatureByBiome(BiomeDef biomeDef)
        {
            if (FeatureByBiomes.ContainsKey(biomeDef))
                return;

            var tileIds = TileFilterBiomes.TileIdsByBiome(biomeDef);

            var featureDict = new Dictionary<int, float>(tileIds.Count);
            foreach (var tileId in tileIds)
            {
                var value = TileFeatureValue(tileId);
                featureDict.Add(tileId, value);
            }

            FeatureByBiomes.Add(biomeDef, featureDict);
        }

        private void QuantizeFeatureForBiome(BiomeDef biome)
        {
            if (FeatureQuantaByBiomes.ContainsKey(biome))
                return;

            var maxQuanta = Mathf.Min(DefaultNumberOfColorSamples, FeatureByBiomes[biome].Count);

            var biomeMinFeatureValue = MinFeatureByBiome(biome);
            var biomeMaxFeatureValue = MaxFeatureByBiome(biome);

            var deltaValue = biomeMaxFeatureValue - biomeMinFeatureValue;
            var featureQuantum = deltaValue / maxQuanta;

            var tempQuantaFeatureList = new List<float>(maxQuanta);

            var currentFeatureQuanta = biomeMinFeatureValue;
            for (var i = 0; i < maxQuanta; i++)
            {
                currentFeatureQuanta += featureQuantum;
                tempQuantaFeatureList.Add(currentFeatureQuanta);
            }

            FeatureQuantaByBiomes.Add(biome, tempQuantaFeatureList);
        }

        public void GenerateColorSamples(BiomeDef biomeDef)
        {
            if (ColorSamplesByBiomes.ContainsKey(biomeDef))
                return;

            var tileCount = FeatureByBiomes[biomeDef].Count;

            var maxSamples = Mathf.Min(tileCount, DefaultNumberOfColorSamples);
            var colorSamples = ColorUtils.CreateColorSamples(_colorGradient, maxSamples);
            ColorSamplesByBiomes[biomeDef] = colorSamples;
        }

        private void GenerateMaterialByBiome(BiomeDef biome)
        {
            if (!ColorSamplesByBiomes.ContainsKey(biome))
                return;
            /*
            var colorSamples = ColorSamplesByBiomes[biome];
            var materialSamples = colorSamples.Select(colorSample => new Material(_defaultMaterial) { color = colorSample }).ToList();
            MaterialSamplesByBiomes[biome] = materialSamples;
            */

            var colorSamples = ColorSamplesByBiomes[biome];
            var materialSamples = colorSamples
                .Select(colorSample => SolidColorMaterials.NewSolidColorMaterial(colorSample,
                    ShaderDatabase.WorldTerrain)).ToList();
            MaterialSamplesByBiomes[biome] = materialSamples;
        }

        public Material MaterialFromTileFeature(BiomeDef biome, int tileId)
        {
            var quantaIndex = FindQuantaIndex(biome, tileId);
            if (MaterialSamplesByBiomes.Count == 0)
                GenerateMaterialByBiome(biome);

            var material = MaterialSamplesByBiomes[biome][quantaIndex];

            return material;
        }

        protected int FindQuantaIndex(BiomeDef biome, int tileId)
        {
            var tileAverageTemp = FeatureByBiomes[biome][tileId];
            var temperatureQuanta = FeatureQuantaByBiomes[biome];
            var quantaLessThanOrEqu = temperatureQuanta.Where(t => t <= tileAverageTemp).ToList();
            if (!quantaLessThanOrEqu.Any())
                return 0;

            var quantTemp = quantaLessThanOrEqu.Max();
            var quantaIndex = temperatureQuanta.IndexOf(quantTemp);
            return quantaIndex;
        }

        public Color ColorFromFeature(BiomeDef biome, int tileId)
        {
            var quantaIndex = FindQuantaIndex(biome, tileId);

            var color = ColorSamplesByBiomes[biome][quantaIndex];

            return color;
        }

        /// <summary>
        ///     Gets the maximum average temperature for a given biomeDef.
        /// </summary>
        /// <param name="biomeDef">The biomeDef from which to get the maximum average temperature.</param>
        /// <returns>The maximum average temperature for a given biomeDef.</returns>
        public float MaxFeatureByBiome(BiomeDef biomeDef)
        {
            if (!FeatureByBiomes.ContainsKey(biomeDef))
                return float.NaN;

            var tilesTemp = FeatureByBiomes[biomeDef];
            return tilesTemp.Values.Max();
        }

        /// <summary>
        ///     Gets the mean for all of the average temperatures in a given biomeDef.
        /// </summary>
        /// <param name="biomeDef">The biomeDef from which to get the mean temperature.</param>
        /// <returns>The mean temperature of the given biomeDef.</returns>
        public float MeanFeatureByBiome(BiomeDef biomeDef)
        {
            if (!FeatureByBiomes.ContainsKey(biomeDef))
                return float.NaN;

            var tilesTemp = FeatureByBiomes[biomeDef];
            return tilesTemp.Values.Mean();
        }

        /// <summary>
        ///     Gets the minimum average temperature for a given biomeDef.
        /// </summary>
        /// <param name="biomeDef">The biomeDef from which to get the minimum average temperature.</param>
        /// <returns>The minimum average temperature for a given biomeDef.</returns>
        public float MinFeatureByBiome(BiomeDef biomeDef)
        {
            if (!FeatureByBiomes.ContainsKey(biomeDef))
                return float.NaN;

            var tilesTemp = FeatureByBiomes[biomeDef];
            return tilesTemp.Values.Min();
        }

        public List<KeyValuePair<int, float>> WorldMinRange(int numberOfItems = 1)
        {
            if (numberOfItems < 1 || numberOfItems > WorldTilesFeatures.Count)
            {
                Log.Message(
                    $"[PrepareLanding] WorldMinRange: Invalid request number of items ({numberOfItems} / {WorldTilesFeatures.Count}).");
                return null;
            }

            // get the lowest world feature values: start at the beginning and fetch the requested number of tiles
            return WorldTilesFeatures.GetRange(0, numberOfItems);
        }

        public List<KeyValuePair<int, float>> WorldMaxRange(int numberOfItems = 1)
        {
            if (numberOfItems < 1 || numberOfItems > WorldTilesFeatures.Count)
            {
                Log.Message(
                    $"[PrepareLanding] WorldMinRange: Invalid request number of items ({numberOfItems} / {WorldTilesFeatures.Count}).");
                return null;
            }

            // get the highest world feature values: fetch all the requested number of tiles up to the end
            var start = WorldTilesFeatures.Count - numberOfItems;
            return WorldTilesFeatures.GetRange(start, numberOfItems);
        }
    }
}