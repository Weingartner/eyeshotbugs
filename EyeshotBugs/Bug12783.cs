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
using Xunit;

namespace EyeshotBugs
{
    public class Bug12783
    {

        [Fact]
        public async Task ShouldWork2()
        {
            await Eyeshot.RunSTADesktop
                ( async () =>
                {
                    var vpl = Eyeshot.ViewportLayout.Model;

                    var block = new Block("Foo");
                    var blockRef = new BlockReference( new Identity(), "Foo" );

                    vpl.Blocks.Add( block );
                    vpl.Entities.Add(blockRef  );

                    var dim = new LinearDim( Plane.XY
                                             , new Point3D(0,0,0)
                                            ,  new Point3D(1,0,0)
                                            , new Point3D(0.5,0.5,0)
                                            ,0.2  );

                    dim.Color = Color.Blue;
                    dim.ColorMethod = colorMethodType.byEntity;

                    block.Entities.Add(dim  );
                    block.Entities.Regen();

                    await Eyeshot.ViewportLayout.ClosedTask;

                } );
        }
    }
}
