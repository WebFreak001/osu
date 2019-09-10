// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using osuTK;
using osuTK.Graphics;
using osu.Framework.Bindables;
using osu.Framework.Configuration;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.UserInterface;
using osu.Framework.MathUtils;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Lines;
using osu.Framework.Input.Events;

namespace osu.Game.Graphics.UserInterface
{
    public class CurvesEditor : Container, IHasCurrentValue<List<Vector2>>
    {
        private Bindable<List<Vector2>> current = new Bindable<List<Vector2>>();

        public Bindable<List<Vector2>> Current
        {
            get => current;
            set
            {
                if (value == null)
                    throw new ArgumentNullException(nameof(value));

                current.UnbindBindings();
                current.BindTo(value);
            }
        }

        private ClickReceivingPath path;
        public Path Path => path;

        private bool endsAllowNon01 = true;
        public bool EndsAllowNon01
        {
            get => endsAllowNon01;
            set
            {
                if (value == endsAllowNon01)
                    return;

                endsAllowNon01 = value;
                Invalidate(Invalidation.DrawSize);
            }
        }

        private Container points;

        private Drawable dragProxy;

        private float clickRadius = 8;

        public CurvesEditor()
        {
            current.Default = new List<Vector2>() { Vector2.Zero, Vector2.One };

            Children = new Drawable[]
            {
                new GridDisplay
                {
                    StepsX = 8,
                    StepsY = 8,
                    MajorX = 4,
                    MajorY = 4,
                    PathRadius = 1,
                    MajorPathRadius = 1,
                    MinorColour = new Color4(1, 1, 1, 0.6f),
                    MajorColour = Color4.White,
                    RelativeSizeAxes = Axes.Both,
                    Size = Vector2.One
                },
                new SmoothPath
                {
                    AutoSizeAxes = Axes.None,
                    RelativeSizeAxes = Axes.Both,
                    VertexRelativePositionAxes = Axes.Both,
                    Size = Vector2.One,
                    PathRadius = 1,
                    Colour = new Color4(1, 1, 1, 0.4f),
                    Vertices = new List<Vector2>() { new Vector2(0, 1), new Vector2(1, 0) },

                    RelativePositionAxes = Axes.None,
                    Position = new Vector2(-1, -1)
                },
                path = new ClickReceivingPath
                {
                    AutoSizeAxes = Axes.None,
                    RelativeSizeAxes = Axes.Both,
                    VertexRelativePositionAxes = Axes.Both,
                    Size = Vector2.One,
                    PathRadius = 2,
                    Colour = Colour,
                    Vertices = new List<Vector2>() { new Vector2(0, 1), new Vector2(1, 0) },

                    RelativePositionAxes = Axes.None,
                    Position = new Vector2(-2, -2)
                },
                points = new Container
                {
                    RelativeSizeAxes = Axes.Both,
                    Size = Vector2.One
                }
            };

            path.Clicked = (e) => OnClick(e);

            current.ValueChanged += value => Recompute();
            current.SetDefault();
            Recompute();
        }

        public void Recompute()
        {
            // some combining IEnumerable + ReadOnlySpan type would be nice to avoid array allocation here
            var visual = PathApproximator.ApproximateLagrangePolynomial(current.Value.ToArray());
            // need to allocate a second list here because we can't modify the existing collection while using the ClampY iterator
            List<Vector2> clamped = new List<Vector2>(visual.Count);
            foreach (Vector2 o in PathApproximator.ClampY(visual, 0, 1))
                clamped.Add(new Vector2(o.X, (1 - o.Y)));
            path.Vertices = clamped;

            if (points.Children.Count > current.Value.Count)
            {
                points.RemoveRange(points.Children.Skip(current.Value.Count));
            }
            else
            {
                while (points.Children.Count < current.Value.Count)
                {
                    points.Add(makeDragCircle(points.Children.Count));
                }
            }

            var p = current.Value;
            for (int i = 0; i < p.Count; i++)
            {
                var position = p[i];
                var circle = points.Children[i] as DragCircle;

                circle.EndsAllowNon01 = endsAllowNon01;
                circle.Index = i;
                circle.IsLast = i == p.Count - 1;
                circle.X = position.X;
                circle.Y = 1 - position.Y;
            }
        }

        private bool checkLineInput(Vector2 screenSpacePos)
        {
            var localPos = ToLocalSpace(screenSpacePos);
            var pathRadiusSquared = clickRadius * clickRadius;

            foreach (var t in path.Segments)
                if (t.DistanceSquaredToPoint(localPos) <= pathRadiusSquared)
                    return true;

            return false;
        }

