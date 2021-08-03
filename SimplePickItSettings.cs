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

        [Menu("Toggle highlighting : ")]
        public HotkeyNode HighlightToggle { get; set; } = new HotkeyNode(Keys.Z);

        [Menu("Maximum Pickup Range in Unit")]
        public RangeNode<int> Range { get; set; } = new RangeNode<int>(50, 0, 100);

        [Menu("Maximum Wait Time per click")]
        public RangeNode<int> MaxWaitTime { get; set; } = new RangeNode<int>(1000, 200, 2000);

        [Menu("Number of attempt before refreshing label :")]
        public ButtonNode Text2 { get; set; } = new ButtonNode();

        [Menu("Minimum (0 = off)")]
        public RangeNode<int> MinLoop { get; set; } = new RangeNode<int>(5, 0, 10);

        [Menu("Maximum")]
        public RangeNode<int> MaxLoop { get; set; } = new RangeNode<int>(10, 1, 10);

        [Menu("Phase run hotkey : ")]
        public HotkeyNode PhaseRunHotkey { get; set; } = new HotkeyNode(Keys.U);

    }
}
