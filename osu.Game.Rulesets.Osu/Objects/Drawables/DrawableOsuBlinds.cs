// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu/master/LICENCE

using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.Textures;
using osu.Game.Beatmaps;
using osu.Game.Skinning;
using osu.Game.Rulesets.Osu.Objects.Drawables.Pieces;
using osuTK;
using osuTK.Graphics;

namespace osu.Game.Rulesets.Osu.Objects.Drawables
{
    /// <summary>
    /// Element for the Blinds mod drawing 2 black boxes covering the whole screen which resize inside a restricted area with some leniency.
    /// </summary>
    public class DrawableOsuBlinds : Container
    {
        /// <summary>
        /// Black background boxes behind blind panel textures.
        /// </summary>
        private Box blackBoxLeft, blackBoxRight;
        private Sprite panelLeft, panelRight;
        private Sprite bgPanelLeft, bgPanelRight;

        private const float npc_movement_start = 1.5f;
        private float npcPosition = npc_movement_start;
        private bool animatingBlinds;

        private readonly Beatmap<OsuHitObject> beatmap;

        private ISkinSource skin;

        private float targetClamp = 1;
        private readonly float targetBreakMultiplier = 0;
        private float target = 1;
        private readonly float easing = 1;

        private const float black_depth = 10;
        private const float bg_panel_depth = 8;
        private const float fg_panel_depth = 4;
        private const float npc_depth = 6;

        private readonly CompositeDrawable restrictTo;
        private readonly bool modEasy, modHardrock;

        /// <summary>
        /// <para>
        /// Percentage of playfield to extend blinds over. Basically moves the origin points where the blinds start.
        /// </para>
        /// <para>
        /// -1 would mean the blinds always cover the whole screen no matter health.
        /// 0 would mean the blinds will only ever be on the edge of the playfield on 0% health.
        /// 1 would mean the blinds are fully outside the playfield on 50% health.
        /// Infinity would mean the blinds are always outside the playfield except on 100% health.
        /// </para>
        /// </summary>
        private const float leniency = 0.1f;

        public DrawableOsuBlinds(CompositeDrawable restrictTo, bool hasEasy, bool hasHardrock, Beatmap<OsuHitObject> beatmap)
        {
            this.restrictTo = restrictTo;
            this.beatmap = beatmap;

            modEasy = hasEasy;
            modHardrock = hasHardrock;
        }

        [BackgroundDependencyLoader]
        private void load(ISkinSource skin, TextureStore textures)
        {
            RelativeSizeAxes = Axes.Both;
            Width = 1;
            Height = 1;

            Add(blackBoxLeft = new Box
            {
                Anchor = Anchor.TopLeft,
                Origin = Anchor.TopLeft,
                Colour = Color4.Black,
                RelativeSizeAxes = Axes.Y,
                Width = 0,
                Height = 1,
                Depth = black_depth
            });
            Add(blackBoxRight = new Box
            {
                Anchor = Anchor.TopRight,
                Origin = Anchor.TopRight,
                Colour = Color4.Black,
                RelativeSizeAxes = Axes.Y,
                Width = 0,
                Height = 1,
                Depth = black_depth
            });

            Add(bgPanelLeft = new ModBlindsPanelSprite {
                Origin = Anchor.TopRight,
                Colour = Color4.Gray,
                Depth = bg_panel_depth + 1
            });
            Add(panelLeft = new ModBlindsPanelSprite {
                Origin = Anchor.TopRight,
                Depth = bg_panel_depth
            });

            Add(bgPanelRight = new ModBlindsPanelSprite {
                Origin = Anchor.TopLeft,
                Colour = Color4.Gray,
                Depth = fg_panel_depth + 1
            });
            Add(panelRight = new ModBlindsPanelSprite {
                Origin = Anchor.TopLeft,
                Depth = fg_panel_depth
            });

            this.skin = skin;
            skin.SourceChanged += skinChanged;
            PanelTexture = textures.Get("Play/osu/blinds-panel");
        }

        private void skinChanged()
        {
            PanelTexture = skin.GetTexture("Play/osu/blinds-panel");
        }

        private float applyGap(float value)
        {
            const float easy_multiplier = 0.95f;
            const float hardrock_multiplier = 1.1f;

            float multiplier = 1;
            if (modEasy)
            {
                multiplier = easy_multiplier;
                // TODO: include OD/CS
            }
            else if (modHardrock)
            {
                multiplier = hardrock_multiplier;
                // TODO: include OD/CS
            }

            return MathHelper.Clamp(value * multiplier, 0, targetClamp) * targetBreakMultiplier;
        }

        private static float applyAdjustmentCurve(float value)
        {
            // lagrange polinominal for (0,0) (0.5,0.35) (1,1) should make a good curve
            return 0.6f * value * value + 0.4f * value;
        }

        protected override void Update()
        {
            float start = Parent.ToLocalSpace(restrictTo.ScreenSpaceDrawQuad.TopLeft).X;
            float end = Parent.ToLocalSpace(restrictTo.ScreenSpaceDrawQuad.TopRight).X;
            float rawWidth = end - start;
            start -= rawWidth * leniency * 0.5f;
            end += rawWidth * leniency * 0.5f;

            float width = (end - start) * 0.5f * applyAdjustmentCurve(applyGap(easing));
            // different values in case the playfield ever moves from center to somewhere else.
            blackBoxLeft.Width = start + width;
            blackBoxRight.Width = DrawWidth - end + width;

            panelLeft.X = start + width;
            panelRight.X = end - width;
            bgPanelLeft.X = start;
            bgPanelRight.X = end;
        }

        protected override void LoadComplete()
        {
            const float break_open_early = 500;
            const float break_close_late = 250;

            base.LoadComplete();

            var firstObj = beatmap.HitObjects[0];
            var startDelay = firstObj.StartTime - firstObj.TimePreempt;

            using (BeginAbsoluteSequence(startDelay + break_close_late, true))
                LeaveBreak();

            foreach (var breakInfo in beatmap.Breaks)
            {
                if (breakInfo.HasEffect)
                {
                    using (BeginAbsoluteSequence(breakInfo.StartTime - break_open_early, true))
                    {
                        EnterBreak();
                        using (BeginDelayedSequence(breakInfo.Duration + break_open_early + break_close_late, true))
                            LeaveBreak();
                    }
                }
            }
        }

        public void EnterBreak()
        {
            this.TransformTo(nameof(targetBreakMultiplier), 0f, 1000, Easing.OutSine);
        }

        public void LeaveBreak()
        {
            this.TransformTo(nameof(targetBreakMultiplier), 1f, 2500, Easing.OutBounce);
        }

        /// <summary>
        /// Health between 0 and 1 for the blinds to base the width on. Will get animated for 200ms using out-quintic easing.
        /// </summary>
        public void AnimateTarget(float value)
        {
            target = value;
            this.TransformTo(nameof(easing), target, 200, Easing.OutQuint);
        }

        public float Target
        {
            get => target;
        }

        public Texture PanelTexture
        {
            set
            {
                panelLeft.Texture = value;
                panelRight.Texture = value;
                bgPanelLeft.Texture = value;
                bgPanelRight.Texture = value;
            }
        }
    }
}