using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using JetBrains.Annotations;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace PrepareLanding
{
    /// <summary>
    ///     Class used to keep user choices (from the main GUI window) and various definitions (<see cref="Verse.Def" />) that
    ///     are used throughout the mod.
    /// </summary>
    public class PrepareLandingUserData : INotifyPropertyChanged
    {
        /// <summary>
        ///     Class constructor.
        /// </summary>
        public PrepareLandingUserData(PrepareLandingFilterOptions options)
        {
            // get alerted when RimWorld has loaded its definition (Defs) files
            PrepareLanding.Instance.OnDefsLoaded += ExecuteOnDefsLoaded;

            // get alerted when RimWorld has finished generating the world
            PrepareLanding.Instance.OnWorldGenerated += ExecuteOnWorldGenerated;

            // save options
            Options = options;

            // register to the option changed event
            Options.PropertyChanged += OptionChanged;

            PresetManager = new PresetManager(this);
        }

        public PresetManager PresetManager { get; }

        /// <summary>
        ///     Filter Options (from the GUI window 'options' tab).
        /// </summary>
        public PrepareLandingFilterOptions Options { get; }

        /// <summary>
        ///     All biome definitions (<see cref="BiomeDef" />) from RimWorld.
        /// </summary>
        public ReadOnlyCollection<BiomeDef> BiomeDefs => _biomeDefs.AsReadOnly();

        /// <summary>
        ///     All "stone" definitions from RimWorld.
        /// </summary>
        /// <remarks>
        ///     Note that stone types (e.g Marble, Granite, etc. are <see cref="ThingDef" /> and have no particular
        ///     definition).
        /// </remarks>
        public ReadOnlyCollection<ThingDef> StoneDefs => _stoneDefs.AsReadOnly();

        /// <summary>
        ///     All road definitions (<see cref="RoadDef" />) from RimWorld.
        /// </summary>
        public ReadOnlyCollection<RoadDef> RoadDefs => _roadDefs.AsReadOnly();

        /// <summary>
        ///     All river definitions (<see cref="RiverDef" />) from RimWorld.
        /// </summary>
        public ReadOnlyCollection<RiverDef> RiverDefs => _riverDefs.AsReadOnly();

        /// <summary>
        ///     All known hilliness (<see cref="Hilliness" />) from RimWorld.
        /// </summary>
        public ReadOnlyCollection<Hilliness> HillinessCollection => _hillinesses.AsReadOnly();

        /// <summary>
        ///     Current user selected biome.
        /// </summary>
        public BiomeDef ChosenBiome
        {
            get { return _chosenBiome; }
            set
            {
                if (value == _chosenBiome)
                    return;

                _chosenBiome = value;
                OnPropertyChanged(nameof(ChosenBiome));
            }
        }

        /// <summary>
        ///     Current user selected hilliness.
        /// </summary>
        public Hilliness ChosenHilliness
        {
            get { return _chosenHilliness; }
            set
            {
                if (value == _chosenHilliness)
                    return;

                _chosenHilliness = value;
                OnPropertyChanged(nameof(ChosenHilliness));
            }
        }

        /// <summary>
        ///     Current user choice for the tiles coastal state.
        /// </summary>
        public MultiCheckboxState ChosenCoastalTileState
        {
            get { return _chosenCoastalTileState; }
            set
            {
                if (value == _chosenCoastalTileState)
                    return;

                _chosenCoastalTileState = value;
                OnPropertyChanged(nameof(ChosenCoastalTileState));
            }
        }

        /// <summary>
        ///     Current user choice for the "Animal Can Graze Now" state.
        /// </summary>
        public MultiCheckboxState ChosenAnimalsCanGrazeNowState
        {
            get { return _chosenAnimalsCanGrazeNowState; }
            set
            {
                if (value == _chosenAnimalsCanGrazeNowState)
                    return;

                _chosenAnimalsCanGrazeNowState = value;
                OnPropertyChanged(nameof(ChosenAnimalsCanGrazeNowState));
            }
        }

        /// <summary>
        ///     If True, filter only tiles with a given number of stone types (whatever the stone types are).
        /// </summary>
        public bool StoneTypesNumberOnly
        {
            get { return _stoneTypesNumberOnly; }
            set
            {
                if (value == _stoneTypesNumberOnly)
                    return;

                _stoneTypesNumberOnly = value;
                OnPropertyChanged(nameof(StoneTypesNumberOnly));
            }
        }

        /// <summary>
        ///     The number of stones per tile to filter when the <see cref="StoneTypesNumberOnly"/> boolean is true.
        /// </summary>
        public int StoneTypesNumber
        {
            get { return _stoneTypesNumber; }
            set
            {
                if (value == _stoneTypesNumber)
                    return;

                _stoneTypesNumber = value;
                OnPropertyChanged(nameof(StoneTypesNumber));
            }
        }

        /// <summary>
        ///     Current user choices for the roads filtering.
        /// </summary>
        public Dictionary<RoadDef, ThreeStateItem> SelectedRoadDefs { get; } =
            new Dictionary<RoadDef, ThreeStateItem>();

        /// <summary>
        ///     Current user choices for the river filtering.
        /// </summary>
        public Dictionary<RiverDef, ThreeStateItem> SelectedRiverDefs { get; } =
            new Dictionary<RiverDef, ThreeStateItem>();

        /// <summary>
        ///     Current user choices for the stone types filtering.
        /// </summary>
        public Dictionary<ThingDef, ThreeStateItem> SelectedStoneDefs { get; } =
            new Dictionary<ThingDef, ThreeStateItem>();

        /// <summary>
        ///     Current order of the stones on the main GUI Window (as this choice order is important).
        /// </summary>
        public List<ThingDef> OrderedStoneDefs { get; } = new List<ThingDef>();

        /// <summary>
        ///     Current user choices for the current movement time.
        /// </summary>
        public UsableMinMaxNumericItem<float> CurrentMovementTime { get; } = new UsableMinMaxNumericItem<float>();

        /// <summary>
        ///     Current user choices for the summer movement time.
        /// </summary>
        public UsableMinMaxNumericItem<float> SummerMovementTime { get; } = new UsableMinMaxNumericItem<float>();

        /// <summary>
        ///     Current user choices for the winter movement time.
        /// </summary>
        public UsableMinMaxNumericItem<float> WinterMovementTime { get; } = new UsableMinMaxNumericItem<float>();

        /// <summary>
        ///     Current user choices for the elevation.
        /// </summary>
        public UsableMinMaxNumericItem<float> Elevation { get; } = new UsableMinMaxNumericItem<float>();

        /// <summary>
        ///     Current user choices for the time zone.
        /// </summary>
        public UsableMinMaxNumericItem<int> TimeZone { get; } = new UsableMinMaxNumericItem<int>();

        /// <summary>
        ///     Current user choices for the average temperature.
        /// </summary>
        public UsableMinMaxNumericItem<float> AverageTemperature { get; } = new UsableMinMaxNumericItem<float>();

        /// <summary>
        ///     Current user choices for the winter temperature.
        /// </summary>
        public UsableMinMaxNumericItem<float> WinterTemperature { get; } = new UsableMinMaxNumericItem<float>();

        /// <summary>
        ///     Current user choices for the summer temperature.
        /// </summary>
        public UsableMinMaxNumericItem<float> SummerTemperature { get; } = new UsableMinMaxNumericItem<float>();

        /// <summary>
        ///     Current user choices for the growing period.
        /// </summary>
        public MinMaxFromRestrictedListItem<Twelfth> GrowingPeriod { get; private set; }

        /// <summary>
        ///     Current user choices for the rain fall.
        /// </summary>
        public UsableMinMaxNumericItem<float> RainFall { get; } = new UsableMinMaxNumericItem<float>();

        /// <summary>
        ///     Other classes can subscribe to this event to be alerted when a user choice changed.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        ///     Reset all fields (user choices on the GUI window) to their default state. Also clear all matching tiles.
        /// </summary>
        public void ResetAllFields()
        {
            // clear the previously matching tiles and highlighted tiles (if any)
            PrepareLanding.Instance.TileFilter.ClearMatchingTiles();

            /*
             * TERRAIN related fields
             */

            _chosenBiome = null;
            _chosenHilliness = Hilliness.Undefined;
            _chosenCoastalTileState = MultiCheckboxState.Partial;
            _chosenAnimalsCanGrazeNowState = MultiCheckboxState.Partial;

            InitSelectedDictionary(_roadDefs, SelectedRoadDefs, nameof(SelectedRoadDefs));
            InitSelectedDictionary(_riverDefs, SelectedRiverDefs, nameof(SelectedRiverDefs));
            InitSelectedDictionary(_stoneDefs, SelectedStoneDefs, nameof(SelectedStoneDefs));

            // patch for the fact that OrderedDictionary<TKey, TValue> doesn't exist in .NET...
            // The list is reorder-able but the dictionary is not. We need to keep the order because it is important.
            OrderedStoneDefs.Clear();
            foreach (var stoneEntry in SelectedStoneDefs)
                OrderedStoneDefs.Add(stoneEntry.Key);
            // order by name at first
            OrderedStoneDefs.Sort((x, y) => string.Compare(x.LabelCap, y.LabelCap, StringComparison.Ordinal));

            // min / max numeric fields containers
            InitUsableMinMaxNumericItem(CurrentMovementTime, nameof(CurrentMovementTime));
            InitUsableMinMaxNumericItem(SummerMovementTime, nameof(SummerMovementTime));
            InitUsableMinMaxNumericItem(WinterMovementTime, nameof(WinterMovementTime));
            InitUsableMinMaxNumericItem(Elevation, nameof(Elevation));
            InitUsableMinMaxNumericItem(TimeZone, nameof(TimeZone));

            /*
             * TEMPERATURE related Fields
             */

            InitUsableMinMaxNumericItem(AverageTemperature, nameof(AverageTemperature));
            InitUsableMinMaxNumericItem(WinterTemperature, nameof(WinterTemperature));
            InitUsableMinMaxNumericItem(SummerTemperature, nameof(SummerTemperature));

            var twelfthList = Enum.GetValues(typeof(Twelfth)).Cast<Twelfth>().ToList();
            GrowingPeriod =
                new MinMaxFromRestrictedListItem<Twelfth>(twelfthList, Twelfth.Undefined, Twelfth.Undefined);
            GrowingPeriod.PropertyChanged += delegate { OnPropertyChanged(nameof(GrowingPeriod)); };

            InitUsableMinMaxNumericItem(RainFall, nameof(RainFall));
        }

        /// <summary>
        ///     Tells whether all fields (user choice on the main window) are in their default sate or not.
        /// </summary>
        /// <returns>true if all user choices are in their default state, false otherwise.</returns>
        public bool AreAllFieldsInDefaultSate()
        {
            if (_chosenBiome != null)
                return false;

            if (_chosenHilliness != Hilliness.Undefined)
                return false;

            if (_chosenCoastalTileState != MultiCheckboxState.Partial)
                return false;

            if (_chosenAnimalsCanGrazeNowState != MultiCheckboxState.Partial)
                return false;
            
            if (!IsDefDictInDefaultState(SelectedRoadDefs))
                return false;

            if (!IsDefDictInDefaultState(SelectedRiverDefs))
                return false;

            if (!IsDefDictInDefaultState(SelectedStoneDefs))
                return false;

            if (_stoneTypesNumberOnly)
                return false;

            if (CurrentMovementTime.Use)
                return false;

            if (SummerMovementTime.Use)
                return false;

            if (WinterMovementTime.Use)
                return false;

            if (Elevation.Use)
                return false;

            if (TimeZone.Use)
                return false;

            if (AverageTemperature.Use)
                return false;

            if (WinterTemperature.Use)
                return false;

            if (SummerTemperature.Use)
                return false;

            if (GrowingPeriod.Use)
                return false;

            if (RainFall.Use)
                return false;

            return true;
        }

        public static bool IsDefDictInDefaultState<T>(Dictionary<T, ThreeStateItem> dict) where T : Def
        {
            return dict.All(def => def.Value.State == MultiCheckboxState.Partial);
        }

        /// <summary>
        ///     Called when a new world map is generated: reset all fields (user choices on  the GUI window) to their default
        ///     state.
        /// </summary>
        protected void ExecuteOnWorldGenerated()
        {
            ResetAllFields();
        }

        /// <summary>
        ///     Called when an option changed.
        /// </summary>
        protected void OptionChanged(object sender, PropertyChangedEventArgs eventArgs)
        {
            // rebuild possible hilliness values if the option changed
            if (eventArgs.PropertyName == nameof(Options.AllowImpassableHilliness))
            {
                _hillinesses = BuildHillinessValues();
                _chosenHilliness = Hilliness.Undefined;
            }
        }

        /// <summary>
        ///     Called when RimWorld definitions (<see cref="Def" />) have been loaded: build definition lists (biomes, rivers,
        ///     roads, stones, etc.)
        /// </summary>
        protected void ExecuteOnDefsLoaded()
        {
            // biome definitions list
            _biomeDefs = BuildBiomeDefs();

            // road definitions list
            _roadDefs = BuildRoadDefs();

            // river definitions list
            _riverDefs = BuildRiverDefs();

            // stone definitions list
            _stoneDefs = BuildStoneDefs();

            // build hilliness values
            _hillinesses = BuildHillinessValues();
        }

        /// <summary>
        ///     Initialize a <see cref="UsableMinMaxNumericItem{T}" /> item.
        /// </summary>
        /// <typeparam name="T">The type used by the <see cref="UsableMinMaxNumericItem{T}" />.</typeparam>
        /// <param name="numericItem">An instance of <see cref="UsableMinMaxNumericItem{T}" /> to be initialized.</param>
        /// <param name="propertyChangedName">The property name bound to the <see cref="UsableMinMaxNumericItem{T}" />.</param>
        protected void InitUsableMinMaxNumericItem<T>(UsableMinMaxNumericItem<T> numericItem,
            string propertyChangedName) where T : struct, IComparable, IConvertible
        {
            numericItem.PropertyChanged += delegate { OnPropertyChanged(propertyChangedName); };
        }

        /// <summary>
        ///     Initialize a dictionary from a list of RimWorld definitions (<see cref="Def" />) where keys are <see cref="Def" />
        ///     and values are <see cref="ThreeStateItem" />.
        ///     The propertyChangedName makes it so that if a <see cref="ThreeStateItem" /> item changed an event is fired for the
        ///     whole dictionary rather than the contained item.
        /// </summary>
        /// <typeparam name="T">The type of the items in the list parameter. <b>T</b> should be a RimWorld <see cref="Def" />.</typeparam>
        /// <param name="initList">A list of <see cref="Def" /> (each entry will be used as a dictionary key).</param>
        /// <param name="dictionary">The dictionary to be initialized.</param>
        /// <param name="propertyChangedName">
        ///     The bound property name (the name of the dictionary in this class). Each time a value
        ///     in the dictionary is changed, this fire an event related to the dictionary name and not the contained values.
        /// </param>
        /// <param name="defaultSate">The default state of the <see cref="ThreeStateItem" />.</param>
        protected void InitSelectedDictionary<T>(List<T> initList, Dictionary<T, ThreeStateItem> dictionary,
            string propertyChangedName, MultiCheckboxState defaultSate = MultiCheckboxState.Partial)
        {
            dictionary.Clear();
            foreach (var elementDef in initList)
            {
                var item = new ThreeStateItem(defaultSate);
                item.PropertyChanged += delegate
                {
                    // cheat! rather than saying that a ThreeState item changed
                    //  just pretend the whole dictionary has changed.
                    // We don't need a finer grain control than that, as the dictionary will contain just a few elements.
                    OnPropertyChanged(propertyChangedName);
                };
                dictionary.Add(elementDef, item);
            }
        }


        protected ThreeStateItem InitThreeStateItem(string propertyChanedName,
            MultiCheckboxState defaultState = MultiCheckboxState.Partial)
        {
            return new ThreeStateItem(defaultState);
        }

        /* Definitions  building */

        /// <summary>
        ///     Build the biome definitions (<see cref="BiomeDef" />) list.
        /// </summary>
        /// <param name="allowUnimplemented">Tells whether or not unimplemented biomes are allowed.</param>
        /// <param name="allowCantBuildBase">Tells whether or not biomes that do not allow bases to be built are allowed.</param>
        /// <returns>A list of all available RimWorld biome definitions (<see cref="BiomeDef" />).</returns>
        protected List<BiomeDef> BuildBiomeDefs(bool allowUnimplemented = false, bool allowCantBuildBase = false)
        {
            var biomeDefsList = new List<BiomeDef>();
            foreach (var biomeDef in DefDatabase<BiomeDef>.AllDefsListForReading)
            {
                BiomeDef currentBiomeDef = null;

                if (biomeDef.implemented)
                    currentBiomeDef = biomeDef;
                else if (!biomeDef.implemented && allowUnimplemented)
                    currentBiomeDef = biomeDef;

                if (biomeDef.canBuildBase)
                {
                    if (!biomeDefsList.Contains(biomeDef))
                        currentBiomeDef = biomeDef;
                }
                else if (!biomeDef.canBuildBase && allowCantBuildBase)
                {
                    if (!biomeDefsList.Contains(biomeDef))
                        currentBiomeDef = biomeDef;
                }
                else if (!biomeDef.canBuildBase && !allowCantBuildBase)
                {
                    if (biomeDefsList.Contains(biomeDef))
                        biomeDefsList.Remove(biomeDef);

                    currentBiomeDef = null;
                }

                if (currentBiomeDef != null)
                    biomeDefsList.Add(currentBiomeDef);
            }

            return biomeDefsList.OrderBy(biome => biome.LabelCap).ToList();
        }

        /// <summary>
        ///     Build the stone definitions (<see cref="ThingDef" />) list.
        /// </summary>
        /// <returns>A list of all available RimWorld stone definitions (<see cref="ThingDef" />).</returns>
        protected List<ThingDef> BuildStoneDefs()
        {
            return DefDatabase<ThingDef>.AllDefs.Where(WorldTileFilter.IsThingDefStone).ToList();
        }

        /// <summary>
        ///     Build the road definitions (<see cref="RoadDef" />) list.
        /// </summary>
        /// <returns>A list of all available RimWorld road definitions (<see cref="RoadDef" />).</returns>
        protected List<RoadDef> BuildRoadDefs()
        {
            var roads = DefDatabase<RoadDef>.AllDefsListForReading;
            return roads;
        }

        /// <summary>
        ///     Build the river definitions (<see cref="RiverDef" />) list.
        /// </summary>
        /// <returns>A list of all available RimWorld river definitions (<see cref="RiverDef" />).</returns>
        protected List<RiverDef> BuildRiverDefs()
        {
            var rivers = DefDatabase<RiverDef>.AllDefsListForReading;
            return rivers;
        }

        /// <summary>
        ///     Build the hilliness definitions (<see cref="Hilliness" />) list.
        /// </summary>
        /// <returns>A list of all available RimWorld hillinesses (<see cref="Hilliness" />).</returns>
        protected List<Hilliness> BuildHillinessValues()
        {
            // get all possible enumeration values for hilliness
            var hillinesses = Enum.GetValues(typeof(Hilliness)).Cast<Hilliness>().ToList();

            // check if impassable tiles are allowed
            if (Options.AllowImpassableHilliness)
                return hillinesses;

            // remove impassable hilliness if not asked specifically for it.
            if (!hillinesses.Remove(Hilliness.Impassable))
                Log.Message("[PrepareLanding] Couldn't remove Impassable hilliness.");

            return hillinesses;
        }

        /// <summary>
        ///     Called when a property (user choice) has changed.
        /// </summary>
        /// <param name="propertyName">The name of the property that has changed.</param>
        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #region PRIVATE_FIELDS

        private bool _allowCantBuildBase;
        private bool _allowUnimplementedBiomes;

        /// <summary>
        ///     All biome definitions (<see cref="BiomeDef" />) from RimWorld.
        /// </summary>
        private List<BiomeDef> _biomeDefs;

        /// <summary>
        ///     Current user choice for the "Animal Can Graze Now" state.
        /// </summary>
        private MultiCheckboxState _chosenAnimalsCanGrazeNowState = MultiCheckboxState.Partial;

        /// <summary>
        ///     The currently selected biome.
        /// </summary>
        private BiomeDef _chosenBiome;

        /// <summary>
        ///     The currently selected coastal tile state.
        /// </summary>
        private MultiCheckboxState _chosenCoastalTileState = MultiCheckboxState.Partial;

        /// <summary>
        ///     The currently selected hilliness state.
        /// </summary>
        private Hilliness _chosenHilliness;

        /// <summary>
        ///     List of all RimWorld hillinesses.
        /// </summary>
        private List<Hilliness> _hillinesses;

        /// <summary>
        ///     If true, filter only tiles with only a given number of stone types.
        /// </summary>
        private bool _stoneTypesNumberOnly;

        /// <summary>
        ///     Number of stone types when filtering with <see cref="_stoneTypesNumberOnly"/>.
        /// </summary>
        private int _stoneTypesNumber = 2;

        /// <summary>
        ///     All river definitions (<see cref="RiverDef" />) from RimWorld.
        /// </summary>
        private List<RiverDef> _riverDefs;

        /// <summary>
        ///     All road definitions (<see cref="RoadDef" />) from RimWorld.
        /// </summary>
        private List<RoadDef> _roadDefs;

        /// <summary>
        ///     All stone (rock types) definitions (<see cref="ThingDef" />) from RimWorld.
        /// </summary>
        private List<ThingDef> _stoneDefs;

        #endregion PRIVATE_FIELDS
    }
}