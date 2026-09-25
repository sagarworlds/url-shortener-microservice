using Shortener.Links.Api.Domain;

namespace Shortener.Links.Api.Application;

/// <summary>Handles the create-link use case.</summary>
public sealed class CreateLinkHandler(
    ILinkRepository repository,
    IShortCodeGenerator codeGenerator,
    TimeProvider timeProvider)
{
    /// <summary>
    /// The number of times a generated code is retried after a collision,
    /// before giving up. Custom aliases are never retried.
    /// </summary>
    private const int MaxGenerationAttempts = 3;

    /// <summary>Validates and creates a link.</summary>
    /// <param name="request">The link to create.</param>
    /// <param name="cancellationToken">Cancels the operation.</param>
    /// <returns>The outcome of the attempt.</returns>
    /// <exception cref="ShortCodeGenerationExhaustedException">
    /// No caller-supplied alias was given, and every generated code collided
    /// with an existing one across <see cref="MaxGenerationAttempts"/> attempts.
    /// </exception>
    public async Task<CreateLinkResult> HandleAsync(CreateLinkRequest request, CancellationToken cancellationToken)
    {
        var urlResult = DestinationUrl.Create(request.Url);
        if (!urlResult.IsValid)
        {
            return CreateLinkResult.Invalid(urlResult.Error!);
        }

        if (!string.IsNullOrWhiteSpace(request.Alias))
        {
            var aliasResult = ShortCode.Create(request.Alias);
            if (!aliasResult.IsValid)
            {
                return CreateLinkResult.Invalid(aliasResult.Error!);
            }

            var link = CreateLink(aliasResult.Value!, urlResult.Value!, request.ExpiresAt);
            var added = await repository.TryAddAsync(link, cancellationToken);
            return added ? CreateLinkResult.Created(link) : CreateLinkResult.AliasTaken();
        }

        for (var attempt = 0; attempt < MaxGenerationAttempts; attempt++)
        {
            var link = CreateLink(codeGenerator.Generate(), urlResult.Value!, request.ExpiresAt);
            if (await repository.TryAddAsync(link, cancellationToken))
            {
                return CreateLinkResult.Created(link);
            }
        }

        throw new ShortCodeGenerationExhaustedException();
    }

    private Link CreateLink(ShortCode code, DestinationUrl url, DateTimeOffset? expiresAt) =>
        new(Guid.NewGuid(), code, url, timeProvider.GetUtcNow(), expiresAt);
}
