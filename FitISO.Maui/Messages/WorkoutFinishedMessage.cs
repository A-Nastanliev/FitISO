using CommunityToolkit.Mvvm.Messaging.Messages;
using FitISO.Maui.Models;

namespace FitISO.Maui.Messages
{
    public class WorkoutFinishedMessage : ValueChangedMessage<Workout>
    {
        public WorkoutFinishedMessage(Workout workout) : base(workout) { }
    }
}
