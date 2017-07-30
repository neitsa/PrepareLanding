using System;
using System.Collections.Generic;
using PrepareLanding.Extensions;
using PrepareLanding.Gui.Tab;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;
using Verse.Sound;
using Widgets = PrepareLanding.Gui.Widgets;

namespace PrepareLanding
{
    public class TabGuiUtilityTerrain : TabGuiUtility
    {
        private static Vector2 _scrollPosRoadSelection = Vector2.zero;
        private static Vector2 _scrollPosRiverSelection = Vector2.zero;
        private static Vector2 _scrollPosStoneSelection = Vector2.zero;
        private string _bufferStringNumberOfStones;

        private readonly PrepareLandingUserData _userData;

        private ThingDef _selectedStoneDef;

        public TabGuiUtilityTerrain(PrepareLandingUserData userData, float columnSizePercent = 0.25f) :
            base(columnSizePercent)
        {
            _userData = userData;
        }

        /// <summary>A unique identifier for the Tab.</summary>
        public override string Id => Name;

        /// <summary>
        ///     The name of the tab (that is actually displayed at its top).
        /// </summary>
        public override string Name => "Terrain";

        /// <summary>Gets whether the tab can be draw or not.</summary>
        public override bool CanBeDrawn { get; set; } = true;

        /// <summary>
        ///     Draw the actual content of this window.
        /// </summary>
        /// <param name="inRect">The <see cref="Rect" /> inside which to draw.</param>
        public override void Draw(Rect inRect)
        {
            Begin(inRect);
            DrawBiomeTypesSelection();
            DrawHillinessTypeSelection();
            DrawRoadTypesSelection();
            DrawRiverTypesSelection();
            NewColumn();
            DrawMovementTime();
            DrawStoneTypesSelection();
            NewColumn();
            DrawCoastalSelection();
            DrawElevationSelection();
            DrawTimeZoneSelection();
            End();
        }

        protected virtual void DrawBiomeTypesSelection()
        {
            DrawEntryHeader("Biome Types", false, backgroundColor: ColorFromFilterSubjectThingDef("Biomes"));

            var biomeDefs = _userData.BiomeDefs;

            // "Select" button
            if (ListingStandard.ButtonText("Select Biome"))
            {
                var floatMenuOptions = new List<FloatMenuOption>();

                // add a dummy 'Any' fake biome type. This sets the chosen biome to null.
                Action actionClick = delegate { _userData.ChosenBiome = null; };
                // tool-tip when hovering above the 'Any' biome name on the floating menu
                Action mouseOverAction = delegate
                {
                    var mousePos = Event.current.mousePosition;
                    var rect = new Rect(mousePos.x, mousePos.y, DefaultElementHeight, DefaultElementHeight);

                    TooltipHandler.TipRegion(rect, "Any Biome");
                };
                var menuOption = new FloatMenuOption("Any", actionClick, MenuOptionPriority.Default, mouseOverAction);
                floatMenuOptions.Add(menuOption);

                // loop through all known biomes
                foreach (var currentBiomeDef in biomeDefs)
                {
                    // clicking on the floating menu saves the selected biome
                    actionClick = delegate { _userData.ChosenBiome = currentBiomeDef; };
                    // tool-tip when hovering above the biome name on the floating menu
                    mouseOverAction = delegate
                    {
                        var mousePos = Event.current.mousePosition;
                        var rect = new Rect(mousePos.x, mousePos.y, DefaultElementHeight, DefaultElementHeight);

                        TooltipHandler.TipRegion(rect, currentBiomeDef.description);
                    };

                    //create the floating menu
                    menuOption = new FloatMenuOption(currentBiomeDef.LabelCap, actionClick, MenuOptionPriority.Default,
                        mouseOverAction);
                    // add it to the list of floating menu options
                    floatMenuOptions.Add(menuOption);
                }

                // create the floating menu
                var floatMenu = new FloatMenu(floatMenuOptions, "Select Biome Type");

                // add it to the window stack to display it
                Find.WindowStack.Add(floatMenu);
            }

            var currHeightBefore = ListingStandard.CurHeight;

            var rightLabel = _userData.ChosenBiome != null ? _userData.ChosenBiome.LabelCap : "Any";
            ListingStandard.LabelDouble("Biome:", rightLabel);

            var currHeightAfter = ListingStandard.CurHeight;

            // display tool-tip over label
            if (_userData.ChosenBiome != null)
            {
                var currentRect = ListingStandard.GetRect(0f);
                currentRect.height = currHeightAfter - currHeightBefore;
                if (!string.IsNullOrEmpty(_userData.ChosenBiome.description))
                    TooltipHandler.TipRegion(currentRect, _userData.ChosenBiome.description);
            }
        }

