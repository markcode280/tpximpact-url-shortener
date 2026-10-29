using System.Security.Cryptography;

namespace UrlShortener.Api.Services;

public interface IAliasGenerator
{
    string GenerateAlias();
}

public sealed class RandomAliasGenerator : IAliasGenerator
{
    public string GenerateAlias()
    {
        const string alphabet = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        return new string(Enumerable.Range(0, 8)
            .Select(_ => alphabet[RandomNumberGenerator.GetInt32(alphabet.Length)]).ToArray());
    }
}

public sealed class AliasConflictException(string alias)
    : InvalidOperationException($"The alias '{alias}' is already taken.");

public sealed class AliasGenerationException()
    : Exception("Could not allocate a unique alias. Please try again.");
