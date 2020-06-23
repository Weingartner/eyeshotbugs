using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using devDept.Eyeshot;
using devDept.Eyeshot.Entities;
using devDept.Geometry;
using EyeshotBugs.Utils;
using Weingartner.Eyeshot.Assembly3D;
using Xunit;

namespace EyeshotBugs
{
    public class Bug13207
    {
        [Fact]
        public async void GridBorderChanges()
        {
            await Eyeshot.RunSTADesktop
            (async () =>
            {
                var vpl = Eyeshot.EyeshotTestWindow.Model;

                var block = new Block("Test");
                var blockRef = new BlockReference(new Identity(), "Test");

                const double radius = 1.0;
                var circle = new Circle(Plane.XY, new Point2D(2, 0), radius);
                block.Entities.Add(circle);

                vpl.Blocks.Add(block);
                vpl.Entities.Add(blockRef);
                vpl.ZoomFit();

                var angle = 0;
                Observable
                    .Interval(TimeSpan.FromSeconds(1))
                    .Take(36)
                    .ObserveOnDispatcher()
                    .Subscribe
                    (v =>
                    {
                        var clonedCircle = (Circle) circle.Clone();
                        angle = angle + 10;
                        var transform = new Rotation(angle * Math.PI / 180, Vector3D.AxisZ);
                        clonedCircle.TransformBy(transform);
                        block.Entities.Clear();
                        block.Entities.Add(clonedCircle);
                        vpl.RegenAll();

                        vpl.Invalidate();
                    });

                await Eyeshot.EyeshotTestWindow.ClosedTask;
            });
        }
    }
}