        protected virtual void DrawHillinessTypeSelection()
        {
            DrawEntryHeader($"{"Terrain".Translate()} Types", backgroundColor: ColorFromFilterSubjectThingDef("Terrains"));

            if (ListingStandard.ButtonText("Select Terrain"))
            {
                var floatMenuOptions = new List<FloatMenuOption>();
                foreach (var hillinessValue in _userData.HillinessCollection)
                {
                    var label = "Any";

                    if (hillinessValue != Hilliness.Undefined)
                        label = hillinessValue.GetLabelCap();

                    var menuOption = new FloatMenuOption(label,
                        delegate { _userData.ChosenHilliness = hillinessValue; });
                    floatMenuOptions.Add(menuOption);
                }

                var floatMenu = new FloatMenu(floatMenuOptions, "Select terrain");
                Find.WindowStack.Add(floatMenu);
            }

            // note: RimWorld logs an error when .GetLabelCap() is used on Hilliness.Undefined
            var rightLabel = _userData.ChosenHilliness != Hilliness.Undefined
                ? _userData.ChosenHilliness.GetLabelCap()
                : "Any";
            ListingStandard.LabelDouble($"{"Terrain".Translate()}:", rightLabel);
        }

        protected virtual void DrawRoadTypesSelection()
        {
            DrawEntryHeader("Road Types", backgroundColor: ColorFromFilterSubjectThingDef("Roads"));

            var roadDefs = _userData.RoadDefs;
            var selectedRoadDefs = _userData.SelectedRoadDefs;

            // Reset button: reset all entries to Off state
            if (ListingStandard.ButtonText("Reset All"))
                foreach (var roadDefEntry in selectedRoadDefs)
                    roadDefEntry.Value.State = MultiCheckboxState.Partial;

            var scrollViewHeight = selectedRoadDefs.Count * DefaultElementHeight;
            var inLs = ListingStandard.BeginScrollView(5 * DefaultElementHeight, scrollViewHeight,
                ref _scrollPosRoadSelection);

            // display road elements
            foreach (var roadDef in roadDefs)
            {
                ThreeStateItem threeStateItem;
                if (!selectedRoadDefs.TryGetValue(roadDef, out threeStateItem))
                {
                    Log.Error(
                        $"[PrepareLanding] [DrawRoadTypesSelection] an item in RoadDefs is not in SelectedRoadDefs: {roadDef.LabelCap}");
                    continue;
                }

                // save temporary state as it might change in CheckBoxLabeledMulti
                var tmpState = threeStateItem.State;

                var itemRect = inLs.GetRect(DefaultElementHeight);
                Widgets.CheckBoxLabeledMulti(itemRect, roadDef.LabelCap, ref tmpState);

                // if the state changed, update the item with the new state
                // ReSharper disable once RedundantCheckBeforeAssignment
                if (tmpState != threeStateItem.State)
                    threeStateItem.State = tmpState;

                if (!string.IsNullOrEmpty(roadDef.description))
                    TooltipHandler.TipRegion(itemRect, roadDef.description);
            }

            ListingStandard.EndScrollView(inLs);
        }

