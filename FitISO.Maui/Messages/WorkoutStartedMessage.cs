using CommunityToolkit.Mvvm.Messaging.Messages;
using FitISO.Maui.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace FitISO.Maui.Messages
{
    public class WorkoutStartedMessage : ValueChangedMessage<Workout>
    {
        public WorkoutStartedMessage(Workout workout) : base(workout) { }
    }
}
