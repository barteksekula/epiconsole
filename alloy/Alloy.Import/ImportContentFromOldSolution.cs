using System;
using System.ComponentModel;
using System.Threading;
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

        public event EventHandler<ProgressChangedEventArgs> Progress;

        public ImportContentFromOldSolution(IContentRepository contentRepository)
        {
            _contentRepository = contentRepository;
        }

        public void Execute()
        {
            var rootReference = new ContentReference(48);

            var totalPages = 20;

            for (int i = 1; i < totalPages; i++)
            {
                Thread.Sleep(1000);

                var articlePage = _contentRepository.GetDefault<ArticlePage>(rootReference);
                articlePage.Name = "Test page " + i;
                _contentRepository.Save(articlePage, SaveAction.Publish, AccessLevel.NoAccess);
                Progress?.Invoke(this, new ProgressChangedEventArgs(100 / (totalPages - i - 1), null));
            }
            Progress?.Invoke(this, new ProgressChangedEventArgs(100, null));
        }
    }

    public class NotifyEventArgs: EventArgs
    {
        public int Current { get; set; }
        public int Total { get; set; }
    }
}
