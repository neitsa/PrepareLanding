using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using PrepareLanding.Extensions;
using PrepareLanding.Filters;
using RimWorld.Planet;
using Verse;

namespace PrepareLanding
{
    public class WorldTileFilter
    {
        private readonly Dictionary<string, ITileFilter> _allFilters;

        private readonly List<int> _allValidTileIds = new List<int>();

        private readonly List<int> _matchingTileIds = new List<int>();
        private readonly List<ITileFilter> _sortedFilters = new List<ITileFilter>();
        private readonly PrepareLandingUserData _userData;
        public ReadOnlyCollection<int> AllTilesWithRiver;
        public ReadOnlyCollection<int> AllTilesWithRoad;


        public WorldTileFilter(PrepareLandingUserData userData)
        {
            _userData = userData;
            _userData.PropertyChanged += OnDataPropertyChanged;

            PrepareLanding.Instance.OnWorldGenerated += WorldGenerated;

            FilterInfo.PropertyChanged += FilterInfoChanged;

            _allFilters = new Dictionary<string, ITileFilter>
            {
                /* terrain */
                {
                    nameof(_userData.ChosenBiome),
                    new TileFilterBiomes(_userData, nameof(_userData.ChosenBiome), FilterHeaviness.Light)
                },
                {
                    nameof(_userData.ChosenHilliness),
                    new TileFilterHilliness(_userData, nameof(_userData.ChosenHilliness), FilterHeaviness.Light)
                },
                {
                    nameof(_userData.SelectedRoadDefs),
                    new TileFilterRoads(_userData, nameof(_userData.SelectedRoadDefs), FilterHeaviness.Light)
                },
                {
                    nameof(_userData.SelectedRiverDefs),
                    new TileFilterRivers(_userData, nameof(_userData.SelectedRiverDefs), FilterHeaviness.Light)
                },
                {
                    nameof(_userData.CurrentMovementTime),
                    new TileFilterCurrentMovementTimes(_userData, nameof(_userData.CurrentMovementTime),
                        FilterHeaviness.Heavy)
                },
                {
                    nameof(_userData.WinterMovementTime),
                    new TileFilterWinterMovementTimes(_userData, nameof(_userData.WinterMovementTime),
                        FilterHeaviness.Heavy)
                },
                {
                    nameof(_userData.SummerMovementTime),
                    new TileFilterSummerMovementTimes(_userData, nameof(_userData.SummerMovementTime),
                        FilterHeaviness.Heavy)
                },
                {
                    nameof(_userData.SelectedStoneDefs),
                    new TileFilterStones(_userData, nameof(_userData.SelectedStoneDefs), FilterHeaviness.Heavy)
                },
                {
                    nameof(_userData.ChosenCoastalTileState),
                    new TileFilterCoastalTiles(_userData, nameof(_userData.ChosenCoastalTileState),
                        FilterHeaviness.Light)
                },
                {
                    nameof(_userData.Elevation),
                    new TileFilterElevations(_userData, nameof(_userData.Elevation), FilterHeaviness.Heavy)
                },
                {
                    nameof(_userData.TimeZone),
                    new TileFilterTimeZones(_userData, nameof(_userData.TimeZone), FilterHeaviness.Medium)
                }, //TODO: check heaviness
                /* temperature */
                {
                    nameof(_userData.AverageTemperature),
                    new TileFilterAverageTemperatures(_userData, nameof(_userData.AverageTemperature),
                        FilterHeaviness.Heavy)
                },
                {
                    nameof(_userData.WinterTemperature),
                    new TileFilterWinterTemperatures(_userData, nameof(_userData.WinterTemperature),
                        FilterHeaviness.Heavy)
                },
                {
                    nameof(_userData.SummerTemperature),
                    new TileFilterSummerTemperatures(_userData, nameof(_userData.SummerTemperature),
                        FilterHeaviness.Heavy)
                },
                {
                    nameof(_userData.GrowingPeriod),
                    new TileFilterGrowingPeriods(_userData, nameof(_userData.GrowingPeriod), FilterHeaviness.Heavy)
                }, // TODO check heaviness
                {
                    nameof(_userData.RainFall),
                    new TileFilterRainFalls(_userData, nameof(_userData.RainFall), FilterHeaviness.Medium)
                }, //TODO check heaviness
                {
                    nameof(_userData.ChosenAnimalsCanGrazeNowState),
                    new TileFilterAnimalsCanGrazeNow(_userData, nameof(_userData.ChosenAnimalsCanGrazeNowState),
                        FilterHeaviness.Heavy)
                } //TODO check heaviness
            };

            var lightFilters = _allFilters.Values.Where(filter => filter.Heaviness == FilterHeaviness.Light).ToList();
            var mediumFilters = _allFilters.Values.Where(filter => filter.Heaviness == FilterHeaviness.Medium).ToList();
            var heavyFilters = _allFilters.Values.Where(filter => filter.Heaviness == FilterHeaviness.Heavy).ToList();

            _sortedFilters.AddRange(lightFilters);
            _sortedFilters.AddRange(mediumFilters);
            _sortedFilters.AddRange(heavyFilters);
        }

