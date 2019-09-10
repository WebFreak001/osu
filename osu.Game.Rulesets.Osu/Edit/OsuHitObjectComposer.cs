// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Game.Beatmaps;
using osu.Game.Graphics.Sprites;
using osu.Game.Graphics.UserInterface;
using osu.Game.Rulesets.Edit;
using osu.Game.Rulesets.Edit.Tools;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Objects.Drawables;
using osu.Game.Rulesets.Osu.Edit.Blueprints.HitCircles;
using osu.Game.Rulesets.Osu.Edit.Blueprints.Sliders;
using osu.Game.Rulesets.Osu.Edit.Blueprints.Spinners;
using osu.Game.Rulesets.Osu.Objects;
using osu.Game.Rulesets.Osu.Objects.Drawables;
using osu.Game.Rulesets.UI;
using osu.Game.Screens.Edit.Compose.Components;
using osuTK;

namespace osu.Game.Rulesets.Osu.Edit
{
    public class OsuHitObjectComposer : HitObjectComposer<OsuHitObject>
    {
        private readonly List<Drawable> sliderSettings = new List<Drawable>();
        private readonly List<Drawable> circleSettings = new List<Drawable>();
        private readonly List<Drawable> spinnerSettings = new List<Drawable>();

        private CurvesEditor sliderBallPathEditor;
        private bool changingProperties;

        public OsuHitObjectComposer(Ruleset ruleset)
            : base(ruleset)
        {
        }

        protected override void FillObjectProperties()
        {
            sliderSettings.Add(new OsuSpriteText
            {
                Text = "Slider ball path"
            });
            sliderSettings.Add(sliderBallPathEditor = new CurvesEditor
            {
                RelativeSizeAxes = Axes.X,
                Width = 1,
                Height = 200
            });
            sliderBallPathEditor.Current.ValueChanged += updateSliderInterpolations;

            foreach (var setting in sliderSettings.Concat(spinnerSettings).Concat(circleSettings))
            {
                setting.Hide();
                ObjectProperties.Add(setting);
            }
        }

        protected override DrawableRuleset<OsuHitObject> CreateDrawableRuleset(Ruleset ruleset, WorkingBeatmap beatmap, IReadOnlyList<Mod> mods)
            => new DrawableOsuEditRuleset(ruleset, beatmap, mods);

        protected override IReadOnlyList<HitObjectCompositionTool> CompositionTools => new HitObjectCompositionTool[]
        {
            new HitCircleCompositionTool(),
            new SliderCompositionTool(),
            new SpinnerCompositionTool()
        };

        public override SelectionHandler CreateSelectionHandler() => new OsuSelectionHandler();

        public override SelectionBlueprint CreateBlueprintFor(DrawableHitObject hitObject)
        {
            switch (hitObject)
            {
                case DrawableHitCircle circle:
                    return new HitCircleSelectionBlueprint(circle);

                case DrawableSlider slider:
                    return new SliderSelectionBlueprint(slider);

                case DrawableSpinner spinner:
                    return new SpinnerSelectionBlueprint(spinner);
            }

            return base.CreateBlueprintFor(hitObject);
        }

        protected override void SelectionChanged()
        {
            foreach (var member in ObjectProperties.Children)
                member.Hide();

            Console.WriteLine("Selection changed to: " + string.Join(", ", Selection.Select(a => a.HitObject.HitObject.GetType().Name)));

            if (!Selection.Any())
                return;

            changingProperties = true;
            if (Selection.All(item => item.HitObject.HitObject is Slider))
            {
                foreach (var option in sliderSettings)
                    option.Show();
                loadSliderSettings(Selection);
            }
            else if (Selection.All(item => item.HitObject.HitObject is Spinner))
            {
                foreach (var option in spinnerSettings)
                    option.Show();
                loadSpinnerSettings(Selection);
            }
            else if (Selection.All(item => item.HitObject.HitObject is HitCircle))
            {
                foreach (var option in circleSettings)
                    option.Show();
                loadCircleSettings(Selection);
            }
            changingProperties = false;
        }

        private void loadSliderSettings(IEnumerable<SelectionBlueprint> selection)
        {
            var points = (selection.Last().HitObject.HitObject as Slider).Path.InterpolationPoints;
            if (points != null)
            {
                sliderBallPathEditor.Current.Value = new List<Vector2>(points.ToArray());
            }
            else
            {
                sliderBallPathEditor.Current.Value = new List<Vector2> { Vector2.Zero, Vector2.One };
            }
        }

        private void loadSpinnerSettings(IEnumerable<SelectionBlueprint> selection)
        {
        }

        private void loadCircleSettings(IEnumerable<SelectionBlueprint> selection)
        {
        }

        private void updateSliderInterpolations(ValueChangedEvent<List<Vector2>> args)
        {
            Console.WriteLine("changed slider");
            if (changingProperties)
                return;

            Console.WriteLine("saving slider path");
            foreach (var item in Selection)
            {
                var prev = ((Slider) item?.HitObject?.HitObject)?.Path;
                if (prev == null)
                    continue;
                Console.WriteLine("for object " + item);
                (item.HitObject.HitObject as Slider).Path = new SliderPath(prev.Value.Type, prev.Value.ControlPoints.ToArray(), prev.Value.ExpectedDistance, args.NewValue.ToArray());
            }
        }
    }
}
