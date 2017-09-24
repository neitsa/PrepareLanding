using System;
using System.Collections.Generic;
using System.Globalization;
using System.Xml.Linq;
using PrepareLanding.GameData;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace PrepareLanding.Presets
{
    public class Preset
    {
        private readonly GameData.GameData _gameData;

        public Preset(string presetName, GameData.GameData gameData)
        {
            PresetName = presetName;
            _gameData = gameData;
            PresetInfo = new PresetInfo();
        }

        public PresetInfo PresetInfo { get; }

        public string PresetName { get; }

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

            _gameData.UserData.ChosenBiome = LoadDef<BiomeDef>(xTerrain, "Biome") as BiomeDef;
            _gameData.UserData.ChosenHilliness = LoadEnum<Hilliness>(xTerrain, "Hilliness");
            LoadMultiThreeStates(xTerrain, "Roads", "Road", _gameData.UserData.SelectedRoadDefs);
            LoadMultiThreeStates(xTerrain, "Rivers", "River", _gameData.UserData.SelectedRiverDefs);
            LoadUsableMinMax(xTerrain, "CurrentMovementTime", _gameData.UserData.CurrentMovementTime);
            LoadUsableMinMax(xTerrain, "SummerMovementTime", _gameData.UserData.SummerMovementTime);
            LoadUsableMinMax(xTerrain, "WinterMovementTime", _gameData.UserData.WinterMovementTime);
            if (xTerrain.Element("StoneTypesNumberOnly") == null)
            {
                LoadMultiThreeStatesOrdered(xTerrain, "Stones", "Stone", _gameData.UserData.SelectedStoneDefs,
                    _gameData.UserData.OrderedStoneDefs);
            }
            else
            {
                LoadBoolean(xTerrain, "StoneTypesNumberOnly", b => _gameData.UserData.StoneTypesNumberOnly = b);
                if (_gameData.UserData.StoneTypesNumberOnly)
                {
                    int stoneTypesNumber;
                    Load(xTerrain, "StoneTypesNumber", out stoneTypesNumber);
                    _gameData.UserData.StoneTypesNumber = stoneTypesNumber;
                }
            }
            _gameData.UserData.ChosenCoastalTileState = LoadThreeState(xTerrain, "CoastalTile");
            _gameData.UserData.ChosenCoastalLakeTileState = LoadThreeState(xTerrain, "CoastalLakeTile");
            LoadUsableMinMax(xTerrain, "Elevation", _gameData.UserData.Elevation);
            LoadUsableMinMax(xTerrain, "TimeZone", _gameData.UserData.TimeZone);


            // temperature
            var xTemperature = xFilters.Element(TemperatureNode);
            if (xTemperature == null)
                return;

            LoadUsableMinMax(xTemperature, "AverageTemperature", _gameData.UserData.AverageTemperature);
            LoadUsableMinMax(xTemperature, "SummerTemperature", _gameData.UserData.SummerTemperature);
            LoadUsableMinMax(xTemperature, "WinterTemperature", _gameData.UserData.WinterTemperature);
            LoadMinMaxFromRestrictedList(xTemperature, "GrowingPeriod", _gameData.UserData.GrowingPeriod);
            LoadUsableMinMax(xTemperature, "RainFall", _gameData.UserData.RainFall);
            _gameData.UserData.ChosenAnimalsCanGrazeNowState = LoadThreeState(xTemperature, "AnimalsCanGrazeNow");
            LoadMostLeastItem(xTemperature, "MostLeastFeature", _gameData.UserData.MostLeastItem);

            /*
             * Options
             */
            var xOptions = xRootNode.Element(OptionNode);
            if (xOptions == null)
                return;

            // just check if asked to load options or not.
            if (!loadOptions)
                return;

            LoadBoolean(xOptions, "AllowImpassableHilliness", b => _gameData.UserData.Options.AllowImpassableHilliness = b);
            LoadBoolean(xOptions, "AllowInvalidTilesForNewSettlement",
                b => _gameData.UserData.Options.AllowInvalidTilesForNewSettlement = b);
            LoadBoolean(xOptions, "AllowLiveFiltering", b => _gameData.UserData.Options.AllowLiveFiltering = b);
            LoadBoolean(xOptions, "BypassMaxHighlightedTiles", b => _gameData.UserData.Options.BypassMaxHighlightedTiles = b);
            LoadBoolean(xOptions, "DisablePreFilterCheck", b => _gameData.UserData.Options.DisablePreFilterCheck = b);
            LoadBoolean(xOptions, "ResetAllFieldsOnNewGeneratedWorld", b => _gameData.UserData.Options.ResetAllFieldsOnNewGeneratedWorld = b);
            LoadBoolean(xOptions, "DisableTileHighlighting", b => _gameData.UserData.Options.DisableTileHighlighting = b);
            LoadBoolean(xOptions, "DisableTileBlinking", b => _gameData.UserData.Options.DisableTileBlinking = b);
            LoadBoolean(xOptions, "ShowDebugTileId", b => _gameData.UserData.Options.ShowDebugTileId = b);
            LoadBoolean(xOptions, "ShowFilterHeaviness", b => _gameData.UserData.Options.ShowFilterHeaviness = b);
        }

        public void LoadPresetInfo()
        {
            XDocument xDocument;
            var xRootNode = GetTopElement(out xDocument, true);
            if (xRootNode == null)
                return;

            PresetInfo.LoadPresetInfo(xRootNode);
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

                SaveDef(xTerrainFilters, "Biome", _gameData.UserData.ChosenBiome);
                SaveHilliness(xTerrainFilters, "Hilliness", _gameData.UserData.ChosenHilliness);
                SaveMultiThreeStates(xTerrainFilters, "Roads", "Road", _gameData.UserData.SelectedRoadDefs);
                SaveMultiThreeStates(xTerrainFilters, "Rivers", "River", _gameData.UserData.SelectedRiverDefs);
                SaveUsableMinMax(xTerrainFilters, "CurrentMovementTime", _gameData.UserData.CurrentMovementTime);
                SaveUsableMinMax(xTerrainFilters, "SummerMovementTime", _gameData.UserData.SummerMovementTime);
                SaveUsableMinMax(xTerrainFilters, "WinterMovementTime", _gameData.UserData.WinterMovementTime);
                if (_gameData.UserData.StoneTypesNumberOnly)
                {
                    SaveBoolean(xTerrainFilters, "StoneTypesNumberOnly", _gameData.UserData.StoneTypesNumberOnly);
                    Save(xTerrainFilters, "StoneTypesNumber", _gameData.UserData.StoneTypesNumber);
                }
                else
                {
                    SaveMultiThreeStatesOrdered(xTerrainFilters, "Stones", "Stone", _gameData.UserData.SelectedStoneDefs,
                        _gameData.UserData.OrderedStoneDefs);
                }
                SaveThreeState(xTerrainFilters, "CoastalTile", _gameData.UserData.ChosenCoastalTileState);
                SaveThreeState(xTerrainFilters, "CoastalLakeTile", _gameData.UserData.ChosenCoastalLakeTileState);
                SaveUsableMinMax(xTerrainFilters, "Elevation", _gameData.UserData.Elevation);
                SaveUsableMinMax(xTerrainFilters, "TimeZone", _gameData.UserData.TimeZone);

                // Temperature
                var xTemperatureFilters = new XElement("Temperature");
                xFilter.Add(xTemperatureFilters);

                SaveUsableMinMax(xTemperatureFilters, "AverageTemperature", _gameData.UserData.AverageTemperature);
                SaveUsableMinMax(xTemperatureFilters, "SummerTemperature", _gameData.UserData.SummerTemperature);
                SaveUsableMinMax(xTemperatureFilters, "WinterTemperature", _gameData.UserData.WinterTemperature);
                SaveMinMaxFromRestrictedList(xTemperatureFilters, "GrowingPeriod", _gameData.UserData.GrowingPeriod);
                SaveUsableMinMax(xTemperatureFilters, "RainFall", _gameData.UserData.RainFall);
                SaveThreeState(xTerrainFilters, "AnimalsCanGrazeNow", _gameData.UserData.ChosenAnimalsCanGrazeNowState);
                SaveMostLeastItem(xTemperatureFilters, "MostLeastFeature", _gameData.UserData.MostLeastItem);

                /*
                 * Options
                 */
                var xOption = new XElement(OptionNode);
                xRoot.Add(xOption);

                // save options if specifically asked for
                if (saveOptions)
                {
                    SaveBoolean(xOption, "AllowImpassableHilliness", _gameData.UserData.Options.AllowImpassableHilliness);
                    SaveBoolean(xOption, "AllowInvalidTilesForNewSettlement",
                        _gameData.UserData.Options.AllowInvalidTilesForNewSettlement);
                    SaveBoolean(xOption, "AllowLiveFiltering", _gameData.UserData.Options.AllowLiveFiltering);
                    SaveBoolean(xOption, "BypassMaxHighlightedTiles", _gameData.UserData.Options.BypassMaxHighlightedTiles);
                    SaveBoolean(xOption, "DisablePreFilterCheck", _gameData.UserData.Options.DisablePreFilterCheck);
                    SaveBoolean(xOption, "ResetAllFieldsOnNewGeneratedWorld", _gameData.UserData.Options.ResetAllFieldsOnNewGeneratedWorld);
                    SaveBoolean(xOption, "DisableTileHighlighting", _gameData.UserData.Options.DisableTileHighlighting);
                    SaveBoolean(xOption, "DisableTileBlinking", _gameData.UserData.Options.DisableTileBlinking);
                    SaveBoolean(xOption, "ShowDebugTileId", _gameData.UserData.Options.ShowDebugTileId);
                    SaveBoolean(xOption, "ShowFilterHeaviness", _gameData.UserData.Options.ShowFilterHeaviness);
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

        // most/least 
        private const string MostLeastItemFeatureNode = "Feature";

        private const string MostLeastItemFeatureTypeNode = "FeatureType";

        private const string MostLeastItemNumberOfItemsNode = "NumberOfItems";

        // def
        private const string DefNameNode = "defName";

        // state
        private const string StateNode = "State";

        #endregion XML_NODES

        #region LOAD_PRESET

        private Def LoadDef<T>(XContainer xParent, string elementName) where T : Def
        {
            var xFoundElement = xParent.Element(elementName);
            if (xFoundElement == null)
                return null;

            switch (typeof(T).Name)
            {
                case nameof(BiomeDef):
                    foreach (var biomeDef in _gameData.DefData.BiomeDefs)
                        if (string.Equals(biomeDef.defName, xFoundElement.Value, StringComparison.OrdinalIgnoreCase))
                            return biomeDef;
                    break;

                case nameof(RoadDef):
                    foreach (var roadDef in _gameData.DefData.RoadDefs)
                        if (string.Equals(roadDef.defName, xFoundElement.Value, StringComparison.OrdinalIgnoreCase))
                            return roadDef;
                    break;

                case nameof(RiverDef):
                    foreach (var riverDef in _gameData.DefData.RiverDefs)
                        if (string.Equals(riverDef.defName, xFoundElement.Value, StringComparison.OrdinalIgnoreCase))
                            return riverDef;
                    break;

                case nameof(ThingDef):
                    // TODO: be wary that multiple things might be ThinDef, so better check if its a stone before parsing StoneDefs.
                    foreach (var stoneDef in _gameData.DefData.StoneDefs)
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

            return (T) Enum.Parse(typeof(T), xFoundElement.Value, true);
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

            result = (T) Convert.ChangeType(xFoundElement.Value, typeof(T));
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
            // note: if xChild is null do NOT return default(MultiCheckboxState) because the default state will be ON!
            return xChild == null ? MultiCheckboxState.Partial : LoadEnum<MultiCheckboxState>(xChild, StateNode);
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

        private static void LoadMostLeastItem(XContainer xParent, string elementName, MostLeastItem item)
        {
            var xFoundElement = xParent.Element(elementName);
            if (xFoundElement == null)
                return;

            var feature = LoadEnum<MostLeastFeature>(xFoundElement, MostLeastItemFeatureNode);
            var featureType = LoadEnum<MostLeastType>(xFoundElement, MostLeastItemFeatureTypeNode);

            int numItems;
            if (!Load(xFoundElement, MostLeastItemNumberOfItemsNode, out numItems))
                return;

            item.Feature = feature;
            item.FeatureType = featureType;
            item.NumberOfItems = numItems;
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

        private static void SaveMostLeastItem(XContainer xRoot, string elementName, MostLeastItem item)
        {
            if (item.IsInDefaultState)
                return;

            var xElement = new XElement(elementName);
            xRoot.Add(xElement);

            xElement.Add(new XElement(MostLeastItemFeatureNode, item.Feature.ToString()));
            xElement.Add(new XElement(MostLeastItemFeatureTypeNode, item.FeatureType.ToString()));
            xElement.Add(new XElement(MostLeastItemNumberOfItemsNode, item.NumberOfItems));
        }

        private static void Save<T>(XContainer xParent, string elementName, T value) where T : IConvertible
        {
            var result = (string) Convert.ChangeType(value, typeof(string));
            var xElement = new XElement(elementName, result);
            xParent.Add(xElement);
        }

        #endregion SAVE_PRESET
    }
}