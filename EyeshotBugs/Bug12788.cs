using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using devDept.Eyeshot;
using devDept.Eyeshot.Entities;
using devDept.Geometry;
using EyeshotBugs.Utils;
using Xunit;

namespace EyeshotBugs
{
    public class Bug12788
    {
        [Fact]
        public async Task MeshRenderingShouldWork()
        {
            await Eyeshot.RunSTADesktop
                ( async () =>
                {
                    var vpl = Eyeshot.ViewportLayout.ViewportLayout;

                    var block = new Block("Foo");
                    var blockRef = new BlockReference( new Identity(), "Foo" );

                    vpl.Blocks.Add( block );
                    vpl.Entities.Add(blockRef  );

                    var mesh = Mesh.CreateCylinder( 1,10,10);

                    mesh.Color = Color.Blue;
                    mesh.ColorMethod = colorMethodType.byEntity;

                    block.Entities.Add(mesh  );
                    block.RegenAllCurved( new RegenParams( 0.1, vpl ) );


                    await Eyeshot.ViewportLayout.ClosedTask;

                } );

        }
    }
}
