using CommunityToolkit.Mvvm.Messaging.Messages;
using FitISO.Maui.Models;

namespace FitISO.Maui.Messages
{
    public class WorkoutTemplateUpdatedMessage : ValueChangedMessage<Workout>
    {
        public WorkoutTemplateUpdatedMessage(Workout value) : base(value) { }
    }
}