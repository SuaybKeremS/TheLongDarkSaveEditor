using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using CodexTldSaveEditor.App.Models;
using The_Long_Dark_Save_Editor_2;
using The_Long_Dark_Save_Editor_2.Game_data;
using The_Long_Dark_Save_Editor_2.Helpers;
using The_Long_Dark_Save_Editor_2.Serialization;

namespace CodexTldSaveEditor.App;

public partial class MainWindow : Window, INotifyPropertyChanged
{
    private sealed class WikiMapReference
    {
        public required string DisplayName { get; init; }
    }

    private static readonly Regex SaveFileRegex = new("^(ep[0-9])?(sandbox|challenge|story|checkpoint|autosave|quicksave|relentless)[0-9]*$", RegexOptions.IgnoreCase);
    private static readonly Dictionary<string, string> MapAssets = new(StringComparer.OrdinalIgnoreCase)
    {
        ["CoastalRegion"] = "CoastalHighwaySF.png",
        ["LakeRegion"] = "MysteryLakeSF.png",
        ["WhalingStationRegion"] = "DesolationPointSF.png",
        ["RuralRegion"] = "PleasantValleySF.png",
        ["CrashMountainRegion"] = "TimberwolfMountainSF.png",
        ["MarshRegion"] = "ForlomMuskeg.png",
        ["RavineTransitionZone"] = "RavineSF.png",
        ["HighwayTransitionZone"] = "CrumblingHighwaySF.png",
        ["MountainTownRegion"] = "MountainTownSF.png",
        ["TracksRegion"] = "BrokenRailRoadSF.png",
        ["RiverValleyRegion"] = "HushedRiverValleySF.png",
        ["CanneryRegion"] = "CanneryRegion.png",
        ["AshCanyonRegion"] = "AshCanyonRegion.png",
        ["BlackrockRegion"] = @"Wiki\BlackrockRegion.jpg",
        ["BlackrockTransitionZone"] = @"Wiki\KeepersPass.jpg",
        ["KeepersPassSouth"] = @"Wiki\KeepersPass.jpg",
        ["KeepersPassSouthRegion"] = @"Wiki\KeepersPass.jpg",
        ["KeepersPassNorth"] = @"Wiki\KeepersPass.jpg",
        ["KeepersPassNorthRegion"] = @"Wiki\KeepersPass.jpg",
        ["WindingRiver"] = @"Wiki\WindingRiver.png",
        ["WindingRiverRegion"] = @"Wiki\WindingRiver.png",
        ["FarRangeBranchLine"] = @"Wiki\FarRangeBranchLine.jpg",
        ["AirfieldRegion"] = @"Wiki\AirfieldRegion.png",
        ["ForsakenAirfieldRegion"] = @"Wiki\AirfieldRegion.png",
        ["TransferPassRegion"] = @"Wiki\TransferPassRegion.jpg",
        ["HubRegion"] = @"Wiki\TransferPassRegion.jpg",
        ["ZoneOfContaminationRegion"] = @"Wiki\ZoneOfContaminationRegion.jpg",
        ["MineRegion"] = @"Wiki\ZoneOfContaminationRegion.jpg",
        ["SunderedPassRegion"] = @"Wiki\SunderedPassRegion.jpg",
        ["PassRegion"] = @"Wiki\SunderedPassRegion.jpg",
        ["CinderHillsCoalMine"] = @"Wiki\SunderedPassRegion.jpg",
        ["AbandonedMineNo3"] = @"Wiki\SunderedPassRegion.jpg",
    };
    private static readonly Dictionary<string, WikiMapReference> WikiMapReferences = new(StringComparer.OrdinalIgnoreCase)
    {
        ["CoastalRegion"] = Wiki("Coastal Highway"),
        ["LakeRegion"] = Wiki("Mystery Lake"),
        ["WhalingStationRegion"] = Wiki("Desolation Point"),
        ["RuralRegion"] = Wiki("Pleasant Valley"),
        ["CrashMountainRegion"] = Wiki("Timberwolf Mountain"),
        ["MarshRegion"] = Wiki("Forlorn Muskeg"),
        ["RavineTransitionZone"] = Wiki("The Ravine"),
        ["HighwayTransitionZone"] = Wiki("Crumbling Highway"),
        ["TracksRegion"] = Wiki("Broken Railroad"),
        ["RiverValleyRegion"] = Wiki("Hushed River Valley"),
        ["MountainTownRegion"] = Wiki("Mountain Town"),
        ["CanneryRegion"] = Wiki("Bleak Inlet"),
        ["AshCanyonRegion"] = Wiki("Ash Canyon"),
        ["BlackrockRegion"] = Wiki("Blackrock"),
        ["BlackrockTransitionZone"] = Wiki("Keeper's Pass South"),
        ["KeepersPassSouth"] = Wiki("Keeper's Pass South"),
        ["KeepersPassSouthRegion"] = Wiki("Keeper's Pass South"),
        ["KeepersPassNorth"] = Wiki("Keeper's Pass North"),
        ["KeepersPassNorthRegion"] = Wiki("Keeper's Pass North"),
        ["WindingRiver"] = Wiki("Winding River"),
        ["WindingRiverRegion"] = Wiki("Winding River"),
        ["FarRangeBranchLine"] = Wiki("Far Range Branch Line"),
        ["AirfieldRegion"] = Wiki("Forsaken Airfield"),
        ["ForsakenAirfieldRegion"] = Wiki("Forsaken Airfield"),
        ["TransferPassRegion"] = Wiki("Transfer Pass"),
        ["HubRegion"] = Wiki("Transfer Pass"),
        ["ZoneOfContaminationRegion"] = Wiki("Zone of Contamination"),
        ["MineRegion"] = Wiki("Zone of Contamination"),
        ["SunderedPassRegion"] = Wiki("Sundered Pass"),
        ["PassRegion"] = Wiki("Sundered Pass"),
        ["CinderHillsCoalMine"] = Wiki("Cinder Hills Coal Mine"),
        ["AbandonedMineNo3"] = Wiki("Abandoned Mine No. 3"),
    };

