using System.Reflection;
using SaveState.Application.GameLibrary.Commands;
using SaveState.Core.GameLibrary.Entities;
using SaveState.Infrastructure.Persistence;

var coreAssembly = typeof(Game).Assembly;
var applicationAssembly = typeof(CreateGameCommand).Assembly;
var infrastructureAssembly = typeof(SaveStateDbContext).Assembly;

var allTypes = GetAllSolutionTypes(coreAssembly, applicationAssembly, infrastructureAssembly);
var serviceTypes = allTypes
    .Where(t => t.IsClass && !t.IsAbstract)
    .Where(t => t.Name.EndsWith("Service", StringComparison.Ordinal) ||
                t.Name.EndsWith("Provider", StringComparison.Ordinal) ||
                t.Name.EndsWith("Client", StringComparison.Ordinal))
    .Where(t => !t.Name.Contains("Plugin", StringComparison.Ordinal))
    .ToList();

var failurePronePatterns = new[] { "get", "find", "load", "fetch", "resolve", "parse", "validate", "process" };

var violations = new List<Violation>();
foreach (var serviceType in serviceTypes)
{
    var methods = serviceType.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
        .Where(m => !m.IsSpecialName)
        .Where(m => m.DeclaringType == serviceType)
        .ToList();

    foreach (var method in methods)
    {
        var returnType = method.ReturnType;

        if (returnType == typeof(void) || returnType == typeof(Task))
            continue;

        if (IsResultLikeType(returnType))
            continue;

        if (IsNullableValueType(returnType))
            continue;

        if (!method.IsPublic)
            continue;

        var methodName = method.Name.ToLowerInvariant();
        if (failurePronePatterns.Any(p => methodName.Contains(p, StringComparison.Ordinal)))
        {
            violations.Add(new Violation(
                serviceType.FullName ?? serviceType.Name,
                serviceType.Name,
                method.Name,
                returnType.FullName ?? returnType.Name,
                method.ToString() ?? method.Name));
        }
    }
}

var ordered = violations
    .OrderBy(v => v.TypeName, StringComparer.Ordinal)
    .ThenBy(v => v.Method, StringComparer.Ordinal)
    .ToList();

var outputPath = Path.Combine("tools", "_guardrail_result_candidates.tsv");
Directory.CreateDirectory("tools");
using (var writer = new StreamWriter(outputPath, false))
{
    writer.WriteLine("TypeName\tMethod\tReturnType\tTypeFullName\tSignature");
    foreach (var v in ordered)
    {
        writer.WriteLine($"{v.TypeName}\t{v.Method}\t{v.ReturnType}\t{v.TypeFullName}\t{v.Signature}");
    }
}

Console.WriteLine($"TOTAL_VIOLATIONS={ordered.Count}");
Console.WriteLine($"OUTPUT={outputPath}");
Console.WriteLine();
Console.WriteLine("Top classes by violation count:");
foreach (var group in ordered.GroupBy(v => v.TypeName).OrderByDescending(g => g.Count()).ThenBy(g => g.Key, StringComparer.Ordinal).Take(30))
{
    Console.WriteLine($"{group.Count(),4}  {group.Key}");
}

Console.WriteLine();
Console.WriteLine("First 60 candidates:");
foreach (var v in ordered.Take(60))
{
    Console.WriteLine($"{v.TypeName}.{v.Method} -> {v.ReturnType}");
}

return;

static List<Type> GetAllSolutionTypes(params Assembly[] assemblies)
{
    var types = new List<Type>();
    foreach (var assembly in assemblies)
    {
        types.AddRange(GetTypesFromAssembly(assembly));
    }

    return types;
}

static List<Type> GetTypesFromAssembly(Assembly assembly)
{
    try
    {
        return assembly.GetTypes()
            .Where(t => t.IsPublic || t.IsNestedPublic)
            .ToList();
    }
    catch (ReflectionTypeLoadException ex)
    {
        return ex.Types.Where(t => t != null).Cast<Type>().ToList();
    }
}

static bool IsNullableValueType(Type type)
{
    if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
        return true;

    if (type.IsGenericType &&
        (type.GetGenericTypeDefinition() == typeof(Task<>) ||
         type.GetGenericTypeDefinition() == typeof(ValueTask<>)))
    {
        var innerType = type.GetGenericArguments()[0];
        if (innerType.IsGenericType && innerType.GetGenericTypeDefinition() == typeof(Nullable<>))
            return true;
    }

    return false;
}

static bool IsResultLikeType(Type type)
{
    if (IsResultType(type))
        return true;

    if (type.IsGenericType &&
        (type.GetGenericTypeDefinition() == typeof(Task<>) ||
         type.GetGenericTypeDefinition() == typeof(ValueTask<>)))
    {
        var innerType = type.GetGenericArguments()[0];
        return IsResultType(innerType);
    }

    return false;
}

static bool IsResultType(Type type)
{
    if (type.Name.StartsWith("Result", StringComparison.Ordinal))
        return true;

    if (type.IsGenericType && type.GetGenericTypeDefinition().Name.StartsWith("Result", StringComparison.Ordinal))
        return true;

    return false;
}

internal sealed record Violation(
    string TypeFullName,
    string TypeName,
    string Method,
    string ReturnType,
    string Signature);
