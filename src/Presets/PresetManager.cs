using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using RimWorld;
using Verse;

namespace PrepareLanding.Presets
{
    public class PresetManager
    {
        public const string DefaultPresetName = "PLPreset";

        public const string DefaultExtension = ".XML";

        private readonly List<FileInfo> _allPresetFiles = new List<FileInfo>();

        private readonly Dictionary<string, Preset> _presetCache = new Dictionary<string, Preset>();

        private readonly GameData.GameData _gameData;

        public PresetManager(GameData.GameData gameData)
        {
            _gameData = gameData;

            var displayPresetFolder = PresetFolder;
            var displayPresetTemplateFolder = PresetTemplateFolder;

           // redact user name from log string
            var userName = Environment.UserName;
            if (displayPresetFolder.Contains(userName))
            {
                displayPresetFolder = displayPresetFolder.Replace(userName, "<redacted>");
                displayPresetTemplateFolder = displayPresetTemplateFolder.Replace(userName, "<redacted>");
            }

            // just make sure the preset dir exists by calling the PresetFolder Property
            Log.Message($"[PrepareLanding] Preset folder is at: {displayPresetFolder}");
            // location of the preset templates, provided de facto with the mod
            Log.Message($"[PrepareLanding] Preset template folder is at: {displayPresetTemplateFolder}");

            CopyFromTemplateFolderToPresetFolder(PresetTemplateFolder, PresetFolder);

            PreloadPresets();
        }

        public List<FileInfo> AllPresetFiles
        {
            get
            {
                if ((_allPresetFiles.Count == 0) && !IsPresetDirectoryEmpty())
                    RenewPresetFileCache();

                return _allPresetFiles;
            }
        }

        /// <summary>
        ///     Name of the preset folder.
        /// </summary>
        public static string FolderName => PrepareLanding.Instance.ModIdentifier;

        public static string PresetTemplateFolder => Path.Combine(PrepareLanding.Instance.ModFolder, "Presets");

        public bool LoadPreset(string presetName, bool forceReload = false)
        {
            bool successfulLoad;

            if (string.IsNullOrEmpty(presetName))
                return false;

            var filePath = GetPresetFilePath(presetName);

            if (!File.Exists(filePath))
                return false;

            if (_presetCache.ContainsKey(presetName) && !forceReload)
                return false;

            // disable live filtering as we are gonna change some filters on the fly
            var liveFilterting = _gameData.UserData.Options.AllowLiveFiltering;
            _gameData.UserData.Options.AllowLiveFiltering = false;

            // reset all filter states into their default state
            _gameData.UserData.ResetAllFields();

            try
            {
                // reload the preset if it was already in the cache
                if (_presetCache.TryGetValue(presetName, out var preset))
                {
                    preset.LoadPreset();

                    //reload its info
                    preset.LoadPresetInfo();
                }
                else
                {
                    // create the preset and load it
                    preset = new Preset(presetName, _gameData);
                    preset.LoadPreset();
                    preset.LoadPresetInfo();

                    // add it to the cache
                    _presetCache.Add(presetName, preset);
                }

                // renew file cache
                RenewPresetFileCache();

                successfulLoad = true;
            }
            catch (Exception e)
            {
                Messages.Message($"[PrepareLanding] {"PLPRESTMAN_ErrorLoadingPreset".Translate()}", MessageTypeDefOf.RejectInput);
                Log.Error($"Failed to load preset file '{filePath}'. Error:\n\t{e}\n\t{e.Message}");

                successfulLoad = false;
            }
            finally
            {
                // re-enable live filtering.
                _gameData.UserData.Options.AllowLiveFiltering = liveFilterting;
            }

            return successfulLoad;
        }

        public void LoadPresetInfo(string presetName, bool forceReload = false)
        {
            var filePath = GetPresetFilePath(presetName);

            if (!File.Exists(filePath))
                return;

            if (_presetCache.ContainsKey(presetName) && !forceReload)
                return;

            try
            {
                // create the preset or load it if it already exists
                var preset = !_presetCache.ContainsKey(presetName)
                    ? new Preset(presetName, _gameData)
                    : _presetCache[presetName];

                preset.LoadPresetInfo();
            }
            catch (Exception e)
            {
                Messages.Message($"[PrepareLanding] {"PLPRESTMAN_ErrorLoadingPresetInfo".Translate()}", MessageTypeDefOf.RejectInput);
                Log.Error($"[PrepareLanding] LoadPresetInfo error: {e}");
                throw;
            }
        }

