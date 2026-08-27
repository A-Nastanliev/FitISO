using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace FitISO.Maui.ViewModels
{
    public abstract partial class PagedCollectionViewModel<TRaw, TItem, TCursor> : ObservableObject
    {
        [ObservableProperty]
        ObservableCollection<TItem> items = new();

        [ObservableProperty]
        bool loading;

        protected TCursor cursor;

        protected bool canLoadMore = true;

        bool hasLoadedOnce;

        protected abstract int BatchSize { get; }

        protected PagedCollectionViewModel()
        {
            WeakReferenceMessenger.Default.RegisterAll(this);
        }

        protected abstract Task<IReadOnlyList<TRaw>> FetchBatchAsync(int batchSize, TCursor cursor);

        protected abstract TItem Wrap(TRaw raw);

        protected abstract TCursor GetCursor(TItem item);

        private bool CanStartLoading() => !Loading && canLoadMore;

        [RelayCommand]
        public virtual async Task Load()
        {
            if (!CanStartLoading()) return;

            Loading = true;

            try
            {
                var batch = await FetchBatchAsync(BatchSize, cursor);
                foreach (var raw in batch)
                    Items.Add(Wrap(raw));

                SyncCursorToTail();

                if (batch.Count < BatchSize)
                    canLoadMore = false;

                hasLoadedOnce = true;
                Loading = false;
            }
            catch (Exception ex)
            {
                Loading = false;
                await Shell.Current.DisplayAlertAsync(ex.Message, ex.InnerException?.ToString() ?? ex.Message, "OK");
            }
        }

        [RelayCommand]
        public async Task LoadFirst()
        {
            if (hasLoadedOnce) return;
            await Load();
        }

        protected void SyncCursorToTail()
            => cursor = Items.Count > 0 ? GetCursor(Items[^1]) : default;

        protected virtual void ResetPaging()
        {
            Items.Clear();
            cursor = default;
            canLoadMore = true;
            hasLoadedOnce = false;
        }
    }
}
