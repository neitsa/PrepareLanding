using System;
using System.Collections.Generic;
using System.Linq;
using PrepareLanding.Core;
using PrepareLanding.Core.Extensions;
using PrepareLanding.GameData;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace PrepareLanding.Filters
{
    public class TileFilterBiomes : TileFilter
    {
        public TileFilterBiomes(UserData userData, string attachedProperty,
            FilterHeaviness heaviness) : base(userData, attachedProperty, heaviness)
        {
        }

        public override bool IsFilterActive => UserData.ChosenBiome != null;

        public override string SubjectThingDef => "PLTILFILT_Biomes".Translate();

        public override void Filter(List<int> inputList)
        {
            base.Filter(inputList);

            // a null biome means any biomes, so that means all tiles match
            if (!IsFilterActive)
                return;

            var chosenBiome = UserData.ChosenBiome;

            foreach (var tileId in inputList)
                if (Find.World.grid[tileId].biome == chosenBiome)
                    _filteredTiles.Add(tileId);
        }

        public static int NumberOfTilesByBiome(BiomeDef biome, List<int> inputList)
        {
            return inputList.Count(tileId => Find.World.grid[tileId].biome == biome);
        }

        public static int NumberOfTilesByBiome(BiomeDef biome)
        {
            return TileIdsByBiome(biome).Count;
        }

        public static List<int> TileIdsByBiome(BiomeDef biomeDef)
        {
            var outList = new List<int>();

            var maxTiles = Find.World.grid.TilesCount;
            for (var i = 0; i < maxTiles; i++)
                if (Find.World.grid[i].biome == biomeDef)
                    outList.Add(i);

            return outList;
        }
    }

    public class TileFilterHilliness : TileFilter
    {
        public TileFilterHilliness(UserData userData, string attachedProperty,
            FilterHeaviness heaviness) : base(userData, attachedProperty, heaviness)
        {
        }

        public override bool IsFilterActive => UserData.ChosenHilliness != Hilliness.Undefined;

        public override string SubjectThingDef => "PLTILFILT_Terrains".Translate();

        public override void Filter(List<int> inputList)
        {
            base.Filter(inputList);

            if (!IsFilterActive)
                return;

            var chosenHilliness = UserData.ChosenHilliness;

            foreach (var tileId in inputList)
                if (Find.World.grid[tileId].hilliness == chosenHilliness)
                    _filteredTiles.Add(tileId);
        }
    }

    public class TileFilterRoads : TileFilter
    {
        public TileFilterRoads(UserData userData, string attachedProperty,
            FilterHeaviness heaviness) : base(userData, attachedProperty, heaviness)
        {
        }

        public override bool IsFilterActive
        {
            get
            {
                var roadDefs = UserData.SelectedRoadDefs;
                return roadDefs.Any(entry => entry.Value.State != MultiCheckboxState.Partial);
            }
        }

        public override string SubjectThingDef => "PLTILFILT_Roads".Translate();

        public override void Filter(List<int> inputList)
        {
            base.Filter(inputList);

            if (!IsFilterActive)
                return;

            switch (UserData.SelectedRoadDefs.FilterBooleanState)
            {
                case FilterBoolean.AndFiltering:
                    FilterAnd(inputList, UserData.SelectedRoadDefs);
                    break;
                case FilterBoolean.OrFiltering:
                    FilterOr(inputList, UserData.SelectedRoadDefs, UserData.SelectedRoadDefs.OffPartialNoSelect);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        protected override bool TileHasDef(Tile tile)
        {
            return TileHasRoad(tile);
        }

        protected override List<T> TileDefs<T>(Tile tile)
        {
            var tileRoadDefs = TileHasDef(tile)
                ? tile.Roads.Select(roadlink => roadlink.road as T).Distinct().ToList()
                : null;

            return tileRoadDefs;
        }

        /// <summary>
        ///     Given a list of tiles, returns only tiles that have at least a road (of any type).
        /// </summary>
        /// <param name="inputList">A list of tiles from which to only get tiles with roads.</param>
        /// <returns>A <see cref="IEnumerable{T}" /> containing the tiles ids that have at least one type of road.</returns>
        public static IEnumerable<int> TilesWithRoads(List<int> inputList)
        {
            return inputList.Intersect(PrepareLanding.Instance.TileFilter.AllTilesWithRoad);
        }

        public static bool TileHasRoad(Tile tile)
        {
            return tile.Roads != null && tile.Roads.Count != 0;
        }
    }

    public class TileFilterStones : TileFilter
    {
        public TileFilterStones(UserData userData, string attachedProperty,
            FilterHeaviness heaviness) : base(userData, attachedProperty, heaviness)
        {
        }

        public override bool IsFilterActive
        {
            get
            {
                var stoneDefs = UserData.SelectedStoneDefs;
                return stoneDefs.Any(entry => entry.Value.State != MultiCheckboxState.Partial) ||
                       UserData.StoneTypesNumberOnly;
            }
        }

        public override string SubjectThingDef => "PLTILFILT_Stones".Translate();

        public override void Filter(List<int> inputList)
        {
            base.Filter(inputList);

            if (!IsFilterActive)
                return;

            // special case where we filter tiles with 2 or 3 stone types, whatever they are.
            if (UserData.StoneTypesNumberOnly)
            {
                var numberOfStones = UserData.StoneTypesNumber;
                foreach (var tileId in inputList)
                    // get number of stone types in the tile
                    if (Find.World.NaturalRockTypesIn(tileId).Count() == numberOfStones)
                        _filteredTiles.Add(tileId);

                return;
            }

            // collect stones that are in On & Partial states, in their precise order on the GUI!
            var orderedStoneDefsOn = (from stone in UserData.SelectedStoneDefs.OrderedItems
                let threeStateItem = UserData.SelectedStoneDefs[stone]
                where threeStateItem.State == MultiCheckboxState.On
                select stone).ToList();

            var orderedStoneDefsPartial = (
                from stone in UserData.SelectedStoneDefs.OrderedItems //UserData.OrderedStoneDefs
                let threeStateItem = UserData.SelectedStoneDefs[stone]
                where threeStateItem.State == MultiCheckboxState.Partial
                select stone).ToList();

            var orderedStoneDefsOnPartial = new List<ThingDef>();
            orderedStoneDefsOnPartial.AddRange(orderedStoneDefsOn);
            orderedStoneDefsOnPartial.AddRange(orderedStoneDefsPartial);

            // stone types explicitly marked OFF
            var stoneOffList = (from userDataSelectedStoneDef in UserData.SelectedStoneDefs
                where userDataSelectedStoneDef.Value.State == MultiCheckboxState.Off
                select userDataSelectedStoneDef.Key).ToList();

            var orderedStoneDefsOnCount = orderedStoneDefsOn.Count;

            // the game doesn't select more than 3 stone types per tile
            if (orderedStoneDefsOnCount > 3)
            {
                PrepareLanding.Instance.TileFilter.FilterInfoLogger.AppendErrorMessage(
                    $"{"PLTILFILT_CantSelectMoreThan3Stones".Translate()} {orderedStoneDefsOnCount}).");
                return;
            }

            // the game use 2 to 3 types of stone per tile, so we must have at least 2 chosen types of stones 
            if (orderedStoneDefsOnPartial.Count < 2)
            {
                PrepareLanding.Instance.TileFilter.FilterInfoLogger.AppendErrorMessage(
                    $"{"PLTILFILT_AtLeast2StoneTypesOnOrPartial".Translate()} {orderedStoneDefsOnPartial.Count}).");
                return;
            }

            foreach (var tileId in inputList)
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

                        if (UserData.SelectedStoneDefs.OrderedFiltering)
                        {
                            // check if the subset list has the same stone types at the same position in the containing list.
                            if (subset.IsSubsetInOrderSamePos(containingList))
                                _filteredTiles.Add(tileId);
                        }
                        else
                        {
                            // check if the subset list has the same stone types bot *not* necessarily at the same position in the containing list.
                            if (subset.IsSubset(containingList))
                                _filteredTiles.Add(tileId);
                        }
                    }
                    // maximum must-have stone types
                    else if (orderedStoneDefsOnCount == 3)
                    {
                        if (UserData.SelectedStoneDefs.OrderedFiltering)
                        {
                            // just check that both lists are equals (same content *and* in the same order!)
                            if (tileStones.SequenceEqual(orderedStoneDefsOn))
                                _filteredTiles.Add(tileId);
                        }
                        else
                        {
                            // just check that both lists are equals (same content *without* any precise order!)
                            if (tileStones.IsEqualNoOrderFast(orderedStoneDefsOn))
                                _filteredTiles.Add(tileId);
                        }
                    }
                    continue;
                }

                // partial stones (may or may not be present)
                if (orderedStoneDefsOnPartial.Count > 0)
                    if (tileStones.IsSubset(orderedStoneDefsOnPartial))
                        _filteredTiles.Add(tileId);
            }
        }
    }

    public class TileFilterRivers : TileFilter
    {
        public TileFilterRivers(UserData userData, string attachedProperty,
            FilterHeaviness heaviness) : base(userData, attachedProperty, heaviness)
        {
        }

        public override bool IsFilterActive
        {
            get
            {
                var stoneDefs = UserData.SelectedRiverDefs;
                return stoneDefs.Any(entry => entry.Value.State != MultiCheckboxState.Partial);
            }
        }

        public override string SubjectThingDef => "PLTILFILT_Rivers".Translate();

        
        public override void Filter(List<int> inputList)
        {
            base.Filter(inputList);

            if (!IsFilterActive)
                return;

            switch (UserData.SelectedRiverDefs.FilterBooleanState)
            {
                case FilterBoolean.AndFiltering:
                    FilterAnd(inputList, UserData.SelectedRiverDefs);
                    break;
                case FilterBoolean.OrFiltering:
                    FilterOr(inputList, UserData.SelectedRiverDefs, UserData.SelectedRiverDefs.OffPartialNoSelect);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        protected override bool TileHasDef(Tile tile)
        {
            return TileHasRiver(tile);
        }

        protected override List<T> TileDefs<T>(Tile tile)
        {
            if (!TileHasRiver(tile))
                return null;

            // note: even though there are multiple rivers in a tile, only the one with the biggest degradeThreshold makes it to the playable map
            var riverLink = tile.Rivers.MaxBy(riverlink => riverlink.river.degradeThreshold);

            return new List<T>{ riverLink.river as T };
        }

        public static IEnumerable<int> TilesWithRiver(IEnumerable<int> inputList)
        {
            return inputList.Intersect(PrepareLanding.Instance.TileFilter.AllTilesWithRiver);
        }

        public static bool TileHasRiver(Tile tile)
        {
            return tile.Rivers != null && tile.Rivers.Count != 0;
        }
    }

    public class TileFilterMovementDifficulty : TileFilter
    {
        public TileFilterMovementDifficulty(UserData userData, string attachedProperty, FilterHeaviness heaviness) : base(userData, attachedProperty, heaviness)
        {
        }

        public override string SubjectThingDef => "MovementDifficulty".Translate();

        public override bool IsFilterActive => UserData.MovementDifficulty.Use;

        public override void Filter(List<int> inputList)
        {
            base.Filter(inputList);

            if (!IsFilterActive)
                return;

            foreach (var tileId in inputList)
            {
                var difficulty = WorldPathGrid.CalculatedMovementDifficultyAt(tileId, false) *
                                 Find.WorldGrid.GetRoadMovementDifficultyMultiplier(tileId, -1);

                if(UserData.MovementDifficulty.InRange(difficulty))
                    FilteredTiles.Add(tileId);
            }
        }
    }

    public class TileFilterForageability : TileFilter
    {
        public TileFilterForageability(UserData userData, string attachedProperty, FilterHeaviness heaviness) : base(userData, attachedProperty, heaviness)
        {
        }

        public override string SubjectThingDef => "Forageability".Translate();

        public override bool IsFilterActive => UserData.Forageability.Use;

        public override void Filter(List<int> inputList)
        {
            //Log.Message($"[PrepareLanding] Filtering TileFilterForageability ({inputList.Count} tiles in input)");
            //Log.Message($"[PrepareLanding] Min: {UserData.Forageability.Min}; Max: {UserData.Forageability.Max}");
            foreach (var tileId in inputList)
            {
                var tile = Find.WorldGrid[tileId];
                if (tile.biome.foragedFood == null)
                    continue;

                //Log.Message($"[PL] Tile: {tileId}; forageability: {tile.biome.forageability}");

                // forageability is a %age, so 25% is 0.25, we have to multiply by 100.
                if (UserData.Forageability.InRange(tile.biome.forageability * 100f)) 
                    FilteredTiles.Add(tileId);
            }
        }
    }

    public class TileFilterForageableFood : TileFilter
    {
        public TileFilterForageableFood(UserData userData, string attachedProperty, FilterHeaviness heaviness) : base(userData, attachedProperty, heaviness)
        {
        }

        public override string SubjectThingDef => "Forageable";

        public override bool IsFilterActive => UserData.ForagedFood != null;

        public override void Filter(List<int> inputList)
        {
            base.Filter(inputList);

            if (!IsFilterActive)
                return;

            foreach (var tileId in inputList)
            {
                var tile = Find.WorldGrid[tileId];

                if(tile.biome.foragedFood != null && tile.biome.foragedFood == UserData.ForagedFood)
                    FilteredTiles.Add(tileId);
            }
        }
    }

    public class TileFilterCoastalTiles : TileFilter
    {
        public TileFilterCoastalTiles(UserData userData, string attachedProperty,
            FilterHeaviness heaviness) : base(userData, attachedProperty, heaviness)
        {
        }

        public override bool IsFilterActive => UserData.ChosenCoastalTileState != MultiCheckboxState.Partial;

        public override string SubjectThingDef => "PLTILFILT_CoastalTiles".Translate();

        public override void Filter(List<int> inputList)
        {
            base.Filter(inputList);

            if (!IsFilterActive)
                return;

            switch (UserData.ChosenCoastalTileState)
            {
                case MultiCheckboxState.On:
                    // gather all tiles that are coastal tiles
                    foreach (var tileId in inputList)
                        if (IsCoastalTile(tileId))
                            _filteredTiles.Add(tileId);
                    break;
                case MultiCheckboxState.Off:
                    // get only tiles that are *not* coastal tiles
                    foreach (var tileId in inputList)
                        if (!IsCoastalTile(tileId))
                            _filteredTiles.Add(tileId);
                    break;

                case MultiCheckboxState.Partial:
                    // consider it as "I don't care if it's coastal or not", so: all tiles match
                    _filteredTiles.AddRange(inputList);
                    break;

                default:
                    // shouldn't happen but... anyway...
                    Log.Error("Unknown case for MultiCheckboxState.");
                    break;
            }
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
    }

    public class TileFilterCoastalLakeTiles : TileFilter
    {
        private static readonly List<Rot4> TmpLakeDirs = new List<Rot4>();

        private static readonly List<int> TmpNeighbors = new List<int>();

        public TileFilterCoastalLakeTiles(UserData userData, string attachedProperty,
            FilterHeaviness heaviness) : base(userData, attachedProperty, heaviness)
        {
        }

        public override bool IsFilterActive => UserData.ChosenCoastalLakeTileState != MultiCheckboxState.Partial;

        public override string SubjectThingDef => "PLTILFILT_CoastalLakeTiles".Translate();

        public override void Filter(List<int> inputList)
        {
            base.Filter(inputList);

            if (!IsFilterActive)
                return;

            switch (UserData.ChosenCoastalLakeTileState)
            {
                case MultiCheckboxState.On:
                    // gather all tiles that are coastal tiles
                    foreach (var tileId in inputList)
                        if (IsCoastalLakeTile(tileId))
                            _filteredTiles.Add(tileId);
                    break;
                case MultiCheckboxState.Off:
                    // get only tiles that are *not* coastal tiles
                    foreach (var tileId in inputList)
                        if (!IsCoastalLakeTile(tileId))
                            _filteredTiles.Add(tileId);
                    break;

                case MultiCheckboxState.Partial:
                    // consider it as "I don't care if it's coastal or not", so: all tiles match
                    _filteredTiles.AddRange(inputList);
                    break;

                default:
                    // shouldn't happen but... anyway...
                    Log.Error("Unknown case for MultiCheckboxState.");
                    break;
            }
        }

        /// <summary>
        ///     Check if a <see cref="Tile" /> is a coastal tile of a lake (works <b>only</b> for lakes).
        /// </summary>
        /// <param name="tileId">The ID of the tile to check.</param>
        /// <returns>true if the tile is a coastal tile of a lake, false otherwise.</returns>
        public static bool IsCoastalLakeTile(int tileId)
        {
            var rot = CoastDirectionAt(tileId);
            return rot.IsValid;
        }

        public static Rot4 CoastDirectionAt(int tileId)
        {
            var tile = Find.World.grid[tileId];
            if (!tile.biome.canBuildBase)
            {
                return Rot4.Invalid;
            }
            TmpLakeDirs.Clear();
            Find.World.grid.GetTileNeighbors(tileId, TmpNeighbors);
            var i = 0;
            var count = TmpNeighbors.Count;
            while (i < count)
            {
                var tile2 = Find.World.grid[TmpNeighbors[i]];
                if (tile2.biome == BiomeDefOf.Lake)
                {
                    var rotFromTo = Find.World.grid.GetRotFromTo(tileId, TmpNeighbors[i]);
                    if (!TmpLakeDirs.Contains(rotFromTo))
                    {
                        TmpLakeDirs.Add(rotFromTo);
                    }
                }
                i++;
            }
            if (TmpLakeDirs.Count == 0)
            {
                return Rot4.Invalid;
            }
            Rand.PushState();
            Rand.Seed = tileId;
            var index = Rand.Range(0, TmpLakeDirs.Count);
            Rand.PopState();
            return TmpLakeDirs[index];
        }
    }

    public class TileFilterElevations : TileFilter
    {
        public TileFilterElevations(UserData userData, string attachedProperty,
            FilterHeaviness heaviness) : base(userData, attachedProperty, heaviness)
        {
        }

        public override bool IsFilterActive => UserData.Elevation.Use;

        public override string SubjectThingDef => "PLTILFILT_Elevations".Translate();

        public override void Filter(List<int> inputList)
        {
            base.Filter(inputList);

            if (!IsFilterActive)
                return;

            if (!UserData.Elevation.IsCorrectRange)
            {
                var message =
                    $"{SubjectThingDef}: {"PLFILT_VerifyMinIsLessOrEqualMax".Translate()}: {UserData.Elevation.Min} <= {UserData.Elevation.Max}).";
                PrepareLanding.Instance.TileFilter.FilterInfoLogger.AppendErrorMessage(message);
                return;
            }

            foreach (var tileId in inputList)
            {
                var tile = Find.World.grid[tileId];

                if (UserData.Elevation.InRange(tile.elevation))
                    _filteredTiles.Add(tileId);
            }
        }
    }

    public class TileFilterTimeZones : TileFilter
    {
        public TileFilterTimeZones(UserData userData, string attachedProperty,
            FilterHeaviness heaviness) : base(userData, attachedProperty, heaviness)
        {
        }

        public override bool IsFilterActive => UserData.TimeZone.Use;

        public override string SubjectThingDef => "PLTILFILT_TimeZones".Translate();


        public override void Filter(List<int> inputList)
        {
            base.Filter(inputList);

            if (!IsFilterActive)
                return;

            if (!UserData.TimeZone.IsCorrectRange)
            {
                var message =
                    $"{SubjectThingDef}: {"PLFILT_VerifyMinIsLessOrEqualMax".Translate()}: {UserData.TimeZone.Min} <= {UserData.TimeZone.Max}).";
                PrepareLanding.Instance.TileFilter.FilterInfoLogger.AppendErrorMessage(message);
                return;
            }

            foreach (var tileId in inputList)
            {
                var timeZone = GenDate.TimeZoneAt(Find.WorldGrid.LongLatOf(tileId).x);
                if (UserData.TimeZone.InRange(timeZone))
                    _filteredTiles.Add(tileId);
            }
        }
    }

    public class TileFilterAverageTemperatures : TileFilterTemperatures
    {
        public TileFilterAverageTemperatures(UserData userData, string attachedProperty,
            FilterHeaviness heaviness) : base(userData, attachedProperty, heaviness)
        {
        }

        public override bool IsFilterActive => UserData.AverageTemperature.Use;

        public override string SubjectThingDef => "PLTILFILT_AverageTemperatures".Translate();
    }

    public class TileFilterMinTemperatures : TileFilterTemperatures
    {
        public TileFilterMinTemperatures(UserData userData, string attachedProperty,
            FilterHeaviness heaviness) : base(userData, attachedProperty, heaviness)
        {
        }

        public override bool IsFilterActive => UserData.MinTemperature.Use;

        public override string SubjectThingDef => "Min Temperatures";
    }

    public class TileFilterMaxTemperatures : TileFilterTemperatures
    {
        public TileFilterMaxTemperatures(UserData userData, string attachedProperty,
            FilterHeaviness heaviness) : base(userData, attachedProperty, heaviness)
        {
        }

        public override bool IsFilterActive => UserData.MaxTemperature.Use;

        public override string SubjectThingDef => "Max Temperatures";
    }

    public class TileFilterGrowingPeriods : TileFilter
    {
        public TileFilterGrowingPeriods(UserData userData, string attachedProperty,
            FilterHeaviness heaviness) : base(userData, attachedProperty, heaviness)
        {
        }

        public override bool IsFilterActive => UserData.GrowingPeriod.Use;

        public override string SubjectThingDef => "PLTILFILT_GrowingPeriods".Translate();

        public override void Filter(List<int> inputList)
        {
            base.Filter(inputList);

            if (!IsFilterActive)
                return;

            if (!UserData.GrowingPeriod.Max.IsEqualOrGreaterGrowingPeriod(UserData.GrowingPeriod.Min))
            {
                var minGrowingDays = UserData.GrowingPeriod.Min.ToGrowingDays();
                var maxGrowingDays = UserData.GrowingPeriod.Max.ToGrowingDays();
                var message =
                    $"{SubjectThingDef}: {"PLFILT_VerifyMinIsLessOrEqualMax".Translate()}: {minGrowingDays} days <= {maxGrowingDays} days).";
                PrepareLanding.Instance.TileFilter.FilterInfoLogger.AppendErrorMessage(message);
                return;
            }

            foreach (var tileId in inputList)
            {
                // twelfthList is a list of Twelfth (where 1 twelfth is 5 days); the count of items indicates how much twelfths you can grow plants
                //   from 0 (no growing period) to 12 (60 days -> year round).
                var twelfthList = GenTemperature.TwelfthsInAverageTemperatureRange(tileId,
                    Plant.MinOptimalGrowthTemperature, Plant.MaxOptimalGrowthTemperature);
                var tileGrowingDays = twelfthList.Count * GenDate.DaysPerTwelfth;

                // GrowingPeriod.Min and GrowingPeriod.Max are only one twelfth,: it indicates *how many periods of 5 days* we must search for.
                // e.g Twelfth.Undefined is 0 days, Twelfth.First is 5 days, Twelfth.Second is 10 days, etc. up to Twelfth.Twelfth (12 * 5 = 60 days = 1 year [year-round])
                var minDays = UserData.GrowingPeriod.Min.ToGrowingDays();
                var maxDays = UserData.GrowingPeriod.Max.ToGrowingDays();

                if (tileGrowingDays >= minDays && tileGrowingDays <= maxDays)
                    _filteredTiles.Add(tileId);
            }
        }
    }

    public class TileFilterRainFalls : TileFilter
    {
        public TileFilterRainFalls(UserData userData, string attachedProperty,
            FilterHeaviness heaviness) : base(userData, attachedProperty, heaviness)
        {
        }

        public override bool IsFilterActive => UserData.RainFall.Use;

        public override string SubjectThingDef => "PLTILFILT_RainFalls".Translate();

        public override void Filter(List<int> inputList)
        {
            base.Filter(inputList);

            if (!IsFilterActive)
                return;

            if (!UserData.RainFall.IsCorrectRange)
            {
                var message =
                    $"{SubjectThingDef}: {"PLFILT_VerifyMinIsLessOrEqualMax".Translate()}: {UserData.RainFall.Min} <= {UserData.RainFall.Max}).";
                PrepareLanding.Instance.TileFilter.FilterInfoLogger.AppendErrorMessage(message);
                return;
            }

            foreach (var tileId in inputList)
            {
                var tile = Find.World.grid[tileId];

                if (UserData.RainFall.InRange(tile.rainfall))
                    _filteredTiles.Add(tileId);
            }
        }
    }

    public class TileFilterAnimalsCanGrazeNow : TileFilter
    {
        public TileFilterAnimalsCanGrazeNow(UserData userData, string attachedProperty,
            FilterHeaviness heaviness) : base(userData, attachedProperty, heaviness)
        {
        }

        public override bool IsFilterActive => UserData.ChosenAnimalsCanGrazeNowState != MultiCheckboxState.Partial;

        public override string SubjectThingDef => "AnimalsCanGrazeNow".Translate();

        public override void Filter(List<int> inputList)
        {
            base.Filter(inputList);

            if (!IsFilterActive)
                return;

            // partial state means "I don't care if they can graze now or not", so all tiles match
            if (UserData.ChosenAnimalsCanGrazeNowState == MultiCheckboxState.Partial)
            {
                _filteredTiles.AddRange(inputList);
                return;
            }

            try
            {
                GameTicks.PushTickAbs();
                foreach (var tileId in inputList)
                {
                    var canGrazeNow = VirtualPlantsUtility.EnvironmentAllowsEatingVirtualPlantsNowAt(tileId);
                    if (UserData.ChosenAnimalsCanGrazeNowState == MultiCheckboxState.On)
                        if (canGrazeNow)
                            _filteredTiles.Add(tileId);

                    if (UserData.ChosenAnimalsCanGrazeNowState == MultiCheckboxState.Off)
                        if (!canGrazeNow)
                            _filteredTiles.Add(tileId);
                }
            }
            finally
            {
                GameTicks.PopTickAbs();
            }
        }
    }

    public class TileFilterHasCave : TileFilter
    {
        public TileFilterHasCave(UserData userData, string attachedProperty,
            FilterHeaviness heaviness) : base(userData, attachedProperty, heaviness)
        {
        }

        public override bool IsFilterActive => UserData.HasCaveState != MultiCheckboxState.Partial;

        public override string SubjectThingDef => "HasCaves".Translate();

        public override void Filter(List<int> inputList)
        {
            base.Filter(inputList);

            if (!IsFilterActive)
                return;

            // partial state means "I don't care if they can graze now or not", so all tiles match
            if (UserData.HasCaveState == MultiCheckboxState.Partial)
            {
                _filteredTiles.AddRange(inputList);
                return;
            }

            foreach (var tileId in inputList)
            {
                var hasCave = Find.World.HasCaves(tileId);
                if (UserData.HasCaveState == MultiCheckboxState.On)
                    if (hasCave)
                        _filteredTiles.Add(tileId);

                if (UserData.HasCaveState == MultiCheckboxState.Off)
                    if (!hasCave)
                        _filteredTiles.Add(tileId);
            }
        }
    }

    public class TileFilterMostLeastCharacteristic : TileFilter
    {
        public TileFilterMostLeastCharacteristic(UserData userData, string attachedProperty, FilterHeaviness heaviness) : base(
            userData, attachedProperty, heaviness)
        {
        }

        public override bool IsFilterActive => !UserData.MostLeastItem.IsInDefaultState;

        public override string SubjectThingDef => $"{"PLMWT2T_MostLeastCharacteristics".Translate()}: {UserData.MostLeastItem.Characteristic}";

        public override void Filter(List<int> inputList)
        {
            base.Filter(inputList);

            if (!IsFilterActive)
                return;

            if (Enumerable.Any(PrepareLanding.Instance.GameData.WorldData.WorldCharacteristics,
                worldCharacteristicData => worldCharacteristicData.Characteristic == UserData.MostLeastItem.Characteristic))
                FilterCharacteristic(UserData.MostLeastItem);
        }

        private void FilterCharacteristic(MostLeastItem item)
        {
            var worldCharacteristicData = PrepareLanding.Instance.GameData.WorldData.WorldCharacteristicDataByCharacteristic(item.Characteristic);
            if (worldCharacteristicData == null)
                return;

            // we want either most or least.
            if (item.CharacteristicType == MostLeastType.None)
                return;

            // get list of KeyValuePair with key being the tile ID and value being the tile characteristic for the whole world
            //    e.g List<KVP<int, float>> where int is tileId and float is temperature or rainfall
            var worldTilesCharacteristics = worldCharacteristicData.WorldTilesCharacteristics;

            // can't request more tiles than there are in the world
            if (UserData.MostLeastItem.NumberOfItems > worldTilesCharacteristics.Count)
            {
                Messages.Message(
                    $"{"PLTILFILT_RequestingMoreTilesThanAvailable".Translate()} {worldTilesCharacteristics.Count})",
                    MessageTypeDefOf.RejectInput);
                return;
            }

            // get a list of KVP where key is tile Id and value is the characteristic value (e.g temperature, rainfall, elevation)
            var mostLeastTilesAndCharacteristics = item.CharacteristicType == MostLeastType.Least
                ? worldCharacteristicData.WorldMinRange(UserData.MostLeastItem.NumberOfItems)
                : worldCharacteristicData.WorldMaxRange(UserData.MostLeastItem.NumberOfItems);
            if (mostLeastTilesAndCharacteristics == null)
                return;

            // we still have a list of KVP, we just want the tiles
            var tileIds = mostLeastTilesAndCharacteristics.Select(kvp => kvp.Key);

            // add them to the filtered tiles list.
            _filteredTiles.AddRange(tileIds);
        }
    }

    public class TileFilterWorldFeature : TileFilter
    {
        public TileFilterWorldFeature(UserData userData, string attachedProperty,
            FilterHeaviness heaviness) : base(userData, attachedProperty, heaviness)
        {
        }

        public override bool IsFilterActive => UserData.WorldFeature != null;

        public override string SubjectThingDef => "PLTILFILT_WorldFeatures".Translate();

        public override void Filter(List<int> inputList)
        {
            base.Filter(inputList);

            if (!IsFilterActive)
                return;

            // intersect the tiles in the world feature with the ones from the input list.
            var result = UserData.WorldFeature.Tiles.Intersect(inputList);

            _filteredTiles.AddRange(result.ToList());

        }
    }

    public class TileFilterCoastRotation : TileFilter
    {
        // make sure both lists are the same
        public static readonly List<Rot4> PossibleRotations = new List<Rot4> { Rot4.North, Rot4.East, Rot4.South, Rot4.West};
        public static readonly List<int> PossibleRotationsInt = new List<int>
        {
            Rot4.North.AsInt, Rot4.East.AsInt, Rot4.South.AsInt, Rot4.West.AsInt
        };

        public TileFilterCoastRotation(UserData userData, string attachedProperty,
            FilterHeaviness heaviness) : base(userData, attachedProperty, heaviness)
        {
        }

        public override bool IsFilterActive => UserData.CoastalRotation.Use;

        public override string SubjectThingDef => "PLTILFILT_CoastalRotation".Translate();

        public override void Filter(List<int> inputList)
        {
            base.Filter(inputList);

            if (!IsFilterActive)
                return;
            
            foreach (var tileId in inputList)
            {
                if(Find.World.CoastDirectionAt(tileId).AsInt == UserData.CoastalRotation.Selected)
                    _filteredTiles.Add(tileId);
            }
        }
    }
}