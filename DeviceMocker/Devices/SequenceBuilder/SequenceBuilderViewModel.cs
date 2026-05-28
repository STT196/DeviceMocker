using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using DeviceMocker.Core;
using DeviceMocker.Helpers;
using DeviceMocker.Models;
using DeviceMocker.Services;

namespace DeviceMocker.Devices.SequenceBuilder
{
    public class SequenceBuilderViewModel : ViewModelBase
    {
        private string _statusMessage = string.Empty;
        private string _countdownText = string.Empty;
        private bool _isRunning;
        private int _currentStepIndex = -1;
        private string _newPayload = string.Empty;
        private string _newSuffix = "Enter";
        private string _newActionType = "Text";
        private int _newDelayAfter = 500;
        private CancellationTokenSource? _cts;

        public ObservableCollection<SequenceStep> Steps { get; } = new();

        public string StatusMessage { get => _statusMessage; set => SetProperty(ref _statusMessage, value); }
        public string CountdownText { get => _countdownText; set => SetProperty(ref _countdownText, value); }
        public bool IsRunning { get => _isRunning; set => SetProperty(ref _isRunning, value); }
        public int CurrentStepIndex { get => _currentStepIndex; set => SetProperty(ref _currentStepIndex, value); }
        public string NewPayload { get => _newPayload; set => SetProperty(ref _newPayload, value); }
        public string NewSuffix { get => _newSuffix; set => SetProperty(ref _newSuffix, value); }
        public string NewActionType { get => _newActionType; set => SetProperty(ref _newActionType, value); }
        public int NewDelayAfter { get => _newDelayAfter; set => SetProperty(ref _newDelayAfter, value); }

        public string[] SuffixOptions { get; } = { "None", "Enter", "Tab" };
        public string[] ActionTypeOptions { get; } = { "Text", "Key", "Shortcut" };

        public ICommand AddStepCommand { get; }
        public ICommand RemoveStepCommand { get; }
        public ICommand ClearStepsCommand { get; }
        public ICommand RunSequenceCommand { get; }
        public ICommand CancelCommand { get; }
        public ICommand LoadPresetCommand { get; }

        public SequenceBuilderViewModel()
        {
            AddStepCommand = new RelayCommand(AddStep, () => !string.IsNullOrEmpty(NewPayload));
            RemoveStepCommand = new RelayCommand(RemoveStep);
            ClearStepsCommand = new RelayCommand(() => { Steps.Clear(); StatusMessage = "Cleared."; });
            RunSequenceCommand = new AsyncRelayCommand(RunSequenceAsync, () => !IsRunning && Steps.Count > 0);
            CancelCommand = new RelayCommand(() => _cts?.Cancel(), () => IsRunning);
            LoadPresetCommand = new RelayCommand(LoadPreset);
        }

        private void AddStep()
        {
            if (string.IsNullOrEmpty(NewPayload)) return;
            Enum.TryParse<ActionType>(NewActionType, out var at);
            Steps.Add(new SequenceStep
            {
                Order = Steps.Count + 1,
                Label = $"Step {Steps.Count + 1}",
                ActionType = at,
                Payload = NewPayload,
                Suffix = NewSuffix,
                DelayAfterMs = NewDelayAfter
            });
            NewPayload = string.Empty;
            StatusMessage = $"{Steps.Count} step(s) in sequence.";
        }

        private void RemoveStep(object? p)
        {
            if (p is SequenceStep step) Steps.Remove(step);
            for (int i = 0; i < Steps.Count; i++) { Steps[i].Order = i + 1; Steps[i].Label = $"Step {i + 1}"; }
        }

