using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Xml.Linq;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace PrepareLanding
{
    public class PresetManager
    {
        public const string PresetVersion = "1.0";

        public const string DefaultExtension = ".XML";

        private const string RootName = "Preset";
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

        public PresetManager(PrepareLandingUserData userData)
        {
            _userData = userData;
        }

        public string FolderName => PrepareLanding.Instance.ModIdentifier;

        public void LoadFilterPreset(string presetName, PrepareLandingUserData userData)
        {
            var filePath = GetPresetFilePath(presetName);

            if (!File.Exists(filePath))
                return;

            // disable live filtering as we are gonna change some filters on the fly
            var liveFilterting = userData.Options.AllowLiveFiltering;
            userData.Options.AllowLiveFiltering = false;

            // reset everything into it's default state
            _userData.ResetAllFields();

            try
            {
                var xDocument = XDocument.Load(filePath);
                if (xDocument.Root == null)
                    throw new Exception("No root node");

                /*
                 * Filters
                 */

                var xFilters = xDocument.Element(RootName)?.Element(FilterNode);

                // terrain
                var xTerrain = xFilters?.Element(TerrainNode);
                if (xTerrain == null)
                    return;

                _userData.ChosenBiome = LoadDef<BiomeDef>(xTerrain, "Biome") as BiomeDef;
                _userData.ChosenHilliness = LoadEnum<Hilliness>(xTerrain, "Hilliness");
                LoadMultiThreeStates(xTerrain, "Roads", "Road", userData.SelectedRoadDefs);
                LoadMultiThreeStates(xTerrain, "Rivers", "River", userData.SelectedRiverDefs);
                LoadUsableMinMax(xTerrain, "CurrentMovementTime", userData.CurrentMovementTime);
                LoadUsableMinMax(xTerrain, "SummerMovementTime", userData.SummerMovementTime);
                LoadUsableMinMax(xTerrain, "WinterMovementTime", userData.WinterMovementTime);
                LoadMultiThreeStatesOrdered(xTerrain, "Stones", "Stone", userData.SelectedStoneDefs,
                    userData.OrderedStoneDefs);
                userData.ChosenCoastalTileState = LoadThreeState(xTerrain, "CoastalTile");
                LoadUsableMinMax(xTerrain, "Elevation", userData.Elevation);
                LoadUsableMinMax(xTerrain, "TimeZone", userData.TimeZone);


                // temperature
                var xTemperature = xFilters.Element(TemperatureNode);
                if (xTemperature == null)
                    return;

                LoadUsableMinMax(xTemperature, "AverageTemperature", userData.AverageTemperature);
                LoadUsableMinMax(xTemperature, "SummerTemperature", userData.SummerTemperature);
                LoadUsableMinMax(xTemperature, "WinterTemperature", userData.WinterTemperature);
                LoadMinMaxFromRestrictedList(xTemperature, "GrowingPeriod", userData.GrowingPeriod);
                LoadUsableMinMax(xTemperature, "RainFall", userData.RainFall);
                userData.ChosenAnimalsCanGrazeNowState = LoadThreeState(xTemperature, "AnimalsCanGrazeNow");
            }
            catch (Exception e)
            {
                Log.Error($"Failed to load preset file '{filePath}'. Error:\n\t{e}\n\t{e.Message}");
                //throw;
            }
            finally
            {
                // re-enable live filtering.
                userData.Options.AllowLiveFiltering = liveFilterting;
            }

            // todo: check root xDoc.Root == null
        }

        public void SaveFilterPreset(string presetName, PrepareLandingUserData userData)
        {
            var filePath = GetPresetFilePath(presetName);

            if (File.Exists(filePath))
            {
                // TODO ask if overwrite
            }

            try
            {
                var xDocument = new XDocument();

                var xRoot = new XElement(RootName);
                xDocument.Add(xRoot);

                xRoot.Add(new XElement("Version", PresetVersion));
                xRoot.Add(new XElement("PresetTemplate", false));
                xRoot.Add(new XElement("Description", "My super description"));

                /*
                 * filters
                 */
                var xFilter = new XElement(FilterNode);
                xRoot.Add(xFilter);

                // Terrain
                var xTerrainFilters = new XElement(TerrainNode);
                xFilter.Add(xTerrainFilters);

                SaveDef(xTerrainFilters, "Biome", userData.ChosenBiome);
                SaveHilliness(xTerrainFilters, "Hilliness", userData.ChosenHilliness);
                SaveMultiThreeStates(xTerrainFilters, "Roads", "Road", userData.SelectedRoadDefs);
                SaveMultiThreeStates(xTerrainFilters, "Rivers", "River", userData.SelectedRiverDefs);
                SaveUsableMinMax(xTerrainFilters, "CurrentMovementTime", userData.CurrentMovementTime);
                SaveUsableMinMax(xTerrainFilters, "SummerMovementTime", userData.SummerMovementTime);
                SaveUsableMinMax(xTerrainFilters, "WinterMovementTime", userData.WinterMovementTime);
                SaveMultiThreeStatesOrdered(xTerrainFilters, "Stones", "Stone", userData.SelectedStoneDefs,
                    userData.OrderedStoneDefs);
                SaveThreeState(xTerrainFilters, "CoastalTile", userData.ChosenCoastalTileState);
                SaveUsableMinMax(xTerrainFilters, "Elevation", userData.Elevation);
                SaveUsableMinMax(xTerrainFilters, "TimeZone", userData.TimeZone);

                // Temperature
                var xTemperatureFilters = new XElement("Temperature");
                xFilter.Add(xTemperatureFilters);

                SaveUsableMinMax(xTemperatureFilters, "AverageTemperature", userData.AverageTemperature);
                SaveUsableMinMax(xTemperatureFilters, "SummerTemperature", userData.SummerTemperature);
                SaveUsableMinMax(xTemperatureFilters, "WinterTemperature", userData.WinterTemperature);
                SaveMinMaxFromRestrictedList(xTemperatureFilters, "GrowingPeriod", userData.GrowingPeriod);
                SaveUsableMinMax(xTemperatureFilters, "RainFall", userData.RainFall);
                SaveThreeState(xTerrainFilters, "AnimalsCanGrazeNow", userData.ChosenAnimalsCanGrazeNowState);

                /*
                 * Options
                 */
                var xOption = new XElement(OptionNode);
                xRoot.Add(xOption);

                SaveBoolean(xOption, "AllowImpassableHilliness", userData.Options.AllowImpassableHilliness);
                SaveBoolean(xOption, "AllowInvalidTilesForNewSettlement",
                    userData.Options.AllowInvalidTilesForNewSettlement);
                SaveBoolean(xOption, "AllowLiveFiltering", userData.Options.AllowLiveFiltering);
                SaveBoolean(xOption, "BypassMaxHighlightedTiles", userData.Options.BypassMaxHighlightedTiles);
                SaveBoolean(xOption, "DisablePreFilterCheck", userData.Options.DisablePreFilterCheck);
                SaveBoolean(xOption, "DisableTileBlinking", userData.Options.DisableTileBlinking);
                SaveBoolean(xOption, "ShowDebugTileId", userData.Options.ShowDebugTileId);
                SaveBoolean(xOption, "ShowFilterHeaviness", userData.Options.ShowFilterHeaviness);

                // save
                xDocument.Save(filePath, SaveOptions.None);
            }
            catch (Exception e)
            {
                Log.Error($"Failed to save preset file '{filePath}'. error:\n\t{e}\n\t{e.Message}");
            }
        }

        public void TestLoad()
        {
            LoadFilterPreset("test.XML", PrepareLanding.Instance.UserData);
        }

        public void TestSave()
        {
            SaveFilterPreset("test.XML", PrepareLanding.Instance.UserData);
        }

        private string GetPresetFilePath(string fileName)
        {
            var folderPath = Path.Combine(GenFilePaths.SaveDataFolderPath, FolderName);
            var directoryInfo = new DirectoryInfo(folderPath);
            if (!directoryInfo.Exists)
            {
                Log.Message($"[PrepareLanding] Creating mod folder at: '{folderPath}'.");
                directoryInfo.Create();
            }

            // file extension checking, just being precautious
            var filePath = Path.Combine(folderPath, fileName);
            if (!Path.HasExtension(filePath))
            {
                filePath = Path.ChangeExtension(filePath, DefaultExtension);
            }
            else
            {
                var extension = Path.GetExtension(filePath);
                if (extension != DefaultExtension)
                    filePath = Path.ChangeExtension(filePath, DefaultExtension);
            }

            return filePath;
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
                    Log.Message("Unknown defType");
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
}