        public bool SavePreset(string presetName, string description = null, string author = null,
            bool saveOptions = false)
        {
            bool successfulSave;

            if (string.IsNullOrEmpty(presetName))
                return false;

            var filePath = GetPresetFilePath(presetName);

            // just check we aren't trying to overwrite a template preset
            if (_presetCache.ContainsKey(presetName))
                if (_presetCache[presetName].PresetInfo.IsTemplate)
                {
                    Messages.Message($"[PrepareLanding] {"PLPRESTMAN_NoOverwritePresetTemplate".Translate()}",
                        MessageTypeDefOf.RejectInput);
                    return false;
                }

            try
            {
                // create preset and start save
                var preset = new Preset(presetName, _gameData);
                preset.PresetInfo.Description = description;
                preset.PresetInfo.Author = author;
                preset.SavePreset(description, saveOptions);

                // reload the preset now if it was already in the cache
                if (_presetCache.ContainsKey(presetName))
                {
                    LoadPreset(presetName, true);
                }
                else
                {
                    // it's a new preset: add it to the cache
                    _presetCache.Add(presetName, preset);

                    // load its info
                    preset.LoadPresetInfo();

                    // renew file cache
                    RenewPresetFileCache();
                }

                successfulSave = true;
            }
            catch (Exception e)
            {
                // remove preset in cache on exception
                if (_presetCache.ContainsKey(presetName))
                    _presetCache.Remove(presetName);

                Messages.Message($"[PrepareLanding] {"PLPRESTMAN_ErrorSavingPreset".Translate()}", MessageTypeDefOf.RejectInput);
                Log.Error($"[PrepareLanding] Failed to save preset file '{filePath}'. error:\n\t{e}\n\t{e.Message}");

                successfulSave = false;
            }

            return successfulSave;
        }

        public bool DeletePreset(string presetName)
        {
            if (string.IsNullOrEmpty(presetName))
                return false;

            var filePath = GetPresetFilePath(presetName);

            // just check we aren't trying to delete a template preset
            if (_presetCache.ContainsKey(presetName))
                if (_presetCache[presetName].PresetInfo.IsTemplate)
                {
                    Messages.Message($"[PrepareLanding] {"PLPRESTMAN_NoDeletePresetTemplate".Translate()}",
                        MessageTypeDefOf.RejectInput);
                    return false;
                }
            
            try
            {
                // delete file
                File.Delete(filePath);
            }
            catch (Exception ex)
            {
                Messages.Message($"[PrepareLanding] {"PLPRESTMAN_ErrorDeletingPreset".Translate()}", MessageTypeDefOf.NegativeEvent);
                Log.Error($"[PrepareLanding] Failed to delete preset file '{filePath}'. error:\n\t{ex}\n\t{ex.Message}");
                return false;
            }

            // remove it from the cache
            if(_presetCache.ContainsKey(presetName))
                _presetCache.Remove(presetName);

            // renew the cache
            RenewPresetFileCache();

            return true;
        }

        private static void CopyFromTemplateFolderToPresetFolder(string sourceFolder, string destFolder)
        {
            if (!Directory.Exists(sourceFolder) || !Directory.Exists(destFolder))
                return;

            foreach (var sourceFile in Directory.GetFiles(sourceFolder))
            {
                var sourceFileName = Path.GetFileName(sourceFile);
                if (string.IsNullOrEmpty(sourceFileName))
                    continue;

                var destFilePath = Path.Combine(destFolder, sourceFileName);
                try
                {
                    if (!Md5HashEquals(sourceFile, destFilePath))
                    {
                        File.Delete(destFilePath);
                        File.Copy(sourceFile, destFilePath);
                    }
                }
                catch (Exception e)
                {
                    Log.Error(
                        $"[PrepareLanding] An error occurred in CopyFromTemplateFolderToPresetFolder.\n\t:Source: {sourceFile}\n\tDest:{destFilePath}\n\tError: {e}");
                }
            }
        }

        private void PreloadPresets()
        {
            foreach (var presetFile in AllPresetFiles)
            {
                var presetName = Path.GetFileNameWithoutExtension(presetFile.Name);
                var preset = new Preset(presetName, _gameData);

                _presetCache.Add(presetName, preset);
                LoadPresetInfo(presetName, true);
            }
        }

