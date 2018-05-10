using System;
using System.Collections.Generic;
using System.IO;
using PrepareLanding.Core.Extensions;
using PrepareLanding.Core.Gui;
using PrepareLanding.Core.Gui.Tab;
using PrepareLanding.Presets;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Steam;
using Widgets = Verse.Widgets;

namespace PrepareLanding
{
    /// <summary>
    ///     Type of mode we are in on the "load / save" tab.
    /// </summary>
    public enum LoadSaveMode
    {
        /// <summary>
        ///     Unknown mode.
        /// </summary>
        Unknown = 0,

        /// <summary>
        ///     Tab is in load mode
        /// </summary>
        Load = 1,

        /// <summary>
        ///     Tab is in save mode.
        /// </summary>
        Save = 2
    }

    public class TabLoadSave : TabGuiUtility
    {
        /// <summary>
        ///     Maximum number of preset to display at once.
        /// </summary>
        public const int MaxItemsToDisplay = 20;

        /// <summary>
        ///     Maximum length of preset description.
        /// </summary>
        public const int MaxDescriptionLength = 300;

        /// <summary>
        ///     Maximum author name length.
        /// </summary>
        public const int MaxAuthorNameLength = 50;

        // invalid selected item index in the preset list.
        private const int InvalidSelectedItemIndex = -1;

        // size of a bottom button (length, height)
        private readonly Vector2 _bottomButtonSize = new Vector2(130f, 30f);

        // list of descriptors for bottom buttons
        private readonly List<ButtonDescriptor> _buttonList;

        // gui style for the preset info text box.
        private readonly GUIStyle _stylePresetInfo;

        // game data
        private readonly GameData.GameData _gameData;

        // wheter or not, if clicking on save, this overwrite the preset directly (true) or first display a warning.
        private bool _allowOverwriteExistingPreset;

        // number of push on delete button (2 times to confirm delete)
        private int _confirmDeletePush;

        // starting index of the preset list
        private int _listDisplayStartIndex;

        // preset author name during save mode.
        private string _presetAuthorSave = string.Empty;

        // preset description text during save mode.
        private string _presetDescriptionSave = string.Empty;

        // whether or not to save options with the preset
        private bool _saveOptions;

        private Vector2 _scrollPosPresetDescription;

        private Vector2 _scrollPosPresetFiles;

        private Vector2 _scrollPosPresetFilterInfo;

        private Vector2 _scrollPosPresetLoadDescription;

        private Vector2 _scrollPosPresetOptionInfo;

        // filename of the selected preset (load mode)
        private string _selectedFileName = string.Empty;

        // index of the selected preset in the preset list (load mode)
        private int _selectedItemIndex = -1;

        public TabLoadSave(GameData.GameData gameData, float columnSizePercent = 0.25f) :
            base(columnSizePercent)
        {
            _gameData = gameData;
            LoadSaveMode = LoadSaveMode.Unknown;

            _stylePresetInfo = new GUIStyle(Text.textFieldStyles[1])
            {
                alignment = TextAnchor.UpperLeft,
                wordWrap = true,
                richText = true
            };

            // setup default name 
            // TODO check if possible to get logged in user if non steam rimworld
            _presetAuthorSave = SteamManager.Initialized ? SteamUtility.SteamPersonaName : "Your_Name";

            #region LIST_BUTTONS

            var buttonListStart = new ButtonDescriptor("<<", delegate
            {
                // reset starting display index
                _listDisplayStartIndex = 0;
            }, "Go to start of item list.");

            var buttonPreviousPage = new ButtonDescriptor("<", delegate
            {
                if (_listDisplayStartIndex >= MaxItemsToDisplay)
                    _listDisplayStartIndex -= MaxItemsToDisplay;
                else
                    Messages.Message("Reached start of item list.", MessageTypeDefOf.RejectInput);
            }, "Go to previous list page.");

            var buttonNextPage = new ButtonDescriptor(">", delegate
            {
                var presetFilesCount = _gameData.PresetManager.AllPresetFiles.Count;
                _listDisplayStartIndex += MaxItemsToDisplay;
                if (_listDisplayStartIndex > presetFilesCount)
                {
                    Messages.Message($"No more available items to display (max: {presetFilesCount}).",
                        MessageTypeDefOf.RejectInput);
                    _listDisplayStartIndex -= MaxItemsToDisplay;
                }
            }, "Go to next list page.");

            var buttonListEnd = new ButtonDescriptor(">>", delegate
            {
                var presetFilesCount = _gameData.PresetManager.AllPresetFiles.Count;
                var displayIndexStart = presetFilesCount - presetFilesCount%MaxItemsToDisplay;
                if (displayIndexStart == _listDisplayStartIndex)
                    Messages.Message($"No more available items to display (max: {presetFilesCount}).",
                        MessageTypeDefOf.RejectInput);

                _listDisplayStartIndex = displayIndexStart;
            }, "Go to end of list.");

            _buttonList =
                new List<ButtonDescriptor> {buttonListStart, buttonPreviousPage, buttonNextPage, buttonListEnd};

            #endregion
        }

