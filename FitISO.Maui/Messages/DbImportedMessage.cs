using CommunityToolkit.Mvvm.Messaging.Messages;

namespace FitISO.Maui.Messages
{
    public class DbImportedMessage : ValueChangedMessage<bool>
    {
        public DbImportedMessage() : base(true)
        {
        }
    }
}
