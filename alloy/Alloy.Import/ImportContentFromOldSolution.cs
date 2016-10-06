using System;
using System.ComponentModel;
using alloy.Models.Pages;
using EPiServer;
using EPiServer.Core;
using EPiServer.DataAccess;
using EPiServer.Security;

namespace Alloy.Import
{
    public class ImportContentFromOldSolution
    {
        private readonly IContentRepository _contentRepository;

        public EventHandler<ProgressChangedEventArgs> Progress;

        public ImportContentFromOldSolution(IContentRepository contentRepository)
        {
            _contentRepository = contentRepository;
        }

        public void Execute()
        {
            var rootReference = new ContentReference(48);

            for (int i = 1; i < 5; i++)
            {
                var articlePage = _contentRepository.GetDefault<ArticlePage>(rootReference);
                articlePage.Name = "Test page " + i;
                _contentRepository.Save(articlePage, SaveAction.CheckIn, AccessLevel.Publish);
                if (Progress != null)
                {
                    Progress(this, new ProgressChangedEventArgs(100 / i, null));
                }
            }
        }
    }

    public class NotifyEventArgs: EventArgs
    {
        public int Current { get; set; }
        public int Total { get; set; }
    }
}
