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
            if (GameController.Game.IngameState.IngameUi.InventoryPanel.IsVisible) return null;
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

            var window = GameController.Window.GetWindowRectangle();
            // For the "waiting" loop
            Stopwatch waitingTime = new Stopwatch();
            // Use item highlight to reset item label position.
            int highlight = 0;
            int limit = 0;
            // List of item to pick.
            var itemList = GetItemToPick(window);
            if (itemList == null)
            {
                IsRunning = false;
                return;
            }

            // Loop until the key is released or the list of item to pick get emptied out.
            do
            {
                // If the inventory is open, stop picking item.
                if (GameController.Game.IngameState.IngameUi.InventoryPanel.IsVisible)
                {
                    IsRunning = false;
                    return;
                }

                // Refresh item position via the Highlight button every 3-6 loop.
                if (Settings.MinLoop.Value != 0)
                {
                    if (Settings.MaxLoop.Value < Settings.MinLoop.Value)
                    {
                        int temp = Settings.MaxLoop.Value;
                        Settings.MaxLoop.Value = Settings.MinLoop.Value;
                        Settings.MinLoop.Value = temp;
                    }
                    if (highlight == 0)
                    {
                        limit = Random.Next(Settings.MinLoop.Value, Settings.MaxLoop.Value + 1);
                    }
                    if (highlight == limit - 1)
                    {
                        Input.KeyDown(Settings.HighlightToggle.Value);
                        Thread.Sleep(Random.Next(10, 20));
                        Input.KeyUp(Settings.HighlightToggle.Value);
                        Thread.Sleep(Random.Next(10, 20));
                        Input.KeyDown(Settings.HighlightToggle.Value);
                        Thread.Sleep(Random.Next(10, 20));
                        Input.KeyUp(Settings.HighlightToggle.Value);
                        highlight = -1;
                    }
                    highlight++;
                }

                // Set the list in order of item closest to the player.
                if (itemList.Count() > 1)
                {
                    itemList = itemList.OrderBy(label => label.ItemOnGround.DistancePlayer).ToList();
                }

                // Current item to pick.
                var nextItem = itemList[0];
                
                // If the current item to pick is further than X unit of distance, stop.
                if (nextItem.ItemOnGround.DistancePlayer > Settings.Range.Value)
                {
                    IsRunning = false;
                    return;
                }

                // Item label position on the screen.
                var centerOfLabel = nextItem?.Label?.GetClientRect().Center
                    + window.TopLeft
                    + new Vector2(Random.Next(0, 2), Random.Next(0, 2));
                if (!centerOfLabel.HasValue)
                {
                    IsRunning = false;
                    return;
                }

                // Attempt to pick the item
                Input.SetCursorPos(centerOfLabel.Value);
                Thread.Sleep(Random.Next(15, 20));
                Input.Click(MouseButtons.Left);

                waitingTime.Start();
                while (nextItem.ItemOnGround.IsTargetable && nextItem.IsVisible && waitingTime.ElapsedMilliseconds < Settings.MaxWaitTime.Value)
                {
                    ; // Waiting loop
                }
                waitingTime.Reset();

                // Remove the item picked from the list.
                itemList.RemoveAt(0);
            } while (Input.GetKeyState(Settings.PickUpKey.Value) && itemList.Any());

            IsRunning = false;
            return;
        }
    }
}
