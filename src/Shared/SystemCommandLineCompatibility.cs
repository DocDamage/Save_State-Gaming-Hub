using System.CommandLine.Parsing;
using System.Linq;

namespace System.CommandLine
{
    internal static class SystemCommandLineCompatibilityExtensions
    {
        public static void AddOption(this Command command, Option option) => command.Add(option);

        public static void AddArgument(this Command command, Argument argument) => command.Add(argument);

        public static void AddCommand(this Command command, Command childCommand) => command.Add(childCommand);

        public static void AddAlias(this Command command, string alias) => command.Aliases.Add(alias);

        public static Task<int> InvokeAsync(this RootCommand rootCommand, string[] args) =>
            rootCommand.Parse(args).InvokeAsync();

        public static void SetHandler(this Command command, Action handler) =>
            command.SetAction(_ => handler());

        public static void SetHandler(this Command command, Func<Task> handler) =>
            command.SetAction(_ => handler());

        public static void SetHandler(this Command command, Action<global::System.CommandLine.Invocation.InvocationContext> handler) =>
            command.SetAction((parseResult, cancellationToken) =>
            {
                handler(new global::System.CommandLine.Invocation.InvocationContext(parseResult, cancellationToken));
                return Task.CompletedTask;
            });

        public static void SetHandler(this Command command, Func<global::System.CommandLine.Invocation.InvocationContext, Task> handler) =>
            command.SetAction((parseResult, cancellationToken) =>
                handler(new global::System.CommandLine.Invocation.InvocationContext(parseResult, cancellationToken)));

        public static void SetHandler<T1>(this Command command, Action<T1> handler) =>
            command.SetAction(parseResult =>
                handler(Bind<T1>(command, parseResult, 0)));

        public static void SetHandler<T1>(this Command command, Func<T1, Task> handler) =>
            command.SetAction(parseResult =>
                handler(Bind<T1>(command, parseResult, 0)));

        public static void SetHandler<T1, T2>(this Command command, Action<T1, T2> handler) =>
            command.SetAction(parseResult =>
                handler(Bind<T1>(command, parseResult, 0), Bind<T2>(command, parseResult, 1)));

        public static void SetHandler<T1, T2>(this Command command, Func<T1, T2, Task> handler) =>
            command.SetAction(parseResult =>
                handler(Bind<T1>(command, parseResult, 0), Bind<T2>(command, parseResult, 1)));

        public static void SetHandler<T1, T2, T3>(this Command command, Action<T1, T2, T3> handler) =>
            command.SetAction(parseResult =>
                handler(Bind<T1>(command, parseResult, 0), Bind<T2>(command, parseResult, 1), Bind<T3>(command, parseResult, 2)));

        public static void SetHandler<T1, T2, T3>(this Command command, Func<T1, T2, T3, Task> handler) =>
            command.SetAction(parseResult =>
                handler(Bind<T1>(command, parseResult, 0), Bind<T2>(command, parseResult, 1), Bind<T3>(command, parseResult, 2)));

        public static void SetHandler<T1, T2, T3, T4>(this Command command, Action<T1, T2, T3, T4> handler) =>
            command.SetAction(parseResult =>
                handler(Bind<T1>(command, parseResult, 0), Bind<T2>(command, parseResult, 1), Bind<T3>(command, parseResult, 2), Bind<T4>(command, parseResult, 3)));

        public static void SetHandler<T1, T2, T3, T4>(this Command command, Func<T1, T2, T3, T4, Task> handler) =>
            command.SetAction(parseResult =>
                handler(Bind<T1>(command, parseResult, 0), Bind<T2>(command, parseResult, 1), Bind<T3>(command, parseResult, 2), Bind<T4>(command, parseResult, 3)));

        public static void SetHandler<T1, T2, T3, T4, T5>(this Command command, Action<T1, T2, T3, T4, T5> handler) =>
            command.SetAction(parseResult =>
                handler(Bind<T1>(command, parseResult, 0), Bind<T2>(command, parseResult, 1), Bind<T3>(command, parseResult, 2), Bind<T4>(command, parseResult, 3), Bind<T5>(command, parseResult, 4)));

        public static void SetHandler<T1, T2, T3, T4, T5>(this Command command, Func<T1, T2, T3, T4, T5, Task> handler) =>
            command.SetAction(parseResult =>
                handler(Bind<T1>(command, parseResult, 0), Bind<T2>(command, parseResult, 1), Bind<T3>(command, parseResult, 2), Bind<T4>(command, parseResult, 3), Bind<T5>(command, parseResult, 4)));

        public static void SetHandler<T1>(this Command command, Action<T1> handler, Symbol symbol1) =>
            command.SetAction(parseResult =>
                handler(Bind<T1>(parseResult, symbol1)));

        public static void SetHandler<T1>(this Command command, Func<T1, Task> handler, Symbol symbol1) =>
            command.SetAction(parseResult =>
                handler(Bind<T1>(parseResult, symbol1)));

