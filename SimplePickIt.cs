using ExileCore;
using ExileCore.PoEMemory.Elements;
using ExileCore.PoEMemory.Components;
using ExileCore.Shared.Enums;
using SharpDX;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Windows.Forms;

namespace SimplePickIt
{
    public class SimplePickIt : BaseSettingsPlugin<SimplePickItSettings>
    {
        private Stopwatch Timer { get; } = new Stopwatch();
        private Random Random { get; } = new Random();
        private static bool IsRunning { get; set; } = false;

        private Vector3 startCoord;
        private bool prevKeyState = false;

        private volatile int playerInventoryItemsCount = 0;

        public override bool Initialise()
        {
            Timer.Start();
            return true;
        }

        public override Job Tick()
        {
            bool keyState = Input.GetKeyState(Settings.PickUpKey.Value);
            if (keyState && !prevKeyState)
            {
                startCoord = GameController.Player.Pos;
            }

            var _playerInventory = GameController.IngameState.ServerData.GetPlayerInventoryByType(InventoryTypeE.MainInventory);
            int _itemsCount = 0;

            if (_playerInventory != null)
            {
                foreach (var _slotItem in _playerInventory.InventorySlotItems)
                {
                    _itemsCount += _slotItem.SizeX * _slotItem.SizeY;
                }
            }

            playerInventoryItemsCount = _itemsCount;

            if (_itemsCount >= 60)
            {
                return null;
            }

            prevKeyState = keyState;

            if (!keyState) return null;
            if (!GameController.Window.IsForeground()) return null;
            if (GameController.Game.IngameState.IngameUi.InventoryPanel.IsVisible) return null;
            if (IsRunning) return null;

            Timer.Restart();

            IsRunning = true;

            return new Job("SimplePickIt", PickItem);

        }

        private List<LabelOnGround> GetItemToPick()
        {
            List<LabelOnGround> ItemToGet = new List<LabelOnGround>();

            if (GameController.Game.IngameState.IngameUi.ItemsOnGroundLabels != null)
            {
                ItemToGet = GameController.Game.IngameState.IngameUi.ItemsOnGroundLabels
                    ?.Where(label => label.Address != 0
                        && label.ItemOnGround?.Type != null
                        && label.ItemOnGround.Type == EntityType.WorldItem
                        && label.IsVisible
                        && label.CanPickUp
                        && !IsUniqueItem(label))
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

        private bool IsUniqueItem(LabelOnGround label) {
            if (label == null) {
                return false;
            }

            var itemItemOnGround = label.ItemOnGround;
            var worldItem = itemItemOnGround?.GetComponent<WorldItem>();
            var groundItem = worldItem?.ItemEntity;

            if (groundItem == null) {
                return false;
            }

            var baseItemType = GameController.Files.BaseItemTypes.Translate(groundItem.Path);

            // Still pickup unique maps
            if (baseItemType.ClassName == "Map")
            {
                return false;
            }

            if (groundItem.HasComponent<Sockets>())
            {
                var sockets = groundItem.GetComponent<Sockets>();

                // pickup 6l even if uniques
                if (sockets != null && sockets.LargestLinkSize == 6)
                {
                    return false;
                }
            }

            LogMessage(baseItemType.BaseName);

            if (groundItem.HasComponent<Mods>())
            {
                var mods = groundItem.GetComponent<Mods>();
                return mods != null && mods.ItemRarity == ItemRarity.Unique;
            }
            else {
                return false;
            }
        }

        private void PickItem()
        {
            try
            {
                if (playerInventoryItemsCount >= 60)
                {
                    IsRunning = false;
                    return;
                }

                var window = GameController.Window.GetWindowRectangle();
                Stopwatch waitingTime = new Stopwatch();
                int highlight = 0;
                int limit = 0;
                LabelOnGround nextItem = null;

                var itemList = GetItemToPick();
                if (itemList == null)
                {
                    IsRunning = false;
                    return;
                }

                do
                {
                    if (GameController.Game.IngameState.IngameUi.InventoryPanel.IsVisible)
                    {
                        IsRunning = false;
                        return;
                    }

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
                            Thread.Sleep(Random.Next(20, 25));
                            Input.KeyUp(Settings.HighlightToggle.Value);
                            Thread.Sleep(Random.Next(20, 25));
                            Input.KeyDown(Settings.HighlightToggle.Value);
                            Thread.Sleep(Random.Next(20, 25));
                            Input.KeyUp(Settings.HighlightToggle.Value);
                            Thread.Sleep(Random.Next(20, 25));
                            highlight = -1;
                        }
                        highlight++;
                    }

                    if (itemList.Count() > 1)
                    {
                        itemList = itemList.Where(label => label != null).Where(label => label.ItemOnGround != null).OrderBy(label => label.ItemOnGround.DistancePlayer).ToList();
                    }

                    nextItem = itemList[0];

                    if (Vector3.Distance(nextItem.ItemOnGround.Pos, startCoord) > 900)
                    {
                        IsRunning = false;
                        return;
                    }

                    if (nextItem.ItemOnGround.DistancePlayer > Settings.Range.Value)
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

                    Input.KeyDown(Settings.PhaseRunHotkey.Value);
                    Thread.Sleep(Random.Next(20, 25));
                    Input.KeyUp(Settings.PhaseRunHotkey.Value);

                    Input.SetCursorPos(centerOfLabel.Value);
                    Thread.Sleep(Random.Next(15, 20));
                    Input.Click(MouseButtons.Left);

                    waitingTime.Start();
                    while (nextItem.ItemOnGround.IsTargetable && waitingTime.ElapsedMilliseconds < Settings.MaxWaitTime.Value)
                    {
                        ;
                    }
                    waitingTime.Reset();

                    if (!nextItem.ItemOnGround.IsTargetable)
                    {
                        itemList.RemoveAt(0);
                    }
                } while (Input.GetKeyState(Settings.PickUpKey.Value) && itemList.Any());

                IsRunning = false;
                return;
            }
            catch
            {
                IsRunning = false;
            }
        }

        public override void Render()
        {
            Color backColor = Color.FromRgba(0x44FFFFFF);
            Color progressColor = Color.FromRgba(0x4400FF00);

            if (playerInventoryItemsCount > 48)
            {
                progressColor = Color.FromRgba(0x440000FF);
            }
            else if (playerInventoryItemsCount > 30)
            {
                progressColor = Color.FromRgba(0x4400FFFF);
            }

            var windowRect = GameController.Window.GetWindowRectangle();

            var x = 25;
            var y = windowRect.Bottom - 300;

            Graphics.DrawBox(new RectangleF(x, y, 200, 100), backColor, 3);
            Graphics.DrawBox(new RectangleF(x, y, (float) playerInventoryItemsCount / 60 * 200, 100), progressColor, 3);
        }
    }
}
