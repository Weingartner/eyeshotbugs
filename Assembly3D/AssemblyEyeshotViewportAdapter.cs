using System;
using System.Diagnostics;
using System.Reactive;
using System.Reactive.Disposables;
using System.Threading;
using devDept.Eyeshot;
using devDept.Eyeshot.Entities;
using devDept.Eyeshot.Labels;

namespace Weingartner.Eyeshot.Assembly3D
{
    public class AssemblyEyeshotViewportAdapter : IAssemblyViewportLayoutAdapter
    {
        private ViewportLayout Layout { get; }

        public AssemblyEyeshotViewportAdapter(ViewportLayout viewportLayout)
        {
            Layout = viewportLayout;
        }

        public void AssertCorrectThread()
        {
            if (Layout.Dispatcher.Thread == Thread.CurrentThread)
                return;
            if (Debugger.IsAttached)
                Debugger.Break();
            throw new Exception("This code needs to be run on the control thread");
        }

        public void AddLabel(Label label) => Layout.Labels.Add(label);
        public void RemoveLabel(Label label) => Layout.Labels.Remove(label);
        public void Invoke(Action action) => Layout.Dispatcher.Invoke(action);
        public T Invoke<T>(Func<T> action) => Layout.Dispatcher.Invoke(action);
        public void Invalidate(bool withRegen) => Layout.InvalidateAndRegen(withRegen);

        /// <summary>
        /// Ensure that the CurrentBlockReference is cleared before manipulating the
        /// viewport block list.
        /// </summary>
        /// <param name="viewportLayout"></param>
        /// <param name="d"></param>
        public static IDisposable UpdateBlockContext(ViewportLayout viewportLayout)
        {
            var stack = viewportLayout.Entities.CurrentBlockReferences;
            foreach (var br in stack.Skip(1))
                viewportLayout.Entities.SetCurrent(br);
            viewportLayout.Entities.SetCurrent(null);

            return Disposable.Create(() => viewportLayout.Entities.SetCurrentStack(stack));
        }

        /// <summary>
        /// Ensure that the CurrentBlockReference is cleared before manipulating the
        /// viewport block list.
        /// </summary>
        /// <param name="d"></param>
        private void UpdateBlockContext(Action d)
        {
            using (UpdateBlockContext(Layout)) d();
        }

        /// <summary>
        /// Add a block to the viewport and return an IDisposable which will remove
        /// it when invoked.
        /// </summary>
        /// <param name="blockReferenceBlockName"></param>
        /// <param name="block"></param>
        /// <returns></returns>
        public IDisposable AddBlock(string blockReferenceBlockName, NamedBlock block)
        {
            // Adding and removing blocks from the viewport layout requires first
            // making sure that the viewportlayout.Entities.CurrentBlockStack is null.
            // We restore it after adding our blocks.
            UpdateBlockContext(() => Layout.Blocks.Add(blockReferenceBlockName, block));
            return Disposable.Create(() => UpdateBlockContext(() => Layout.Blocks.Remove(blockReferenceBlockName)));
        }

        public IDisposable AddBlockReference(BlockReference blockReference)
        {
            Layout.Entities.Add(blockReference);
            return Disposable.Create(()=>Layout.Entities.Remove(blockReference));
        }

        public IObservable<Unit> Ready() => Layout.Ready();
        public IDisposable SetCurrent(BlockReference o) => Layout.SetCurrent(o);
    }

    public class AdapterException : Exception
    {
        public AdapterException(string s):base(s)
        {
        }
    }
}