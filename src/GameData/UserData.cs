using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using JetBrains.Annotations;
using PrepareLanding.Presets;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace PrepareLanding.GameData
{
    /// <summary>
    ///     Class used to keep user choices (from the main GUI window)
    /// </summary>
    public class UserData : INotifyPropertyChanged
    {
        /// <summary>
        ///     Class constructor.
        /// </summary>
        public UserData(FilterOptions options)
        {
            // get alerted when RimWorld has finished generating the world
            PrepareLanding.Instance.OnWorldGenerated += ExecuteOnWorldGenerated;

            // save options
            Options = options;

            // register to the option changed event
            Options.PropertyChanged += OptionChanged;

            // create the preset manager.
            PresetManager = new PresetManager(this);
        }

        /// <summary>
        ///     Current user choices for the average temperature.
        /// </summary>
        public UsableMinMaxNumericItem<float> AverageTemperature { get; } = new UsableMinMaxNumericItem<float>();

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
        ///     Current user choices for the current movement time.
        /// </summary>
        public UsableMinMaxNumericItem<float> CurrentMovementTime { get; } = new UsableMinMaxNumericItem<float>();

        /// <summary>
        ///     Current user choices for the elevation.
        /// </summary>
        public UsableMinMaxNumericItem<float> Elevation { get; } = new UsableMinMaxNumericItem<float>();

        /// <summary>
        ///     Current user choices for the growing period.
        /// </summary>
        public MinMaxFromRestrictedListItem<Twelfth> GrowingPeriod { get; private set; }

        /// <summary>
        ///     Filter Options (from the GUI window 'options' tab).
        /// </summary>
        public FilterOptions Options { get; }

        /// <summary>
        ///     Current order of the stones on the main GUI Window (as this choice order is important).
        /// </summary>
        public List<ThingDef> OrderedStoneDefs { get; } = new List<ThingDef>();

        /// <summary>
        ///     Used to load / save filters and options.
        /// </summary>
        public PresetManager PresetManager { get; }

        /// <summary>
        ///     Current user choices for the rain fall.
        /// </summary>
        public UsableMinMaxNumericItem<float> RainFall { get; } = new UsableMinMaxNumericItem<float>();


        /// <summary>
        ///     Current user choices for the river filtering.
        /// </summary>
        public Dictionary<RiverDef, ThreeStateItem> SelectedRiverDefs { get; } =
            new Dictionary<RiverDef, ThreeStateItem>();

        /// <summary>
        ///     Current user choices for the roads filtering.
        /// </summary>
        public Dictionary<RoadDef, ThreeStateItem> SelectedRoadDefs { get; } =
            new Dictionary<RoadDef, ThreeStateItem>();

        /// <summary>
        ///     Current user choices for the stone types filtering.
        /// </summary>
        public Dictionary<ThingDef, ThreeStateItem> SelectedStoneDefs { get; } =
            new Dictionary<ThingDef, ThreeStateItem>();



        /// <summary>
        ///     The number of stones per tile to filter when the <see cref="StoneTypesNumberOnly" /> boolean is true.
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
        ///     Current user choices for the summer movement time.
        /// </summary>
        public UsableMinMaxNumericItem<float> SummerMovementTime { get; } = new UsableMinMaxNumericItem<float>();

        /// <summary>
        ///     Current user choices for the summer temperature.
        /// </summary>
        public UsableMinMaxNumericItem<float> SummerTemperature { get; } = new UsableMinMaxNumericItem<float>();

        /// <summary>
        ///     Current user choices for the time zone.
        /// </summary>
        public UsableMinMaxNumericItem<int> TimeZone { get; } = new UsableMinMaxNumericItem<int>();

        /// <summary>
        ///     Current user choices for the winter movement time.
        /// </summary>
        public UsableMinMaxNumericItem<float> WinterMovementTime { get; } = new UsableMinMaxNumericItem<float>();

        /// <summary>
        ///     Current user choices for the winter temperature.
        /// </summary>
        public UsableMinMaxNumericItem<float> WinterTemperature { get; } = new UsableMinMaxNumericItem<float>();

        /// <summary>
        ///     Other classes can subscribe to this event to be alerted when a user choice changed.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

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

            var defProps = PrepareLanding.Instance.GameData.DefData;
            InitSelectedDictionary(defProps.RoadDefs, SelectedRoadDefs, nameof(SelectedRoadDefs));
            InitSelectedDictionary(defProps.RiverDefs, SelectedRiverDefs, nameof(SelectedRiverDefs));
            InitSelectedDictionary(defProps.StoneDefs, SelectedStoneDefs, nameof(SelectedStoneDefs));

            // patch for the fact that OrderedDictionary<TKey, TValue> doesn't exist in .NET...
            // The list is reorder-able but the dictionary is not. We need to keep the order because it is important.
            OrderedStoneDefs.Clear();
            foreach (var stoneEntry in SelectedStoneDefs)
                OrderedStoneDefs.Add(stoneEntry.Key);
            // order by name at first
            OrderedStoneDefs.Sort((x, y) => string.Compare(x.LabelCap, y.LabelCap, StringComparison.Ordinal));

            // stone numbers
            StoneTypesNumberOnly = false;
            StoneTypesNumber = 2;

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
            GrowingPeriod.Use = false;

            InitUsableMinMaxNumericItem(RainFall, nameof(RainFall));
        }

        /// <summary>
        ///     Called when a new world map is generated: reset all fields (user choices on  the GUI window) to their default
        ///     state.
        /// </summary>
        protected void ExecuteOnWorldGenerated()
        {
            if (Options.ResetAllFieldsOnNewGeneratedWorld || !_firstResetDone)
            {
                _firstResetDone = true;
                ResetAllFields();
            }
        }

        /// <summary>
        ///     Initialize a dictionary from a list of RimWorld definitions (<see cref="Def" />) where keys are <see cref="Def" />
        ///     and values are <see cref="ThreeStateItem" />.
        ///     The propertyChangedName makes it so that if a <see cref="ThreeStateItem" /> item changed an event is fired for the
        ///     whole dictionary rather than the contained item.
        /// </summary>
        /// <typeparam name="T">The type of the items in the list parameter. <b>T</b> should be a RimWorld <see cref="Def" />.</typeparam>
        /// <param name="initCollection">A collection of <see cref="Def" /> (each entry will be used as a dictionary key).</param>
        /// <param name="dictionary">The dictionary to be initialized.</param>
        /// <param name="propertyChangedName">
        ///     The bound property name (the name of the dictionary in this class). Each time a value
        ///     in the dictionary is changed, this fire an event related to the dictionary name and not the contained values.
        /// </param>
        /// <param name="defaultSate">The default state of the <see cref="ThreeStateItem" />.</param>
        protected void InitSelectedDictionary<T>(ReadOnlyCollection<T> initCollection, Dictionary<T, ThreeStateItem> dictionary,
            string propertyChangedName, MultiCheckboxState defaultSate = MultiCheckboxState.Partial)
        {
            dictionary.Clear();
            foreach (var elementDef in initCollection)
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

        /// <summary>
        ///     Initialize a <see cref="UsableMinMaxNumericItem{T}" /> item.
        /// </summary>
        /// <typeparam name="T">The type used by the <see cref="UsableMinMaxNumericItem{T}" />.</typeparam>
        /// <param name="numericItem">An instance of <see cref="UsableMinMaxNumericItem{T}" /> to be initialized.</param>
        /// <param name="propertyChangedName">The property name bound to the <see cref="UsableMinMaxNumericItem{T}" />.</param>
        protected void InitUsableMinMaxNumericItem<T>(UsableMinMaxNumericItem<T> numericItem,
            string propertyChangedName) where T : struct, IComparable, IConvertible
        {
            numericItem.Use = false;
            numericItem.PropertyChanged += delegate { OnPropertyChanged(propertyChangedName); };
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

        /// <summary>
        ///     Called when an option changed.
        /// </summary>
        protected void OptionChanged(object sender, PropertyChangedEventArgs eventArgs)
        {
            // reset the chosen Hilliness if the option changed
            if (eventArgs.PropertyName == nameof(Options.AllowImpassableHilliness))
            {
                _chosenHilliness = Hilliness.Undefined;
            }
        }

        #region PRIVATE_FIELDS

        private bool _allowCantBuildBase;
        private bool _allowUnimplementedBiomes;

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
        ///     If true, filter only tiles with only a given number of stone types.
        /// </summary>
        private bool _stoneTypesNumberOnly;

        /// <summary>
        ///     Number of stone types when filtering with <see cref="_stoneTypesNumberOnly" />.
        /// </summary>
        private int _stoneTypesNumber = 2;

        /// <summary>
        ///     Used to tell if the first reset has been done. It must be done once in the lifetime of the mod to at least
        ///     initialize all fields.
        /// </summary>
        private bool _firstResetDone;

        #endregion PRIVATE_FIELDS
    }
}