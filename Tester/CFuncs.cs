using MenuCli;

namespace Tester {
    class CFuncs {
        private bool _Visibility = true;
        public bool GetVisibility() => _Visibility;
        public MenuManager.MenuResponse ChangeVisibility() {
            _Visibility = ! _Visibility;
            return new();
        }
    }
}