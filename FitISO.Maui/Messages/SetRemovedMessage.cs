using CommunityToolkit.Mvvm.Messaging.Messages;
using FitISO.Maui.Models;

namespace FitISO.Maui.Messages
{
    public class SetRemovedMessage : ValueChangedMessage<WorkoutExercise>
    {
        public SetRemovedMessage(WorkoutExercise exercise) : base(exercise) { }
    }
}