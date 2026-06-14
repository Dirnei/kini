using Akka.Actor;
using Akka.Event;
using MongoDB.Driver;

namespace Kini.Api.Authentication.Identities;

public sealed record CreateIdentityCommand(Guid OrgId, string Username, string Email, string? DisplayName);

public sealed record IdentityResult(Identity? Identity, string? ErrorCode);

public sealed class IdentityActor : ReceiveActor
{
    private readonly IdentitiesCollection _identities;
    private readonly ILoggingAdapter _log = Context.GetLogger();

    public IdentityActor(IdentitiesCollection identities)
    {
        _identities = identities;
        ReceiveAsync<CreateIdentityCommand>(HandleCreate);
    }

    private async Task HandleCreate(CreateIdentityCommand cmd)
    {
        try
        {
            var identity = new Identity(
                Id: Guid.NewGuid(),
                OrgId: cmd.OrgId,
                Username: cmd.Username,
                Email: cmd.Email.Trim().ToLowerInvariant(),
                DisplayName: cmd.DisplayName,
                CreatedAt: DateTimeOffset.UtcNow,
                VerifiedAt: null);

            await _identities.Collection.InsertOneAsync(identity);
            _log.Info("Identity created: {Id} {Username} {Email}", identity.Id, identity.Username, identity.Email);
            Sender.Tell(new IdentityResult(identity, null));
        }
        catch (MongoWriteException ex) when (ex.WriteError.Category == ServerErrorCategory.DuplicateKey)
        {
            // Could be email OR username collision; the error message gives a hint
            // but for v0 we just bucket both under "taken".
            var code = ex.WriteError.Message.Contains("username") ? "username_taken" : "email_taken";
            _log.Info("Identity create rejected: {Code} for {Email}", code, cmd.Email);
            Sender.Tell(new IdentityResult(null, code));
        }
        catch (Exception ex)
        {
            _log.Error(ex, "Failed to create identity for {Email}", cmd.Email);
            Sender.Tell(new IdentityResult(null, "internal_error"));
        }
    }
}
