using System;
using osu.Game.Graphics;
using osu.Game.Graphics.Sprites;

namespace osu.Game.Overlays.OSD
{
    public class InactivityCountdown : OsuSpriteText
    {
        public Action? TimeoutElapsed { private get; init; }

        private int numSeconds = 0;
        private int displayedSeconds = 0;
        private double startTime = 0;

        public InactivityCountdown(int numSeconds = 90)
        {
            this.numSeconds = numSeconds;
            displayedSeconds = numSeconds;
            Text = numSeconds.ToString();
            Font = OsuFont.GetFont(Typeface.Torus, size: 100, fixedWidth: true);
        }

        public void Reset()
        {
            Text = numSeconds.ToString();
            displayedSeconds = numSeconds;
            startTime = 0;
        }

        protected override void Update()
        {
            base.Update();

            if (startTime == 0)
                startTime = Clock.CurrentTime;

            int remaining = numSeconds - (int)((Clock.CurrentTime - startTime) / 1000);

            if (remaining < 0)
            {
                if (displayedSeconds != -1)
                {
                    TimeoutElapsed?.Invoke();
                    displayedSeconds = -1;
                }
            }
            else if (remaining != displayedSeconds)
            {
                displayedSeconds = remaining;
                this.Text = remaining.ToString();
            }
        }
    }
}
