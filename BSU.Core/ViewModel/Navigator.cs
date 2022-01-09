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

    private readonly List<object> _pages;
    private int _index;

    public Navigator(object initial)
    {
        _content = initial;
        _pages = new List<object> { initial };
    }

    public void Back()
    {
        if (_index == 0) return;
        _index--;
        Content = _pages[_index];
    }

    public void Forward()
    {
        if (_index == _pages.Count - 1) return;
        _index++;
        Content = _pages[_index];
    }

    public void To(object page)
    {
        // remove pages after the current one
        if (_pages.Count > _index + 1)
            _pages.RemoveRange(_index + 1, _pages.Count - _index - 1);
        _pages.Add(page);
        _index++;
        Content = page;
    }
}
