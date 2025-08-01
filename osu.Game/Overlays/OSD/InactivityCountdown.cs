using System;
using osu.Framework.Graphics;
using osu.Framework.Screens;
using osu.Game.Graphics;
using osu.Game.Graphics.Sprites;
using osuTK;

namespace osu.Game.Overlays.OSD
{
    public class InactivityCountdown : OsuSpriteText
    {
        public Action? TimeoutElapsed { private get; init; }
        public IScreen? Screen { get; }

        private int numSeconds = 0;
        private int displayedSeconds = 0;
        private double startTime = 0;

        public InactivityCountdown(IScreen? screen = null, int numSeconds = 90)
        {
            this.numSeconds = numSeconds;
            Screen = screen;
            displayedSeconds = numSeconds;
            Anchor = Anchor.BottomLeft;
            Origin = Anchor.BottomLeft;
            RelativePositionAxes = Axes.Both;
            Position = new Vector2(0.02f, -0.02f);
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

                    if (Screen != null && Screen.IsCurrentScreen())
                        Screen.Exit();
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
