using CommunityToolkit.Mvvm.Messaging.Messages;
using FitISO.Maui.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace FitISO.Maui.Messages
{
    public class WorkoutTemplateCreatedMessage : ValueChangedMessage<Workout>
    {
        public WorkoutTemplateCreatedMessage(Workout workout) : base(workout) { }
    }
}
