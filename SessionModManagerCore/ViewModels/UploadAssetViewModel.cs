﻿using Google.Apis.Upload;
using Newtonsoft.Json;
using SessionAssetStore;
using SessionMapSwitcherCore.Utils;
using SessionMapSwitcherCore.ViewModels;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace SessionModManagerCore.ViewModels
{
    public class UploadAssetViewModel : ViewModelBase
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        public const string DefaultStatusMesssage = "Enter the required info and click 'Upload Asset' to upload your asset to the Asset Store";

        private string _name;
        private string _author;
        private string _description;
        private string _pathToFile;
        private string _pathToThumbnail;
        private string _selectedCategory;
        private string _statusMessage;
        private bool _isUploadingAsset;
        private List<string> _avaialbleCategories;

        private StorageManager _assetManager;

        public bool HasAuthenticated { get; set; }

        public string PathToCredentialsJson { get; set; }

        public string Name
        {
            get { return _name; }
            set
            {
                _name = value;
                NotifyPropertyChanged();
            }
        }
        public string Author
        {
            get { return _author; }
            set
            {
                _author = value;
                NotifyPropertyChanged();
            }
        }
        public string Description
        {
            get { return _description; }
            set
            {
                _description = value;
                NotifyPropertyChanged();
            }
        }

        public string PathToFile
        {
            get { return _pathToFile; }
            set
            {
                _pathToFile = value;
                NotifyPropertyChanged();
            }
        }
        public string PathToThumbnail
        {
            get { return _pathToThumbnail; }
            set
            {
                _pathToThumbnail = value;
                NotifyPropertyChanged();
            }
        }
        public string SelectedCategory
        {
            get { return _selectedCategory; }
            set
            {
                _selectedCategory = value;
                NotifyPropertyChanged();
            }
        }
        public string StatusMessage
        {
            get { return _statusMessage; }
            set
            {
                _statusMessage = value;
                NotifyPropertyChanged();
            }
        }
        public bool IsUploadingAsset
        {
            get { return _isUploadingAsset; }
            set
            {
                _isUploadingAsset = value;
                NotifyPropertyChanged();
                NotifyPropertyChanged(nameof(IsNotUploadingAsset));
            }
        }

        public bool IsNotUploadingAsset
        {
            get { return !_isUploadingAsset; }
        }

        public List<string> AvailableCategories
        {
            get { return _avaialbleCategories; }
            set
            {
                _avaialbleCategories = value;
                NotifyPropertyChanged();
            }
        }

        public StorageManager AssetManager
        {
            get
            {
                if (_assetManager == null)
                    _assetManager = new StorageManager();

                return _assetManager;
            }
        }

        public UploadAssetViewModel()
        {
            IsUploadingAsset = false;
            HasAuthenticated = false;
            SelectedCategory = "Maps";
            StatusMessage = DefaultStatusMesssage;
            PathToCredentialsJson = AppSettingsUtil.GetAppSetting(SettingKey.PathToCredentialsJson);
            Author = AppSettingsUtil.GetAppSetting(SettingKey.UploaderAuthor);
            InitAvailableCategories();
        }

        private void InitAvailableCategories()
        {
            List<string> categories = new List<string>()
            {
                "Maps",
                "Decks",
                "Griptapes",
                "Trucks",
                "Wheels",
                "Hats",
                "Pants",
                "Shirts",
                "Shoes"
            };

            AvailableCategories = categories;
        }

        public void SetPathToCredentialsJson(string fileName)
        {
            PathToCredentialsJson = fileName;
            AppSettingsUtil.AddOrUpdateAppSettings(SettingKey.PathToCredentialsJson, PathToCredentialsJson);
        }

        public AssetCategory GetAssetCategoryBasedOnSelectedCategory()
        {
            switch (SelectedCategory)
            {
                case "Maps":
                    return AssetCategory.Maps;
                case "Decks":
                    return AssetCategory.Decks;
                case "Griptapes":
                    return AssetCategory.Griptapes;
                case "Trucks":
                    return AssetCategory.Trucks;
                case "Wheels":
                    return AssetCategory.Wheels;
                case "Hats":
                    return AssetCategory.Hats;
                case "Pants":
                    return AssetCategory.Pants;
                case "Shirts":
                    return AssetCategory.Shirts;
                case "Shoes":
                    return AssetCategory.Shoes;
                default:
                    return null;
            }
        }

        public void UploadAsset()
        {
            // Validate fields before uploading

            if (File.Exists(PathToFile) == false)
            {
                StatusMessage = $"File does not exist at {PathToFile}.";
                return;
            }

            if (File.Exists(PathToThumbnail) == false)
            {
                StatusMessage = $"Thumbnail does not exist at {PathToThumbnail}.";
                return;
            }

            if (String.IsNullOrEmpty(SelectedCategory))
            {
                StatusMessage = "Please select an Asset Category first.";
                return;
            }

            if (String.IsNullOrEmpty(Name))
            {
                StatusMessage = "Please provide a Name for the asset.";
                return;
            }

            if (String.IsNullOrEmpty(Author))
            {
                StatusMessage = "Please provide an Author for the asset.";
                return;
            }

            IsUploadingAsset = true;

            AppSettingsUtil.AddOrUpdateAppSettings(SettingKey.UploaderAuthor, Author); // store author in app settings so it is remembered for future uploads

            Task uploadTask = Task.Factory.StartNew(() =>
            {
                if (String.IsNullOrEmpty(Description))
                {
                    // if description is not given then use name as description
                    Description = Name;
                }

                string fileName = Path.GetFileName(PathToFile);
                string thumbnailName = Path.GetFileName(PathToThumbnail);
                AssetCategory category = GetAssetCategoryBasedOnSelectedCategory();

                Asset assetToUpload = new Asset(Name, Description, Author, fileName, thumbnailName, category.Value);

                // save asset .json to disk to upload
                string pathToTempJson = Path.Combine(AssetStoreViewModel.AbsolutePathToTempDownloads, $"{Path.GetFileNameWithoutExtension(PathToFile)}.json");
                string jsonToSave = JsonConvert.SerializeObject(assetToUpload, Formatting.Indented);

                File.WriteAllText(pathToTempJson, jsonToSave);

                AssetManager.UploadAsset(pathToTempJson, PathToThumbnail, PathToFile, new IProgress<IUploadProgress>[] {
                new Progress<IUploadProgress>(p => StatusMessage = $"Uploading manifest: {p.Status} {p.BytesSent} Bytes ..."),
                new Progress<IUploadProgress>(p => StatusMessage = $"Uploading thumbnail: {p.Status} {p.BytesSent / 1000:0.00} KB ..."),
                new Progress<IUploadProgress>(p => StatusMessage = $"Uploading file: {p.Status} {p.BytesSent / 1000000:0.00} MB ...")
                });

                File.Delete(pathToTempJson); // delete temp manifest after completion
            });

            uploadTask.ContinueWith((uploadResult) =>
            {
                IsUploadingAsset = false;

                if (uploadResult.IsFaulted)
                {
                    StatusMessage = $"An error occurred while uploading the files: {uploadResult.Exception.InnerException?.Message}";
                    Logger.Error(uploadResult.Exception, "failed to upload asset");
                    return;
                }

                StatusMessage = $"The asset {Name} has been uploaded successfully! You can close this window or leave it open to upload another asset.";
            });

            
        }

        public void BrowseForFile()
        {
            throw new NotImplementedException();
        }

        public void BrowseForThumbnailFile()
        {
            throw new NotImplementedException();
        }

        public void TryAuthenticate()
        {
            if (File.Exists(PathToCredentialsJson) == false)
            {
                StatusMessage = $"Failed to authenticate to asset store: {PathToCredentialsJson} does not exist.";
                return;
            }

            try
            {
                Task t = AssetManager.Authenticate(PathToCredentialsJson);
                t.Wait();
                HasAuthenticated = true;
            }
            catch (AggregateException e)
            {
                StatusMessage = $"Failed to authenticate to asset store: {e.InnerException?.Message}";
                Logger.Error(e, "Failed to authenticate to asset store");
            }
            catch (Exception e)
            {
                StatusMessage = $"Failed to authenticate to asset store: {e.Message}";
                Logger.Error(e, "Failed to authenticate to asset store");
            }
        }
    }
}