    private SaveFileEntry selectedSaveEntry;
    private GameSave currentSave;
    private Profile currentProfile;
    private InventoryItemSaveData selectedInventoryItem;
    private string selectedKnownItemId;
    private string customPrefabId;
    private string duplicateCountText = "1";
    private string inventorySearchText;
    private CategoryFilterOption selectedInventoryCategory;
    private ICollectionView inventoryItemsView;
    private string rawSlotJson = string.Empty;
    private string rawBootJson = string.Empty;
    private string rawGlobalJson = string.Empty;
    private string rawProfileJson = string.Empty;
    private string appliedRawSlotJson = string.Empty;
    private string appliedRawBootJson = string.Empty;
    private string appliedRawGlobalJson = string.Empty;
    private string appliedRawProfileJson = string.Empty;
    private string overviewNotes = "Choose a save to begin.";
    private string statusMessage = "Waiting for a save selection.";
    private string topActionMessage = string.Empty;
    private string activeProfilePath = "Not loaded";
    private string mapStatusText = "No save loaded.";
    private string mapSourceText = "No map source selected.";
    private bool includeHiddenItemsInBulkAdd;
    private Brush topActionBrush = SuccessFeedbackBrush;
    private int topActionMessageVersion;

    private MapInfo currentMapInfo;
    private Point currentPlayerPosition;
    private string currentMapRegion;
    private bool mapMouseDown;
    private Point mapClickPosition;
    private Point mapLastMousePosition;

    public event PropertyChangedEventHandler PropertyChanged;

    public ObservableCollection<SaveFileEntry> SaveFiles { get; } = new();
    public ObservableCollection<string> KnownItemIds { get; } = new();
    public ObservableCollection<string> FilteredKnownItemIds { get; } = new();
    public ObservableCollection<CategoryFilterOption> InventoryCategories { get; } = new();
    public ObservableCollection<string> UnknownInventoryIds { get; } = new();

    public SaveFileEntry SelectedSaveEntry
    {
        get => selectedSaveEntry;
        set
        {
            if (SetField(ref selectedSaveEntry, value) && value != null)
            {
                LoadSelectedSave(value);
            }
        }
    }

    public GameSave CurrentSave
    {
        get => currentSave;
        set => SetField(ref currentSave, value);
    }

    public Profile CurrentProfile
    {
        get => currentProfile;
        set => SetField(ref currentProfile, value);
    }

    public InventoryItemSaveData SelectedInventoryItem
    {
        get => selectedInventoryItem;
        set
        {
            if (SetField(ref selectedInventoryItem, value))
            {
                RaiseInventoryEditorStateChanged();
            }
        }
    }

    public string SelectedKnownItemId
    {
        get => selectedKnownItemId;
        set => SetField(ref selectedKnownItemId, value);
    }

    public string CustomPrefabId
    {
        get => customPrefabId;
        set => SetField(ref customPrefabId, value);
    }

    public string InventorySearchText
    {
        get => inventorySearchText;
        set
        {
            if (SetField(ref inventorySearchText, value))
            {
                InventoryItemsView?.Refresh();
            }
        }
    }

    public string DuplicateCountText
    {
        get => duplicateCountText;
        set => SetField(ref duplicateCountText, value);
    }

    public CategoryFilterOption SelectedInventoryCategory
    {
        get => selectedInventoryCategory;
        set
        {
            if (SetField(ref selectedInventoryCategory, value))
            {
                InventoryItemsView?.Refresh();
                RefreshKnownItemIds();
            }
        }
    }

    public ICollectionView InventoryItemsView
    {
        get => inventoryItemsView;
        set => SetField(ref inventoryItemsView, value);
    }

    public string RawSlotJson
    {
        get => rawSlotJson;
        set => SetField(ref rawSlotJson, value);
    }

    public string RawBootJson
    {
        get => rawBootJson;
        set => SetField(ref rawBootJson, value);
    }

    public string RawGlobalJson
    {
        get => rawGlobalJson;
        set => SetField(ref rawGlobalJson, value);
    }

    public string RawProfileJson
    {
        get => rawProfileJson;
        set => SetField(ref rawProfileJson, value);
    }

    public string OverviewNotes
    {
        get => overviewNotes;
        set => SetField(ref overviewNotes, value);
    }

    public string StatusMessage
    {
        get => statusMessage;
        set => SetField(ref statusMessage, value);
    }

