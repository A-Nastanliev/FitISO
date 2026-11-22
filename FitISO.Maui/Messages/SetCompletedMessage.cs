using CommunityToolkit.Mvvm.Messaging.Messages;
using FitISO.Maui.Models;

namespace FitISO.Maui.Messages
{
    public class SetCompletedMessage : ValueChangedMessage<Set>
    {
        public SetCompletedMessage(Set set) : base(set) { }
    }
}