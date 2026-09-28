using Casko.DefaultsForUmbraco.Automate.Email;
using Umbraco.Automate.Core.Actions;
using Umbraco.Automate.Core.Security;
using Umbraco.Cms.Core.Actions;
using Umbraco.Cms.Core.PublishedCache;

namespace Casko.DefaultsForUmbraco.Automate.Actions.SendEmail;

[Action(
    "Casko.SendEmail",
    "Send Email to User",
    Description = "Creates an email from published content and sends it to the configured lists.",
    Group = "Casko",
    Icon = "icon-message",
    RequiredSections = [Umbraco.Cms.Core.Constants.Applications.Content],
    RequiredPermissions = [ActionBrowse.ActionLetter])]
public sealed class SendEmailAction : ActionBase<SendEmailSettings, SendEmailOutput>
{
    private const string ListAliasesPropertyAlias = "listAliases";
    private static readonly IReadOnlyList<string> RequiredContentPermissions = [ActionBrowse.ActionLetter];

    private readonly IEmailTemplateFactory _emailTemplateFactory;
    private readonly IEmailSender _emailSender;
    private readonly IPublishedContentCache _publishedContentCache;
    private readonly IAutomationActionAuthorizer _authorizer;

    public SendEmailAction(
        ActionInfrastructure infrastructure,
        IEmailTemplateFactory emailTemplateFactory,
        IEmailSender emailSender,
        IPublishedContentCache publishedContentCache,
        IAutomationActionAuthorizer authorizer)
        : base(infrastructure)
    {
        _emailTemplateFactory = emailTemplateFactory;
        _emailSender = emailSender;
        _publishedContentCache = publishedContentCache;
        _authorizer = authorizer;
    }

    public override async Task<ActionResult> ExecuteAsync(
        ActionContext context,
        CancellationToken cancellationToken)
    {
        var settings = context.GetSettings<SendEmailSettings>();
        if (!Guid.TryParse(settings.ContentKey, out var contentKey))
        {
            return ActionResult.Failed(
                new ArgumentException("A valid Content Key is required."),
                StepRunErrorCategory.Validation);
        }

        if (await _authorizer.AuthorizeContentOrFailAsync(
                contentKey,
                RequiredContentPermissions,
                cancellationToken) is { } authorizationFailure)
        {
            return authorizationFailure;
        }

        var content = await _publishedContentCache.GetByIdAsync(contentKey, preview: false);
        if (content is null)
        {
            return Success(new SendEmailOutput
            {
                Reason = "Published content was not found."
            });
        }

        var property = content.GetProperty(ListAliasesPropertyAlias);
        if (property is null)
        {
            return Success(new SendEmailOutput
            {
                Reason = "Content does not contain a listAliases property."
            });
        }

        var rawAliases = property.GetValue()?.ToString();
        if (string.IsNullOrWhiteSpace(rawAliases))
        {
            return Success(new SendEmailOutput
            {
                Reason = "listAliases is empty."
            });
        }

        var aliases = ParseListAliases(rawAliases);
        if (aliases.Count == 0)
        {
            return Success(new SendEmailOutput
            {
                Reason = "listAliases is empty."
            });
        }

        var template = await _emailTemplateFactory.CreateAsync(content, cancellationToken);
        var message = new EmailMessage(aliases, template.Subject, template.HtmlBody);
        await _emailSender.SendAsync(message, cancellationToken);

        return Success(new SendEmailOutput
        {
            Sent = true,
            ListAliases = aliases
        });
    }

    private static IReadOnlyCollection<string> ParseListAliases(string value) =>
        value.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
}
