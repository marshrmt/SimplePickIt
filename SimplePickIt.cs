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
using System.Collections.Generic;

namespace SimplePickIt
{
    public class SimplePickIt : BaseSettingsPlugin<SimplePickItSettings>
    {
        private Random Random { get; } = new Random();

        private volatile int playerInventoryItemsCount = 0;

        public override Job Tick()
        {
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
            //if (GameController.Game.IngameState.IngameUi.InventoryPanel.IsVisible) return false;

            return true;
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
                        //&& label.IsVisible
                        //&& label.CanPickUp
                        //&& !IsUniqueItem(label)
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

        private IEnumerator PickItems()
        {
            var window = GameController.Window.GetWindowRectangle();
            Stopwatch waitingTime = new Stopwatch();
            int highlight = 0;
            int limit = 0;
            LabelOnGround nextItem = null;

            var itemList = GetItemToPick();
            if (itemList == null)
            {
                yield break;
            }

            do
            {
                if (GameController.Game.IngameState.IngameUi.InventoryPanel.IsVisible)
                {
                    yield break;
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

                if (nextItem.ItemOnGround.DistancePlayer > Settings.Range.Value)
                {
                    yield break;
                }

                var centerOfLabel = nextItem?.Label?.GetClientRect().Center
                    + window.TopLeft
                    + new Vector2(Random.Next(0, 2), Random.Next(0, 2));
                if (!centerOfLabel.HasValue)
                {
                    yield break;
                }

                Input.KeyDown(Settings.PhaseRunHotkey.Value);
                Thread.Sleep(Random.Next(20, 25));
                Input.KeyUp(Settings.PhaseRunHotkey.Value);
                Thread.Sleep(Random.Next(20, 25));

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

            yield break;
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
            /*if (baseItemType?.ClassName != null && baseItemType.ClassName != "StackableCurrency" && playerInventoryItemsCount >= 60)
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
            }*/

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
