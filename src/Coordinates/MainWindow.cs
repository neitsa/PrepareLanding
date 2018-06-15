using System.Collections.Generic;
using PrepareLanding.Core.Extensions;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace PrepareLanding.Coordinates
{
    public class MainWindow : Window
    {
        private readonly ButtonDrawerHandler _buttonDrawerHandler;
        private readonly float _columnSizePct;
        private readonly float[] _goToCoordsPct = { 0.30f, 0.50f, 0.20f };

        private readonly float[] _goToTileSplitPct = { 0.55f, 0.25f, 0.20f };
        private readonly Listing_Standard _listingStandard;

        private List<ButtonTextDescriptorCoordinates> _buttonsDescriptors;

        private string _goToCoordsString;

        private int _goToTileId;
        private string _goToTileIdString;

#if DEBUG_LONGLATDRAWER
        private LongitudeLatitudeDrawer _longitudeLatitudeDrawer;
#endif

        public MainWindow(float columnSizePercent = 1.0f)
        {
            doCloseButton = false;
            doCloseX = true;
            preventCameraMotion = false;
            draggable = true;

            _listingStandard = new Listing_Standard();
            _columnSizePct = columnSizePercent;

            void UnfoldAction(DrawerButton button)
            {
                if (button.IsUnfolded)
                    windowRect.height += button.DrawerHeight;
                else
                    windowRect.height -= button.DrawerHeight;
            }

            var buttonSpecificLocations =
                new DrawerButton("PLCOORDWIN_WorldSpecificLocations".Translate(), DrawSpecificLocations, UnfoldAction);
            //var buttonReportLog = new DrawerButton("Report Log", DrawReportLog, unfoldAction);

            _buttonDrawerHandler = new ButtonDrawerHandler();
            _buttonDrawerHandler.AddButton(buttonSpecificLocations);
            //_buttonDrawerHandler.AddButton(buttonReportLog);
        }

        public override Vector2 InitialSize { get; } = new Vector2(300f, 115f);

        public static bool CanBeDisplayed => WorldRendererUtility.WorldRenderedNow;

        public static bool IsInWindowStack => Find.WindowStack.IsOpen<MainWindow>();

        public override void PreOpen()
        {
            base.PreOpen();
            _buttonsDescriptors = new List<ButtonTextDescriptorCoordinates>();
            var b1 = new ButtonTextDescriptorCoordinates("PLCOORDWIN_NorthPoleAbbr".Translate(),
                "PLCOORDWIN_NorthPole".Translate(), Find.WorldGrid.NorthPolePos);
            _buttonsDescriptors.Add(b1);
            var b2 = new ButtonTextDescriptorCoordinates("PLCOORDWIN_SouthPoleAbbr".Translate(),
                "PLCOORDWIN_SouthPole".Translate(), new Vector3(0, -100f, 0));
            _buttonsDescriptors.Add(b2);
            var b3 = new ButtonTextDescriptorCoordinates("PLCOORDWIN_OriginPointAbbr".Translate(),
                "PLCOORDWIN_OriginPoint".Translate(),
                new Vector3(0, 0, -100)); // note: negative z component is facing us at world start.
            _buttonsDescriptors.Add(b3);
            var b4 = new ButtonTextDescriptorCoordinates("PLCOORDWIN_AnteOriginAbbr".Translate(),
                "PLCOORDWIN_AnteOrigin".Translate(), new Vector3(0, 0, 100));
            _buttonsDescriptors.Add(b4);

#if DEBUG_LONGLATDRAWER
            _longitudeLatitudeDrawer = new LongitudeLatitudeDrawer();
#endif
        }

        public override void PreClose()
        {
            base.PreClose();
#if DEBUG_LONGLATDRAWER
            _longitudeLatitudeDrawer.UnRegister();
            _longitudeLatitudeDrawer = null;
#endif
        }

        private void Begin(Rect inRect)
        {
            _listingStandard.ColumnWidth = inRect.width * _columnSizePct;
            _listingStandard.Begin(inRect);
        }

        private void End()
        {
            _listingStandard.End();
        }

        public override void DoWindowContents(Rect inRect)
        {
            // close the window if it cannot be displayed
            if(!CanBeDisplayed)
                Close();

            // draw content
            Begin(inRect);
            DrawGoToTile();
            DrawGoToCoordinates();

            var drawerRect = _listingStandard.GetRect(15f);
            _buttonDrawerHandler.DrawButtons(drawerRect);
            var contentRect = _listingStandard.GetRect(30f);
            _buttonDrawerHandler.DrawUnfoldedContent(contentRect);

            End();
        }

        private void DrawGoToTile()
        {
            var goToTileRect = _listingStandard.GetRect(30f);
            var rects = goToTileRect.SplitBy(_goToTileSplitPct, 5f);

            var selectedTile = Find.WorldSelector.selectedTile;
            if (selectedTile != Tile.Invalid && selectedTile != _goToTileId)
                SetGuiFromTile(selectedTile, Find.WorldGrid.GetTileCenter(selectedTile), false);

            var maxTileId = Find.WorldGrid.TilesCount - 1;
            Widgets.Label(rects[0], string.Format("PLCOORDWIN_TileIdNum".Translate(), maxTileId));
            Widgets.TextFieldNumeric(rects[1], ref _goToTileId, ref _goToTileIdString, 0, maxTileId);
            if (!Widgets.ButtonText(rects[2], "PLCOORDWIN_GoButton".Translate()))
                return;

            if (_goToTileId == Tile.Invalid || _goToTileId < 0 || _goToTileId >= Find.WorldGrid.TilesCount)
                Messages.Message(
                    $"[PrepareLanding] {string.Format("PLCOORDWIN_TileIdOutOfRange".Translate(), _goToTileId, Find.WorldGrid.TilesCount)}",
                    MessageTypeDefOf.RejectInput);
            else
                SetGuiFromTile(_goToTileId, Find.WorldGrid.GetTileCenter(_goToTileId));
        }

        private void DrawGoToCoordinates()
        {
            var goToCoordsRect = _listingStandard.GetRect(30f);
            var rects = goToCoordsRect.SplitBy(_goToCoordsPct, 5f);

            Widgets.Label(rects[0], "PLCOORDWIN_CoordsLabel".Translate());
            _goToCoordsString = Widgets.TextField(rects[1], _goToCoordsString);
            if (!Widgets.ButtonText(rects[2], "PLCOORDWIN_GoButton".Translate()))
                return;

            var coords = new Coordinates(_goToCoordsString);
            var tileId = coords.FindTile();
            SetGuiFromTile(tileId, coords.CoordinatesVector);
        }

        private void DrawSpecificLocations(Rect inRect)
        {
            var numButtons = Mathf.RoundToInt(inRect.width / 50f);
            var buttonRects = inRect.Split(numButtons - 1, 5f);

            var index = 0;
            foreach (var buttonDescriptor in _buttonsDescriptors)
            {
                if (Widgets.ButtonText(buttonRects[index], buttonDescriptor.ButtonText))
                {
                    var coords = new Coordinates(buttonDescriptor.Coordinates);
                    var tileId = coords.FindTile();
                    SetGuiFromTile(tileId, buttonDescriptor.Coordinates);
                }
                TooltipHandler.TipRegion(buttonRects[index], buttonDescriptor.ToolTip);
                index++;
            }
        }

        private void SetGuiFromTile(int tileId, Vector3 coordinates, bool jumpTo = true)
        {
            if (tileId != Tile.Invalid)
            {
                if (jumpTo)
                    Find.WorldCameraDriver.JumpTo(tileId);
                Find.WorldSelector.selectedTile = tileId;
                _goToTileId = tileId;
                _goToTileIdString = tileId.ToString();
                var vec = Find.WorldGrid.LongLatOf(tileId);
                _goToCoordsString = $"{vec.y.ToStringLatitude()} {vec.x.ToStringLongitude()}";
            }
            else
            {
                if (jumpTo)
                    Find.WorldCameraDriver.JumpTo(coordinates);
                Find.WorldSelector.selectedTile = Tile.Invalid;
                _goToTileId = tileId;
                _goToTileIdString = string.Empty;
                _goToCoordsString = Coordinates.LongLatOfString(coordinates);
                Messages.Message($"[PrepareLanding] {"PLCOORDWIN_NoTileForCoords".Translate()} '{_goToCoordsString}",
                    MessageTypeDefOf.NegativeEvent);
            }
        }
    }
}
