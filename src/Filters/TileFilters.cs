using System.Collections.Generic;
using System.Linq;
using PrepareLanding.Extensions;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace PrepareLanding.Filters
{
    public class TileFilterBiomes : TileFilter
    {
        public TileFilterBiomes(PrepareLandingUserData userData, string attachedProperty,
            FilterHeaviness heaviness) : base(userData, attachedProperty, heaviness)
        {
        }

        public override string SubjectThingDef => "Biomes";

        public override bool IsFilterActive => UserData.ChosenBiome != null;

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
    }

    public class TileFilterHilliness : TileFilter
    {
        public TileFilterHilliness(PrepareLandingUserData userData, string attachedProperty, FilterHeaviness heaviness) : base(userData, attachedProperty, heaviness)
        {
        }

        public override string SubjectThingDef => "Terrains";

        public override bool IsFilterActive => UserData.ChosenHilliness != Hilliness.Undefined;

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
        public TileFilterRoads(PrepareLandingUserData userData, string attachedProperty, FilterHeaviness heaviness) : base(userData, attachedProperty, heaviness)
        {
        }

        public override string SubjectThingDef => "Roads";

        public override bool IsFilterActive
        {
            get
            {
                var roadDefs = UserData.SelectedRoadDefs;
                return roadDefs.Any(entry => entry.Value.State != MultiCheckboxState.Partial);
            }
        }

        public override void Filter(List<int> inputList)
        {
            base.Filter(inputList);

            if (!IsFilterActive)
                return;

            // get a  list of roadDefs that *must not* be present
            var roadDefs = UserData.SelectedRoadDefs;
            var unwantedRoadDefs = (from entry in roadDefs where entry.Value.State == MultiCheckboxState.Off select entry.Key).ToList();

            // from the input list, get only tiles that have roads (of any type)
            var tilesWithRoads = TilesWithRoads(inputList);

            foreach (var tileId in tilesWithRoads)
            {
                // for all road type in the current tile
                var visibleRoads = Find.World.grid[tileId].VisibleRoads;
                foreach (var roadLink in visibleRoads)
                {
                    // do not add the current tile if the current tile road is in the unwanted list
                    if (unwantedRoadDefs.Contains(roadLink.road))
                        break;

                    _filteredTiles.Add(tileId);
                }
            }
        }

        /// <summary>
        /// Given a list of tiles, returns only tiles that have at least a road (of any type).
        /// </summary>
        /// <param name="inputList">A list of tiles from which to only get tiles with roads.</param>
        /// <returns>A <see cref="IEnumerable{T}"/> containing the tiles ids that have at least one type of road.</returns>
        public static IEnumerable<int> TilesWithRoads(List<int> inputList)
        {
            return inputList.Intersect(PrepareLanding.Instance.TileFilter.AllTilesWithRoad);
        }
    }

    public class TileFilterStones : TileFilter
    {
        public TileFilterStones(PrepareLandingUserData userData, string attachedProperty, FilterHeaviness heaviness) : base(userData, attachedProperty, heaviness)
        {
        }

        public override string SubjectThingDef => "Stones";

        public override bool IsFilterActive
        {
            get
            {
                var stoneDefs = UserData.SelectedStoneDefs;
                return stoneDefs.Any(entry => entry.Value.State != MultiCheckboxState.Partial);
            }
        }

        public override void Filter(List<int> inputList)
        {
            base.Filter(inputList);

            if (!IsFilterActive)
                return;

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
                return;

            // the game use 2 to 3 types of stone per tile, so we must have at least 2 chosen types of stones 
            if (orderedStoneDefsOnPartial.Count < 2)
                return;

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
        public TileFilterRivers(PrepareLandingUserData userData, string attachedProperty, FilterHeaviness heaviness) : base(userData, attachedProperty, heaviness)
        {
        }

        public override string SubjectThingDef => "Rivers";

        public override bool IsFilterActive
        {
            get
            {
                var stoneDefs = UserData.SelectedRiverDefs;
                return stoneDefs.Any(entry => entry.Value.State != MultiCheckboxState.Partial);
            }
        }

        public override void Filter(List<int> inputList)
        {
            base.Filter(inputList);

            if (!IsFilterActive)
                return;

            foreach (var selectedRiverDef in UserData.SelectedRiverDefs)
            {
                if (selectedRiverDef.Value.State != MultiCheckboxState.On &&
                    selectedRiverDef.Value.State != MultiCheckboxState.Partial)
                    continue;

                foreach (var tileId in inputList)
                {
                    // note : even though there are multiple rivers in a tile, only the one with the biggest degradeThreshold makes it to the playable map
                    var riverLink = Find.World.grid[tileId].VisibleRivers
                        .MaxBy(riverlink => riverlink.river.degradeThreshold);

                    if (riverLink.river == selectedRiverDef.Key)
                        _filteredTiles.Add(tileId);
                }
            }
        }
    }

    public class TileFilterCurrentMovementTimes : TileFilter
    {
        public TileFilterCurrentMovementTimes(PrepareLandingUserData userData, string attachedProperty, FilterHeaviness heaviness) : base(userData, attachedProperty, heaviness)
        {
        }

        public override string SubjectThingDef => "Current Movement Times";

        public override bool IsFilterActive => UserData.CurrentMovementTime.Use;

        public override void Filter(List<int> inputList)
        {
            base.Filter(inputList);

            if (!IsFilterActive)
                return;


            var tileIdsCount = inputList.Count;
            for (var i = 0; i < tileIdsCount; i++)
            {
                var tileId = inputList[i];

                // must be passable
                if (Find.World.Impassable(tileId))
                    continue;

                FilterMovementTime(tileId, -1f, UserData.CurrentMovementTime, _filteredTiles);
            }
        }

        public static void FilterMovementTime(int tileId, float yearPct, UsableMinMaxNumericItem<float> item,
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

    public class TileFilterWinterMovementTimes : TileFilter
    {
        public TileFilterWinterMovementTimes(PrepareLandingUserData userData, string attachedProperty, FilterHeaviness heaviness) : base(userData, attachedProperty, heaviness)
        {
        }

        public override string SubjectThingDef => "Winter Movement Times";

        public override bool IsFilterActive => UserData.WinterMovementTime.Use;

        public override void Filter(List<int> inputList)
        {
            base.Filter(inputList);

            if (!IsFilterActive)
                return;

            var tileIdsCount = inputList.Count;
            for (var i = 0; i < tileIdsCount; i++)
            {
                var tileId = inputList[i];

                // must be passable
                if (!Find.World.Impassable(tileId))
                    continue;

                var y = Find.WorldGrid.LongLatOf(tileId).y;
                var yearPct = Season.Winter.GetMiddleYearPct(y);

                TileFilterCurrentMovementTimes.FilterMovementTime(tileId, yearPct, UserData.WinterMovementTime,
                    _filteredTiles);
            }
        }
    }

    public class TileFilterSummerMovementTimes : TileFilter
    {
        public TileFilterSummerMovementTimes(PrepareLandingUserData userData, string attachedProperty, FilterHeaviness heaviness) : base(userData, attachedProperty, heaviness)
        {
        }

        public override string SubjectThingDef => "Summer Movement Times";

        public override bool IsFilterActive => UserData.SummerMovementTime.Use;

        public override void Filter(List<int> inputList)
        {
            base.Filter(inputList);

            if (!IsFilterActive)
                return;

            var tileIdsCount = inputList.Count;
            for (var i = 0; i < tileIdsCount; i++)
            {
                var tileId = inputList[i];

                // must be passable
                if (!Find.World.Impassable(tileId))
                    continue;

                var y = Find.WorldGrid.LongLatOf(tileId).y;
                var yearPct = Season.Summer.GetMiddleYearPct(y);

                TileFilterCurrentMovementTimes.FilterMovementTime(tileId, yearPct, UserData.SummerMovementTime,
                    _filteredTiles);
            }
        }
    }

    public class TileFilterCoastalTiles : TileFilter
    {
        public TileFilterCoastalTiles(PrepareLandingUserData userData, string attachedProperty, FilterHeaviness heaviness) : base(userData, attachedProperty, heaviness)
        {
        }

        public override string SubjectThingDef => "Coastal Tiles";

        public override bool IsFilterActive => UserData.ChosenCoastalTileState != MultiCheckboxState.Partial;

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

    public class TileFilterElevations : TileFilter
    {
        public TileFilterElevations(PrepareLandingUserData userData, string attachedProperty, FilterHeaviness heaviness) : base(userData, attachedProperty, heaviness)
        {
        }

        public override string SubjectThingDef => "Elevations";

        public override bool IsFilterActive => UserData.Elevation.Use;

        public override void Filter(List<int> inputList)
        {
            base.Filter(inputList);

            if (!IsFilterActive)
                return;

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
        public TileFilterTimeZones(PrepareLandingUserData userData, string attachedProperty, FilterHeaviness heaviness) : base(userData, attachedProperty, heaviness)
        {
        }

        public override string SubjectThingDef => "Time Zones";

        public override bool IsFilterActive => UserData.TimeZone.Use;


        public override void Filter(List<int> inputList)
        {
            base.Filter(inputList);

            if (!IsFilterActive)
                return;

            foreach (var tileId in inputList)
            {
                var timeZone = GenDate.TimeZoneAt(Find.WorldGrid.LongLatOf(tileId).x);
                if (UserData.TimeZone.InRange(timeZone))
                    _filteredTiles.Add(tileId);
            }
        }
    }

    public class TileFilterAverageTemperatures : TileFilter
    {
        public TileFilterAverageTemperatures(PrepareLandingUserData userData, string attachedProperty, FilterHeaviness heaviness) : base(userData, attachedProperty, heaviness)
        {
        }

        public override string SubjectThingDef => "Average Temperatures";

        public override bool IsFilterActive => UserData.AverageTemperature.Use;

        public override void Filter(List<int> inputList)
        {
            base.Filter(inputList);

            if (!IsFilterActive)
                return;

            foreach (var tileId in inputList)
            {
                var tile = Find.World.grid[tileId];

                if (UserData.AverageTemperature.InRange(tile.temperature))
                    _filteredTiles.Add(tileId);
            }
        }
    }

    public class TileFilterWinterTemperatures : TileFilter
    {
        public TileFilterWinterTemperatures(PrepareLandingUserData userData, string attachedProperty, FilterHeaviness heaviness) : base(userData, attachedProperty, heaviness)
        {
        }

        public override string SubjectThingDef => "Winter Temperatures";

        public override bool IsFilterActive => UserData.WinterTemperature.Use;

        public override void Filter(List<int> inputList)
        {
            base.Filter(inputList);

            if (!IsFilterActive)
                return;

            foreach (var tileId in inputList)
            {
                var y = Find.WorldGrid.LongLatOf(tileId).y;

                var celsiusTemp =
                    GenTemperature.AverageTemperatureAtTileForTwelfth(tileId, Season.Winter.GetMiddleTwelfth(y));

                if (UserData.WinterTemperature.InRange(celsiusTemp))
                    _filteredTiles.Add(tileId);
            }
        }
    }

    public class TileFilterSummerTemperatures : TileFilter
    {
        public TileFilterSummerTemperatures(PrepareLandingUserData userData, string attachedProperty, FilterHeaviness heaviness) : base(userData, attachedProperty, heaviness)
        {
        }

        public override string SubjectThingDef => "Summer Temperatures";

        public override bool IsFilterActive => UserData.SummerTemperature.Use;

        public override void Filter(List<int> inputList)
        {
            base.Filter(inputList);

            if (!IsFilterActive)
                return;

            foreach (var tileId in inputList)
            {
                var y = Find.WorldGrid.LongLatOf(tileId).y;

                var celsiusTemp =
                    GenTemperature.AverageTemperatureAtTileForTwelfth(tileId, Season.Summer.GetMiddleTwelfth(y));

                if (UserData.SummerTemperature.InRange(celsiusTemp))
                    _filteredTiles.Add(tileId);
            }
        }
    }

    public class TileFilterGrowingPeriods : TileFilter
    {
        public TileFilterGrowingPeriods(PrepareLandingUserData userData, string attachedProperty, FilterHeaviness heaviness) : base(userData, attachedProperty, heaviness)
        {
        }

        public override string SubjectThingDef => "Growing Periods";

        public override bool IsFilterActive => UserData.GrowingPeriod.Use;

        public override void Filter(List<int> inputList)
        {
            base.Filter(inputList);

            if (!IsFilterActive)
                return;

            // TODO send problems to GUI tab
            if (!UserData.GrowingPeriod.Max.IsEqualOrGreaterGrowingPeriod(UserData.GrowingPeriod.Min))
                Messages.Message("Minimum growing period can't be greater than maximum growing period",
                    MessageSound.RejectInput);

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
        public TileFilterRainFalls(PrepareLandingUserData userData, string attachedProperty, FilterHeaviness heaviness) : base(userData, attachedProperty, heaviness)
        {
        }

        public override string SubjectThingDef => "Rain Falls";

        public override bool IsFilterActive => UserData.RainFall.Use;

        public override void Filter(List<int> inputList)
        {
            base.Filter(inputList);

            if (!IsFilterActive)
                return;

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
        public TileFilterAnimalsCanGrazeNow(PrepareLandingUserData userData, string attachedProperty, FilterHeaviness heaviness) : base(userData, attachedProperty, heaviness)
        {
        }

        public override string SubjectThingDef => "Animals Can Graze Now";

        public override bool IsFilterActive => UserData.ChosenAnimalsCanGrazeNowState != MultiCheckboxState.Partial;

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
    }
}