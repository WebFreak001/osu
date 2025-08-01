﻿// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Extensions.LocalisationExtensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Cursor;
using osu.Framework.Input.Events;
using osu.Framework.Localisation;
using osu.Game.Beatmaps;
using osu.Game.Beatmaps.Drawables;
using osu.Game.Configuration;
using osu.Game.Graphics;
using osu.Game.Graphics.Sprites;
using osu.Game.Graphics.UserInterface;
using osu.Game.Rulesets;
using osu.Game.Rulesets.Mods;
using osu.Game.Utils;

namespace osu.Game.Overlays.Mods
{
    /// <summary>
    /// On the mod select overlay, this provides a local updating view of BPM, star rating and other
    /// difficulty attributes so the user can have a better insight into what mods are changing.
    /// </summary>
    public partial class BeatmapAttributesDisplay : ModFooterInformationDisplay, IHasCustomTooltip<AdjustedAttributesTooltip.Data?>
    {
        private StarRatingDisplay starRatingDisplay = null!;
        private BPMDisplay bpmDisplay = null!;

        private VerticalAttributeDisplay circleSizeDisplay = null!;
        private VerticalAttributeDisplay drainRateDisplay = null!;
        private VerticalAttributeDisplay approachRateDisplay = null!;
        private VerticalAttributeDisplay overallDifficultyDisplay = null!;

        public Bindable<IBeatmapInfo?> BeatmapInfo { get; } = new Bindable<IBeatmapInfo?>();

        public Bindable<IReadOnlyList<Mod>> Mods { get; } = new Bindable<IReadOnlyList<Mod>>();

        public BindableBool Collapsed { get; } = new BindableBool(true);

        private ModSettingChangeTracker? modSettingChangeTracker;

        [Resolved]
        private BeatmapDifficultyCache difficultyCache { get; set; } = null!;

        [Resolved]
        private OsuGameBase game { get; set; } = null!;

        protected IBindable<RulesetInfo> GameRuleset = null!;

        private CancellationTokenSource? cancellationSource;
        private IBindable<StarDifficulty> starDifficulty = null!;

        public ITooltip<AdjustedAttributesTooltip.Data?> GetCustomTooltip() => new AdjustedAttributesTooltip();

        public AdjustedAttributesTooltip.Data? TooltipContent { get; private set; }

        private const float transition_duration = 250;

        [BackgroundDependencyLoader]
        private void load()
        {
            LeftContent.AddRange(new Drawable[]
            {
                starRatingDisplay = new StarRatingDisplay(default, animated: true)
                {
                    Origin = Anchor.CentreLeft,
                    Anchor = Anchor.CentreLeft,
                    Shear = -OsuGame.SHEAR,
                },
                bpmDisplay = new BPMDisplay
                {
                    Origin = Anchor.CentreLeft,
                    Anchor = Anchor.CentreLeft,
                    Shear = -OsuGame.SHEAR,
                    AutoSizeAxes = Axes.Y,
                    Width = 75,
                }
            });

            RightContent.Alpha = 0;
            RightContent.AddRange(new Drawable[]
            {
                circleSizeDisplay = new VerticalAttributeDisplay("CS") { Shear = -OsuGame.SHEAR, },
                approachRateDisplay = new VerticalAttributeDisplay("AR") { Shear = -OsuGame.SHEAR, },
                overallDifficultyDisplay = new VerticalAttributeDisplay("OD") { Shear = -OsuGame.SHEAR, },
                drainRateDisplay = new VerticalAttributeDisplay("HP") { Shear = -OsuGame.SHEAR, },
            });
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            Mods.BindValueChanged(_ =>
            {
                modSettingChangeTracker?.Dispose();
                modSettingChangeTracker = new ModSettingChangeTracker(Mods.Value);
                modSettingChangeTracker.SettingChanged += _ => updateValues();
                updateValues();
            }, true);

            Collapsed.BindValueChanged(_ =>
            {
                // Only start autosize animations on first collapse toggle. This avoids an ugly initial presentation.
                startAnimating();
                updateCollapsedState();
            });

            GameRuleset = game.Ruleset.GetBoundCopy();
            GameRuleset.BindValueChanged(_ => updateValues());

            BeatmapInfo.BindValueChanged(_ =>
            {
                updateStarDifficultyBindable();
                updateValues();
            }, true);

            updateCollapsedState();
        }

