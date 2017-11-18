using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using PrepareLanding.Core.Extensions;
using PrepareLanding.Filters;
using PrepareLanding.GameData;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace PrepareLanding
{
    /// <summary>
    ///     Class used to filter tiles (depending on user choices) from the world map.
    /// </summary>
    public class WorldTileFilter
    {
        /// <summary>
        ///     Contains all tiles (from the world map) with at least one river in it.
        /// </summary>
        public ReadOnlyCollection<int> AllTilesWithRiver;

        /// <summary>
        ///     Contains all tiles (from the world map) with at least one road in it.
        /// </summary>
        public ReadOnlyCollection<int> AllTilesWithRoad;

        /// <summary>
        ///     Class constructor.
        /// </summary>
        /// <param name="userData">An instance of the class used to keep user choice from the main GUI window.</param>
        public WorldTileFilter(UserData userData)
        {
            // save user data and subscribe to the event that is fired when a property changed (so we know if something changed on the GUI).
            _userData = userData;
            _userData.PropertyChanged += OnUserDataPropertyChanged;

            // register to option property changed
            _userData.Options.PropertyChanged += OnOptionPropertyChanged;

            // be alerted when the world map is generated or loaded.
            PrepareLanding.Instance.EventHandler.WorldGeneratedOrLoaded += PrefilterQueueLongEvent;

            // instantiate all existing filters
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
                    nameof(_userData.ChosenCoastalLakeTileState),
                    new TileFilterCoastalLakeTiles(_userData, nameof(_userData.ChosenCoastalLakeTileState),
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
                }, //TODO check heaviness
                {
                    nameof(_userData.MostLeastItem),
                    new TileFilterMostLeastFeature(_userData, nameof(_userData.MostLeastItem), FilterHeaviness.Light)
                }
            };

            // gather filters by their "heaviness": light filters are filters that will probably be fast (light on CPU cycles) 
            //  while heavy filters will probably take time and have a good chance of freezing the game because they take a lot
            //  of time and CPU power.
            var lightFilters = _allFilters.Values.Where(filter => filter.Heaviness == FilterHeaviness.Light).ToList();
            var mediumFilters = _allFilters.Values.Where(filter => filter.Heaviness == FilterHeaviness.Medium).ToList();
            var heavyFilters = _allFilters.Values.Where(filter => filter.Heaviness == FilterHeaviness.Heavy).ToList();

            // save the filters according to their "heaviness": lighter first and heavier last.
            _sortedFilters.AddRange(lightFilters);
            _sortedFilters.AddRange(mediumFilters);
            _sortedFilters.AddRange(heavyFilters);
        }

        /// <summary>
        ///     All the tiles that are valid after being filtered. A <see cref="ReadOnlyCollection{T}" /> of
        ///     <see cref="_matchingTileIds" />.
        /// </summary>
        public ReadOnlyCollection<int> AllMatchingTiles => _matchingTileIds.AsReadOnly();

        /// <summary>
        ///     All the tiles that are deemed as "valid". A <see cref="ReadOnlyCollection{T}" /> of
        ///     <see cref="_allValidTileIds" />.
        /// </summary>
        public ReadOnlyCollection<int> AllValidTilesReadOnly => _allValidTileIds.AsReadOnly();

        /// <summary>
        ///     An instance of the filter logger (used on the GUI in the info tab). Tells some useful info to the end user.
        /// </summary>
        public FilterInfoLogger FilterInfoLogger { get; } = new FilterInfoLogger();

        /// <summary>
        ///     Clear all tiles that match a set of filters. Also clear the highlighted tiles on the world map.
        /// </summary>
        public void ClearMatchingTiles()
        {
            FilterInfoLogger.AppendWarningMessage("Filtered tiles cleared.");

            // clear the list of matched tiles.
            _matchingTileIds.Clear();

            // clear also highlighted tiles, if any
            PrepareLanding.Instance.TileHighlighter.RemoveAllTiles();
        }

        /// <summary>
        ///     The main method of this class: filters world map tiles according to a set of filters chosen by the user.
        /// </summary>
        /// <remarks>This method is actually a wrapper around <see cref="FilterTiles" />.</remarks>
        public void Filter()
        {
            // check if live filtering is allowed or not:
            //  - If it's allowed we filter directly.
            //  - If it's not allowed, we filter everything on a queued long event.
            if (_userData.Options.AllowLiveFiltering)
                FilterTiles();
            else
                LongEventHandler.QueueLongEvent(FilterTiles,
                    $"[{"PrepareLanding".Translate()}] {"FilteringWorldTiles".Translate()}", true, null);
        }

        /// <summary>
        ///     Get a random settle-able tile from the filtered tiles.
        /// </summary>
        /// <returns>A random tile ID (or Tile.Invalid if no tile could be found).</returns>
        public int RandomFilteredTile()
        {
            if (_matchingTileIds.Count == 0)
            {
                Messages.Message("Please filter tiles first.", MessageTypeDefOf.RejectInput);
                return Tile.Invalid;
            }

            var random = new System.Random();

            var minTries = Math.Min(_matchingTileIds.Count, 500);
            for (var i = 0; i < minTries; i++)
            {
                var minRange = Math.Min(_matchingTileIds.Count, 100);

                int tileId;
                if ((from _ in Enumerable.Range(0, minRange) select _matchingTileIds[random.Next(_matchingTileIds.Count)]).TryRandomElementByWeight(delegate(int x)
                {
                    var tile = Find.WorldGrid[x];

                    if (!PrepareLanding.Instance.GameData.UserData.Options.AllowImpassableHilliness &&
                        tile.hilliness == Hilliness.Impassable)
                        return 0f;

                    if (!tile.biome.canBuildBase || !tile.biome.implemented)
                        return 0f;

                    if (!tile.biome.canAutoChoose)
                        return 0f;

                    return tile.biome.factionBaseSelectionWeight;
                }, out tileId))
                {
                    if (TileFinder.IsValidTileForNewSettlement(tileId))
                        return tileId;
                }
            }

            Messages.Message("Failed to find a valid base tile.", MessageTypeDefOf.RejectInput);
            Log.Error("[PrepareLanding] Failed to find a valid base tile.");
            return Tile.Invalid;
        }

        /// <summary>
        ///     Given the <see cref="TileFilter.SubjectThingDef" /> from a filter, returns the filter heaviness.
        /// </summary>
        /// <param name="subjectThingDef">The <see cref="TileFilter.SubjectThingDef" /> from a filter.</param>
        /// <returns>The filter heaviness of the filter if the filter is found, otherwise <see cref="FilterHeaviness.Unknown" />.</returns>
        public FilterHeaviness FilterHeavinessFromFilterSubjectThingDef(string subjectThingDef)
        {
            if (_filterHeavinessCache == null)
            {
                _filterHeavinessCache = new Dictionary<string, FilterHeaviness>();

                foreach (var filter in _allFilters.Values)
                    _filterHeavinessCache.Add(filter.SubjectThingDef, filter.Heaviness);
            }

            FilterHeaviness filterHeaviness;
            return _filterHeavinessCache.TryGetValue(subjectThingDef, out filterHeaviness)
                ? filterHeaviness
                : FilterHeaviness.Unknown;
        }

        /// <summary>
        ///     Class can subscribe to this event to know that the pre-filtering has been done.
        /// </summary>
        public event Action OnPrefilterDone;

        /// <summary>
        ///     Main workhorse method that does the actual tile filtering. <see cref="Filter" /> is actually a wrapper around this
        ///     method.
        /// </summary>
        private void FilterTiles()
        {
            // do a preventive check before filtering anything
            if (!FilterPreCheck())
                return;

            // clear all previous matching tiles and remove all previously highlighted tiles on the world map
            ClearMatchingTiles();

            var separator = "-".Repeat(80);
            FilterInfoLogger.AppendMessage($"{separator}\nNew Filtering\n{separator}", textColor: Color.yellow);

            var globalFilterStopWatch = new Stopwatch();
            var localFilterStopWatch = new Stopwatch();

            globalFilterStopWatch.Start();

            // filter tiles
            var result = new List<int>();
            var firstUnionDone = false;

            FilterInfoLogger.AppendMessage($"Starting filtering with: {_allValidTileIds.Count} tiles.");

            var usedFilters = 0;
            for (var i = 0; i < _sortedFilters.Count; i++)
            {
                // get the filter
                var filter = _sortedFilters[i];

                // only use an active filter
                if (!filter.IsFilterActive)
                    continue;

                // add filter (used for logging purpose)
                usedFilters++;

                // use all valid tiles until we have a first result
                var currentList = firstUnionDone ? result : _allValidTileIds;

                // do the actual filtering
                localFilterStopWatch.Start();
                filter.FilterAction(currentList);
                localFilterStopWatch.Stop();
                var filterTime = localFilterStopWatch.Elapsed;
                localFilterStopWatch.Reset();

                // check if anything was filtered
                var filteredTiles = filter.FilteredTiles;
                if (filteredTiles.Count == 0 && filter.IsFilterActive)
                {
                    var conjunctionMessage = ".";
                    if (usedFilters > 1)
                        conjunctionMessage = $"(in conjunction with the previous {usedFilters - 1} filters)";

                    FilterInfoLogger.AppendErrorMessage(
                        $"{filter.RunningDescription}: this filter resulted in 0 matching tiles{conjunctionMessage} Maybe the filtering was a little bit too harsh?",
                        "Filter resulted in 0 tiles", sendToLog: true);

                    globalFilterStopWatch.Stop();
                    return;
                }

                // just send a warning that even if some filter was active it resulted in all tiles matching...
                // this might happen, for example, on 5% coverage wold where the map is composed of only one biome.
                if (filteredTiles.Count == _allValidTileIds.Count)
                    FilterInfoLogger.AppendWarningMessage(
                        $"{filter.RunningDescription}: this filter results in all valid tiles matching.", true);

                // actually make a union with the empty result (as of now) when we have the first filter giving something.
                if (!firstUnionDone)
                {
                    result = filteredTiles.Union(result).ToList();
                    firstUnionDone = true;
                }
                else
                {
                    // just intersect this filter result with all the previous results
                    result = filteredTiles.Intersect(result).ToList();
                }

                FilterInfoLogger.AppendMessage($"{filter.RunningDescription}: {result.Count} tiles found.");
                FilterInfoLogger.AppendMessage(
                    $"\t\"{filter.SubjectThingDef}\" filter ran in: {filterTime}.");
            }

            // all results into one list
            _matchingTileIds.AddRange(result);

            FilterInfoLogger.AppendMessage(
                $"Before checking for valid tiles: a total of {_matchingTileIds.Count} tile(s) matches all filters.");

            // last pass, remove all tile that are deemed as not being settleable
            if (!_userData.Options.AllowInvalidTilesForNewSettlement)
                _matchingTileIds.RemoveAll(tileId => TileFinder.IsValidTileForNewSettlement(tileId) == false);

            globalFilterStopWatch.Stop();

            // check if the applied filters gave no resulting tiles (the set of applied filters was probably too harsh).
            if (_matchingTileIds.Count == 0)
            {
                FilterInfoLogger.AppendErrorMessage("No tile matches the given filter(s).", sendToLog: true);
            }
            else
            {
                FilterInfoLogger.AppendMessage($"All {usedFilters} filter(s) ran in {globalFilterStopWatch.Elapsed}.");
                FilterInfoLogger.AppendSuccessMessage(
                    $"A total of {_matchingTileIds.Count} tile(s) matches all filters.", true);
            }

            // now highlight filtered tiles
            LongEventHandler.ExecuteWhenFinished(delegate
            {
                PrepareLanding.Instance.TileHighlighter.HighlightTileList(_matchingTileIds);
            });
        }

        /// <summary>
        ///     Do a pre-filtering of tiles on the world map. Mostly used to gather "valid" tiles (that is, tiles that are
        ///     settleable).
        /// </summary>
        private void Prefilter()
        {
            Log.Message($"[PrepareLanding] Prefilter: {Find.WorldGrid.tiles.Count} tiles in WorldGrid.tiles");

            var separator = "-".Repeat(80);
            FilterInfoLogger.AppendMessage($"{separator}\nPreFiltering\n{separator}", textColor: Color.cyan);

            ClearMatchingTiles();

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

            FilterInfoLogger.AppendMessage(
                $"Prefilter: {_allValidTileIds.Count} tiles remain after filter ({Find.WorldGrid.tiles.Count - _allValidTileIds.Count} removed).");

            // get all tiles with at least one river
            var allTilesWithRivers = _allValidTileIds.FindAll(
                tileId => Find.World.grid[tileId].VisibleRivers != null &&
                          Find.World.grid[tileId].VisibleRivers.Count != 0);
            AllTilesWithRiver = new ReadOnlyCollection<int>(allTilesWithRivers);
            FilterInfoLogger.AppendMessage($"Prefilter: {allTilesWithRivers.Count} tiles with at least one river.");

            // get all tiles with at least one road
            var allTilesWithRoads =
                _allValidTileIds.FindAll(tileId => Find.World.grid[tileId].VisibleRoads != null &&
                                                   Find.World.grid[tileId].VisibleRoads.Count != 0);

            AllTilesWithRoad = new ReadOnlyCollection<int>(allTilesWithRoads);
            FilterInfoLogger.AppendMessage($"Prefilter: {allTilesWithRoads.Count} tiles with at least one road.");

            OnPrefilterDone?.Invoke();
        }

        /// <summary>
        ///     Called when the world map has been generated. We use it to pre-filter valid tiles.
        /// </summary>
        private void PrefilterQueueLongEvent()
        {
            LongEventHandler.QueueLongEvent(Prefilter,
                $"[{"PrepareLanding".Translate()}] {"PreFilteringWorldTiles".Translate()}", true, null);
        }

        /// <summary>
        ///     Do some checks before filtering.
        /// </summary>
        /// <returns>true if the filtering is allowed, false if it is not.</returns>
        private bool FilterPreCheck()
        {
            // check if all filters are in their default state (as when the main window GUI appears for the first time)
            //  this won't give any meaningful result in the default state as it match all the settleable tiles on the world map.
            if (_userData.AreAllFieldsInDefaultSate())
            {
                FilterInfoLogger.AppendErrorMessage(
                    "All filters are in their default state, please select at least one filter.");
                return false;
            }

            // advise user that filtering all tiles without preselected biomes or hilliness is not advised (with a world coverage >= 50%)
            //  as it takes too much times with some filter, so it would be better to narrow down the filtering.
            if (Find.World.info.planetCoverage >= 0.5f)
                if (!_userData.Options.DisablePreFilterCheck)
                    if (_userData.ChosenBiome == null || _userData.ChosenHilliness == Hilliness.Undefined)
                    {
                        FilterInfoLogger.AppendErrorMessage(
                            "No biome and no terrain selected for a Planet coverage >= 50%\n\tPlease select a biome and a terrain first.");
                        return false;
                    }

            return true;
        }

        /// <summary>
        ///     Called when a property from <see cref="FilterOptions" /> has changed.
        /// </summary>
        /// <param name="sender">The sender of the event.</param>
        /// <param name="e">The argument of the event.</param>
        private void OnOptionPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            //TODO: also use a dictionary and Action for this kind of event
            // options checks
            if (e.PropertyName == nameof(_userData.Options.AllowImpassableHilliness))
                PrefilterQueueLongEvent();
        }

        /// <summary>
        ///     Called when a property from <see cref="UserData" /> has changed.
        /// </summary>
        /// <param name="sender">The sender of the event.</param>
        /// <param name="e">The argument of the event.</param>
        private void OnUserDataPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            // check if live filtering is allowed or not. If it's not allowed, we filter everything on the 'Filter' button push.
            if (!_userData.Options.AllowLiveFiltering)
                return;

            // defensive check to see if the filter exists.
            if (!_allFilters.ContainsKey(e.PropertyName))
            {
                Log.Message(
                    $"[PrepareLanding] [OnUserDataPropertyChanged] An unknown property name was passed: {e.PropertyName}");

                return;
            }

            // filter now
            Filter();
        }

        #region PRIVATE_FIELDS

        /// <summary>
        ///     Dictionary used to keep all filters. Key is a property name from <see cref="UserData" />. Value is a
        ///     <see cref="ITileFilter" /> instance.
        /// </summary>
        private readonly Dictionary<string, ITileFilter> _allFilters;

        /// <summary>
        ///     Keeps all tiles IDs that are deemed as "valid". Valid tiles are found by <see cref="IsViableTile" /> method.
        /// </summary>
        private readonly List<int> _allValidTileIds = new List<int>();

        /// <summary>
        ///     List of tile IDs that are valid according to a set of filters (e.g only tiles from a specific biome or whatever the
        ///     user has chosen).
        /// </summary>
        private readonly List<int> _matchingTileIds = new List<int>();

        /// <summary>
        ///     List of filters sorted by their <see cref="FilterHeaviness" />. The lighter (taking less time) are first while the
        ///     heavier (probably taking a long time) come last.
        /// </summary>
        private readonly List<ITileFilter> _sortedFilters = new List<ITileFilter>();

        /// <summary>
        ///     A <see cref="UserData" /> instance used to keep user choices on the GUI.
        /// </summary>
        private readonly UserData _userData;

        /// <summary>
        ///     A cache for filter heaviness. Key is the <see cref="TileFilter.SubjectThingDef" />, value is the filter heaviness (
        ///     <see cref="FilterHeaviness" />).
        /// </summary>
        private Dictionary<string, FilterHeaviness> _filterHeavinessCache;

        #endregion PRIVATE_FIELDS

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

        /// <summary>
        ///     Tells whether a tile seems to be viable or not for a new settlement. This is a quick pass and not a deep method.
        /// </summary>
        /// <param name="tileId">The identifier of the tile to check for viability.</param>
        /// <returns>true if the tile is meant to be viable, false otherwise.</returns>
        public bool IsViableTile(int tileId)
        {
            var tile = Find.World.grid[tileId];

            var impassableTilesCondition = _userData.Options.AllowImpassableHilliness ||
                                           tile.hilliness != Hilliness.Impassable;

            // we must be able to build a base, the tile biome must be implemented and the tile itself must not be impassable
            // Side note on tile.WaterCovered: this doesn't work for sea ice biomes as elevation is < 0, but sea ice is a perfectly valid biome where to settle.
            return tile.biome.canBuildBase && tile.biome.implemented && impassableTilesCondition;
        }

        #endregion PREDICATES
    }
}