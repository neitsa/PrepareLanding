using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Xml.Linq;
using PrepareLanding.Core.Extensions;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace PrepareLanding.Presets
{
    public class PresetInfo
    {
        public const string InfoNode = "Info";
        public const string PresetVersionNode = "Version";
        public const string PresetVersion = "1.0";
        public const string PresetDescriptionNode = "Description";
        public const string PresetTemplateNode = "Template";
        public const string PresetAuthorNode = "Author";
        public const string PresetDateNode = "Date";

        public string Description { get; set; }

        public string Version { get; set; }

        public string Author { get; set; }

        public DateTime Date { get; set; }

        public bool IsTemplate { get; private set; }

        private StringBuilder _filterInfo;

        public string FilterInfo => _filterInfo.ToString();

        private StringBuilder _optionInfo;

        public string OptionInfo => _optionInfo.ToString();

        private int _indentLevel;

        public void SavePresetInfo(XContainer xRootElement)
        {
            var xInfoElement = new XElement(InfoNode);
            xRootElement.Add(xInfoElement);

            xInfoElement.Add(new XElement(PresetVersionNode, PresetVersion));
            xInfoElement.Add(new XElement(PresetAuthorNode, Author));
            xInfoElement.Add(new XElement(PresetTemplateNode, false));
            xInfoElement.Add(new XElement(PresetDescriptionNode, string.IsNullOrEmpty(Description) ? "None" : Description));
            //xInfoElement.Add(new XElement(PresetDateNode, DateTime.Now));
        }

        public void LoadPresetInfo(XContainer xRootNode)
        {
            _filterInfo = new StringBuilder();
            _optionInfo = new StringBuilder();

            var xInfoNode = xRootNode?.Element(InfoNode);
            if (xInfoNode == null)
                return;

            Version = xInfoNode.Element(PresetVersionNode)?.Value;
            Author = xInfoNode.Element(PresetAuthorNode)?.Value;
            //Date = xRootNode.Element(PresetDateNode)?.Value;
            Description = xInfoNode.Element(PresetDescriptionNode)?.Value;
            Preset.LoadBoolean(xInfoNode, PresetTemplateNode, b => IsTemplate = b);

            var xPresetNode = xInfoNode.Parent;

            var xFilters = xPresetNode?.Element(Preset.FilterNode);
            if (xFilters == null)
                return;

            _indentLevel = 0;
            LoadPresetInfoRecursive(xFilters, _filterInfo);

            var xOptions = xPresetNode.Element(Preset.OptionNode);
            if (xOptions == null)
                return;

            _indentLevel = 0;
            LoadPresetInfoRecursive(xOptions, _optionInfo);
        }

        private void LoadPresetInfoRecursive(XContainer xRootNode, StringBuilder sb)
        {
            foreach (var element in xRootNode.Elements())
            {
                var indentString = " ".Repeat(_indentLevel);
                sb.AppendLine(element.HasElements
                    ? $"{indentString}{element.Name}"
                    : $"{indentString}{element.Name}: {element.Value}");

                _indentLevel += 4;
                LoadPresetInfoRecursive(element, sb);
            }

            _indentLevel -= 4;
            if (_indentLevel < 0)
                _indentLevel = 0;
        }
    }

    public class Preset
    {
        #region XML_NODES

        public const string RootName = "Preset";
        public const string FilterNode = "Filters";
        private const string TerrainNode = "Terrain";
        private const string TemperatureNode = "Temperature";

        public const string OptionNode = "Options";

        // use / min / max
        private const string MinNode = "Min";

        private const string MaxNode = "Max";

        private const string UseNode = "Use";

        // def
        private const string DefNameNode = "defName";

        // state
        private const string StateNode = "State";

        #endregion XML_NODES

        private readonly UserData _userData;

        public string PresetName { get; }

        public PresetInfo PresetInfo { get; }

        public Preset(string presetName, UserData userData)
        {
            PresetName = presetName;
            _userData = userData;
            PresetInfo = new PresetInfo();
        }

        private XElement GetTopElement(out XDocument xDocument, bool fileMustExist)
        {
            var filePath = PresetManager.FullPresetPathFromPresetName(PresetName, fileMustExist);
            if (string.IsNullOrEmpty(filePath))
                throw new ArgumentException($"[PrepareLanding] presetName ({PresetName}) doesn't lead to a full path.");

            XElement xPreset;
            if (fileMustExist)
            {
                // load the document and check if there's a root node.
                xDocument = XDocument.Load(filePath);
                if (xDocument.Root == null)
                    throw new Exception("No root node");

                // get the root element
                xPreset = xDocument.Element(RootName);
                if (xPreset == null)
                    throw new Exception($"No root node named '{RootName}'");
            }
            else
            {
                // create document
                xDocument = new XDocument();

                // add root node
                xPreset = new XElement(RootName);
                xDocument.Add(xPreset);
            }

            return xPreset;
        }

        public void LoadPresetInfo()
        {
            XDocument xDocument;
            var xRootNode = GetTopElement(out xDocument, true);
            if (xRootNode == null)
                return;

            PresetInfo.LoadPresetInfo(xRootNode);
        }

        public void LoadPreset(bool loadOptions = true)
        {
            XDocument xDocument;
            var xRootNode = GetTopElement(out xDocument, true);
            if (xRootNode == null)
                return;
            /*
             * Header
             */
            LoadPresetInfo();

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
            if(xTerrain.Element("StoneTypesNumberOnly") == null)
            { 
                LoadMultiThreeStatesOrdered(xTerrain, "Stones", "Stone", _userData.SelectedStoneDefs, _userData.OrderedStoneDefs);
            }
            else
            {
                LoadBoolean(xTerrain, "StoneTypesNumberOnly", b => _userData.StoneTypesNumberOnly = b);
                if (_userData.StoneTypesNumberOnly)
                {
                    int stoneTypesNumber;
                    Load(xTerrain, "StoneTypesNumber", out stoneTypesNumber);
                    _userData.StoneTypesNumber = stoneTypesNumber;
                }
            }
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

            // just check if asked to load options or not.
            if (!loadOptions)
                return;

            LoadBoolean(xOptions, "AllowImpassableHilliness", b => _userData.Options.AllowImpassableHilliness = b);
            LoadBoolean(xOptions, "AllowInvalidTilesForNewSettlement", b => _userData.Options.AllowInvalidTilesForNewSettlement = b);
            LoadBoolean(xOptions, "AllowLiveFiltering", b => _userData.Options.AllowLiveFiltering = b);
            LoadBoolean(xOptions, "BypassMaxHighlightedTiles", b => _userData.Options.BypassMaxHighlightedTiles = b);
            LoadBoolean(xOptions, "DisablePreFilterCheck", b => _userData.Options.DisablePreFilterCheck = b);
            LoadBoolean(xOptions, "DisableTileHighlighting", b => _userData.Options.DisableTileHighlighting = b);
            LoadBoolean(xOptions, "DisableTileBlinking", b => _userData.Options.DisableTileBlinking = b);
            LoadBoolean(xOptions, "ShowDebugTileId", b => _userData.Options.ShowDebugTileId = b);
            LoadBoolean(xOptions, "ShowFilterHeaviness", b => _userData.Options.ShowFilterHeaviness = b);
        }

        public void SavePreset(string description = null, bool saveOptions = false)
        {
            try
            {
                XDocument xDocument;
                var xRoot = GetTopElement(out xDocument, false);
                if (xRoot == null)
                    return;

                // preset info
                PresetInfo.SavePresetInfo(xRoot);

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
                if (_userData.StoneTypesNumberOnly)
                {
                    SaveBoolean(xTerrainFilters, "StoneTypesNumberOnly", _userData.StoneTypesNumberOnly);
                    Save(xTerrainFilters, "StoneTypesNumber", _userData.StoneTypesNumber);
                }
                else
                {
                    SaveMultiThreeStatesOrdered(xTerrainFilters, "Stones", "Stone", _userData.SelectedStoneDefs, _userData.OrderedStoneDefs);
                }
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

                // save options if specifically asked for
                if (saveOptions)
                {
                    SaveBoolean(xOption, "AllowImpassableHilliness", _userData.Options.AllowImpassableHilliness);
                    SaveBoolean(xOption, "AllowInvalidTilesForNewSettlement",
                        _userData.Options.AllowInvalidTilesForNewSettlement);
                    SaveBoolean(xOption, "AllowLiveFiltering", _userData.Options.AllowLiveFiltering);
                    SaveBoolean(xOption, "BypassMaxHighlightedTiles", _userData.Options.BypassMaxHighlightedTiles);
                    SaveBoolean(xOption, "DisablePreFilterCheck", _userData.Options.DisablePreFilterCheck);
                    SaveBoolean(xOption, "DisableTileHighlighting", _userData.Options.DisableTileHighlighting);
                    SaveBoolean(xOption, "DisableTileBlinking", _userData.Options.DisableTileBlinking);
                    SaveBoolean(xOption, "ShowDebugTileId", _userData.Options.ShowDebugTileId);
                    SaveBoolean(xOption, "ShowFilterHeaviness", _userData.Options.ShowFilterHeaviness);
                }

                // save the document
                xDocument.Save(PresetManager.FullPresetPathFromPresetName(PresetName, false));
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
            return xChild == null ? default(MultiCheckboxState) : LoadEnum<MultiCheckboxState>(xChild, StateNode);
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

        internal static bool LoadBoolean(XContainer xParent, string entryName, Action<bool> actionSet)
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
            if (UserData.IsDefDictInDefaultState(dict))
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
            if (UserData.IsDefDictInDefaultState(dict))
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

        private static void Save<T>(XContainer xParent, string elementName, T value) where T : IConvertible
        {
            var result = (string) Convert.ChangeType(value, typeof(string));
            var xElement = new XElement(elementName, result);
            xParent.Add(xElement);
        }

        #endregion SAVE_PRESET

    }

    public class PresetManager
    {
        public const string DefaultPresetName = "PLPreset";

        public const string DefaultExtension = ".XML";

        private readonly List<FileInfo> _allPresetFiles = new List<FileInfo>();

        private readonly UserData _userData;

        private readonly Dictionary<string, Preset> _presetCache = new Dictionary<string, Preset>();

        public List<FileInfo> AllPresetFiles
        {
            get
            {
                if (_allPresetFiles.Count == 0 && !IsPresetDirectoryEmpty())
                    RenewPresetFileCache();

                return _allPresetFiles;
            }
        }

        /// <summary>
        /// Name of the preset folder.
        /// </summary>
        public static string FolderName => PrepareLanding.Instance.ModIdentifier;

        public static string PresetTemplateFolder => Path.Combine(PrepareLanding.Instance.ModFolder, "Presets");

        public PresetManager(UserData userData)
        {
            _userData = userData;

            // just make sure the preset dir exists by calling the PresetFolder Property
            Log.Message($"[PrepareLanding] Preset folder is at: {PresetFolder}");

            // location of the preset templates, provided de facto with the mod
            Log.Message($"[PrepareLanding] Preset template folder is at: {PresetTemplateFolder}");

            CopyFromTemplateFolderToPresetFolder(PresetTemplateFolder, PresetFolder);

            PreloadPresets();
        }

        private static void CopyFromTemplateFolderToPresetFolder(string sourceFolder, string destFolder)
        {
            if (!Directory.Exists(sourceFolder) || !Directory.Exists(destFolder))
                return;

            foreach (var sourceFile in Directory.GetFiles(sourceFolder))
            {
                var sourceFileName = Path.GetFileName(sourceFile);
                if (string.IsNullOrEmpty(sourceFileName))
                    continue;

                var destFilePath = Path.Combine(destFolder, sourceFileName);
                try
                {
                    if (!Md5HashEquals(sourceFile, destFilePath))
                    {
                        File.Delete(destFilePath);
                        File.Copy(sourceFile, destFilePath);
                    }
                }
                catch (Exception e)
                {
                    Log.Error($"[PrepareLanding] An error occured in CopyFromTemplateFolderToPresetFolder.\n\t:Source: {sourceFile}\n\tDest:{destFilePath}\n\tError: {e}");
                }
            }
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
                // create the preset or load it if it already exists
                var preset = !_presetCache.ContainsKey(presetName) ? new Preset(presetName, _userData) : _presetCache[presetName];

                preset.LoadPresetInfo();
            }
            catch (Exception e)
            {
                Messages.Message("[PrepareLanding] Error loading preset info.", MessageSound.RejectInput);
                Log.Error($"[PrepareLanding] LoadPresetInfo error: {e}");
                throw;
            }
        }

        public bool LoadPreset(string presetName, bool forceReload = false)
        {
            bool successfulLoad;

            if (string.IsNullOrEmpty(presetName))
                return false;

            var filePath = GetPresetFilePath(presetName);

            if (!File.Exists(filePath))
                return false;

            if (_presetCache.ContainsKey(presetName) && !forceReload)
                return false;

            // disable live filtering as we are gonna change some filters on the fly
            var liveFilterting = _userData.Options.AllowLiveFiltering;
            _userData.Options.AllowLiveFiltering = false;

            // reset all filter states into their default state
            _userData.ResetAllFields();

            try
            {
                // reload the preset if it was already in the cache
                Preset preset;
                if (_presetCache.TryGetValue(presetName, out preset))
                {
                    preset.LoadPreset();

                    //reload its info
                    preset.LoadPresetInfo();
                }
                else
                {
                    // create the preset and load it
                    preset = new Preset(presetName, _userData);
                    preset.LoadPreset();
                    preset.LoadPresetInfo();

                    // add it to the cache
                    _presetCache.Add(presetName, preset);
                }

                // renew file cache
                RenewPresetFileCache();

                successfulLoad = true;
            }
            catch (Exception e)
            {
                Messages.Message("[PrepareLanding] Error loading preset.", MessageSound.RejectInput);
                Log.Error($"Failed to load preset file '{filePath}'. Error:\n\t{e}\n\t{e.Message}");

                successfulLoad = false;
            }
            finally
            {
                // re-enable live filtering.
                _userData.Options.AllowLiveFiltering = liveFilterting;
            }

            return successfulLoad;
        }

        public bool SavePreset(string presetName, string description = null, string author = null, bool saveOptions = false)
        {
            bool successfulSave;

            if (string.IsNullOrEmpty(presetName))
                return false;

            var filePath = GetPresetFilePath(presetName);

            // just check we aren't trying to overwrite a template preset
            if (_presetCache.ContainsKey(presetName))
            {
                if (_presetCache[presetName].PresetInfo.IsTemplate)
                {
                    Messages.Message("[PrepareLanding] It is not allowed to overwrite a template preset.",
                        MessageSound.RejectInput);
                    return false;
                }
            }

            try
            {
                // create preset and start save
                var preset = new Preset(presetName, _userData);
                preset.PresetInfo.Description = description;
                preset.PresetInfo.Author = author;
                preset.SavePreset(description, saveOptions);

                // reload the preset now if it was already in the cache
                if (_presetCache.ContainsKey(presetName))
                {
                    LoadPreset(presetName, true);
                }
                else
                {
                    // it's a new preset: add it to the cache
                    _presetCache.Add(presetName, preset);

                    // load its info
                    preset.LoadPresetInfo();

                    // renew file cache
                    RenewPresetFileCache();
                }

                successfulSave = true;
            }
            catch (Exception e)
            {
                // remove preset in cache on exception
                if (_presetCache.ContainsKey(presetName))
                    _presetCache.Remove(presetName);

                Messages.Message("[PrepareLanding] Failed to save preset file.", MessageSound.RejectInput);
                Log.Error($"[PrepareLanding] Failed to save preset file '{filePath}'. error:\n\t{e}\n\t{e.Message}");

                successfulSave = false;
            }

            return successfulSave;
        }

        private void PreloadPresets()
        {
            foreach (var presetFile in AllPresetFiles)
            {
                var presetName = Path.GetFileNameWithoutExtension(presetFile.Name);
                var preset = new Preset(presetName, _userData);
                
                _presetCache.Add(presetName, preset);
                LoadPresetInfo(presetName, true);
            }
        }

        #region FILE_DIR_HANDLING

        /// <summary>
        /// Full path of the preset folder (from user folder, not the mod one).
        /// </summary>
        public static string PresetFolder
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
            return Directory.GetFiles(PresetFolder).Length == 0;
        }

        private static string GetPresetFilePath(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
                throw new ArgumentException("[PrepareLanding] GetPresetFilePath: filename is null");

            var filePath = Path.Combine(PresetFolder, fileName);

            // file extension checking, just being precautious
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

        public static string FullPresetPathFromPresetName(string presetName, bool fileMustExists)
        {
            var filePath = GetPresetFilePath(presetName);
            if(fileMustExists)
                return File.Exists(filePath) ? filePath : null;

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
            var dirInfo = new DirectoryInfo(PresetFolder);

            _allPresetFiles.Clear();

            var orderedFiles = from file in dirInfo.GetFiles()
                         where string.Compare(file.Extension, DefaultExtension, StringComparison.OrdinalIgnoreCase) == 0
                         orderby file.LastWriteTime descending
                         select file;

            _allPresetFiles.AddRange(orderedFiles);
        }

        public string NextPresetFileName
        {
            get
            {
                var counter = 0;
                string fileName;
                do
                {
                    fileName = $"{DefaultPresetName}_{counter}{DefaultExtension}";
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

        public static bool Md5HashEquals(string filePath1, string filePath2)
        {
            if (!File.Exists(filePath1) || !File.Exists(filePath2))
                return false;

            byte[] file1Hash;
            byte[] file2Hash;

            using (var md5 = MD5.Create())
            {
                using (var stream = File.OpenRead(filePath1))
                {
                    file1Hash = md5.ComputeHash(stream);
                }
                
                using (var stream = File.OpenRead(filePath2))
                {
                    file2Hash = md5.ComputeHash(stream);
                }
            }

            if (file1Hash.Length != file2Hash.Length)
                return false;

            for (var i = 0; i < file1Hash.Length; i++)
            {
                if (file1Hash[i] != file2Hash[i])
                    return false;
            }

            return true;
        }

        #endregion FILE_DIR_HANDLING
    }
}