        public FilterInfo FilterInfo { get; } = new FilterInfo();

        public ReadOnlyCollection<int> AllValidTilesReadOnly => _allValidTileIds.AsReadOnly();

        public string FilterInfoText => FilterInfo.Text;

        public event Action OnFilterInfoTextChanged = delegate { };

        public int Count()
        {
            return _matchingTileIds.Count;
        }

        public void ClearMatchingTiles()
        {
            _matchingTileIds.Clear();
        }

        public void FilterInfoChanged(object sender, PropertyChangedEventArgs e)
        {
            OnFilterInfoTextChanged?.Invoke();
        }

        public void WorldGenerated()
        {
            LongEventHandler.QueueLongEvent(Prefilter, "[PrepareLanding] Prefiltering World Tiles", true, null);
        }

        public void Prefilter()
        {
            //TODO allow user to use non valid tiles in their search

            Log.Message($"[PrepareLanding] Prefilter: {Find.WorldGrid.tiles.Count} tiles in WorldGrid.tiles");

            // clear all valid tile ids
            _allValidTileIds.Clear();

            // get all valid tiles for a new settlement
            var tileCount = Find.World.grid.TilesCount;
            for (var i = 0; i < tileCount; i++)
            {
                if (!IsViableTile(i))
                    continue;

                _allValidTileIds.Add(i);
            }

            FilterInfo.AppendMessage(
                $"Prefilter: {_allValidTileIds.Count} tiles remain after filter ({Find.WorldGrid.tiles.Count - _allValidTileIds.Count} removed).");


            // get all tiles with at least one river
            var allTilesWithRivers = _allValidTileIds.FindAll(
                tileId => Find.World.grid[tileId].VisibleRivers != null &&
                          Find.World.grid[tileId].VisibleRivers.Count != 0);
            AllTilesWithRiver = new ReadOnlyCollection<int>(allTilesWithRivers);
            FilterInfo.AppendMessage($"Prefilter: {allTilesWithRivers.Count} tiles with at least one river.");

            // get all tiles with at least one road
            var allTilesWithRoads =
                _allValidTileIds.FindAll(tileId => Find.World.grid[tileId].VisibleRoads != null &&
                                                   Find.World.grid[tileId].VisibleRoads.Count != 0);

            AllTilesWithRoad = new ReadOnlyCollection<int>(allTilesWithRoads);
            FilterInfo.AppendMessage($"Prefilter: {allTilesWithRoads.Count} tiles with at least one road.");
        }

        public void Filter()
        {
            // check if live filtering is allowed or not:
            //  - If it's allowed we filter directly.
            //  - If it's not allowed, we filter everything on a queued long event.
            if (PrepareLanding.Instance.UserData.AllowLiveFiltering)
                FilterTiles();
            else
                LongEventHandler.QueueLongEvent(FilterTiles, "[PrepareLanding] Filtering World Tiles", true, null);
        }