        protected virtual void DrawRiverTypesSelection()
        {
            DrawEntryHeader("River Types", backgroundColor: ColorFromFilterSubjectThingDef("Rivers"));

            var riverDefs = _userData.RiverDefs;
            var selectedRiverDefs = _userData.SelectedRiverDefs;

            // Reset button: reset all entries to Off state
            if (ListingStandard.ButtonText("Reset All"))
                foreach (var riverDefEntry in selectedRiverDefs)
                    riverDefEntry.Value.State = MultiCheckboxState.Partial;

            var inLs = ListingStandard.BeginScrollView(4 * DefaultElementHeight,
                selectedRiverDefs.Count * DefaultElementHeight, ref _scrollPosRiverSelection);

            // display river elements
            foreach (var riverDef in riverDefs)
            {
                ThreeStateItem threeStateItem;
                if (!selectedRiverDefs.TryGetValue(riverDef, out threeStateItem))
                {
                    Log.Error(
                        $"[PrepareLanding] [DrawRiverTypesSelection] an item in riverDefs is not in selectedRiverDefs: {riverDef.LabelCap}");
                    continue;
                }

                // save temporary state as it might change in CheckBoxLabeledMulti
                var tmpState = threeStateItem.State;

                var itemRect = inLs.GetRect(DefaultElementHeight);
                Widgets.CheckBoxLabeledMulti(itemRect, riverDef.LabelCap, ref tmpState);

                // if the state changed, update the item with the new state
                // ReSharper disable once RedundantCheckBeforeAssignment
                if (tmpState != threeStateItem.State)
                    threeStateItem.State = tmpState;

                if (!string.IsNullOrEmpty(riverDef.description))
                    TooltipHandler.TipRegion(itemRect, riverDef.description);
            }

            ListingStandard.EndScrollView(inLs);
        }

        protected void DrawMovementTime()
        {
            DrawEntryHeader("Movement Times (hours)", false, backgroundColor: ColorFromFilterSubjectThingDef("Current Movement Times"));

            DrawUsableMinMaxNumericField(_userData.CurrentMovementTime, "Current Movement Time");
            DrawUsableMinMaxNumericField(_userData.SummerMovementTime, "Summer Movement Time");
            DrawUsableMinMaxNumericField(_userData.WinterMovementTime, "Winter Movement Time");
        }

        protected void DrawElevationSelection()
        {
            DrawEntryHeader("Elevation (meters)", backgroundColor: ColorFromFilterSubjectThingDef("Elevations"));

            // note: see RimWorld.Planet.WorldGenStep_Terrain.ElevationRange for min / max elevation (private static var)
            // max is defined in RimWorld.Planet.WorldMaterials.ElevationMax
            DrawUsableMinMaxNumericField(_userData.Elevation, "Elevation", -500f, 5000f);
        }

