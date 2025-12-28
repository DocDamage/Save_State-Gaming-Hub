Reviewed core services, DI setup, config, netplay, memory scanning, data/storage, and dependencies. Here�s the ranked audit with concrete refactors.

High Severity
- High | M | Architecture: AI service locator bypasses DI and builds a parallel object graph (src/SaveState.Core/Services/AiServiceProvider.cs:46, src/SaveState.Core/Services/AiServiceProvider.cs:157, src/SaveState.Core/ServiceCollectionExtensions.cs:77); refactor by removing static Instance, injecting AiServiceProvider/ILlmService via DI, and registering with constructor injection.
- High | S | Configuration: AppConfiguration only parses URL lines and ignores Features/Limits in appsettings.json (src/SaveState.Core/Infrastructure/AppConfiguration.cs:93, src/SaveState.Core/Infrastructure/AppConfiguration.cs:123); refactor to use IConfiguration + IOptions<AppSettings> and delete the custom parser.
- High | M | Netplay protocol: raw JSON over TCP without framing causes partial-read corruption (src/SaveState.Core/Services/Netplay/NetplayService.cs:319, src/SaveState.Core/Services/Netplay/NetplayService.cs:353); refactor to length-prefix or newline-delimited framing with buffered parsing and multi-message handling.
- High | M | Memory scanning: allocates (int)RegionSize buffers and truncates 64-bit pointers (src/SaveState.Core/Services/MemoryScannerService.cs:139, src/SaveState.Core/Services/MemoryScannerService.cs:261, src/SaveState.Core/Services/MemoryScannerService.cs:313); refactor to chunked reads with ArrayPool<byte>, keep long sizes, and remove address casts.

Medium Severity
- Medium | S | Data lifecycle: EnsureCreated and always-on sample seeding runs in all environments (src/SaveState.App/Program.cs:44, src/SaveState.App/Program.cs:46); refactor to Database.Migrate() and guard seeding to Dev/test via explicit seed method.
- Medium | M | Cloud sync: fixed Desktop path + manifest write without directory creation + manual HttpClient (src/SaveState.Core/Services/Cloud/CloudSyncService.cs:74, src/SaveState.Core/Services/Cloud/CloudSyncService.cs:75, src/SaveState.Core/Services/Cloud/CloudSyncService.cs:352); refactor to configurable root, Directory.CreateDirectory, inject IHttpClientFactory, stream uploads, and lock manifest updates.
- Medium | M | Vector search scalability: loads all embeddings into memory per query (src/SaveState.Core/Services/VectorStoreService.cs:54, src/SaveState.Core/Services/VectorStoreService.cs:63); refactor to AsNoTracking + Select + candidate cap, or add a vector-index backend behind IVectorStoreService.
- Medium | S | IPC: IpcWorker never starts a server while single-instance uses a named pipe (src/SaveState.App/Program.cs:206, src/SaveState.Core/Infrastructure/SingleInstanceLock.cs:43); refactor to start gRPC server on the same pipe or remove the client path.

Low Severity / Dependencies
- Low | S | Code smell: duplicate orchestrator implementations (src/SaveState.Core/Services/Ai/UltimateAiOrchestrator.cs:13, src/SaveState.Core/Services/Ai/UltimateAiOrchestrator.Refactored.cs:14); refactor by choosing one and deleting/obsoleting the other.
- Low | S | Dependency mismatch: Avalonia.Controls.ItemsRepeater is 11.1.5 while Avalonia core is 11.3.10 (Directory.Packages.props:13); refactor by aligning the version.
- Low | S | Dependency duplication/platform: both SQLite providers referenced and Windows-only packages in a cross-platform UI (src/SaveState.Core/SaveState.Core.csproj:11, src/SaveState.Core/SaveState.Core.csproj:28, src/SaveState.Core/SaveState.Core.csproj:29); refactor by removing unused provider and conditioning Windows-only packages.
- Low | S | Netplay code: session code builder inserts two hyphens (src/SaveState.Core/Services/Netplay/NetplayService.cs:372, src/SaveState.Core/Services/Netplay/NetplayService.cs:374); refactor to generate a single XXXX-XXXX format.

No tests run (audit only).

Next steps if you want implementation help:
1) Pick the top 1�2 high-severity items to fix first (DI/service locator and config are the fastest wins).
2) Decide whether to prioritize Netplay protocol hardening or DB migration cleanup.
3) If desired, I can draft a concrete migration plan and start the refactor work.