        public void FilterTiles()
        {
            // do a preventive check before filtering anything
            if (!FilterPreCheck())
                return;

            // clear all previous matching tiles
            ClearMatchingTiles();

            // remove all previously highlighted tiles on the world map
            PrepareLanding.Instance.TileHighlighter.RemoveAllTiles();

            var separator = "-".Repeat(40);
            FilterInfo.AppendMessage($"{separator}\nNew Filtering\n{separator}");

            // filter tiles
            var result = new List<int>();
            var firstUnionDone = false;

            for (var i = 0; i < _sortedFilters.Count; i++)
            {
                // get the filter
                var filter = _sortedFilters[i];

                // only use an active filter
                if (!filter.IsFilterActive)
                    continue;

                // use all valid tiles until we have a first result
                var currentList = firstUnionDone ? result : _allValidTileIds;

                // do the actual filtering
                filter.FilterAction(currentList);

                // check if anything was filtered
                var filteredTiles = filter.FilteredTiles;
                if (filteredTiles.Count == 0 && filter.IsFilterActive)
                {
                    FilterInfo.AppendErrorMessage(
                        $"{filter.RunningDescription}: this filter results in 0 matching tiles.", sendToLog: true);
                    return;
                }

                if (filteredTiles.Count == _allValidTileIds.Count)
                    FilterInfo.AppendWarningMessage(
                        $"{filter.RunningDescription}: this filter results in all valid tiles matching.", true);

                if (!firstUnionDone)
                {
                    result = filteredTiles.Union(result).ToList();
                    firstUnionDone = true;
                }
                else
                {
                    result = filteredTiles.Intersect(result).ToList();
                }

                FilterInfo.AppendMessage($"{filter.RunningDescription}: {result.Count} tiles found.");
            }

            // all results into one list
            _matchingTileIds.AddRange(result);

            if(_matchingTileIds.Count == 0)
                FilterInfo.AppendErrorMessage("No tile matches the given filter(s).", sendToLog: true);
            else
                FilterInfo.AppendSuccessMessage($"A total of {_matchingTileIds.Count} tile(s) matches all filters.", true);


            // highlight filtered tiles
            PrepareLanding.Instance.TileHighlighter.HighlightTileList(_matchingTileIds);
        }

        private bool FilterPreCheck()
        {
            if (_userData.AreAllFieldsInDefaultSate())
            {
                FilterInfo.AppendErrorMessage("All filters are in their default state, please select at least one filter.");
                return false;
            }

            var filteredBiomes = _allFilters[nameof(_userData.ChosenBiome)].FilteredTiles;
            var filteredHilliness = _allFilters[nameof(_userData.ChosenHilliness)].FilteredTiles;

            // advise user that filtering all tiles without preselected biomes or hilliness is not advised (on world coverage >= 50%)
            //  as it takes too much times with some filter, so it's better to narrow down the filtering.
            if (Find.World.info.planetCoverage >= 0.5f)
                if (filteredBiomes.Count == 0 || filteredHilliness.Count == 0 ||
                    filteredBiomes.Count == _allValidTileIds.Count || filteredHilliness.Count == _allValidTileIds.Count)
                {
                    FilterInfo.AppendErrorMessage(
                        "No biome and no terrain selected for Planet coverage >= 50%\n\tPlease select a biome and a terrain first.");
                    return false;
                }

            return true;
        }

        private void OnDataPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            // check if live filtering is allowed or not. If it's not allowed, we filter everything on the 'Filter' button push.
            if (!PrepareLanding.Instance.UserData.AllowLiveFiltering)
                return;

            ITileFilter tileFilter;
            if (!_allFilters.TryGetValue(e.PropertyName, out tileFilter))
            {
                Log.Message(
                    $"[PrepareLanding] [OnDataPropertyChanged] An unknown property name was passed: {e.PropertyName}");

                return;
            }

            // call the filter
            tileFilter.FilterAction(_allValidTileIds);
        }

        #region PREDICATES

        /// <summary>
        ///     Negate an existing predicate.
        /// </summary>
        /// <typeparam name="T">Type used by the predicate.</typeparam>
        /// <param name="predicate">The predicate to be negated.</param>
        /// <returns>Returns a <see cref="bool" /> that is the negated value of the predicate.</returns>
        public static Predicate<T> NegatePredicate<T>(Predicate<T> predicate)
        {
            return x => !predicate(x);
        }

        /// <summary>
        ///     Check if a <see cref="ThingDef" /> describes a stone / rock type.
        /// </summary>
        /// <param name="thingDef">The <see cref="ThingDef" /> to check.</param>
        /// <returns>true if the ThingDef describes a stone type, false otherwise.</returns>
        public static bool IsThingDefStone(ThingDef thingDef)
        {
            return thingDef.category == ThingCategory.Building &&
                   thingDef.building.isNaturalRock &&
                   !thingDef.building.isResourceRock;
        }

        public static bool IsViableTile(int tileId)
        {
            var tile = Find.World.grid[tileId];

            // we must be able to build a base, the tile biome must be implemented and the tile itself must not be impassable
            // Side note on tile.WaterCovered: this doesn't work for sea ice as elevation is < 0, but sea ice is a perfectly valid biome where to settle.
            return tile.biome.canBuildBase && tile.biome.implemented && tile.hilliness != Hilliness.Impassable;
        }

        #endregion PREDICATES
    }
}