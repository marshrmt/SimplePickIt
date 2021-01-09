using ExileCore;
using ExileCore.PoEMemory.Elements;
using ExileCore.Shared.Enums;
using ExileCore.Shared.Helpers;
using SharpDX;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SimplePickIt
{
    public class SimplePickIt : BaseSettingsPlugin<SimplePickItSettings>
    {
        private Stopwatch Timer { get; } = new Stopwatch();
        private Random Random { get; } = new Random();

        public override bool Initialise()
        {
            Timer.Start();
            return true;
        }

        public override Job Tick()
        {
            if (!Input.GetKeyState(Settings.PickUpKey.Value)) return null;
            if (!GameController.Window.IsForeground()) return null;
            if (Timer.ElapsedMilliseconds < Settings.WaitTimeInMs.Value - 10 + Random.Next(0, 20)) return null;

            Timer.Restart();

            return new Job("SimplePickIt", PickItem);
        }

        private LabelOnGround GetItemToPick(RectangleF window)
        {
            var windowSize = new RectangleF(0, 0, window.Width, window.Height);

            var closestLabel = GameController.Game.IngameState.IngameUi.ItemsOnGroundLabels
                ?.Where(label => label.Address != 0
                    && label.ItemOnGround?.Type != null
                    && label.ItemOnGround.Type == EntityType.WorldItem
                    && label.IsVisible
                    && (label.CanPickUp || label.MaxTimeForPickUp.TotalSeconds <= 0)
                    && (label.Label.GetClientRect().Center).PointInRectangle(windowSize)
                    )
                .OrderBy(label => label.ItemOnGround.DistancePlayer)
                .FirstOrDefault();

            return closestLabel;
        }

        private void PickItem()
        {
            var window = GameController.Window.GetWindowRectangle();
            var nextItem = GetItemToPick(window);
            if (nextItem == null) return;

            var centerOfLabel = nextItem?.Label?.GetClientRect().Center 
                + window.TopLeft
                + new Vector2(Random.Next(0, 2), Random.Next(0, 2));

            if (!centerOfLabel.HasValue) return;

            Input.SetCursorPos(centerOfLabel.Value);
            Input.Click(MouseButtons.Left);
        }
    }
}
