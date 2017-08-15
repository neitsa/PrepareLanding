using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PrepareLanding.Core.Extensions;
using PrepareLanding.Filters;
using PrepareLanding.Overlays;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace PrepareLanding.GameData
{
    public class TemperatureData
    {
        public const int DefaultNumberOfColorSamples = 100;

        private readonly Material _defaultMaterial = WorldMaterials.SelectedTile;

        private readonly DefData _defData;

        private BiomeDef _biome;

        private bool _allowDrawOverlay;

        private readonly Gradient _colorGradient;

        public TemperatureData(DefData defData)
        {
            _defData = defData;

            PrepareLanding.Instance.OnWorldGenerated += OnWorldGenerated;

            // Cocorico!
            GradientColors = new List<Color> {Color.blue, Color.white, Color.red};

            _colorGradient = CreateSolidGradient(GradientColors);
        }

        public bool AllowDrawOverlay
        {
            get { return _allowDrawOverlay; }
            set
            {
                if (value == _allowDrawOverlay)
                    return;

                _allowDrawOverlay = value;
                Find.World.renderer.SetDirty<WorldLayerTemperature>();
            }
        }

        public List<Color> GradientColors { get; }

        public Dictionary<BiomeDef, List<Color>> ColorSamplesByBiomes = new Dictionary<BiomeDef, List<Color>>();

        public Dictionary<BiomeDef, List<Material>> MaterialSamplesByBiomes = new Dictionary<BiomeDef, List<Material>>();

        public BiomeDef Biome
        {
            get { return _biome; }
            set
            {
                if (value == _biome)
                    return;

                _biome = value;
                LongEventHandler.QueueLongEvent(delegate
                    {
                        FetchAllTemperatureDataForBiome(value);
                    }, "Fetching Temperatures",
                    true, null);
            }
        }

        public Dictionary<BiomeDef, Dictionary<int, float>> TemperaturesByBiomes { get; } =
            new Dictionary<BiomeDef, Dictionary<int, float>>();

        public Dictionary<BiomeDef, List<float>> TemperaturesQuantaByBiomes { get; } = new Dictionary<BiomeDef, List<float>>();

        public bool TickManagerHasTickAbs => Find.TickManager.gameStartAbsTick == 0;

        public Texture2D TemperatureGradientTexure { get; private set; }

        public Color ColorFromTemperature(BiomeDef biome, int tileId)
        {
            QuantizeTemperaturesForBiome(biome);

            var quantaIndex = FindQuantaIndex(biome, tileId);
           
            var color = ColorSamplesByBiomes[biome][quantaIndex];

            return color;
        }

        public Material MaterialTemperatureFromTile(BiomeDef biome, int tileId)
        {
            var quantaIndex = FindQuantaIndex(biome, tileId);

            var material = MaterialSamplesByBiomes[biome][quantaIndex];

            return material;
        }

        /// <summary>
        ///     Create a list of colors for a given number of samples.
        /// </summary>
        /// <param name="gradient">The gradient from which to create the colors.</param>
        /// <param name="numberOfsamples">The number of color samples required.</param>
        /// <returns>A list of colors.</returns>
        public static List<Color> CreateColorSamples(Gradient gradient, int numberOfsamples)
        {
            /*
             not that hard to understand but I may forget, so:
              let say the gradient contains two colors: leftmost is black and rightmost is white (in-between a gradient of colors between black and white)
              now let say we need 3 "numberOfSamples", so:
              iSample = 1 / (3 - 1) =  0.5
              we have 3 loops so time = 0 ; 0.5 ; 1
              so the resulting list of colors is: pure black (0), gray (0.5) and pure white (1) [with their respective RGBA values]
              This function is helpful to map gradient colors to another set of samples (like, for instance, temperatures)
            */

            var colorList = new List<Color>(numberOfsamples);

            var iSample = 1f / (numberOfsamples - 1);
            for (var i = 0; i < numberOfsamples; i++)
            {
                var time = i * iSample;
                var color = gradient.Evaluate(time);
                colorList.Add(color);
            }

            return colorList;
        }

        public static Texture2D CreateGradientTexture(Gradient gradient, int width = 100, int height = 1)
        {
            // width, height, [red, green, blue, alpha], no mip-map and usual bilinear filter
            var gradientTexture =
                new Texture2D(width, height, TextureFormat.RGBA32, false) {filterMode = FilterMode.Bilinear};

            var colors = CreateColorSamples(gradient, width);

            for (var y = 0; y < height; y++)
            for (var x = 0; x < width; x++)
                gradientTexture.SetPixel(x, y, colors[x]);

            gradientTexture.Apply();
            return gradientTexture;
        }

        /// <summary>
        ///     Create a solid (no alpha) gradient from a list of colors.
        /// </summary>
        /// <param name="colors">The list of colors to create the gradient (2 colors minimum).</param>
        /// <returns>The <see cref="Gradient" /> create from the given list of colors or null if less than 2 colors were given.</returns>
        public static Gradient CreateSolidGradient(IList<Color> colors)
        {
            var numColors = colors.Count;
            if (numColors < 2)
                return null;

            var gradient = new Gradient();

            var t = 1f / (numColors - 1);

            var gck = new GradientColorKey[numColors];
            var gak = new GradientAlphaKey[numColors];
            for (var i = 0; i < numColors; i++)
            {
                gck[i].color = colors[i];
                gck[i].time = i * t;

                gak[i].alpha = 1.0f; // "solid" color
                gak[i].time = i * t;
            }

            gradient.SetKeys(gck, gak);

            return gradient;
        }

        /// <summary>
        /// Gets the maximum average temperature for a given biomeDef. 
        /// </summary>
        /// <param name="biomeDef">The biomeDef from which to get the maximum average temperature.</param>
        /// <returns>The maximum average temperature for a given biomeDef.</returns>
        public float MaxTemperatureByBiome(BiomeDef biomeDef)
        {
            if (!TemperaturesByBiomes.ContainsKey(biomeDef))
                return float.NaN;

            var tilesTemp = TemperaturesByBiomes[biomeDef];
            return tilesTemp.Values.Max();
        }

        /// <summary>
        /// Gets the mean for all of the average temperatures in a given biomeDef.
        /// </summary>
        /// <param name="biomeDef">The biomeDef from which to get the mean temperature.</param>
        /// <returns>The mean temperature of the given biomeDef.</returns>
        public float MeanTemperatureByBiome(BiomeDef biomeDef)
        {
            if (!TemperaturesByBiomes.ContainsKey(biomeDef))
                return float.NaN;

            var tilesTemp = TemperaturesByBiomes[biomeDef];
            return tilesTemp.Values.Mean();
        }

        /// <summary>
        /// Gets the minimum average temperature for a given biomeDef. 
        /// </summary>
        /// <param name="biomeDef">The biomeDef from which to get the minimum average temperature.</param>
        /// <returns>The minimum average temperature for a given biomeDef.</returns>
        public float MinTemperatureByBiome(BiomeDef biomeDef)
        {
            if (!TemperaturesByBiomes.ContainsKey(biomeDef))
                return float.NaN;

            var tilesTemp = TemperaturesByBiomes[biomeDef];
            return tilesTemp.Values.Min();
        }

        public void FetchAllTemperatureDataForBiome(BiomeDef biomeDef)
        {
            TilesTemperatureByBiome(biomeDef);
            QuantizeTemperaturesForBiome(biomeDef);
            GenerateColorSamples(biomeDef);
            GenerateMaterialByBiome(biomeDef);
        }

        public void TilesTemperatureByBiome(BiomeDef biomeDef)
        {
            if (TemperaturesByBiomes.ContainsKey(biomeDef))
                return;

            var tileIds = TileFilterBiomes.TileIdsByBiome(biomeDef);

            var temperatureDictionary = new Dictionary<int, float>(tileIds.Count);
            foreach (var tileId in tileIds)
            {
                var tile = Find.World.grid[tileId];
                temperatureDictionary.Add(tileId, tile.temperature);
            }

            TemperaturesByBiomes.Add(biomeDef, temperatureDictionary);
        }

        public void GenerateColorSamples(BiomeDef biomeDef)
        {
            if (ColorSamplesByBiomes.ContainsKey(biomeDef))
                return;

            var tileCount = TemperaturesByBiomes[biomeDef].Count;

            var maxSamples = Mathf.Min(tileCount, DefaultNumberOfColorSamples);
            var colorSamples = CreateColorSamples(_colorGradient, maxSamples);
            ColorSamplesByBiomes[biomeDef] = colorSamples;
        }

        private void GenerateMaterialByBiome(BiomeDef biome)
        {
            if (!ColorSamplesByBiomes.ContainsKey(biome))
                return;

            var colorSamples = ColorSamplesByBiomes[biome];
            var materialSamples = colorSamples.Select(colorSample => new Material(_defaultMaterial) {color = colorSample}).ToList();
            MaterialSamplesByBiomes[biome] = materialSamples;
        }

        private void QuantizeTemperaturesForBiome(BiomeDef biome)
        {
            if (TemperaturesQuantaByBiomes.ContainsKey(biome))
                return;

            var temperaturesByBiome = TemperaturesByBiomes[biome];
            var maxQuanta = Mathf.Min(DefaultNumberOfColorSamples, temperaturesByBiome.Count);

            var biomeMinTemp = MinTemperatureByBiome(biome);
            var biomeMaxTemp = MaxTemperatureByBiome(biome);

            var deltaTemp = biomeMaxTemp - biomeMinTemp;
            var temperatureQuantum = deltaTemp / maxQuanta;

            var tempQuantaTemperatureList = new List<float>(maxQuanta);

            var currentTempQuanta = biomeMinTemp;
            for (var i = 0; i < maxQuanta; i++)
            {
                currentTempQuanta += temperatureQuantum;
                tempQuantaTemperatureList.Add(currentTempQuanta);
            }

            TemperaturesQuantaByBiomes.Add(biome, tempQuantaTemperatureList);
        }

        private void OnWorldGenerated()
        {
            // clear the dictionaries
            TemperaturesByBiomes.Clear();
            TemperaturesQuantaByBiomes.Clear();
            ColorSamplesByBiomes.Clear();
            MaterialSamplesByBiomes.Clear();

            if (TemperatureGradientTexure == null)
                TemperatureGradientTexure = CreateGradientTexture(_colorGradient);
        }

        private int FindQuantaIndex(BiomeDef biome, int tileId)
        {
            var tileAverageTemp = TemperaturesByBiomes[biome][tileId];
            var temperatureQuanta = TemperaturesQuantaByBiomes[biome];
            var quantaLessThanOrEqu = temperatureQuanta.Where(t => t <= tileAverageTemp).ToList();
            if (!quantaLessThanOrEqu.Any())
                return 0;

            var quantTemp = quantaLessThanOrEqu.Max();
            var quantaIndex = temperatureQuanta.IndexOf(quantTemp);
            return quantaIndex;
        }

        public static T LessThanOrEqual<T>(List<T> list, T value) where T : IComparable
        {
            var index = list.BinarySearch(value);
            if (index < 0)
            {
                index = ~index - 1;
            }
            if (index >= 0)
            {
                var result = list[index];
                return result;
            }

            throw new ArgumentOutOfRangeException($"value {value} is out of bounds.");
        }

#if DEBUG
        public void DebugLog()
        {
            if (_biome == null)
                return;

            var sb = new StringBuilder();

            var bigSeparator = "-".Repeat(80);
            var smallSeparator = "-".Repeat(40);

            var biomeMinTemp = MinTemperatureByBiome(_biome);
            var biomeMaxTemp = MaxTemperatureByBiome(_biome);

            sb.AppendLine(bigSeparator);
            sb.AppendLine(smallSeparator);
            sb.AppendLine("---------- DebugLogTemperature ----------");
            sb.AppendLine(smallSeparator);
            sb.AppendLine($"Biome: {_biome}; biomeMinTemp: {biomeMinTemp}; biomeMaxTemp: {biomeMaxTemp}");

            sb.AppendLine(smallSeparator);
            sb.AppendLine("---------- temperatureQuanta ----------");
            sb.AppendLine(smallSeparator);
            var temperatureQuanta = TemperaturesQuantaByBiomes[_biome];
            sb.AppendLine($"temperatureQuanta.Count: {temperatureQuanta.Count}");
            for (var i = 0; i < temperatureQuanta.Count; i++)
                sb.AppendLine($"{i}: {temperatureQuanta[i]}");

            sb.AppendLine(smallSeparator);
            sb.AppendLine("---------- By Tile ----------");
            sb.AppendLine(smallSeparator);
            var tilesAndTemps = TemperaturesByBiomes[_biome];
            foreach (var tileId in tilesAndTemps.Keys)
            {
                var tileAverageTemp = tilesAndTemps[tileId];
                try
                {
                    var quantaIndex = FindQuantaIndex(_biome, tileId);
                    var color = ColorSamplesByBiomes[_biome][quantaIndex];
                    var s = $"tileId: {tileId}; tileAverageTemp: {tileAverageTemp}; quantaIndex: {quantaIndex}; color: {color}";
                    sb.AppendLine(s);
                }
                catch (InvalidOperationException)
                {
                    var s = $"[Error: quantaIndex: OOB] tileId: {tileId}; tileAverageTemp: {tileAverageTemp}";
                    sb.AppendLine(s);
                }
            }
            sb.AppendLine(bigSeparator);

            Log.Message(sb.ToString());
        }
#endif
    }
}