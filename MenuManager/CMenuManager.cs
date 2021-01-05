using System;
using System.Collections.Generic;
using System.Linq;

namespace MenuCli {
    public delegate MenuManager.MenuResponse HandlerFunc();
    public interface IMenu {
        string Selector {get; }
        string Title { get; }
        HandlerFunc Func { get; init; }
        bool ShowAtEveryLevel { get; init; }
        IMenu Ancestor { get; init; }
    }
    public class MenuManager {
        //  CTOR
        public MenuManager() : this("") { }
        public MenuManager(string iRootTitle) {
            BlankLinesBeforePrintMenu = 3;
			UseColors = true;
			CurrentMenu = null;
			RootTitle = iRootTitle;
			_Menu = new () {
				new CMenu("B", "Go Up") { Func = _GoBack },
				new CMenu("M", "Menu") { ShowAtEveryLevel = true, Func = _PrintMenu },
				new CMenu("Q", "Quit") { ShowAtEveryLevel = true, Func = _Quit }
			};
        }
        private List<IMenu> _Menu;
		private IMenu CurrentMenu { get; set; }
        private int _BlankLines = 0;
		public string RootTitle { get; set; }
		public int BlankLinesBeforePrintMenu { get; set; }
		public bool UseColors { get; set; }
        public record MenuResponse(bool Continue = true) {
            public bool IsOk { get; set; } = true;
            public string Message { get; set; } = string.Empty;
        }
        #region PUBLIC FUNCS
        public void AddMenu(IMenu iMenu) => AddMenu(new List<IMenu> { iMenu });
        public void AddMenu(List<IMenu> iMenus) {
			_Menu.AddRange(iMenus);
			//	Check if there are two or more menus with the same selector and with the same ancestor (which means they can't work together!)
			var sameMenuCheck = _Menu.GroupBy(m => new { m.Selector, m.Ancestor }).Any(g => g.Count() > 1);
            if(sameMenuCheck) {
				throw new Exception("Menus with duplicate selectors. Fatal error.");
			}
		}
        #endregion
        #region STANDARD FUNCS
		public MenuResponse _PrintMenu() {
			List<string> path = new();
			BuildPath(path, CurrentMenu);
			WritePath(path);
			var menuToShow = GetSelectablesMenus();
			menuToShow.ForEach(m => {
				//	Check if this is a menu father
				var subMenusPresent = _Menu.Any(sm => sm.Ancestor == m);
				var subMenuIdentifier = subMenusPresent ? " >>" : String.Empty;
				//var menuOutput = $"{m.Selector} - {m.Title}{subMenuIdentifier}";
				//Console.WriteLine(menuOutput);
				Console.WriteLine($"{m}{subMenuIdentifier}");
			});
            return new();
		}
		public MenuResponse _Quit() {
			Console.WriteLine("Bye bye, true believer");
			return new(false);
		}
		public MenuResponse _GoBack() {
			CurrentMenu = CurrentMenu?.Ancestor;
			return _PrintMenu();
		}
		#endregion
		#region PUBLIC METHODS
		public MenuResponse Interpreter(string iCommand, bool iShowMenu = true) {
			MenuResponse retValue = new MenuResponse();
			if(String.IsNullOrEmpty(iCommand)) {
				_BlankLines ++;
				if(_BlankLines == BlankLinesBeforePrintMenu) {
					_BlankLines = 0;
					iCommand = "m";
				} else {
					retValue.IsOk = true;
					return retValue;
				}
			}
			var commandFound = GetSelectablesMenus().FirstOrDefault(
				m => m.Selector.Equals(iCommand, StringComparison.InvariantCultureIgnoreCase)
			);
			if(commandFound != null) {
				//	Check if this is an ancestor menu
				bool hasSubMenus = _Menu.Any(m => m.Ancestor == commandFound);
				if(hasSubMenus) {
					//	This is a menu which leads to another menu
					CurrentMenu = _Menu.First(m => m == commandFound);
					if(iShowMenu) 
						retValue = _PrintMenu();
				} else {
					if(commandFound.Func != null)
						retValue = commandFound.Func();
					if(!retValue.IsOk) {
						Console.WriteLine(retValue.Message);
					}
				}
			} else if(iCommand != "") {
				retValue.IsOk = true;
				Console.WriteLine("Command not found");
			}
			return retValue;
		}
		public void InstantInterpreter(string iCommand) {
			const char MULTIPLE_COMMAND_SEPARATOR = ' ';
			//	Check if command is multiple commands
			List<string> commandsToExecute = new ();
			iCommand.Split(new char[] { MULTIPLE_COMMAND_SEPARATOR }, StringSplitOptions.RemoveEmptyEntries).ToList().ForEach(c => commandsToExecute.Add(c));
			//	Execution of multiple commands go ahead in any case
			InstantInterpreter(commandsToExecute);
		}
		public void InstantInterpreter(IEnumerable<string> iCommands) {
			const char SUBMENU_SEPARATOR = '-';
			//	Execution of multiple commands go ahead in any case
			iCommands.ToList().ForEach(c => {
				//	Search if this is a submenu
				var subMenus = c.Split(new char[] { SUBMENU_SEPARATOR });
				string lastCommand = c;
				if(subMenus.Count() > 1) {
					var executableSubMenus = subMenus.Take(subMenus.Count() - 1);
					foreach(string s in executableSubMenus)
						this.Interpreter(s, false);
					lastCommand = subMenus.Last();
				}
				//	Finally execute :-)
				Interpreter(lastCommand, false);
				CurrentMenu = null;
			});	
		}
		public void InteractiveMode(){
			_PrintMenu();
			var goOn = true;
			while(goOn) {
				Console.Write("->");
				var response = Interpreter(Console.ReadLine());
				goOn = response.Continue;
			}
		}
		#endregion
		#region PRIVATE METHODS
		private void BuildPath(List<string> iFullPath, IMenu iMenu) {
			if(iMenu != null) {
				iFullPath.Add(iMenu.Title);
				BuildPath(iFullPath, iMenu.Ancestor);
			}
		}
		private void WritePath(List<string> iPath) {
			iPath.Reverse();
			if(UseColors)
				Console.ForegroundColor = ConsoleColor.Cyan;
			Console.Write(RootTitle);
			iPath.ForEach(p => Console.Write($" >> {p}"));
			if(UseColors)
				Console.ResetColor();
			Console.WriteLine();
		}
		private List<IMenu> GetSelectablesMenus() {
			List<IMenu> retValue = new();
			if(CurrentMenu != null) {
				retValue.Add(_Menu.First(m => m.Func == _GoBack));
			}
			retValue.AddRange(
				_Menu.Where(m => m.Ancestor == CurrentMenu && m.Func != _GoBack || m.ShowAtEveryLevel)
			);
			//	My own sorting: numbers first, then letters
			Comparison<IMenu> comparer = (x, y) => Compare(x, y);
			retValue.Sort(comparer);
			return retValue;
		}
		private static int Compare(IMenu x, IMenu y) {
            var v = (xNumber : IsInteger(x.Selector), yNumber : IsInteger(y.Selector));

            return v switch {
                _ when (v.xNumber && v.yNumber)     => Convert.ToInt32(x.Selector).CompareTo(Convert.ToInt32(y.Selector)),
                _ when (v.xNumber && ! v.yNumber)   => -1,
                _ when (! v.xNumber && v.yNumber)   => 1,
                _                                   => x.Selector.CompareTo(y.Selector)
            };
		}
		private static bool IsInteger(string i) => Int32.TryParse(i, out int _);
		#endregion
    }
    public record CMenu(string Selector, string Title) : IMenu {
        public HandlerFunc Func { get; init; }
        public bool ShowAtEveryLevel { get; init; } = false;
        public IMenu Ancestor { get; init; }
        public override string ToString() => $"{Selector} - {Title}";
    }
	public record CMenuVar<T> : CMenu {
		public CMenuVar(string iSelector, string iTitle, Func<T> iValueGetter) : base(iSelector, iTitle) => ValueGetter = iValueGetter;
		public Func<T> ValueGetter { get; init; }
        public override string ToString() => base.ToString() + $" <{ValueGetter()}>";
	}
}
