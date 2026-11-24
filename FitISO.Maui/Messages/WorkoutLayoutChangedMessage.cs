using CommunityToolkit.Mvvm.Messaging.Messages;
using FitISO.Maui.Models;

namespace FitISO.Maui.Messages
{
    public class WorkoutLayoutChangedMessage : ValueChangedMessage<WorkoutLayout>
    {
        public WorkoutLayoutChangedMessage(WorkoutLayout value) : base(value)
        {
        }
    }
}