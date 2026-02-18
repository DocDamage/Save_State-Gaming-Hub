using System.Reflection;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;

namespace SaveState.Infrastructure.Tests.Architecture;

/// <summary>
/// Code quality architecture tests to enforce patterns established during technical debt remediation.
/// These tests prevent regression of null safety, Result pattern, and time provider usage.
/// </summary>
public class CodeQualityTests
{
    private readonly ITestOutputHelper _testOutputHelper;
    
    public CodeQualityTests(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
    }
    
    private readonly Assembly _coreAssembly = typeof(SaveState.Core.GameLibrary.Entities.Game).Assembly;
    private readonly Assembly _applicationAssembly = typeof(SaveState.Application.GameLibrary.Commands.CreateGameCommand).Assembly;
    private readonly Assembly _infrastructureAssembly = typeof(SaveState.Infrastructure.Persistence.SaveStateDbContext).Assembly;

    #region Result Pattern Tests

    [Fact]
    public void Public_Service_Methods_Should_Return_Result_For_Operations_That_Can_Fail()
    {
        // This test verifies that public service methods use Result<T> pattern
        // instead of returning null on failure
        var allTypes = GetAllSolutionTypes();
        var serviceTypes = allTypes
            .Where(t => t.IsClass && !t.IsAbstract)
            .Where(t => t.Name.EndsWith("Service") || t.Name.EndsWith("Provider") || t.Name.EndsWith("Client"))
            .Where(t => !t.Name.Contains("Plugin"))
            .ToList();

        var violations = new List<string>();
        
        foreach (var serviceType in serviceTypes)
        {
            var methods = serviceType.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                .Where(m => !m.IsSpecialName) // Exclude property accessors
                .Where(m => m.DeclaringType == serviceType) // Only methods declared in this type
                .ToList();

            foreach (var method in methods)
            {
                var returnType = method.ReturnType;
                
                // Skip void returns
                if (returnType == typeof(void) || returnType == typeof(Task))
                    continue;
                
                // Skip methods that already return Result<T>
                if (returnType.Name.StartsWith("Result") || 
                    (returnType.IsGenericType && returnType.GetGenericTypeDefinition().Name.StartsWith("Result")))
                    continue;
                
                // Skip methods that return nullable value types (acceptable pattern)
                if (IsNullableValueType(returnType))
                    continue;
                
                // Skip private/internal helpers (we check public APIs)
                if (!method.IsPublic)
                    continue;
                
                // Check if method name suggests it could fail
                var methodName = method.Name.ToLowerInvariant();
                var failurePronePatterns = new[] { "get", "find", "load", "fetch", "resolve", "parse", "validate", "process" };
                
                if (failurePronePatterns.Any(p => methodName.Contains(p)))
                {
                    // This method should probably return Result<T>
                    violations.Add($"{serviceType.Name}.{method.Name} returns {returnType.Name} but should use Result<T>");
                }
            }
        }

        if (violations.Any())
        {
            _testOutputHelper.WriteLine($"Methods that should use Result<T> pattern: {violations.Count}");
            foreach (var violation in violations.Take(10))
            {
                _testOutputHelper.WriteLine($"  - {violation}");
            }
        }
        
        // Baseline: 548 methods need Result<T> (Feb 2026)
        // Note: Many are acceptable patterns (nullable types for "not found")
        // This test documents the current state for gradual improvement
        violations.Count.Should().BeLessThanOrEqualTo(550, 
            $"{violations.Count} public methods should potentially use Result<T> pattern");
    }

    #endregion

    #region ITimeProvider Usage Tests

