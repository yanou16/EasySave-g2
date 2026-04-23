using EasySave.Console.Bootstrap;
using EasySave.Console.Cli;
using EasySave.Console.ConsoleUi;

// Keep the entry point thin: compose dependencies, parse arguments, dispatch execution.
var bootstrapper = new AppBootstrapper();
var appContext = bootstrapper.Create();

var parser = new CommandLineParser();
var parseResult = parser.Parse(args);

var app = new ConsoleApp(appContext);
return app.Run(parseResult);
