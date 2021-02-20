using ExileCore.Shared.Interfaces;
using ExileCore.Shared.Nodes;
using ExileCore.Shared.Attributes;
using System.Windows.Forms;

namespace SimplePickIt
{
    public class SimplePickItSettings : ISettings
    {
        public ToggleNode Enable { get; set; } = new ToggleNode(true);

        [Menu("MAKE SURE YOUR HOTKEY IN-GAME MATCH WITH THOSE IN HERE")]
        public ButtonNode Text { get; set; } = new ButtonNode();

        [Menu("Item pickup key : ")]
        public HotkeyNode PickUpKey { get; set; } = new HotkeyNode(Keys.F);

        [Menu("Current Movement Speed %")]
        public RangeNode<int> MovementSpeed { get; set; } = new RangeNode<int>(0, 0, 300);
        
        [Menu("Maximum Pickup Range in Unit")]
        public RangeNode<int> Range { get; set; } = new RangeNode<int>(50, 0, 100);

        [Menu("Toggle highlighting : ")]
        public HotkeyNode HighlightToggle { get; set; } = new HotkeyNode(Keys.Z);

        [Menu("Number of attempt before refreshing label :")]
        public ButtonNode Text2 { get; set; } = new ButtonNode();

        [Menu("Minimum (0 = off)")]
        public RangeNode<int> MinLoop { get; set; } = new RangeNode<int>(5, 0, 10);

        [Menu("Maximum")]
        public RangeNode<int> MaxLoop { get; set; } = new RangeNode<int>(10, 1, 10);

        [Menu("Consider Latency?")]
        public ToggleNode Latency { get; set; } = new ToggleNode(true);
    }
}
