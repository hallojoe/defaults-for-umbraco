using Umbraco.Cms.Core.Models.PublishedContent;

namespace Casko.DefaultsForUmbraco.Automate.Email;

public interface IEmailTemplateFactory
{
    Task<EmailTemplate> CreateAsync(IPublishedContent content, CancellationToken cancellationToken);
}
