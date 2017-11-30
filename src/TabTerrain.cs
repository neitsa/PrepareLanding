using System;
using System.Collections.Generic;
using PrepareLanding.Core.Extensions;
using PrepareLanding.Core.Gui.Tab;
using PrepareLanding.Filters;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;
using Verse.Sound;
using Widgets = PrepareLanding.Core.Gui.Widgets;

namespace PrepareLanding
{
    public class TabTerrain : TabGuiUtility
    {
        // scroll bar position for road selection
        private static Vector2 _scrollPosRoadSelection = Vector2.zero;
        // scroll bar position for river selection
        private static Vector2 _scrollPosRiverSelection = Vector2.zero;
        // scroll bar position for stone selection
        private static Vector2 _scrollPosStoneSelection = Vector2.zero;

        // game data
        private readonly GameData.GameData _gameData;
        // string buffer for "number of stones" selection 
        private string _bufferStringNumberOfStones;

        // holds the currently selected stone def when reordering them.
        private ThingDef _selectedStoneDef;

        /// <summary>
        ///     Filters Terrain tab constructor.
        /// </summary>
        /// <param name="gameData">Instance of game data.</param>
        /// <param name="columnSizePercent">Size of a column (in percent of the tab).</param>
        public TabTerrain(GameData.GameData gameData, float columnSizePercent = 0.25f) :
            base(columnSizePercent)
        {
            _gameData = gameData;
        }

        /// <summary>Gets whether the tab can be draw or not.</summary>
        public override bool CanBeDrawn { get; set; } = true;

        /// <summary>A unique identifier for the Tab.</summary>
        public override string Id => Name;

        /// <summary>
        ///     The name of the tab (that is actually displayed at its top).
        /// </summary>
        public override string Name => "Terrain";

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
            DrawElevationSelection();
            DrawTimeZoneSelection();
            NewColumn();
            DrawCoastalSelection();
            DrawStoneTypesSelection();
            End();
        }

        /// <summary>
        /// Re-order elements in a list.
        /// </summary>
        /// <typeparam name="T">type of elements in the list.</typeparam>
        /// <param name="index">The old index of the element to move.</param>
        /// <param name="newIndex">The new index of the element to move.</param>
        /// <param name="elementsList">The list of elements.</param>
        public static void ReorderElements<T>(int index, int newIndex, IList<T> elementsList)
        {
            if ((index == newIndex) || (index < 0))
            {
                Log.Message($"[PrepareLanding] ReorderElements -> index: {index}; newIndex: {newIndex}");
                return;
            }

            if (elementsList.Count == 0)
            {
                Log.Message("[PrepareLanding] ReorderElements: elementsList count is 0.");
                return;
            }

            if ((index >= elementsList.Count) || (newIndex >= elementsList.Count))
            {
                Log.Message(
                    $"[PrepareLanding] ReorderElements -> index: {index}; newIndex: {newIndex}; elemntsList.Count: {elementsList.Count}");
                return;
            }

            var item = elementsList[index];
            elementsList.RemoveAt(index);
            elementsList.Insert(newIndex, item);
        }

