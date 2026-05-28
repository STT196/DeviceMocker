using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Input;
using DeviceMocker.Core;
using DeviceMocker.Helpers;
using DeviceMocker.Models;
using Microsoft.Win32;

namespace DeviceMocker.ViewModels
{
    public class ProfilesViewModel : ViewModelBase
    {
        private DeviceProfile? _selectedProfile;
        private string _statusMessage = string.Empty;
        private bool _isError;
        private string _searchText = string.Empty;

        private string _editName = string.Empty;
        private string _editDescription = string.Empty;
        private string _editSuffix = "Enter";
        private string _editPrefix = string.Empty;
        private int _editDelay = 10;
        private DeviceType _editDeviceType = DeviceType.Scanner;
        private OutputChannelType _editOutputChannel = OutputChannelType.Keyboard;
        private bool _isEditing;

        public ObservableCollection<DeviceProfile> Profiles { get; } = new();
        public ICollectionView ProfilesView { get; }

        public DeviceProfile? SelectedProfile
        {
            get => _selectedProfile;
            set
            {
                if (SetProperty(ref _selectedProfile, value) && value != null)
                {
                    LoadEditFromProfile(value);
                    IsEditing = true;
                }
                else if (value == null) IsEditing = false;
            }
        }

        public string StatusMessage { get => _statusMessage; set => SetProperty(ref _statusMessage, value); }
        public bool IsError { get => _isError; set => SetProperty(ref _isError, value); }

        public string SearchText
        {
            get => _searchText;
            set { if (SetProperty(ref _searchText, value)) ProfilesView.Refresh(); }
        }

        public string EditName { get => _editName; set => SetProperty(ref _editName, value); }
        public string EditDescription { get => _editDescription; set => SetProperty(ref _editDescription, value); }
        public string EditSuffix { get => _editSuffix; set => SetProperty(ref _editSuffix, value); }
        public string EditPrefix { get => _editPrefix; set => SetProperty(ref _editPrefix, value); }
        public int EditDelay { get => _editDelay; set => SetProperty(ref _editDelay, value); }
        public DeviceType EditDeviceType { get => _editDeviceType; set => SetProperty(ref _editDeviceType, value); }
        public OutputChannelType EditOutputChannel { get => _editOutputChannel; set => SetProperty(ref _editOutputChannel, value); }
        public bool IsEditing { get => _isEditing; set => SetProperty(ref _isEditing, value); }

        public string[] SuffixOptions { get; } = { "None", "Enter", "Tab", "CR", "LF", "CRLF" };
        public Array DeviceTypeOptions { get; } = Enum.GetValues(typeof(DeviceType));
        public Array OutputChannelOptions { get; } = Enum.GetValues(typeof(OutputChannelType));

        public ICommand LoadProfilesCommand { get; }
        public ICommand CreateProfileCommand { get; }
        public ICommand DeleteProfileCommand { get; }
        public ICommand DuplicateProfileCommand { get; }
        public ICommand ExportProfileCommand { get; }
        public ICommand ImportProfileCommand { get; }
        public ICommand SaveEditCommand { get; }
        public ICommand CancelEditCommand { get; }

        public ProfilesViewModel()
        {
            ProfilesView = CollectionViewSource.GetDefaultView(Profiles);
            ProfilesView.Filter = FilterProfile;

            LoadProfilesCommand = new AsyncRelayCommand(LoadProfilesAsync);
            CreateProfileCommand = new AsyncRelayCommand(CreateProfileAsync);
            DeleteProfileCommand = new AsyncRelayCommand(DeleteProfileAsync, () => SelectedProfile != null);
            DuplicateProfileCommand = new AsyncRelayCommand(DuplicateProfileAsync, () => SelectedProfile != null);
            ExportProfileCommand = new AsyncRelayCommand(ExportProfileAsync, () => SelectedProfile != null);
            ImportProfileCommand = new AsyncRelayCommand(ImportProfileAsync);
            SaveEditCommand = new AsyncRelayCommand(SaveEditAsync, () => SelectedProfile != null);
            CancelEditCommand = new RelayCommand(CancelEdit, () => SelectedProfile != null);
        }

