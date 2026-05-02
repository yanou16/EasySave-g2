using EasySave.Views.Console.Bootstrap;
using EasySave.Views.Console.Cli;
using EasySave.Views.Console.ConsoleUi;


var bootstrapper = new AppBootstrapper();
var appContext   = bootstrapper.Create();

var parser      = new CommandLineParser();
var parseResult = parser.Parse(args);

var app = new ConsoleApp(appContext);
return app.Run(parseResult);
