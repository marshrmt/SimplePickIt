using ExileCore.Shared.Interfaces;
using ExileCore.Shared.Nodes;
using ExileCore.Shared.Attributes;
using System.Windows.Forms;

namespace SimplePickIt
{
    public class SimplePickItSettings : ISettings
    {
        public ToggleNode Enable { get; set; } = new ToggleNode(true);

        [Menu("Item pickup key : ")]
        public HotkeyNode PickUpKey { get; set; } = new HotkeyNode(Keys.F);

        [Menu("Phase run hotkey : ")]
        public HotkeyNode PhaseRunHotkey { get; set; } = new HotkeyNode(Keys.U);

        [Menu("Maximum Distance. 100 -> anywhere")]
        public RangeNode<int> MaxDistance { get; set; } = new RangeNode<int>(700, 10, 700);

        [Menu("Time between clicks in milliseconds")]
        public RangeNode<int> DelayClicksInMs { get; set; } = new RangeNode<int>(40, 0, 100);

        [Menu("Delay between search items to pick")]
        public RangeNode<int> DelayGetItemsToPick { get; set; } = new RangeNode<int>(200, 50, 500);

        [Menu("Activate extensive debug logging")]
        public ToggleNode DebugLogging { get; set; } = new ToggleNode(false);
    }
}
