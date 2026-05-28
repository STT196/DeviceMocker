using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
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
        private string _editName = string.Empty;
        private string _editDescription = string.Empty;
        private string _editSuffix = "Enter";
        private int _editDelay = 10;
        private bool _isEditing;

        public ObservableCollection<DeviceProfile> Profiles { get; } = new();

        public DeviceProfile? SelectedProfile
        {
            get => _selectedProfile;
            set
            {
                if (SetProperty(ref _selectedProfile, value) && value != null)
                {
                    EditName = value.Name;
                    EditDescription = value.Description;
                    EditSuffix = value.DefaultSuffix;
                    EditDelay = value.DelayPerCharacterMs;
                    IsEditing = true;
                }
                else if (value == null) IsEditing = false;
            }
        }

        public string StatusMessage { get => _statusMessage; set => SetProperty(ref _statusMessage, value); }
        public string EditName { get => _editName; set => SetProperty(ref _editName, value); }
        public string EditDescription { get => _editDescription; set => SetProperty(ref _editDescription, value); }
        public string EditSuffix { get => _editSuffix; set => SetProperty(ref _editSuffix, value); }
        public int EditDelay { get => _editDelay; set => SetProperty(ref _editDelay, value); }
        public bool IsEditing { get => _isEditing; set => SetProperty(ref _isEditing, value); }

        public string[] SuffixOptions { get; } = { "None", "Enter", "Tab", "CR", "LF", "CRLF" };

        public ICommand LoadProfilesCommand { get; }
        public ICommand CreateProfileCommand { get; }
        public ICommand DeleteProfileCommand { get; }
        public ICommand DuplicateProfileCommand { get; }
        public ICommand ExportProfileCommand { get; }
        public ICommand ImportProfileCommand { get; }
        public ICommand SaveEditCommand { get; }

        public ProfilesViewModel()
        {
            LoadProfilesCommand = new AsyncRelayCommand(LoadProfilesAsync);
            CreateProfileCommand = new AsyncRelayCommand(CreateProfileAsync);
            DeleteProfileCommand = new AsyncRelayCommand(DeleteProfileAsync, () => SelectedProfile != null);
            DuplicateProfileCommand = new AsyncRelayCommand(DuplicateProfileAsync, () => SelectedProfile != null);
            ExportProfileCommand = new AsyncRelayCommand(ExportProfileAsync, () => SelectedProfile != null);
            ImportProfileCommand = new AsyncRelayCommand(ImportProfileAsync);
            SaveEditCommand = new AsyncRelayCommand(SaveEditAsync, () => SelectedProfile != null);
        }

        private async Task LoadProfilesAsync()
        {
            try
            {
                var profiles = await ServiceLocator.ProfileManager.GetAllProfilesAsync();
                Profiles.Clear();
                foreach (var p in profiles) Profiles.Add(p);
                StatusMessage = $"Loaded {profiles.Count} profile(s).";
            }
            catch (Exception ex) { StatusMessage = $"Error: {ex.Message}"; }
        }

        private async Task CreateProfileAsync()
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
            StatusMessage = "Profile created.";
        }

        private async Task DeleteProfileAsync()
        {
            if (SelectedProfile == null) return;
            await ServiceLocator.ProfileManager.DeleteProfileAsync(SelectedProfile.Id);
            await LoadProfilesAsync();
            StatusMessage = "Profile deleted.";
        }

        private async Task DuplicateProfileAsync()
        {
            if (SelectedProfile == null) return;
            await ServiceLocator.ProfileManager.DuplicateProfileAsync(SelectedProfile.Id);
            await LoadProfilesAsync();
            StatusMessage = "Profile duplicated.";
        }

        private async Task ExportProfileAsync()
        {
            if (SelectedProfile == null) return;
            var dialog = new SaveFileDialog { Filter = "JSON files (*.json)|*.json", FileName = $"{SelectedProfile.Name}.json" };
            if (dialog.ShowDialog() == true)
            {
                await ServiceLocator.ProfileManager.ExportProfileAsync(SelectedProfile.Id, dialog.FileName);
                StatusMessage = $"Exported to {dialog.FileName}";
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
                    StatusMessage = "Profile imported.";
                }
                catch (Exception ex) { StatusMessage = $"Import error: {ex.Message}"; }
            }
        }

        private async Task SaveEditAsync()
        {
            if (SelectedProfile == null) return;
            SelectedProfile.Name = EditName;
            SelectedProfile.Description = EditDescription;
            SelectedProfile.DefaultSuffix = EditSuffix;
            SelectedProfile.DelayPerCharacterMs = EditDelay;
            await ServiceLocator.ProfileManager.SaveProfileAsync(SelectedProfile);
            await LoadProfilesAsync();
            StatusMessage = "Profile saved.";
        }
    }
}
