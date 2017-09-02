using System;
using System.Collections.Generic;
using PrepareLanding.GameData;

namespace PrepareLanding.Filters
{
    public abstract class TileFilter : ITileFilter
    {
        protected List<int> _filteredTiles = new List<int>();

        protected UserData UserData;

        protected TileFilter(UserData userData, string attachedProperty, FilterHeaviness heaviness)
        {
            UserData = userData;
            AttachedProperty = attachedProperty;
            Heaviness = heaviness;
            FilterAction = Filter;
        }

        public abstract string SubjectThingDef { get; }

        public abstract bool IsFilterActive { get; }

        public virtual List<int> FilteredTiles => _filteredTiles;

        public virtual string RunningDescription => $"Filtering {SubjectThingDef}";

        public string AttachedProperty { get; }

        public Action<List<int>> FilterAction { get; }

        public FilterHeaviness Heaviness { get; }

        public virtual void Filter(List<int> inputList)
        {
            _filteredTiles.Clear();
        }
    }
}