        private void LoadPreset(object? p)
        {
            if (p is not string preset) return;
            Steps.Clear();
            switch (preset)
            {
                case "POS Login":
                    Steps.Add(new SequenceStep { Order = 1, Label = "Username", ActionType = ActionType.Text, Payload = "admin", Suffix = "Tab", DelayAfterMs = 300 });
                    Steps.Add(new SequenceStep { Order = 2, Label = "Password", ActionType = ActionType.Text, Payload = "password123", Suffix = "Enter", DelayAfterMs = 500 });
                    break;
                case "Scan 3 Items":
                    Steps.Add(new SequenceStep { Order = 1, Label = "Item 1", ActionType = ActionType.Text, Payload = "4801234567890", Suffix = "Enter", DelayAfterMs = 800 });
                    Steps.Add(new SequenceStep { Order = 2, Label = "Item 2", ActionType = ActionType.Text, Payload = "5449000000996", Suffix = "Enter", DelayAfterMs = 800 });
                    Steps.Add(new SequenceStep { Order = 3, Label = "Item 3", ActionType = ActionType.Text, Payload = "8850999220017", Suffix = "Enter", DelayAfterMs = 800 });
                    break;
                case "Form Fill":
                    Steps.Add(new SequenceStep { Order = 1, Label = "Name", ActionType = ActionType.Text, Payload = "John Doe", Suffix = "Tab", DelayAfterMs = 300 });
                    Steps.Add(new SequenceStep { Order = 2, Label = "Email", ActionType = ActionType.Text, Payload = "john@example.com", Suffix = "Tab", DelayAfterMs = 300 });
                    Steps.Add(new SequenceStep { Order = 3, Label = "Phone", ActionType = ActionType.Text, Payload = "+1234567890", Suffix = "Tab", DelayAfterMs = 300 });
                    Steps.Add(new SequenceStep { Order = 4, Label = "Submit", ActionType = ActionType.Key, Payload = "Enter", Suffix = "None", DelayAfterMs = 0 });
                    break;
            }
            StatusMessage = $"Loaded preset: {preset} ({Steps.Count} steps)";
        }

        private async Task RunSequenceAsync()
        {
            if (Steps.Count == 0) return;
            IsRunning = true;
            _cts = new CancellationTokenSource();

            try
            {
                // Countdown first
                var cd = new CountdownSendService();
                var secs = ServiceLocator.Settings.Current.CountdownSeconds; if (secs <= 0) secs = 3;
                cd.CountdownTick += r => System.Windows.Application.Current?.Dispatcher?.Invoke(() => CountdownText = $"Starting in {r}...");
                cd.CountdownCompleted += () => System.Windows.Application.Current?.Dispatcher?.Invoke(() => CountdownText = "Running...");

                await cd.StartCountdownAsync(secs, async () =>
                {
                    for (int i = 0; i < Steps.Count; i++)
                    {
                        _cts!.Token.ThrowIfCancellationRequested();
                        var step = Steps[i];

                        System.Windows.Application.Current?.Dispatcher?.Invoke(() =>
                        {
                            CurrentStepIndex = i;
                            CountdownText = $"Step {i + 1}/{Steps.Count}: {step.Label}";
                        });

                        var action = new DeviceAction
                        {
                            DeviceId = "sequence-builder",
                            DeviceName = "Test Sequence",
                            DeviceType = DeviceType.CustomScripted,
                            ActionType = step.ActionType,
                            OutputChannelType = OutputChannelType.Keyboard,
                            Payload = step.Payload,
                            Suffix = step.Suffix,
                            DelayPerCharacterMs = 10
                        };

                        await ServiceLocator.DeviceManager.GetDevice("sequence-builder")!.SendAsync(action, _cts.Token);

                        if (step.DelayAfterMs > 0 && i < Steps.Count - 1)
                            await Task.Delay(step.DelayAfterMs, _cts.Token);
                    }

                    System.Windows.Application.Current?.Dispatcher?.Invoke(() =>
                    {
                        StatusMessage = $"Sequence complete! Ran {Steps.Count} steps.";
                        CountdownText = string.Empty;
                        CurrentStepIndex = -1;
                    });
                }, _cts.Token);
            }
            catch (OperationCanceledException) { StatusMessage = "Sequence cancelled."; CountdownText = string.Empty; CurrentStepIndex = -1; }
            catch (Exception ex) { StatusMessage = $"Error: {ex.Message}"; CountdownText = string.Empty; CurrentStepIndex = -1; }
            finally { IsRunning = false; _cts?.Dispose(); _cts = null; }
        }
    }
}
