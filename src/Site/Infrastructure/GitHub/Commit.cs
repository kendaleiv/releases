using System.Collections.Generic;

namespace RimDev.Releases.Infrastructure.GitHub
{
    public class Commit
    {
        public Commit()
        {
            Authors = new List<Author>();
        }

        // There are other properties, adding only the one we currently need.

        public IEnumerable<Author> Authors { get; set; }
    }
}
