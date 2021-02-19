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

            // Static movement speed (might be off), would be nice to grab the current MS % of the player dynamically.
            float currentSpeed = 36 * (1 + ((float)Settings.MovementSpeed.Value / 100));
            var window = GameController.Window.GetWindowRectangle();
            // Use ServerRequestCounter as a way to know if the item was picked.
            var playerInventory = GameController.Game.IngameState.ServerData.PlayerInventories[0].Inventory;
            int invState = playerInventory.ServerRequestCounter;
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
                // Set the list in order of item closest to the player after a new item is picked.
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
                // Calculate the amount of time required to reach and pick the item.
                int waitTime = (int)((nextItem.ItemOnGround.DistancePlayer / currentSpeed) * 1000);
                // We can add the latency if required
                if(Settings.Latency.Value)
                {
                    waitTime += (int)GameController.Game.IngameState.CurLatency;
                }
                // Attempt to pick the item
                Input.SetCursorPos(centerOfLabel.Value);
                Thread.Sleep(Random.Next(10, 20));
                Input.Click(MouseButtons.Left);
                Thread.Sleep(waitTime);
                // If the ServerRequestCounter goes up, the item was probably picked.
                if (playerInventory.ServerRequestCounter != invState)
                {
                    // Remove the item from the list of item we have to pick.
                    itemList.RemoveAt(0);
                }
                // Update the counter with the new value.
                invState = playerInventory.ServerRequestCounter;
                
            } while (Input.GetKeyState(Settings.PickUpKey.Value) && itemList.Any());

            IsRunning = false;
            return;
        }
    }
}
