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
        private static bool IsRunning { get; set; } = false;

        public override bool Initialise()
        {
            Timer.Start();
            return true;
        }

        public override Job Tick()
        {
            if (!Input.GetKeyState(Settings.PickUpKey.Value)) return null;
            if (!GameController.Window.IsForeground()) return null;
            if (IsRunning) return null;

            Timer.Restart();

            return new Job("SimplePickIt", PickItem);
        }

        private List<LabelOnGround> GetItemToPick(RectangleF window)
        {
            var windowSize = new RectangleF(0, 0, window.Width, window.Height);

            List<LabelOnGround> ItemToGet = new List<LabelOnGround>();

            if(GameController.Game.IngameState.IngameUi.ItemsOnGroundLabels != null)
            {
                ItemToGet = GameController.Game.IngameState.IngameUi.ItemsOnGroundLabels
                    ?.Where(label => label.Address != 0
                        && label.ItemOnGround?.Type != null
                        && label.ItemOnGround.Type == EntityType.WorldItem
                        && label.IsVisible
                        && (label.CanPickUp || label.MaxTimeForPickUp.TotalSeconds <= 0)
                        && (label.Label.GetClientRect().Center).PointInRectangle(windowSize)
                        )
                    .OrderBy(label => label.ItemOnGround.DistancePlayer)
                    .ToList();
            }

            if (ItemToGet.Any())
            {
                return ItemToGet;
            }
            else
            {
                return null;
            }
        }

        private void PickItem()
        {
            IsRunning = true;

            float currentSpeed = 36 * (1 + ((float)Settings.MovementSpeed.Value / 100));
            var window = GameController.Window.GetWindowRectangle();

            var itemList = GetItemToPick(window);
            if (itemList == null)
            {
                IsRunning = false;
                return;
            }

            do
            {
                itemList = itemList.OrderBy(label => label.ItemOnGround.DistancePlayer).ToList();

                var nextItem = itemList[0];

                if (nextItem.ItemOnGround.DistancePlayer >= 50)
                {
                    IsRunning = false;
                    return;
                }

                var centerOfLabel = nextItem?.Label?.GetClientRect().Center
                    + window.TopLeft
                    + new Vector2(Random.Next(0, 2), Random.Next(0, 2));

                if (!centerOfLabel.HasValue)
                {
                    IsRunning = false;
                    return;
                }

                int waitTime = (int)((nextItem.ItemOnGround.DistancePlayer / currentSpeed) * 1000 + GameController.IngameState.CurLatency);

                Input.SetCursorPos(centerOfLabel.Value);
                Thread.Sleep(Random.Next(1, 3));
                Input.Click(MouseButtons.Left);
                Thread.Sleep(waitTime);

                itemList.RemoveAt(0);

            } while (Input.GetKeyState(Settings.PickUpKey.Value) && itemList.Any());

            IsRunning = false;
            return;
        }
    }
}
