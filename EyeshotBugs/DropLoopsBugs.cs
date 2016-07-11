using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using devDept.Eyeshot.Entities;
using devDept.Geometry;
using devDept.Graphics;
using WeinCadSW;
using Xunit;

namespace EyeshotBugs
{
    public class DropLoopsBugs
    {
        private List<T> MakeList<T>(params T[] items) => items.ToList();

        /// <summary>
        /// https://devdept.zendesk.com/hc/en-us/requests/8720
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task DropLoopsFailsWithCone()
        {

            await Eyeshot.RunSTADesktop
                ((async () =>
                {
                    var data = new
                        #region data

                    {
                        Surface = new
                        {
                            ControlPointList = MakeList
                                (
                                    MakeList
                                        (
                                            new
                                            {
                                                X = -1200.2,
                                                Y = 0.0,
                                                Z = -9995.0,
                                                W = 1.0
                                            },
                                            new
                                            {
                                                X = -400.066681,
                                                Y = 800.133362,
                                                Z = -3331.66675,
                                                W = 0.333333343
                                            },
                                            new
                                            {
                                                X = 400.066681,
                                                Y = 800.133362,
                                                Z = -3331.66675,
                                                W = 0.333333343
                                            },
                                            new
                                            {
                                                X = 1200.2,
                                                Y = 0.0,
                                                Z = -9995.0,
                                                W = 1.0
                                            },
                                            new
                                            {
                                                X = 400.066681,
                                                Y = -800.133362,
                                                Z = -3331.66675,
                                                W = 0.333333343
                                            },
                                            new
                                            {
                                                X = -400.066681,
                                                Y = -800.133362,
                                                Z = -3331.66675,
                                                W = 0.333333343
                                            },
                                            new
                                            {
                                                X = -1200.2,
                                                Y = 0.0,
                                                Z = -9995.0,
                                                W = 1.0
                                            }
                                        ),
                                    MakeList
                                        (
                                            new
                                            {
                                                X = -5.639933E-14,
                                                Y = 0.0,
                                                Z = 6.66666651,
                                                W = 1.0
                                            },
                                            new
                                            {
                                                X = -1.8799776E-14,
                                                Y = 3.7599552E-14,
                                                Z = 2.22222233,
                                                W = 0.333333343
                                            },
                                            new
                                            {
                                                X = 1.8799776E-14,
                                                Y = 3.7599552E-14,
                                                Z = 2.22222233,
                                                W = 0.333333343
                                            },
                                            new
                                            {
                                                X = 5.639933E-14,
                                                Y = 0.0,
                                                Z = 6.66666651,
                                                W = 1.0
                                            },
                                            new
                                            {
                                                X = 1.8799776E-14,
                                                Y = -3.7599552E-14,
                                                Z = 2.22222233,
                                                W = 0.333333343
                                            },
                                            new
                                            {
                                                X = -1.8799776E-14,
                                                Y = -3.7599552E-14,
                                                Z = 2.22222233,
                                                W = 0.333333343
                                            },
                                            new
                                            {
                                                X = -5.639933E-14,
                                                Y = 0.0,
                                                Z = 6.66666651,
                                                W = 1.0
                                            }
                                        )
                                ),
                            SwOrderU = 2,
                            SwOrderV = 4,
                            KnotVectorU = MakeList
                                (
                                    0.0,
                                    0.0,
                                    1.0,
                                    1.0
                                ),
                            KnotVectorV = MakeList
                                (
                                    0.0,
                                    0.0,
                                    0.0,
                                    0.0,
                                    0.5,
                                    0.5,
                                    0.5,
                                    1.0,
                                    1.0,
                                    1.0,
                                    1.0
                                )
                        },
                        TrimLoops = MakeList
                            (
                                MakeList
                                    (
                                        new
                                        {
                                            ControlPoints = MakeList
                                                (
                                                    new
                                                    {
                                                        X = -0.44,
                                                        Y = 5.38844575E-17,
                                                        Z = 3.0,
                                                        W = 1.0
                                                    },
                                                    new
                                                    {
                                                        X = -0.146666661,
                                                        Y = -0.293333322,
                                                        Z = 1.0,
                                                        W = 0.333333343
                                                    },
                                                    new
                                                    {
                                                        X = 0.146666661,
                                                        Y = -0.293333322,
                                                        Z = 1.0,
                                                        W = 0.333333343
                                                    },
                                                    new
                                                    {
                                                        X = 0.44,
                                                        Y = 0.0,
                                                        Z = 3.0,
                                                        W = 1.0
                                                    }
                                                ),
                                            IsPeriodic = false,
                                            IsClosed = false,
                                            Order = 4,
                                            KnotVectorU = MakeList
                                                (
                                                    0.0,
                                                    0.0,
                                                    0.0,
                                                    0.0,
                                                    1.0,
                                                    1.0,
                                                    1.0,
                                                    1.0
                                                )
                                        },
                                        new
                                        {
                                            ControlPoints = MakeList
                                                (
                                                    new
                                                    {
                                                        X = 0.44,
                                                        Y = 5.38844575E-17,
                                                        Z = 3.0,
                                                        W = 1.0
                                                    },
                                                    new
                                                    {
                                                        X = 0.5,
                                                        Y = 6.123234E-17,
                                                        Z = 2.5,
                                                        W = 1.0
                                                    }
                                                ),
                                            IsPeriodic = false,
                                            IsClosed = false,
                                            Order = 2,
                                            KnotVectorU = MakeList
                                                (
                                                    0.0,
                                                    0.0,
                                                    1.0,
                                                    1.0
                                                )
                                        },
                                        new
                                        {
                                            ControlPoints = MakeList
                                                (
                                                    new
                                                    {
                                                        X = 0.5,
                                                        Y = 2.378128E-17,
                                                        Z = 2.5,
                                                        W = 1.0
                                                    },
                                                    new
                                                    {
                                                        X = 0.5,
                                                        Y = -0.05519385,
                                                        Z = 2.5,
                                                        W = 1.0
                                                    },
                                                    new
                                                    {
                                                        X = 0.5,
                                                        Y = -0.08855397,
                                                        Z = 2.44039917,
                                                        W = 1.0
                                                    },
                                                    new
                                                    {
                                                        X = 0.5,
                                                        Y = -0.140400663,
                                                        Z = 2.343973,
                                                        W = 1.0
                                                    },
                                                    new
                                                    {
                                                        X = 0.5,
                                                        Y = -0.160410866,
                                                        Z = 2.29318714,
                                                        W = 1.0
                                                    },
                                                    new
                                                    {
                                                        X = 0.5,
                                                        Y = -0.215426221,
                                                        Z = 2.13885379,
                                                        W = 1.0
                                                    },
                                                    new
                                                    {
                                                        X = 0.5,
                                                        Y = -0.244192526,
                                                        Z = 2.033732,
                                                        W = 1.0
                                                    },
                                                    new
                                                    {
                                                        X = 0.5,
                                                        Y = -0.3239982,
                                                        Z = 1.71701622,
                                                        W = 1.0
                                                    },
                                                    new
                                                    {
                                                        X = 0.5,
                                                        Y = -0.367298782,
                                                        Z = 1.50324655,
                                                        W = 1.0
                                                    },
                                                    new
                                                    {
                                                        X = 0.5,
                                                        Y = -0.489204526,
                                                        Z = 0.861043751,
                                                        W = 1.0
                                                    },
                                                    new
                                                    {
                                                        X = 0.5,
                                                        Y = -0.55828613,
                                                        Z = 0.430733562,
                                                        W = 1.0
                                                    },
                                                    new
                                                    {
                                                        X = 0.5,
                                                        Y = -0.6244998,
                                                        Z = -1.88303617E-16,
                                                        W = 1.0
                                                    }
                                                ),
                                            IsPeriodic = false,
                                            IsClosed = false,
                                            Order = 4,
                                            KnotVectorU = MakeList
                                                (
                                                    0.0,
                                                    0.0,
                                                    0.0,
                                                    0.0,
                                                    0.062499999999999833,
                                                    0.062499999999999833,
                                                    0.12499999999999967,
                                                    0.12499999999999967,
                                                    0.24999999999999983,
                                                    0.24999999999999983,
                                                    0.49999999999999989,
                                                    0.49999999999999989,
                                                    1.0,
                                                    1.0,
                                                    1.0,
                                                    1.0
                                                )
                                        },
                                        new
                                        {
                                            ControlPoints = MakeList
                                                (
                                                    new
                                                    {
                                                        X = -0.5,
                                                        Y = -0.6244998,
                                                        Z = 0.0,
                                                        W = 1.0
                                                    },
                                                    new
                                                    {
                                                        X = -0.166666672,
                                                        Y = -0.74149996,
                                                        Z = 0.0,
                                                        W = 0.8537498
                                                    },
                                                    new
                                                    {
                                                        X = 0.166666672,
                                                        Y = -0.74149996,
                                                        Z = 0.0,
                                                        W = 0.8537498
                                                    },
                                                    new
                                                    {
                                                        X = 0.5,
                                                        Y = -0.6244998,
                                                        Z = 0.0,
                                                        W = 1.0
                                                    }
                                                ),
                                            IsPeriodic = false,
                                            IsClosed = false,
                                            Order = 4,
                                            KnotVectorU = MakeList
                                                (
                                                    0.0,
                                                    0.0,
                                                    0.0,
                                                    0.0,
                                                    1.0,
                                                    1.0,
                                                    1.0,
                                                    1.0
                                                )
                                        },
                                        new
                                        {
                                            ControlPoints = MakeList
                                                (
                                                    new
                                                    {
                                                        X = -0.5,
                                                        Y = -0.6244998,
                                                        Z = -5.295359E-16,
                                                        W = 1.0
                                                    },
                                                    new
                                                    {
                                                        X = -0.5,
                                                        Y = -0.55828613,
                                                        Z = 0.430733562,
                                                        W = 1.0
                                                    },
                                                    new
                                                    {
                                                        X = -0.5,
                                                        Y = -0.489204526,
                                                        Z = 0.861043751,
                                                        W = 1.0
                                                    },
                                                    new
                                                    {
                                                        X = -0.5,
                                                        Y = -0.367298782,
                                                        Z = 1.50324655,
                                                        W = 1.0
                                                    },
                                                    new
                                                    {
                                                        X = -0.5,
                                                        Y = -0.3239982,
                                                        Z = 1.71701622,
                                                        W = 1.0
                                                    },
                                                    new
                                                    {
                                                        X = -0.5,
                                                        Y = -0.244192526,
                                                        Z = 2.033732,
                                                        W = 1.0
                                                    },
                                                    new
                                                    {
                                                        X = -0.5,
                                                        Y = -0.215426221,
                                                        Z = 2.13885379,
                                                        W = 1.0
                                                    },
                                                    new
                                                    {
                                                        X = -0.5,
                                                        Y = -0.160410866,
                                                        Z = 2.29318714,
                                                        W = 1.0
                                                    },
                                                    new
                                                    {
                                                        X = -0.5,
                                                        Y = -0.140400663,
                                                        Z = 2.343973,
                                                        W = 1.0
                                                    },
                                                    new
                                                    {
                                                        X = -0.5,
                                                        Y = -0.08855397,
                                                        Z = 2.44039917,
                                                        W = 1.0
                                                    },
                                                    new
                                                    {
                                                        X = -0.5,
                                                        Y = -0.05519385,
                                                        Z = 2.5,
                                                        W = 1.0
                                                    },
                                                    new
                                                    {
                                                        X = -0.5,
                                                        Y = -3.66965642E-16,
                                                        Z = 2.5,
                                                        W = 1.0
                                                    }
                                                ),
                                            IsPeriodic = false,
                                            IsClosed = false,
                                            Order = 4,
                                            KnotVectorU = MakeList
                                                (
                                                    0.0,
                                                    0.0,
                                                    0.0,
                                                    0.0,
                                                    0.49999999999999956,
                                                    0.49999999999999956,
                                                    0.74999999999999933,
                                                    0.74999999999999933,
                                                    0.87499999999999944,
                                                    0.87499999999999944,
                                                    0.93749999999999967,
                                                    0.93749999999999967,
                                                    1.0,
                                                    1.0,
                                                    1.0,
                                                    1.0
                                                )
                                        },
                                        new
                                        {
                                            ControlPoints = MakeList
                                                (
                                                    new
                                                    {
                                                        X = -0.44,
                                                        Y = 0.0,
                                                        Z = 3.0,
                                                        W = 1.0
                                                    },
                                                    new
                                                    {
                                                        X = -0.5,
                                                        Y = 0.0,
                                                        Z = 2.5,
                                                        W = 1.0
                                                    }
                                                ),
                                            IsPeriodic = false,
                                            IsClosed = false,
                                            Order = 2,
                                            KnotVectorU = MakeList
                                                (
                                                    0.0,
                                                    0.0,
                                                    1.0,
                                                    1.0
                                                )
                                        }
                                    )
                            )
                    };

                    #endregion

                    var uDegree = data.Surface.SwOrderU - 1;
                    var vDegree = data.Surface.SwOrderV - 1;

                    var ctrlPoints =
                        new Point4D[data.Surface.ControlPointList.Count, data.Surface.ControlPointList[0].Count];
                    var u = 0;
                    foreach (var list in data.Surface.ControlPointList)
                    {
                        var v = 0;
                        foreach (var p in list)
                        {
                            ctrlPoints[u, v] = new Point4D(p.X, p.Y, p.Z, p.W);
                            v++;
                        }
                        u++;
                    }

                    var surface = new Surface
                        (
                        uDegree
                        ,
                        data.Surface.KnotVectorU.ToArray()
                        ,
                        vDegree
                        ,
                        data.Surface.KnotVectorV.ToArray()
                        ,
                        ctrlPoints);


                    var esLoops = data.TrimLoops
                        .Select
                        (loopSubCurves =>
                        {
                            var curve = (ICurve) new CompositeCurve
                                (loopSubCurves.Select
                                    (loopSubCurve =>
                                    {
                                        var loopCtrlPoints = loopSubCurve
                                            .ControlPoints
                                            .Select(p => new Point4D(p.X, p.Y, p.Z, p.W))
                                            .ToList();
                                        var degree = loopSubCurve.Order - 1;
                                        return new devDept.Eyeshot.Entities.Curve
                                            (degree, loopSubCurve.KnotVectorU.ToArray(), loopCtrlPoints.ToArray());
                                    }));
                            return curve;
                        })
                        .ToList();

                    var eyeshotSurface = Surface.DropLoops(surface, esLoops);
                    foreach (var esLoop in esLoops)
                    {
                        var nurb = esLoop.GetNurbsForm();
                        nurb.LineWeightMethod = colorMethodType.byEntity;
                        nurb.Color = Color.Yellow;
                        nurb.ColorMethod = colorMethodType.byEntity;
                        nurb.LineWeight = 5;
                        nurb.AddTo(Eyeshot.ViewportLayout);
                    }
                    eyeshotSurface.AddTo(Eyeshot.ViewportLayout);
                    Eyeshot.ViewportLayout.ViewportLayout.ZoomFit();
                    await Eyeshot.ViewportLayout.ClosedTask;
                }));


        }



        [Fact]
        public async Task AnotherDropLoopsBug()
        {
            await Eyeshot.RunSTADesktop
                ((
                    async () =>
                    {

                        var path = new Circle(devDept.Geometry.Plane.XY, new Point3D(0, 0, 1), 1);
                        var trim = new Circle(devDept.Geometry.Plane.YZ, new Point3D(1, 0, 1.5), 0.4);

                        var s = path.ExtrudeAsSurface(Vector3D.AxisZ);

                        s = s.SelectMany(q => Surface.DropLoops(q, new[] { trim })).ToArray();

                        trim.AddTo(Eyeshot.ViewportLayout);

                        foreach (var surface in s)
                        {
                            surface.ShowControl = true;
                        }

                        foreach (var surface in s)
                        {

                            surface.Color = Color.Green;
                            surface.ColorMethod = colorMethodType.byEntity;
                            surface.ShowControl = true;
                            surface.AddTo(Eyeshot.ViewportLayout);
                        }

                        Eyeshot.ViewportLayout.ViewportLayout.ZoomFit();
                        await Eyeshot.ViewportLayout.ClosedTask;

                    }));

        }


    }

}
