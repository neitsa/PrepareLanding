using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace PrepareLanding.GameData
{
    /// <summary>
    /// Holds various RimWorld definitions (<see cref="Verse.Def" />) that are used throughout the mod.
    /// </summary>
    public class DefData
    {
        /// <summary>
        ///     All biome definitions (<see cref="BiomeDef" />) from RimWorld.
        /// </summary>
        private List<BiomeDef> _biomeDefs;

        /// <summary>
        ///     All river definitions (<see cref="RiverDef" />) from RimWorld.
        /// </summary>
        private List<RiverDef> _riverDefs;

        /// <summary>
        ///     All road definitions (<see cref="RoadDef" />) from RimWorld.
        /// </summary>
        private List<RoadDef> _roadDefs;

        /// <summary>
        ///     All stone (rock types) definitions (<see cref="ThingDef" />) from RimWorld.
        /// </summary>
        private List<ThingDef> _stoneDefs;

        /// <summary>
        ///     List of all RimWorld hillinesses.
        /// </summary>
        private List<Hilliness> _hillinesses;

        private readonly FilterOptions _filterOptions;

        /// <summary>
        ///     Methods can register to this event to be called when definitions (Defs) have been parsed and are available.
        /// </summary>
        public event Action DefParsed = delegate { };

        public DefData(FilterOptions filterOptions)
        {
            _filterOptions = filterOptions;

            // get alerted when RimWorld has loaded its definition (Defs) files
            PrepareLanding.Instance.EventHandler.DefsLoaded += ExecuteOnDefsLoaded;

            // register to the option changed event
            _filterOptions.PropertyChanged += OptionChanged;
        }

        /// <summary>
        ///     All biome definitions (<see cref="BiomeDef" />) from RimWorld.
        /// </summary>
        public ReadOnlyCollection<BiomeDef> BiomeDefs => _biomeDefs.AsReadOnly();

        /// <summary>
        ///     All "stone" definitions from RimWorld.
        /// </summary>
        /// <remarks>
        ///     Note that stone types (e.g Marble, Granite, etc. are <see cref="ThingDef" /> and have no particular
        ///     definition).
        /// </remarks>
        public ReadOnlyCollection<ThingDef> StoneDefs => _stoneDefs.AsReadOnly();

        /// <summary>
        ///     All river definitions (<see cref="RiverDef" />) from RimWorld.
        /// </summary>
        public ReadOnlyCollection<RiverDef> RiverDefs => _riverDefs.AsReadOnly();

        /// <summary>
        ///     All road definitions (<see cref="RoadDef" />) from RimWorld.
        /// </summary>
        public ReadOnlyCollection<RoadDef> RoadDefs => _roadDefs.AsReadOnly();

        /// <summary>
        ///     All known hilliness (<see cref="Hilliness" />) from RimWorld.
        /// </summary>
        public ReadOnlyCollection<Hilliness> HillinessCollection => _hillinesses.AsReadOnly();

        /// <summary>
        ///     Called when RimWorld definitions (<see cref="Def" />) have been loaded: build definition lists (biomes, rivers,
        ///     roads, stones, etc.)
        /// </summary>
        protected void ExecuteOnDefsLoaded()
        {
            // biome definitions list
            _biomeDefs = BuildBiomeDefs();

            // road definitions list
            _roadDefs = BuildRoadDefs();

            // river definitions list
            _riverDefs = BuildRiverDefs();

            // stone definitions list
            _stoneDefs = BuildStoneDefs();

            // build hilliness values
            _hillinesses = BuildHillinessValues();

            // alert subscribers.
            DefParsed?.Invoke();
        }

        /// <summary>
        ///     Build the biome definitions (<see cref="BiomeDef" />) list.
        /// </summary>
        /// <param name="allowUnimplemented">Tells whether or not unimplemented biomes are allowed.</param>
        /// <param name="allowCantBuildBase">Tells whether or not biomes that do not allow bases to be built are allowed.</param>
        /// <returns>A list of all available RimWorld biome definitions (<see cref="BiomeDef" />).</returns>
        private List<BiomeDef> BuildBiomeDefs(bool allowUnimplemented = false, bool allowCantBuildBase = false)
        {
            var biomeDefsList = new List<BiomeDef>();
            foreach (var biomeDef in DefDatabase<BiomeDef>.AllDefsListForReading)
            {
                BiomeDef currentBiomeDef = null;

                if (biomeDef.implemented)
                    currentBiomeDef = biomeDef;
                else if (!biomeDef.implemented && allowUnimplemented)
                    currentBiomeDef = biomeDef;

                if (biomeDef.canBuildBase)
                {
                    if (!biomeDefsList.Contains(biomeDef))
                        currentBiomeDef = biomeDef;
                }
                else if (!biomeDef.canBuildBase && allowCantBuildBase)
                {
                    if (!biomeDefsList.Contains(biomeDef))
                        currentBiomeDef = biomeDef;
                }
                else if (!biomeDef.canBuildBase && !allowCantBuildBase)
                {
                    if (biomeDefsList.Contains(biomeDef))
                        biomeDefsList.Remove(biomeDef);

                    currentBiomeDef = null;
                }

                if (currentBiomeDef != null)
                    biomeDefsList.Add(currentBiomeDef);
            }

            return biomeDefsList.OrderBy(biome => biome.LabelCap).ToList();
        }

        /// <summary>
        ///     Build the hilliness definitions (<see cref="Hilliness" />) list.
        /// </summary>
        /// <returns>A list of all available RimWorld hillinesses (<see cref="Hilliness" />).</returns>
        private List<Hilliness> BuildHillinessValues()
        {
            // get all possible enumeration values for hilliness
            var hillinesses = Enum.GetValues(typeof(Hilliness)).Cast<Hilliness>().ToList();

            // check if impassable tiles are allowed
            if (_filterOptions.AllowImpassableHilliness)
                return hillinesses;

            // remove impassable hilliness if not asked specifically for it.
            if (!hillinesses.Remove(Hilliness.Impassable))
                Log.Message("[PrepareLanding] Couldn't remove Impassable hilliness.");

            return hillinesses;
        }

        /// <summary>
        ///     Build the river definitions (<see cref="RiverDef" />) list.
        /// </summary>
        /// <returns>A list of all available RimWorld river definitions (<see cref="RiverDef" />).</returns>
        private List<RiverDef> BuildRiverDefs()
        {
            var rivers = DefDatabase<RiverDef>.AllDefsListForReading;
            return rivers;
        }

        /// <summary>
        ///     Build the road definitions (<see cref="RoadDef" />) list.
        /// </summary>
        /// <returns>A list of all available RimWorld road definitions (<see cref="RoadDef" />).</returns>
        private List<RoadDef> BuildRoadDefs()
        {
            var roads = DefDatabase<RoadDef>.AllDefsListForReading;
            return roads;
        }

        /// <summary>
        ///     Build the stone definitions (<see cref="ThingDef" />) list.
        /// </summary>
        /// <returns>A list of all available RimWorld stone definitions (<see cref="ThingDef" />).</returns>
        private List<ThingDef> BuildStoneDefs()
        {
            return DefDatabase<ThingDef>.AllDefs.Where(WorldTileFilter.IsThingDefStone).ToList();
        }

        /// <summary>
        ///     Called when an option changed.
        /// </summary>
        protected void OptionChanged(object sender, PropertyChangedEventArgs eventArgs)
        {
            // rebuild possible hilliness values if the option changed
            if (eventArgs.PropertyName == nameof(_filterOptions.AllowImpassableHilliness))
            {
                _hillinesses = BuildHillinessValues();
            }
        }
    }
}
