using System;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using devDept.Eyeshot;
using devDept.Eyeshot.Entities;
using devDept.Geometry;
using devDept.Graphics;
using EyeshotBugs.Utils;
using Weingartner.Eyeshot.Assembly3D;
using Xunit;

namespace EyeshotBugs
{
    public class ToolBodyCylinderWithInsertRadius
    {
        public ToolBodyCylinderWithInsertRadius(double length, double radius, double insertRadius)
        {
            var p0 = new Point3D(0, 0, 0);
            var p1 = new Point3D(0, radius, 0);
            var p2 = new Point3D(length, radius, 0);
            var p3 = new Point3D(length, 0, 0);

            var line0 = new Line(p0, p1);
            var line1 = new Line(p1, p2);
            var line2 = new Line(p2, p3);
            var line3 = new Line(p3, p0);

            if (insertRadius > 0)
            {
                var r = Curve.Fillet(line0, line1, insertRadius, false, false, true, true, out var fillet);
                Debug.Assert(r, "Failed to fillet");

                SurfaceOfRevolutionCrossSection = new CompositeCurve(line0, fillet, line1, line2, line3);
            }
            else
            {
                SurfaceOfRevolutionCrossSection = new CompositeCurve(line0, line1, line2, line3);
            }

            SurfaceOfRevolutionAxis = new Vector3D(p0, p3);

            ToolBody = SurfaceOfRevolutionCrossSection.RevolveAsSolid(0, System.Math.PI * 2, SurfaceOfRevolutionAxis,
                Point3D.Origin, 20, 1e-9);
        }

        public Solid ToolBody { get; }
        public ICurve SurfaceOfRevolutionCrossSection { get; }
        public Vector3D SurfaceOfRevolutionAxis { get; }
    }


    public class RoughStockData
    {
        public double Length { get; }
        public double Diameter { get; }
        public double Zero { get; }

        public RoughStockData(double length, double diameter, double zero)
        {
            Length = length;
            Diameter = diameter;
            Zero = zero;
        }

        public ICurve SurfaceOfRevolutionCurve
        {
            get
            {
                var p0 = new Point3D(0, 0, Zero);
                var p1 = new Point3D(0, 0, Zero + Length);
                var p2 = new Point3D(Diameter / 2, 0, Zero + Length);
                var p3 = new Point3D(Diameter / 2, 0, Zero);

                //var curve = new LinearPath(p0, p1, p2, p3, p0);
                var curve = new LinearPath(p1, p2, p3, p0);

                // Dumb hack to make the surface of revolution have
                // normals in the correct direction. 
                // See https://devdept.zendesk.com/hc/en-us/requests/11058?page=1
                curve.Reverse();

                return curve;
            }
        }

        public Vector3D Axis => new Vector3D(new Point3D(0, 0, Zero), new Point3D(0, 0, Zero + Length));

        public Solid Solid =>
            SurfaceOfRevolutionCurve.RevolveAsSolid(0.0, System.Math.PI * 2, Axis, Point3D.Origin, 20, 1e-9);
    }


    public class Solid3DBugs
    {
        [WpfFact]
        public async Task Solid3DIntersectIsWonky()
        {
            var tb = new ToolBodyCylinderWithInsertRadius(20e-3, 5e-3, 1e-3);
            var stock = new RoughStockData(80e-3, 80e-3, 0);

            var tbSolid = tb.ToolBody;
            tbSolid.Translate(30e-3, 0, 0);
            tbSolid.SetColor(Color.Blue);
            var stockSolid = stock.Solid;
            stockSolid.SetColor(Color.Red);

            var closed = false;
            Eyeshot.ViewportLayout.ClosedTask.ContinueWith(_ => closed = true);

            Eyeshot.ViewportLayout.Model.Backface.ColorMethod = backfaceColorMethodType.Cull;


            while (!closed)

            {
                tbSolid.AddTo(Eyeshot.ViewportLayout);
                stockSolid.AddTo(Eyeshot.ViewportLayout);
                Eyeshot.ViewportLayout.Model.ZoomFit();

                Eyeshot.ViewportLayout.Model.Invalidate();

                await Task.Delay(TimeSpan.FromSeconds(0.5));

                var intersection = Solid.Intersection(stockSolid, tbSolid);

                Eyeshot.ViewportLayout.Model.Entities.Remove(tbSolid);
                Eyeshot.ViewportLayout.Model.Entities.Remove(stockSolid);

                intersection.AddTo(Eyeshot.ViewportLayout);

                Eyeshot.ViewportLayout.Model.Invalidate();

                await Task.Delay(TimeSpan.FromSeconds(0.5));
            }

        }


    }
}