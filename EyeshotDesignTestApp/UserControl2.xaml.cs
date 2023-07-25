using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using devDept.Eyeshot;
using devDept.Eyeshot.Entities;
using devDept.Geometry;
using Block = devDept.Eyeshot.Block;
using System.Reactive.Disposables;

namespace EyeshotDesignTestApp
{
    public class UserControl2ViewModel : ReactiveObject
    {
        public UserControl2ViewModel(params Entity[] entities)
        {
            Part = new WeingartnerEyeshotDoc(entities);
        }

        public IWeingartnerEyeshotDoc Part { get; set; }
    }

    public interface IWeingartnerEyeshotDoc
    {
        IDisposable AddTo(DesignDocument design);
    }

    public class WeingartnerEyeshotDoc : ReactiveObject, IWeingartnerEyeshotDoc
    {
        private Block _Block;
        private BlockReference _BlockRef;


        public WeingartnerEyeshotDoc(Entity[] entities )
        {
            _Block = new Block(Guid.NewGuid().ToString());
            _BlockRef = new BlockReference(new Identity(), _Block.Name);
            // add the entities to the block
            _Block.Entities.AddRange(entities);
        }
        /// <summary>
        /// add the wg doc to the eyeshot designdocument<br/>
        /// disposing will remove the wg doc from the eyeshot designdocument
        /// </summary>
        /// <param name="designDoc"></param>
        /// <returns></returns>
        public IDisposable AddTo(DesignDocument designDoc)
        {
            var cd = new CompositeDisposable();
            designDoc.Blocks.Add(_Block);
            cd.Add(Disposable.Create((designDoc, _Block), t => t.designDoc.Blocks.Remove(t._Block.Name)));
            designDoc.RootBlock.Entities.Add(_BlockRef);
            cd.Add(Disposable.Create((designDoc.RootBlock, _BlockRef), t => t.RootBlock.Entities.Remove(t._BlockRef)));
            return cd;
        }
    }

    /// <summary>
    /// Interaction logic for UserControl2.xaml
    /// </summary>
    public partial class UserControl2 : UserControl
    {
        public UserControl2()
        {
            InitializeComponent();
        }
    }
}