        private bool FilterProfile(object obj)
        {
            if (obj is not DeviceProfile p) return false;
            if (string.IsNullOrWhiteSpace(SearchText)) return true;
            var q = SearchText.Trim();
            return (p.Name?.IndexOf(q, StringComparison.OrdinalIgnoreCase) ?? -1) >= 0
                || (p.Description?.IndexOf(q, StringComparison.OrdinalIgnoreCase) ?? -1) >= 0
                || p.DeviceType.ToString().IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private void LoadEditFromProfile(DeviceProfile p)
        {
            EditName = p.Name;
            EditDescription = p.Description;
            EditSuffix = string.IsNullOrEmpty(p.DefaultSuffix) ? "None" : p.DefaultSuffix;
            EditPrefix = p.DefaultPrefix;
            EditDelay = p.DelayPerCharacterMs;
            EditDeviceType = p.DeviceType;
            EditOutputChannel = p.DefaultOutputChannel;
        }

        private void CancelEdit()
        {
            if (SelectedProfile != null) LoadEditFromProfile(SelectedProfile);
            SetStatus("Changes discarded.", false);
        }

        private void SetStatus(string msg, bool isError)
        {
            StatusMessage = msg;
            IsError = isError;
        }

        private async Task LoadProfilesAsync()
        {
            try
            {
                var profiles = await ServiceLocator.ProfileManager.GetAllProfilesAsync();
                Profiles.Clear();
                foreach (var p in profiles.OrderBy(x => x.Name)) Profiles.Add(p);
                SetStatus($"Loaded {profiles.Count} profile(s).", false);
            }
            catch (Exception ex) { SetStatus($"Error: {ex.Message}", true); }
        }

        private async Task CreateProfileAsync()
        {
            try
            {
                var profile = new DeviceProfile
                {
                    Name = "New Profile",
                    DeviceType = DeviceType.Scanner,
                    Description = "New device profile",
                    DefaultOutputChannel = OutputChannelType.Keyboard,
                    DefaultSuffix = "Enter",
                    DelayPerCharacterMs = 10
                };
                await ServiceLocator.ProfileManager.SaveProfileAsync(profile);
                await LoadProfilesAsync();
                SelectedProfile = Profiles.FirstOrDefault(p => p.Id == profile.Id);
                SetStatus("Profile created.", false);
            }
            catch (Exception ex) { SetStatus($"Create error: {ex.Message}", true); }
        }

        private async Task DeleteProfileAsync()
        {
            if (SelectedProfile == null) return;
            try
            {
                await ServiceLocator.ProfileManager.DeleteProfileAsync(SelectedProfile.Id);
                await LoadProfilesAsync();
                SetStatus("Profile deleted.", false);
            }
            catch (Exception ex) { SetStatus($"Delete error: {ex.Message}", true); }
        }

        private async Task DuplicateProfileAsync()
        {
            if (SelectedProfile == null) return;
            try
            {
                await ServiceLocator.ProfileManager.DuplicateProfileAsync(SelectedProfile.Id);
                await LoadProfilesAsync();
                SetStatus("Profile duplicated.", false);
            }
            catch (Exception ex) { SetStatus($"Duplicate error: {ex.Message}", true); }
        }

        private async Task ExportProfileAsync()
        {
            if (SelectedProfile == null) return;
            var dialog = new SaveFileDialog { Filter = "JSON files (*.json)|*.json", FileName = $"{SelectedProfile.Name}.json" };
            if (dialog.ShowDialog() == true)
            {
                try
                {
                    await ServiceLocator.ProfileManager.ExportProfileAsync(SelectedProfile.Id, dialog.FileName);
                    SetStatus($"Exported to {dialog.FileName}", false);
                }
                catch (Exception ex) { SetStatus($"Export error: {ex.Message}", true); }
            }
        }

        private async Task ImportProfileAsync()
        {
            var dialog = new OpenFileDialog { Filter = "JSON files (*.json)|*.json" };
            if (dialog.ShowDialog() == true)
            {
                try
                {
                    await ServiceLocator.ProfileManager.ImportProfileAsync(dialog.FileName);
                    await LoadProfilesAsync();
                    SetStatus("Profile imported.", false);
                }
                catch (Exception ex) { SetStatus($"Import error: {ex.Message}", true); }
            }
        }

        private async Task SaveEditAsync()
        {
            if (SelectedProfile == null) return;
            if (string.IsNullOrWhiteSpace(EditName))
            {
                SetStatus("Name cannot be empty.", true);
                return;
            }
            try
            {
                SelectedProfile.Name = EditName.Trim();
                SelectedProfile.Description = EditDescription ?? string.Empty;
                SelectedProfile.DefaultSuffix = EditSuffix == "None" ? string.Empty : EditSuffix;
                SelectedProfile.DefaultPrefix = EditPrefix ?? string.Empty;
                SelectedProfile.DelayPerCharacterMs = Math.Max(0, EditDelay);
                SelectedProfile.DeviceType = EditDeviceType;
                SelectedProfile.DefaultOutputChannel = EditOutputChannel;
                SelectedProfile.UpdatedAt = DateTime.Now;
                await ServiceLocator.ProfileManager.SaveProfileAsync(SelectedProfile);
                var savedId = SelectedProfile.Id;
                await LoadProfilesAsync();
                SelectedProfile = Profiles.FirstOrDefault(p => p.Id == savedId);
                SetStatus("Profile saved.", false);
            }
            catch (Exception ex) { SetStatus($"Save error: {ex.Message}", true); }
        }
    }
}
