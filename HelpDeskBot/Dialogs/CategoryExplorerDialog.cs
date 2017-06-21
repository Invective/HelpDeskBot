using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace HelpDeskBot.Dialogs
{
    using Microsoft.Bot.Builder.Dialogs;
    using Model;
    using Services;
    using System;
    using System.Collections.Generic;
    using System.Text.RegularExpressions;
    using System.Threading.Tasks;
    using Util;

    [Serializable]
    public class CategoryExplorerDialog : IDialog<object>
    {
        private readonly AzureSearchService searchService = new AzureSearchService();
        private string category = null;
        private string originalText = null;

        public CategoryExplorerDialog(string category)
        {
            this.category = category;
        }

        //public async Task StartAsync(IDialogContext context)
        //{
        //    if (string.IsNullOrWhiteSpace(this.category))
        //    {
        //        await context.PostAsync($"Try typing something like _explore hardware_.");
        //        context.Done<object>(null);
        //    }
        //    else
        //    {
        //        SearchResult searchResult = await this.searchService.SearchByCategory(this.category);
        //        string message;
        //        if (searchResult.Value.Length != 0)
        //        {
        //            message = $"These are some articles I've found in the knowledge base for the _'{this.category}'_ category:";
        //            foreach (var item in searchResult.Value)
        //            {
        //                message += $"\n * {item.Title}";
        //            }
        //        }
        //        else
        //        {
        //            message = $"Sorry, I could not find any results in the knowledge base for _'{this.category}'_";
        //        }
        //        await context.PostAsync(message);
        //        context.Done<object>(null);
        //    }
        //}

        public async Task StartAsync(IDialogContext context)
        {
            if (string.IsNullOrWhiteSpace(this.category))
            {
                FacetResult facetResult = await this.searchService.FetchFacets();
                if (facetResult.Facets.Category.Length != 0)
                {
                    List<string> categories = new List<string>();
                    foreach (Category category in facetResult.Facets.Category)
                    {
                        categories.Add($"{category.Value} ({category.Count})");
                    }

                    PromptDialog.Choice(context, this.AfterMenuSelection, categories, "Let\'s see if I can find something in the knowledge for you. Which category is your question about?");
                }
            }
            else
            {
                SearchResult searchResult = await this.searchService.SearchByCategory(this.category);

                if (searchResult.Value.Length > 0)
                {
                    await context.PostAsync($"These are some articles I\'ve found in the knowledge base for _'{this.category}'_, click **More details** to read the full article:");
                }

                await CardUtil.ShowSearchResults(context, searchResult, $"Sorry, I could not find any results in the knowledge base for _'{this.category}'_");

                context.Done<object>(null);
            }
        }

        public virtual async Task AfterMenuSelection(IDialogContext context, IAwaitable<string> result)
        {
            this.category = await result;
            this.category = Regex.Replace(this.category, @"\s\([^)]*\)", string.Empty);

            SearchResult searchResult = await this.searchService.SearchByCategory(this.category);
            await context.PostAsync($"These are some articles I\'ve found in the knowledge base for _'{this.category}'_, click **More details** to read the full article:");

            await CardUtil.ShowSearchResults(context, searchResult, $"Sorry, I could not find any results in the knowledge base for _'{this.category}'_");
            context.Done<object>(null);
        }

        public CategoryExplorerDialog(string category, string originalText)
        {
            this.category = category;
            this.originalText = originalText;
        }
    }
}