        private void updateStarDifficultyBindable()
        {
            cancellationSource?.Cancel();

            if (BeatmapInfo.Value == null)
                return;

            starDifficulty = difficultyCache.GetBindableDifficulty(BeatmapInfo.Value, (cancellationSource = new CancellationTokenSource()).Token);
            starDifficulty.BindValueChanged(s =>
            {
                starRatingDisplay.Current.Value = s.NewValue;

                if (!starRatingDisplay.IsPresent)
                    starRatingDisplay.FinishTransforms(true);
            });
        }

        protected override bool OnHover(HoverEvent e)
        {
            startAnimating();
            updateCollapsedState();
            return true;
        }

        protected override void OnHoverLost(HoverLostEvent e)
        {
            updateCollapsedState();
            base.OnHoverLost(e);
        }

        protected override bool OnMouseDown(MouseDownEvent e) => true;

        protected override bool OnClick(ClickEvent e) => true;

        private void startAnimating()
        {
            LeftContent.AutoSizeEasing = Content.AutoSizeEasing = Easing.OutQuint;
            LeftContent.AutoSizeDuration = Content.AutoSizeDuration = transition_duration;
        }

        private void updateValues() => Scheduler.AddOnce(() =>
        {
            if (BeatmapInfo.Value == null)
                return;

            double rate = ModUtils.CalculateRateWithMods(Mods.Value);

            bpmDisplay.Current.Value = FormatUtils.RoundBPM(BeatmapInfo.Value.BPM, rate);

            BeatmapDifficulty originalDifficulty = new BeatmapDifficulty(BeatmapInfo.Value.Difficulty);
            BeatmapDifficulty adjustedDifficulty = new BeatmapDifficulty(originalDifficulty);

            foreach (var mod in Mods.Value.OfType<IApplicableToDifficulty>())
                mod.ApplyToDifficulty(adjustedDifficulty);

            Ruleset ruleset = GameRuleset.Value.CreateInstance();
            adjustedDifficulty = ruleset.GetAdjustedDisplayDifficulty(adjustedDifficulty, Mods.Value);

            TooltipContent = new AdjustedAttributesTooltip.Data(originalDifficulty, adjustedDifficulty);

            circleSizeDisplay.AdjustType.Value = VerticalAttributeDisplay.CalculateEffect(originalDifficulty.CircleSize, adjustedDifficulty.CircleSize);
            drainRateDisplay.AdjustType.Value = VerticalAttributeDisplay.CalculateEffect(originalDifficulty.DrainRate, adjustedDifficulty.DrainRate);
            approachRateDisplay.AdjustType.Value = VerticalAttributeDisplay.CalculateEffect(originalDifficulty.ApproachRate, adjustedDifficulty.ApproachRate);
            overallDifficultyDisplay.AdjustType.Value = VerticalAttributeDisplay.CalculateEffect(originalDifficulty.OverallDifficulty, adjustedDifficulty.OverallDifficulty);

            circleSizeDisplay.Current.Value = adjustedDifficulty.CircleSize;
            drainRateDisplay.Current.Value = adjustedDifficulty.DrainRate;
            approachRateDisplay.Current.Value = adjustedDifficulty.ApproachRate;
            overallDifficultyDisplay.Current.Value = adjustedDifficulty.OverallDifficulty;
        });

        private void updateCollapsedState()
        {
            RightContent.FadeTo(Collapsed.Value && !IsHovered ? 0 : 1, transition_duration, Easing.OutQuint);
        }

        public partial class BPMDisplay : RollingCounter<int>
        {
            protected override double RollingDuration => 250;

            protected override LocalisableString FormatCount(int count) => count.ToLocalisableString("0 BPM");

            protected override OsuSpriteText CreateSpriteText() => new OsuSpriteText
            {
                Anchor = Anchor.CentreRight,
                Origin = Anchor.CentreRight,
                Font = OsuFont.Default.With(size: 20, weight: FontWeight.SemiBold),
                UseFullGlyphHeight = false,
            };
        }
    }
}
