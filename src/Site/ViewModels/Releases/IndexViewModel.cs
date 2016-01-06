using System.Linq;
using System.Collections.Generic;
using RimDev.Releases.Models;

namespace RimDev.Releases.ViewModels.Releases
{
    public class IndexViewModel
    {
        public IndexViewModel()
        {
            Releases = new List<ReleaseViewModel>();
        }

        public AppSettings AppSettings {get;set;}
        public IList<ReleaseViewModel> Releases {get;set;}

        public bool NotEmpty => Releases != null && Releases.Any();
    }
}
