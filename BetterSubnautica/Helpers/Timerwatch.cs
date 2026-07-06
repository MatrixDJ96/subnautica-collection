using System.Diagnostics;

namespace BetterSubnautica.Helpers
{
    public class Timerwatch : Stopwatch
    {
        public bool ForceFinished { get; set; }

        private long Milliseconds { get; set; }

        public Timerwatch(int seconds, bool forceFinished = false)
        {
            Milliseconds = seconds * 1000;
            ForceFinished = forceFinished;
        }

        public new void Restart()
        {
            ForceFinished = false;
            base.Restart();
        }

        public bool IsFinished() => ForceFinished || ElapsedMilliseconds == 0 || ElapsedMilliseconds >= Milliseconds;
    }
}
