using System;
using System.ComponentModel;
using System.Linq;
using JetBrains.Annotations;
using PrepareLanding.Filters;
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
            PrepareLanding.Instance.EventHandler.WorldGeneratedOrLoaded += ExecuteOnWorldGeneratedOrLoaded;

            // save options
            Options = options;

            // register to the option changed event
            Options.PropertyChanged += OptionChanged;
        }

        /// <summary>
        ///     Current user choices for the average temperature.
        /// </summary>
        public UsableMinMaxNumericItem<float> AverageTemperature { get; } = new UsableMinMaxNumericItem<float>();

        /// <summary>
        ///     Current user choices for the minimum temperature.
        /// </summary>
        public UsableMinMaxNumericItem<float> MinTemperature { get; } = new UsableMinMaxNumericItem<float>();

        /// <summary>
        ///     Current user choices for the maximum temperature.
        /// </summary>
        public UsableMinMaxNumericItem<float> MaxTemperature { get; } = new UsableMinMaxNumericItem<float>();

        /// <summary>
        ///     Current user choice for the "Animal Can Graze Now" state.
        /// </summary>
        public MultiCheckboxState ChosenAnimalsCanGrazeNowState
        {
            get => _chosenAnimalsCanGrazeNowState;
            set
            {
                if (value == _chosenAnimalsCanGrazeNowState)
                    return;

                _chosenAnimalsCanGrazeNowState = value;
                OnPropertyChanged(nameof(ChosenAnimalsCanGrazeNowState));
            }
        }

        /// <summary>
        ///     Current user choice for the "Has Cave" state.
        /// </summary>
        public MultiCheckboxState HasCaveState
        {
            get => _hasCaveState;
            set
            {
                if (value == _hasCaveState)
                    return;

                _hasCaveState = value;
                OnPropertyChanged(nameof(HasCaveState));
            }
        }

        /// <summary>
        ///     Current user selected biome.
        /// </summary>
        public BiomeDef ChosenBiome
        {
            get => _chosenBiome;
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
            get => _chosenCoastalTileState;
            set
            {
                if (value == _chosenCoastalTileState)
                    return;

                _chosenCoastalTileState = value;
                OnPropertyChanged(nameof(ChosenCoastalTileState));
            }
        }

        /// <summary>
        ///     Current user choice for the tiles coastal state.
        /// </summary>
        public MultiCheckboxState ChosenCoastalLakeTileState
        {
            get => _coastalLakeTileState;
            set
            {
                if (value == _coastalLakeTileState)
                    return;

                _coastalLakeTileState = value;
                OnPropertyChanged(nameof(ChosenCoastalLakeTileState));
            }
        }

        /// <summary>
        ///     Current user selected hilliness.
        /// </summary>
        public Hilliness ChosenHilliness
        {
            get => _chosenHilliness;
            set
            {
                if (value == _chosenHilliness)
                    return;

                _chosenHilliness = value;
                OnPropertyChanged(nameof(ChosenHilliness));
            }
        }

        public ThingDef ForagedFood
        {
            get => _foragedFood;

            set
            {
                if (value == _foragedFood)
                    return;

                _foragedFood = value;
                OnPropertyChanged(nameof(ForagedFood));
            }
        }

        /// <summary>
        ///     Current user choices for forageability.
        /// </summary>
        public UsableMinMaxNumericItem<float> Forageability { get; } = new UsableMinMaxNumericItem<float>();

        /// <summary>
        ///     Current user choices for movement difficulty.
        /// </summary>
        public UsableMinMaxNumericItem<float> MovementDifficulty { get; } = new UsableMinMaxNumericItem<float>();

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
        ///     Current user choices for the rain fall.
        /// </summary>
        public UsableMinMaxNumericItem<float> RainFall { get; } = new UsableMinMaxNumericItem<float>();

        /// <summary>
        ///     Current user choices for the river filtering.
        /// </summary>
        public ThreeStateItemContainer<RiverDef> SelectedRiverDefs { get; } = new ThreeStateItemContainer<RiverDef>();

        /// <summary>
        ///     Current user choices for the roads filtering.
        /// </summary>
        public ThreeStateItemContainer<RoadDef> SelectedRoadDefs { get; } = new ThreeStateItemContainer<RoadDef>();

        /// <summary>
        ///     Current user choices for the stone types filtering.
        /// </summary>
        public ThreeStateItemContainerOrdered<ThingDef> SelectedStoneDefs { get; } = new ThreeStateItemContainerOrdered<ThingDef>();

        /// <summary>
        ///     The number of stones per tile to filter when the <see cref="StoneTypesNumberOnly" /> boolean is true.
        /// </summary>
        public int StoneTypesNumber
        {
            get => _stoneTypesNumber;
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
            get => _stoneTypesNumberOnly;
            set
            {
                if (value == _stoneTypesNumberOnly)
                    return;

                _stoneTypesNumberOnly = value;
                OnPropertyChanged(nameof(StoneTypesNumberOnly));
            }
        }

        /// <summary>
        ///     Selected World Feature (name on the world map).
        /// </summary>
        public WorldFeature WorldFeature
        {
            get => _worldFeature;
            set
            {
                if (value == _worldFeature)
                    return;

                _worldFeature = value;
                OnPropertyChanged(nameof(WorldFeature));
            }
        }

        public UsableFromList<int> CoastalRotation { get; } =  new UsableFromList<int>(
            TileFilterCoastRotation.PossibleRotationsInt, TileFilterCoastRotation.PossibleRotationsInt[0]);

        /// <summary>
        ///     Current user choices for the time zone.
        /// </summary>
        public UsableMinMaxNumericItem<int> TimeZone { get; } = new UsableMinMaxNumericItem<int>();


        /// <summary>
        ///     Current user choice for Most / Least item
        /// </summary>
        public MostLeastItem MostLeastItem { get; } = new MostLeastItem();

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

            if (_coastalLakeTileState != MultiCheckboxState.Partial)
                return false;

            if (CoastalRotation.Use)
                return false;

            if (_chosenAnimalsCanGrazeNowState != MultiCheckboxState.Partial)
                return false;

            if (_hasCaveState != MultiCheckboxState.Partial)
                return false;

            if (!SelectedRoadDefs.IsInDefaultState())
                return false;

            if (!SelectedRiverDefs.IsInDefaultState())
                return false;

            if (!SelectedStoneDefs.IsInDefaultState())
                return false;

            if (_stoneTypesNumberOnly)
                return false;

            if (MovementDifficulty.Use)
                return false;

            if (Forageability.Use)
                return false;

            if (_foragedFood != null)
                return false;

            if (Elevation.Use)
                return false;

            if (TimeZone.Use)
                return false;

            if (AverageTemperature.Use)
                return false;

            if (MinTemperature.Use)
                return false;

            if (MaxTemperature.Use)
                return false;

            if (GrowingPeriod.Use)
                return false;

            if (RainFall.Use)
                return false;

            if (!MostLeastItem.IsInDefaultState)
                return false;

            if (WorldFeature != null)
                return false;

            return true;
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
            _coastalLakeTileState = MultiCheckboxState.Partial;
            CoastalRotation.Reset(false);
            _chosenAnimalsCanGrazeNowState = MultiCheckboxState.Partial;
            _foragedFood = null;

            var defProps = PrepareLanding.Instance.GameData.DefData;
            SelectedRoadDefs.SetContainer(defProps.RoadDefs, nameof(SelectedRoadDefs));
            SelectedRiverDefs.SetContainer(defProps.RiverDefs, nameof(SelectedRiverDefs));
            SelectedStoneDefs.SetContainer(defProps.StoneDefs, nameof(SelectedStoneDefs));

            // stone numbers
            StoneTypesNumberOnly = false;
            StoneTypesNumber = 2;

            // min / max numeric fields containers
            InitUsableMinMaxNumericItem(MovementDifficulty, nameof(MovementDifficulty));
            InitUsableMinMaxNumericItem(Forageability, nameof(Forageability));
            InitUsableMinMaxNumericItem(Elevation, nameof(Elevation));
            InitUsableMinMaxNumericItem(TimeZone, nameof(TimeZone));

            _hasCaveState = MultiCheckboxState.Partial;
            _worldFeature = null;

            /*
             * TEMPERATURE related Fields
             */

            InitUsableMinMaxNumericItem(AverageTemperature, nameof(AverageTemperature));
            InitUsableMinMaxNumericItem(MinTemperature, nameof(MinTemperature));
            InitUsableMinMaxNumericItem(MaxTemperature, nameof(MaxTemperature));

            var twelfthList = Enum.GetValues(typeof(Twelfth)).Cast<Twelfth>().ToList();
            GrowingPeriod =
                new MinMaxFromRestrictedListItem<Twelfth>(twelfthList, Twelfth.Undefined, Twelfth.Undefined);
            GrowingPeriod.PropertyChanged += delegate { OnPropertyChanged(nameof(GrowingPeriod)); };
            GrowingPeriod.Use = false;

            InitUsableMinMaxNumericItem(RainFall, nameof(RainFall));

            MostLeastItem.Reset();
        }

        /// <summary>
        ///     Called when a new world map is generated: reset all fields (user choices on  the GUI window) to their default
        ///     state.
        /// </summary>
        private void ExecuteOnWorldGeneratedOrLoaded()
        {
            if (Options.ResetAllFieldsOnNewGeneratedWorld || !_firstResetDone)
            {
                _firstResetDone = true;
                ResetAllFields();
            }
        }

        /// <summary>
        ///     Initialize a <see cref="UsableMinMaxNumericItem{T}" /> item.
        /// </summary>
        /// <typeparam name="T">The type used by the <see cref="UsableMinMaxNumericItem{T}" />.</typeparam>
        /// <param name="numericItem">An instance of <see cref="UsableMinMaxNumericItem{T}" /> to be initialized.</param>
        /// <param name="propertyChangedName">The property name bound to the <see cref="UsableMinMaxNumericItem{T}" />.</param>
        private void InitUsableMinMaxNumericItem<T>(UsableMinMaxNumericItem<T> numericItem,
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
        private void OptionChanged(object sender, PropertyChangedEventArgs eventArgs)
        {
            // reset the chosen Hilliness if the option changed
            if (eventArgs.PropertyName == nameof(Options.AllowImpassableHilliness))
            {
                _chosenHilliness = Hilliness.Undefined;
            }
        }

        #region PRIVATE_FIELDS
        /// <summary>
        ///     Current user choice for the "Animal Can Graze Now" state.
        /// </summary>
        private MultiCheckboxState _chosenAnimalsCanGrazeNowState = MultiCheckboxState.Partial;

        /// <summary>
        ///     Current user choice for the "Has cave" state.
        /// </summary>
        private MultiCheckboxState _hasCaveState = MultiCheckboxState.Partial;

        /// <summary>
        ///     The currently selected biome.
        /// </summary>
        private BiomeDef _chosenBiome;

        /// <summary>
        ///     The currently selected coastal tile state.
        /// </summary>
        private MultiCheckboxState _chosenCoastalTileState = MultiCheckboxState.Partial;

        /// <summary>
        ///     Whether we should filter for coastal tiles (only for lakes!)
        /// </summary>
        private MultiCheckboxState _coastalLakeTileState = MultiCheckboxState.Partial;

        /// <summary>
        ///     The currently selected hilliness state.
        /// </summary>
        private Hilliness _chosenHilliness;


        private ThingDef _foragedFood;

        /// <summary>
        ///     If true, filter only tiles with only a given number of stone types.
        /// </summary>
        private bool _stoneTypesNumberOnly;

        /// <summary>
        ///     Number of stone types when filtering with <see cref="_stoneTypesNumberOnly" />.
        /// </summary>
        private int _stoneTypesNumber = 2;

        /// <summary>
        ///     Selected wold feature by user.
        /// </summary>
        private WorldFeature _worldFeature;

        /// <summary>
        ///     Used to tell if the first reset has been done. It must be done once in the lifetime of the mod to at least
        ///     initialize all fields.
        /// </summary>
        private bool _firstResetDone;

        #endregion PRIVATE_FIELDS
    }
}