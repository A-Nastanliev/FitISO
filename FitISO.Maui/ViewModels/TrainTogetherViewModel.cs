using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FitISO.Maui.Services;
using FitISO.Sharing.Session;
using FitISO.Sharing.Snapshots;
using FitISO.Sharing.Transport;

namespace FitISO.Maui.ViewModels
{
    public partial class TrainTogetherViewModel : ObservableObject, IDisposable
    {
        readonly IPairingTransport _transport;
        readonly PartnerSessionCoordinator _coordinator;
        readonly WorkoutSnapshotPublisher _publisher;
        string? _endedName;
        bool _partnerEnded;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsOff), nameof(IsSearching), nameof(IsReconnecting),
            nameof(IsConnected), nameof(IsConnectionLost), nameof(IsPartnerEnded),
            nameof(IsCardVisible), nameof(ShowToggle))]
        TrainTogetherStatus status = TrainTogetherStatus.Off;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasPartnerName))]
        string? partnerName;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(ViewingMe))]
        bool viewingPartner;

        public bool HasPartnerName => !string.IsNullOrEmpty(PartnerName);

        [ObservableProperty]
        bool isStarting;

        public bool IsOff => Status == TrainTogetherStatus.Off;
        public bool IsSearching => Status == TrainTogetherStatus.Searching;
        public bool IsReconnecting => Status == TrainTogetherStatus.Reconnecting;
        public bool IsConnected => Status == TrainTogetherStatus.Connected;
        public bool IsConnectionLost => Status == TrainTogetherStatus.ConnectionLost;
        public bool IsPartnerEnded => Status == TrainTogetherStatus.PartnerEnded;
        public bool IsCardVisible => Status != TrainTogetherStatus.Off;

        public bool ShowToggle => IsConnected || IsPartnerEnded;

        public bool ViewingMe => !ViewingPartner;

        public WorkoutSnapshot? PartnerSnapshot => _coordinator.PartnerSnapshot;

        public DateTime PartnerTimeToLocal(DateTime partnerUtc) => _coordinator.PartnerTimeToLocal(partnerUtc);

        public TrainTogetherViewModel(IPairingTransport transport, PartnerSessionCoordinator coordinator, WorkoutSnapshotPublisher publisher)
        {
            _transport = transport;
            _coordinator = coordinator;
            _publisher = publisher;

            _transport.StateChanged += OnTransportStateChanged;
            _transport.PairingRequested += OnPairingRequested;
            _coordinator.PartnerUpdated += OnPartnerUpdated;
            _publisher.SessionEnded += OnOwnSessionEnded;
        }

        [RelayCommand]
        async Task Start()
        {
            if (IsStarting || !IsOff) return;

            IsStarting = true;
            try
            {
                if (!await EnsurePermissionAsync())
                    return;

                await _transport.StartAsync();
                Refresh();
            }
            catch (Exception ex)
            {
                await ShowAlertAsync("Train together",
                    "Couldn't start searching for a partner. Make sure Bluetooth and Wi-Fi are on" +
                    " (and Location on Android 11 or older).\n\n" + ex.Message);
                Refresh();
            }
            finally
            {
                IsStarting = false;
            }
        }

        [RelayCommand]
        Task Stop() => StopSessionAsync();

        async Task StopSessionAsync()
        {
            _partnerEnded = false;
            _endedName = null;
            ViewingPartner = false;

            await _transport.StopAsync();
            _coordinator.ResetPartner();
            Refresh();
        }

        [RelayCommand]
        async Task Reconnect()
        {
            await _transport.ReconnectAsync();
            Refresh();
        }

        [RelayCommand]
        void ShowMe() => ViewingPartner = false;

        [RelayCommand]
        void ShowPartner()
        {
            if (ShowToggle)
                ViewingPartner = true;
        }

        static async Task<bool> EnsurePermissionAsync()
        {
#if ANDROID
            var status = await Permissions.CheckStatusAsync<NearbyPermission>();
            if (status != PermissionStatus.Granted)
                status = await Permissions.RequestAsync<NearbyPermission>();

            if (status == PermissionStatus.Granted)
                return true;

            bool openSettings = await Shell.Current.DisplayAlertAsync(
                "Permission needed",
                "Train together needs the \"Nearby devices\" permission to find your partner's phone.",
                "Open settings", "Cancel");

            if (openSettings)
                AppInfo.ShowSettingsUI();

            return false;
#else
            await Task.CompletedTask;
            return false;
#endif
        }

        void OnTransportStateChanged(object? sender, TransportState state) =>
            MainThread.BeginInvokeOnMainThread(Refresh);

        void OnPairingRequested(object? sender, PairingRequest request) =>
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                bool accept = await Shell.Current.DisplayAlertAsync(
                    "Train together",
                    $"Pair with {request.PartnerName}?\n\nCode: {request.Code}\n\n" +
                    "Only accept if the same code is shown on their phone.",
                    "Pair", "Cancel");

                if (accept) request.Accept();
                else request.Reject();
            });

        void OnPartnerUpdated(object? sender, EventArgs e) =>
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                if (_coordinator.PartnerFinished && !_partnerEnded && !IsOff)
                {
                    _endedName = _transport.PartnerName ?? PartnerName;
                    _partnerEnded = true;

                    await _transport.StopAsync();
                    Refresh();
                }

                OnPropertyChanged(nameof(PartnerSnapshot));
            });

        void OnOwnSessionEnded(object? sender, EventArgs e) =>
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                if (IsOff) return;
                await StopSessionAsync();
            });

        void Refresh()
        {
            if (_partnerEnded)
            {
                PartnerName = _endedName;
                Status = TrainTogetherStatus.PartnerEnded;
                return;
            }

            PartnerName = _transport.PartnerName;

            Status = _transport.State switch
            {
                TransportState.Connected => TrainTogetherStatus.Connected,
                TransportState.Connecting => _transport.PartnerName is null
                    ? TrainTogetherStatus.Searching
                    : TrainTogetherStatus.Reconnecting,
                _ => _transport.ConnectionLost
                    ? TrainTogetherStatus.ConnectionLost
                    : TrainTogetherStatus.Off
            };

            if (!ShowToggle)
                ViewingPartner = false;

            OnPropertyChanged(nameof(PartnerSnapshot));
        }

        static Task ShowAlertAsync(string title, string message) =>
            MainThread.InvokeOnMainThreadAsync(() => Shell.Current.DisplayAlertAsync(title, message, "OK"));

        public void Dispose()
        {
            _transport.StateChanged -= OnTransportStateChanged;
            _transport.PairingRequested -= OnPairingRequested;
            _coordinator.PartnerUpdated -= OnPartnerUpdated;
            _publisher.SessionEnded -= OnOwnSessionEnded;
        }
    }
}