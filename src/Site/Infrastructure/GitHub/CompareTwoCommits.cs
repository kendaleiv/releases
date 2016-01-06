using System.Collections.Generic;

namespace RimDev.Releases.Infrastructure.GitHub
{
    public class CompareTwoCommits
    {
        public CompareTwoCommits()
        {
            Commits = new List<Commit>();
        }

        public IEnumerable<Commit> Commits { get; set; }
    }
}
