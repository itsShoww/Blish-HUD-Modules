using System.Timers;
namespace Nekres.Music_Mixer
{
    public static class TimerExtensions
    {
        public static void Restart(this Timer timer) {
            if (!timer.Enabled)
                timer.Start();
            // http://msdn.microsoft.com/en-us/library/system.timers.timer.enabled.aspx
            timer.Interval = timer.Interval;
        }
    }
}
