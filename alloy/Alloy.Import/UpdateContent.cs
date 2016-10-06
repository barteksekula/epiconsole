using EPiServer;

namespace Alloy.Import
{
    public class UpdateContent: ImportContentFromOldSolution
    {
        public UpdateContent(IContentRepository contentRepository) : base(contentRepository)
        {
        }
    }
}