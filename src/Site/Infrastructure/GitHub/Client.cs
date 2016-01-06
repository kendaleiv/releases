using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace RimDev.Releases.Infrastructure.GitHub
{
    public class Client
    {
        private const string baseUrl = "https://api.github.com/";

        /// <remarks>
        /// https://developer.github.com/v3/repos/commits/#compare-two-commits
        /// is limited to 250 commits.
        /// </remarks>
        private const int CommitCompareApiMaximum = 250;

        private readonly string apiToken;
        private readonly IMarkdownCache markdownCache;

        private readonly string userAgent;
        public const string DefaultUserAgent = "RimDev.Releases";
        private ILogger<Client> logger;

        public Client(string apiToken, IMarkdownCache markdownCache, ILogger<Client> logger, string userAgent = "RimDev.Releases")
        {
            this.apiToken = apiToken;
            this.markdownCache = markdownCache;
            this.userAgent = userAgent;
            this.logger = logger;
        }

        public async Task<ReleasesResponse> GetReleases(string owner, string repo, int page = 1, int pageSize = 10)
        {
            var url = new Uri($"{baseUrl}repos/{owner}/{repo}/releases?page={page}&per_page={pageSize}");

            using (var client = GetClient(url))
            {
                var result = await client.GetAsync("");

                result.EnsureSuccessStatusCode();

                var response = await result.Content.ReadAsStringAsync();

                IEnumerable<string> links;
                result.Headers.TryGetValues("Link", out links);
                var header = (links ?? new string[0]).FirstOrDefault();

                return new ReleasesResponse
                {
                    Releases = JsonConvert.DeserializeObject<List<Release>>(response).AsReadOnly(),
                    Page = page,
                    PageSize = pageSize
                }.ParsePaging(header);
            }
        }

        public async Task<IEnumerable<Author>> GetAuthorsBetweenRange(string owner, string repo, string compareBase, string compareHead)
        {
            var url = new Uri($"{baseUrl}repos/{owner}/{repo}/compare?:{compareBase}...:{compareHead}");

            using (var client = GetClient(url))
            {
                var result = await client.GetAsync("");

                result.EnsureSuccessStatusCode();

                var response = await result.Content.ReadAsStringAsync();

                var json = JsonConvert.DeserializeObject<CompareTwoCommits>(response);

                if (json.Commits.Count() >= CommitCompareApiMaximum)
                {
                    logger.LogWarning(
                        $"Encountered the maximum number of commits ({CommitCompareApiMaximum}) " +
                        $"while comparing :{compareBase}...:{compareHead}. " +
                        "Some authors may be missing from comparison.");
                }

                return json.Commits.SelectMany(x => x.Authors).Distinct();
            }
        }

        public async Task<Release> GetLatestRelease(string owner, string repo)
        {
            var release = await GetReleases(owner, repo, 1, 1);
            return release.Releases.FirstOrDefault();
        }

        public async Task<string> RenderMarkdown(string markdown, string context, string mode = "gfm")
        {
            var cacheItem = await markdownCache.Get(markdown, context, mode);
            if (cacheItem != null)
            {
                return cacheItem;
            }

            var url = new Uri($"{baseUrl}markdown");
            var json = new { text = markdown, mode = mode, context = context };

            using (var client = GetClient(url))
            {
                var response = await client.PostAsync("", new StringContent(JsonConvert.SerializeObject(json)));
                var content = await response.Content.ReadAsStringAsync();

                await markdownCache.Add(markdown, context, mode, content);

                return content;
            }
        }

        private HttpClient GetClient(Uri uri)
        {
            var client = new HttpClient
            {
                BaseAddress = uri
            };

            client.DefaultRequestHeaders.Add("Authorization", $"Token {apiToken}");
            client.DefaultRequestHeaders.Add("User-Agent", userAgent);

            return client;
        }
    }
}