        protected virtual void DrawBiomeTypesSelection()
        {
            DrawEntryHeader("Biome Types", false, backgroundColor: ColorFromFilterSubjectThingDef("Biomes"));

            var biomeDefs = _gameData.DefData.BiomeDefs;

            // "Select" button
            if (ListingStandard.ButtonText("Select Biome"))
            {
                var floatMenuOptions = new List<FloatMenuOption>();

                // add a dummy 'Any' fake biome type. This sets the chosen biome to null.
                Action actionClick = delegate { _gameData.UserData.ChosenBiome = null; };
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
                    actionClick = delegate { _gameData.UserData.ChosenBiome = currentBiomeDef; };
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

            var rightLabel = _gameData.UserData.ChosenBiome != null ? _gameData.UserData.ChosenBiome.LabelCap : "Any";
            ListingStandard.LabelDouble("Biome:", rightLabel);

            var currHeightAfter = ListingStandard.CurHeight;

            // display tool-tip over label
            if (_gameData.UserData.ChosenBiome != null)
            {
                var currentRect = ListingStandard.GetRect(0f);
                currentRect.height = currHeightAfter - currHeightBefore;
                if (!string.IsNullOrEmpty(_gameData.UserData.ChosenBiome.description))
                    TooltipHandler.TipRegion(currentRect, _gameData.UserData.ChosenBiome.description);
            }
        }

        protected virtual void DrawCoastalSelection()
        {
            DrawEntryHeader("Coastal Tiles", false, backgroundColor: ColorFromFilterSubjectThingDef("Coastal Tiles"));

            // coastal tiles (sea)
            var rect = ListingStandard.GetRect(DefaultElementHeight);
            var tmpCheckState = _gameData.UserData.ChosenCoastalTileState;
            Widgets.CheckBoxLabeledMulti(rect, "Is Coastal Tile (sea):", ref tmpCheckState);

            _gameData.UserData.ChosenCoastalTileState = tmpCheckState;

            ListingStandard.Gap(6f);

            // coastal tiles (lake)
            rect = ListingStandard.GetRect(DefaultElementHeight);
            tmpCheckState = _gameData.UserData.ChosenCoastalLakeTileState;
            Widgets.CheckBoxLabeledMulti(rect, "Is Coastal Tile (lake):", ref tmpCheckState);

            _gameData.UserData.ChosenCoastalLakeTileState = tmpCheckState;

            /*
             * Coastal rotation
             */
            var filterCoastalRotation = _gameData.UserData.CoastalRotation.Use;
            ListingStandard.CheckboxLabeled("Use Coastal Rotation", ref filterCoastalRotation);
            _gameData.UserData.CoastalRotation.Use = filterCoastalRotation;

            // "Select" button
            if (ListingStandard.ButtonText("Select Rotation"))
            {
                var floatMenuOptions = new List<FloatMenuOption>();

                // loop through all meaningful rotations
                foreach (var currentRotation in TileFilterCoastRotation.PossibleRotations)
                {
                    // clicking on the floating menu saves the selected rotation
                    Action actionClick = delegate { _gameData.UserData.CoastalRotation.Selected = currentRotation.AsInt; };
                    // tool-tip when hovering above the rotation name on the floating menu
                    Action mouseOverAction = delegate
                    {
                        var mousePos = Event.current.mousePosition;
                        rect = new Rect(mousePos.x, mousePos.y, DefaultElementHeight, DefaultElementHeight);

                        TooltipHandler.TipRegion(rect, ("HasCoast" + currentRotation).Translate());
                    };

                    //create the floating menu
                    var menuOption = new FloatMenuOption(currentRotation.ToStringHuman(), actionClick, MenuOptionPriority.Default,
                        mouseOverAction);
                    // add it to the list of floating menu options
                    floatMenuOptions.Add(menuOption);
                }

                // create the floating menu
                var floatMenu = new FloatMenu(floatMenuOptions, "Select Coast Rotation");

                // add it to the window stack to display it
                Find.WindowStack.Add(floatMenu);
            }

            var rightLabel = _gameData.UserData.CoastalRotation.Use /*&& _gameData.UserData.CoastalRotation.Selected != Rot4.Invalid*/
                ? ("HasCoast" + _gameData.UserData.CoastalRotation.Selected).Translate().CapitalizeFirst() 
                : "None";
            ListingStandard.LabelDouble("Coast Rotation:", rightLabel);
        }

        protected void DrawElevationSelection()
        {
            DrawEntryHeader("Elevation (meters)", backgroundColor: ColorFromFilterSubjectThingDef("Elevations"));

            // note: see RimWorld.Planet.WorldGenStep_Terrain.ElevationRange for min / max elevation (private static var)
            // max is defined in RimWorld.Planet.WorldMaterials.ElevationMax
            DrawUsableMinMaxNumericField(_gameData.UserData.Elevation, "Elevation", -500f, 5000f);
        }

        protected virtual void DrawHillinessTypeSelection()
        {
            DrawEntryHeader($"{"Terrain".Translate()} Types",
                backgroundColor: ColorFromFilterSubjectThingDef("Terrains"));

            if (ListingStandard.ButtonText("Select Terrain"))
            {
                var floatMenuOptions = new List<FloatMenuOption>();
                foreach (var hillinessValue in _gameData.DefData.HillinessCollection)
                {
                    var label = "Any";

                    if (hillinessValue != Hilliness.Undefined)
                        label = hillinessValue.GetLabelCap();

                    var menuOption = new FloatMenuOption(label,
                        delegate { _gameData.UserData.ChosenHilliness = hillinessValue; });
                    floatMenuOptions.Add(menuOption);
                }

                var floatMenu = new FloatMenu(floatMenuOptions, "Select terrain");
                Find.WindowStack.Add(floatMenu);
            }

            // note: RimWorld logs an error when .GetLabelCap() is used on Hilliness.Undefined
            var rightLabel = _gameData.UserData.ChosenHilliness != Hilliness.Undefined
                ? _gameData.UserData.ChosenHilliness.GetLabelCap()
                : "Any";
            ListingStandard.LabelDouble($"{"Terrain".Translate()}:", rightLabel);
        }

        protected void DrawMovementTime()
        {
            DrawEntryHeader("Movement Times (hours)", false,
                backgroundColor: ColorFromFilterSubjectThingDef("Current Movement Times"));

            DrawUsableMinMaxNumericField(_gameData.UserData.CurrentMovementTime, "Current Movement Time");
            DrawUsableMinMaxNumericField(_gameData.UserData.SummerMovementTime, "Summer Movement Time");
            DrawUsableMinMaxNumericField(_gameData.UserData.WinterMovementTime, "Winter Movement Time");
        }

        protected virtual void DrawRiverTypesSelection()
        {
            DrawEntryHeader("River Types", backgroundColor: ColorFromFilterSubjectThingDef("Rivers"));

            var riverDefs = _gameData.DefData.RiverDefs;
            var selectedRiverDefs = _gameData.UserData.SelectedRiverDefs;

            /*
             * Buttons
             */
            const int numButtons = 3;
            var buttonsRect = ListingStandard.GetRect(DefaultElementHeight).SplitRectWidthEvenly(numButtons);
            if (buttonsRect.Count != numButtons)
            {
                Log.ErrorOnce($"[PrepareLanding] DrawRiverTypesSelection: couldn't get the right number of buttons: {numButtons}", 0x123acafe);
                return;
            }

            // Reset button: reset all entries to Partial state
            if (Verse.Widgets.ButtonText(buttonsRect[0], "Reset"))
                foreach (var riverDefEntry in selectedRiverDefs)
                    riverDefEntry.Value.State = MultiCheckboxState.Partial;

            // All rivers
            if (Verse.Widgets.ButtonText(buttonsRect[1], "All"))
                foreach (var riverDefEntry in selectedRiverDefs)
                    riverDefEntry.Value.State = MultiCheckboxState.On;

            // No rivers
            if (Verse.Widgets.ButtonText(buttonsRect[2], "None"))
                foreach (var riverDefEntry in selectedRiverDefs)
                    riverDefEntry.Value.State = MultiCheckboxState.Off;

            /*
             * ScrollView
             */

            var inLs = ListingStandard.BeginScrollView(4*DefaultElementHeight,
                selectedRiverDefs.Count*DefaultElementHeight, ref _scrollPosRiverSelection, DefaultScrollableViewShrinkWidth);

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

        protected virtual void DrawRoadTypesSelection()
        {
            DrawEntryHeader("Road Types", backgroundColor: ColorFromFilterSubjectThingDef("Roads"));

            var roadDefs = _gameData.DefData.RoadDefs;
            var selectedRoadDefs = _gameData.UserData.SelectedRoadDefs;
            
            /*
             * Buttons
             */
            const int numButtons = 3;
            var buttonsRect = ListingStandard.GetRect(DefaultElementHeight).SplitRectWidthEvenly(numButtons);
            if (buttonsRect.Count != numButtons)
            {
                Log.ErrorOnce($"[PrepareLanding] DrawRoadTypesSelection: couldn't get the right number of buttons: {numButtons}", 0x1239cafe);
                return;
            }

            // Reset button: reset all entries to Partial state
            if (Verse.Widgets.ButtonText(buttonsRect[0], "Reset"))
                foreach (var roadDefEntry in selectedRoadDefs)
                    roadDefEntry.Value.State = MultiCheckboxState.Partial;

            // all roads
            if (Verse.Widgets.ButtonText(buttonsRect[1], "All"))
                foreach (var roadDefEntry in selectedRoadDefs)
                    roadDefEntry.Value.State = MultiCheckboxState.On;

            // no roads
            if (Verse.Widgets.ButtonText(buttonsRect[2], "None"))
                foreach (var roadDefEntry in selectedRoadDefs)
                    roadDefEntry.Value.State = MultiCheckboxState.Off;

            /*
             * ScrollView
             */

            var scrollViewHeight = selectedRoadDefs.Count*DefaultElementHeight;
            var inLs = ListingStandard.BeginScrollView(5 * DefaultElementHeight, scrollViewHeight,
                ref _scrollPosRoadSelection, DefaultScrollableViewShrinkWidth);

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

        protected virtual void DrawStoneTypesSelection()
        {
            DrawEntryHeader("StoneTypesHere".Translate(), backgroundColor: ColorFromFilterSubjectThingDef("Stones"));

            var selectedStoneDefs = _gameData.UserData.SelectedStoneDefs;
            var orderedStoneDefs = _gameData.UserData.OrderedStoneDefs;

            // Reset button: reset all entries to Off state
            if (ListingStandard.ButtonText("Reset All"))
            {
                foreach (var stoneDefEntry in selectedStoneDefs)
                    stoneDefEntry.Value.State = stoneDefEntry.Value.DefaultState;

                _gameData.UserData.StoneTypesNumberOnly = false;
            }

            // re-orderable list group
            var reorderableGroup = ReorderableWidget.NewGroup(delegate(int from, int to)
            {
                //TODO find a way to raise an event to tell an observer that the list order has changed
                ReorderElements(from, to, orderedStoneDefs);
                SoundDefOf.TickHigh.PlayOneShotOnCamera();
            });

            var maxNumStones = (InRect.height - ListingStandard.CurHeight - DefaultGapLineHeight - DefaultElementHeight - 15f) / DefaultElementHeight;
            var maxHeight = maxNumStones * DefaultElementHeight;
            var height = Mathf.Min(selectedStoneDefs.Count*DefaultElementHeight, maxHeight);

            if (!_gameData.UserData.StoneTypesNumberOnly)
            {
                // stone types, standard selection

                var inLs = ListingStandard.BeginScrollView(height, selectedStoneDefs.Count*DefaultElementHeight,
                    ref _scrollPosStoneSelection, DefaultScrollableViewShrinkWidth);

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

            var filterByStoneNumber = _gameData.UserData.StoneTypesNumberOnly;
            Verse.Widgets.CheckboxLabeled(leftRect, "Use # of stone types [2,3]:", ref filterByStoneNumber);
            _gameData.UserData.StoneTypesNumberOnly = filterByStoneNumber;

            var numberOfStones = _gameData.UserData.StoneTypesNumber;
            Verse.Widgets.TextFieldNumeric(rightRect, ref numberOfStones, ref _bufferStringNumberOfStones, 2, 3);
            _gameData.UserData.StoneTypesNumber = numberOfStones;

            const string tooltipText =
                "Filter tiles that have only the given number of stone types (whatever the types are). This disables the other stone filters.";
            TooltipHandler.TipRegion(leftRect, tooltipText);
        }

        protected virtual void DrawTimeZoneSelection()
        {
            DrawEntryHeader("Time Zone [-12, +12]", backgroundColor: ColorFromFilterSubjectThingDef("Time Zones"));

            DrawUsableMinMaxNumericField(_gameData.UserData.TimeZone, "Time Zone", -12, 12);
        }
    }
}