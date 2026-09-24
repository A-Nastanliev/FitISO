using CommunityToolkit.Mvvm.Messaging.Messages;

namespace FitISO.Maui.Messages
{
    public class HeatmapWeekStartsOnMondayChangedMessage : ValueChangedMessage<bool>
    {
        public HeatmapWeekStartsOnMondayChangedMessage(bool value) : base(value) { }
    }
}
