using System;
using System.Collections.Generic;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using devDept.Eyeshot.Entities;
using devDept.Eyeshot.Labels;

namespace Weingartner.Eyeshot.Assembly3D
{
    /// <summary>
    /// An adapter between an assembly and a mock viewportlayout adapter. Used
    /// only for testing the Assembly3D class
    /// </summary>
    public class AssemblyMockViewportLayoutAdapter : IAssemblyViewportLayoutAdapter
    {
        private readonly HashSet<Label> _Labels = new HashSet<Label>();
        public void AssertCorrectThread()
        {}

        public void AddLabel(Label label) => _Labels.Add(label);
        public void RemoveLabel(Label label) => _Labels.Remove(label);
        public IDisposable AddBlock(string blockReferenceBlockName, NamedBlock block) => Disposable.Empty;
        public void Invoke(Action action) => action();
        public T Invoke<T>(Func<T> action) => action();
        public void Invalidate(bool withRegen) { }
        public IObservable<Unit> Ready() => Observable.Return(Unit.Default);
        public IDisposable SetCurrent(BlockReference o) => Disposable.Empty;

        IDisposable IAssemblyViewportLayoutAdapter.AddBlockReference(BlockReference blockReference) => Disposable.Empty;
    }
}