        #region FILE_DIR_HANDLING

        /// <summary>
        ///     Full path of the preset folder (from user folder, not the mod one).
        /// </summary>
        public static string PresetFolder
        {
            get
            {
                var folderPath = Path.Combine(GenFilePaths.SaveDataFolderPath, FolderName);
                var directoryInfo = new DirectoryInfo(folderPath);
                if (directoryInfo.Exists)
                    return folderPath;

                Log.Message($"[PrepareLanding] Trying to create mod folder at: '{folderPath}'.");
                try
                {
                    directoryInfo.Create();
                    Log.Message($"[PrepareLanding] Successfully created the mod folder at: '{folderPath}'.");
                }
                catch (Exception e)
                {
                    Log.Message(
                        $"[PrepareLanding] An error occurred while trying to create the mod folder at: '{folderPath}'.\n\tThe Error was: {e.Message}");
                    return null;
                }
                return folderPath;
            }
        }

        public bool IsPresetDirectoryEmpty()
        {
            return Directory.GetFiles(PresetFolder).Length == 0;
        }

        private static string GetPresetFilePath(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
                throw new ArgumentException("[PrepareLanding] GetPresetFilePath: filename is null");

            var filePath = Path.Combine(PresetFolder, fileName);

            // file extension checking, just being precautious
            if (!Path.HasExtension(filePath))
            {
                filePath = Path.ChangeExtension(filePath, DefaultExtension);
            }
            else
            {
                var extension = Path.GetExtension(filePath);
                if (string.Compare(extension, DefaultExtension, StringComparison.OrdinalIgnoreCase) != 0)
                    filePath = Path.ChangeExtension(filePath, DefaultExtension);
            }

            return filePath;
        }

        public static string FullPresetPathFromPresetName(string presetName, bool fileMustExists)
        {
            var filePath = GetPresetFilePath(presetName);
            if (fileMustExists)
                return File.Exists(filePath) ? filePath : null;

            return filePath;
        }

        public bool PresetFileExists(string presetName)
        {
            if (string.IsNullOrEmpty(presetName))
                return false;

            return
                AllPresetFiles.Any(
                    file => string.Compare(file.Name, presetName, StringComparison.CurrentCultureIgnoreCase) == 0);
        }

        public Preset PresetByPresetName(string presetName)
        {
            if (string.IsNullOrEmpty(presetName))
                return null;

            _presetCache.TryGetValue(presetName, out var presetValue);
            return presetValue;
        }

        private void RenewPresetFileCache()
        {
            var dirInfo = new DirectoryInfo(PresetFolder);

            _allPresetFiles.Clear();

            var orderedFiles = from file in dirInfo.GetFiles()
                where string.Compare(file.Extension, DefaultExtension, StringComparison.OrdinalIgnoreCase) == 0
                orderby file.LastWriteTime descending
                select file;

            _allPresetFiles.AddRange(orderedFiles);
        }

        public string NextPresetFileName
        {
            get
            {
                var counter = 0;
                string fileName;
                do
                {
                    fileName = $"{DefaultPresetName}_{counter}{DefaultExtension}";
                    counter++;
                } while (PresetFileExists($"{fileName}"));

                return Path.GetFileNameWithoutExtension(fileName);
            }
        }

        public static bool PresetExists(string presetName)
        {
            var filePath = GetPresetFilePath(presetName);

            return File.Exists(filePath);
        }

        public static bool Md5HashEquals(string filePath1, string filePath2)
        {
            if (!File.Exists(filePath1) || !File.Exists(filePath2))
                return false;

            byte[] file1Hash;
            byte[] file2Hash;

            using (var md5 = MD5.Create())
            {
                using (var stream = File.OpenRead(filePath1))
                {
                    file1Hash = md5.ComputeHash(stream);
                }

                using (var stream = File.OpenRead(filePath2))
                {
                    file2Hash = md5.ComputeHash(stream);
                }
            }

            if (file1Hash.Length != file2Hash.Length)
                return false;

            for (var i = 0; i < file1Hash.Length; i++)
                if (file1Hash[i] != file2Hash[i])
                    return false;

            return true;
        }

        #endregion FILE_DIR_HANDLING
    }
}