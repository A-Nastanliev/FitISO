using CommunityToolkit.Mvvm.Messaging.Messages;
using FitISO.Maui.Models;


namespace FitISO.Maui.Messages
{
    public class ExerciseUpdatedMessage : ValueChangedMessage<Exercise>
    {
        public ExerciseUpdatedMessage(Exercise exercise) : base(exercise) { }
    }
}
