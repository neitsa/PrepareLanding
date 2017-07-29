using System;
using System.IO;
using PrepareLanding.Extensions;
using PrepareLanding.Gui.Tab;
using UnityEngine;
using Verse;
using Verse.Steam;

namespace PrepareLanding
{
    public enum LoadSaveMode
    {
        Unknown = 0,
        Load = 1,
        Save = 2,
    }

    public class TabGuiUtilityLoadSave : TabGuiUtility
    {
        private readonly Vector2 _bottomButtonSize = new Vector2(130f, 30f);

        private Vector2 _scrollPosPresetFiles;

        private Vector2 _scrollPosPresetFilterInfo;

        private Vector2 _scrollPosPresetOptionInfo;

        private Vector2 _scrollPosPresetDescription;

        private Vector2 _scrollPosPresetLoadDescription;

        private int _fileDisplayIndexStart = 0;

        private int _selectedFileIndex = -1;

        private string _selectedFileName = string.Empty;

        private string _presetDescriptionSave = string.Empty;

        private string _presetAuthorSave = string.Empty;

        private bool _saveOptions;

        private bool _allowOverwriteExistingPreset;

        private readonly GUIStyle _stylePresetInfo;

        public const float MaxDisplayedFiles = 20f;

        public const int MaxDescriptionLength = 300;

        public const int MaxAuthorNameLength = 50;

        public LoadSaveMode LoadSaveMode { get; set; }

        private readonly PrepareLandingUserData _userData;

        public TabGuiUtilityLoadSave(PrepareLandingUserData userData, float columnSizePercent = 0.25f) :
            base(columnSizePercent)
        {
            _userData = userData;
            LoadSaveMode = LoadSaveMode.Unknown;

            _stylePresetInfo = new GUIStyle(Text.textFieldStyles[1])
            {
                alignment = TextAnchor.UpperLeft,
                wordWrap = true,
                richText = true
            };

            // setup default name
            if (SteamManager.Initialized)
                _presetAuthorSave = SteamUtility.SteamPersonaName;
            // TODO check if possible to get logged in user if non steam rimworld
        }

        /// <summary>A unique identifier for the Tab.</summary>
        public override string Id => "LoadSave";

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