    public string TopActionMessage
    {
        get => topActionMessage;
        set
        {
            if (SetField(ref topActionMessage, value))
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(TopActionVisibility)));
            }
        }
    }

    public Brush TopActionBrush
    {
        get => topActionBrush;
        set => SetField(ref topActionBrush, value);
    }

    public Visibility TopActionVisibility => string.IsNullOrWhiteSpace(TopActionMessage) ? Visibility.Collapsed : Visibility.Visible;

    public string ActiveProfilePath
    {
        get => activeProfilePath;
        set => SetField(ref activeProfilePath, value);
    }

    public string MapStatusText
    {
        get => mapStatusText;
        set => SetField(ref mapStatusText, value);
    }

    public string MapSourceText
    {
        get => mapSourceText;
        set => SetField(ref mapSourceText, value);
    }

    public bool IncludeHiddenItemsInBulkAdd
    {
        get => includeHiddenItemsInBulkAdd;
        set => SetField(ref includeHiddenItemsInBulkAdd, value);
    }

    public bool CanEditCondition => SelectedInventoryItem?.Gear != null;
    public bool CanEditAmount => SelectedInventoryItem?.Gear?.StackableItem != null;
    public bool CanEditCalories => SelectedInventoryItem?.Gear?.FoodItem != null;
    public bool CanEditLiquidLiters => SelectedInventoryItem?.Gear?.LiquidItem != null;
    public bool CanEditFuelLiters => SelectedInventoryItem?.Gear?.KeroseneLampItem != null;
    public bool CanEditWaterVolume => SelectedInventoryItem?.Gear?.WaterSupply != null;
    public bool CanEditRounds => SelectedInventoryItem?.Gear?.WeaponItem != null;
    public bool CanEditCraftProgress => SelectedInventoryItem?.Gear?.InProgressItem != null;
    public bool CanEditEvolutionTime => SelectedInventoryItem?.Gear?.EvolveItem != null;

    public string SaveRootPath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Hinterland", "TheLongDark");

    private static readonly Brush SuccessFeedbackBrush = CreateFrozenBrush("#A7E3B3");
    private static readonly Brush FailureFeedbackBrush = CreateFrozenBrush("#F1A4A4");

    private static WikiMapReference Wiki(string displayName)
    {
        return new WikiMapReference
        {
            DisplayName = displayName
        };
    }

    private static Brush CreateFrozenBrush(string hexColor)
    {
        var brush = (SolidColorBrush)new BrushConverter().ConvertFromString(hexColor);
        brush.Freeze();
        return brush;
    }

    private static string GetMapDisplayName(string regionKey)
    {
        if (!string.IsNullOrWhiteSpace(regionKey) && WikiMapReferences.TryGetValue(regionKey, out var reference))
        {
            return reference.DisplayName;
        }

        return MapDictionary.GetInGameName(regionKey);
    }

    public MainWindow()
    {
        JsonConvert.DefaultSettings = () => new JsonSerializerSettings
        {
            FloatFormatHandling = FloatFormatHandling.Symbol,
            Converters = new List<JsonConverter> { new The_Long_Dark_Save_Editor_2.Serialization.ByteArrayConverter() }
        };

        DataContext = this;
        InitializeComponent();

        foreach (var key in ItemDictionary.itemInfo.Keys.OrderBy(x => x))
        {
            KnownItemIds.Add(key);
        }

        InventoryCategories.Add(new CategoryFilterOption { Label = "All", Category = null });
        foreach (var category in Enum.GetValues<ItemCategory>())
        {
            InventoryCategories.Add(new CategoryFilterOption { Label = category.ToString(), Category = category });
        }

        SelectedInventoryCategory = InventoryCategories.First();
        RefreshKnownItemIds();
        SelectedKnownItemId = FilteredKnownItemIds.FirstOrDefault() ?? string.Empty;
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            var markerPath = GetMapAssetPath("location-indicator.png");
            if (File.Exists(markerPath))
            {
                PlayerMarker.Source = new BitmapImage(new Uri(markerPath));
            }
        }
        catch
        {
        }

        RefreshSaveList();
    }

    private void RefreshSaveList(string preferredPath = null)
    {
        var discovered = EnumerateSaveFiles().OrderByDescending(x => File.GetLastWriteTime(x.FullPath)).ToList();
        SaveFiles.Clear();
        foreach (var item in discovered)
        {
            SaveFiles.Add(item);
        }

        var targetPath = preferredPath ?? SelectedSaveEntry?.FullPath;
        var target = SaveFiles.FirstOrDefault(x => string.Equals(x.FullPath, targetPath, StringComparison.OrdinalIgnoreCase));
        if (target != null)
        {
            SelectedSaveEntry = target;
        }
        else if (SaveFiles.Count > 0)
        {
            SelectedSaveEntry = SaveFiles[0];
        }
        else
        {
            CurrentSave = null;
            CurrentProfile = null;
            SelectedInventoryItem = null;
            InventoryItemsView = null;
            ActiveProfilePath = "Not found";
            OverviewNotes = "No compatible saves were detected.";
            StatusMessage = "Create or copy a save into the The Long Dark save folder, then refresh.";
            MapStatusText = "No map available.";
            MapSourceText = "No map source selected.";
            MapImage.Source = null;
            PlayerMarker.Visibility = Visibility.Collapsed;
            MapCanvasMessage.Text = "No save loaded";
        }
    }

    private IEnumerable<SaveFileEntry> EnumerateSaveFiles()
    {
        var folders = new[]
        {
            SaveRootPath,
            Path.Combine(SaveRootPath, "Survival"),
        }.Where(Directory.Exists).Distinct(StringComparer.OrdinalIgnoreCase);

        foreach (var folder in folders)
        {
            foreach (var file in Directory.GetFiles(folder))
            {
                if (!SaveFileRegex.IsMatch(Path.GetFileName(file)))
                {
                    continue;
                }

                SaveFileEntry entry;
                try
                {
                    var json = EncryptString.Decompress(File.ReadAllBytes(file));
                    var obj = JObject.Parse(json);
                    entry = new SaveFileEntry
                    {
                        DisplayName = (string)obj["m_DisplayName"] ?? Path.GetFileNameWithoutExtension(file),
                        FileName = Path.GetFileName(file),
                        FullPath = file,
                        DirectoryPath = folder,
                        Timestamp = (string)obj["m_Timestamp"] ?? File.GetLastWriteTime(file).ToString("yyyy-MM-dd HH:mm:ss"),
                        GameMode = (string)obj["m_GameMode"] ?? "UNKNOWN",
                        Kind = DetermineKind(file, folder),
                        InternalName = (string)obj["m_InternalName"] ?? (string)obj["m_Name"] ?? Path.GetFileNameWithoutExtension(file),
                        Version = (int?)obj["m_Version"] ?? 0,
                        Changelist = (int?)obj["m_Changelist"] ?? 0,
                        GameId = (int?)obj["m_GameId"] ?? 0,
                        SectionCount = obj["m_Dict"] is JObject dict ? dict.Properties().Count() : 0
                    };
                }
                catch
                {
                    continue;
                }

                yield return entry;
            }
        }
    }

    private string DetermineKind(string file, string folder)
    {
        if (string.Equals(Path.GetFileName(folder), "Survival", StringComparison.OrdinalIgnoreCase))
        {
            return "Survival";
        }

        var name = Path.GetFileName(file).ToLowerInvariant();
        if (name.StartsWith("story"))
        {
            return "Wintermute";
        }

        if (name.StartsWith("checkpoint"))
        {
            return "Checkpoint";
        }

        return "General";
    }

    private void LoadSelectedSave(SaveFileEntry entry)
    {
        try
        {
            var save = new GameSave();
            save.LoadSave(entry.FullPath);
            CurrentSave = save;

            ActiveProfilePath = FindProfilePathForSave(entry) ?? "Not found";
            CurrentProfile = ActiveProfilePath == "Not found" ? null : new Profile(ActiveProfilePath);

            RebuildInventoryView();
            SelectedInventoryItem = CurrentSave.Global?.Inventory?.Items?.FirstOrDefault();
            LoadRawEditorsFromCurrent();
            RebuildOverview();
            UpdateMap();

            StatusMessage = $"Loaded {entry.DisplayName} from {entry.FileName}.";
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.ToString(), "Failed to load save", MessageBoxButton.OK, MessageBoxImage.Error);
            StatusMessage = "Failed to load the selected save.";
        }
    }

    private string FindProfilePathForSave(SaveFileEntry entry)
    {
        var folders = new[] { entry.DirectoryPath, SaveRootPath, Path.Combine(SaveRootPath, "Survival") }
            .Where(Directory.Exists)
            .Distinct(StringComparer.OrdinalIgnoreCase);

        return folders
            .SelectMany(folder => Directory.GetFiles(folder, "user001.*"))
            .OrderByDescending(File.GetLastWriteTime)
            .FirstOrDefault();
    }

    private void RebuildOverview()
    {
        UnknownInventoryIds.Clear();
        if (CurrentSave?.Global?.Inventory?.Items != null)
        {
            foreach (var id in CurrentSave.Global.Inventory.Items
                .Where(x => x.Category == ItemCategory.Unknown)
                .Select(x => x.m_PrefabName)
                .Distinct()
                .OrderBy(x => x))
            {
                UnknownInventoryIds.Add(id);
            }
        }

        if (CurrentSave == null)
        {
            OverviewNotes = "Choose a save to inspect and edit.";
            return;
        }

        OverviewNotes = CurrentSave.Global?.Inventory?.Items != null
            ? $"Player, Inventory, Map and Afflictions are ready. Inventory items: {CurrentSave.Global.Inventory.Items.Count}."
            : "Player, Inventory, Map and Afflictions are ready.";
    }

    private void RefreshKnownItemIds()
    {
        var filtered = ItemDictionary.itemInfo
            .Where(kvp => SelectedInventoryCategory?.Category == null || kvp.Value.category == SelectedInventoryCategory.Category.Value)
            .Select(kvp => kvp.Key)
            .OrderBy(x => x)
            .ToList();

        FilteredKnownItemIds.Clear();
        foreach (var itemId in filtered)
        {
            FilteredKnownItemIds.Add(itemId);
        }

        if (FilteredKnownItemIds.Count == 0)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(SelectedKnownItemId))
        {
            SelectedKnownItemId = FilteredKnownItemIds[0];
            return;
        }

        var currentIsKnown = ItemDictionary.itemInfo.ContainsKey(SelectedKnownItemId);
        var currentVisible = FilteredKnownItemIds.Any(x => string.Equals(x, SelectedKnownItemId, StringComparison.OrdinalIgnoreCase));
        if (currentIsKnown && !currentVisible)
        {
            SelectedKnownItemId = FilteredKnownItemIds[0];
        }
    }

    private void RaiseInventoryEditorStateChanged()
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CanEditCondition)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CanEditAmount)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CanEditCalories)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CanEditLiquidLiters)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CanEditFuelLiters)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CanEditWaterVolume)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CanEditRounds)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CanEditCraftProgress)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CanEditEvolutionTime)));
    }

    private void RebuildInventoryView()
    {
        if (CurrentSave?.Global?.Inventory?.Items == null)
        {
            InventoryItemsView = null;
            return;
        }

        InventoryItemsView = CollectionViewSource.GetDefaultView(CurrentSave.Global.Inventory.Items);
        InventoryItemsView.SortDescriptions.Clear();
        InventoryItemsView.SortDescriptions.Add(new SortDescription(nameof(InventoryItemSaveData.Category), ListSortDirection.Ascending));
        InventoryItemsView.SortDescriptions.Add(new SortDescription(nameof(InventoryItemSaveData.InGameName), ListSortDirection.Ascending));
        InventoryItemsView.Filter = FilterInventory;
    }

    private bool FilterInventory(object obj)
    {
        if (obj is not InventoryItemSaveData item)
        {
            return false;
        }

        if (SelectedInventoryCategory?.Category != null && item.Category != SelectedInventoryCategory.Category.Value)
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(InventorySearchText))
        {
            return true;
        }

        return item.m_PrefabName.Contains(InventorySearchText, StringComparison.OrdinalIgnoreCase)
            || item.InGameName.Contains(InventorySearchText, StringComparison.OrdinalIgnoreCase);
    }

    private void LoadRawEditorsFromCurrent()
    {
        RawSlotJson = CurrentSave?.SlotJson ?? string.Empty;
        RawBootJson = CurrentSave?.BootJson ?? string.Empty;
        RawGlobalJson = CurrentSave?.GlobalJson ?? string.Empty;
        RawProfileJson = CurrentProfile?.RawJson ?? string.Empty;
        appliedRawSlotJson = RawSlotJson;
        appliedRawBootJson = RawBootJson;
        appliedRawGlobalJson = RawGlobalJson;
        appliedRawProfileJson = RawProfileJson;
    }

    private bool HasPendingRawEditorChanges()
    {
        return !string.Equals(RawSlotJson, appliedRawSlotJson, StringComparison.Ordinal)
            || !string.Equals(RawBootJson, appliedRawBootJson, StringComparison.Ordinal)
            || !string.Equals(RawGlobalJson, appliedRawGlobalJson, StringComparison.Ordinal)
            || !string.Equals(RawProfileJson, appliedRawProfileJson, StringComparison.Ordinal);
    }

    private bool TryApplyRawEditors()
    {
        try
        {
            if (CurrentSave != null)
            {
                var updatedSave = new GameSave { path = CurrentSave.path };
                updatedSave.ReplaceRawJson(RawSlotJson, RawBootJson, RawGlobalJson);
                CurrentSave = updatedSave;
            }

            if (CurrentProfile != null && !string.IsNullOrWhiteSpace(RawProfileJson))
            {
                var updatedProfile = new Profile(CurrentProfile.path);
                updatedProfile.ReplaceRawJson(RawProfileJson);
                CurrentProfile = updatedProfile;
            }

            RebuildInventoryView();
            SelectedInventoryItem = CurrentSave?.Global?.Inventory?.Items?.FirstOrDefault();
            RebuildOverview();
            UpdateMap();
            LoadRawEditorsFromCurrent();

            StatusMessage = "Raw changes applied in memory.";
            return true;
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.ToString(), "Invalid raw JSON", MessageBoxButton.OK, MessageBoxImage.Error);
            StatusMessage = "Raw changes failed to apply.";
            return false;
        }
    }

    private InventoryItemSaveData CreateInventoryItem(string prefabId)
    {
        var gear = GearItemSaveDataProxy.Create(CurrentSave);
        if (ItemDictionary.itemInfo.TryGetValue(prefabId, out var info))
        {
            JsonConvert.PopulateObject(info.defaultSerialized, gear);
        }

        return new InventoryItemSaveData
        {
            m_PrefabName = prefabId,
            Gear = gear
        };
    }

    private int GenerateUniqueInventoryInstanceId()
    {
        if (CurrentSave?.Global?.Inventory?.Items == null)
        {
            return Random.Shared.Next();
        }

        var existingIds = CurrentSave.Global.Inventory.Items
            .Where(x => x?.Gear != null)
            .Select(x => x.Gear.m_InstanceIDProxy)
            .ToHashSet();

        var id = Random.Shared.Next();
        while (existingIds.Contains(id))
        {
            id = Random.Shared.Next();
        }

        return id;
    }

    private InventoryItemSaveData CreateDuplicateInventoryItem(InventoryItemSaveData source)
    {
        if (source == null)
        {
            return null;
        }

        var clone = JsonConvert.DeserializeObject<InventoryItemSaveData>(JsonConvert.SerializeObject(source));
        if (clone?.Gear == null)
        {
            return clone;
        }

        clone.Gear.m_InstanceIDProxy = GenerateUniqueInventoryInstanceId();
        clone.Gear.m_BeenInPlayerInventoryProxy = true;
        clone.Gear.m_BeenInContainerProxy = false;
        clone.Gear.m_HasBeenOwnedByPlayer = true;
        clone.Gear.m_ItemLootedProxy = true;
        clone.Gear.m_RolledSpawnChanceProxy = true;
        clone.Gear.m_HoursPlayed = CurrentSave?.Global?.TimeOfDay?.m_HoursPlayedNotPausedProxy ?? clone.Gear.m_HoursPlayed;
        return clone;
    }

    private void SaveAll_Click(object sender, RoutedEventArgs e)
    {
        if (HasPendingRawEditorChanges() && !TryApplyRawEditors())
        {
            ShowTopActionMessage("Apply failed", false);
            return;
        }

        try
        {
            CurrentSave?.Save();
            CurrentProfile?.Save();
            LoadRawEditorsFromCurrent();
            RefreshSaveList(CurrentSave?.path);
            StatusMessage = "Changes saved. Backups were created before writing.";
            ShowTopActionMessage("Saved", true);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.ToString(), "Save failed", MessageBoxButton.OK, MessageBoxImage.Error);
            StatusMessage = "Saving failed.";
            ShowTopActionMessage("Save failed", false);
        }
    }

    private void ApplyRaw_Click(object sender, RoutedEventArgs e)
    {
        TryApplyRawEditors();
    }

    private void RefreshButton_Click(object sender, RoutedEventArgs e)
    {
        RefreshSaveList(CurrentSave?.path);
        ShowTopActionMessage("Refreshed", true);
    }

    private void ResetRawEditors_Click(object sender, RoutedEventArgs e)
    {
        LoadRawEditorsFromCurrent();
        StatusMessage = "Raw editor text reset to current in-memory state.";
    }

    private void OpenSaveFolder_Click(object sender, RoutedEventArgs e)
    {
        var folder = CurrentSave != null ? Path.GetDirectoryName(CurrentSave.path) : SaveRootPath;
        OpenFolder(folder);
    }

    private void OpenBackupsFolder_Click(object sender, RoutedEventArgs e)
    {
        var folder = CurrentSave != null
            ? Path.Combine(Path.GetDirectoryName(CurrentSave.path), "backups")
            : Path.Combine(SaveRootPath, "backups");
        Directory.CreateDirectory(folder);
        OpenFolder(folder);
    }

    private void AddItem_Click(object sender, RoutedEventArgs e)
    {
        if (CurrentSave == null)
        {
            return;
        }

        var prefabId = string.IsNullOrWhiteSpace(CustomPrefabId)
            ? SelectedKnownItemId
            : CustomPrefabId;

        if (string.IsNullOrWhiteSpace(prefabId))
        {
            return;
        }

        var item = CreateInventoryItem(prefabId.Trim());
        CurrentSave.Global.Inventory.Items.Add(item);
        RebuildInventoryView();
        SelectedInventoryItem = item;
        RebuildOverview();
        StatusMessage = $"Added {item.m_PrefabName} to inventory.";
    }

    private void AddAllKnownItems_Click(object sender, RoutedEventArgs e)
    {
        if (CurrentSave == null)
        {
            return;
        }

        var itemsToAdd = ItemDictionary.itemInfo
            .Where(x => SelectedInventoryCategory?.Category == null || x.Value.category == SelectedInventoryCategory.Category.Value)
            .OrderBy(x => x.Key);

        foreach (var kvp in itemsToAdd)
        {
            if (!IncludeHiddenItemsInBulkAdd && kvp.Value.hide)
            {
                continue;
            }

            CurrentSave.Global.Inventory.Items.Add(CreateInventoryItem(kvp.Key));
        }

        RebuildInventoryView();
        SelectedInventoryItem = CurrentSave.Global.Inventory.Items.LastOrDefault();
        RebuildOverview();
        StatusMessage = SelectedInventoryCategory?.Category == null
            ? "Added every known item entry to the inventory."
            : $"Added every known {SelectedInventoryCategory.Category.Value} item to the inventory.";
    }

    private void DuplicateSelectedItem_Click(object sender, RoutedEventArgs e)
    {
        if (CurrentSave?.Global?.Inventory?.Items == null || SelectedInventoryItem == null)
        {
            return;
        }

        if (!int.TryParse(DuplicateCountText?.Trim(), out var duplicateCount) || duplicateCount <= 0)
        {
            StatusMessage = "Enter a duplicate count greater than 0.";
            ShowTopActionMessage("Duplicate failed", false);
            return;
        }

        InventoryItemSaveData lastDuplicate = null;
        var added = 0;
        for (var i = 0; i < duplicateCount; i++)
        {
            var duplicate = CreateDuplicateInventoryItem(SelectedInventoryItem);
            if (duplicate == null)
            {
                continue;
            }

            CurrentSave.Global.Inventory.Items.Add(duplicate);
            lastDuplicate = duplicate;
            added++;
        }

        if (added == 0)
        {
            StatusMessage = "The selected item could not be duplicated.";
            ShowTopActionMessage("Duplicate failed", false);
            return;
        }

        RebuildInventoryView();
        SelectedInventoryItem = lastDuplicate;
        RebuildOverview();
        StatusMessage = added == 1
            ? $"Duplicated {lastDuplicate.InGameName} once."
            : $"Duplicated {lastDuplicate.InGameName} {added} times.";
        ShowTopActionMessage(added == 1 ? "Duplicated" : $"Duplicated x{added}", true);
    }

    private void RepairAllItems_Click(object sender, RoutedEventArgs e)
    {
        if (CurrentSave?.Global?.Inventory?.Items == null)
        {
            return;
        }

        var repaired = 0;
        var skipped = 0;

        foreach (var item in CurrentSave.Global.Inventory.Items)
        {
            if (TryRepairInventoryItem(item))
            {
                repaired++;
            }
            else
            {
                skipped++;
            }
        }

        StatusMessage = skipped == 0
            ? $"Repaired {repaired} inventory items."
            : $"Repaired {repaired} inventory items. Skipped {skipped} incomplete entries.";
        ShowTopActionMessage("Repair done", true);
    }

    private void BoostAllItemDurability_Click(object sender, RoutedEventArgs e)
    {
        if (CurrentSave?.Global?.Inventory?.Items == null)
        {
            return;
        }

        var boosted = 0;
        foreach (var item in CurrentSave.Global.Inventory.Items)
        {
            if (!TryBoostInventoryItemDurability(item))
            {
                continue;
            }

            boosted++;
        }

        StatusMessage = $"Boosted durability on {boosted} inventory items.";
        ShowTopActionMessage("Durability boosted", true);
    }

    private void MaxSprintStamina_Click(object sender, RoutedEventArgs e)
    {
        if (CurrentSave?.Global?.PlayerMovement == null)
        {
            return;
        }

        CurrentSave.Global.PlayerMovement.m_SprintStamina = 99999f;
        StatusMessage = "Sprint stamina set to a very high value for this save.";
        ShowTopActionMessage("Sprint boosted", true);
    }

    private void RemoveSelectedItem_Click(object sender, RoutedEventArgs e)
    {
        if (CurrentSave?.Global?.Inventory?.Items == null || SelectedInventoryItem == null)
        {
            return;
        }

        CurrentSave.Global.Inventory.Items.Remove(SelectedInventoryItem);
        SelectedInventoryItem = CurrentSave.Global.Inventory.Items.FirstOrDefault();
        RebuildOverview();
        StatusMessage = "Selected item removed.";
    }

    private void RemoveAllItems_Click(object sender, RoutedEventArgs e)
    {
        CurrentSave?.Global?.Inventory?.Items?.Clear();
        RebuildInventoryView();
        SelectedInventoryItem = null;
        RebuildOverview();
        StatusMessage = "Inventory cleared.";
    }

    private void ClearNegativeAfflictions_Click(object sender, RoutedEventArgs e)
    {
        CurrentSave?.Afflictions?.Negative?.Clear();
        StatusMessage = "Negative afflictions cleared.";
    }

    private void ClearPositiveAfflictions_Click(object sender, RoutedEventArgs e)
    {
        CurrentSave?.Afflictions?.Positive?.Clear();
        StatusMessage = "Positive afflictions cleared.";
    }

    private static string NormalizeMapRegion(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();
        if (WikiMapReferences.ContainsKey(trimmed) || MapDictionary.MapExists(trimmed) || MapAssets.ContainsKey(trimmed))
        {
            return trimmed;
        }

        if (trimmed.Contains("Keeper", StringComparison.OrdinalIgnoreCase) && trimmed.Contains("South", StringComparison.OrdinalIgnoreCase))
        {
            return "KeepersPassSouth";
        }

        if (trimmed.Contains("Keeper", StringComparison.OrdinalIgnoreCase) && trimmed.Contains("North", StringComparison.OrdinalIgnoreCase))
        {
            return "KeepersPassNorth";
        }

        if (trimmed.Contains("Hangar", StringComparison.OrdinalIgnoreCase) || trimmed.Contains("Airfield", StringComparison.OrdinalIgnoreCase))
        {
            return "AirfieldRegion";
        }

        if (trimmed.Contains("TransferPass", StringComparison.OrdinalIgnoreCase) || string.Equals(trimmed, "Hub", StringComparison.OrdinalIgnoreCase) || trimmed.Contains("HubRegion", StringComparison.OrdinalIgnoreCase))
        {
            return "TransferPassRegion";
        }

        if (trimmed.Contains("ZoneOfContamination", StringComparison.OrdinalIgnoreCase) || string.Equals(trimmed, "MineRegion", StringComparison.OrdinalIgnoreCase))
        {
            return "ZoneOfContaminationRegion";
        }

        if (trimmed.Contains("SunderedPass", StringComparison.OrdinalIgnoreCase) || string.Equals(trimmed, "PassRegion", StringComparison.OrdinalIgnoreCase))
        {
            return "SunderedPassRegion";
        }

        if (trimmed.Contains("FarRangeBranchLine", StringComparison.OrdinalIgnoreCase))
        {
            return "FarRangeBranchLine";
        }

        if (trimmed.Contains("WindingRiver", StringComparison.OrdinalIgnoreCase))
        {
            return "WindingRiver";
        }

        if (trimmed.Contains("CrumblingHighway", StringComparison.OrdinalIgnoreCase))
        {
            return "HighwayTransitionZone";
        }

        if (trimmed.Contains("Ravine", StringComparison.OrdinalIgnoreCase))
        {
            return "RavineTransitionZone";
        }

        if (trimmed.Contains("Blackrock", StringComparison.OrdinalIgnoreCase))
        {
            return "BlackrockRegion";
        }

        if (trimmed.Contains("BleakInlet", StringComparison.OrdinalIgnoreCase))
        {
            return "CanneryRegion";
        }

        return trimmed;
    }

    private static bool TryResolveLocalMap(string rawRegion, out string normalizedRegion, out MapInfo mapInfo, out string assetFile)
    {
        normalizedRegion = NormalizeMapRegion(rawRegion);
        if (!string.IsNullOrWhiteSpace(normalizedRegion)
            && MapAssets.TryGetValue(normalizedRegion, out assetFile))
        {
            mapInfo = MapDictionary.MapExists(normalizedRegion)
                ? MapDictionary.GetMapInfo(normalizedRegion)
                : null;
            return true;
        }

        mapInfo = null;
        assetFile = null;
        return false;
    }

    private static bool TryResolveWikiMap(string rawRegion, out string normalizedRegion, out WikiMapReference reference)
    {
        normalizedRegion = NormalizeMapRegion(rawRegion);
        if (!string.IsNullOrWhiteSpace(normalizedRegion) && WikiMapReferences.TryGetValue(normalizedRegion, out reference))
        {
            return true;
        }

        reference = null;
        return false;
    }

    private void ShowWikiFallback(string rawRegion, string normalizedRegion, string resolutionSource, WikiMapReference reference)
    {
        currentMapInfo = null;
        currentMapRegion = normalizedRegion;
        MapImage.Source = null;
        MapStatusText = $"{resolutionSource}: {rawRegion}. {reference.DisplayName} is known, but no bundled local image is available yet.";
        MapSourceText = "No external site was opened.";
        MapCanvasMessage.Text = "No bundled map image yet";
        PlayerMarker.Visibility = Visibility.Collapsed;
    }

    private void UpdateMap()
    {
        currentMapInfo = null;
        currentMapRegion = null;
        MapImage.Source = null;
        MapLayerCanvas.Width = 0;
        MapLayerCanvas.Height = 0;
        PlayerMarker.Visibility = Visibility.Collapsed;
        MapCanvasMessage.Text = string.Empty;
        MapSourceText = "No map source selected.";

        if (CurrentSave == null)
        {
            MapStatusText = "No save loaded.";
            MapSourceText = "No map source selected.";
            MapCanvasMessage.Text = "No save loaded";
            return;
        }

        var sceneName = CurrentSave.Boot?.m_SceneName?.Value;
        var lastOutdoor = CurrentSave.Global?.GameManagerData?.SceneTransition?.m_LastOutdoorScene;
        var scenePosition = ReadPlayerPosition(CurrentSave.Global?.PlayerManager?.m_SaveGamePosition);
        var lastOutdoorPosition = ReadSceneTransitionPosition(CurrentSave.Global?.GameManagerData?.SceneTransition?.m_PosBeforeInteriorLoad);
        var candidates = new List<(string Region, Point Position, string Source)>();
        var seenCandidates = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        void AddCandidate(string region, Point position, string source)
        {
            if (string.IsNullOrWhiteSpace(region) || !seenCandidates.Add(region))
            {
                return;
            }

            candidates.Add((region, position, source));
        }

        AddCandidate(sceneName, scenePosition, "Current scene");
        AddCandidate(lastOutdoor, lastOutdoorPosition, "Last outdoor scene");

        foreach (var candidate in candidates)
        {
            if (!TryResolveLocalMap(candidate.Region, out var normalizedRegion, out var mapInfo, out var assetFile))
            {
                continue;
            }

            var assetPath = GetMapAssetPath(assetFile);
            if (!File.Exists(assetPath))
            {
                continue;
            }

            currentMapRegion = normalizedRegion;
            currentMapInfo = mapInfo;
            currentPlayerPosition = candidate.Position;
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.UriSource = new Uri(assetPath);
            bitmap.EndInit();
            bitmap.Freeze();
            MapImage.Source = bitmap;
            MapImage.Width = currentMapInfo?.width ?? bitmap.PixelWidth;
            MapImage.Height = currentMapInfo?.height ?? bitmap.PixelHeight;
            MapLayerCanvas.Width = MapImage.Width;
            MapLayerCanvas.Height = MapImage.Height;
            MapCanvasMessage.Text = string.Empty;
            var resolvedName = GetMapDisplayName(currentMapRegion);
            if (currentMapInfo != null)
            {
                MapStatusText = $"Click a point on the map to move the player. {candidate.Source} resolved to {resolvedName}.";
                MapSourceText = $"Source: bundled calibrated map for {resolvedName}.";
            }
            else
            {
                MapStatusText = $"{candidate.Source} resolved to {resolvedName}. Showing the bundled local map image.";
                MapSourceText = "Source: bundled map image from The Long Dark Wiki interactive maps. Click-to-move and the live marker need calibration data for this region.";
            }

            var imageWidth = MapImage.Width;
            var imageHeight = MapImage.Height;
            var wScale = MapCanvas.ActualWidth / imageWidth;
            var hScale = MapCanvas.ActualHeight / imageHeight;
            var scale = Math.Max(Math.Min(wScale, hScale), 0.5);
            MapScaleTransform.ScaleX = scale;
            MapScaleTransform.ScaleY = scale;
            if (currentMapInfo != null)
            {
                MapMarkerScaleTransform.ScaleX = 1 / scale;
                MapMarkerScaleTransform.ScaleY = 1 / scale;
                UpdatePlayerMarker();
            }
            return;
        }

        foreach (var candidate in candidates)
        {
            if (TryResolveWikiMap(candidate.Region, out var normalizedRegion, out var reference))
            {
                currentPlayerPosition = candidate.Position;
                ShowWikiFallback(candidate.Region, normalizedRegion, candidate.Source, reference);
                MapCanvasMessage.Text = string.Empty;
                return;
            }
        }

        MapStatusText = $"No local or wiki-backed map was resolved for scene '{sceneName ?? "Unknown"}'.";
        MapSourceText = "Map source: unresolved scene or region key.";
        MapCanvasMessage.Text = "No map for this scene";
    }

    private static Point ReadPlayerPosition(System.Collections.Generic.IList<float> values)
    {
        if (values == null || values.Count < 3)
        {
            return new Point(0, 0);
        }

        return new Point(values[0], values[2]);
    }

    private static Point ReadSceneTransitionPosition(float[] values)
    {
        if (values == null || values.Length < 3)
        {
            return new Point(0, 0);
        }

        return new Point(values[0], values[2]);
    }

    private void UpdatePlayerMarker()
    {
        if (currentMapInfo == null)
        {
            PlayerMarker.Visibility = Visibility.Collapsed;
            return;
        }

        var point = currentMapInfo.ToLayer(currentPlayerPosition);
        Canvas.SetLeft(PlayerMarker, point.X);
        Canvas.SetTop(PlayerMarker, point.Y);
        PlayerMarker.Visibility = Visibility.Visible;
    }

    private void MapCanvas_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        UpdateMap();
    }

    private void MapCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (currentMapInfo == null)
        {
            return;
        }

        mapMouseDown = true;
        mapClickPosition = e.GetPosition(MapCanvas);
        mapLastMousePosition = mapClickPosition;
    }

    private void MapCanvas_MouseMove(object sender, MouseEventArgs e)
    {
        if (currentMapInfo == null || !mapMouseDown)
        {
            return;
        }

        MapCanvas.CaptureMouse();
        var mousePos = e.GetPosition(MapCanvas);
        MapTranslateTransform.X += mousePos.X - mapLastMousePosition.X;
        MapTranslateTransform.Y += mousePos.Y - mapLastMousePosition.Y;
        mapLastMousePosition = mousePos;
    }

    private void MapCanvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (currentMapInfo == null)
        {
            return;
        }

        mapMouseDown = false;
        MapCanvas.ReleaseMouseCapture();
        var release = e.GetPosition(MapCanvas);
        if ((release - mapClickPosition).Length > 3)
        {
            return;
        }

        currentPlayerPosition = currentMapInfo.ToRegion(e.GetPosition(MapImage));
        CurrentSave.Boot.m_SceneName.Value = currentMapRegion;
        CurrentSave.Global.PlayerManager.m_SaveGamePosition[0] = (float)currentPlayerPosition.X;
        CurrentSave.Global.PlayerManager.m_SaveGamePosition[2] = (float)currentPlayerPosition.Y;
        UpdatePlayerMarker();
        RebuildOverview();
        StatusMessage = $"Player moved to X={currentPlayerPosition.X:0.##}, Y={currentPlayerPosition.Y:0.##}.";
    }

    private void MapCanvas_MouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (currentMapInfo == null)
        {
            return;
        }

        var zoom = e.Delta > 0 ? .25 * MapScaleTransform.ScaleX : -.25 * MapScaleTransform.ScaleX;
        MapScaleTransform.ScaleX = Math.Max(0.25, MapScaleTransform.ScaleX + zoom);
        MapScaleTransform.ScaleY = Math.Max(0.25, MapScaleTransform.ScaleY + zoom);
        MapMarkerScaleTransform.ScaleX = 1 / MapScaleTransform.ScaleX;
        MapMarkerScaleTransform.ScaleY = 1 / MapScaleTransform.ScaleY;
    }

    private string GetMapAssetPath(string fileName)
    {
        return Path.Combine(AppContext.BaseDirectory, "Assets", "Maps", fileName);
    }

    private void OpenFolder(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        Process.Start(new ProcessStartInfo
        {
            FileName = path,
            UseShellExecute = true
        });
    }

    private static bool TryRepairInventoryItem(InventoryItemSaveData item)
    {
        if (item?.Gear == null)
        {
            return false;
        }

        try
        {
            item.Gear.NormalizedCondition = 1;
            item.Gear.m_WornOut = false;

            if (item.Gear.FlareItem != null)
            {
                item.Gear.FlareItem.m_StateProxy ??= new EnumWrapper<FlareState>(FlareState.Fresh.ToString());
                item.Gear.FlareItem.m_StateProxy.SetValue(FlareState.Fresh);
                item.Gear.FlareItem.m_ElapsedBurnMinutesProxy = 0;
            }

            if (item.Gear.TorchItem != null)
            {
                item.Gear.TorchItem.m_StateProxy ??= new EnumWrapper<TorchState>(TorchState.Fresh.ToString());
                item.Gear.TorchItem.m_StateProxy.SetValue(TorchState.Fresh);
                item.Gear.TorchItem.m_ElapsedBurnMinutesProxy = 0;
            }

            return true;
        }
        catch
        {
            return false;
        }

    }

    private static bool TryBoostInventoryItemDurability(InventoryItemSaveData item)
    {
        if (item?.Gear == null)
        {
            return false;
        }

        try
        {
            item.Gear.NormalizedCondition = 1;
            item.Gear.m_WornOut = false;
            item.Gear.m_CurrentHPProxy = Math.Max(item.Gear.m_CurrentHPProxy, 99999f);

            if (item.Gear.BodyHarvest != null)
            {
                item.Gear.BodyHarvest.m_Condition = Math.Max(item.Gear.BodyHarvest.m_Condition, 99999f);
            }

            if (item.Gear.FlareItem != null)
            {
                item.Gear.FlareItem.m_StateProxy ??= new EnumWrapper<FlareState>(FlareState.Fresh.ToString());
                item.Gear.FlareItem.m_StateProxy.SetValue(FlareState.Fresh);
                item.Gear.FlareItem.m_ElapsedBurnMinutesProxy = 0;
            }

            if (item.Gear.TorchItem != null)
            {
                item.Gear.TorchItem.m_StateProxy ??= new EnumWrapper<TorchState>(TorchState.Fresh.ToString());
                item.Gear.TorchItem.m_StateProxy.SetValue(TorchState.Fresh);
                item.Gear.TorchItem.m_ElapsedBurnMinutesProxy = 0;
            }

            return true;
        }
        catch
        {
            return false;
        }
    }

    private bool SetField<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return false;
        }

        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        return true;
    }

    private async void ShowTopActionMessage(string message, bool success)
    {
        TopActionBrush = success ? SuccessFeedbackBrush : FailureFeedbackBrush;
        var version = ++topActionMessageVersion;
        TopActionMessage = message;

        await Task.Delay(3200);
        if (version != topActionMessageVersion)
        {
            return;
        }

        TopActionMessage = string.Empty;
    }
}
