using System;
using System.Reactive.Linq;
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
                    var vpl = Eyeshot.ViewportLayout.Model;

                    var block = new Block("Foo");
                    var blockRef = new BlockReference( new Identity(), "Foo" );

                    var cube = new Bar( 0,0,0,1,0,0,0.5,20);

                    block.Entities.Add(cube);

                    vpl.Blocks.Add( block );
                    vpl.Entities.Add(blockRef  );
                    await Task.Delay( TimeSpan.FromMilliseconds( 100 ) );
                    vpl.ZoomFit();

                    var transform = new Identity();

                    // Comment out the below line and the bar
                    // will be visible.
                    blockRef.Transformation = transform;
                    blockRef.RegenMode = regenType.NotNeeded;

                    //vpl.Entities.Regen(  );
                    await Eyeshot.ViewportLayout.ClosedTask;

                } );
        }

        [Fact]
        public async Task ShouldWork2()
        {
            await Eyeshot.RunSTADesktop
                ( async () =>
                {
                    var vpl = Eyeshot.ViewportLayout.Model;

                    var block = new Block("Foo");
                    var blockRef = new BlockReference( new Identity(), "Foo" );

                    var cube = new Bar( 0,0,0,1,0,0,0.5,20);

                    block.Entities.Add(cube);

                    vpl.Blocks.Add( block );
                    vpl.Entities.Add(blockRef  );
                    await Task.Delay( TimeSpan.FromMilliseconds( 100 ) );
                    vpl.ZoomFit();

                    var angle = 0;
                    Observable
                       .Interval( TimeSpan.FromMilliseconds( 10 ) )
                       .ObserveOnDispatcher()
                       .Subscribe
                            ( v =>
                            {
                                angle = angle + 5;
                                var transform = new Rotation( angle * Math.PI/180,Vector3D.AxisZ );

                                // Comment out the below line and the bar
                                // will be visible.
                                blockRef.Transformation = transform;
                                blockRef.RegenMode = regenType.NotNeeded;
                                vpl.Invalidate();

                            });

                    //vpl.Entities.Regen(  );
                    await Eyeshot.ViewportLayout.ClosedTask;

                } );
        }

    }
}
