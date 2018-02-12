using System;
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
                    await Task.Delay( TimeSpan.FromMilliseconds( 100 ) );
                    vpl.ZoomFit();

                    var transform = new Identity();

                    // Comment out the below line and the bar
                    // will be visible.
                    blockRef.Transformation = transform;

                    await Eyeshot.ViewportLayout.ClosedTask;

                } );
        }
    }
}