    [Fact]
    public void Services_Should_Depend_On_ITimeProvider()
    {
        // Verify that services use ITimeProvider instead of DateTime
        var allTypes = GetAllSolutionTypes();
        var serviceTypes = allTypes
            .Where(t => t.IsClass && !t.IsAbstract)
            .Where(t => t.Name.EndsWith("Service"))
            .Where(t => !t.Name.Contains("Plugin"))
            .ToList();

        var servicesWithTimeProvider = new List<string>();
        var servicesWithoutTimeProvider = new List<string>();

        foreach (var serviceType in serviceTypes)
        {
            var constructors = serviceType.GetConstructors();
            var hasTimeProvider = constructors.Any(c => 
                c.GetParameters().Any(p => p.ParameterType.Name.Contains("TimeProvider")));

            if (hasTimeProvider)
            {
                servicesWithTimeProvider.Add(serviceType.Name);
            }
            else
            {
                // Check if it has any date/time related methods
                var hasDateTimeMethods = serviceType.GetMethods()
                    .Any(m => m.ReturnType == typeof(DateTime) || 
                              m.ReturnType == typeof(DateTimeOffset) ||
                              m.GetParameters().Any(p => p.ParameterType == typeof(DateTime)));
                
                if (hasDateTimeMethods)
                {
                    servicesWithoutTimeProvider.Add(serviceType.Name);
                }
            }
        }

        _testOutputHelper.WriteLine($"Services with ITimeProvider: {servicesWithTimeProvider.Count}");
        _testOutputHelper.WriteLine($"Services possibly needing ITimeProvider: {servicesWithoutTimeProvider.Count}");
        
        // Most services should use ITimeProvider
        servicesWithTimeProvider.Count.Should().BeGreaterThanOrEqualTo(20, 
            "At least 20 services should depend on ITimeProvider for testability");
    }

    #endregion

    #region Async Pattern Tests

    [Fact]
    public void Async_Methods_Should_Follow_Naming_Convention()
    {
        var allTypes = GetAllSolutionTypes();
        var violations = new List<string>();

        foreach (var type in allTypes.Where(t => t.IsClass))
        {
            var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .Where(m => m.DeclaringType == type)
                .ToList();

            foreach (var method in methods)
            {
                var returnType = method.ReturnType;
                
                // Check if method returns Task or Task<T>
                if (returnType == typeof(Task) || 
                    (returnType.IsGenericType && returnType.GetGenericTypeDefinition() == typeof(Task<>)))
                {
                    // Should end with Async
                    if (!method.Name.EndsWith("Async"))
                    {
                        violations.Add($"{type.Name}.{method.Name}");
                    }
                }
            }
        }

        if (violations.Any())
        {
            _testOutputHelper.WriteLine($"Async methods without Async suffix: {violations.Count}");
            foreach (var v in violations.Take(10))
            {
                _testOutputHelper.WriteLine($"  - {v}");
            }
        }

        // Baseline: 255 methods without Async suffix (Feb 2026)
        // Goal: Gradually rename to follow convention
        violations.Count.Should().BeLessThanOrEqualTo(260,
            $"{violations.Count} async methods don't follow Async naming convention");
    }

    #endregion

    #region Helper Methods

    private List<Type> GetAllSolutionTypes()
    {
        var types = new List<Type>();
        types.AddRange(GetTypesFromAssembly(_coreAssembly));
        types.AddRange(GetTypesFromAssembly(_applicationAssembly));
        types.AddRange(GetTypesFromAssembly(_infrastructureAssembly));
        return types;
    }

    private List<Type> GetTypesFromAssembly(Assembly assembly)
    {
        try
        {
            return assembly.GetTypes()
                .Where(t => t.IsPublic || t.IsNestedPublic)
                .ToList();
        }
        catch (ReflectionTypeLoadException ex)
        {
            return ex.Types.Where(t => t != null).ToList()!;
        }
    }

    private bool IsNullableValueType(Type type)
    {
        // Check if it's a nullable value type (T?)
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
            return true;
        
        // Check if it's Task<T?> or Task<Nullable<T>>
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Task<>))
        {
            var innerType = type.GetGenericArguments()[0];
            if (innerType.IsGenericType && innerType.GetGenericTypeDefinition() == typeof(Nullable<>))
                return true;
        }
        
        return false;
    }

    #endregion
}
