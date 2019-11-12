﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace SessionModManagerCore.Classes
{
    public class MapMetaData
    {
        public string MapName { get; set; }
        
        public string CustomName { get; set; }

        public bool IsHiddenByUser { get; set; }

        /// <summary>
        /// path to folder that was imported into Content.
        /// Used for Re-import feature
        /// </summary>
        public string OriginalImportPath { get; set; }

        /// <summary>
        /// Path to directory where .umap file exists
        /// </summary>
        public string MapFileDirectory { get; set; }

        /// <summary>
        /// List of absolute paths to files that were imported for the map
        /// </summary>
        public List<string> FilePaths { get; set; }

        /// <summary>
        /// Returns the name of the json file that this meta data is saved as
        /// </summary>
        /// <returns></returns>
        public string GetJsonFileName()
        {
            DirectoryInfo dirInfo = new DirectoryInfo(MapFileDirectory);
            return $"{dirInfo.Name}_{MapName}_meta.json";
        }
    }
}