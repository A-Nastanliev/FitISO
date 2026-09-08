using CommunityToolkit.Mvvm.Messaging.Messages;

namespace FitISO.Maui.Messages
{
    public class AutoBackupCompletedMessage : ValueChangedMessage<DateTime>
    {
        public AutoBackupCompletedMessage(DateTime backupUtc) : base(backupUtc) { }
    }
}