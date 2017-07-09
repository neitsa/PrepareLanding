using System;
using System.Collections.Generic;
using System.Linq;
using PrepareLanding.Extensions;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace PrepareLanding.Filters
{
    public abstract class TileFilter : ITileFilter
    {
        protected List<int> _filteredTiles = new List<int>();

        public abstract string SubjectThingDef { get; }

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


}