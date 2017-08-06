using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using JetBrains.Annotations;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace PrepareLanding.GameData
{
    public class GodModeData : INotifyPropertyChanged
    {
        private const float FloatEpsilon = 1e-03f;

        private float _averageTemperature;

        private BiomeDef _biome;

        private float _elevation;

        private Hilliness _hilliness;

        private float _rainfall;

        private int _selectedTileId = -1;

        private readonly DefData _defData;

        public GodModeData(DefData defData)
        {
            _defData = defData;

            // get alerted when RimWorld has loaded its definition (Defs) files
            _defData.DefParsed += InitDefs;
        }

        private void InitDefs()
        {
            // initialize roads
            foreach (var roadDef in _defData.RoadDefs)
                SelectedRoadDefs.Add(roadDef, false);

            // initialize rivers
            foreach (var riverDef in _defData.RiverDefs)
                SelectedRiverDefs.Add(riverDef, false);
        }

        public void InitFromTileId(int tileId)
        {
            if (tileId < 0)
                return;

            _selectedTileId = tileId;

            var tile = Find.World.grid[tileId];

            _biome = tile.biome;
            _elevation = tile.elevation;
            _hilliness = tile.hilliness;
            _averageTemperature = tile.temperature;
            _rainfall = tile.rainfall;

            ResetSelectedRoadDefs();
            if(tile.VisibleRoads != null)
            { 
                foreach (var visibleRoad in tile.VisibleRoads)
                {
                    var roadDef = visibleRoad.road;
                    SelectedRoadDefs[roadDef] = true;
                }
            }

            ResetSelectedRiverDefs();
            if(tile.VisibleRivers != null)
            { 
                foreach (var visibleRiver in tile.VisibleRivers)
                {
                    var riverDef = visibleRiver.river;
                    SelectedRiverDefs[riverDef] = true;
                }
            }
        }

        public void ResetSelectedRoadDefs()
        {
            foreach (var roadDef in SelectedRoadDefs.Keys.ToList())
            {
                SelectedRoadDefs[roadDef] = false;
            }
        }

        public void ResetSelectedRiverDefs()
        {
            foreach (var riverDef in SelectedRiverDefs.Keys.ToList())
            {
                SelectedRiverDefs[riverDef] = false;
            }
        }

        /// <summary>
        ///     The id of the tile to modify.
        /// </summary>
        public int SelectedTileId
        {
            get { return _selectedTileId; }
            set
            {
                if (value == _selectedTileId)
                    return;

                _selectedTileId = value;
                OnPropertyChanged(nameof(SelectedTileId));
            }
        }

        /// <summary>
        ///     The new biome to set in the tile.
        /// </summary>
        public BiomeDef Biome
        {
            get { return _biome; }
            set
            {
                if (value == _biome)
                    return;

                _biome = value;
                OnPropertyChanged(nameof(Biome));
            }
        }

        /// <summary>
        ///     Average temperature (in degrees Celsius) to set in the tile.
        /// </summary>
        public float AverageTemperature
        {
            get { return _averageTemperature; }
            set
            {
                if (Math.Abs(value - _averageTemperature) < FloatEpsilon)
                    return;

                _averageTemperature = value;
                OnPropertyChanged(nameof(AverageTemperature));
            }
        }

        /// <summary>
        ///     New Rainfall (millimeters) to set in the tile.
        /// </summary>
        public float Rainfall
        {
            get { return _rainfall; }
            set
            {
                if (Math.Abs(value - _rainfall) < FloatEpsilon)
                    return;

                _rainfall = value;
                OnPropertyChanged(nameof(Rainfall));
            }
        }

        /// <summary>
        ///     New elevation (meters) to set in the tile.
        /// </summary>
        public float Elevation
        {
            get { return _elevation; }
            set
            {
                if (Math.Abs(value - _elevation) < FloatEpsilon)
                    return;

                _elevation = value;
                OnPropertyChanged(nameof(Elevation));
            }
        }


        /// <summary>
        ///     New hilliness to set in the tile.
        /// </summary>
        public Hilliness Hilliness
        {
            get { return _hilliness; }
            set
            {
                if (value == _hilliness)
                    return;

                _hilliness = value;
                OnPropertyChanged(nameof(Hilliness));
            }
        }

        public Dictionary<RoadDef, bool> SelectedRoadDefs { get; } = new Dictionary<RoadDef, bool>();


        public Dictionary<RiverDef, bool> SelectedRiverDefs { get; } = new Dictionary<RiverDef, bool>();


        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}