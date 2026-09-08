using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Mvvm.Messaging;
using FitISO.Maui.Messages;
using Microsoft.Data.Sqlite;
#if ANDROID
using Android.Content;
using Android.OS;
using Android.Provider;
using AndroidUri = Android.Net.Uri;
#endif

namespace FitISO.Maui.Services
{
    public class AutoBackupService : IRecipient<WorkoutFinishedMessage>
    {
        public const string AutoSaveEnabledKey = "auto_save_enabled";
        public const string LastBackupUtcKey = "last_export_utc";

        public AutoBackupService()
        {
            WeakReferenceMessenger.Default.Register(this);
        }

        public void Receive(WorkoutFinishedMessage message)
        {
            if (!Preferences.Get(AutoSaveEnabledKey, false))
                return;

            _ = RunAutoBackupAsync();
        }

        async Task RunAutoBackupAsync()
        {
            var success = await Task.Run(TrySaveToPublicDownloadsAsync);

            if (!success)
                return;

            var backupUtc = DateTime.UtcNow;
            Preferences.Set(LastBackupUtcKey, backupUtc.ToString("O"));

            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                WeakReferenceMessenger.Default.Send(new AutoBackupCompletedMessage(backupUtc));
                _ = Toast.Make("Backup auto-saved").Show();
            });
        }

        async Task<bool> TrySaveToPublicDownloadsAsync()
        {
#if ANDROID
            try
            {
                var dbPath = App.DatabasePath;
                if (!File.Exists(dbPath))
                    return false;

                SqliteConnection.ClearAllPools();

                var fileName = $"fitiso_backup_{DateTime.Now:yyyyMMdd_HHmmss}.db3";

                return await SaveToAndroidDownloadsAsync(dbPath, fileName);
            }
            catch
            {
                return false;
            }
#else
            await Task.CompletedTask;
            return false;
#endif
        }

#if ANDROID
        static async Task<bool> SaveToAndroidDownloadsAsync(string sourceDbPath, string fileName)
        {
            var context = global::Android.App.Application.Context;
            var resolver = context.ContentResolver;
            if (resolver is null) return false;

            var values = new ContentValues();
            values.Put(MediaStore.MediaColumns.DisplayName, fileName);
            values.Put(MediaStore.MediaColumns.MimeType, "application/octet-stream");

            AndroidUri? collection;

            if (Build.VERSION.SdkInt >= BuildVersionCodes.Q)
            {
                values.Put(MediaStore.MediaColumns.RelativePath, global::Android.OS.Environment.DirectoryDownloads);
                collection = MediaStore.Downloads.ExternalContentUri;
            }
            else
            {
                var downloadsDir = global::Android.OS.Environment.GetExternalStoragePublicDirectory(
                    global::Android.OS.Environment.DirectoryDownloads);
                values.Put(MediaStore.MediaColumns.Data, Path.Combine(downloadsDir!.AbsolutePath, fileName));
                collection = MediaStore.Files.GetContentUri("external");
            }

            var itemUri = resolver.Insert(collection!, values);
            if (itemUri is null) return false;

            await using var outputStream = resolver.OpenOutputStream(itemUri);
            if (outputStream is null) return false;

            await using var inputStream = File.OpenRead(sourceDbPath);
            await inputStream.CopyToAsync(outputStream);

            return true;
        }
#endif
    }
}