using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace FitISO.Maui.Models
{
    public partial class Set : ObservableObject
    {
        [ObservableProperty]
        int id;
        [ObservableProperty]
        double? weight;
        [ObservableProperty]
        double? reps;

        [System.Text.Json.Serialization.JsonIgnore]
        public Func<Set, Task>? SaveAction { get; set; }

        static readonly TimeSpan DebounceDelay = TimeSpan.FromSeconds(1.2);
        CancellationTokenSource? _debounceCts;

        public event EventHandler? JustCompleted;

        bool _wasCompleteBeforeChange;

        public bool IsComplete => Weight is >= 0 && Reps is > 0;

        public Set()
        {

        }

        public Set(FitISO.Data.Models.Set set)
        {
            Id = set.Id;
            Weight = set.Weight;
            Reps = set.Reps;
        }

        partial void OnWeightChanging(double? value) => _wasCompleteBeforeChange = IsComplete;
        partial void OnRepsChanging(double? value) => _wasCompleteBeforeChange = IsComplete;

        partial void OnWeightChanged(double? value)
        {
            if (value < 0)
                Weight = null;

            DebounceSave();
            RaiseJustCompletedIfNeeded();
        }
        partial void OnRepsChanged(double? value)
        {
            if (value < 0)
                Reps = null;

            DebounceSave();
            RaiseJustCompletedIfNeeded();
        }

        void RaiseJustCompletedIfNeeded()
        {
            if (!_wasCompleteBeforeChange && IsComplete)
                JustCompleted?.Invoke(this, EventArgs.Empty);
        }

        void DebounceSave()
        {
            if (SaveAction is null) return;

            _debounceCts?.Cancel();
            _debounceCts = new CancellationTokenSource();
            _ = DebounceSaveAsync(_debounceCts.Token);
        }

        async Task DebounceSaveAsync(CancellationToken token)
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

            await SaveAction!.Invoke(this);
        }
        public void CancelPendingSave() => _debounceCts?.Cancel();

    }
}
