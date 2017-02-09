using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using devDept.Eyeshot;
using devDept.Eyeshot.Entities;
using devDept.Geometry;
using WeinCadSW;
using Xunit;

namespace EyeshotBugs
{
    public class CurrentBlockReferenceBug
    {
        [Theory]
        [InlineData(false, false)]
        [InlineData(false, true)]
        [InlineData(true , false)]
        [InlineData(true, true)]
        public async Task SetCurrentBlockReferenceCausesFailureWhenRemovingBlock(bool addBlock, bool setCurrent)
        {
            await Eyeshot.RunSTADesktop
                ((
                    () =>
                    {

                        ViewportLayout vpl = Eyeshot.CreateEyeshotWindow().ViewportLayout;

                        var name0 = "name0";
                        var block0 = new Block();
                        var blockRef0 = new BlockReference(new Identity(), name0);

                        var name1 = "name1";
                        var block1 = new Block();
                        var blockRef1 = new BlockReference(new Identity(), name1 );

                        vpl.Blocks.Add(name0,block0);
                        vpl.Blocks.Add(name1,block1);

                        if(addBlock)
                            block1.Entities.Add(blockRef0);

                        vpl.Entities.Add(blockRef1);

                        if(setCurrent)
                            vpl.Entities.SetCurrent(blockRef1);

                        if(addBlock)
                            block1.Entities.Remove(blockRef0);

                        vpl.Blocks.Remove(name0);

                        vpl.ZoomFit();

                       // await Eyeshot.ViewportLayout.ClosedTask;

                    }));

        }

    }
}