        /// <summary>Gets whether the tab can be draw or not.</summary>
        public override bool CanBeDrawn
        {
            get { return LoadSaveMode != LoadSaveMode.Unknown; }
            set { }
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
            const int numButtons = 2;

            var buttonRects = inRect.SpaceEvenlyFromCenter(buttonsY, numButtons, _bottomButtonSize.x, _bottomButtonSize.y, 20f);
            if (buttonRects.Count != numButtons)
            {
                return;
            }

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

            var presetExistsNoOverwrite = false;
            if (!string.IsNullOrEmpty(_selectedFileName))
                presetExistsNoOverwrite = PresetManager.PresetExists(_selectedFileName) && !_allowOverwriteExistingPreset;

            var savedColor = GUI.color;
            if (LoadSaveMode == LoadSaveMode.Save)
                GUI.color = presetExistsNoOverwrite ? Color.red : Color.green;
            else
                GUI.color = Color.green;

            if (Verse.Widgets.ButtonText(buttonRects[0], $"{verb} Preset"))
            {
                if (LoadSaveMode == LoadSaveMode.Load)
                {
                    if(!string.IsNullOrEmpty(_selectedFileName))
                    { 
                        if(_userData.PresetManager.LoadPreset(_selectedFileName, true))
                            Messages.Message("Successfuly loaded the preset!", MessageSound.Benefit);
                        else
                            Messages.Message("Error: couldn't load the preset...", MessageSound.Negative);
                    }
                    else
                        Messages.Message("Pick a preset first.", MessageSound.Negative);
                }
                else if (LoadSaveMode == LoadSaveMode.Save)
                {
                    if (presetExistsNoOverwrite)
                    {
                        _allowOverwriteExistingPreset = true;
                        Messages.Message($"Click again on the \"{verb}\" button to confirm the overwrite of the existing preset.", MessageSound.Standard);
                    }
                    else
                    {
                        _allowOverwriteExistingPreset = false;
                        if(_userData.PresetManager.SavePreset(_selectedFileName, _presetDescriptionSave, _presetAuthorSave, _saveOptions))
                            Messages.Message("Successfuly saved the preset!", MessageSound.Benefit);
                        else
                            Messages.Message("Error: couldn't save the preset...", MessageSound.Negative);
                    }
                }
            }
            GUI.color = savedColor;

            if (Verse.Widgets.ButtonText(buttonRects[1], $"Exit {verb}"))
            {
                LoadSaveMode = LoadSaveMode.Unknown;
                _allowOverwriteExistingPreset = false;
                _selectedFileIndex = -1;
                _selectedFileName = null;
                PrepareLanding.Instance.MainWindow.TabController.SetPreviousTabAsSelectedTab();
            }
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

        protected void DrawLoadPresetList(Rect inRect)
        {
            DrawEntryHeader("Preset Files: Load mode", backgroundColor:Color.green);

            var presetFiles = _userData.PresetManager.AllPresetFiles;
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
             * Calculate heights
             */

            // height of the scrollable outer Rect (visible portion of the scroll view, not the 'virtual' one)
            var maxScrollViewOuterHeight = InRect.height - ListingStandard.CurHeight - 30f;

            // height of the 'virtual' portion of the scroll view
            var scrollableViewHeight = presetFilesCount * DefaultElementHeight + DefaultGapLineHeight * MaxDisplayedFiles;

            /*
             * Scroll view
             */
            var innerLs = ListingStandard.BeginScrollView(maxScrollViewOuterHeight, scrollableViewHeight,
                ref _scrollPosPresetFiles, 16f);

            var endIndex = _fileDisplayIndexStart + presetFilesCount;
            for (var i = _fileDisplayIndexStart; i < endIndex; i++)
            {
                var selectedFile = presetFiles[i];

                // get file name
                var labelText = Path.GetFileNameWithoutExtension(selectedFile.Name);

                // display the label
                var labelRect = innerLs.GetRect(DefaultElementHeight);
                var selected = i == _selectedFileIndex;
                if (Gui.Widgets.LabelSelectable(labelRect, labelText, ref selected, TextAnchor.MiddleCenter))
                {
                    // go to the location of the selected tile
                    _selectedFileIndex = i;
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

            if (_selectedFileIndex < 0)
                return;

            ListingStandard.TextEntryLabeled2("Preset Name:", _selectedFileName);

            var preset = _userData.PresetManager.PresetByPresetName(_selectedFileName);
            if (preset == null)
                return;

            ListingStandard.TextEntryLabeled2("Author:", preset.PresetInfo.Author);

            ListingStandard.Label("Description:");
            var descriptionRect = ListingStandard.GetRect(80f);
            Widgets.TextAreaScrollable(descriptionRect, preset.PresetInfo.Description, ref _scrollPosPresetLoadDescription);

            ListingStandard.Label("Filters:");
            var maxOuterRectHeight = 130f;
            ListingStandard.ScrollableTextArea(maxOuterRectHeight, preset.PresetInfo.FilterInfo, ref _scrollPosPresetFilterInfo, _stylePresetInfo, DefaultScrollableViewShrinkWidth);

            ListingStandard.Label("Options:");
            ListingStandard.ScrollableTextArea(maxOuterRectHeight, preset.PresetInfo.OptionInfo, ref _scrollPosPresetOptionInfo, _stylePresetInfo, DefaultScrollableViewShrinkWidth);
        }

        #endregion LOAD_MODE

        #region SAVE_MODE

        protected void DrawSaveMode(Rect inRect)
        {
            Begin(inRect);
            DrawSaveFileName(inRect);
            End();
        }

        protected void DrawSaveFileName(Rect inRect)
        {
            DrawEntryHeader("Preset Files: Save mode", backgroundColor: Color.red);

            var fileNameRect = ListingStandard.GetRect(DefaultElementHeight);

            var fileNameLabelRect = fileNameRect.LeftPart(0.2f);
            Widgets.Label(fileNameLabelRect, "FileName:");

            var fileNameTextRect = fileNameRect.RightPart(0.8f);
            if (string.IsNullOrEmpty(_selectedFileName))
                _selectedFileName = _userData.PresetManager.NextPresetFileName;

            _selectedFileName = Widgets.TextField(fileNameTextRect, _selectedFileName);

            ListingStandard.GapLine(DefaultGapLineHeight);

            ListingStandard.CheckboxLabeled("Save Options", ref _saveOptions, "Check to also save options alongside filters.");

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
