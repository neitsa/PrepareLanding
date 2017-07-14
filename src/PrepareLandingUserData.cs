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
    public class PrepareLandingUserData : INotifyPropertyChanged
    {
        private bool _allowCantBuildBase;
        private bool _allowUnimplementedBiomes;

        private List<BiomeDef> _biomeDefs;

        private MultiCheckboxState _chosenAnimalsCanGrazeNowState = MultiCheckboxState.Partial;
        private BiomeDef _chosenBiome;
        private MultiCheckboxState _chosenCoastalTileState = MultiCheckboxState.Partial;
        private Hilliness _chosenHilliness;
        private List<Hilliness> _hillinesses;
        private List<RiverDef> _riverDefs;
        private List<RoadDef> _roadDefs;
        private List<ThingDef> _stoneDefs;


        /// <summary>
        ///     Class constructor. Called once (when the mod is loaded)
        /// </summary>
        public PrepareLandingUserData()
        {
            // get alerted when RimWorld has loaded its definition (Defs) files
            PrepareLanding.Instance.OnDefsLoaded += ExecuteOnDefsLoaded;

            // get alerted when RimWorld has finished generating the world
            PrepareLanding.Instance.OnWorldGenerated += ExecuteOnWorldGenerated;
        }

        public ReadOnlyCollection<BiomeDef> BiomeDefs => _biomeDefs.AsReadOnly();

        public ReadOnlyCollection<ThingDef> StoneDefs => _stoneDefs.AsReadOnly();

        public ReadOnlyCollection<RoadDef> RoadDefs => _roadDefs.AsReadOnly();

        public ReadOnlyCollection<RiverDef> RiverDefs => _riverDefs.AsReadOnly();

        public ReadOnlyCollection<Hilliness> HillinessCollection => _hillinesses.AsReadOnly();

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

        /* options */

        public bool AllowLiveFiltering { get; set; }


        /* terrain data */

        public Dictionary<RoadDef, ThreeStateItem> SelectedRoadDefs { get; } =
            new Dictionary<RoadDef, ThreeStateItem>();

        public Dictionary<RiverDef, ThreeStateItem> SelectedRiverDefs { get; } =
            new Dictionary<RiverDef, ThreeStateItem>();

        public Dictionary<ThingDef, ThreeStateItem> SelectedStoneDefs { get; } =
            new Dictionary<ThingDef, ThreeStateItem>();

        public List<ThingDef> OrderedStoneDefs { get; } = new List<ThingDef>();

        public UsableMinMaxNumericItem<float> CurrentMovementTime { get; } = new UsableMinMaxNumericItem<float>();
        public UsableMinMaxNumericItem<float> SummerMovementTime { get; } = new UsableMinMaxNumericItem<float>();
        public UsableMinMaxNumericItem<float> WinterMovementTime { get; } = new UsableMinMaxNumericItem<float>();
        public UsableMinMaxNumericItem<float> Elevation { get; } = new UsableMinMaxNumericItem<float>();
        public UsableMinMaxNumericItem<int> TimeZone { get; } = new UsableMinMaxNumericItem<int>();

        /* temperature data */
        public UsableMinMaxNumericItem<float> AverageTemperature { get; } = new UsableMinMaxNumericItem<float>();

        public UsableMinMaxNumericItem<float> WinterTemperature { get; } = new UsableMinMaxNumericItem<float>();

        public UsableMinMaxNumericItem<float> SummerTemperature { get; } = new UsableMinMaxNumericItem<float>();

        public MinMaxFromRestrictedListItem<Twelfth> GrowingPeriod { get; private set; }

        public UsableMinMaxNumericItem<float> RainFall { get; } = new UsableMinMaxNumericItem<float>();


        /* events */
        public event PropertyChangedEventHandler PropertyChanged;

        protected void ExecuteOnWorldGenerated()
        {
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

        protected void InitUsableMinMaxNumericItem<T>(UsableMinMaxNumericItem<T> numericItem,
            string propertyChangedName) where T : struct
        {
            numericItem.PropertyChanged += delegate { OnPropertyChanged(propertyChangedName); };
        }

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
                    // We don't need a finer grain control than that as the dictionary will contain just a few elements.
                    OnPropertyChanged(propertyChangedName);
                };
                dictionary.Add(elementDef, item);
            }
        }

        /* Definitions  building */

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

        protected List<ThingDef> BuildStoneDefs()
        {
            return DefDatabase<ThingDef>.AllDefs.Where(WorldTileFilter.IsThingDefStone).ToList();
        }

        protected List<RoadDef> BuildRoadDefs()
        {
            var roads = DefDatabase<RoadDef>.AllDefsListForReading;
            return roads;
        }

        protected List<RiverDef> BuildRiverDefs()
        {
            var rivers = DefDatabase<RiverDef>.AllDefsListForReading;
            return rivers;
        }

        protected List<Hilliness> BuildHillinessValues()
        {
            //TODO: disable "impassable" hilliness except if explicitly asked for (debug menu or something like that)
            return Enum.GetValues(typeof(Hilliness)).Cast<Hilliness>().ToList();
        }

        [NotifyPropertyChangedInvocator] //TODO: comment when releasing.
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}