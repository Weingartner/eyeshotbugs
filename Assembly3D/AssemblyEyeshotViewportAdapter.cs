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

        public IDisposable AddBlock(string blockReferenceBlockName, NamedBlock block)
        {
            Layout.Blocks.Add(blockReferenceBlockName, block);
            return Disposable.Create(() => Layout.Blocks.Remove(blockReferenceBlockName));
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