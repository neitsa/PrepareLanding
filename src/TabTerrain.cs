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
        public override string Id => "Terrain";

        /// <summary>
        ///     The name of the tab (that is actually displayed at its top).
        /// </summary>
        public override string Name => "PLMWTT_TabName".Translate();

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

        private void DrawBiomeTypesSelection()
        {
            DrawEntryHeader("PLMWTT_BiomeTypes".Translate(), false, backgroundColor: ColorFromFilterSubjectThingDef("Biomes"));

            var biomeDefs = _gameData.DefData.BiomeDefs;

            // "Select" button
            if (ListingStandard.ButtonText("PLMWTT_SelectBiome".Translate()))
            {
                var floatMenuOptions = new List<FloatMenuOption>();

                // add a dummy 'Any' fake biome type. This sets the chosen biome to null.
                Action actionClick = delegate { _gameData.UserData.ChosenBiome = null; };
                // tool-tip when hovering above the 'Any' biome name on the floating menu
                Action mouseOverAction = delegate
                {
                    var mousePos = Event.current.mousePosition;
                    var rect = new Rect(mousePos.x, mousePos.y, DefaultElementHeight, DefaultElementHeight);

                    TooltipHandler.TipRegion(rect, "PLMWTT_AnyBiome".Translate());
                };
                var menuOption = new FloatMenuOption("PLMW_SelectAny".Translate(), actionClick, MenuOptionPriority.Default, mouseOverAction);
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
                var floatMenu = new FloatMenu(floatMenuOptions, "PLMWTT_SelectBiomeType".Translate());

                // add it to the window stack to display it
                Find.WindowStack.Add(floatMenu);
            }

            var currHeightBefore = ListingStandard.CurHeight;

            var rightLabel = _gameData.UserData.ChosenBiome != null ? _gameData.UserData.ChosenBiome.LabelCap : "PLMW_SelectAny".Translate();
            ListingStandard.LabelDouble($"{"Biome".Translate()}:", rightLabel);

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

        private void DrawCoastalSelection()
        {
            DrawEntryHeader("PLMWTT_CoastalTiles".Translate(), false, backgroundColor: ColorFromFilterSubjectThingDef("Coastal Tiles"));

            // coastal tiles (sea)
            var rect = ListingStandard.GetRect(DefaultElementHeight);
            var tmpCheckState = _gameData.UserData.ChosenCoastalTileState;
            Widgets.CheckBoxLabeledMulti(rect, $"{"PLMWTT_IsCoastalTileOcean".Translate()}:", ref tmpCheckState);

            _gameData.UserData.ChosenCoastalTileState = tmpCheckState;

            ListingStandard.Gap(6f);

            /*
             * Coastal rotation
             */
            var filterCoastalRotation = _gameData.UserData.CoastalRotation.Use;
            ListingStandard.CheckboxLabeled("PLMWTT_UseCoastalRoation".Translate(), ref filterCoastalRotation,
                "PLMWTT_UseCoastalRoationTooltip".Translate());
            _gameData.UserData.CoastalRotation.Use = filterCoastalRotation;

            // "Select" button
            if (ListingStandard.ButtonText("PLMWTT_SelectCoastRotation".Translate()))
            {
                var floatMenuOptions = new List<FloatMenuOption>();

                // loop through all meaningful rotations
                foreach (var currentRotation in TileFilterCoastRotation.PossibleRotations)
                {
                    // clicking on the floating menu saves the selected rotation
                    void ActionClick()
                    {
                        _gameData.UserData.CoastalRotation.Selected = currentRotation.AsInt;
                    }

                    // tool-tip when hovering above the rotation name on the floating menu
                    void MouseOverAction()
                    {
                        var mousePos = Event.current.mousePosition;
                        rect = new Rect(mousePos.x, mousePos.y, DefaultElementHeight, DefaultElementHeight);

                        TooltipHandler.TipRegion(rect, ("HasCoast" + currentRotation).Translate());
                    }

                    //create the floating menu
                    var menuOption = new FloatMenuOption(currentRotation.ToStringHuman(), ActionClick, MenuOptionPriority.Default,
                        MouseOverAction);
                    // add it to the list of floating menu options
                    floatMenuOptions.Add(menuOption);
                }

                // create the floating menu
                var floatMenu = new FloatMenu(floatMenuOptions, "PLMWTT_SelectCoastRotation".Translate());

                // add it to the window stack to display it
                Find.WindowStack.Add(floatMenu);
            }

            var rightLabel = _gameData.UserData.CoastalRotation.Use /*&& _gameData.UserData.CoastalRotation.Selected != Rot4.Invalid*/
                ? ("HasCoast" + _gameData.UserData.CoastalRotation.Selected).Translate().CapitalizeFirst() 
                : "PLMW_None".Translate();
            ListingStandard.LabelDouble($"{"PLMWTT_SelectedCoastRotation".Translate()}:", rightLabel);

            /*
             * coastal tiles (lake)
             */

            ListingStandard.Gap(6f);

            
            rect = ListingStandard.GetRect(DefaultElementHeight);
            TooltipHandler.TipRegion(rect, "PLMWTT_IsCoastalTileLakeTooltip".Translate());
            tmpCheckState = _gameData.UserData.ChosenCoastalLakeTileState;
            Widgets.CheckBoxLabeledMulti(rect, $"{"PLMWTT_IsCoastalTileLake".Translate()}:", ref tmpCheckState);

            _gameData.UserData.ChosenCoastalLakeTileState = tmpCheckState;
        }

        private void DrawElevationSelection()
        {
            DrawEntryHeader($"{"Elevation".Translate()} ({Find.ActiveLanguageWorker.Pluralize("PLMW_Meter".Translate())})", backgroundColor: ColorFromFilterSubjectThingDef("Elevations"));

            // note: see RimWorld.Planet.WorldGenStep_Terrain.ElevationRange for min / max elevation (private static var)
            // max is defined in RimWorld.Planet.WorldMaterials.ElevationMax
            DrawUsableMinMaxNumericField(_gameData.UserData.Elevation, "Elevation", -500f, 5000f);
        }

        private void DrawHillinessTypeSelection()
        {
            DrawEntryHeader("PLMWTT_TerrainTypes".Translate(),
                backgroundColor: ColorFromFilterSubjectThingDef("Terrains"));

            if (ListingStandard.ButtonText("PLMWTT_SelectTerrain".Translate()))
            {
                var floatMenuOptions = new List<FloatMenuOption>();
                foreach (var hillinessValue in _gameData.DefData.HillinessCollection)
                {
                    var label = "PLMW_SelectAny".Translate();

                    if (hillinessValue != Hilliness.Undefined)
                        label = hillinessValue.GetLabelCap();

                    var menuOption = new FloatMenuOption(label,
                        delegate { _gameData.UserData.ChosenHilliness = hillinessValue; });
                    floatMenuOptions.Add(menuOption);
                }

                var floatMenu = new FloatMenu(floatMenuOptions, "PLMWTT_SelectTerrain".Translate());
                Find.WindowStack.Add(floatMenu);
            }

            // note: RimWorld logs an error when .GetLabelCap() is used on Hilliness.Undefined
            var rightLabel = _gameData.UserData.ChosenHilliness != Hilliness.Undefined
                ? _gameData.UserData.ChosenHilliness.GetLabelCap()
                : "PLMW_SelectAny".Translate();
            ListingStandard.LabelDouble($"{"Terrain".Translate()}:", rightLabel);
        }

        private void DrawMovementTime()
        {
            DrawEntryHeader($"{"CaravanBaseMovementTime".Translate()} ({Find.ActiveLanguageWorker.Pluralize("PLMW_Hour".Translate())})", false,
                backgroundColor: ColorFromFilterSubjectThingDef("Current Movement Times"));

            DrawUsableMinMaxNumericField(_gameData.UserData.CurrentMovementTime, "MovementTimeNow".Translate());
            DrawUsableMinMaxNumericField(_gameData.UserData.SummerMovementTime, "MovementTimeSummer".Translate());
            DrawUsableMinMaxNumericField(_gameData.UserData.WinterMovementTime, "MovementTimeWinter".Translate());
        }

        private void DrawRiverTypesSelection()
        {
            DrawEntryHeader("PLMWTT_RiverTypes".Translate(), backgroundColor: ColorFromFilterSubjectThingDef("Rivers"));

            var riverDefs = _gameData.DefData.RiverDefs;
            var selectedRiverDefs = _gameData.UserData.SelectedRiverDefs;

            /*
             * Buttons
             */
            var numButtons = 4;
            if (_gameData.UserData.Options.ViewPartialOffNoSelect)
                numButtons += 1;

            var buttonsRect = ListingStandard.GetRect(DefaultElementHeight).SplitRectWidthEvenly(numButtons);
            if (buttonsRect.Count != numButtons)
            {
                Log.ErrorOnce($"[PrepareLanding] DrawRiverTypesSelection: couldn't get the right number of buttons: {numButtons}", 0x123acafe);
                return;
            }

            // Reset button: reset the container
            if (Widgets.ButtonTextToolTip(buttonsRect[0], "PLMW_Reset".Translate(), "PLMWTT_ButtonResetTooltip".Translate()))
                selectedRiverDefs.Reset(riverDefs, nameof(_gameData.UserData.SelectedRiverDefs));

            // All rivers
            if (Widgets.ButtonTextToolTip(buttonsRect[1], "PLMW_All".Translate(), "PLMWTT_ButtonAllTooltip".Translate()))
                selectedRiverDefs.All();

            // No rivers
            if (Widgets.ButtonTextToolTip(buttonsRect[2], "PLMW_None".Translate(), "PLMWTT_ButtonNoneTooltip".Translate()))
                selectedRiverDefs.None();

            // boolean filtering type
            if (Widgets.ButtonTextToolTipColor(buttonsRect[3], selectedRiverDefs.FilterBooleanState.ToStringHuman(), "PLMWTT_ORANDTooltip".Translate(), selectedRiverDefs.FilterBooleanState.Color()))
            {
                selectedRiverDefs.FilterBooleanState = selectedRiverDefs.FilterBooleanState.Next();
            }

            if (_gameData.UserData.Options.ViewPartialOffNoSelect)
            {
                var color = selectedRiverDefs.OffPartialNoSelect ? Color.green : Color.red;
                if (Widgets.ButtonTextToolTipColor(buttonsRect[4], $"{"PLMWTT_SelectedShort".Translate()} {selectedRiverDefs.OffPartialNoSelect}", "PLMWTT_OffPartialTooltip".Translate(), color))
                {
                    selectedRiverDefs.OffPartialNoSelect = !selectedRiverDefs.OffPartialNoSelect;
                }
            }

            /*
             * ScrollView
             */
            var inLs = ListingStandard.BeginScrollView(4*DefaultElementHeight,
                selectedRiverDefs.Count*DefaultElementHeight, ref _scrollPosRiverSelection, DefaultScrollableViewShrinkWidth);

            // display river elements
            foreach (var riverDef in riverDefs)
            {
                if (!selectedRiverDefs.TryGetValue(riverDef, out var threeStateItem))
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

        private void DrawRoadTypesSelection()
        {
            DrawEntryHeader("PLMWTT_RoadTypes".Translate(), backgroundColor: ColorFromFilterSubjectThingDef("Roads"));

            var roadDefs = _gameData.DefData.RoadDefs;
            var selectedRoadDefs = _gameData.UserData.SelectedRoadDefs;
            
            /*
             * Buttons
             */
            var numButtons = 4;
            if (_gameData.UserData.Options.ViewPartialOffNoSelect)
                numButtons += 1;

            var buttonsRect = ListingStandard.GetRect(DefaultElementHeight).SplitRectWidthEvenly(numButtons);
            if (buttonsRect.Count != numButtons)
            {
                Log.ErrorOnce($"[PrepareLanding] DrawRoadTypesSelection: couldn't get the right number of buttons: {numButtons}", 0x1239cafe);
                return;
            }

            // Reset button: reset the container
            if (Widgets.ButtonTextToolTip(buttonsRect[0], "PLMW_Reset".Translate(), "PLMWTT_ButtonResetTooltip".Translate()))
                selectedRoadDefs.Reset(roadDefs, nameof(_gameData.UserData.SelectedRoadDefs));

            // all roads
            if (Widgets.ButtonTextToolTip(buttonsRect[1], "PLMW_All".Translate(), "PLMWTT_ButtonAllTooltip".Translate()))
                selectedRoadDefs.All();

            // no roads
            if (Widgets.ButtonTextToolTip(buttonsRect[2], "PLMW_None".Translate(), "PLMWTT_ButtonNoneTooltip".Translate()))
                selectedRoadDefs.None();

            // boolean filtering type
            if (Widgets.ButtonTextToolTipColor(buttonsRect[3], selectedRoadDefs.FilterBooleanState.ToStringHuman(), "PLMWTT_ORANDTooltip".Translate(), selectedRoadDefs.FilterBooleanState.Color()))
            {
                selectedRoadDefs.FilterBooleanState = selectedRoadDefs.FilterBooleanState.Next();
            }

            if (_gameData.UserData.Options.ViewPartialOffNoSelect)
            {
                var color = selectedRoadDefs.OffPartialNoSelect ? Color.green : Color.red;
                if (Widgets.ButtonTextToolTipColor(buttonsRect[4], $"{"PLMWTT_SelectedShort".Translate()} {selectedRoadDefs.OffPartialNoSelect}", "PLMWTT_OffPartialTooltip".Translate(), color))
                {
                    selectedRoadDefs.OffPartialNoSelect = !selectedRoadDefs.OffPartialNoSelect;
                }
            }

            /*
             * ScrollView
             */
            var scrollViewHeight = selectedRoadDefs.Count*DefaultElementHeight;
            var inLs = ListingStandard.BeginScrollView(5 * DefaultElementHeight, scrollViewHeight,
                ref _scrollPosRoadSelection, DefaultScrollableViewShrinkWidth);

            // display road elements
            foreach (var roadDef in roadDefs)
            {
                if (!selectedRoadDefs.TryGetValue(roadDef, out var threeStateItem))
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

        private void DrawStoneTypesSelection()
        {
            DrawEntryHeader("PLMWTT_StoneTypes".Translate(), backgroundColor: ColorFromFilterSubjectThingDef("Stones"));

            var selectedStoneDefs = _gameData.UserData.SelectedStoneDefs;

            /*
             * Buttons
             */
            const int numButtons = 2;
            var buttonsRect = ListingStandard.GetRect(DefaultElementHeight).SplitRectWidthEvenly(numButtons);
            if (buttonsRect.Count != numButtons)
            {
                Log.ErrorOnce($"[PrepareLanding] DrawStoneTypesSelection: couldn't get the right number of buttons: {numButtons}", 0x123acafe);
                return;
            }

            // Reset button: reset all entries to Partial state
            if (Verse.Widgets.ButtonText(buttonsRect[0], "PLMW_Reset".Translate()))
            {
                selectedStoneDefs.Reset(_gameData.DefData.StoneDefs, nameof(_gameData.UserData.SelectedStoneDefs));

                _gameData.UserData.StoneTypesNumberOnly = false;
            }

            // order / no order button
            TooltipHandler.TipRegion(buttonsRect[1], "PLMWTT_StoneOrderTooltip".Translate());
            var orderText = selectedStoneDefs.OrderedFiltering ? "PLMWTT_Ordered".Translate() : "PLMWTT_NoOrder".Translate();
            var savedColor = GUI.color;
            GUI.color = selectedStoneDefs.OrderedFiltering ? Color.green : Color.red;
            if (Verse.Widgets.ButtonText(buttonsRect[1], $"{"PLMWTT_Filter".Translate()}: {orderText}"))
            {
                selectedStoneDefs.OrderedFiltering = !selectedStoneDefs.OrderedFiltering;
            }
            GUI.color = savedColor;

            // re-orderable list group
            var reorderableGroup = ReorderableWidget.NewGroup(delegate(int from, int to)
            {
                //TODO find a way to raise an event to tell an observer that the list order has changed
                selectedStoneDefs.ReorderElements(from, to);
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

                foreach (var currentOrderedStoneDef in selectedStoneDefs.OrderedItems)
                {
                    if (!selectedStoneDefs.TryGetValue(currentOrderedStoneDef, out var threeStateItem))
                    {
                        Log.ErrorOnce("A stoneDef wasn't found in selectedStoneDefs", 0x1cafe9);
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
            Verse.Widgets.CheckboxLabeled(leftRect, $"{"PLMWTT_UseNumberOfStoneTypes".Translate()}:", ref filterByStoneNumber);
            _gameData.UserData.StoneTypesNumberOnly = filterByStoneNumber;

            var numberOfStones = _gameData.UserData.StoneTypesNumber;
            Verse.Widgets.TextFieldNumeric(rightRect, ref numberOfStones, ref _bufferStringNumberOfStones, 2, 3);
            _gameData.UserData.StoneTypesNumber = numberOfStones;

            TooltipHandler.TipRegion(leftRect, "PLMWTT_UseNumberOfStoneTypesToolTip".Translate());
        }

        private void DrawTimeZoneSelection()
        {
            DrawEntryHeader($"{"TimeZone".Translate()} [-12, +12]", backgroundColor: ColorFromFilterSubjectThingDef("Time Zones"));

            DrawUsableMinMaxNumericField(_gameData.UserData.TimeZone, "TimeZone".Translate(), -12, 12);
        }
    }
}