using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace BSU.CLI
{
    [AttributeUsage(AttributeTargets.Method)]
    internal class CliCommand : Attribute
    {
        public string Name, Description, Usage;
        public CliCommand(string name, string description, string usage=null)
        {
            Name = name;
            Description = description;
            Usage = usage;
        }
    }

    class Commands
    {
        private Dictionary<string, Command> _commands = new Dictionary<string, Command>();
        public Commands(object actionsClass)
        {
            var methods = actionsClass.GetType().GetMethods(BindingFlags.NonPublic | BindingFlags.Instance);
            foreach (var methodInfo in methods)
            {
                var attr = methodInfo.GetCustomAttributes(false).OfType<CliCommand>().SingleOrDefault();
                if (attr == null) continue;
                var func = (Action<string[]>) methodInfo.CreateDelegate(typeof(Action<string[]>), actionsClass);
                _commands[attr.Name] = new Command(attr.Name, attr.Description, attr.Usage, func);
            }
        }

        public void Process(string line)
        {
            var command = ParseCommand(line, out var args);
            if (command == null)
            {
                Help();
                return;
            }

            command.Func(args);
        }

        private Command ParseCommand(string line, out string[] args)
        {
            args = null;
            if (line == null) return null;

            var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0) return null;

            if (!_commands.TryGetValue(parts[0], out var command)) return null;

            args = parts.Skip(1).ToArray();
            return command;
        }

        private void Help()
        {
            Console.WriteLine("Available commands:");
            Console.WriteLine("help - prints this help.");
            Console.WriteLine("exit - exit (duh).");
            foreach (var command in _commands)
            {
                Console.WriteLine($"{command.Key} - {command.Value.Description}" +
                                  (command.Value == null ? "" : $" Usage: {command.Key} {command.Value.Usage}"));
            }
        }

        class Command
        {
            public string Name, Description, Usage;
            public Action<string[]> Func;

            public Command(string name, string description, string usage, Action<string[]> func)
            {
                Name = name;
                Description = description;
                Usage = usage;
                Func = func;
            }
        }
    }
}
