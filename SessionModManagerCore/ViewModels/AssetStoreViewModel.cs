﻿using Google.Apis.Download;
using SessionAssetStore;
using SessionMapSwitcherCore.Classes;
using SessionMapSwitcherCore.Utils;
using SessionModManagerCore.Classes;
using SessionModManagerCore.ViewModels;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace SessionMapSwitcherCore.ViewModels
{
    public class AssetStoreViewModel : ViewModelBase
    {
        #region Fields

        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        public const string storeFolderName = "store_data";

        public const string downloadsFolderName = "temp_downloads";

        public const string thumbnailFolderName = "thumbnails";

        private StorageManager _assetManager;
        private string _userMessage;
        private string _installButtonText;
        private string _removeButtonText;
        private string _imageSource;
        private string _selectedDescription;
        private string _selectedAuthor;
        private bool _isInstallButtonEnabled;
        private bool _isRemoveButtonEnabled;
        private bool _isLoadingManifests;
        private bool _isInstallingAsset;
        private List<AssetViewModel> _filteredAssetList;
        private List<AssetViewModel> _allAssets;

        private object filteredListLock = new object();
        private object allListLock = new object();

        private bool _displayAll;
        private bool _displayMaps;
        private bool _displayDecks;
        private bool _displayGriptapes;
        private bool _displayTrucks;
        private bool _displayWheels;
        private bool _displayHats;
        private bool _displayShirts;
        private bool _displayPants;
        private bool _displayShoes;

        #endregion

        #region Properties

        public string AbsolutePathToStoreData
        {
            get
            {
                return Path.Combine(AppContext.BaseDirectory, storeFolderName);
            }
        }

        public string AbsolutePathToThumbnails
        {
            get
            {
                return Path.Combine(AbsolutePathToStoreData, thumbnailFolderName);
            }
        }

        public string AbsolutePathToTempDownloads
        {
            get
            {
                return Path.Combine(AbsolutePathToStoreData, downloadsFolderName);
            }
        }

        public bool DisplayAll
        {
            get { return _displayAll; }
            set
            {
                _displayAll = value;

                // setting the private variables so the list is not refreshed for every category until the end
                _displayMaps = value;
                _displayDecks = value;
                _displayGriptapes = value;
                _displayTrucks = value;
                _displayWheels = value;
                _displayHats = value;
                _displayShirts = value;
                _displayPants = value;
                _displayShoes = value;

                NotifyPropertyChanged();
                NotifyPropertyChanged(nameof(DisplayMaps));
                NotifyPropertyChanged(nameof(DisplayDecks));
                NotifyPropertyChanged(nameof(DisplayGriptapes));
                NotifyPropertyChanged(nameof(DisplayTrucks));
                NotifyPropertyChanged(nameof(DisplayWheels));
                NotifyPropertyChanged(nameof(DisplayHats));
                NotifyPropertyChanged(nameof(DisplayShirts));
                NotifyPropertyChanged(nameof(DisplayPants));
                NotifyPropertyChanged(nameof(DisplayShoes));
                RefreshFilteredAssetList();
            }
        }
        public bool DisplayMaps
        {
            get { return _displayMaps; }
            set
            {
                _displayMaps = value;
                NotifyPropertyChanged();
                RefreshFilteredAssetList();
            }
        }
        public bool DisplayDecks
        {
            get { return _displayDecks; }
            set
            {
                _displayDecks = value;
                NotifyPropertyChanged();
                RefreshFilteredAssetList();
            }
        }
        public bool DisplayGriptapes
        {
            get { return _displayGriptapes; }
            set
            {
                _displayGriptapes = value;
                NotifyPropertyChanged();
                RefreshFilteredAssetList();
            }
        }
        public bool DisplayTrucks
        {
            get { return _displayTrucks; }
            set
            {
                _displayTrucks = value;
                NotifyPropertyChanged();
                RefreshFilteredAssetList();
            }
        }
        public bool DisplayWheels
        {
            get { return _displayWheels; }
            set
            {
                _displayWheels = value;
                NotifyPropertyChanged();
                RefreshFilteredAssetList();
            }
        }
        public bool DisplayHats
        {
            get { return _displayHats; }
            set
            {
                _displayHats = value;
                NotifyPropertyChanged();
                RefreshFilteredAssetList();
            }
        }
        public bool DisplayShirts
        {
            get { return _displayShirts; }
            set
            {
                _displayShirts = value;
                NotifyPropertyChanged();
                RefreshFilteredAssetList();
            }
        }
        public bool DisplayPants
        {
            get { return _displayPants; }
            set
            {
                _displayPants = value;
                NotifyPropertyChanged();
                RefreshFilteredAssetList();
            }
        }
        public bool DisplayShoes
        {
            get { return _displayShoes; }
            set
            {
                _displayShoes = value;
                NotifyPropertyChanged();
                RefreshFilteredAssetList();
            }
        }

        public string UserMessage
        {
            get { return _userMessage; }
            set
            {
                _userMessage = value;
                Logger.Info($"UserMessage = {_userMessage}");
                NotifyPropertyChanged();
            }
        }

        public string InstallButtonText
        {
            get
            {
                if (String.IsNullOrEmpty(_installButtonText))
                    _installButtonText = "Install Asset";
                return _installButtonText;
            }
            set
            {
                _installButtonText = value;
                NotifyPropertyChanged();
            }
        }

        public bool IsInstallButtonEnabled
        {
            get
            {
                if (IsInstallingAsset)
                    return false;

                return _isInstallButtonEnabled;
            }
            set
            {
                _isInstallButtonEnabled = value;
                NotifyPropertyChanged();
            }
        }

        public string RemoveButtonText
        {
            get
            {
                if (String.IsNullOrEmpty(_removeButtonText))
                    _removeButtonText = "Remove Asset";
                return _removeButtonText;
            }
            set
            {
                _removeButtonText = value;
                NotifyPropertyChanged();
            }
        }

        public bool IsRemoveButtonEnabled
        {
            get
            {
                if (IsInstallingAsset)
                    return false;

                return _isRemoveButtonEnabled;
            }
            set
            {
                _isRemoveButtonEnabled = value;
                NotifyPropertyChanged();
            }
        }

        public bool IsInstallingAsset
        {
            get
            {
                return _isInstallingAsset;
            }
            set
            {
                _isInstallingAsset = value;
                NotifyPropertyChanged();
                NotifyPropertyChanged(nameof(IsRemoveButtonEnabled));
                NotifyPropertyChanged(nameof(IsInstallButtonEnabled));
            }
        }

        public string PreviewImageSource
        {
            get { return _imageSource; }
            set
            {
                _imageSource = value;
                NotifyPropertyChanged();
            }
        }

        public string SelectedAuthor
        {
            get { return _selectedAuthor; }
            set
            {
                _selectedAuthor = value;
                NotifyPropertyChanged();
            }
        }

        public string SelectedDescription
        {
            get { return _selectedDescription; }
            set
            {
                _selectedDescription = value;
                NotifyPropertyChanged();
            }
        }

        public bool IsLoadingManifests
        {
            get { return _isLoadingManifests; }
            set
            {
                _isLoadingManifests = value;
                NotifyPropertyChanged();
            }
        }

        public bool IsManifestsDownloaded { get; set; }

        public bool HasAuthenticated { get; set; }

        public StorageManager AssetManager
        {
            get
            {
                if (_assetManager == null)
                    _assetManager = new StorageManager();

                return _assetManager;
            }
        }

        public List<AssetViewModel> AllAssets
        {
            get
            {
                if (_allAssets == null)
                    _allAssets = new List<AssetViewModel>();
                return _allAssets;
            }
            set
            {
                lock (allListLock)
                {
                    _allAssets = value;
                }
            }
        }

        public List<AssetViewModel> FilteredAssetList
        {
            get
            {
                if (_filteredAssetList == null)
                    _filteredAssetList = new List<AssetViewModel>();
                return _filteredAssetList;
            }
            set
            {
                lock (filteredListLock)
                {
                    _filteredAssetList = value;
                }
                NotifyPropertyChanged();
            }
        }

        public AssetViewModel SelectedAsset
        {
            get
            {
                lock (filteredListLock)
                {
                    return FilteredAssetList.Where(a => a.IsSelected).FirstOrDefault();
                }
            }
        }

        #endregion


        public AssetStoreViewModel()
        {
            IsManifestsDownloaded = false;
            IsLoadingManifests = false;
            IsInstallingAsset = false;
            HasAuthenticated = false;
            DisplayMaps = true;
        }


        public void GetManifestsAsync(bool forceRefresh = false, bool getSelectedOnly = false)
        {
            if (forceRefresh == false && IsManifestsDownloaded)
            {
                return; // the manifests will not be re-downloaded because it has been downloaded once and not forced
            }

            IsLoadingManifests = true;
            UserMessage = "Fetching latest asset manifests ...";

            Task t = Task.Factory.StartNew(() =>
            {
                if (HasAuthenticated == false)
                {
                    TryAuthenticate();
                }

                if (getSelectedOnly)
                {
                    GetManifestsForSelectedCategories();
                }
                else
                {
                    AssetManager.GetAllAssetManifests();
                }
            });

            t.ContinueWith((taskResult) =>
            {
                if (taskResult.IsFaulted)
                {
                    UserMessage = "An error occurred fetching manifests ...";
                    Logger.Error(taskResult.Exception, "failed to get all manifests");
                }

                IsManifestsDownloaded = true;
                IsLoadingManifests = false;

                RefreshFilteredAssetList();

                UserMessage = "Manifests downloaded ...";
            });
        }

        public void GetManifestsForSelectedCategories()
        {
            List<AssetCategory> selectedCategories = GetSelectedCategories();

            foreach (AssetCategory cat in selectedCategories)
            {
                AssetManager.GetAssetManifests(cat, new Progress<IDownloadProgress>(p => UserMessage = $"Downloading asset: {p.Status} {p.BytesDownloaded / 1000:0.00} KB..."));
            }
        }

        public void RefreshFilteredAssetList()
        {
            if (IsManifestsDownloaded == false)
            {
                return;
            }

            List<AssetCategory> categories = GetSelectedCategories();
            List<AssetViewModel> newList = new List<AssetViewModel>();

            foreach (AssetCategory cat in categories)
            {
                LoadAssetsFromManifest(cat);
                newList.AddRange(GetAssetsByCategory(cat));
            }

            FilteredAssetList = newList;

            if (FilteredAssetList.Count == 0 && GetSelectedCategories().Count() == 0)
            {
                UserMessage = "Check categories to view the list of downloadable assets ...";
            }
        }

        public void RefreshPreviewForSelected()
        {
            SelectedAuthor = SelectedAsset.Author;
            SelectedDescription = SelectedAsset.Description;

            bool isInstalled = IsSelectedAssetInstalled();

            IsInstallButtonEnabled = !isInstalled;
            IsRemoveButtonEnabled = isInstalled;

            RefreshInstallButtonText();
            GetSelectedPreviewImageAsync();
        }

        private bool IsSelectedAssetInstalled()
        {
            if (SelectedAsset.AssetCategory == AssetCategory.Maps.Value)
            {
                List<MapMetaData> installedMaps = MetaDataManager.GetAllMetaDataForMaps();

                return installedMaps.Any(m => m.AssetName == SelectedAsset.Asset.AssetName);
            }

            return false;
        }

        private IEnumerable<AssetViewModel> GetAssetsByCategory(AssetCategory cat)
        {
            IEnumerable<AssetViewModel> assetsInCategory = new List<AssetViewModel>();

            lock (allListLock)
            {
                assetsInCategory = AllAssets.Where(a => a.AssetCategory == cat.Value);
            }

            return assetsInCategory;
        }

        private List<AssetCategory> GetSelectedCategories()
        {
            List<AssetCategory> selectedCategories = new List<AssetCategory>();

            if (DisplayDecks)
            {
                selectedCategories.Add(AssetCategory.Decks);
            }
            if (DisplayGriptapes)
            {
                selectedCategories.Add(AssetCategory.Griptapes);
            }
            if (DisplayHats)
            {
                selectedCategories.Add(AssetCategory.Hats);
            }
            if (DisplayMaps)
            {
                selectedCategories.Add(AssetCategory.Maps);
            }
            if (DisplayPants)
            {
                selectedCategories.Add(AssetCategory.Pants);
            }
            if (DisplayShirts)
            {
                selectedCategories.Add(AssetCategory.Shirts);
            }
            if (DisplayShoes)
            {
                selectedCategories.Add(AssetCategory.Shoes);
            }
            if (DisplayTrucks)
            {
                selectedCategories.Add(AssetCategory.Trucks);
            }
            if (DisplayWheels)
            {
                selectedCategories.Add(AssetCategory.Wheels);
            }

            return selectedCategories;
        }

        private void LoadAssetsFromManifest(AssetCategory category)
        {
            if (IsAssetsLoaded(category) == false)
            {
                List<Asset> assets = AssetManager.GenerateAssets(category);
                foreach (Asset asset in assets)
                {
                    if (asset == null)
                        continue;

                    lock (allListLock)
                    {
                        AllAssets.Add(new AssetViewModel(asset));
                    }
                }
            }
        }

        private bool IsAssetsLoaded(AssetCategory category)
        {
            bool hasAssets = false;

            lock (allListLock)
            {
                hasAssets = AllAssets.Any(a => a.AssetCategory == category.Value);
            }

            return hasAssets;
        }

        public void TryAuthenticate()
        {
            try
            {
                AssetManager.Authenticate();
                HasAuthenticated = true;
            }
            catch (Exception e)
            {
                UserMessage = $"Failed to authenticate to asset store: {e.Message}";
                Logger.Error(e, "Failed to authenticate to asset store");
            }
        }

        private void GetSelectedPreviewImageAsync()
        {
            Task t = Task.Factory.StartNew(() =>
            {
                CreateRequiredFolders();

                string pathToThumbnail = Path.Combine(AbsolutePathToThumbnails, SelectedAsset.Asset.Thumbnail);

                if (File.Exists(pathToThumbnail) == false)
                {
                    AssetManager.DownloadAssetThumbnail(SelectedAsset.Asset, pathToThumbnail, new Progress<IDownloadProgress>(p => UserMessage = $"fetching preview image: {p.Status} {p.BytesDownloaded / 1000:0.00} KB..."), true);
                }

                PreviewImageSource = new Uri(pathToThumbnail).AbsolutePath;
            });

            t.ContinueWith((taskResult) =>
            {
                if (taskResult.IsFaulted)
                {
                    UserMessage = "Failed to get preview image.";
                    PreviewImageSource = null;
                    Logger.Error(taskResult.Exception);
                }
            });

        }

        public void RefreshInstallButtonText()
        {
            if (SelectedAsset == null)
            {
                InstallButtonText = "Install Asset";
                RemoveButtonText = "Remove Asset";
                return;
            }

            string assetCatName = SelectedAsset.AssetCategory;

            Dictionary<string, string> categoryToText = new Dictionary<string, string>();
            categoryToText.Add(AssetCategory.Decks.Value, "Deck");
            categoryToText.Add(AssetCategory.Griptapes.Value, "Griptape");
            categoryToText.Add(AssetCategory.Hats.Value, "Hat");
            categoryToText.Add(AssetCategory.Maps.Value, "Map");
            categoryToText.Add(AssetCategory.Pants.Value, "Pants");
            categoryToText.Add(AssetCategory.Shirts.Value, "Shirt");
            categoryToText.Add(AssetCategory.Shoes.Value, "Shoes");
            categoryToText.Add(AssetCategory.Trucks.Value, "Trucks");
            categoryToText.Add(AssetCategory.Wheels.Value, "Wheels");

            if (categoryToText.ContainsKey(assetCatName))
            {
                InstallButtonText = $"Install {categoryToText[assetCatName]}";
                RemoveButtonText = $"Remove {categoryToText[assetCatName]}";
            }
            else
            {
                InstallButtonText = "Install Asset";
                RemoveButtonText = "Remove Asset";
            }
        }

        public void DownloadSelectedAssetAsync(bool deleteAfterInstall = true)
        {
            CreateRequiredFolders();

            IsInstallingAsset = true;
            AssetViewModel assetToDownload = SelectedAsset; // get the selected asset currently in-case user selection changes while download occurs
            string pathToDownload = "";

            Task downloadTask = Task.Factory.StartNew(() =>
            {
                pathToDownload = Path.Combine(AbsolutePathToTempDownloads, assetToDownload.Asset.AssetName);

                AssetManager.DownloadAsset(assetToDownload.Asset, pathToDownload, new Progress<IDownloadProgress>(p => UserMessage = $"Downloading asset: {p.Status} {p.BytesDownloaded / 1000000:0.00} MB..."), true);
            });

            downloadTask.ContinueWith((result) =>
            {
                if (result.IsFaulted)
                {
                    UserMessage = $"Failed to install asset ...";
                    Logger.Error(result.Exception);
                    IsInstallingAsset = false;
                    return;
                }

                UserMessage = $"Installing asset: {assetToDownload.Name} ... ";
                Task installTask = Task.Factory.StartNew(() =>
                {
                    InstallDownloadedAsset(assetToDownload, pathToDownload);
                });

                installTask.ContinueWith((installResult) =>
                {
                    if (installResult.IsFaulted)
                    {
                        UserMessage = $"Failed to install asset ...";
                        Logger.Error(result.Exception);
                        IsInstallingAsset = false;
                        return;
                    }

                    // lastly delete downloaded file
                    if (deleteAfterInstall && File.Exists(pathToDownload))
                    {
                        File.Delete(pathToDownload);
                    }

                    IsInstallingAsset = false;
                });
            });
        }

        private void InstallDownloadedAsset(AssetViewModel assetToInstall, string pathToDownload)
        {
            if (assetToInstall.AssetCategory == AssetCategory.Maps.Value)
            {
                // import map
                ComputerImportViewModel importViewModel = new ComputerImportViewModel()
                {
                    IsZipFileImport = true,
                    PathInput = pathToDownload,
                    AssetToImport = assetToInstall.Asset
                };
                Task<BoolWithMessage> importTask = importViewModel.ImportMapAsync();
                importTask.Wait();

                if (importTask.Result.Result)
                {
                    UserMessage = $"Successfully installed {assetToInstall.Name}!";
                }
                else
                {
                    UserMessage = $"Failed to install {assetToInstall.Name}: {importTask.Result.Message}";
                    Logger.Warn($"install failed: {importTask.Result.Message}");
                }
            }
            else
            {
                // replace texture
                TextureReplacerViewModel replacerViewModel = new TextureReplacerViewModel()
                {
                    PathToFile = pathToDownload
                };
                replacerViewModel.MessageChanged += TextureReplacerViewModel_MessageChanged;
                replacerViewModel.ReplaceTextures();
                replacerViewModel.MessageChanged -= TextureReplacerViewModel_MessageChanged;
            }
        }

        private void TextureReplacerViewModel_MessageChanged(string message)
        {
            UserMessage = message;
        }

        public void CreateRequiredFolders()
        {
            Directory.CreateDirectory(AbsolutePathToStoreData);
            Directory.CreateDirectory(AbsolutePathToThumbnails);
            Directory.CreateDirectory(AbsolutePathToTempDownloads);
        }

        public void RemoveSelectedAssetAsync()
        {
            if (SelectedAsset.AssetCategory == AssetCategory.Maps.Value)
            {
                MapMetaData mapToDelete = MetaDataManager.GetAllMetaDataForMaps()?.Where(m => m.AssetName == SelectedAsset.Asset.AssetName).FirstOrDefault();

                if (mapToDelete == null)
                {
                    UserMessage = "Failed to find meta data to delete map files ...";
                    return;
                }

                BoolWithMessage deleteResult = FileUtils.DeleteMapFiles(mapToDelete);

                UserMessage = deleteResult.Message;

                if (deleteResult.Result)
                {
                    RefreshPreviewForSelected();
                }
            }
        }

    }
}
