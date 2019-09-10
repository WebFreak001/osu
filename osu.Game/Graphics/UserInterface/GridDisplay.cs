// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using osuTK;
using osuTK.Graphics;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Lines;
using osu.Framework.Graphics.Primitives;

namespace osu.Game.Graphics.UserInterface
{
	public class GridDisplay : CompositeDrawable
    {
        private GridDisplayPart minor, major;

        public GridDisplay()
        {
            InternalChildren = new Drawable[]
            {
                minor = new GridDisplayPart
                {
                    PathRadius = 1,
                    GenerateMajor = false,
                    AutoSizeAxes = Axes.None,
                    RelativeSizeAxes = Axes.Both
                },
                major = new GridDisplayPart
                {
                    PathRadius = 2,
                    GenerateMajor = true,
                    AutoSizeAxes = Axes.None,
                    RelativeSizeAxes = Axes.Both
                }
            };
        }

        public float PathRadius
        {
            get => minor.PathRadius;
            set => minor.PathRadius = value;
        }

        public float MajorPathRadius
        {
            get => major.PathRadius;
            set => major.PathRadius = value;
        }

        public int StepsX
        {
            get => minor.StepsX;
            set
            {
                minor.StepsX = value;
                major.StepsX = value;
            }
        }
        public int StepsY
        {
            get => minor.StepsY;
            set
            {
                minor.StepsY = value;
                major.StepsY = value;
            }
        }

        public int MajorX
        {
            get => minor.MajorX;
            set
            {
                minor.MajorX = value;
                major.MajorX = value;
            }
        }
        public int MajorY
        {
            get => minor.MajorY;
            set
            {
                minor.MajorY = value;
                major.MajorY = value;
            }
        }

        public Color4 MinorColour
        {
            get => minor.Colour;
            set => minor.Colour = value;
        }

        public Color4 MajorColour
        {
            get => major.Colour;
            set => major.Colour = value;
        }
    }

    /// <summary>
    /// Represents a graphical rectangular grid of lines of equal distance.
    /// </summary>
    public class GridDisplayPart : Lines
    {
        private int stepsX = 10;
        public int StepsX
        {
            get => stepsX;
            set
            {
                if (stepsX == value) return;

                stepsX = value;
                Invalidate(Invalidation.DrawSize);
            }
        }
        private int stepsY = 10;
        public int StepsY
        {
            get => stepsY;
            set
            {
                if (stepsY == value) return;

                stepsY = value;
                Invalidate(Invalidation.DrawSize);
            }
        }

        private int majorX = 5;
        public int MajorX
        {
            get => majorX;
            set
            {
                if (majorX == value) return;

                majorX = value;
                Invalidate(Invalidation.DrawSize);
            }
        }
        private int majorY = 5;
        public int MajorY
        {
            get => majorY;
            set
            {
                if (majorY == value) return;

                majorY = value;
                Invalidate(Invalidation.DrawSize);
            }
        }

        public bool GenerateMajor { get; set; }

        protected override IEnumerable<Vector2> BoundingVertices
        {
            get
            {
                yield return new Vector2(0, 0);
                yield return new Vector2(DrawWidth, DrawHeight);
            }
        }

        protected override IEnumerable<Line> GenerateSegments()
        {
            float spacingX = DrawWidth / StepsX;
            float spacingY = DrawHeight / StepsY;

            for (int y = 0; y <= StepsY; y++)
            {
                bool major = (y % MajorY) == 0;
                if (GenerateMajor == major)
                    yield return new Line(new Vector2(0, y * spacingY), new Vector2(DrawWidth, y * spacingY));
            }

            for (int x = 0; x <= StepsX; x++)
            {
                bool major = (x % MajorX) == 0;
                if (GenerateMajor == major)
                    yield return new Line(new Vector2(x * spacingX, 0), new Vector2(x * spacingX, DrawHeight));
            }
        }
    }
}
