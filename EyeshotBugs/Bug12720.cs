using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using devDept.Eyeshot;
using devDept.Eyeshot.Entities;
using devDept.Geometry;
using EyeshotBugs.Utils;
using Xunit;

namespace EyeshotBugs
{
    public class Bug12720
    {
        [Fact]
        public async Task ShouldWork()
        {
            await Eyeshot.RunSTADesktop
                ( async () =>
                {
                    var vpl = Eyeshot.ViewportLayout.ViewportLayout;

                    var block = new Block("Foo");
                    var blockRef = new BlockReference( new Identity(), "Foo" );

                    var cube = new Bar( 0,0,0,1,0,0,0.5,20);

                    block.Entities.Add(cube);

                    vpl.Blocks.Add( block );
                    vpl.Entities.Add(blockRef  );
                    vpl.ZoomFit();

                    var ct = new CancellationTokenSource();

                    //await Eyeshot.ViewportLayout.ClosedTask;


                    var angle = 0;
                    var d = Observable
                           .Interval( TimeSpan.FromMilliseconds( 100 ) )
                           .ObserveOnDispatcher()
                           .Subscribe
                                ( _ =>
                                {
                                    angle += 5;
                                    var transform = new Rotation( angle * Math.PI/180, Vector3D.AxisZ );
                                    blockRef.Transformation = transform;
                                    //If you uncomment the Regen command then it works. Not necessary in ES10
                                    //vpl.Entities.Regen(  );
                                    vpl.Invalidate();

                                } );

                    await Eyeshot.ViewportLayout.ClosedTask;
                    d.Dispose();

                } );
        }
    }
}
