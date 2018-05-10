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
        private string _presetAuthorSave;

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
            }, "PLMWLODSAV_GoToStartOfItemList".Translate());

            var buttonPreviousPage = new ButtonDescriptor("<", delegate
            {
                if (_listDisplayStartIndex >= MaxItemsToDisplay)
                    _listDisplayStartIndex -= MaxItemsToDisplay;
                else
                    Messages.Message("PLMWLODSAV_ReachedListStart".Translate(), MessageTypeDefOf.RejectInput);
            }, "PLMWLODSAV_GoToPreviousListPage".Translate());

            var buttonNextPage = new ButtonDescriptor(">", delegate
            {
                var presetFilesCount = _gameData.PresetManager.AllPresetFiles.Count;
                _listDisplayStartIndex += MaxItemsToDisplay;
                if (_listDisplayStartIndex > presetFilesCount)
                {
                    Messages.Message($"{"PLMWLODSAV_NoMoreItemsAvailable".Translate()} {presetFilesCount}).",
                        MessageTypeDefOf.RejectInput);
                    _listDisplayStartIndex -= MaxItemsToDisplay;
                }
            }, "PLMWLODSAV_GoToNextListPage".Translate());

            var buttonListEnd = new ButtonDescriptor(">>", delegate
            {
                var presetFilesCount = _gameData.PresetManager.AllPresetFiles.Count;
                var displayIndexStart = presetFilesCount - presetFilesCount%MaxItemsToDisplay;
                if (displayIndexStart == _listDisplayStartIndex)
                    Messages.Message($"{"PLMWLODSAV_NoMoreItemsAvailable".Translate()} {presetFilesCount}).",
                        MessageTypeDefOf.RejectInput);

                _listDisplayStartIndex = displayIndexStart;
            }, "PLMWLODSAV_GoToEndOfList".Translate());

            _buttonList =
                new List<ButtonDescriptor> {buttonListStart, buttonPreviousPage, buttonNextPage, buttonListEnd};

            #endregion
        }

        /// <summary>Gets whether the tab can be draw or not.</summary>
        public override bool CanBeDrawn
        {
            get => LoadSaveMode != LoadSaveMode.Unknown;
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
                        name = $"{"PLMWLODSAV_Load".Translate()} / {"PLMWLODSAV_Save".Translate()}";
                        break;
                    case LoadSaveMode.Load:
                        name = "PLMWLODSAV_Load".Translate();
                        break;
                    case LoadSaveMode.Save:
                        name = "PLMWLODSAV_Save".Translate();
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

                default:
                    Log.ErrorOnce("[PrepareLanding] TabLoadSave: unknown mode", 0x1234cafe);
                    break;
            }
            DrawBottomButtons(inRect);
        }

        private void DrawBottomButtons(Rect inRect)
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
            if (Widgets.ButtonText(buttonRects[0], $"{Name} Preset"))
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
                var deleteButtonText = "PLMWLODSAV_Delete".Translate();
                if (_confirmDeletePush == 1)
                    deleteButtonText = "PLMWLODSAV_DeleteConfirm".Translate();

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
            if (Widgets.ButtonText(buttonRects[rectIndex], $"{"PLMWLODSAV_ExitCurrentMode".Translate()} {Name}"))
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
                Messages.Message("PLMWLODSAV_PickPresetFirst".Translate(), MessageTypeDefOf.NegativeEvent);
                return;
            }

            if (_gameData.PresetManager.DeletePreset(_selectedFileName))
                    Messages.Message("PLMWLODSAV_SuccessDelete".Translate(), MessageTypeDefOf.PositiveEvent);
            else
                Messages.Message("PLMWLODSAV_ErrorDelete".Translate(), MessageTypeDefOf.NegativeEvent);
        }

        #region LOAD_MODE

        private void DrawLoadMode(Rect inRect)
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
                    Messages.Message("PLMWLODSAV_SuccessLoad".Translate(), MessageTypeDefOf.PositiveEvent);
                else
                    Messages.Message("PLMWLODSAV_ErrorLoad".Translate(), MessageTypeDefOf.NegativeEvent);
            else
                Messages.Message("PLMWLODSAV_PickPresetFirst".Translate(), MessageTypeDefOf.NegativeEvent);
        }

        private void DrawLoadPresetList(Rect inRect)
        {
            DrawEntryHeader("PLMWLODSAV_PresetLoadMode".Translate(), backgroundColor: Color.green);

            var presetFiles = _gameData.PresetManager.AllPresetFiles;
            if (presetFiles == null)
            {
                Log.ErrorOnce("[PrepareLanding] PresetManager.AllPresetFiles is null.", 0x1238cafe);
                return;
            }

            var presetFilesCount = presetFiles.Count;
            if (presetFiles.Count == 0)
            {
                ListingStandard.Label("PLMWLODSAV_NoExistingPresets".Translate());
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

        private void DrawLoadPresetInfo(Rect inRect)
        {
            DrawEntryHeader("PLMWLODSAV_PresetInfo".Translate(), backgroundColor: Color.green);

            if (_selectedItemIndex < 0)
                return;

            ListingStandard.TextEntryLabeled2("PLMWLODSAV_PresetName".Translate(), _selectedFileName);

            var preset = _gameData.PresetManager.PresetByPresetName(_selectedFileName);
            if (preset == null)
                return;

            ListingStandard.TextEntryLabeled2("PLMWLODSAV_PresetAuthor".Translate(), preset.PresetInfo.Author);

            ListingStandard.Label("PLMWLODSAV_PresetDescription".Translate());
            var descriptionRect = ListingStandard.GetRect(80f);
            Widgets.TextAreaScrollable(descriptionRect, preset.PresetInfo.Description,
                ref _scrollPosPresetLoadDescription);

            ListingStandard.Label("PLMWLODSAV_PresetFilters".Translate());
            const float maxOuterRectHeight = 130f;
            ListingStandard.ScrollableTextArea(maxOuterRectHeight, preset.PresetInfo.FilterInfo,
                ref _scrollPosPresetFilterInfo, _stylePresetInfo, DefaultScrollableViewShrinkWidth);

            ListingStandard.Label("PLMWLODSAV_PresetOptions".Translate());
            ListingStandard.ScrollableTextArea(maxOuterRectHeight, preset.PresetInfo.OptionInfo,
                ref _scrollPosPresetOptionInfo, _stylePresetInfo, DefaultScrollableViewShrinkWidth);
        }

        #endregion LOAD_MODE

        #region SAVE_MODE

        private void DrawSaveMode(Rect inRect)
        {
            Begin(inRect);
            DrawSaveFileName(inRect);
            End();
        }

        private void SavePreset(bool presetExistsProtectFromOverwrite)
        {
            if (_gameData.UserData.AreAllFieldsInDefaultSate())
                Messages.Message("PLMWLODSAV_AllFiltersInDefaultState".Translate(), MessageTypeDefOf.RejectInput);
            else
            {
                if (presetExistsProtectFromOverwrite)
                {
                    _allowOverwriteExistingPreset = true;
                    Messages.Message("PLMWLODSAV_ClickAgainOnSaveButton".Translate(), MessageTypeDefOf.NeutralEvent);
                }
                else
                {
                    _allowOverwriteExistingPreset = false;
                    if (_gameData.PresetManager.SavePreset(_selectedFileName, _presetDescriptionSave, _presetAuthorSave,
                        _saveOptions))
                        Messages.Message("PLMWLODSAV_SuccessSave".Translate(), MessageTypeDefOf.PositiveEvent);
                    else
                        Messages.Message("PLMWLODSAV_ErrorSave".Translate(), MessageTypeDefOf.NegativeEvent);
                }
            }
        }

        private void DrawSaveFileName(Rect inRect)
        {
            DrawEntryHeader("PLMWLODSAV_PresetSaveMode".Translate(), backgroundColor: Color.red);

            var fileNameRect = ListingStandard.GetRect(DefaultElementHeight);

            var fileNameLabelRect = fileNameRect.LeftPart(0.2f);
            Widgets.Label(fileNameLabelRect, "PLMWLODSAV_FileName".Translate());

            var fileNameTextRect = fileNameRect.RightPart(0.8f);
            if (string.IsNullOrEmpty(_selectedFileName))
                _selectedFileName = _gameData.PresetManager.NextPresetFileName;

            _selectedFileName = Widgets.TextField(fileNameTextRect, _selectedFileName);

            ListingStandard.GapLine(DefaultGapLineHeight);

            ListingStandard.CheckboxLabeled("PLMWLODSAV_SaveOptions".Translate(), ref _saveOptions,
                "PLMWLODSAV_SaveOptionsToolTip".Translate());

            ListingStandard.GapLine(DefaultGapLineHeight);

            DrawEntryHeader(string.Format("PLMWLODSAV_AuthorName".Translate(), MaxAuthorNameLength));

            _presetAuthorSave = ListingStandard.TextEntry(_presetAuthorSave);
            if (_presetAuthorSave.Length >= MaxAuthorNameLength)
                _presetAuthorSave = _presetAuthorSave.Substring(0, MaxAuthorNameLength);

            ListingStandard.GapLine(DefaultGapLineHeight);

            DrawEntryHeader(string.Format("PLMWLODSAV_DescriptionString".Translate(), MaxDescriptionLength));

            var descriptionRect = ListingStandard.GetRect(80f);
            _presetDescriptionSave = Widgets.TextAreaScrollable(descriptionRect, _presetDescriptionSave,
                ref _scrollPosPresetDescription);
            if (_presetDescriptionSave.Length >= MaxDescriptionLength)
                _presetDescriptionSave = _presetDescriptionSave.Substring(0, MaxDescriptionLength);
        }

        #endregion SAVE_MODE
    }
}