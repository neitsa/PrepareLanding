using System;
using System.Collections.Generic;

namespace PrepareLanding.Filters
{
    public abstract class TileFilter : ITileFilter
    {
        protected List<int> _filteredTiles = new List<int>();

        protected PrepareLandingUserData UserData;

        public abstract string SubjectThingDef { get; }

        public abstract bool IsFilterActive { get; }

        public virtual List<int> FilteredTiles => _filteredTiles;

        public virtual string RunningDescription => $"Filtering {SubjectThingDef}";

        public string AttachedProperty { get; }

        public Action<List<int>> FilterAction { get; }

        public FilterHeaviness Heaviness { get; }

        protected TileFilter(PrepareLandingUserData userData, string attachedProperty, FilterHeaviness heaviness)
        {
            UserData = userData;
            AttachedProperty = attachedProperty;
            Heaviness = heaviness;
            FilterAction = Filter;
        }

        public virtual void Filter(List<int> inputList)
        {
            _filteredTiles.Clear();
        }
    }
}