using System.Collections.Generic;
using RimDev.Releases.Infrastructure.GitHub;
using RimDev.Releases.Models;

namespace RimDev.Releases.ViewModels.Releases
{
    public class ReleaseViewModel
    {
        public ReleaseViewModel(
            GitHubRepository gitHubRepository,
            Release release,
            IEnumerable<Author> authors)
        {
            GitHubRepository = gitHubRepository;
            Release = release;
            Authors = authors ?? new List<Author>();
        }

        public GitHubRepository GitHubRepository{get;protected set;}
        public Release Release {get;protected set;}

        public string FullName => GitHubRepository.FullName;
        public string Description => GitHubRepository.Description;
        public IEnumerable<Author> Authors {get;protected set;}
        public string Title => Release?.Name;

        public string Body => Release?.Body;
        public string CreatedAt => (Release != null) ? Release.CreatedAt.Date.ToShortDateString() : "n/a";

        public bool HasRelease => Release != null;

        public string GetReleaseStatus()
        {
            if (HasRelease)
            {
                if (Release.Draft)
                {
                    return "Draft";
                }
                else if (Release.Prerelease)
                {
                    return "Pre-release";
                }
                else
                {
                    return "Release";
                }
            }
            else
            {
                return "Unknown";
            }
        }

        public bool IsDraft => HasRelease ? Release.Draft : false;

        public bool IsRelease => HasRelease ? !Release.Draft && !Release.Prerelease : false;

        public bool IsPreRelease => HasRelease ? Release.Prerelease : false;
    }
}
