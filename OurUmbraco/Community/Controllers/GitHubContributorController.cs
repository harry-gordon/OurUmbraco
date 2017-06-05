﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web.Http;
using System.Web.Mvc;
using OurUmbraco.Community.Models;
using OurUmbraco.Our.Api;
using RestSharp;
using Umbraco.Core.Cache;
using Umbraco.Core.Logging;
using Umbraco.Web.Mvc;

namespace OurUmbraco.Community.Controllers
{
    public class GitHubContributorController : SurfaceController
    {
        private readonly string[] Repositories =
        {
            "Umbraco-CMS",
            "UmbracoDocs",
            "OurUmbraco",
            "Umbraco.Deploy.Contrib",
            "Umbraco.Courier.Contrib",
            "Umbraco.Deploy.ValueConnectors"
        };

        public ActionResult GitHubGetContributorsResult()
        {
            var model = new GitHubContributorsModel();
            try
            {
                string configPath = Server.MapPath("~/config/githubhq.txt");
                if (!System.IO.File.Exists(configPath))
                {
                    LogHelper.Debug<GitHubContributorController>("Config file was not found: " + configPath);
                    return PartialView("~/Views/Partials/Home/GitHubContributors.cshtml", model);
                }

                // Get the GitHub ID of each HQ contributor
                string[] login = System.IO.File.ReadAllLines(configPath).Where(x => x.Trim() != "").Distinct().ToArray();
                var contributors = ApplicationContext.ApplicationCache.RuntimeCache.GetCacheItem<List<GitHubContributorModel>>("UmbracoGitHubContributors",
                    () =>
                    {
                        var githubController = new GitHubController();
                        var gitHubContributors = new List<GitHubContributorModel>();
                        foreach (var repo in Repositories)
                        {
                            var response = githubController.GetAllRepoContributors(repo);
                            if (response.StatusCode == HttpStatusCode.OK &&
                                response.ResponseStatus == ResponseStatus.Completed)
                            {
                                gitHubContributors.AddRange(response.Data);
                            }
                            else
                            {
                                LogHelper.Warn<IGitHubContributorsModel>(string.Format("Invalid HTTP response for repository {0}", repo));
                            }
                        }
                        return gitHubContributors;

                    }, TimeSpan.FromDays(1));

                var filteredContributors = contributors
                    .OrderByDescending(c => c.Total)
                    .Where(g => !login.Contains(g.Author.Login))
                    .GroupBy(g => g.Author.Id);

                model.Contributors = filteredContributors;
            }
            catch (Exception ex)
            {
                LogHelper.Error<IGitHubContributorsModel>("Could not get GitHub Contributors", ex);
            }

            return PartialView("~/Views/Partials/Home/GitHubContributors.cshtml", model);
        }
    }
}
