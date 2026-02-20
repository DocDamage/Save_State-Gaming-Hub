using SaveState.Core.Common;
using SaveState.Core.GameLibrary.Entities;

namespace SaveState.Core.GameLibrary.Services;

/// <summary>
/// Interface for managing game memory signatures and pattern detection.
/// </summary>
public interface IMemoryPatternDatabase
{
    /// <summary>
    /// Gets all memory signatures for a specific game title.
    /// Also returns universal patterns (GameTitle = "*").
    /// </summary>
    /// <param name="gameTitle">The game title to search for.</param>
    /// <returns>Result containing matching signatures or empty list if none found.</returns>
    Result<IReadOnlyList<GameMemorySignature>> GetSignaturesForGame(string gameTitle);

    /// <summary>
    /// Adds a new signature to the database.
    /// </summary>
    /// <param name="signature">The signature to add.</param>
    /// <returns>Success or failure result.</returns>
    Result AddSignature(GameMemorySignature signature);

    /// <summary>
    /// Adds a signature for a specific game title.
    /// </summary>
    /// <param name="gameTitle">The game title.</param>
    /// <param name="signature">The signature to add.</param>
    /// <returns>Success or failure result.</returns>
    Result AddSignature(string gameTitle, GameMemorySignature signature);

    /// <summary>
    /// Removes a signature by game title and name.
    /// </summary>
    /// <param name="gameTitle">The game title.</param>
    /// <param name="name">The signature name.</param>
    /// <returns>Success or failure result.</returns>
    Result RemoveSignature(string gameTitle, string name);

    /// <summary>
    /// Removes all signatures for a game.
    /// </summary>
    /// <param name="gameTitle">The game title.</param>
    /// <returns>Success or failure result.</returns>
    Result RemoveAllSignaturesForGame(string gameTitle);

    /// <summary>
    /// Gets all supported game titles in the database.
    /// </summary>
    /// <returns>Result containing list of game titles.</returns>
    Result<IReadOnlyList<string>> GetSupportedGames();

    /// <summary>
    /// Checks if the database has signatures for a game.
    /// </summary>
    /// <param name="gameTitle">The game title to check.</param>
    /// <returns>True if signatures exist.</returns>
    bool HasSignaturesForGame(string gameTitle);

    /// <summary>
    /// Gets all signatures in the database.
    /// </summary>
    /// <returns>All signatures.</returns>
    IReadOnlyList<GameMemorySignature> GetAllSignatures();

    /// <summary>
    /// Updates an existing signature.
    /// </summary>
    /// <param name="gameTitle">The game title.</param>
    /// <param name="name">The signature name.</param>
    /// <param name="updatedSignature">The updated signature data.</param>
    /// <returns>Success or failure result.</returns>
    Result UpdateSignature(string gameTitle, string name, GameMemorySignature updatedSignature);

    /// <summary>
    /// Clears all signatures from the database.
    /// </summary>
    /// <returns>Success result.</returns>
    Result Clear();

    /// <summary>
    /// Gets the total count of signatures.
    /// </summary>
    int Count { get; }
}
