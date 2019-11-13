﻿using Newtonsoft.Json;
using SessionMapSwitcherCore.Utils;
using SessionModManagerCore.Classes;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SessionMapSwitcherCore.Classes
{
    public class MetaDataManager
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        public const string MetaFolderName = "MapSwitcherMetaData";

        public const string InstalledTextureFileName = "installed_textures.json";

        public static string FullPathToMetaFolder
        {
            get
            {
                return Path.Combine(SessionPath.ToContent, MetaFolderName);
            }
        }

        public static string PathToInstalledTextureFile
        {
            get
            {
                return Path.Combine(FullPathToMetaFolder, InstalledTextureFileName);
            }
        }


        internal static string GetOriginalImportLocation(MapListItem map)
        {
            MapMetaData metaData = LoadMapMetaData(map);

            if (metaData == null)
            {
                return "";
            }

            return metaData.OriginalImportPath;
        }

        public static bool IsImportLocationStored(MapListItem map)
        {
            return String.IsNullOrEmpty(GetOriginalImportLocation(map)) == false;
        }


        /// <summary>
        /// Loops over maps and updates meta .json files with new custom properties
        /// </summary>
        /// <returns> true if files updated; false if exception thrown </returns>
        public static bool WriteCustomMapPropertiesToFile(IEnumerable<MapListItem> maps)
        {
            try
            {
                CreateMetaDataFolder();

                foreach (MapListItem map in maps)
                {
                    WriteCustomMapPropertiesToFile(map);
                }
            }
            catch (Exception e)
            {
                Logger.Error(e, "failed to write custom map properties to file");
                return false;
            }

            return true;
        }

        /// <summary>
        /// Loops over each map and gets the custom properties from their respective meta .json file
        /// </summary>
        internal static void SetCustomPropertiesForMaps(IEnumerable<MapListItem> maps, bool createIfNotExists = false)
        {
            try
            {
                foreach (MapListItem map in maps)
                {
                    SetCustomPropertiesForMap(map, createIfNotExists);
                }
            }
            catch (Exception e)
            {
                Logger.Error(e, "Failed to set custom map properties");
            }
        }

        /// <summary>
        /// Gets the meta .json for the map if it exists and updates
        /// the custom name and IsHiddenByUser property
        /// </summary>
        public static void SetCustomPropertiesForMap(MapListItem map, bool createIfNotExists = false)
        {
            MapMetaData savedMetaData = LoadMapMetaData(map);

            if (savedMetaData == null)
            {
                if (createIfNotExists)
                {
                    savedMetaData = CreateMapMetaData(map);
                    SaveMapMetaData(savedMetaData);
                }
                else
                {
                    return;
                }
            }

            map.IsHiddenByUser = savedMetaData.IsHiddenByUser;
            map.CustomName = savedMetaData.CustomName;
        }

        /// <summary>
        /// Updates the meta .json files for the maps with the new
        /// custom names and if it is hidden.
        /// </summary>
        public static void WriteCustomMapPropertiesToFile(MapListItem map)
        {
            MapMetaData metaDataToSave = LoadMapMetaData(map);

            if (metaDataToSave == null)
            {
                return;
            }

            metaDataToSave.IsHiddenByUser = map.IsHiddenByUser;
            metaDataToSave.CustomName = map.CustomName;

            SaveMapMetaData(metaDataToSave);
        }


        /// <summary>
        /// Creates folder to Meta data folder if it does not exists
        /// </summary>
        internal static void CreateMetaDataFolder()
        {
            if (Directory.Exists(FullPathToMetaFolder) == false)
            {
                Directory.CreateDirectory(FullPathToMetaFolder);
            }
        }

        public static MapMetaData CreateMapMetaData(string sourceMapFolder, bool findMapFiles)
        {
            MapMetaData metaData = new MapMetaData();
            metaData.IsHiddenByUser = false;
            metaData.CustomName = "";

            MapListItem validMap = GetFirstMapInFolder(sourceMapFolder, isValid: true);

            if (validMap == null)
            {
                return null;
            }

            metaData.MapName = validMap.MapName;
            metaData.MapFileDirectory = ReplaceSourceMapPathWithPathToContent(sourceMapFolder, validMap.DirectoryPath);

            if (findMapFiles)
            {
                metaData.FilePaths = FileUtils.GetAllFilesInDirectory(sourceMapFolder);

                // modify file paths to match the target folder Session "Content" folder
                for (int i = 0; i < metaData.FilePaths.Count; i++)
                {
                    metaData.FilePaths[i] = ReplaceSourceMapPathWithPathToContent(sourceMapFolder, metaData.FilePaths[i]);
                }
            }
            else
            {
                metaData.FilePaths = new List<string>();
            }

            return metaData;
        }

        public static MapMetaData CreateMapMetaData(MapListItem map)
        {
            MapMetaData metaData = new MapMetaData();

            metaData.MapName = map.MapName;
            metaData.MapFileDirectory = map.DirectoryPath;
            metaData.FilePaths = new List<string>();
            metaData.IsHiddenByUser = map.IsHiddenByUser;
            metaData.CustomName = map.CustomName;

            return metaData;
        }

        /// <summary>
        /// Returns a new absolute file path replacing <paramref name="sourceMapFolder"/> with <see cref="SessionPath.ToContent"/>
        /// </summary>
        /// <param name="sourceMapFolder"> path to replace with <see cref="SessionPath.ToContent"/>. </param>
        /// <param name="absoluteFilePath"> path to file that is in the <paramref name="sourceMapFolder"/>. </param>
        private static string ReplaceSourceMapPathWithPathToContent(string sourceMapFolder, string absoluteFilePath)
        {
            if (absoluteFilePath.IndexOf(sourceMapFolder) < 0)
            {
                return "";
            }

            int startIndex = sourceMapFolder.Length + absoluteFilePath.IndexOf(sourceMapFolder);

            if (startIndex >= absoluteFilePath.Length)
            {
                return SessionPath.ToContent;
            }

            string relativePath = absoluteFilePath.Substring(startIndex + 1);
            return Path.Combine(SessionPath.ToContent, relativePath);
        }

        public static MapListItem GetFirstMapInFolder(string sourceMapFolder, bool isValid)
        {
            foreach (string file in Directory.GetFiles(sourceMapFolder, "*.umap"))
            {
                FileInfo fileInfo = new FileInfo(file);

                MapListItem map = new MapListItem()
                {
                    FullPath = file,
                    MapName = fileInfo.NameWithoutExtension()
                };
                map.Validate();

                if (map.IsValid == isValid)
                {
                    return map;
                }
            }

            foreach (string dir in Directory.GetDirectories(sourceMapFolder))
            {
                MapListItem foundMap = GetFirstMapInFolder(dir, isValid);

                if (foundMap != null)
                {
                    return foundMap;
                }
            }

            return null;
        }

        internal static bool DoesValidMapExistInFolder(string sourceFolder)
        {
            return GetFirstMapInFolder(sourceFolder, isValid: true) != null;
        }

        public static MapMetaData LoadMapMetaData(MapListItem mapItem)
        {
            try
            {
                CreateMetaDataFolder();

                DirectoryInfo dirInfo = new DirectoryInfo(mapItem.DirectoryPath);

                string fileName = $"{dirInfo.Name}_{mapItem.MapName}_meta.json";
                string pathToFile = Path.Combine(FullPathToMetaFolder, fileName);

                string fileContents = File.ReadAllText(pathToFile);

                return JsonConvert.DeserializeObject<MapMetaData>(fileContents);
            }
            catch (Exception e)
            {
                Logger.Error(e, "failed to load map meta data");
                return null;
            }
        }

        public static MapMetaData LoadMapMetaData(string pathToJson)
        {
            try
            {
                string fileContents = File.ReadAllText(pathToJson);
                return JsonConvert.DeserializeObject<MapMetaData>(fileContents);
            }
            catch (Exception e)
            {
                Logger.Error(e, "failed to load map meta data");
                return null;
            }
        }

        public static void SaveMapMetaData(MapMetaData metaData)
        {
            if (metaData == null)
            {
                return;
            }

            try
            {
                CreateMetaDataFolder();

                string fileName = metaData.GetJsonFileName();
                string pathToFile = Path.Combine(FullPathToMetaFolder, fileName);


                string jsonToSave = JsonConvert.SerializeObject(metaData);

                File.WriteAllText(pathToFile, jsonToSave);
            }
            catch (Exception e)
            {
                Logger.Error(e, "failed to save map meta data");
                return;
            }
        }

        internal static void SaveTextureMetaData(TextureMetaData metaData)
        {
            if (metaData == null)
            {
                return;
            }

            try
            {
                CreateMetaDataFolder();

                InstalledTexturesMetaData currentlyInstalled = LoadTextureMetaData();
                currentlyInstalled.Replace(metaData);

                SaveTextureMetaData(currentlyInstalled);
            }
            catch (Exception e)
            {
                Logger.Error(e, "failed to save texture meta data");
                return;
            }
        }

        internal static void SaveTextureMetaData(InstalledTexturesMetaData metaData)
        {
            if (metaData == null)
            {
                return;
            }

            try
            {
                CreateMetaDataFolder();

                string jsonToSave = JsonConvert.SerializeObject(metaData);

                File.WriteAllText(PathToInstalledTextureFile, jsonToSave);
            }
            catch (Exception e)
            {
                Logger.Error(e, "failed to save texture meta data");
                return;
            }
        }

        public static InstalledTexturesMetaData LoadTextureMetaData()
        {
            try
            {
                if (File.Exists(PathToInstalledTextureFile) == false)
                {
                    return new InstalledTexturesMetaData();
                }

                string fileContents = File.ReadAllText(PathToInstalledTextureFile);
                return JsonConvert.DeserializeObject<InstalledTexturesMetaData>(fileContents);
            }
            catch (Exception e)
            {
                Logger.Error(e, "failed to load map meta data");
                return null;
            }
        }

        public static bool HasPathToMapFilesStored(MapListItem map)
        {
            MapMetaData metaData = LoadMapMetaData(map);

            if (metaData != null)
            {
                return metaData.FilePaths?.Count > 0;
            }

            return false;
        }

        public static List<MapMetaData> GetAllMetaDataForMaps()
        {
            List<MapMetaData> maps = new List<MapMetaData>();
            CreateMetaDataFolder();

            foreach (string file in Directory.GetFiles(FullPathToMetaFolder, "*_meta.json"))
            {
                MapMetaData foundMetaData = LoadMapMetaData(file);

                if (foundMetaData != null)
                {
                    maps.Add(foundMetaData);
                }
            }

            return maps;
        }

        public static BoolWithMessage DeleteMapFiles(MapMetaData metaData)
        {

            if (metaData == null)
            {
                return BoolWithMessage.False($"meta data is null");
            }

            if (metaData.FilePaths?.Count == 0)
            {
                return BoolWithMessage.False($"List of files to delete is unknown for {metaData.MapName}. You must manually delete the map files from the following folder: {metaData.MapFileDirectory}");
            }

            try
            {
                BoolWithMessage result = FileUtils.DeleteFiles(metaData.FilePaths);

                if (result.Result == false)
                {
                    return result;
                }

                // lastly delete meta data file
                string pathToMetaData = Path.Combine(MetaDataManager.FullPathToMetaFolder, metaData.GetJsonFileName());
                if (File.Exists(pathToMetaData))
                {
                    File.Delete(pathToMetaData);
                }

                return BoolWithMessage.True($"{metaData.MapName} has been deleted!");
            }
            catch (Exception e)
            {
                return BoolWithMessage.False($"Failed to delete files: {e.Message}");
            }

        }

        public static BoolWithMessage DeleteTextureFiles(TextureMetaData metaData)
        {
            if (metaData == null)
            {
                return BoolWithMessage.False($"meta data is null");
            }

            if (metaData.FilePaths?.Count == 0)
            {
                return BoolWithMessage.False($"List of files to delete is unknown for {metaData.Name}.");
            }

            try
            {
                BoolWithMessage result = FileUtils.DeleteFiles(metaData.FilePaths);

                if (result.Result == false)
                {
                    return result;
                }

                // lastly delete entry from list of installed textures
                InstalledTexturesMetaData currentlyInstalledTextures = MetaDataManager.LoadTextureMetaData();
                currentlyInstalledTextures.Remove(metaData);
                MetaDataManager.SaveTextureMetaData(currentlyInstalledTextures);

                return BoolWithMessage.True($"{metaData.Name} has been deleted!");
            }
            catch (Exception e)
            {
                return BoolWithMessage.False($"Failed to delete files: {e.Message}");
            }
        }

        internal static TextureMetaData GetTextureMetaDataByName(string assetName)
        {
            InstalledTexturesMetaData installedTextures = LoadTextureMetaData();
            return installedTextures.InstalledTextures.Where(t => t.AssetName == assetName).FirstOrDefault();
        }
    }
}
