using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using PrepareLanding.Extensions;
using PrepareLanding.Gui.Tab;
using UnityEngine;
using Verse;

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

        private Vector2 _scrollPosPresetInfo;

        private Vector2 _scrollPosPresetDescription;

        private int _fileDisplayIndexStart = 0;

        private int _selectedFileIndex = -1;

        private string _selectedFileName = string.Empty;

        private string _presetDescription = string.Empty;

        private bool _saveOptions;

        private bool _presetExistsFlag;

        private bool _allowOverwriteExistingPreset;

        private readonly GUIStyle _stylePresetInfo;

        public const float MaxDisplayedFiles = 20f;

        public const int MaxDescriptionLength = 300;

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

            var numButtons = 2;
            if (_presetExistsFlag)
                numButtons++;

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

            var savedColor = GUI.color;
            GUI.color = Color.green;
            if (Verse.Widgets.ButtonText(buttonRects[0], $"{verb} Preset"))
            {
                if (LoadSaveMode == LoadSaveMode.Load)
                {
                    _userData.PresetManager.LoadPreset(_selectedFileName, true);
                }
                else if (LoadSaveMode == LoadSaveMode.Save)
                {
                    if (PresetManager.PresetExists(_selectedFileName) && !_allowOverwriteExistingPreset)
                        _presetExistsFlag = true;
                    else
                    {
                        _presetExistsFlag = false;
                        _allowOverwriteExistingPreset = false;
                        _userData.PresetManager.SavePreset(_selectedFileName, _presetDescription, _saveOptions);
                    }

                }
            }
            GUI.color = savedColor;

            if (Verse.Widgets.ButtonText(buttonRects[1], $"Exit {verb}"))
            {
                LoadSaveMode = LoadSaveMode.Unknown;
                _selectedFileIndex = -1;
                _selectedFileName = null;
                PrepareLanding.Instance.MainWindow.TabController.SetPreviousTabAsSelectedTab();
            }

            if (_presetExistsFlag && numButtons == 3)
            {
                savedColor = GUI.color;
                GUI.color = Color.red;
                if (Verse.Widgets.ButtonText(buttonRects[2], "Confirm Overwrite"))
                {
                    _allowOverwriteExistingPreset = true;
                }
                GUI.color = savedColor;
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

            var fileInfos = presetFiles as IList<FileInfo> ?? presetFiles.ToList();
            if (!fileInfos.Any())
            {
                ListingStandard.Label("No existing presets.");
                return;
            }

            var itemsToDisplay = fileInfos.Count;

            // add a gap before the scroll view
            ListingStandard.Gap(DefaultGapLineHeight);

            /*
             * Calculate heights
             */

            // height of the scrollable outer Rect (visible portion of the scroll view, not the 'virtual' one)
            var maxScrollViewOuterHeight = InRect.height - ListingStandard.CurHeight - 30f;

            // height of the 'virtual' portion of the scroll view
            var scrollableViewHeight = itemsToDisplay * DefaultElementHeight + DefaultGapLineHeight * MaxDisplayedFiles;

            /*
             * Scroll view
             */
            var innerLs = ListingStandard.BeginScrollView(maxScrollViewOuterHeight, scrollableViewHeight,
                ref _scrollPosPresetFiles, 16f);

            var endIndex = _fileDisplayIndexStart + itemsToDisplay;
            for (var i = _fileDisplayIndexStart; i < endIndex; i++)
            {
                var selectedFile = fileInfos[i];

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
            DrawEntryHeader("Preset info", backgroundColor: Color.green);

            if (_selectedFileIndex < 0)
                return;

            ListingStandard.TextEntryLabeled("Preset Name:", _selectedFileName);

            var preset = _userData.PresetManager.PresetByPresetName(_selectedFileName);
            if (preset == null)
                return;

            var maxOuterRectHeight = InRect.height - ListingStandard.CurHeight - DefaultElementHeight;
            ListingStandard.ScrollableTextArea(maxOuterRectHeight, preset.PresetInfo, ref _scrollPosPresetInfo, _stylePresetInfo, DefaultScrollableViewShrinkWidth);
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

            DrawEntryHeader($"Description: [optional; {MaxDescriptionLength} chars max]");

            var descriptionRect = ListingStandard.GetRect(80f);
            _presetDescription = Widgets.TextAreaScrollable(descriptionRect, _presetDescription,
                ref _scrollPosPresetDescription);
            if (_presetDescription.Length >= MaxDescriptionLength)
                _presetDescription = _presetDescription.Substring(0, MaxDescriptionLength);

        }

        #endregion SAVE_MODE
    }
}