        public static void SetHandler<T1, T2>(this Command command, Action<T1, T2> handler, Symbol symbol1, Symbol symbol2) =>
            command.SetAction(parseResult =>
                handler(Bind<T1>(parseResult, symbol1), Bind<T2>(parseResult, symbol2)));

        public static void SetHandler<T1, T2>(this Command command, Func<T1, T2, Task> handler, Symbol symbol1, Symbol symbol2) =>
            command.SetAction(parseResult =>
                handler(Bind<T1>(parseResult, symbol1), Bind<T2>(parseResult, symbol2)));

        public static void SetHandler<T1, T2, T3>(this Command command, Action<T1, T2, T3> handler, Symbol symbol1, Symbol symbol2, Symbol symbol3) =>
            command.SetAction(parseResult =>
                handler(Bind<T1>(parseResult, symbol1), Bind<T2>(parseResult, symbol2), Bind<T3>(parseResult, symbol3)));

        public static void SetHandler<T1, T2, T3>(this Command command, Func<T1, T2, T3, Task> handler, Symbol symbol1, Symbol symbol2, Symbol symbol3) =>
            command.SetAction(parseResult =>
                handler(Bind<T1>(parseResult, symbol1), Bind<T2>(parseResult, symbol2), Bind<T3>(parseResult, symbol3)));

        public static void SetHandler<T1, T2, T3, T4>(this Command command, Action<T1, T2, T3, T4> handler, Symbol symbol1, Symbol symbol2, Symbol symbol3, Symbol symbol4) =>
            command.SetAction(parseResult =>
                handler(Bind<T1>(parseResult, symbol1), Bind<T2>(parseResult, symbol2), Bind<T3>(parseResult, symbol3), Bind<T4>(parseResult, symbol4)));

        public static void SetHandler<T1, T2, T3, T4>(this Command command, Func<T1, T2, T3, T4, Task> handler, Symbol symbol1, Symbol symbol2, Symbol symbol3, Symbol symbol4) =>
            command.SetAction(parseResult =>
                handler(Bind<T1>(parseResult, symbol1), Bind<T2>(parseResult, symbol2), Bind<T3>(parseResult, symbol3), Bind<T4>(parseResult, symbol4)));

        public static void SetHandler<T1, T2, T3, T4, T5>(this Command command, Action<T1, T2, T3, T4, T5> handler, Symbol symbol1, Symbol symbol2, Symbol symbol3, Symbol symbol4, Symbol symbol5) =>
            command.SetAction(parseResult =>
                handler(Bind<T1>(parseResult, symbol1), Bind<T2>(parseResult, symbol2), Bind<T3>(parseResult, symbol3), Bind<T4>(parseResult, symbol4), Bind<T5>(parseResult, symbol5)));

        public static void SetHandler<T1, T2, T3, T4, T5>(this Command command, Func<T1, T2, T3, T4, T5, Task> handler, Symbol symbol1, Symbol symbol2, Symbol symbol3, Symbol symbol4, Symbol symbol5) =>
            command.SetAction(parseResult =>
                handler(Bind<T1>(parseResult, symbol1), Bind<T2>(parseResult, symbol2), Bind<T3>(parseResult, symbol3), Bind<T4>(parseResult, symbol4), Bind<T5>(parseResult, symbol5)));

        private static T Bind<T>(Command command, ParseResult parseResult, int index)
        {
            var symbol = GetBindingSymbol(command, index);
            if (symbol is null)
            {
                return default!;
            }

            return Bind<T>(parseResult, symbol);
        }

        private static T Bind<T>(ParseResult parseResult, Symbol symbol)
        {
            try
            {
                return parseResult.GetValue<T>(symbol.Name);
            }
            catch
            {
                return default!;
            }
        }

        private static Symbol? GetBindingSymbol(Command command, int index)
        {
            if (index < command.Arguments.Count)
            {
                return command.Arguments.ElementAt(index);
            }

            var optionIndex = index - command.Arguments.Count;
            if (optionIndex < command.Options.Count)
            {
                return command.Options.ElementAt(optionIndex);
            }

            return null;
        }
    }
}

namespace System.CommandLine.Invocation
{
    public sealed class InvocationContext
    {
        private readonly CancellationToken _cancellationToken;

        internal InvocationContext(ParseResult parseResult, CancellationToken cancellationToken)
        {
            ParseResult = new CompatibilityParseResult(parseResult);
            _cancellationToken = cancellationToken;
        }

        public CompatibilityParseResult ParseResult { get; }

        public int ExitCode { get; set; }

        public CancellationToken GetCancellationToken() => _cancellationToken;
    }

    public sealed class CompatibilityParseResult
    {
        private readonly ParseResult _parseResult;

        internal CompatibilityParseResult(ParseResult parseResult)
        {
            _parseResult = parseResult;
        }

        public IReadOnlyList<string> UnmatchedTokens => _parseResult.UnmatchedTokens;

        public T GetValueForArgument<T>(Argument<T> argument) => _parseResult.GetValue(argument);

        public T GetValueForOption<T>(Option<T> option) => _parseResult.GetValue(option);
    }
}
