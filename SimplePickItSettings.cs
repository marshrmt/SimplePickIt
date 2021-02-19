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
        
        [Menu("Maximum Pickup Range in Unit")]
        public RangeNode<int> Range { get; set; } = new RangeNode<int>(50, 0, 100);

        [Menu("Consider Latency?")]
        public ToggleNode Latency { get; set; } = new ToggleNode(true);
    }
}
