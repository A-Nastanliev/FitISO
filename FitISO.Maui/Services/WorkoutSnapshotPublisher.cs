using FitISO.Maui.Models;
using FitISO.Maui.ViewModels;
using FitISO.Sharing.Session;
using FitISO.Sharing.Transport;

namespace FitISO.Maui.Services
{
    public sealed class WorkoutSnapshotPublisher : IDisposable
    {
        static readonly TimeSpan DebounceDelay = TimeSpan.FromMilliseconds(300);
        static readonly TimeSpan SettleDelay = TimeSpan.FromMilliseconds(400);

        readonly ActiveWorkoutViewModel _activeWorkout;
        readonly PartnerSessionCoordinator _coordinator;
        readonly SnapshotVersionSource _versions;
        CancellationTokenSource? _debounceCts;

        bool _sessionActive;

        public event EventHandler? SessionEnded;

        public WorkoutSnapshotPublisher(ActiveWorkoutViewModel activeWorkout, PartnerSessionCoordinator coordinator, SnapshotVersionSource versions)
        {
            _activeWorkout = activeWorkout;
            _coordinator = coordinator;
            _versions = versions;

            _activeWorkout.StateChanged += OnWorkoutStateChanged;
            _coordinator.ConnectionStateChanged += OnConnectionStateChanged;
        }

        static bool IsActive(Workout? workout) =>
            workout is not null && workout.Id != 0 && workout.EndTime is null;

        void OnWorkoutStateChanged(object? sender, EventArgs e)
        {
            if (_sessionActive && !IsActive(_activeWorkout.Workout))
            {
                EndSessionNow();
                return;
            }

            SchedulePublish();
        }

        void OnConnectionStateChanged(object? sender, TransportState state)
        {
            if (state == TransportState.Connected)
                MainThread.BeginInvokeOnMainThread(SchedulePublish);
        }

        void SchedulePublish()
        {
            _debounceCts?.Cancel();
            _debounceCts = new CancellationTokenSource();
            _ = PublishAfterDelayAsync(_debounceCts.Token);
        }

        async Task PublishAfterDelayAsync(CancellationToken token)
        {
            try
            {
                await Task.Delay(DebounceDelay, token);
            }
            catch (TaskCanceledException)
            {
                return;
            }

            if (token.IsCancellationRequested) return;

            var workout = _activeWorkout.Workout;

            if (!IsActive(workout))
            {
                if (_sessionActive)
                    EndSessionNow();
                return;
            }

            _sessionActive = true;
            await PublishSnapshotAsync(workout);
        }

        void EndSessionNow()
        {
            _debounceCts?.Cancel();
            _sessionActive = false;

            var workout = _activeWorkout.Workout;

            var finished = workout is not null && workout.Id != 0 && workout.EndTime is not null
                ? workout
                : null;

            _ = EndSessionAsync(finished);
        }

        async Task EndSessionAsync(Workout? finishedWorkout)
        {
            try
            {
                if (finishedWorkout is not null)
                    await PublishSnapshotAsync(finishedWorkout);

                await _coordinator.PublishSessionEndedAsync(_versions.Next());
                await Task.Delay(SettleDelay);
            }
            catch (Exception)
            {
            }

            SessionEnded?.Invoke(this, EventArgs.Empty);
        }

        Task PublishSnapshotAsync(Workout workout)
        {
            var snapshot = WorkoutSnapshotMapper.Map(
                workout,
                _activeWorkout.RestStartTime,
                _activeWorkout.RestIsStopped,
                _versions.Next());

            return _coordinator.PublishAsync(snapshot);
        }

        public void Dispose()
        {
            _debounceCts?.Cancel();
            _activeWorkout.StateChanged -= OnWorkoutStateChanged;
            _coordinator.ConnectionStateChanged -= OnConnectionStateChanged;
        }
    }
}