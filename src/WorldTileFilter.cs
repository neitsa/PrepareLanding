using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using PrepareLanding.Extensions;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace PrepareLanding
{
    public class WorldTileFilter
    {



        private readonly FilterInfo _filterInfo = new FilterInfo();

        private readonly List<List<int>> _allFilteredLists;

        private readonly List<int> _allValidTileIds = new List<int>();

        /* 
         * note: if adding a list below, add it to _allFilteredLists in the constructor
         */

        private readonly List<int> _filteredAnimalsCanGrazeNow = new List<int>();
        private readonly List<int> _filteredAverageTemperature = new List<int>();
        private readonly List<int> _filteredBiomes = new List<int>();
        private readonly List<int> _filteredCoastalTiles = new List<int>();
        private readonly List<int> _filteredCurrentMovementTime = new List<int>();
        private readonly List<int> _filteredElevation = new List<int>();
        private readonly List<int> _filteredGrowingPeriod = new List<int>();
        private readonly List<int> _filteredHilliness = new List<int>();
        private readonly List<int> _filteredRainfall = new List<int>();
        private readonly List<int> _filteredRivers = new List<int>();
        private readonly List<int> _filteredRoads = new List<int>();
        private readonly List<int> _filteredStones = new List<int>();
        private readonly List<int> _filteredSummerMovementTime = new List<int>();
        private readonly List<int> _filteredSummerTemperature = new List<int>();
        private readonly List<int> _filteredTimeZone = new List<int>();
        private readonly List<int> _filteredWinterMovementTime = new List<int>();
        private readonly List<int> _filteredWinterTemperature = new List<int>();


        private readonly List<int> _matchingTileIds = new List<int>();
        private readonly PrepareLandingUserData _userData;
        private List<int> _allTilesWithRivers;
        private List<int> _allTilesWithRoads;

        public Predicate<ThingDef> PredicateIsThingDefStone = IsThingDefStone;

        public ReadOnlyCollection<int> AllValidTilesReadOnly => _allValidTileIds.AsReadOnly();

        public event Action OnFilterInfoTextChanged = delegate { };

        public string FilterInfoText => _filterInfo.Text;

        private readonly Dictionary<string, Action> _allFilterActions;

        public WorldTileFilter(PrepareLandingUserData userData)
        {
            _userData = userData;
            _userData.PropertyChanged += OnDataPropertyChanged;

            PrepareLanding.Instance.OnWorldGenerated += WorldGenerated;

            _filterInfo.PropertyChanged += FilterInfoChanged;

            _allFilteredLists = new List<List<int>>
            {
                /* terrain */
                _filteredBiomes,
                _filteredHilliness,
                _filteredRoads,
                _filteredRivers,
                _filteredCoastalTiles,
                _filteredCurrentMovementTime,
                _filteredWinterMovementTime,
                _filteredSummerMovementTime,
                _filteredStones,
                _filteredElevation,
                _filteredTimeZone,

                /* temperature */
                _filteredAverageTemperature,
                _filteredWinterTemperature,
                _filteredSummerTemperature,
                _filteredGrowingPeriod,
                _filteredRainfall,
                _filteredAnimalsCanGrazeNow
            };

            _allFilterActions = new Dictionary<string, Action>
            {
                /* terrain */
                {nameof(_userData.ChosenBiome), FilterBiomes},
                {nameof(_userData.ChosenHilliness), FilterHilliness },
                {nameof(_userData.SelectedRoadDefs), FilterRoads},
                {nameof(_userData.SelectedRiverDefs), FilterRivers},
                {nameof(_userData.CurrentMovementTime), FilterCurrentMovementTime},
                {nameof(_userData.WinterMovementTime), FilterWinterMovementTime},
                {nameof(_userData.SummerMovementTime), FilterSummerMovementTime},
                {nameof(_userData.SelectedStoneDefs), FilterStones},
                {nameof(_userData.ChosenCoastalTileState), FilterCoastal},
                {nameof(_userData.Elevation), FilterElevation},
                {nameof(_userData.TimeZone), FilterTimeZone},
                /* temperature */
                {nameof(_userData.AverageTemperature), FilterAverageTemperature},
                {nameof(_userData.WinterTemperature), FilterWinterTemperature},
                {nameof(_userData.SummerTemperature), FilterSummerTemperature},
                {nameof(_userData.GrowingPeriod), FilterGrowingPeriod},
                {nameof(_userData.RainFall), FilterRainfall},
                {nameof(_userData.ChosenAnimalsCanGrazeNowState), FilterAnimalsCanGrazeNow}
            };
        }

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

            Log.Message(
                $"[PrepareLanding] Prefilter: {_allValidTileIds.Count} tiles remain after filter ({Find.WorldGrid.tiles.Count - _allValidTileIds.Count} removed).");


            // get all tiles with at least one river
            _allTilesWithRivers = _allValidTileIds.FindAll(
                tileId => Find.World.grid[tileId].VisibleRivers != null &&
                          Find.World.grid[tileId].VisibleRivers.Count != 0);
            Log.Message($"[PrepareLanding] Prefilter: {_allTilesWithRivers.Count} tiles with at least one river.");

            // get all tiles with at least one road
            _allTilesWithRoads =
                _allValidTileIds.FindAll(tileId => Find.World.grid[tileId].VisibleRoads != null &&
                                                   Find.World.grid[tileId].VisibleRoads.Count != 0);
            Log.Message($"[PrepareLanding] Prefilter: {_allTilesWithRoads.Count} tiles with at least one road.");
        }

        public void Filter()
        {
            // do a preventive check before filtering anything
            if (!FilterPreCheck())
                return;

            // clear all previous matching tiles
            ClearMatchingTiles();

            // remove all previously highlighted tiles on the world map
            PrepareLanding.Instance.TileDrawer.RemoveAllTiles();

            /* 
             * start filtering 
             */

            // filter out everything
            var result = Enumerable.Empty<int>();
            var firstUnionDone = false;
            foreach (var filteredList in _allFilteredLists)
            {
                if (filteredList == null)
                {
                    Log.Error("[PrepareLanding: Filter(): a list was not initialized properly.");
                    continue;
                }

                if (filteredList.Count == 0)
                    continue;

                // the first is an union and the other one are intersections
                if (!firstUnionDone)
                {
                    result = filteredList.Union(result);
                    firstUnionDone = true;
                }
                else
                {
                    result = filteredList.Intersect(result);
                }
            }

            // all results into one list
            _matchingTileIds.AddRange(result);

            // highlight filtered tiles
            PrepareLanding.Instance.TileDrawer.HighlightTileList(_matchingTileIds);

            Log.Message($"Matching Tiles: {_matchingTileIds.Count}");
        }

        private bool FilterPreCheck()
        {
            // clear filter info
            _filterInfo.Clear();

            if (Find.World.info.planetCoverage >= 0.5f)
            {
                if (_filteredBiomes.Count == 0 || _filteredHilliness.Count == 0 || _filteredBiomes.Count == _allValidTileIds.Count || _filteredHilliness.Count == _allValidTileIds.Count)
                {
                    _filterInfo.AppendErrorMessage("No biome and no terrain selected for Planet coverage >= 50%\n\tPlease select a biome and a terrain first.");
                    return false;
                }
            }

            return true;

        }

        private void OnDataPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            // check if live filtering is allowed or not. If it's not allowed, we filter everything on the 'Filter' button push.
            if (!PrepareLanding.Instance.UserData.AllowLiveFiltering)
                return;

            Action filter;
            if (!_allFilterActions.TryGetValue(e.PropertyName, out filter))
            {
                Log.Message(
                    $"[PrepareLanding] [OnDataPropertyChanged] An unknown property name was passed: {e.PropertyName}");

                return;
            }

            // call the filter
            filter();
        }

        #region TILE_FILTERS

        /* A note about filters:
         *   - Filters are *exclusive* by default, which means if a filter does not filter anything it must an empty sequence!
         *   - Filter have their own output list. 
         *   - We don't filter everything out on each user interaction: this final filtering is only done when the 'Filter' button is clicked
         */

        protected virtual void FilterBiomes()
        {
            _filteredBiomes.Clear();

            var chosenBiome = _userData.ChosenBiome;

            // a null biome means any biomes, so return all tiles
            if (chosenBiome == null)
            {
                _filteredBiomes.AddRange(_allValidTileIds);
                return;
            }

            foreach (var tileId in _allValidTileIds)
                if (Find.World.grid[tileId].biome == chosenBiome)
                    _filteredBiomes.Add(tileId);
        }

        protected virtual void FilterHilliness()
        {
            _filteredHilliness.Clear();

            var chosenHilliness = _userData.ChosenHilliness;

            // an undefined hilliness means 'any' type of hilliness, so all tile match
            if (chosenHilliness == Hilliness.Undefined)
            {
                _filteredBiomes.AddRange(_allValidTileIds);
                return;
            }

            foreach (var tileId in _allValidTileIds)
                if (Find.World.grid[tileId].hilliness == chosenHilliness)
                    _filteredHilliness.Add(tileId);
        }

        protected virtual void FilterRoads()
        {
            _filteredRoads.Clear();

            var roadDefs = _userData.SelectedRoadDefs;
            foreach (var entry in roadDefs)
            {
                var currentRoadDef = entry.Key;

                if (entry.Value.State != MultiCheckboxState.Off)
                    foreach (var tileId in _allTilesWithRoads)
                    foreach (var roadLink in Find.World.grid[tileId].VisibleRoads)
                        if (roadLink.road == currentRoadDef)
                            _filteredRoads.Add(tileId);
            }
        }

        protected virtual void FilterStones()
        {
            _filteredStones.Clear();

            // collect stones that are in On & Partial states, in their precise order on the GUI!
            var orderedStoneDefsOn = (from stone in _userData.OrderedStoneDefs
                let threeStateItem = _userData.SelectedStoneDefs[stone]
                where threeStateItem.State == MultiCheckboxState.On
                select stone).ToList();
            var orderedStoneDefsPartial = (from stone in _userData.OrderedStoneDefs
                let threeStateItem = _userData.SelectedStoneDefs[stone]
                where threeStateItem.State == MultiCheckboxState.Partial
                select stone).ToList();
            var orderedStoneDefsOnPartial = new List<ThingDef>();
            orderedStoneDefsOnPartial.AddRange(orderedStoneDefsOn);
            orderedStoneDefsOnPartial.AddRange(orderedStoneDefsPartial);
            // stone types explicitly marked OFF
            var stoneOffList = (from userDataSelectedStoneDef in _userData.SelectedStoneDefs
                where userDataSelectedStoneDef.Value.State == MultiCheckboxState.Off
                select userDataSelectedStoneDef.Key).ToList();

            var orderedStoneDefsOnCount = orderedStoneDefsOn.Count;

            // the game doesn't select more than 3 stone types per tile
            if (orderedStoneDefsOnCount > 3)
                return;

            // the game use 2 to 3 types of stone per tile, so we must have at least 2 chosen types of stones 
            if (orderedStoneDefsOnPartial.Count < 2)
                return;

            foreach (var tileId in _allValidTileIds)
            {
                // get stone types in this tile
                var tileStones = Find.World.NaturalRockTypesIn(tileId).ToList();

                // we don't want any tile that has one or more of the forbidden stone types
                if (stoneOffList.Count > 0)
                {
                    var containsUnwantedStone = Enumerable.Any(tileStones, stoneDef => stoneOffList.Contains(stoneDef));
                    if (containsUnwantedStone)
                        continue;
                }

                // is there any must have stone types?
                if (orderedStoneDefsOn.Count > 0)
                {
                    if (orderedStoneDefsOnCount < 3)
                    {
                        // the list with the fewer elements will be the subset list, the one with the most elements will be the containing list.
                        var subset = tileStones.Count <= orderedStoneDefsOn.Count ? tileStones : orderedStoneDefsOn;
                        var containingList = subset == tileStones ? orderedStoneDefsOn : tileStones;

                        // check if the subset list has the same stone types at the same position in the containing list.
                        if (IsSubsetInOrderSamePos(subset, containingList))
                            _filteredStones.Add(tileId);
                    }
                    // maximum must-have stone types
                    else if (orderedStoneDefsOnCount == 3)
                    {
                        // just check that both lists are equals (same content *and* in the same order!)
                        if (tileStones.SequenceEqual(orderedStoneDefsOn))
                            _filteredStones.Add(tileId);
                    }
                    continue;
                }

                // partial stones (may or may not be present)
                if (orderedStoneDefsOnPartial.Count > 0)
                    if (tileStones.IsSubset(orderedStoneDefsOnPartial))
                        _filteredStones.Add(tileId);
            }
        }

        protected virtual void FilterRivers()
        {
            _filteredRivers.Clear();

            foreach (var selectedRiverDef in _userData.SelectedRiverDefs)
            {
                if (selectedRiverDef.Value.State != MultiCheckboxState.On &&
                    selectedRiverDef.Value.State != MultiCheckboxState.Partial)
                    continue;

                foreach (var tileId in _allTilesWithRivers)
                {
                    // note : even though there are multiple rivers in a tile, only the one with the biggest degradeThreshold makes it to the playable map
                    var riverLink = Find.World.grid[tileId].VisibleRivers
                        .MaxBy(riverlink => riverlink.river.degradeThreshold);

                    if (riverLink.river == selectedRiverDef.Key)
                        _filteredRivers.Add(tileId);
                }
            }
        }

        public void FilterCurrentMovementTime()
        {
            _filteredCurrentMovementTime.Clear();

            if (!_userData.CurrentMovementTime.Use)
                return;


            var tileIdsCount = _allValidTileIds.Count;
            for (var i = 0; i < tileIdsCount; i++)
            {
                var tileId = _allValidTileIds[i];

                // must be passable
                if (!Find.World.Impassable(tileId))
                    continue;

                FilterMovementTime(tileId, -1f, _userData.CurrentMovementTime, _filteredCurrentMovementTime);
            }
        }

        public void FilterWinterMovementTime()
        {
            _filteredWinterMovementTime.Clear();

            if (!_userData.WinterMovementTime.Use)
                return;

            var tileIdsCount = _allValidTileIds.Count;
            for (var i = 0; i < tileIdsCount; i++)
            {
                var tileId = _allValidTileIds[i];

                // must be passable
                if (!Find.World.Impassable(tileId))
                    continue;

                var y = Find.WorldGrid.LongLatOf(tileId).y;
                var yearPct = Season.Winter.GetMiddleYearPct(y);

                FilterMovementTime(tileId, yearPct, _userData.WinterMovementTime, _filteredWinterMovementTime);
            }
        }

        public void FilterSummerMovementTime()
        {
            _filteredSummerMovementTime.Clear();

            if (!_userData.SummerMovementTime.Use)
                return;

            var tileIdsCount = _allValidTileIds.Count;
            for (var i = 0; i < tileIdsCount; i++)
            {
                var tileId = _allValidTileIds[i];

                // must be passable
                if (!Find.World.Impassable(tileId))
                    continue;

                var y = Find.WorldGrid.LongLatOf(tileId).y;
                var yearPct = Season.Summer.GetMiddleYearPct(y);

                FilterMovementTime(tileId, yearPct, _userData.SummerMovementTime, _filteredSummerMovementTime);
            }
        }

        protected void FilterMovementTime(int tileId, float yearPct, UsableMinMaxNumericItem<float> item,
            List<int> resultList)
        {
            var ticks = Mathf.Min(GenDate.TicksPerHour + WorldPathGrid.CalculatedCostAt(tileId, false, yearPct),
                Caravan_PathFollower.MaxMoveTicks);

            int years, quadrums, days;
            float hours;
            ticks.TicksToPeriod(out years, out quadrums, out days, out hours);

            // combine everything into hours; note that we shouldn't get anything other than 'hours' and 'days'. Technically, a tile is should be passable in less than 48 hours.
            var totalHours = hours + days * GenDate.HoursPerDay +
                             quadrums * GenDate.DaysPerQuadrum * GenDate.HoursPerDay +
                             years * GenDate.DaysPerTwelfth * GenDate.TwelfthsPerYear * GenDate.HoursPerDay;

            //TODO: see how RimWorld rounds movement time numbers; e.g 4.06 is 4.1, does that mean that 4.02 is 4?

            if (item.InRange(totalHours))
                resultList.Add(tileId);
        }

        protected virtual void FilterCoastal()
        {
            _filteredCoastalTiles.Clear();

            switch (_userData.ChosenCoastalTileState)
            {
                case MultiCheckboxState.On:
                    // gather all tiles that are coastal tiles
                    foreach (var tileId in _allValidTileIds)
                        if (IsCoastalTile(tileId))
                            _filteredCoastalTiles.Add(tileId);
                    break;
                case MultiCheckboxState.Off:
                    // get only tiles that are *not* coastal tiles
                    foreach (var tileId in _allValidTileIds)
                        if (!IsCoastalTile(tileId))
                            _filteredCoastalTiles.Add(tileId);
                    break;

                case MultiCheckboxState.Partial:
                    // consider it as "I don't care if it's coastal or not", so: all tiles match
                    _filteredCoastalTiles.AddRange(_allValidTileIds);
                    break;

                default:
                    // shouldn't happen but... anyway...
                    Log.Error("Unknown case for MultiCheckboxState.");
                    break;
            }
        }

        protected virtual void FilterElevation()
        {
            _filteredElevation.Clear();

            if (!_userData.Elevation.Use)
                return;

            foreach (var tileId in _allValidTileIds)
            {
                var tile = Find.World.grid[tileId];

                if (_userData.Elevation.InRange(tile.elevation))
                    _filteredElevation.Add(tileId);
            }
        }

        protected virtual void FilterTimeZone()
        {
            _filteredTimeZone.Clear();

            if (!_userData.TimeZone.Use)
                return;

            foreach (var tileId in _allValidTileIds)
            {
                var timeZone = GenDate.TimeZoneAt(Find.WorldGrid.LongLatOf(tileId).x);
                if (_userData.TimeZone.InRange(timeZone))
                    _filteredTimeZone.Add(tileId);
            }
        }

        protected virtual void FilterAverageTemperature()
        {
            _filteredAverageTemperature.Clear();

            if (!_userData.AverageTemperature.Use)
                return;

            foreach (var tileId in _allValidTileIds)
            {
                var tile = Find.World.grid[tileId];

                if (_userData.AverageTemperature.InRange(tile.temperature))
                    _filteredAverageTemperature.Add(tileId);
            }
        }

        protected virtual void FilterWinterTemperature()
        {
            _filteredWinterTemperature.Clear();

            if (!_userData.WinterTemperature.Use)
                return;

            foreach (var tileId in _allValidTileIds)
            {
                var y = Find.WorldGrid.LongLatOf(tileId).y;

                var celsiusTemp =
                    GenTemperature.AverageTemperatureAtTileForTwelfth(tileId, Season.Winter.GetMiddleTwelfth(y));

                if (_userData.WinterTemperature.InRange(celsiusTemp))
                    _filteredWinterTemperature.Add(tileId);
            }
        }

        protected virtual void FilterSummerTemperature()
        {
            _filteredSummerTemperature.Clear();

            if (!_userData.SummerTemperature.Use)
                return;

            foreach (var tileId in _allValidTileIds)
            {
                var y = Find.WorldGrid.LongLatOf(tileId).y;

                var celsiusTemp =
                    GenTemperature.AverageTemperatureAtTileForTwelfth(tileId, Season.Summer.GetMiddleTwelfth(y));

                if (_userData.SummerTemperature.InRange(celsiusTemp))
                    _filteredSummerTemperature.Add(tileId);
            }
        }

        protected virtual void FilterGrowingPeriod()
        {
            _filteredGrowingPeriod.Clear();

            if (!_userData.GrowingPeriod.Use)
                return;

            // TODO send problems to GUI tab
            if (!_userData.GrowingPeriod.Max.IsEqualOrGreaterGrowingPeriod(_userData.GrowingPeriod.Min))
                Messages.Message("Minimum growing period can't be greater than maximum growing period",
                    MessageSound.RejectInput);

            foreach (var tileId in _allValidTileIds)
            {
                // twelfthList is a list of Twelfth (where 1 twelfth is 5 days); the count of items indicates how much twelfths you can grow plants
                //   from 0 (no growing period) to 12 (60 days -> year round).
                var twelfthList = GenTemperature.TwelfthsInAverageTemperatureRange(tileId,
                    Plant.MinOptimalGrowthTemperature, Plant.MaxOptimalGrowthTemperature);
                var tileGrowingDays = twelfthList.Count * GenDate.DaysPerTwelfth;

                // GrowingPeriod.Min and GrowingPeriod.Max are only one twelfth,: it indicates *how many periods of 5 days* we must search for.
                // e.g Twelfth.Undefined is 0 days, Twelfth.First is 5 days, Twelfth.Second is 10 days, etc. up to Twelfth.Twelfth (12 * 5 = 60 days = 1 year [year-round])
                var minDays = _userData.GrowingPeriod.Min.ToGrowingDays();
                var maxDays = _userData.GrowingPeriod.Max.ToGrowingDays();

                if (tileGrowingDays >= minDays && tileGrowingDays <= maxDays)
                    _filteredGrowingPeriod.Add(tileId);
            }
        }

        protected virtual void FilterRainfall()
        {
            _filteredRainfall.Clear();

            if (!_userData.RainFall.Use)
                return;

            foreach (var tileId in _allValidTileIds)
            {
                var tile = Find.World.grid[tileId];

                if (_userData.RainFall.InRange(tile.rainfall))
                    _filteredRainfall.Add(tileId);
            }
        }

        protected virtual void FilterAnimalsCanGrazeNow()
        {
            _filteredAnimalsCanGrazeNow.Clear();

            // partial state means "I don't care if they can graze now or not", so all tiles match
            if (_userData.ChosenAnimalsCanGrazeNowState == MultiCheckboxState.Partial)
            {
                _filteredAnimalsCanGrazeNow.AddRange(_allValidTileIds);
                return;
            }

            foreach (var tileId in _allValidTileIds)
            {
                var canGrazeNow = VirtualPlantsUtility.EnvironmentAllowsEatingVirtualPlantsNowAt(tileId);
                if (_userData.ChosenAnimalsCanGrazeNowState == MultiCheckboxState.On)
                    if (canGrazeNow)
                        _filteredAnimalsCanGrazeNow.Add(tileId);

                if (_userData.ChosenAnimalsCanGrazeNowState == MultiCheckboxState.Off)
                    if (!canGrazeNow)
                        _filteredAnimalsCanGrazeNow.Add(tileId);
            }
        }

        #endregion TILE_FILTERS

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
        ///     Check if a <see cref="Tile" /> is a coastal tile.
        /// </summary>
        /// <param name="tileId">The ID of the tile to check.</param>
        /// <returns>true if the tile is a coastal tile, false otherwise.</returns>
        public static bool IsCoastalTile(int tileId)
        {
            var rot = Find.World.CoastDirectionAt(tileId);
            return rot.IsValid;
        }

        public static bool IsViableTile(int tileId)
        {
            var tile = Find.World.grid[tileId];

            // we must be able to build a base, the tile biome must be implemented and the tile itself must not be impassable
            // Side note on tile.WaterCovered: this doesn't work for sea ice as elevation is < 0, but sea ice is a perfectly valid biome where to settle.
            return tile.biome.canBuildBase && tile.biome.implemented && tile.hilliness != Hilliness.Impassable;
        }

        public bool IsSubsetInOrder(List<ThingDef> subsetList, List<ThingDef> other)
        {
            if (!subsetList.IsSubset(other))
                return false;

            var otherIndex = other.IndexOf(subsetList[0]);
            if (otherIndex + subsetList.Count > other.Count)
                return false;

            return !subsetList.Where((t, i) => other[i + otherIndex] != t).Any();
        }

        public bool IsSubsetInOrderSamePos(List<ThingDef> subsetList, List<ThingDef> other)
        {
            if (subsetList.Count > other.Count)
                return false;

            if (!subsetList.IsSubset(other))
                return false;

            return !subsetList.Where((t, i) => t != other[i]).Any();
        }

        #endregion PREDICATES
    }
}