using System;
using System.Collections.Generic;
using System.Linq;
using PrepareLanding.Extensions;
using RimWorld.Planet;
using Verse;

namespace PrepareLanding.Filters
{
    public abstract class TileFilter : ITileFilter
    {
        protected List<int> _filteredTiles = new List<int>();

        public abstract string SubjectThingDef { get;  }

        public virtual List<int> FilteredTiles => _filteredTiles;

        public virtual string RunningDescription => $"Filtering {SubjectThingDef}";

        public string AttachedProperty { get; }
        public Action<PrepareLandingUserData, List<int>> FilterAction { get; }
        public FilterHeaviness Heaviness { get; }

        protected TileFilter(string attachedProperty, FilterHeaviness heaviness)
        {
            AttachedProperty = attachedProperty;
            Heaviness = heaviness;
            FilterAction = Filter;
        }

        public virtual void Filter(PrepareLandingUserData userData, List<int> inputList)
        {
            _filteredTiles.Clear();
        }
    }

    public class TileFilterBiomes : TileFilter
    {
        public override string SubjectThingDef => "Biomes";

        public TileFilterBiomes(string attachedProperty, FilterHeaviness heaviness) : base(attachedProperty, heaviness) { }

        public override void Filter(PrepareLandingUserData userData, List<int> inputList)
        {
            base.Filter(userData, inputList);

            var chosenBiome = userData.ChosenBiome;

            // a null biome means any biomes, so return all tiles
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
        public override string SubjectThingDef => "Terrains";

        public TileFilterHilliness(string attachedProperty, FilterHeaviness heaviness) : base(attachedProperty, heaviness) { }

        public override void Filter(PrepareLandingUserData userData, List<int> inputList)
        {
            base.Filter(userData, inputList);

            var chosenHilliness = userData.ChosenHilliness;

            // an undefined hilliness means 'any' type of hilliness, so all tile match
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
        public override string SubjectThingDef => "Roads";

        public TileFilterRoads(string attachedProperty, FilterHeaviness heaviness) : base(attachedProperty, heaviness) { }

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
        public override string SubjectThingDef => "Stones";

        public TileFilterStones(string attachedProperty, FilterHeaviness heaviness) : base(attachedProperty, heaviness) { }

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
                        if (IsSubsetInOrderSamePos(subset, containingList))
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
}
