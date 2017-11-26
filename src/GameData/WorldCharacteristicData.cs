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
    public abstract class WorldCharacteristicData
    {
        public const int DefaultNumberOfColorSamples = 100;
        private readonly Gradient _colorGradient;

        protected readonly Material _defaultMaterial = WorldMaterials.SelectedTile;

        public readonly Dictionary<BiomeDef, List<Color>> ColorSamplesByBiomes =
            new Dictionary<BiomeDef, List<Color>>();

        protected readonly DefData DefData;

        public readonly Dictionary<BiomeDef, List<Material>> MaterialSamplesByBiomes =
            new Dictionary<BiomeDef, List<Material>>();

        private Texture2D _characteristicGradientTexture;

        protected WorldCharacteristicData(DefData defData)
        {
            DefData = defData;

            PrepareLanding.Instance.EventHandler.WorldGeneratedOrLoaded += ExecuteOnWorldGeneratedOrLoaded;

            // Cocorico!
            GradientColors = new List<Color> {Color.blue, Color.white, Color.red};

            _colorGradient = ColorUtils.CreateSolidGradient(GradientColors);
        }

        public BiomeDef Biome { get; set; }

        public abstract MostLeastCharacteristic Characteristic { get; }

        public Dictionary<BiomeDef, Dictionary<int, float>> CharacteristicByBiomes { get; } =
            new Dictionary<BiomeDef, Dictionary<int, float>>();

        public Texture2D CharacteristicGradientTexture
        {
            get
            {
                if (_characteristicGradientTexture == null)
                    _characteristicGradientTexture = ColorUtils.CreateGradientTexture(_colorGradient);

                return _characteristicGradientTexture;
            }

            private set
            {
                if (_characteristicGradientTexture != null && value != _characteristicGradientTexture)
                    _characteristicGradientTexture = value;
            }
        }

        public abstract string CharacteristicMeasureUnit { get; }

        public virtual string CharacteristicName => Characteristic.ToString();

        public Dictionary<BiomeDef, List<float>> CharacteristicQuantaByBiomes { get; } =
            new Dictionary<BiomeDef, List<float>>();

        public List<Color> GradientColors { get; }

        /// <summary>
        ///     Whole world ordered list of <see cref="KeyValuePair{TKey,TValue}" /> where the key is a tile ID and the value is
        ///     the characteristic value.
        /// </summary>
        public List<KeyValuePair<int, float>> WorldTilesCharacteristics { get; } = new List<KeyValuePair<int, float>>();

        protected abstract float TileCharacteristicValue(int tileId);

        private void ExecuteOnWorldGeneratedOrLoaded()
        {
            // clear dictionaries and lists
            CharacteristicByBiomes.Clear();
            CharacteristicQuantaByBiomes.Clear();
            ColorSamplesByBiomes.Clear();
            MaterialSamplesByBiomes.Clear();
            WorldTilesCharacteristics.Clear();

            // and now fetch all characteristic related data
            FetchAllCharacteristicData();
        }

        public void FetchAllCharacteristicData()
        {
            foreach (var biomeDef in DefData.BiomeDefs)
            {
                if (!PrepareLanding.Instance.GameData.WorldData.BiomeHasTiles(biomeDef))
                    continue;

                FetchAllCharacteristicDataForBiome(biomeDef);

                var tilesAndCharacteristicValues = CharacteristicByBiomes[biomeDef];
                WorldTilesCharacteristics.AddRange(tilesAndCharacteristicValues.ToList());
            }

            // sort world characteristic
            WorldTilesCharacteristics.Sort((x, y) => x.Value.CompareTo(y.Value));
        }

        public void FetchAllCharacteristicDataForBiome(BiomeDef biomeDef)
        {
            TilesCharacteristicByBiome(biomeDef);
            QuantizeCharacteristicForBiome(biomeDef);
            GenerateColorSamples(biomeDef);
            // Can't do the following here on some occasions (i.e Load a save without going to the select landing page).
            // because this would load materials on another thread than the main thread.
            //GenerateMaterialByBiome(biomeDef); 
        }

        public void TilesCharacteristicByBiome(BiomeDef biomeDef)
        {
            if (CharacteristicByBiomes.ContainsKey(biomeDef))
                return;

            var tileIds = TileFilterBiomes.TileIdsByBiome(biomeDef);

            var characteristicDict = new Dictionary<int, float>(tileIds.Count);
            foreach (var tileId in tileIds)
            {
                var value = TileCharacteristicValue(tileId);
                characteristicDict.Add(tileId, value);
            }

            CharacteristicByBiomes.Add(biomeDef, characteristicDict);
        }

        private void QuantizeCharacteristicForBiome(BiomeDef biome)
        {
            if (CharacteristicQuantaByBiomes.ContainsKey(biome))
                return;

            var maxQuanta = Mathf.Min(DefaultNumberOfColorSamples, CharacteristicByBiomes[biome].Count);

            var biomeMinCharacteristicValue = MinCharacteristicByBiome(biome);
            var biomeMaxCharacteristicValue = MaxCharacteristicByBiome(biome);

            var deltaValue = biomeMaxCharacteristicValue - biomeMinCharacteristicValue;
            var characteristicQuantum = deltaValue / maxQuanta;

            var tempQuantaCharacteristicList = new List<float>(maxQuanta);

            var currentCharacteristicQuanta = biomeMinCharacteristicValue;
            for (var i = 0; i < maxQuanta; i++)
            {
                currentCharacteristicQuanta += characteristicQuantum;
                tempQuantaCharacteristicList.Add(currentCharacteristicQuanta);
            }

            CharacteristicQuantaByBiomes.Add(biome, tempQuantaCharacteristicList);
        }

        public void GenerateColorSamples(BiomeDef biomeDef)
        {
            if (ColorSamplesByBiomes.ContainsKey(biomeDef))
                return;

            var tileCount = CharacteristicByBiomes[biomeDef].Count;

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

        public Material MaterialFromTileCharacteristic(BiomeDef biome, int tileId)
        {
            var quantaIndex = FindQuantaIndex(biome, tileId);
            if (MaterialSamplesByBiomes.Count == 0)
                GenerateMaterialByBiome(biome);

            var material = MaterialSamplesByBiomes[biome][quantaIndex];

            return material;
        }

        protected int FindQuantaIndex(BiomeDef biome, int tileId)
        {
            var tileAverageTemp = CharacteristicByBiomes[biome][tileId];
            var temperatureQuanta = CharacteristicQuantaByBiomes[biome];
            var quantaLessThanOrEqu = temperatureQuanta.Where(t => t <= tileAverageTemp).ToList();
            if (!quantaLessThanOrEqu.Any())
                return 0;

            var quantTemp = quantaLessThanOrEqu.Max();
            var quantaIndex = temperatureQuanta.IndexOf(quantTemp);
            return quantaIndex;
        }

        public Color ColorFromCharacteristic(BiomeDef biome, int tileId)
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
        public float MaxCharacteristicByBiome(BiomeDef biomeDef)
        {
            if (!CharacteristicByBiomes.ContainsKey(biomeDef))
                return float.NaN;

            var tilesTemp = CharacteristicByBiomes[biomeDef];
            return tilesTemp.Values.Max();
        }

        /// <summary>
        ///     Gets the mean for all of the average temperatures in a given biomeDef.
        /// </summary>
        /// <param name="biomeDef">The biomeDef from which to get the mean temperature.</param>
        /// <returns>The mean temperature of the given biomeDef.</returns>
        public float MeanCharacteristicByBiome(BiomeDef biomeDef)
        {
            if (!CharacteristicByBiomes.ContainsKey(biomeDef))
                return float.NaN;

            var tilesTemp = CharacteristicByBiomes[biomeDef];
            return tilesTemp.Values.Mean();
        }

        /// <summary>
        ///     Gets the minimum average temperature for a given biomeDef.
        /// </summary>
        /// <param name="biomeDef">The biomeDef from which to get the minimum average temperature.</param>
        /// <returns>The minimum average temperature for a given biomeDef.</returns>
        public float MinCharacteristicByBiome(BiomeDef biomeDef)
        {
            if (!CharacteristicByBiomes.ContainsKey(biomeDef))
                return float.NaN;

            var tilesTemp = CharacteristicByBiomes[biomeDef];
            return tilesTemp.Values.Min();
        }

        public List<KeyValuePair<int, float>> WorldMinRange(int numberOfItems = 1)
        {
            if (numberOfItems < 1 || numberOfItems > WorldTilesCharacteristics.Count)
            {
                Log.Message(
                    $"[PrepareLanding] WorldMinRange: Invalid request number of items ({numberOfItems} / {WorldTilesCharacteristics.Count}).");
                return null;
            }

            // get the lowest world characteristic values: start at the beginning and fetch the requested number of tiles
            return WorldTilesCharacteristics.GetRange(0, numberOfItems);
        }

        public List<KeyValuePair<int, float>> WorldMaxRange(int numberOfItems = 1)
        {
            if (numberOfItems < 1 || numberOfItems > WorldTilesCharacteristics.Count)
            {
                Log.Message(
                    $"[PrepareLanding] WorldMinRange: Invalid request number of items ({numberOfItems} / {WorldTilesCharacteristics.Count}).");
                return null;
            }

            // get the highest world characteristic values: fetch all the requested number of tiles up to the end
            var start = WorldTilesCharacteristics.Count - numberOfItems;
            return WorldTilesCharacteristics.GetRange(start, numberOfItems);
        }
    }
}