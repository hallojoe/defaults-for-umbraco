using Casko.DefaultsForUmbraco.Automate.Email;
using Microsoft.Extensions.DependencyInjection;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;

namespace Casko.DefaultsForUmbraco.Automate.Configuration;

public sealed class CaskoAutomateComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
    {
        builder.Services.AddSingleton<IEmailTemplateFactory, ContentEmailTemplateFactory>();
        builder.Services.AddSingleton<IEmailSender, LoggingEmailSender>();
    }
}
