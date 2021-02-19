using ExileCore.Shared.Interfaces;
using ExileCore.Shared.Nodes;
using ExileCore.Shared.Attributes;
using System.Windows.Forms;

namespace SimplePickIt
{
    public class SimplePickItSettings : ISettings
    {
        public ToggleNode Enable { get; set; } = new ToggleNode(true);

        [Menu("PickUp Hotkey")]
        public HotkeyNode PickUpKey { get; set; } = new HotkeyNode(Keys.F1);

        [Menu("Current Movement Speed %")]
        public RangeNode<int> MovementSpeed { get; set; } = new RangeNode<int>(0, 0, 300);
    }
}
