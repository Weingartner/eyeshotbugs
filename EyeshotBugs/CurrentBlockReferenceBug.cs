using System.Threading.Tasks;
using devDept.Eyeshot;
using devDept.Eyeshot.Entities;
using devDept.Geometry;
using EyeshotBugs.Utils;
using Weingartner.Eyeshot.Assembly3D;
using Xunit;
using FluentAssertions;

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


        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task RemoveBlockWithBlockRefSetShouldWork(bool setCurrent)
        {
            await Eyeshot.RunSTADesktop
                (() =>
                {

                    var viewportLayout = Eyeshot.CreateEyeshotWindow().ViewportLayout;
                    var a1 = new Assembly3D(Mesh.CreateSphere(10, 10, 10), Mesh.CreateBox(10,10,10));
                    var a2 = new Assembly3D();
                    var adapter = new AssemblyEyeshotViewportAdapter(viewportLayout);
                    a1.Add(a2);
                    adapter.SetRootAssembly(a1);

                    if(setCurrent)
                        adapter.SetCurrent(a1.BlockReference);

                    adapter.Invoke
                        (() =>
                        {
                            // Clear a1 by calling the Assembly3D::clear method. This was the original
                            // failure I discovered when using Assemlby3D
                            a1.Clear();

                            a1.Block.Entities.Count.Should().Be(0);
                        });


                    //await Eyeshot.ViewportLayout.ClosedTask;

                });
        }

        
        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task RemoveBlockWithBlockRefSetShouldWork2(bool setCurrent)
        {
            await Eyeshot.RunSTADesktop
                (() =>
                {
                    var meshA = Mesh.CreateSphere(10, 10, 10);
                    var meshB = Mesh.CreateSphere(10, 10, 10);
                    var block1 = new Block();
                    var block2 = new Block();
                    block1.Entities.Add(meshA);
                    block1.Entities.Add(meshB);

                    var viewportLayout = Eyeshot.CreateEyeshotWindow().ViewportLayout;

                    viewportLayout.Blocks.Add("1", block1);
                    viewportLayout.Blocks.Add("2", block2);

                    var br1 = new BlockReference("1");
                    var br2 = new BlockReference("2");

                    block2.Entities.Add(meshA);
                    block2.Entities.Add(meshB);
                    block1.Entities.Add(br2);

                    viewportLayout.Entities.Add(br1);
                    if (setCurrent)
                        viewportLayout.SetCurrent(br1);

                    var adapter = new AssemblyEyeshotViewportAdapter(viewportLayout);
                    adapter.Invoke
                        (() =>
                        {
                            // Clear a1 by inlining all the relevant parts of Assembly3D::clear and
                            // subsequent calls to isolate the problem

                            block1.Entities.Clear();
                            block1.Entities.Count.Should().Be(0);

                            // Set the block reference stack to null
                            if(setCurrent)
                                viewportLayout.Entities.SetCurrent(null);
                            block1.Entities.Count.Should().Be(0); // NOTE : Test fails here

                            // Remove the top level block
                            viewportLayout.Blocks.Remove("2");
                            block1.Entities.Count.Should().Be(0);

                            // Set the stack back to what it was before;
                            if(setCurrent)
                                viewportLayout.Entities.SetCurrent(br1);
                            block1.Entities.Count.Should().Be(0);

                            block1.Entities.Count.Should().Be(0);
                        });


                    //await Eyeshot.ViewportLayout.ClosedTask;

                });
        }

    }
}
