using System;
using MenuCli;
using CommandLine;
using System.Collections.Generic;
using System.Linq;

namespace Tester {
    public class Options {
        [Option('c', "credits", Required = false, HelpText = "Credits.")]
        public bool Credits { get; set; }
        [Option('r', "run", Required = false, HelpText = "Launch a command and execute it instantly.")]
        public IEnumerable<string> Run { get; set; }
    }
    class Program {
        static void Main(string[] args) {
            MenuManager mm = new("Menu con net 5");
            CFuncs funcs = new();
            var mnu_1 = new CMenu("1", "Prova") { Func = () => {
                Console.WriteLine("Prova 1");
                return new MenuManager.MenuResponse(); 
            } };
            var mnu_2 = new CMenu("2", "Prova");
            var mnu_2_1 = new CMenu("1", "SubMenu 1") { Ancestor = mnu_2, Func = () => {
                Console.WriteLine("SubMenu 1");
                return new();
            }};
            var mnu_2_2 = new CMenu("2", "SubMenu 2") { Ancestor = mnu_2, Func = () => {
                Console.WriteLine("SubMenu 2");
                return new();
            }};

            mm.AddMenu(mnu_1);
            mm.AddMenu(mnu_2);
            mm.AddMenu(mnu_2_1);
            mm.AddMenu(mnu_2_2);

            mm.AddMenu(new List<IMenu>() {
                new CMenuVar<bool>("3", "Set visibility", funcs.GetVisibility) { Func = funcs.ChangeVisibility }
            });

            Parser.Default.ParseArguments<Options>(args).WithParsed<Options>(o => {
                if (o.Credits) {
                    Console.WriteLine("Code by the marvelous Sm3P!");
                } else {
                    //  Check for instant command
                    if(o.Run.Any()) {
                        mm.InstantInterpreter(o.Run);
                    } else {
                        mm.InteractiveMode();
                    }
                }
            });
        }
    }
}
