using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using PrepareLanding.Core;
using PrepareLanding.Core.Extensions;
using PrepareLanding.GameData;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
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

        public override string SubjectThingDef => "Biomes";

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

        public override string SubjectThingDef => "Terrains";

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

        public override string SubjectThingDef => "Roads";

        public override void Filter(List<int> inputList)
        {
            base.Filter(inputList);

            if (!IsFilterActive)
                return;

            var roadDefs = UserData.SelectedRoadDefs;

            // get a list of roadDefs that *must not* be present
            var unwantedRoadDefs = (from entry in roadDefs
                where entry.Value.State == MultiCheckboxState.Off
                select entry.Key).ToList();

            // get a list of roadDefs that *must* be present
            var wantedRoadDefs = (from entry in roadDefs
                where entry.Value.State == MultiCheckboxState.On
                select entry.Key).ToList();

            foreach (var tileId in inputList)
            {
                var tile = Find.World.grid[tileId];
                var tileHasRoad = TileHasRoad(tile);
                if (tileHasRoad)
                {
                    var tileRoadDefs = tile.VisibleRoads.Select(roadlink => roadlink.road).ToList();

                    // check that any of the road in the tile is *not* in the unwanted list, if it is, then just continue
                    if (unwantedRoadDefs.Select(r => r).Intersect(tileRoadDefs).Any())
                        continue;

                    // otherwise add the tile (if the road type is MultiCheckboxState.On or MultiCheckboxState.Partial)
                    _filteredTiles.Add(tileId);
                }
                else //tile has no roads
                {
                    //tile has no roads
                    //  if user wants a specific road: do nothing
                    //  if user doesn't absolutely want a specific road type, add the tile 
                    //    (works for MultiCheckboxState.Off and MultiCheckboxState.Partial)
                    if (wantedRoadDefs.Count == 0)
                        _filteredTiles.Add(tileId);
                }
            }
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

        public bool TileHasRoad(Tile tile)
        {
            return tile.VisibleRoads != null && tile.VisibleRoads.Count != 0;
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

        public override string SubjectThingDef => "Stones";

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
            var orderedStoneDefsOn = (from stone in UserData.OrderedStoneDefs
                let threeStateItem = UserData.SelectedStoneDefs[stone]
                where threeStateItem.State == MultiCheckboxState.On
                select stone).ToList();
            var orderedStoneDefsPartial = (from stone in UserData.OrderedStoneDefs
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
                    $"Cannot select more than 3 stone types (selected: {orderedStoneDefsOnCount}).");
                return;
            }

            // the game use 2 to 3 types of stone per tile, so we must have at least 2 chosen types of stones 
            if (orderedStoneDefsOnPartial.Count < 2)
            {
                PrepareLanding.Instance.TileFilter.FilterInfoLogger.AppendErrorMessage(
                    $"At least 2 types of stone types must be in ON or PARTIAL state (selected: {orderedStoneDefsOnPartial.Count}).");
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

                        // check if the subset list has the same stone types at the same position in the containing list.
                        if (subset.IsSubsetInOrderSamePos(containingList))
                            _filteredTiles.Add(tileId);
                    }
                    // maximum must-have stone types
                    else if (orderedStoneDefsOnCount == 3)
                    {
                        // just check that both lists are equals (same content *and* in the same order!)
                        if (tileStones.SequenceEqual(orderedStoneDefsOn))
                            _filteredTiles.Add(tileId);
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

        public override string SubjectThingDef => "Rivers";

        public override void Filter(List<int> inputList)
        {
            base.Filter(inputList);

            if (!IsFilterActive)
                return;

            var riverDefs = UserData.SelectedRiverDefs;

            // get a list of riverDefs that *must not* be present
            var unwantedRiverDefs = (from entry in riverDefs
                where entry.Value.State == MultiCheckboxState.Off
                select entry.Key).ToList();

            // get a list of riverDefs that *must* be present
            var wantedRiverDefs = (from entry in riverDefs
                where entry.Value.State == MultiCheckboxState.On
                select entry.Key).ToList();

            foreach (var tileId in inputList)
            {
                var tileHasRiver = TileHasRiver(tileId);
                if (tileHasRiver)
                {
                    // note: even though there are multiple rivers in a tile, only the one with the biggest degradeThreshold makes it to the playable map
                    var riverLink = Find.World.grid[tileId].VisibleRivers
                        .MaxBy(riverlink => riverlink.river.degradeThreshold);

                    // check that the river is not in the unwanted list, if it is, then just continue
                    if (unwantedRiverDefs.Contains(riverLink.river))
                        continue;

                    // add the tile if the river type is MultiCheckboxState.On or MultiCheckboxState.Partial
                    _filteredTiles.Add(tileId);
                }
                else //tile has no rivers
                {
                    //tile has no river
                    //  if user wants a river: do nothing
                    //  if user doesn't absolutely want a specific river type, add the tile 
                    //    (works for MultiCheckboxState.Off and MultiCheckboxState.Partial)
                    if (wantedRiverDefs.Count == 0)
                        _filteredTiles.Add(tileId);
                }
            }
        }

        public static IEnumerable<int> TilesWithRiver(List<int> inputList)
        {
            return inputList.Intersect(PrepareLanding.Instance.TileFilter.AllTilesWithRiver);
        }

        public static bool TileHasRiver(int tileId)
        {
            var tile = Find.World.grid[tileId];
            return tile.VisibleRivers != null && tile.VisibleRivers.Count != 0;
        }
    }

    public abstract class TileFilterMovementTime : TileFilter
    {
        protected TileFilterMovementTime(UserData userData, string attachedProperty,
            FilterHeaviness heaviness) : base(userData, attachedProperty, heaviness)
        {
        }

        protected abstract float YearPct(int tileId);

        public override void Filter(List<int> inputList)
        {
            base.Filter(inputList);

            if (!IsFilterActive)
                return;

            // e.g userData.CurrentMovementTime, UserData.SummerMovementTime or UserData.WinterMovementTime
            var movementTime = (UsableMinMaxNumericItem<float>) UserData.GetType().GetProperty(AttachedProperty)
                ?.GetValue(UserData, null);
            if (movementTime == null)
            {
                PrepareLanding.Instance.TileFilter.FilterInfoLogger.AppendErrorMessage(
                    "MovementTime is null in TileFilterMovementTime.Filter.", sendToLog: true);
                return;
            }

            if (!movementTime.IsCorrectRange)
            {
                var message =
                    $"{SubjectThingDef}: please verify that Min value is less or equal than Max value (actual comparison: {movementTime.Min} <= {movementTime.Max}).";
                PrepareLanding.Instance.TileFilter.FilterInfoLogger.AppendErrorMessage(message);
                return;
            }

            var tileIdsCount = inputList.Count;
            for (var i = 0; i < tileIdsCount; i++)
            {
                var tileId = inputList[i];

                // must be passable
                if (Find.World.Impassable(tileId))
                    continue;

                var yearPct = YearPct(tileId);

                FilterMovementTime(tileId, yearPct, movementTime, _filteredTiles);
            }
        }

        protected static void FilterMovementTime(int tileId, float yearPct, UsableMinMaxNumericItem<float> item,
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
    }

    public class TileFilterCurrentMovementTimes : TileFilterMovementTime
    {
        public TileFilterCurrentMovementTimes(UserData userData, string attachedProperty,
            FilterHeaviness heaviness) : base(userData, attachedProperty, heaviness)
        {
        }

        public override bool IsFilterActive => UserData.CurrentMovementTime.Use;

        public override string SubjectThingDef => "Current Movement Times";

        protected override float YearPct(int tileId)
        {
            return -1;
        }
    }

    public class TileFilterWinterMovementTimes : TileFilterMovementTime
    {
        public TileFilterWinterMovementTimes(UserData userData, string attachedProperty,
            FilterHeaviness heaviness) : base(userData, attachedProperty, heaviness)
        {
        }

        public override bool IsFilterActive => UserData.WinterMovementTime.Use;

        public override string SubjectThingDef => "Winter Movement Times";

        protected override float YearPct(int tileId)
        {
            var y = Find.WorldGrid.LongLatOf(tileId).y;
            var yearPct = Season.Winter.GetMiddleYearPct(y);
            return yearPct;
        }
    }

    public class TileFilterSummerMovementTimes : TileFilterMovementTime
    {
        public TileFilterSummerMovementTimes(UserData userData, string attachedProperty,
            FilterHeaviness heaviness) : base(userData, attachedProperty, heaviness)
        {
        }

        public override bool IsFilterActive => UserData.SummerMovementTime.Use;

        public override string SubjectThingDef => "Summer Movement Times";

        protected override float YearPct(int tileId)
        {
            var y = Find.WorldGrid.LongLatOf(tileId).y;
            var yearPct = Season.Summer.GetMiddleYearPct(y);
            return yearPct;
        }
    }

    public class TileFilterCoastalTiles : TileFilter
    {
        public TileFilterCoastalTiles(UserData userData, string attachedProperty,
            FilterHeaviness heaviness) : base(userData, attachedProperty, heaviness)
        {
        }

        public override bool IsFilterActive => UserData.ChosenCoastalTileState != MultiCheckboxState.Partial;

        public override string SubjectThingDef => "Coastal Tiles";

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

        public override string SubjectThingDef => "Coastal Lake Tiles";

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

        public override string SubjectThingDef => "Elevations";

        public override void Filter(List<int> inputList)
        {
            base.Filter(inputList);

            if (!IsFilterActive)
                return;

            if (!UserData.Elevation.IsCorrectRange)
            {
                var message =
                    $"{SubjectThingDef}: please verify that Min value is less or equal than Max value (actual comparison: {UserData.Elevation.Min} <= {UserData.Elevation.Max}).";
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

        public override string SubjectThingDef => "Time Zones";


        public override void Filter(List<int> inputList)
        {
            base.Filter(inputList);

            if (!IsFilterActive)
                return;

            if (!UserData.TimeZone.IsCorrectRange)
            {
                var message =
                    $"{SubjectThingDef}: please verify that Min value is less or equal than Max value (actual comparison: {UserData.TimeZone.Min} <= {UserData.TimeZone.Max}).";
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

    public abstract class TileFilterTemperatures : TileFilter
    {
        protected TileFilterTemperatures(UserData userData, string attachedProperty,
            FilterHeaviness heaviness) : base(userData, attachedProperty, heaviness)
        {
        }

        protected abstract float TemperatureForTile(int tileId);

        public override void Filter(List<int> inputList)
        {
            base.Filter(inputList);

            if (!IsFilterActive)
                return;

            // e.g UserData.AverageTemperature, UserData.SummerTemperature or UserData.WinterTemperature
            var temperatureItem = (UsableMinMaxNumericItem<float>) UserData.GetType().GetProperty(AttachedProperty)
                ?.GetValue(UserData, null);
            if (temperatureItem == null)
            {
                PrepareLanding.Instance.TileFilter.FilterInfoLogger.AppendErrorMessage(
                    "TemperatureItem is null in TileFilterTemperatures.Filter.", sendToLog: true);
                return;
            }

            if (!temperatureItem.IsCorrectRange)
            {
                var message =
                    $"{SubjectThingDef}: please verify that Min value is less or equal than Max value (actual comparison: {temperatureItem.Min} <= {temperatureItem.Max}).";
                PrepareLanding.Instance.TileFilter.FilterInfoLogger.AppendErrorMessage(message);
                return;
            }

            foreach (var tileId in inputList)
            {
                var tileTemp = TemperatureForTile(tileId);

                if (temperatureItem.InRange(tileTemp))
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

        public override string SubjectThingDef => "Average Temperatures";

        protected override float TemperatureForTile(int tileId)
        {
            var tile = Find.World.grid[tileId];
            return tile.temperature;
        }
    }

    public class TileFilterWinterTemperatures : TileFilterTemperatures
    {
        public TileFilterWinterTemperatures(UserData userData, string attachedProperty,
            FilterHeaviness heaviness) : base(userData, attachedProperty, heaviness)
        {
        }

        public override bool IsFilterActive => UserData.WinterTemperature.Use;

        public override string SubjectThingDef => "Winter Temperatures";

        protected override float TemperatureForTile(int tileId)
        {
            var y = Find.WorldGrid.LongLatOf(tileId).y;

            var celsiusTemp =
                GenTemperature.AverageTemperatureAtTileForTwelfth(tileId, Season.Winter.GetMiddleTwelfth(y));

            return celsiusTemp;
        }
    }


    public class TileFilterSummerTemperatures : TileFilterTemperatures
    {
        public TileFilterSummerTemperatures(UserData userData, string attachedProperty,
            FilterHeaviness heaviness) : base(userData, attachedProperty, heaviness)
        {
        }

        public override bool IsFilterActive => UserData.SummerTemperature.Use;

        public override string SubjectThingDef => "Summer Temperatures";

        protected override float TemperatureForTile(int tileId)
        {
            var y = Find.WorldGrid.LongLatOf(tileId).y;

            var celsiusTemp =
                GenTemperature.AverageTemperatureAtTileForTwelfth(tileId, Season.Summer.GetMiddleTwelfth(y));

            return celsiusTemp;
        }
    }

    public class TileFilterGrowingPeriods : TileFilter
    {
        public TileFilterGrowingPeriods(UserData userData, string attachedProperty,
            FilterHeaviness heaviness) : base(userData, attachedProperty, heaviness)
        {
        }

        public override bool IsFilterActive => UserData.GrowingPeriod.Use;

        public override string SubjectThingDef => "Growing Periods";

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
                    $"{SubjectThingDef}: please verify that Min value is less or equal than Max value (actual comparison: {minGrowingDays} days <= {maxGrowingDays} days).";
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

        public override string SubjectThingDef => "Rain Falls";

        public override void Filter(List<int> inputList)
        {
            base.Filter(inputList);

            if (!IsFilterActive)
                return;

            if (!UserData.RainFall.IsCorrectRange)
            {
                var message =
                    $"{SubjectThingDef}: please verify that Min value is less or equal than Max value (actual comparison: {UserData.RainFall.Min} <= {UserData.RainFall.Max}).";
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

        public override string SubjectThingDef => "Animals Can Graze Now";

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

        public override string SubjectThingDef => "Has Cave";

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

        public override string SubjectThingDef => $"MostLeastCharacteristic: {UserData.MostLeastItem.Characteristic}";

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
                    $"You are requesting more tiles than the number of available tiles (max: {worldTilesCharacteristics.Count})",
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

        public override string SubjectThingDef => "World Feature";

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
        private static readonly List<Rot4> PossibleRotations = new List<Rot4> { /*Rot4.Invalid,*/ Rot4.North, Rot4.East, Rot4.South, Rot4.West};

        public TileFilterCoastRotation(UserData userData, string attachedProperty,
            FilterHeaviness heaviness) : base(userData, attachedProperty, heaviness)
        {
        }

        public override bool IsFilterActive => UserData.UseCoastRotation;

        public override string SubjectThingDef => "Coast Rotation";

        public override void Filter(List<int> inputList)
        {
            base.Filter(inputList);

            if (!IsFilterActive)
                return;
            
            foreach (var tileId in inputList)
            {
                if(Find.World.CoastDirectionAt(tileId) == UserData.CoastRotation)
                    _filteredTiles.Add(tileId);
            }
        }

        public static ReadOnlyCollection<Rot4> Rotations => PossibleRotations.AsReadOnly();

    }
}