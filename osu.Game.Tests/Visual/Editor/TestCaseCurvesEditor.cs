// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using NUnit.Framework;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Game.Graphics.UserInterface;
using osu.Game.Overlays;
using osu.Game.Overlays.Settings;
using osuTK;
using osuTK.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;

namespace osu.Game.Tests.Visual.Editor
{
    public class TestCaseCurvesEditor : OsuTestScene
    {
        [BackgroundDependencyLoader]
        private void load()
        {
            CurvesEditor editor;

            Children = new[]
            {
                editor = new CurvesEditor
                {
                    RelativeSizeAxes = Axes.None,
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                    Size = new Vector2(200, 200)
                },
            };
        }
    }
}
