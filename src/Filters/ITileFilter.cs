using System;
using System.Collections.Generic;

namespace PrepareLanding.Filters
{
    public enum FilterHeaviness
    {
        Unknown = 0,
        Light = 1,
        Medium = 2,
        Heavy = 3,
        
    }

    public interface ITileFilter
    {
        string SubjectThingDef { get; }

        string RunningDescription { get; }

        string AttachedProperty { get; }

        Action<List<int>> FilterAction { get; }

        FilterHeaviness Heaviness { get; }

        List<int> FilteredTiles { get; }

        bool IsFilterActive { get; }

        void Filter(List<int> inputList);
    }
}