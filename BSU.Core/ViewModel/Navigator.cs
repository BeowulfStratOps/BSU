using System.Collections.Generic;
using System.Linq;
using BSU.Core.ViewModel.Util;

namespace BSU.Core.ViewModel;

public class Navigator : ObservableBase
{
    private object _content;
    public object Content
    {
        get => _content;
        private set
        {
            if (_content == value) return;
            _content = value;
            OnPropertyChanged();
        }
    }

    private readonly Stack<object> _navigationStack = new();

    public Navigator(object initial)
    {
        _content = initial;
    }

    public void Back()
    {
        if (_navigationStack.Any())
            Content = _navigationStack.Pop();
    }

    public void Forward()
    {

    }

    public void To(object repository)
    {
        _navigationStack.Push(Content);
        Content = repository;
    }
}
