using System.Collections.Generic;
using BSU.Core.ViewModel.Util;

namespace BSU.Core.ViewModel
{
    public class AddRepository : ObservableBase
    {
        public string Name { get; set; }
        public string Url { get; set; }
    }
}