        protected virtual void DrawStoneTypesSelection()
        {
            DrawEntryHeader("StoneTypesHere".Translate(), backgroundColor: ColorFromFilterSubjectThingDef("Stones"));

            var selectedStoneDefs = _userData.SelectedStoneDefs;
            var orderedStoneDefs = _userData.OrderedStoneDefs;

            // Reset button: reset all entries to Off state
            if (ListingStandard.ButtonText("Reset All"))
            {
                foreach (var stoneDefEntry in selectedStoneDefs)
                    stoneDefEntry.Value.State = stoneDefEntry.Value.DefaultState;

                _userData.StoneTypesNumberOnly = false;
            }

            // re-orderable list group
            var reorderableGroup = ReorderableWidget.NewGroup(delegate(int from, int to)
            {
                //TODO find a way to raise an event to tell an observer that the list order has changed
                ReorderElements(from, to, orderedStoneDefs);
                SoundDefOf.TickHigh.PlayOneShotOnCamera();
            });

            var maxHeight = InRect.height - ListingStandard.CurHeight;
            var height = Mathf.Min(selectedStoneDefs.Count * DefaultElementHeight, maxHeight);

            if (!_userData.StoneTypesNumberOnly)
            {
                // stone types, standard selection

                var inLs = ListingStandard.BeginScrollView(height, selectedStoneDefs.Count * DefaultElementHeight,
                    ref _scrollPosStoneSelection);
                
                foreach (var currentOrderedStoneDef in orderedStoneDefs)
                {
                    ThreeStateItem threeStateItem;

                    if (!selectedStoneDefs.TryGetValue(currentOrderedStoneDef, out threeStateItem))
                    {
                        Log.Message("A stoneDef wasn't found in selectedStoneDefs");
                        continue;
                    }

                    var flag = currentOrderedStoneDef == _selectedStoneDef;

                    // save temporary state as it might change in CheckBoxLabeledMulti
                    var tmpState = threeStateItem.State;

                    var itemRect = inLs.GetRect(DefaultElementHeight);
                    if (Widgets.CheckBoxLabeledSelectableMulti(itemRect, currentOrderedStoneDef.LabelCap,
                        ref flag, ref tmpState))
                        _selectedStoneDef = currentOrderedStoneDef;

                    // if the state changed, update the item with the new state
                    threeStateItem.State = tmpState;

                    ReorderableWidget.Reorderable(reorderableGroup, itemRect);
                    TooltipHandler.TipRegion(itemRect, currentOrderedStoneDef.description);
                }

                ListingStandard.EndScrollView(inLs);
            }
            else
            {
                // just keep the height of what should have been the scroll view but don't draw it. Put a big red cross on it.
                var scrollViewRect = ListingStandard.GetRect(height);
                GUI.DrawTexture(scrollViewRect, Verse.Widgets.CheckboxOffTex);
            }

            // choose stone types depending on their number on tiles.
            ListingStandard.GapLine(DefaultGapLineHeight);

            var stoneTypesNumberRect = ListingStandard.GetRect(DefaultElementHeight);
            var leftRect = stoneTypesNumberRect.LeftPart(0.80f);
            var rightRect = stoneTypesNumberRect.RightPart(0.20f);

            var filterByStoneNumber = _userData.StoneTypesNumberOnly;
            Verse.Widgets.CheckboxLabeled(leftRect, "Use # of stone types [2,3]:", ref filterByStoneNumber);
            _userData.StoneTypesNumberOnly = filterByStoneNumber;

            var numberOfStones = _userData.StoneTypesNumber;
            Verse.Widgets.TextFieldNumeric(rightRect, ref numberOfStones, ref _bufferStringNumberOfStones, 2, 3);
            _userData.StoneTypesNumber = numberOfStones;

            const string tooltipText = "Filter tiles that have only the given number of stone types (whatever the types are). This disables the other stone filters.";
            TooltipHandler.TipRegion(leftRect, tooltipText);
        }

        public static void ReorderElements<T>(int index, int newIndex, IList<T> elementsList)
        {
            if (index == newIndex || index < 0)
            {
                Log.Message($"[PrepareLanding] ReorderElements -> index: {index}; newIndex: {newIndex}");
                return;
            }

            if (elementsList.Count == 0)
            {
                Log.Message("[PrepareLanding] ReorderElements: elementsList count is 0.");
                return;
            }

            if (index >= elementsList.Count || newIndex >= elementsList.Count)
            {
                Log.Message($"[PrepareLanding] ReorderElements -> index: {index}; newIndex: {newIndex}; elemntsList.Count: {elementsList.Count}");
                return;
            }

            var item = elementsList[index];
            elementsList.RemoveAt(index);
            elementsList.Insert(newIndex, item);
        }

        protected virtual void DrawCoastalSelection()
        {
            DrawEntryHeader("Coastal Tile", false, backgroundColor: ColorFromFilterSubjectThingDef("Coastal Tiles"));

            var rect = ListingStandard.GetRect(DefaultElementHeight);
            var tmpCheckState = _userData.ChosenCoastalTileState;
            Widgets.CheckBoxLabeledMulti(rect, "Is Coastal Tile:", ref tmpCheckState);

            _userData.ChosenCoastalTileState = tmpCheckState;
        }

        protected virtual void DrawTimeZoneSelection()
        {
            DrawEntryHeader("Time Zone [-12, +12]", backgroundColor: ColorFromFilterSubjectThingDef("Time Zones"));

            DrawUsableMinMaxNumericField(_userData.TimeZone, "Time Zone", -12, 12);
        }
    }
}