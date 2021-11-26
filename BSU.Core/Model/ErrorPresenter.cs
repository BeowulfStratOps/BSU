using System;
using System.Collections.Generic;

namespace BSU.Core.Model
{
    public class ErrorPresenter : IErrorPresenter
    {
        private IErrorPresenter _connectedPresenter;
        private readonly List<string> _cache = new();

        public void Connect(IErrorPresenter presenter)
        {
            if (_connectedPresenter != null) throw new InvalidOperationException("Already connected!");
            _connectedPresenter = presenter;
            foreach (var cached in _cache)
            {
                presenter.AddError(cached);
            }
        }

        public void AddError(string error)
        {
            if (_connectedPresenter == null)
            {
                _cache.Add(error);
                return;
            }
            _connectedPresenter.AddError(error);
        }
    }
}
