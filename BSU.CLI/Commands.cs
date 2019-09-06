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
        public CliCommand(string name, string description, string usage)
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
