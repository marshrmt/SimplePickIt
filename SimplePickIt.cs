using ExileCore;
using ExileCore.PoEMemory.Elements;
using ExileCore.PoEMemory.Components;
using ExileCore.Shared.Enums;
using SharpDX;
using System;
using System.Diagnostics;
using System.Linq;
using System.Windows.Forms;
using System.Collections;
using ExileCore.Shared;
using ExileCore.Shared.Helpers;
using System.Threading;

namespace SimplePickIt
{
    public class SimplePickIt : BaseSettingsPlugin<SimplePickItSettings>
    {
        private Random Random { get; } = new Random();

        private LabelOnGround[] _itemsToPick = new LabelOnGround[10];
        private Stopwatch _getItemsToPickTimer = new Stopwatch();
        private volatile int playerInventoryItemsCount = 0;

        public override bool Initialise()
        {
            _getItemsToPickTimer.Start();
            return base.Initialise();
        }

        public override Job Tick()
        {
            var gameWindow = GameController.Window.GetWindowRectangle();
            var lootableGameWindow = new RectangleF(150, 150, gameWindow.Width - 150, gameWindow.Height - 150);

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

            if (!Input.GetKeyState(Settings.PickUpKey.Value)) return null;
            if (!_getItemsToPickTimer.IsRunning
                || _getItemsToPickTimer.ElapsedMilliseconds < Settings.DelayGetItemsToPick?.Value) return null;

            _itemsToPick = GetItemsToPick(lootableGameWindow, 10);
            _getItemsToPickTimer.Restart();
            return null;
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
            Graphics.DrawBox(new RectangleF(x, y, (float)playerInventoryItemsCount / 60 * 200, 100), progressColor, 3);

            if (!IsRunConditionMet()) return;

            var coroutineWorker = new Coroutine(PickItems(), this, "SimplePickIt.PickItems");
            Core.ParallelRunner.Run(coroutineWorker);
        }

        private bool IsRunConditionMet()
        {
            if (!Input.GetKeyState(Settings.PickUpKey.Value)) return false;
            if (!GameController.Window.IsForeground()) return false;

            return true;
        }

        private IEnumerator PickItems()
        {
            var gameWindow = GameController.Window.GetWindowRectangle();

            var clickTimer = new Stopwatch();
            clickTimer.Start();
            var firstRun = true;
            while (_itemsToPick.Any() && Input.GetKeyState(Settings.PickUpKey.Value))
            {
                var nextItem = _itemsToPick[0];
                var onlyMoveMouse = ((long)Settings.DelayClicksInMs > clickTimer.ElapsedMilliseconds) && !firstRun;

                yield return PickItem(nextItem, gameWindow, onlyMoveMouse);
                if (!onlyMoveMouse)
                {
                    clickTimer.Restart();
                    firstRun = false;
                }
            }
        }

        private IEnumerator PickItem(LabelOnGround itemToPick, RectangleF window, bool onlyMoveMouse)
        {
            var centerOfLabel = itemToPick?.Label?.GetClientRect().Center + window.TopLeft;

            if (!centerOfLabel.HasValue) yield break;
            if (centerOfLabel.Value.X <= 0 || centerOfLabel.Value.Y <= 0) yield break;
            if (centerOfLabel.Value.X > 10000 || centerOfLabel.Value.Y > 10000) yield break;
            if (float.IsNaN(centerOfLabel.Value.X) || float.IsNaN(centerOfLabel.Value.Y)) yield break;

            Input.KeyDown(Settings.PhaseRunHotkey.Value);
            Thread.Sleep(Random.Next(20, 25));
            Input.KeyUp(Settings.PhaseRunHotkey.Value);
            Thread.Sleep(Random.Next(20, 25));

            Input.SetCursorPos(centerOfLabel.Value);

            if (onlyMoveMouse) yield break;
            Input.Click(MouseButtons.Left);

            if (Settings.DebugLogging?.Value == true) DebugWindow.LogDebug($"SimplePickIt.PickItem -> {DateTime.Now:mm:ss.fff} clicked position x: {centerOfLabel.Value.X} y: {centerOfLabel.Value.Y}");
        }

        private LabelOnGround[] GetItemsToPick(RectangleF window, int maxAmount = 10)
        {
            var windowSize = new RectangleF(150, 150, window.Width - 150, window.Height - 150);

            var itemsToPick = GameController.Game.IngameState.IngameUi.ItemsOnGroundLabels
                ?.Where(label => label.Address != 0
                    && label.ItemOnGround?.Type != null
                    && label.ItemOnGround.Type == EntityType.WorldItem
                    && label.IsVisible
                    && (label.CanPickUp || label.MaxTimeForPickUp.TotalSeconds <= 0)
                    && label.ItemOnGround.DistancePlayer <= Settings.MaxDistance.Value
                    && (label.Label.GetClientRect().Center).PointInRectangle(windowSize)
                    && !IsUniqueItem(label)
                    )
                .OrderBy(label => label.ItemOnGround.DistancePlayer)
                .Take(maxAmount)
                .ToArray();

            return itemsToPick;
        }


        private bool IsUniqueItem(LabelOnGround label)
        {
            if (label == null)
            {
                return false;
            }

            var itemItemOnGround = label.ItemOnGround;

            if (itemItemOnGround == null || !(itemItemOnGround.HasComponent<WorldItem>()))
            {
                return false;
            }

            var worldItem = itemItemOnGround?.GetComponent<WorldItem>();
            var groundItem = worldItem?.ItemEntity;

            if (groundItem == null)
            {
                return false;
            }

            var baseItemType = GameController.Files.BaseItemTypes.Translate(groundItem.Path);

            // Dont pickup non currency if inventory is full
            if (baseItemType?.ClassName != null && baseItemType.ClassName != "StackableCurrency" && playerInventoryItemsCount >= 60)
            {
                return true;
            }

            if (baseItemType?.ClassName != null && baseItemType.ClassName != "StackableCurrency")
            {
                int itemSize = baseItemType.Width * baseItemType.Height;

                // dont pickup big items if we 100% know we cant
                if (playerInventoryItemsCount + itemSize >= 60)
                {
                    return true;
                }
            }

            // Still pickup unique maps
            if (baseItemType?.ClassName != null && baseItemType.ClassName == "Map")
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

            string[] bestUniques = {
                        //t1
                        "Blood Raiment", "Crusader Boots", "Crusader Helmet", "Ezomyte Tower Shield", "Fluted Bascinet",
                        "Greatwolf Talisman", "Jewelled Foil", "Jingling Spirit Shield", "Large Cluster Jewel", "Occultist's Vestment",
                        "Ornate Quiver", "Prismatic Jewel", "Prophecy Wand", "Rawhide Boots", "Ruby Flask", "Sapphire Flask", "Siege Axe",
                        "Silk Gloves", "Timeless Jewel", "Vaal Rapier",
                        //t2
                        "Zodiac Leather", "Granite Flask", "Ezomyte Dagger"
                    };


            if (groundItem.HasComponent<Mods>())
            {
                var mods = groundItem.GetComponent<Mods>();

                if (mods != null && mods.ItemRarity == ItemRarity.Unique)
                {
                    if (baseItemType?.BaseName != null && baseItemType.BaseName != "")
                    {
                        return !(bestUniques.Contains(baseItemType.BaseName));
                    }
                    else
                    {
                        return true;
                    }
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
        }
    } 
}
