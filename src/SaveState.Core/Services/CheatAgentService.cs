using System.Text.RegularExpressions;
using SaveState.Core.Interfaces;
using SaveState.Core.Services;
using Serilog;

namespace SaveState.Core.Services;

public class CheatAgentService
{
    private readonly IAiService _aiService;
    private readonly IMemoryScannerService _scannerService;
    private readonly IProcessService _processService;
    private readonly ITrainerService _trainerService;
    private readonly IKnowledgeService? _knowledgeService;
    private readonly IMemoryAnomalyService? _anomalyService;
    private readonly ILogger _logger = Log.ForContext<CheatAgentService>();

    public CheatAgentService(
        IAiService aiService, 
        IMemoryScannerService scannerService, 
        IProcessService processService, 
        ITrainerService trainerService,
        IKnowledgeService? knowledgeService = null,
        IMemoryAnomalyService? anomalyService = null)
    {
        _aiService = aiService;
        _scannerService = scannerService;
        _processService = processService;
        _trainerService = trainerService;
        _knowledgeService = knowledgeService;
        _anomalyService = anomalyService;
    }

    public async Task<string> ProcessUserRequestAsync(string userRequest)
    {
        // 1. Check if we are attached
        if (_scannerService.CurrentProcessId == null)
        {
             // Try to infer game from request or just ask user to attach
             // specific logic to auto-attach could go here, for now we prompt
             return "I am not attached to any game process. Please start a game and tell me which one to attach to (e.g., 'Attach to Notepad').";
        }

        // 2. Retrieve relevant knowledge context (RAG)
        var knowledgeContext = "";
        if (_knowledgeService != null)
        {
            try
            {
                knowledgeContext = await _knowledgeService.GetRelevantContextAsync(userRequest);
                if (!string.IsNullOrEmpty(knowledgeContext))
                {
                    _logger.Debug("RAG: Retrieved {Len} chars of context", knowledgeContext.Length);
                }
            }
            catch (Exception ex)
            {
                _logger.Warning(ex, "Failed to retrieve RAG context");
            }
        }

        // 3. Check for anomalies (MBAD)
        var anomalyWarning = "";
        if (_anomalyService != null && _anomalyService.IsCheatDetected)
        {
            anomalyWarning = $"\n⚠️ ANOMALY DETECTED: External memory modification detected (confidence: {_anomalyService.CurrentAnomalyScore:P0}). Proceed with caution.\n";
        }

        // 4. Construct Prompt with RAG context
        var ragSection = string.IsNullOrEmpty(knowledgeContext) 
            ? "" 
            : $"""
            
            RELEVANT KNOWLEDGE:
            {knowledgeContext}
            
            Use the above knowledge to inform your response if relevant.
            """;

        var prompt = $$"""
            You are an expert game cheating agent. You have access to a memory scanner attached to process ID {{_scannerService.CurrentProcessId}}.
            {{anomalyWarning}}{{ragSection}}
            User Request: "{{userRequest}}"
            
            Available Tools (Call them by writing the command exactly as shown):
            - SCAN: <value> (Scans memory for an integer value. Returns count of found addresses.)
            - SCAN_FLOAT: <value> (Scans memory for a FLOAT value. Use if integer scan fails for stats.)
            - NEXT_SCAN: <value> (Filters previous scan results for new value.)
            - WRITE: <address_dec> <value> (Writes an integer to an address.)
            - WRITE_FLOAT: <address_dec> <value> (Writes a FLOAT to an address.)
            - READ: <address_dec> (Reads value at an address.)
            - FIND_POINTER: <address_dec> (Finds the pointer path to an address. Use for static cheats.)
            - CREATE_CHEAT: <name> <address/pointer> <type> <value> (Creates a permanent cheat button in the Trainer tool.)
            
            If you need to perform actions, output the command on a single line. 
            If you need to explain something to the user, just write it.
            Do not provide C# code, use the tools directly.
            """;

        // 5. Get AI Response
        var response = await _aiService.ChatAsync(prompt);

        // 4. Parse execution loop
        // If response contains commands, execute them and feed back? 
        // For this MVP, we will handle single-turn execution or simple command parsing.
        
        var lines = response.Split('\n');
        var outputLog = new System.Text.StringBuilder();
        outputLog.AppendLine(response); // Include original thought

        foreach (var line in lines)
        {
            var trimmed = line.Trim();
            if (trimmed.StartsWith("SCAN:"))
            {
                if (int.TryParse(trimmed.Substring(5).Trim(), out int val))
                {
                    _logger.Information("Agent requesting SCAN: {Value}", val);
                    var results = await _scannerService.ScanInt32Async(val);
                    outputLog.AppendLine($"[System] Scan complete. Found {results.Count} addresses.");
                    
                    if (results.Count > 0 && results.Count < 5)
                    {
                        outputLog.AppendLine($"[System] Addresses: " + string.Join(", ", results));
                    }
                }
            }
            if (trimmed.StartsWith("SCAN_FLOAT:"))
            {
                if (float.TryParse(trimmed.Substring(11).Trim(), out float val))
                {
                    _logger.Information("Agent requesting SCAN_FLOAT: {Value}", val);
                    var results = await _scannerService.ScanFloatAsync(val);
                    outputLog.AppendLine($"[System] Scan complete. Found {results.Count} addresses.");
                    
                    if (results.Count > 0 && results.Count < 5)
                    {
                        outputLog.AppendLine($"[System] Addresses: " + string.Join(", ", results));
                    }
                }
            }
            if (trimmed.StartsWith("WRITE_FLOAT:"))
            {
               var parts = trimmed.Substring(12).Trim().Split(' ');
               if (parts.Length == 2 && long.TryParse(parts[0], out long addr) && float.TryParse(parts[1], out float val))
               {
                   _scannerService.WriteFloat(addr, val);
                   outputLog.AppendLine($"[System] Wrote float {val} to {addr}.");
               }
            }
            if (trimmed.StartsWith("FIND_POINTER:"))
            {
                if (long.TryParse(trimmed.Substring(13).Trim(), out long addr))
                {
                     _logger.Information("Agent requesting FIND_POINTER: {Addr}", addr);
                     var ptr = await _scannerService.ScanForPointerAsync(addr);
                     if (ptr != null)
                        outputLog.AppendLine($"[System] Found Pointer: {ptr}");
                     else
                        outputLog.AppendLine($"[System] No pointer found.");
                }
            }
            if (trimmed.StartsWith("CREATE_CHEAT:"))
            {
                // Format: CREATE_CHEAT: "Infinite Health" "ff6.exe+0x123" "int" "9999"
                var match = Regex.Match(trimmed, "CREATE_CHEAT: \"(.*?)\" \"(.*?)\" \"(.*?)\" \"(.*?)\"");
                if (match.Success)
                {
                    var name = match.Groups[1].Value;
                    var addr = match.Groups[2].Value;
                    var type = match.Groups[3].Value;
                    var val = match.Groups[4].Value;
                    
                    var procName = _processService.GetProcessById(_scannerService.CurrentProcessId.Value)?.ProcessName ?? "Unknown";
                    await _trainerService.CreateCheatAsync(procName, name, addr, type, val, true);
                    outputLog.AppendLine($"[System] Created trainer cheat '{name}'.");
                }
            }
            if (trimmed.StartsWith("NEXT_SCAN:"))
            {
                if (int.TryParse(trimmed.Substring(10).Trim(), out int val))
                {
                    _logger.Information("Agent requesting NEXT_SCAN: {Value}", val);
                    var results = await _scannerService.NextScanInt32Async(val);
                    outputLog.AppendLine($"[System] Next scan complete. {results.Count} addresses remaining.");
                    
                    if (results.Count > 0 && results.Count <= 10)
                    {
                        outputLog.AppendLine($"[System] Addresses: " + string.Join(", ", results));
                    }
                    else if (results.Count == 1)
                    {
                        outputLog.AppendLine($"[System] Found single address: {results[0]}. Ready to WRITE or CREATE_CHEAT.");
                    }
                }
            }
            if (trimmed.StartsWith("NEXT_SCAN_FLOAT:"))
            {
                if (float.TryParse(trimmed.Substring(16).Trim(), out float val))
                {
                    _logger.Information("Agent requesting NEXT_SCAN_FLOAT: {Value}", val);
                    var results = await _scannerService.NextScanFloatAsync(val);
                    outputLog.AppendLine($"[System] Next scan complete. {results.Count} addresses remaining.");
                    
                    if (results.Count > 0 && results.Count <= 10)
                    {
                        outputLog.AppendLine($"[System] Addresses: " + string.Join(", ", results));
                    }
                }
            }
            if (trimmed.StartsWith("READ:"))
            {
                if (long.TryParse(trimmed.Substring(5).Trim(), out long addr))
                {
                    var value = _scannerService.ReadInt32(addr);
                    outputLog.AppendLine($"[System] Value at {addr}: {value}");
                }
            }
            if (trimmed.StartsWith("WRITE:"))
            {
                var parts = trimmed.Substring(6).Trim().Split(' ');
                if (parts.Length == 2 && long.TryParse(parts[0], out long addr) && int.TryParse(parts[1], out int val))
                {
                    _scannerService.WriteInt32(addr, val);
                    outputLog.AppendLine($"[System] Wrote {val} to address {addr}.");
                }
            }
        }

        return outputLog.ToString();
    }
    
    // Command to handle explicit "Attach to <Process>" requests from the chat
    public bool TryHandleSystemCommand(string message, out string response)
    {
        if (message.StartsWith("Attach to ", StringComparison.OrdinalIgnoreCase))
        {
            var procName = message.Substring(10).Trim();
            var processes = _processService.GetProcessesByName(procName);
            var proc = processes.FirstOrDefault();
            
            if (proc != null)
            {
                if (_scannerService.Attach(proc))
                {
                    response = $"Successfully attached to {proc.ProcessName} ({proc.Id}).";
                    return true;
                }
            }
            
            // Try fuzzy search
            var all = _processService.GetProcesses();
            proc = all.FirstOrDefault(p => p.ProcessName.Contains(procName, StringComparison.OrdinalIgnoreCase));
             if (proc != null)
            {
                if (_scannerService.Attach(proc))
                {
                    response = $"Successfully attached to {proc.ProcessName} ({proc.Id}).";
                    return true;
                }
            }

            response = $"Could not find process '{procName}'.";
            return true;
        }
        
        response = "";
        return false;
    }
}
