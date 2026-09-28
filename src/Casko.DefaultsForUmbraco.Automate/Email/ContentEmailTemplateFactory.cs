using System.Net;
using Umbraco.Cms.Core.Models.PublishedContent;

namespace Casko.DefaultsForUmbraco.Automate.Email;

public sealed class ContentEmailTemplateFactory : IEmailTemplateFactory
{
    public Task<EmailTemplate> CreateAsync(IPublishedContent content, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var name = content.Name;
        return Task.FromResult(new EmailTemplate(
            name,
            $"<p>{WebUtility.HtmlEncode(name)}</p>"));
    }
}