        /// <summary>Gets whether the tab can be draw or not.</summary>
        public override bool CanBeDrawn
        {
            get { return LoadSaveMode != LoadSaveMode.Unknown; }
            set { }
        }

        /// <summary>A unique identifier for the Tab.</summary>
        public override string Id => "LoadSave";

        public LoadSaveMode LoadSaveMode { get; set; }

        /// <summary>The name of the tab (that is actually displayed at its top).</summary>
        public override string Name
        {
            get
            {
                string name;
                switch (LoadSaveMode)
                {
                    case LoadSaveMode.Unknown:
                        name = "Load / Save";
                        break;
                    case LoadSaveMode.Load:
                        name = "Load";
                        break;
                    case LoadSaveMode.Save:
                        name = "Save";
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
                return name;
            }
        }

        /// <summary>Draw the content of the tab.</summary>
        /// <param name="inRect">The <see cref="T:UnityEngine.Rect" /> in which to draw the tab content.</param>
        public override void Draw(Rect inRect)
        {
            if (!CanBeDrawn)
                return;

            switch (LoadSaveMode)
            {
                case LoadSaveMode.Load:
                    DrawLoadMode(inRect);
                    break;
                case LoadSaveMode.Save:
                    DrawSaveMode(inRect);
                    break;

                case LoadSaveMode.Unknown:
                    break;
            }
            DrawBottomButtons(inRect);
        }

        protected void DrawBottomButtons(Rect inRect)
        {
            var buttonsY = inRect.height - 30f;
            var numButtons = 2;

            // add 1 button for delete if we are in load mode
            if (LoadSaveMode == LoadSaveMode.Load)
                numButtons += 1;

            // get Rect for buttons
            var buttonRects = inRect.SpaceEvenlyFromCenter(buttonsY, numButtons, _bottomButtonSize.x,
                _bottomButtonSize.y, 20f);
            if (buttonRects.Count != numButtons)
            {
                Log.ErrorOnce("[Prepare Landing]: DrawBottomButtons(); wrong number of buttons", 0x1cafe9);
                return;
            }

            // get the text of button, depending on the mode we're in
            string verb;
            switch (LoadSaveMode)
            {
                case LoadSaveMode.Unknown:
                    // shouldn't happen
                    verb = "Load / Save";
                    break;
                case LoadSaveMode.Load:
                    verb = "Load";
                    break;
                case LoadSaveMode.Save:
                    verb = "Save";
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            // just change the save button color if we could overwrite an existing preset.
            var presetExistsProtectFromOverwrite = false;
            if (!string.IsNullOrEmpty(_selectedFileName))
                presetExistsProtectFromOverwrite = PresetManager.PresetExists(_selectedFileName) &&
                                                   !_allowOverwriteExistingPreset;

            var savedColor = GUI.color;
            if (LoadSaveMode == LoadSaveMode.Save)
                GUI.color = presetExistsProtectFromOverwrite ? Color.red : Color.green;
            else
                GUI.color = Color.green;

            // display the action button (load / save)
            if (Widgets.ButtonText(buttonRects[0], $"{verb} Preset"))
                switch (LoadSaveMode)
                {
                    case LoadSaveMode.Load:
                        LoadPreset();
                        break;
                    case LoadSaveMode.Save:
                        SavePreset(presetExistsProtectFromOverwrite);
                        break;
                }
            GUI.color = savedColor;

            // delete button: only if in load mode!
            var rectIndex = 1;
            if (LoadSaveMode == LoadSaveMode.Load)
            {
                GUI.color = Color.red;
                var deleteButtonText = "Delete";
                if (_confirmDeletePush == 1)
                    deleteButtonText = "Delete [Confirm]";

                if (Widgets.ButtonText(buttonRects[rectIndex], deleteButtonText))
                {
                    _confirmDeletePush++;
                    if (_confirmDeletePush == 2)
                    {
                        _confirmDeletePush = 0;
                        DeletePreset();
                        _selectedItemIndex = InvalidSelectedItemIndex;
                        _selectedFileName = null;
                    }
                }
                GUI.color = savedColor;

                rectIndex++;
            }

            // exit button
            if (Widgets.ButtonText(buttonRects[rectIndex], $"Exit {verb}"))
            {
                LoadSaveMode = LoadSaveMode.Unknown;
                _allowOverwriteExistingPreset = false;
                _selectedItemIndex = InvalidSelectedItemIndex;
                _selectedFileName = null;
                _confirmDeletePush = 0;
                PrepareLanding.Instance.MainWindow.TabController.SetPreviousTabAsSelectedTab();
            }
        }

        private void DeletePreset()
        {
            if (string.IsNullOrEmpty(_selectedFileName))
            {
                Messages.Message("Pick a preset first.", MessageTypeDefOf.NegativeEvent);
                return;
            }

            if (_gameData.PresetManager.DeletePreset(_selectedFileName))
                    Messages.Message("Successfully deleted the preset!", MessageTypeDefOf.PositiveEvent);
            else
                Messages.Message("Error: couldn't delete the preset...", MessageTypeDefOf.NegativeEvent);
        }

        #region LOAD_MODE

        protected void DrawLoadMode(Rect inRect)
        {
            Begin(inRect);
            DrawLoadPresetList(inRect);
            NewColumn();
            DrawLoadPresetInfo(inRect);
            End();
        }

        private void LoadPreset()
        {
            if (!string.IsNullOrEmpty(_selectedFileName))
                if (_gameData.PresetManager.LoadPreset(_selectedFileName, true))
                    Messages.Message("Successfully loaded the preset!", MessageTypeDefOf.PositiveEvent);
                else
                    Messages.Message("Error: couldn't load the preset...", MessageTypeDefOf.NegativeEvent);
            else
                Messages.Message("Pick a preset first.", MessageTypeDefOf.NegativeEvent);
        }

        private void DrawLoadPresetList(Rect inRect)
        {
            DrawEntryHeader("Preset Files: Load mode", backgroundColor: Color.green);

            var presetFiles = _gameData.PresetManager.AllPresetFiles;
            if (presetFiles == null)
            {
                Log.ErrorOnce("[PrepareLanding] PresetManager.AllPresetFiles is null.", 0x1238cafe);
                return;
            }

            var presetFilesCount = presetFiles.Count;
            if (presetFiles.Count == 0)
            {
                ListingStandard.Label("No existing presets.");
                return;
            }

            // add a gap before the scroll view
            ListingStandard.Gap(DefaultGapLineHeight);

            /*
             * Buttons
             */

            var buttonsRectSpace = ListingStandard.GetRect(30f);
            var splittedRect = buttonsRectSpace.SplitRectWidthEvenly(_buttonList.Count);

            for (var i = 0; i < _buttonList.Count; i++)
            {
                // get button descriptor
                var buttonDescriptor = _buttonList[i];

                // display button; if clicked: call the related action
                if (Widgets.ButtonText(splittedRect[i], buttonDescriptor.Label))
                    buttonDescriptor.Action();

                // display tool-tip (if any)
                if (!string.IsNullOrEmpty(buttonDescriptor.ToolTip))
                    TooltipHandler.TipRegion(splittedRect[i], buttonDescriptor.ToolTip);
            }

            /*
             * Label
             */

            // number of elements (tiles) to display
            var itemsToDisplay = Math.Min(presetFilesCount - _listDisplayStartIndex, MaxItemsToDisplay);

            // label to display where we actually are in the tile list
            GenUI.SetLabelAlign(TextAnchor.MiddleCenter);
            var heightBefore = ListingStandard.StartCaptureHeight();
            ListingStandard.Label(
                $"{_listDisplayStartIndex}: {_listDisplayStartIndex + itemsToDisplay - 1} / {presetFilesCount - 1}",
                DefaultElementHeight);
            GenUI.ResetLabelAlign();
            var counterLabelRect = ListingStandard.EndCaptureHeight(heightBefore);
            Core.Gui.Widgets.DrawHighlightColor(counterLabelRect, Color.cyan, 0.50f);

            // add a gap before the scroll view
            ListingStandard.Gap(DefaultGapLineHeight);

            /*
             * Calculate heights
             */

            // height of the scrollable outer Rect (visible portion of the scroll view, not the 'virtual' one)
            var maxScrollViewOuterHeight = InRect.height - ListingStandard.CurHeight - 30f;

            // height of the 'virtual' portion of the scroll view
            var scrollableViewHeight = itemsToDisplay*DefaultElementHeight + DefaultGapLineHeight*MaxItemsToDisplay;

            /*
             * Scroll view
             */
            var innerLs = ListingStandard.BeginScrollView(maxScrollViewOuterHeight, scrollableViewHeight,
                ref _scrollPosPresetFiles, 16f);

            var endIndex = _listDisplayStartIndex + itemsToDisplay;
            for (var i = _listDisplayStartIndex; i < endIndex; i++)
            {
                var selectedPresetFile = presetFiles[i];
                var labelText = Path.GetFileNameWithoutExtension(selectedPresetFile.Name);

                // display the label
                var labelRect = innerLs.GetRect(DefaultElementHeight);
                var selected = i == _selectedItemIndex;
                if (Core.Gui.Widgets.LabelSelectable(labelRect, labelText, ref selected, TextAnchor.MiddleCenter))
                {
                    // save item index
                    _selectedItemIndex = i;
                    _selectedFileName = labelText;
                }
                // add a thin line between each label
                innerLs.GapLine(DefaultGapLineHeight);
            }

            ListingStandard.EndScrollView(innerLs);
        }

        protected void DrawLoadPresetInfo(Rect inRect)
        {
            DrawEntryHeader("Preset Info:", backgroundColor: Color.green);

            if (_selectedItemIndex < 0)
                return;

            ListingStandard.TextEntryLabeled2("Preset Name:", _selectedFileName);

            var preset = _gameData.PresetManager.PresetByPresetName(_selectedFileName);
            if (preset == null)
                return;

            ListingStandard.TextEntryLabeled2("Author:", preset.PresetInfo.Author);

            ListingStandard.Label("Description:");
            var descriptionRect = ListingStandard.GetRect(80f);
            Widgets.TextAreaScrollable(descriptionRect, preset.PresetInfo.Description,
                ref _scrollPosPresetLoadDescription);

            ListingStandard.Label("Filters:");
            const float maxOuterRectHeight = 130f;
            ListingStandard.ScrollableTextArea(maxOuterRectHeight, preset.PresetInfo.FilterInfo,
                ref _scrollPosPresetFilterInfo, _stylePresetInfo, DefaultScrollableViewShrinkWidth);

            ListingStandard.Label("Options:");
            ListingStandard.ScrollableTextArea(maxOuterRectHeight, preset.PresetInfo.OptionInfo,
                ref _scrollPosPresetOptionInfo, _stylePresetInfo, DefaultScrollableViewShrinkWidth);
        }

        #endregion LOAD_MODE

        #region SAVE_MODE

        protected void DrawSaveMode(Rect inRect)
        {
            Begin(inRect);
            DrawSaveFileName(inRect);
            End();
        }

        private void SavePreset(bool presetExistsProtectFromOverwrite)
        {
            if (_gameData.UserData.AreAllFieldsInDefaultSate())
                Messages.Message("All filters seem to be in their default state", MessageTypeDefOf.RejectInput);
            else
            {
                if (presetExistsProtectFromOverwrite)
                {
                    _allowOverwriteExistingPreset = true;
                    Messages.Message(
                        "Click again on the \"Save\" button to confirm the overwrite of the existing preset.",
                        MessageTypeDefOf.NeutralEvent);
                }
                else
                {
                    _allowOverwriteExistingPreset = false;
                    if (_gameData.PresetManager.SavePreset(_selectedFileName, _presetDescriptionSave, _presetAuthorSave,
                        _saveOptions))
                        Messages.Message("Successfully saved the preset!", MessageTypeDefOf.PositiveEvent);
                    else
                        Messages.Message("Error: couldn't save the preset...", MessageTypeDefOf.NegativeEvent);
                }
            }
        }

        protected void DrawSaveFileName(Rect inRect)
        {
            DrawEntryHeader("Preset Files: Save mode", backgroundColor: Color.red);

            var fileNameRect = ListingStandard.GetRect(DefaultElementHeight);

            var fileNameLabelRect = fileNameRect.LeftPart(0.2f);
            Widgets.Label(fileNameLabelRect, "FileName:");

            var fileNameTextRect = fileNameRect.RightPart(0.8f);
            if (string.IsNullOrEmpty(_selectedFileName))
                _selectedFileName = _gameData.PresetManager.NextPresetFileName;

            _selectedFileName = Widgets.TextField(fileNameTextRect, _selectedFileName);

            ListingStandard.GapLine(DefaultGapLineHeight);

            ListingStandard.CheckboxLabeled("Save Options", ref _saveOptions,
                "Check to also save options alongside filters.");

            ListingStandard.GapLine(DefaultGapLineHeight);

            DrawEntryHeader($"Author: [optional; {MaxAuthorNameLength} chars max]");

            _presetAuthorSave = ListingStandard.TextEntry(_presetAuthorSave);
            if (_presetAuthorSave.Length >= MaxAuthorNameLength)
                _presetAuthorSave = _presetAuthorSave.Substring(0, MaxAuthorNameLength);

            ListingStandard.GapLine(DefaultGapLineHeight);

            DrawEntryHeader($"Description: [optional; {MaxDescriptionLength} chars max]");

            var descriptionRect = ListingStandard.GetRect(80f);
            _presetDescriptionSave = Widgets.TextAreaScrollable(descriptionRect, _presetDescriptionSave,
                ref _scrollPosPresetDescription);
            if (_presetDescriptionSave.Length >= MaxDescriptionLength)
                _presetDescriptionSave = _presetDescriptionSave.Substring(0, MaxDescriptionLength);
        }

        #endregion SAVE_MODE
    }
}