        protected override bool OnDragStart(DragStartEvent e)
        {
            Recompute();

            var localPos = ToLocalSpace(e.ScreenSpaceMouseDownPosition);
            Vector2 pos = new Vector2(
                MathHelper.Clamp(localPos.X / DrawWidth, 0, 1),
                MathHelper.Clamp(1 - localPos.Y / DrawHeight, 0, 1)
            );

            // find existing circle at this position
            for (int i = 0; i < current.Value.Count; i++)
            {
                var p = current.Value[i];
                var existing = getClosestDragCircle(pos);
                if ((localPos - p).LengthSquared < 12 * 12)
                {
                    // this shouldn't actually happen because the circles define drag events but it happens...
                    foreach (var c in points.Children)
                    {
                        if ((c as DragCircle).Index == i)
                        {
                            dragProxy = c;
                            c.TriggerEvent(e);
                            break;
                        }
                    }
                    return true;
                }
            }

            if (checkLineInput(e.ScreenSpaceMouseDownPosition))
            {
                current.Value.Insert(current.Value.Count - 1, pos);
                Recompute();

                dragProxy = getClosestDragCircle(new Vector2(pos.X, 1 - pos.Y));
                dragProxy.TriggerEvent(e);
                return true;
            }
            return false;
        }

        protected override bool OnDrag(DragEvent e)
        {
            if (dragProxy != null)
                return dragProxy.TriggerEvent(e);
            else
                return false;
        }

        protected override bool OnDragEnd(DragEndEvent e)
        {
            if (dragProxy != null)
            {
                bool ret = dragProxy.TriggerEvent(e);
                dragProxy = null;
                return ret;
            }
            else
                return false;
        }

        private DragCircle getClosestDragCircle(Vector2 point)
        {
            DragCircle min = points.Children[0] as DragCircle;
            float minDist = (min.Position - point).LengthSquared;
            if (minDist == 0)
                return min;

            for (int i = 1; i < points.Children.Count; i++)
            {
                DragCircle other = points.Children[i] as DragCircle;
                float dist = (other.Position - point).LengthSquared;
                if (dist == 0)
                    return other;

                if (dist < minDist)
                {
                    min = other;
                    minDist = dist;
                }
            }
            return min;
        }

        private DragCircle makeDragCircle(int index)
        {
            return new DragCircle
            {
                Index = index,
                Editor = this
            };
        }

        private class DragCircle : CompositeDrawable
        {
            public CurvesEditor Editor { get; set; }
            public int Index { get; set; }
            public float DragOutMargin { get; set; }
            public bool EndsAllowNon01 { get; set; }

            private bool isLast;
            public bool IsLast
            {
                get => isLast;
                set
                {
                    isLast = value;
                    CheckVisibility();
                }
            }

            public bool IsEndNode => Index == 0 || IsLast;

            private bool isDragging;

            public DragCircle()
            {
                RelativePositionAxes = Axes.Both;
                RelativeSizeAxes = Axes.None;
                Origin = Anchor.Centre;
                Width = 24;
                Height = 24;
                DragOutMargin = 0.5f;

                AddInternal(new Circle
                {
                    RelativePositionAxes = Axes.None,
                    RelativeSizeAxes = Axes.None,
                    Origin = Anchor.Centre,
                    X = Width / 2,
                    Y = Height / 2,
                    Width = Width / 3,
                    Height = Height / 3,
                    BorderColour = Color4.Black,
                    BorderThickness = 2
                });

                CheckVisibility();
            }

            public void CheckVisibility()
            {
                if (IsEndNode && !EndsAllowNon01)
                    Hide();
                else
                    Show();
            }

            protected override bool OnDragStart(DragStartEvent e)
            {
                isDragging = true;
                return handleDragEvent(e);
            }

            protected override bool OnDragEnd(DragEndEvent e)
            {
                bool ret = handleDragEvent(e);
                isDragging = false;
                return ret;
            }

            protected override bool OnDrag(DragEvent e)
            {
                return handleDragEvent(e);
            }

            private bool handleDragEvent(MouseButtonEvent e)
            {
                if (!isDragging || (IsEndNode && !EndsAllowNon01) || Index == -1)
                    return false;

                float x = e.MousePosition.X / Editor.DrawWidth;
                float y = 1 - e.MousePosition.Y / Editor.DrawHeight;

                if ((x < -DragOutMargin || x > 1 + DragOutMargin || y < -DragOutMargin || y > 1 + DragOutMargin) && !IsEndNode)
                {
                    Editor.Current.Value.RemoveAt(Index);
                    Index = -1;
                    isDragging = false;
                    // clears any drag proxy
                    Editor.TriggerEvent(new DragEndEvent(e.CurrentState, e.Button, e.ScreenSpaceMouseDownPosition));
                }
                else
                {
                    if (IsEndNode)
                        x = Editor.Current.Value[Index].X;

                    Editor.Current.Value[Index] = new Vector2(
                            MathHelper.Clamp(x, 0, 1),
                            MathHelper.Clamp(y, 0, 1));
                }
                Editor.Current.TriggerChange();
                return true;
            }
        }

        private class ClickReceivingPath : SmoothPath
        {
            public Func<ClickEvent, bool> Clicked { get; set; }

            protected override bool OnClick(ClickEvent args)
            {
                return Clicked(args);
            }
        }
    }
}
