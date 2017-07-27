using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.Remoting.Activation;
using System.Security.Cryptography;
using System.Text;
using System.Xml.Linq;
using PrepareLanding.Extensions;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace PrepareLanding
{

    public class Preset
    {
        private const string PresetVersionNode = "Version";
        public const string PresetVersion = "1.0";
        private const string PresetDescriptionNode = "Description";
        private const string PresetTemplateNode = "Template";

        private const string FilterNode = "Filters";
        private const string TerrainNode = "Terrain";
        private const string TemperatureNode = "Temperature";

        private const string OptionNode = "Options";

        // use / min / max
        private const string MinNode = "Min";

        private const string MaxNode = "Max";

        private const string UseNode = "Use";

        // def
        private const string DefNameNode = "defName";

        // state
        private const string StateNode = "State";

        private readonly PrepareLandingUserData _userData;

        private StringBuilder _presetInfo;
        private int _indent;

        public string Description { get; set; }

        public string Version { get; set;  }

        public bool IsTemplate { get; private set; }

        public string PresetInfo => _presetInfo.ToString();

        public Preset(PrepareLandingUserData userData)
        {
            _userData = userData;
            IsTemplate = false;
        }

        public void LoadPresetInfo(XElement xRootNode)
        {
            Version = xRootNode.Element(PresetVersionNode)?.Value;
            Description = xRootNode.Element(PresetDescriptionNode)?.Value;
            LoadBoolean(xRootNode, PresetTemplateNode, b => IsTemplate = b);

            _presetInfo = new StringBuilder();
            _indent = 0;

            LoadPresetInfoRecusive(xRootNode);
        }

        private void LoadPresetInfoRecusive(XElement xRootNode)
        {
            
            foreach (var element in xRootNode.Elements())
            {
                var indentString = " ".Repeat(_indent);
                _presetInfo.AppendLine(element.HasElements
                    ? $"{indentString}{element.Name}"
                    : $"{indentString}{element.Name}: {element.Value}");

                _indent += 4;
                LoadPresetInfoRecusive(element);
            }

            _indent -= 4;
            if (_indent < 0)
                _indent = 0;
        }

        public void LoadPreset(XElement xRootNode)
        {
            /*
             * Header
             */
            Version = xRootNode.Element(PresetVersionNode)?.Value;
            Description = xRootNode.Element(PresetDescriptionNode)?.Value;
            LoadBoolean(xRootNode, PresetTemplateNode, b => IsTemplate = b);

            /*
             *  Filters
             */

            var xFilters = xRootNode.Element(FilterNode);

            // terrain
            var xTerrain = xFilters?.Element(TerrainNode);
            if (xTerrain == null)
                return;

            _userData.ChosenBiome = LoadDef<BiomeDef>(xTerrain, "Biome") as BiomeDef;
            _userData.ChosenHilliness = LoadEnum<Hilliness>(xTerrain, "Hilliness");
            LoadMultiThreeStates(xTerrain, "Roads", "Road", _userData.SelectedRoadDefs);
            LoadMultiThreeStates(xTerrain, "Rivers", "River", _userData.SelectedRiverDefs);
            LoadUsableMinMax(xTerrain, "CurrentMovementTime", _userData.CurrentMovementTime);
            LoadUsableMinMax(xTerrain, "SummerMovementTime", _userData.SummerMovementTime);
            LoadUsableMinMax(xTerrain, "WinterMovementTime", _userData.WinterMovementTime);
            LoadMultiThreeStatesOrdered(xTerrain, "Stones", "Stone", _userData.SelectedStoneDefs,
                _userData.OrderedStoneDefs);
            _userData.ChosenCoastalTileState = LoadThreeState(xTerrain, "CoastalTile");
            LoadUsableMinMax(xTerrain, "Elevation", _userData.Elevation);
            LoadUsableMinMax(xTerrain, "TimeZone", _userData.TimeZone);


            // temperature
            var xTemperature = xFilters.Element(TemperatureNode);
            if (xTemperature == null)
                return;

            LoadUsableMinMax(xTemperature, "AverageTemperature", _userData.AverageTemperature);
            LoadUsableMinMax(xTemperature, "SummerTemperature", _userData.SummerTemperature);
            LoadUsableMinMax(xTemperature, "WinterTemperature", _userData.WinterTemperature);
            LoadMinMaxFromRestrictedList(xTemperature, "GrowingPeriod", _userData.GrowingPeriod);
            LoadUsableMinMax(xTemperature, "RainFall", _userData.RainFall);
            _userData.ChosenAnimalsCanGrazeNowState = LoadThreeState(xTemperature, "AnimalsCanGrazeNow");

            /*
             * Options
             */
            var xOptions = xRootNode.Element(OptionNode);
            if (xOptions == null)
                return;

            LoadBoolean(xOptions, "AllowImpassableHilliness", b => _userData.Options.AllowImpassableHilliness = b);
            LoadBoolean(xOptions, "AllowInvalidTilesForNewSettlement", b => _userData.Options.AllowInvalidTilesForNewSettlement = b);
            LoadBoolean(xOptions, "AllowLiveFiltering", b => _userData.Options.AllowLiveFiltering = b);
            LoadBoolean(xOptions, "BypassMaxHighlightedTiles", b => _userData.Options.BypassMaxHighlightedTiles = b);
            LoadBoolean(xOptions, "DisablePreFilterCheck", b => _userData.Options.DisablePreFilterCheck = b);
            LoadBoolean(xOptions, "DisableTileBlinking", b => _userData.Options.DisableTileBlinking = b);
            LoadBoolean(xOptions, "ShowDebugTileId", b => _userData.Options.ShowDebugTileId = b);
            LoadBoolean(xOptions, "ShowFilterHeaviness", b => _userData.Options.ShowFilterHeaviness = b);

        }

        public void SavePreset(XElement xRoot, string description = null, bool saveOptions = false)
        {
            try
            {
                xRoot.Add(new XElement(PresetVersionNode, PresetVersion));
                xRoot.Add(new XElement(PresetTemplateNode, false));
                xRoot.Add(new XElement(PresetDescriptionNode, string.IsNullOrEmpty(description) ? "None" : description));

                /*
                 * filters
                 */
                var xFilter = new XElement(FilterNode);
                xRoot.Add(xFilter);

                // Terrain
                var xTerrainFilters = new XElement(TerrainNode);
                xFilter.Add(xTerrainFilters);

                SaveDef(xTerrainFilters, "Biome", _userData.ChosenBiome);
                SaveHilliness(xTerrainFilters, "Hilliness", _userData.ChosenHilliness);
                SaveMultiThreeStates(xTerrainFilters, "Roads", "Road", _userData.SelectedRoadDefs);
                SaveMultiThreeStates(xTerrainFilters, "Rivers", "River", _userData.SelectedRiverDefs);
                SaveUsableMinMax(xTerrainFilters, "CurrentMovementTime", _userData.CurrentMovementTime);
                SaveUsableMinMax(xTerrainFilters, "SummerMovementTime", _userData.SummerMovementTime);
                SaveUsableMinMax(xTerrainFilters, "WinterMovementTime", _userData.WinterMovementTime);
                SaveMultiThreeStatesOrdered(xTerrainFilters, "Stones", "Stone", _userData.SelectedStoneDefs,
                    _userData.OrderedStoneDefs);
                SaveThreeState(xTerrainFilters, "CoastalTile", _userData.ChosenCoastalTileState);
                SaveUsableMinMax(xTerrainFilters, "Elevation", _userData.Elevation);
                SaveUsableMinMax(xTerrainFilters, "TimeZone", _userData.TimeZone);

                // Temperature
                var xTemperatureFilters = new XElement("Temperature");
                xFilter.Add(xTemperatureFilters);

                SaveUsableMinMax(xTemperatureFilters, "AverageTemperature", _userData.AverageTemperature);
                SaveUsableMinMax(xTemperatureFilters, "SummerTemperature", _userData.SummerTemperature);
                SaveUsableMinMax(xTemperatureFilters, "WinterTemperature", _userData.WinterTemperature);
                SaveMinMaxFromRestrictedList(xTemperatureFilters, "GrowingPeriod", _userData.GrowingPeriod);
                SaveUsableMinMax(xTemperatureFilters, "RainFall", _userData.RainFall);
                SaveThreeState(xTerrainFilters, "AnimalsCanGrazeNow", _userData.ChosenAnimalsCanGrazeNowState);

                /*
                 * Options
                 */
                var xOption = new XElement(OptionNode);
                xRoot.Add(xOption);

                // don't save options if not asked for
                if (!saveOptions)
                    return;

                SaveBoolean(xOption, "AllowImpassableHilliness", _userData.Options.AllowImpassableHilliness);
                SaveBoolean(xOption, "AllowInvalidTilesForNewSettlement",
                    _userData.Options.AllowInvalidTilesForNewSettlement);
                SaveBoolean(xOption, "AllowLiveFiltering", _userData.Options.AllowLiveFiltering);
                SaveBoolean(xOption, "BypassMaxHighlightedTiles", _userData.Options.BypassMaxHighlightedTiles);
                SaveBoolean(xOption, "DisablePreFilterCheck", _userData.Options.DisablePreFilterCheck);
                SaveBoolean(xOption, "DisableTileBlinking", _userData.Options.DisableTileBlinking);
                SaveBoolean(xOption, "ShowDebugTileId", _userData.Options.ShowDebugTileId);
                SaveBoolean(xOption, "ShowFilterHeaviness", _userData.Options.ShowFilterHeaviness);
            }
            catch (Exception e)
            {
                Log.Error($"Failed to save preset file. error:\n\t{e}\n\t{e.Message}");
                throw;
            }
        }

        #region LOAD_PRESET

        private Def LoadDef<T>(XContainer xParent, string elementName) where T : Def
        {
            var xFoundElement = xParent.Element(elementName);
            if (xFoundElement == null)
                return null;

            switch (typeof(T).Name)
            {
                case nameof(BiomeDef):
                    foreach (var biomeDef in _userData.BiomeDefs)
                        if (string.Equals(biomeDef.defName, xFoundElement.Value, StringComparison.OrdinalIgnoreCase))
                            return biomeDef;
                    break;

                case nameof(RoadDef):
                    foreach (var roadDef in _userData.RoadDefs)
                        if (string.Equals(roadDef.defName, xFoundElement.Value, StringComparison.OrdinalIgnoreCase))
                            return roadDef;
                    break;

                case nameof(RiverDef):
                    foreach (var riverDef in _userData.RiverDefs)
                        if (string.Equals(riverDef.defName, xFoundElement.Value, StringComparison.OrdinalIgnoreCase))
                            return riverDef;
                    break;

                case nameof(ThingDef): // TODO: be wary that multiple things might be ThinDef, so better check if its a stone before parsing StoneDefs.
                    foreach (var stoneDef in _userData.StoneDefs)
                        if (string.Equals(stoneDef.defName, xFoundElement.Value, StringComparison.OrdinalIgnoreCase))
                            return stoneDef;
                    break;

                default:
                    Log.Error("[PrepareLanding] LoadDef: Unknown defType");
                    break;
            }

            return null;
        }

        private static T LoadEnum<T>(XContainer xParent, string elementName)
        {
            var xFoundElement = xParent.Element(elementName);
            if (xFoundElement == null)
                return default(T); // note: remember that default(T) for an Enum is the value at 0

            if (!Enum.IsDefined(typeof(T), xFoundElement.Value))
                return default(T);

            return (T)Enum.Parse(typeof(T), xFoundElement.Value, true);
        }

        private void LoadMultiThreeStates<T>(XContainer xParent, string elementName, string subElementName,
            Dictionary<T, ThreeStateItem> dict) where T : Def
        {
            var xFoundElement = xParent.Element(elementName);
            if (xFoundElement == null)
            {
                // everything in default state
                foreach (var value in dict.Values)
                    value.State = MultiCheckboxState.Partial;
                return;
            }

            foreach (var xSubElement in xFoundElement.Elements())
            {
                if (xSubElement.Name != subElementName)
                    continue;

                var def = LoadDef<T>(xSubElement, DefNameNode) as T;
                if (def == null)
                    continue;

                var xState = xSubElement.Element(StateNode);
                if (xState == null)
                    continue;

                ThreeStateItem threeStateItem;
                if (!dict.TryGetValue(def, out threeStateItem))
                    continue;

                var state = LoadEnum<MultiCheckboxState>(xSubElement, StateNode);
                threeStateItem.State = state;
            }
        }

        private static void LoadUsableMinMax<T>(XContainer xParent, string elementName, UsableMinMaxNumericItem<T> item)
            where T : struct, IComparable, IConvertible
        {
            var xFoundElement = xParent.Element(elementName);

            var xUse = xFoundElement?.Element(UseNode);
            if (xUse == null)
                return;

            bool use;
            if (!Load(xFoundElement, UseNode, out use))
                return;

            if (!use)
                return;

            item.Use = true;

            T value;
            if (!Load(xFoundElement, MinNode, out value))
                return;

            item.Min = value;
            item.MinString = xFoundElement.Element(MinNode)?.Value;

            if (!Load(xFoundElement, MaxNode, out value))
                return;

            item.Max = value;
            item.MaxString = xFoundElement.Element(MaxNode)?.Value;
        }

        private static bool Load<T>(XContainer xParent, string elementName, out T result) where T : IConvertible
        {
            var xFoundElement = xParent.Element(elementName);
            if (xFoundElement == null)
            {
                result = default(T);
                return false;
            }

            result = (T)Convert.ChangeType(xFoundElement.Value, typeof(T));
            return true;
        }

        private void LoadMultiThreeStatesOrdered<T>(XContainer xParent, string elementName, string entryName,
            IDictionary<T, ThreeStateItem> dict, ICollection<T> orderedList) where T : Def
        {
            var xFoundElement = xParent.Element(elementName);
            if (xFoundElement == null)
                return;

            orderedList.Clear();
            foreach (var xElement in xFoundElement.Elements(entryName))
            {
                var xDefName = xElement.Element(DefNameNode);
                if (xDefName == null)
                    goto EnsureAllEntriesPresent;

                var def = LoadDef<T>(xElement, DefNameNode) as T;
                if (def == null)
                    goto EnsureAllEntriesPresent;

                orderedList.Add(def);

                ThreeStateItem threeStateItem;
                if (!dict.TryGetValue(def, out threeStateItem))
                    goto EnsureAllEntriesPresent;

                var state = LoadEnum<MultiCheckboxState>(xElement, StateNode);
                threeStateItem.State = state;
            }

            EnsureAllEntriesPresent:
            foreach (var entry in dict)
            {
                if (orderedList.Contains(entry.Key))
                    continue;

                orderedList.Add(entry.Key);
            }
        }

        private static MultiCheckboxState LoadThreeState(XContainer xParent, string containerName)
        {
            var xChild = xParent.Element(containerName);
            if (xChild == null)
                return default(MultiCheckboxState);

            return LoadEnum<MultiCheckboxState>(xChild, StateNode);
        }

        private static void LoadMinMaxFromRestrictedList<T>(XContainer xParent, string elementName,
            MinMaxFromRestrictedListItem<T> item) where T : struct, IConvertible
        {
            var xFoundElement = xParent.Element(elementName);

            var xUse = xFoundElement?.Element(UseNode);
            if (xUse == null)
                return;

            bool use;
            if (!Load(xFoundElement, UseNode, out use))
                return;

            if (!use)
                return;

            item.Use = true;

            if (typeof(T).IsEnum)
            {
                string value;

                // min
                if (!Load(xFoundElement, MinNode, out value))
                    return;

                if (string.IsNullOrEmpty(value))
                {
                    item.Use = false;
                    return;
                }
                item.Min = LoadEnum<T>(xFoundElement, MinNode);

                // max
                if (!Load(xFoundElement, MaxNode, out value))
                    return;

                if (string.IsNullOrEmpty(value))
                {
                    item.Use = false;
                    return;
                }
                item.Max = LoadEnum<T>(xFoundElement, MaxNode);
            }
            else
            {
                T value;
                if (!Load(xFoundElement, MinNode, out value))
                    return;
                item.Min = value;

                if (!Load(xFoundElement, MaxNode, out value))
                    return;
                item.Max = value;
            }
        }

        private static bool LoadBoolean(XContainer xParent, string entryName, Action<bool> actionSet)
        {
            bool value;
            if (!Load(xParent, entryName, out value))
                return false;

            actionSet(value);

            return true;
        }

        #endregion LOAD_PRESET

        #region SAVE_PRESET

        private static void SaveBoolean(XContainer xRoot, string entryName, bool value)
        {
            if (!value)
                return;

            xRoot.Add(new XElement(entryName, true));
        }

        private static void SaveDef<T>(XContainer xRoot, string entryName, T def) where T : Def
        {
            if (def == null)
                return;

            xRoot.Add(new XElement(entryName, def.defName));
        }

        private static void SaveHilliness(XContainer xRoot, string entryName, Hilliness hilliness)
        {
            if (hilliness == Hilliness.Undefined)
                return;

            xRoot.Add(new XElement(entryName, hilliness.ToString()));
        }

        private static void SaveMultiThreeStates<T>(XContainer xRoot, string containerName, string entryName,
            Dictionary<T, ThreeStateItem> dict) where T : Def
        {
            if (PrepareLandingUserData.IsDefDictInDefaultState(dict))
                return;

            var xContainerElement = new XElement(containerName);
            xRoot.Add(xContainerElement);
            foreach (var entry in dict)
            {
                var xEntry = new XElement(entryName);
                xEntry.Add(new XElement(DefNameNode, entry.Key.defName));
                xEntry.Add(new XElement(StateNode, entry.Value.State.ToString()));
                xContainerElement.Add(xEntry);
            }
        }

        private static void SaveThreeState(XContainer xRoot, string containerName, MultiCheckboxState state)
        {
            if (state == MultiCheckboxState.Partial)
                return;

            var xContainerElement = new XElement(containerName);
            xRoot.Add(xContainerElement);

            xContainerElement.Add(new XElement(StateNode, state.ToString()));
        }

        private static void SaveMultiThreeStatesOrdered<T>(XContainer xRoot, string containerName, string entryName,
            Dictionary<T, ThreeStateItem> dict, IEnumerable<T> orderedList) where T : Def
        {
            if (PrepareLandingUserData.IsDefDictInDefaultState(dict))
                return;

            var xContainerElement = new XElement(containerName);
            xRoot.Add(xContainerElement);
            foreach (var def in orderedList)
            {
                ThreeStateItem threeStateItem;
                if (!dict.TryGetValue(def, out threeStateItem))
                {
                    // shouldn't happen, but just a defensive check
                    Log.Error($"[PrepareLanding] The def '{def.defName}' doesn't exit in the given dictionary.");
                    continue;
                }
                var xEntry = new XElement(entryName);
                xEntry.Add(new XElement(DefNameNode, def.defName));
                xEntry.Add(new XElement(StateNode, threeStateItem.State.ToString()));
                xContainerElement.Add(xEntry);
            }
        }

        private static void SaveUsableMinMax<T>(XContainer xRoot, string elementName, UsableMinMaxNumericItem<T> item)
            where T : struct, IComparable, IConvertible
        {
            if (!item.Use)
                return;

            var xElement = new XElement(elementName);
            xRoot.Add(xElement);

            xElement.Add(new XElement(UseNode, item.Use));
            xElement.Add(new XElement(MinNode, item.Min));
            xElement.Add(new XElement(MaxNode, item.Max));
        }

        private static void SaveMinMaxFromRestrictedList<T>(XContainer xRoot, string elementName,
            MinMaxFromRestrictedListItem<T> item) where T : struct, IConvertible
        {
            if (!item.Use)
                return;

            var xElement = new XElement(elementName);
            xRoot.Add(xElement);

            xElement.Add(new XElement(UseNode, item.Use));
            xElement.Add(new XElement(MinNode, item.Min.ToString(CultureInfo.InvariantCulture)));
            xElement.Add(new XElement(MaxNode, item.Max.ToString(CultureInfo.InvariantCulture)));
        }

        #endregion SAVE_PRESET

    }


    public class PresetManager
    {
        public const string DefaultFileName = "PLPreset";

        public const string PresetVersion = "1.0";

        public const string DefaultExtension = ".XML";

        private const string RootName = "Preset";

        private IEnumerable<FileInfo> _allPresetFiles;

        private readonly PrepareLandingUserData _userData;

        private readonly Dictionary<string, Preset> _presetCache = new Dictionary<string, Preset>();

        public IEnumerable<FileInfo> AllPresetFiles
        {
            get
            {
                if (_allPresetFiles == null && !IsPresetDirectoryEmpty())
                    RenewPresetFileCache();

                return _allPresetFiles;
            }
            private set { _allPresetFiles = value; }
        }

        /// <summary>
        /// Name of the preset save folder.
        /// </summary>
        public static string FolderName => PrepareLanding.Instance.ModIdentifier;

        public PresetManager(PrepareLandingUserData userData)
        {
            _userData = userData;
            PreloadPresets();
        }

        public void LoadPresetInfo(string presetName, bool forceReload = false)
        {
            var filePath = GetPresetFilePath(presetName);

            if (!File.Exists(filePath))
                return;

            if (_presetCache.ContainsKey(presetName) && !forceReload)
                return;

            try
            {
                var xDocument = XDocument.Load(filePath);
                if (xDocument.Root == null)
                    throw new Exception("No root node");

                // get the root element
                var xPreset = xDocument.Element(RootName);
                if (xPreset == null)
                    throw new Exception($"No root node named '{RootName}'");

                // create the preset or load it if it already exists
                var preset = !_presetCache.ContainsKey(presetName) ? new Preset(_userData) : _presetCache[presetName];

                preset.LoadPresetInfo(xPreset);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }

        }

        public void LoadPreset(string presetName, bool forceReload = false)
        {
            if (string.IsNullOrEmpty(presetName))
                return;

            var filePath = GetPresetFilePath(presetName);

            if (!File.Exists(filePath))
                return;

            if (_presetCache.ContainsKey(presetName) && !forceReload)
                return;

            // disable live filtering as we are gonna change some filters on the fly
            var liveFilterting = _userData.Options.AllowLiveFiltering;
            _userData.Options.AllowLiveFiltering = false;

            // reset all filter states into their default state
            _userData.ResetAllFields();

            try
            {
                var xDocument = XDocument.Load(filePath);
                if (xDocument.Root == null)
                    throw new Exception("No root node");

                // get the root element
                var xPreset = xDocument.Element(RootName);
                if (xPreset == null)
                    throw new Exception($"No root node named '{RootName}'");

                // reload the preset if it was already in the cache
                Preset preset;
                if (_presetCache.TryGetValue(presetName, out preset))
                {
                    preset.LoadPreset(xPreset);

                    //reload its info
                    preset.LoadPresetInfo(xPreset);
                }
                else
                {
                    // create the preset and load it
                    preset = new Preset(_userData);
                    preset.LoadPreset(xPreset);
                    preset.LoadPresetInfo(xPreset);

                    // add it to the cache
                    _presetCache.Add(presetName, preset);
                }

                // renew file cache
                RenewPresetFileCache();

            }
            catch (Exception e)
            {
                Log.Error($"Failed to load preset file '{filePath}'. Error:\n\t{e}\n\t{e.Message}");
                //throw;
            }
            finally
            {
                // re-enable live filtering.
                _userData.Options.AllowLiveFiltering = liveFilterting;
            }
        }

        public void SavePreset(string presetName, string description = null, bool saveOptions = false)
        {
            if (string.IsNullOrEmpty(presetName))
                return;

            var filePath = GetPresetFilePath(presetName);

            try
            {
                // create document
                var xDocument = new XDocument();

                // add root node
                var xRoot = new XElement(RootName);
                xDocument.Add(xRoot);

                // create preset and start save
                var preset = new Preset(_userData);
                preset.SavePreset(xRoot, description, saveOptions);

                // save the document
                xDocument.Save(filePath);

                // reload the preset if it was already in the cache
                if (_presetCache.ContainsKey(presetName))
                {
                    LoadPreset(presetName, true);
                }
                else
                {
                    // add it to the cache
                    _presetCache.Add(presetName, preset);

                    // renew file cache
                    RenewPresetFileCache();
                }
            }
            catch (Exception e)
            {
                Log.Error($"Failed to save preset file '{filePath}'. error:\n\t{e}\n\t{e.Message}");
            }
        }

        private void PreloadPresets()
        {
            foreach (var presetFile in AllPresetFiles)
            {
                var preset = new Preset(_userData);
                var presetName = Path.GetFileNameWithoutExtension(presetFile.Name);
                _presetCache.Add(presetName, preset);
                LoadPresetInfo(presetName, true);
            }
        }

        #region FILE_DIR_HANDLING

        /// <summary>
        /// Full path of the save folder
        /// </summary>
        public static string SaveFolder
        {
            get
            {
                var folderPath = Path.Combine(GenFilePaths.SaveDataFolderPath, FolderName);
                var directoryInfo = new DirectoryInfo(folderPath);
                if (directoryInfo.Exists)
                    return folderPath;

                Log.Message($"[PrepareLanding] Trying to create mod folder at: '{folderPath}'.");
                try
                {
                    directoryInfo.Create();
                    Log.Message($"[PrepareLanding] Successfully created the mod folder at: '{folderPath}'.");
                }
                catch (Exception e)
                {
                    Log.Message(
                        $"[PrepareLanding] An error occurred while trying to create the mod folder at: '{folderPath}'.\n\tThe Error was: {e.Message}");
                    return null;
                }
                return folderPath;
            }
        }

        public bool IsPresetDirectoryEmpty()
        {
            return Directory.GetFiles(SaveFolder).Length == 0;
        }

        private static string GetPresetFilePath(string fileName)
        {
            // file extension checking, just being precautious
            var filePath = Path.Combine(SaveFolder, fileName);
            if (!Path.HasExtension(filePath))
            {
                filePath = Path.ChangeExtension(filePath, DefaultExtension);
            }
            else
            {
                var extension = Path.GetExtension(filePath);
                if (string.Compare(extension, DefaultExtension, StringComparison.OrdinalIgnoreCase) != 0)
                    filePath = Path.ChangeExtension(filePath, DefaultExtension);
            }

            return filePath;
        }

        public bool PresetFileExists(string presetName)
        {
            if (string.IsNullOrEmpty(presetName))
                return false;

            return AllPresetFiles.Any(file => string.Compare(file.Name, presetName, StringComparison.CurrentCultureIgnoreCase) == 0);
        }

        public Preset PresetByPresetName(string presetName)
        {
            if (string.IsNullOrEmpty(presetName))
                return null;

            Preset presetValue;
            _presetCache.TryGetValue(presetName, out presetValue);
            return presetValue;
        }

        private void RenewPresetFileCache()
        {
            var dirInfo = new DirectoryInfo(SaveFolder);

            AllPresetFiles = from file in dirInfo.GetFiles()
                         where string.Compare(file.Extension, DefaultExtension, StringComparison.OrdinalIgnoreCase) == 0
                         orderby file.LastWriteTime descending
                         select file;
        }

        public string NextPresetFileName
        {
            get
            {
                var counter = 0;
                string fileName;
                do
                {
                    fileName = $"{DefaultFileName}_{counter}{DefaultExtension}";
                    counter++;
                } while (PresetFileExists($"{fileName}"));

                return Path.GetFileNameWithoutExtension(fileName);
            }
        }

        public static bool PresetExists(string presetName)
        {
            var filePath = GetPresetFilePath(presetName);

            return File.Exists(filePath);
        }

        public static string PresetFileMd5(string filePath)
        {
            using (var md5 = MD5.Create())
            {
                using (var stream = File.OpenRead(filePath))
                {
                    return BitConverter.ToString(md5.ComputeHash(stream)).Replace("-", "‌​").ToLower();
                }
            }
        }

        #endregion FILE_DIR_HANDLING
    }
}