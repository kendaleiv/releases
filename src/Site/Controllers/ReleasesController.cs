using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNet.Mvc;
using Microsoft.Extensions.Logging;
using RimDev.Releases.Infrastructure.GitHub;
using RimDev.Releases.Models;
using RimDev.Releases.ViewModels.Releases;

namespace Site.Controllers
{
    public class ReleasesController : Controller
    {
        private readonly AppSettings appSettings;
        private readonly Client gitHub;
        private readonly ILogger logger;

        public ReleasesController(
            AppSettings appSettings,
            Client gitHub,
            ILogger<ReleasesController> logger)
        {
            this.appSettings = appSettings;
            this.gitHub = gitHub;
            this.logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            var model = new IndexViewModel();

            var requests = appSettings
                .GetAllRepositories()
                .Select(x => GetLatestRelease(x))
                .ToList();

            await Task.WhenAll(requests);

            foreach (var request in requests)
            {
                model.Releases.Add(request.Result);
            }

            return View(model);
        }

        public async Task<IActionResult> Show(string id, int page = 1)
        {
            // never go below 1
            page = Math.Max(1, page);

            var currentRepository = appSettings.Find(id);

            if (currentRepository == null)
                return HttpNotFound();

            var releases = await GetAllReleases(currentRepository, page);

            if (releases == null)
                return View(new ShowViewModel(currentRepository));

            var model = new ShowViewModel(currentRepository) {
              Releases = releases.Releases
                  .Select(x => new ReleaseViewModel(currentRepository, x, GetAuthorsForRelease(currentRepository, x).Result))
                  .ToList(),
              Page = releases.Page,
              PageSize = releases.PageSize,
              FirstPage = releases.FirstPage,
              NextPage = releases.NextPage,
              PreviousPage = releases.PreviousPage,
              LastPage = releases.LastPage
            };

            return View(model);
        }

        public IActionResult Error()
        {
            return View();
        }

        private async Task<ReleasesResponse> GetAllReleases(GitHubRepository gitHubRepository, int page)
        {
            try
            {
                var releases = new List<ReleaseViewModel>();
                var gitHubReleases = await gitHub.GetReleases(gitHubRepository.Owner, gitHubRepository.Name, page);

                return gitHubReleases;
            }
            catch (Exception ex)
            {
                logger.LogError($"github request failed for {gitHubRepository.Name}", ex);
                return null;;
            }
        }

        private async Task<IEnumerable<Author>> GetAuthorsForRelease(
            GitHubRepository gitHubRepository,
            Release release)
        {
            try
            {
               var releaseBaseTarget = await GetPreviousReleaseTarget(gitHubRepository, release);

                var authors = await gitHub.GetAuthorsBetweenRange(
                    gitHubRepository.Owner,
                    gitHubRepository.Name,
                    releaseBaseTarget,
                    release.TargetCommitish);

                return authors;
            }
            catch (Exception ex)
            {
                logger.LogError($"github request failed for {gitHubRepository.Name}", ex);
                return new List<Author> { release.Author };
            }
        }

        private async Task<ReleaseViewModel> GetLatestRelease(GitHubRepository gitHubRepository)
        {
            try
            {
                var release = await gitHub.GetLatestRelease(gitHubRepository.Owner, gitHubRepository.Name);

                var previousReleaseTarget = await GetPreviousReleaseTarget(gitHubRepository, release);

                var authors = await gitHub.GetAuthorsBetweenRange(
                    gitHubRepository.Owner,
                    gitHubRepository.Name,
                    previousReleaseTarget,
                    release.TargetCommitish);

                return new ReleaseViewModel(gitHubRepository, release, authors);
            }
            catch (Exception ex)
            {
                logger.LogError($"github request failed for {gitHubRepository.Name}", ex);
                return new ReleaseViewModel(gitHubRepository, null, null);
            }
        }

        private async Task<string> GetPreviousReleaseTarget(GitHubRepository gitHubRepository, Release release)
        {
            var releasesResponse = await gitHub.GetReleases(gitHubRepository.Owner, gitHubRepository.Name);

            throw new NotImplementedException();

            var releaseBaseTarget = releasesResponse.Releases.Count == 2
                // TODO: Get previous release from current release
                // ? releasesResponse.Releases.Skip(1).First().TargetCommitish
                : "master"; // Using master here is not 'perfect', but hopefully good enough.

            return releaseBaseTarget;
        }
    }
}
