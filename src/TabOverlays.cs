using System;
using System.Collections.Generic;
using PrepareLanding.Core.Gui.Tab;
using UnityEngine;
using Verse;

namespace PrepareLanding
{
    public class TabOverlays : TabGuiUtility
    {
        private readonly GameData.GameData _gameData;

        public TabOverlays(GameData.GameData gameData, float columnSizePercent) : base(columnSizePercent)
        {
            _gameData = gameData;
        }

        /// <summary>Gets whether the tab can be drawn or not.</summary>
        public override bool CanBeDrawn
        {
            get { return true; }
            set { }
        }

        /// <summary>A unique identifier for the Tab.</summary>
        public override string Id => "Overlays";

        /// <summary>The name of the tab (that is actually displayed at its top).</summary>
        public override string Name => "Overlays";

        /// <summary>Draw the content of the tab.</summary>
        /// <param name="inRect">The <see cref="T:UnityEngine.Rect" /> in which to draw the tab content.</param>
        public override void Draw(Rect inRect)
        {
            Begin(inRect);
            DrawOverLaySelection();
            DrawBiomeTypesSelection();
            End();
        }

        private void DrawOverLaySelection()
        {
            DrawEntryHeader("Overlay Selection", backgroundColor: ColorLibrary.BurntOrange);

            var boxRect = ListingStandard.GetRect(DefaultElementHeight);

            // draw the map colors
            GUI.DrawTexture(boxRect, _gameData.WorldData.TemperatureData.TemperatureGradientTexure);

            ListingStandard.GapLine(DefaultGapLineHeight);

            var buttonsRect = ListingStandard.GetRect(DefaultElementHeight);
            var drawOverlayButtonRect = buttonsRect.LeftHalf();
            var clearOverlayButtonRect = buttonsRect.RightHalf();

            if (Widgets.ButtonText(drawOverlayButtonRect, "Draw Overlay"))
                _gameData.WorldData.TemperatureData.AllowDrawOverlay = true;

            if (Widgets.ButtonText(clearOverlayButtonRect, "Clear Overlay"))
                _gameData.WorldData.TemperatureData.AllowDrawOverlay = false;

#if DEBUG
            if (ListingStandard.ButtonText("DebugLog"))
            {
                _gameData.WorldData.TemperatureData.DebugLog();
            }
#endif
        }

        private void DrawBiomeTypesSelection() // TODO : factorize this function with the one from TabTerrain
        {
            DrawEntryHeader("Biome Types", backgroundColor: ColorLibrary.BurntOrange);

            var biomeDefs = _gameData.DefData.BiomeDefs;

            // "Select" button
            if (ListingStandard.ButtonText("Select Biome"))
            {
                var floatMenuOptions = new List<FloatMenuOption>();

                // add a dummy 'Any' fake biome type. This sets the chosen biome to null.
                Action actionClick = delegate { _gameData.WorldData.TemperatureData.Biome = null; };
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
                    actionClick = delegate { _gameData.WorldData.TemperatureData.Biome = currentBiomeDef; };
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

            var rightLabel = _gameData.WorldData.TemperatureData.Biome != null
                ? _gameData.WorldData.TemperatureData.Biome.LabelCap
                : "Any";
            ListingStandard.LabelDouble("Biome:", rightLabel);

            var currHeightAfter = ListingStandard.CurHeight;

            // display tool-tip over label
            if (_gameData.WorldData.TemperatureData.Biome != null)
            {
                var currentRect = ListingStandard.GetRect(0f);
                currentRect.height = currHeightAfter - currHeightBefore;
                if (!string.IsNullOrEmpty(_gameData.WorldData.TemperatureData.Biome.description))
                    TooltipHandler.TipRegion(currentRect, _gameData.WorldData.TemperatureData.Biome.description);
            }
        }
    }
}