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
        public TileFilterBiomes(string attachedProperty, FilterHeaviness heaviness) : base(attachedProperty, heaviness)
        {
        }

        public override string SubjectThingDef => "Biomes";

        public override void Filter(PrepareLandingUserData userData, List<int> inputList)
        {
            base.Filter(userData, inputList);

            var chosenBiome = userData.ChosenBiome;

            // a null biome means any biomes, so that means all tiles match
            if (chosenBiome == null)
            {
                _filteredTiles.AddRange(inputList);
                return;
            }

            foreach (var tileId in inputList)
                if (Find.World.grid[tileId].biome == chosenBiome)
                    _filteredTiles.Add(tileId);
        }
    }

    public class TileFilterHilliness : TileFilter
    {
        public TileFilterHilliness(string attachedProperty, FilterHeaviness heaviness) : base(attachedProperty,
            heaviness)
        {
        }

        public override string SubjectThingDef => "Terrains";

        public override void Filter(PrepareLandingUserData userData, List<int> inputList)
        {
            base.Filter(userData, inputList);

            var chosenHilliness = userData.ChosenHilliness;

            // an undefined hilliness means 'any' type of hilliness, so all tiles match
            if (chosenHilliness == Hilliness.Undefined)
            {
                _filteredTiles.AddRange(inputList);
                return;
            }

            foreach (var tileId in inputList)
                if (Find.World.grid[tileId].hilliness == chosenHilliness)
                    _filteredTiles.Add(tileId);
        }
    }

    public class TileFilterRoads : TileFilter
    {
        public TileFilterRoads(string attachedProperty, FilterHeaviness heaviness) : base(attachedProperty, heaviness)
        {
        }

        public override string SubjectThingDef => "Roads";

        public override void Filter(PrepareLandingUserData userData, List<int> inputList)
        {
            base.Filter(userData, inputList);

            var roadDefs = userData.SelectedRoadDefs;
            foreach (var entry in roadDefs)
            {
                var currentRoadDef = entry.Key;

                if (entry.Value.State != MultiCheckboxState.Off)
                    foreach (var tileId in inputList)
                    foreach (var roadLink in Find.World.grid[tileId].VisibleRoads)
                        if (roadLink.road == currentRoadDef)
                            _filteredTiles.Add(tileId);
            }
        }
    }

    public class TileFilterStones : TileFilter
    {
        public TileFilterStones(string attachedProperty, FilterHeaviness heaviness) : base(attachedProperty, heaviness)
        {
        }

        public override string SubjectThingDef => "Stones";

        public override void Filter(PrepareLandingUserData userData, List<int> inputList)
        {
            base.Filter(userData, inputList);

            // collect stones that are in On & Partial states, in their precise order on the GUI!
            var orderedStoneDefsOn = (from stone in userData.OrderedStoneDefs
                let threeStateItem = userData.SelectedStoneDefs[stone]
                where threeStateItem.State == MultiCheckboxState.On
                select stone).ToList();
            var orderedStoneDefsPartial = (from stone in userData.OrderedStoneDefs
                let threeStateItem = userData.SelectedStoneDefs[stone]
                where threeStateItem.State == MultiCheckboxState.Partial
                select stone).ToList();
            var orderedStoneDefsOnPartial = new List<ThingDef>();
            orderedStoneDefsOnPartial.AddRange(orderedStoneDefsOn);
            orderedStoneDefsOnPartial.AddRange(orderedStoneDefsPartial);
            // stone types explicitly marked OFF
            var stoneOffList = (from userDataSelectedStoneDef in userData.SelectedStoneDefs
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
        public TileFilterRivers(string attachedProperty, FilterHeaviness heaviness) : base(attachedProperty, heaviness)
        {
        }

        public override string SubjectThingDef => "Rivers";

        public override void Filter(PrepareLandingUserData userData, List<int> inputList)
        {
            _filteredTiles.Clear();

            foreach (var selectedRiverDef in userData.SelectedRiverDefs)
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
        public TileFilterCurrentMovementTimes(string attachedProperty, FilterHeaviness heaviness) : base(
            attachedProperty, heaviness)
        {
        }

        public override string SubjectThingDef => "Current Movement Times";

        public override void Filter(PrepareLandingUserData userData, List<int> inputList)
        {
            base.Filter(userData, inputList);

            if (!userData.CurrentMovementTime.Use)
                return;


            var tileIdsCount = inputList.Count;
            for (var i = 0; i < tileIdsCount; i++)
            {
                var tileId = inputList[i];

                // must be passable
                if (!Find.World.Impassable(tileId))
                    continue;

                FilterMovementTime(tileId, -1f, userData.CurrentMovementTime, _filteredTiles);
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
        public TileFilterWinterMovementTimes(string attachedProperty, FilterHeaviness heaviness) : base(
            attachedProperty, heaviness)
        {
        }

        public override string SubjectThingDef => "Winter Movement Times";

        public override void Filter(PrepareLandingUserData userData, List<int> inputList)
        {
            base.Filter(userData, inputList);

            if (!userData.WinterMovementTime.Use)
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

                TileFilterCurrentMovementTimes.FilterMovementTime(tileId, yearPct, userData.WinterMovementTime,
                    _filteredTiles);
            }
        }
    }

    public class TileFilterSummerMovementTimes : TileFilter
    {
        public TileFilterSummerMovementTimes(string attachedProperty, FilterHeaviness heaviness) : base(
            attachedProperty, heaviness)
        {
        }

        public override string SubjectThingDef => "Summer Movement Times";

        public override void Filter(PrepareLandingUserData userData, List<int> inputList)
        {
            base.Filter(userData, inputList);

            if (!userData.SummerMovementTime.Use)
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

                TileFilterCurrentMovementTimes.FilterMovementTime(tileId, yearPct, userData.SummerMovementTime,
                    _filteredTiles);
            }
        }
    }

    public class TileFilterCoastalTiles : TileFilter
    {
        public TileFilterCoastalTiles(string attachedProperty, FilterHeaviness heaviness) : base(
            attachedProperty, heaviness)
        {
        }

        public override string SubjectThingDef => "Coastal Tiles";

        public override void Filter(PrepareLandingUserData userData, List<int> inputList)
        {
            base.Filter(userData, inputList);

            switch (userData.ChosenCoastalTileState)
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
        public TileFilterElevations(string attachedProperty, FilterHeaviness heaviness) : base(
            attachedProperty, heaviness)
        {
        }

        public override string SubjectThingDef => "Elevations";

        public override void Filter(PrepareLandingUserData userData, List<int> inputList)
        {
            base.Filter(userData, inputList);

            if (!userData.Elevation.Use)
                return;

            foreach (var tileId in inputList)
            {
                var tile = Find.World.grid[tileId];

                if (userData.Elevation.InRange(tile.elevation))
                    _filteredTiles.Add(tileId);
            }
        }
    }

    public class TileFilterTimeZones : TileFilter
    {
        public TileFilterTimeZones(string attachedProperty, FilterHeaviness heaviness) : base(
            attachedProperty, heaviness)
        {
        }

        public override string SubjectThingDef => "Time Zones";

        public override void Filter(PrepareLandingUserData userData, List<int> inputList)
        {
            base.Filter(userData, inputList);

            if (!userData.TimeZone.Use)
                return;

            foreach (var tileId in inputList)
            {
                var timeZone = GenDate.TimeZoneAt(Find.WorldGrid.LongLatOf(tileId).x);
                if (userData.TimeZone.InRange(timeZone))
                    _filteredTiles.Add(tileId);
            }
        }
    }

    public class TileFilterAverageTemperatures : TileFilter
    {
        public TileFilterAverageTemperatures(string attachedProperty, FilterHeaviness heaviness) : base(
            attachedProperty, heaviness)
        {
        }

        public override string SubjectThingDef => "Average Temperatures";

        public override void Filter(PrepareLandingUserData userData, List<int> inputList)
        {
            base.Filter(userData, inputList);

            if (!userData.AverageTemperature.Use)
                return;

            foreach (var tileId in inputList)
            {
                var tile = Find.World.grid[tileId];

                if (userData.AverageTemperature.InRange(tile.temperature))
                    _filteredTiles.Add(tileId);
            }
        }
    }

    public class TileFilterWinterTemperatures : TileFilter
    {
        public TileFilterWinterTemperatures(string attachedProperty, FilterHeaviness heaviness) : base(
            attachedProperty, heaviness)
        {
        }

        public override string SubjectThingDef => "Winter Temperatures";

        public override void Filter(PrepareLandingUserData userData, List<int> inputList)
        {
            base.Filter(userData, inputList);

            if (!userData.WinterTemperature.Use)
                return;

            foreach (var tileId in inputList)
            {
                var y = Find.WorldGrid.LongLatOf(tileId).y;

                var celsiusTemp =
                    GenTemperature.AverageTemperatureAtTileForTwelfth(tileId, Season.Winter.GetMiddleTwelfth(y));

                if (userData.WinterTemperature.InRange(celsiusTemp))
                    _filteredTiles.Add(tileId);
            }
        }
    }

    public class TileFilterSummerTemperatures : TileFilter
    {
        public TileFilterSummerTemperatures(string attachedProperty, FilterHeaviness heaviness) : base(
            attachedProperty, heaviness)
        {
        }

        public override string SubjectThingDef => "Summer Temperatures";

        public override void Filter(PrepareLandingUserData userData, List<int> inputList)
        {
            base.Filter(userData, inputList);

            if (!userData.SummerTemperature.Use)
                return;

            foreach (var tileId in inputList)
            {
                var y = Find.WorldGrid.LongLatOf(tileId).y;

                var celsiusTemp =
                    GenTemperature.AverageTemperatureAtTileForTwelfth(tileId, Season.Summer.GetMiddleTwelfth(y));

                if (userData.SummerTemperature.InRange(celsiusTemp))
                    _filteredTiles.Add(tileId);
            }
        }
    }

    public class TileFilterGrowingPeriods : TileFilter
    {
        public TileFilterGrowingPeriods(string attachedProperty, FilterHeaviness heaviness) : base(
            attachedProperty, heaviness)
        {
        }

        public override string SubjectThingDef => "Growing Periods";

        public override void Filter(PrepareLandingUserData userData, List<int> inputList)
        {
            base.Filter(userData, inputList);

            if (!userData.GrowingPeriod.Use)
                return;

            // TODO send problems to GUI tab
            if (!userData.GrowingPeriod.Max.IsEqualOrGreaterGrowingPeriod(userData.GrowingPeriod.Min))
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
                var minDays = userData.GrowingPeriod.Min.ToGrowingDays();
                var maxDays = userData.GrowingPeriod.Max.ToGrowingDays();

                if (tileGrowingDays >= minDays && tileGrowingDays <= maxDays)
                    _filteredTiles.Add(tileId);
            }
        }
    }

    public class TileFilterRainFalls : TileFilter
    {
        public TileFilterRainFalls(string attachedProperty, FilterHeaviness heaviness) : base(
            attachedProperty, heaviness)
        {
        }

        public override string SubjectThingDef => "Rain Falls";

        public override void Filter(PrepareLandingUserData userData, List<int> inputList)
        {
            base.Filter(userData, inputList);

            if (!userData.RainFall.Use)
                return;

            foreach (var tileId in inputList)
            {
                var tile = Find.World.grid[tileId];

                if (userData.RainFall.InRange(tile.rainfall))
                    _filteredTiles.Add(tileId);
            }
        }
    }

    public class TileFilterAnimalsCanGrazeNow : TileFilter
    {
        public TileFilterAnimalsCanGrazeNow(string attachedProperty, FilterHeaviness heaviness) : base(
            attachedProperty, heaviness)
        {
        }

        public override string SubjectThingDef => "Animals Can Graze Now";

        public override void Filter(PrepareLandingUserData userData, List<int> inputList)
        {
            base.Filter(userData, inputList);

            // partial state means "I don't care if they can graze now or not", so all tiles match
            if (userData.ChosenAnimalsCanGrazeNowState == MultiCheckboxState.Partial)
            {
                _filteredTiles.AddRange(inputList);
                return;
            }

            foreach (var tileId in inputList)
            {
                var canGrazeNow = VirtualPlantsUtility.EnvironmentAllowsEatingVirtualPlantsNowAt(tileId);
                if (userData.ChosenAnimalsCanGrazeNowState == MultiCheckboxState.On)
                    if (canGrazeNow)
                        _filteredTiles.Add(tileId);

                if (userData.ChosenAnimalsCanGrazeNowState == MultiCheckboxState.Off)
                    if (!canGrazeNow)
                        _filteredTiles.Add(tileId);
            }
        }
    }
}