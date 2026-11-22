using CommunityToolkit.Mvvm.Messaging.Messages;
using FitISO.Maui.Models;

namespace FitISO.Maui.Messages
{
    public class WorkoutExerciseRemovedMessage : ValueChangedMessage<WorkoutExercise>
    {
        public WorkoutExerciseRemovedMessage(WorkoutExercise exercise) : base(exercise) { }
    }
}
