using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

using FlatRedBall;
using FlatRedBall.Input;
using FlatRedBall.Instructions;
using FlatRedBall.AI.Pathfinding;
using FlatRedBall.Graphics.Animation;
using FlatRedBall.Graphics.Particle;
using FlatRedBall.Math.Geometry;
using FlatRedBall.Localization;
using FlatRedBall.Scripting;
using Microsoft.Xna.Framework;
using FishStory.Entities;
using FlatRedBall.TileEntities;
using FishStory.Managers;
using FlatRedBall.Gui;
using FishStory.DataTypes;
using FishStory.GumRuntimes;
using static DialogTreePlugin.SaveClasses.DialogTreeRaw;
using DialogTreePlugin.SaveClasses;

namespace FishStory.Screens
{
    public partial class GameScreen
    {
        #region Structs

        struct FishWeight
        {
            public string Fish;
            public float Weight;

            public override string ToString()
            {
                return $"{Fish} {Weight}";
            }
        }

        #endregion

        #region Fields/Properties

        protected ScreenScript<GameScreen> script;

        List<string> dialogTagsThisFrame = new List<string>();

        Dictionary<string, List<string>> ItemsBought = new Dictionary<string, List<string>>();

        #endregion

        #region Initialize

        void CustomInitialize()
        {
            script = new ScreenScript<GameScreen>(this);

            InitializeEntitiesFromMap();

            DialogBox.Visible = false;

            InitializePlayer();

            InitializeCamera();

            SpriteManager.OrderedSortType = FlatRedBall.Graphics.SortType.ZSecondaryParentY;

            InitializeCollision();

            InitializeUi();

            InitializeRestartVariables();

#if DEBUG
            DebugInitialize();
#endif
        }

        private void InitializePlayer()
        {
            PlayerCharacterInstance.FishLost += HandleFishLost;
        }

#if DEBUG
        private void DebugInitialize()
        {
            if (DebuggingVariables.ShowFishLootZones)
            {
                foreach(var zone in this.FishingZoneList)
                {
                    zone.Collision.Visible = true;
                }
            }


            void Award(string item) => PlayerDataManager.PlayerData.AwardItem(item);
            if (DebuggingVariables.AwardTonsOfBait)
            {
                Award(DataTypes.ItemDefinition.Sardine);
                Award(DataTypes.ItemDefinition.Sardine);
                Award(DataTypes.ItemDefinition.Sardine);

                Award(DataTypes.ItemDefinition.Blood_Worm);
                Award(DataTypes.ItemDefinition.Blood_Worm);
                Award(DataTypes.ItemDefinition.Blood_Worm);

                Award(DataTypes.ItemDefinition.Squid);
                Award(DataTypes.ItemDefinition.Squid);
                Award(DataTypes.ItemDefinition.Little_Bonito);
                Award(DataTypes.ItemDefinition.Little_Bonito);
            }

            if(DebuggingVariables.AwardUnidentifiedFish)
            {
                foreach(var item in GlobalContent.ItemDefinition.Where(item => !string.IsNullOrEmpty(item.Value.AssociatedItem)))
                {
                    Award(item.Key);
                }
            }

            if (DebuggingVariables.AwardIdentifiedFish)
            {
                foreach (var item in GlobalContent.ItemDefinition.Where(item => item.Value.IsFish))
                {
                    Award(item.Key);
                }
            }

            var testBlackMarketDialog = GetRootObject("Sell At Black Market Dialog",
                new List<string>
                {
                    "KabukiQuantum|Sell"
                });

            testBlackMarketDialog.passages.Add(new Passage
            {
                pid = "1",
                text = "sell=blackmarket"
            });



            var testFishMonger = GetRootObject("Fishmonger dialog",
                new List<string>
                {
                    "Sell",
                    "Cancel"
                });

            testFishMonger.passages.Add(new Passage
            {
                pid = "1",
                text = "sell=fishmonger"
            });

            var testIdentifier = GetRootObject("Identifier dialog",
                new List<string>
                {
                    "Identify",
                    "Cancel"
                });

            testIdentifier.passages.Add(new Passage
            {
                pid = "1",
                text = "id="
            });


            var testStoreNpc = NPCList.First(item => item.Name == "TestBlackMarket");
            testStoreNpc.DirectlySetDialog = testBlackMarketDialog;


            var testFishmongerNpc = NPCList.First(item => item.Name == "TestFishmonger");
            testFishmongerNpc.DirectlySetDialog = testFishMonger;


            var testIdentifierNpc = NPCList.First(item => item.Name == "TestIdentifier");
            testIdentifierNpc.DirectlySetDialog = testIdentifier;
        }
#endif

        private void InitializeEntitiesFromMap()
        {
            TileEntityInstantiator.CreateEntitiesFrom(Map);

            foreach(var npc in NPCList)
            {
                npc.Z = 0; // same as player so they sort
            }
        }

        private void InitializeCamera()
        {
            Camera.Main.X = PlayerCharacterInstance.X;
            Camera.Main.Y = PlayerCharacterInstance.Y;
        }

        private void InitializeCollision()
        {
            PlayerCharacterInstanceActivityCollisionVsNPCListBodyCollision.CollisionOccurred +=
                HandlePlayerVsNpcActivityCollision;
        }

        private void InitializeUi()
        {
            #region Dialog Box

            if (PlayerCharacterInstance.InputDevice is Keyboard keyboard)
            {
                DialogBox.UpInput = keyboard.GetKey(Microsoft.Xna.Framework.Input.Keys.Up)
                    .Or(keyboard.GetKey(Microsoft.Xna.Framework.Input.Keys.W));

                DialogBox.DownInput = keyboard.GetKey(Microsoft.Xna.Framework.Input.Keys.Down)
                    .Or(keyboard.GetKey(Microsoft.Xna.Framework.Input.Keys.S));
            }
            else
            {
                throw new NotImplementedException();
            }

            DialogBox.SelectInput = PlayerCharacterInstance.TalkInput;

            DialogBox.AfterHide += HandleDialogBoxHide;
            DialogBox.StoreShouldShow += HandleStoreShouldShow;
            DialogBox.SellingShouldShow += HandleSellingShouldShow;
            DialogBox.IdentifyPerformed += HandleIdentify;
            DialogBox.DialogTagShown += HandleDialogTagShown;

            #endregion

            #region Store
            GameScreenGum.StoreInstance.CancelInput = PlayerCharacterInstance.CancelInput;
            GameScreenGum.StoreInstance.Visible = false;
            GameScreenGum.StoreInstance.BuyButtonClick += HandleBuyClicked;
            #endregion

            #region Inventory

            var inventory = GameScreenGum.InventoryInstance;
            inventory.Visible = false;
            inventory.SellClicked += HandleSellClicked;
            inventory.CancelInput = PlayerCharacterInstance.CancelInput;
            inventory.InventoryInput = PlayerCharacterInstance.InventoryInput;
            #endregion

            GameScreenGum.NotificationBoxInstance.UpdateVisibility();
        }

        private void HandlePlayerVsNpcActivityCollision(PlayerCharacter player, NPC npc)
        {
            player.NpcForAction = npc;
        }

        private void InitializeRestartVariables()
        {
            RestartVariables.Add(
                $"this.{nameof(PlayerCharacterInstance)}.{nameof(PlayerCharacterInstance.X)}");
            RestartVariables.Add(
                $"this.{nameof(PlayerCharacterInstance)}.{nameof(PlayerCharacterInstance.Y)}");
        }

        #endregion

        #region Activity

        void CustomActivity(bool firstTimeCalled)
        {
            dialogTagsThisFrame.Clear();
            if(InputManager.Mouse.ButtonPushed(Mouse.MouseButtons.RightButton))
            {
                RestartScreen(true);
            }
            CameraActivity();
            
            UiActivity();

            PlayerCollisionActivity();

            DebuggingActivity();

            // do script *after* the UI
            script.Activity();
            Map?.AnimateSelf();
        }

        private void DebuggingActivity()
        {
            var keyboard = InputManager.Keyboard;

            if(DebuggingVariables.AwardMoneyByPressingM && keyboard.KeyPushed(Microsoft.Xna.Framework.Input.Keys.M))
            {
                AwardMoney(50);
            }
            if(DebuggingVariables.SkipDayWithCtrlD && keyboard.IsCtrlDown && 
                keyboard.KeyPushed(Microsoft.Xna.Framework.Input.Keys.D))
            {
                GoToNewDay();
            }
        }

        void CameraActivity()
        {
            var difference = (PlayerCharacterInstance.Position - Camera.Main.Position).ToVector2();
            Camera.Main.Velocity = difference.ToVector3();

        }

        private void PlayerCollisionActivity()
        {
            if (PlayerCharacterInstance.TalkInput.WasJustPressed && PlayerCharacterInstance.InputEnabled)
            {
                PlayerCharacterInstance.NpcForAction = null;

                PlayerCharacterInstanceActivityCollisionVsNPCListBodyCollision.DoCollisions();

                if (PlayerCharacterInstance.NpcForAction != null)
                {
                    var npc = PlayerCharacterInstance.NpcForAction;
                    if(npc.DirectlySetDialog != null)
                    {
                        if (DialogBox.TryShow(npc.DirectlySetDialog))
                        {
                            PlayerCharacterInstance.ObjectsBlockingInput.Add(DialogBox);
                        }

                    }
                    else
                    {
                        if (DialogBox.TryShow(npc.TwineDialogId))
                        {
                            PlayerCharacterInstance.ObjectsBlockingInput.Add(DialogBox);
                        }
                    }
                }

                if(PlayerCharacterInstance.ObjectsBlockingInput.Count == 0 &&
                    PlayerCharacterInstance.NpcForAction == null &&
                    PlayerCharacterInstanceActivityCollisionVsPlayerHouseDoorList.DoCollisions())
                {
                    string text = "Call it a day?";
                    List<string> options = new List<string>()
                    {
                        "Yes",
                        "No"
                    };

                    var rootObject = GetRootObject(text, options);
                    DialogBox.TryShow(rootObject, HandleDoorOptionSelected);
                    PlayerCharacterInstance.ObjectsBlockingInput.Add(DialogBox);
                }

                if(PlayerCharacterInstance.ObjectsBlockingInput.Count == 0 && 
                    PlayerCharacterInstanceActivityCollisionVsFishIdentifiedSignList.DoCollisions())
                {
                    string text;
                    var identifiedDictionary = PlayerDataManager.PlayerData.TimesFishIdentified;
                    var hasIdentifiedAny = identifiedDictionary.Values.Any(item => item > 0);
                    if(!hasIdentifiedAny)
                    {
                        text = "No fish identified";
                    }
                    else
                    {
                        text = "Identified fish:";
                        foreach(var kvp in identifiedDictionary)
                        {
                            text += $"\n{kvp.Key} {kvp.Value}";
                        }
                    }
                    var rootObject = GetRootObject(text, new List<string>());
                    DialogBox.TryShow(rootObject);
                    PlayerCharacterInstance.ObjectsBlockingInput.Add(DialogBox);
                }
            }


            DoFishingActivity();
        }

        private void HandleDoorOptionSelected(DialogTreeRaw.Link selectedLink)
        {
            DialogBox.TryHide();
            PlayerCharacterInstance.ObjectsBlockingInput.Remove(DialogBox);
            if(selectedLink.name == "Yes")
            {
                GoToNewDay();
            }

        }

        private void DoFishingActivity()
        {
            if (PlayerCharacterInstance.IsFishing == false && PlayerCharacterInstance.TalkInput.WasJustPressed &&
                PlayerCharacterInstanceFishingCollisionVsWaterCollision.DoCollisions())
            {
                var baitSelection = GetBaitRootObject();

                if (baitSelection == null)
                {
                    AddNotification("Can't fish - no bait");
                }
                else
                {
                    if (DialogBox.TryShow(baitSelection, HandleFishingLinkSelected))
                    {
                        PlayerCharacterInstance.ObjectsBlockingInput.Add(DialogBox);
                    }
                }
            }
            else if (PlayerCharacterInstance.IsFishing &&
                PlayerCharacterInstance.TalkInput.WasJustPressed &&
                PlayerCharacterInstance.LastTimeFishingStarted != TimeManager.CurrentScreenTime)
            {
                if (PlayerCharacterInstance.IsFishOnLine)
                {
                    string fishCaught = GetFishCaught(PlayerCharacterInstance.CurrentBait);
                    PlayerDataManager.PlayerData.AwardItem(fishCaught);
                    AddNotification($"Caught {fishCaught}");
                }
                PlayerCharacterInstance.StopFishing();
            }
        }

        private string GetFishCaught(string baitType)
        {
            var lootTable = GlobalContent.DefaultLootTable;

            // see if the player is colliding with any fishing zones
            var collidedFishingZone = this.FishingZoneList.First(item => item.CollideAgainst(PlayerCharacterInstance.BodyCollision));
            if(collidedFishingZone != null)
            {
                if(string.IsNullOrEmpty(collidedFishingZone.LootTable))
                {
                    throw new Exception("The current zone does not have a specified LootTable, it should");
                }
                lootTable = (Dictionary<string, FishingLootTable>)GlobalContent.GetFile(collidedFishingZone.LootTable);

                if(lootTable == null)
                {
                    throw new Exception($"The loot table {collidedFishingZone.LootTable} either doesn't exist or it doesn't use FishingLootTable created class");
                }
            }

            var field = typeof(FishingLootTable).GetField(baitType);

            if(field == null)
            {
                throw new InvalidOperationException($"Trying to find a row for bait {baitType} but couldn't find it");
            }

            List<FishWeight> fishWeights = new List<FishWeight>();

            foreach(var kvp in lootTable)
            {
                var weightAsObject = field.GetValue(kvp.Value);
                int weight = 0;
                if (weightAsObject != null)
                {
                    weight = (int)weightAsObject;
                }

                if(weight != 0)
                {
                    var fishWeight = new FishWeight
                    {
                        Fish = kvp.Key,
                        Weight = weight
                    };
                    fishWeights.Add(fishWeight);
                }
            }

            var sum = fishWeights.Sum(item => item.Weight);

            var randomValue = FlatRedBallServices.Random.Between(0, sum);

            var sumSoFar = 0.0f;

            foreach(var item in fishWeights)
            {
                sumSoFar += item.Weight;

                if(randomValue < sumSoFar)
                {
                    return item.Fish;
                }
            }

            return fishWeights.Last().Fish;
        }

        private void HandleFishingLinkSelected(DialogTreeRaw.Link selectedLink)
        {
            var text = selectedLink.name;

            PlayerCharacterInstance.ObjectsBlockingInput.Remove(DialogBox);
            DialogBox.TryHide();
            if(text == "Cancel")
            {
                // do nothing
            }
            else
            {
                var baitType = text.Substring(0, text.LastIndexOf(" ("));

                PlayerDataManager.PlayerData.RemoveItem(baitType);
                AddNotification($"Used {baitType}");

                PlayerCharacterInstance.StartFishing(baitType);
            }
        }

        private RootObject GetBaitRootObject()
        {
            var inventory = PlayerDataManager.PlayerData.ItemInventory;

            var baitItems = inventory
                .Where(item => item.Value > 0)
                .Where(item => GlobalContent.ItemDefinition[item.Key].IsBait)
                .ToList();
            if(baitItems.Count == 0)
            {
                return null;
            }
            else
            {
                string text = "Select Bait";
                List<string> options = new List<string>();

                foreach (var item in baitItems)
                {
                    options.Add($"{item.Key} ({item.Value})");
                }
                options.Add("Cancel");

                return GetRootObject(text, options);
            }
        }

        private RootObject GetRootObject(string text, List<string> options)
        {

            var rootObject = new RootObject();
            rootObject.startnode = "start";

            List<Passage> passages = new List<Passage>();

            var mainPassage = new Passage();
            mainPassage.name = "start";
            mainPassage.pid = "start";
            mainPassage.text = text;


            var links = new List<DialogTreePlugin.SaveClasses.DialogTreeRaw.Link>();

            int id = 1;
            foreach (var option in options)
            {
                var link = new DialogTreePlugin.SaveClasses.DialogTreeRaw.Link();
                link.name = option;
                link.pid = id.ToString();
                links.Add(link);
                id++;
            }

            mainPassage.links = links.ToArray();

            passages.Add(mainPassage);

            rootObject.passages = passages;

            return rootObject;
        }

        private void HandleFishLost()
        {
            AddNotification("Fish got away with bait");
        }

        public void GoToNewDay()
        {
            // stop player from moving
            PlayerCharacterInstance.ObjectsBlockingInput.Add(GameScreenGum.OverlayInstance);

            // fade UI out
            GameScreenGum.ToBlackAnimation.Play();

            // reset purchases
            this.ItemsBought.Clear();

            // fade UI in
            GameScreenGum.ToTransparentAnimation.PlayAfter(GameScreenGum.ToBlackAnimation.Length);

            // allow player movement
            this.Call(() =>
            {
                PlayerCharacterInstance.ObjectsBlockingInput.Remove(GameScreenGum.OverlayInstance);

                PlayerDataManager.PlayerData.CurrentDay++;

                AddNotification($"Fishing Festival: Day {PlayerDataManager.PlayerData.CurrentDay}");

            }).After(GameScreenGum.ToBlackAnimation.Length + GameScreenGum.ToTransparentAnimation.Length);
        }

        #region UI Activity

        private void UiActivity()
        {
            //if(InputManager.Keyboard.KeyPushed(Microsoft.Xna.Framework.Input.Keys.Space))
            //{
            //    GameScreenGum.NotificationBoxInstance.AddNotification($"You pushed space at {DateTime.Now.ToShortTimeString()}");
            //}

            DialogBox.CustomActivity();

            GameScreenGum.StoreInstance.CustomActivity();

            GameScreenGum.NotificationBoxInstance.CustomActivity();

            InventoryUiActivity();
        }

        protected void AddNotification(string notification) =>
            GameScreenGum.NotificationBoxInstance.AddNotification(notification);

        private void HandleStoreShouldShow(string storeName)
        {
            var store = GameScreenGum.StoreInstance;
            store.Visible = true;
            store.PlayerMoneyText = "$" + PlayerDataManager.PlayerData.Money.ToString();

            store.PopulateFromStoreName(storeName, ItemsBought);
        }

        private void HandleSellingShouldShow(string sellerName)
        {
            sellerName = sellerName?.ToLowerInvariant();
            var isBlackMarket = sellerName == "blackmarket";
            var isFishStore = sellerName == "fishmonger";

            var sellPriceMultiplier = isBlackMarket ? 
                NPC.BlackMarketSellPriceMultiplier : 1.0f;
            var inventoryRestrictions = isFishStore ?
                InventoryRestrictions.IdentifiedFishOnly :
                InventoryRestrictions.NoRestrictions;


            ShowInventory(InventoryRuntime.ViewOrSell.Sell, 
                sellPriceMultiplier, inventoryRestrictions);
        }
        //todo - need to actually call this if text is "id="
        private void HandleIdentify()
        {
            var itemDefinitions = GlobalContent.ItemDefinition;

            bool IsUnidentified(string itemName)
            {
                return !string.IsNullOrEmpty(itemDefinitions[itemName].AssociatedItem);
            }

            var toConvert = PlayerDataManager.PlayerData.ItemInventory
                .Where(item => IsUnidentified(item.Key))
                .ToArray();

            Dictionary<string, int> newItemCounts = new Dictionary<string, int>();

            foreach(var item in toConvert)
            {
                var itemName = item.Key;

                var itemDefinition = GlobalContent.ItemDefinition[itemName];

                var newItemName = itemDefinition.AssociatedItem;

                for(int i = 0; i < item.Value; i++)
                {
                    PlayerDataManager.PlayerData.AwardItem(newItemName);
                    PlayerDataManager.PlayerData.RemoveItem(itemName);
                }

                newItemCounts[newItemName] = item.Value;

                PlayerDataManager.PlayerData.TimesFishIdentified.IncrementBy(newItemName, item.Value);
            }


            foreach(var newItemKvp in newItemCounts)
            {
                AddNotification($"Identified {newItemKvp.Key} ({newItemKvp.Value})");
            }

        }

        private void HandleSellClicked()
        {
            var inventory = GameScreenGum.InventoryInstance;
            var selectedItemName = inventory.SelectedItemName;

            if(!string.IsNullOrEmpty(selectedItemName))
            {
                var item = GlobalContent.ItemDefinition[selectedItemName];

                if(item.PlayerSellingCost <= 0)
                {
                    AddNotification("Item cannot be sold");
                }
                else
                {
                    // remove the item from inventory
                    PlayerDataManager.PlayerData.RemoveItem(selectedItemName);

                    // award money
                    PlayerDataManager.PlayerData.Money += 
                        (int)(item.PlayerSellingCost * inventory.LastSellPriceMultiplier);

                    // refresh the UI
                    inventory.FillWithInventory(
                        PlayerDataManager.PlayerData.ItemInventory,
                        inventory.LastSellPriceMultiplier,
                        inventory.InventoryRestrictions

                        );
                    inventory.PlayerMoneyText = "$" + PlayerDataManager.PlayerData.Money.ToString();

                    inventory.SelectedItemName = selectedItemName;

                    // Show the notification
                    AddNotification($"Sold {selectedItemName}");
                }
            }
        }

        private void HandleDialogBoxHide()
        {
            if(PlayerCharacterInstance.ObjectsBlockingInput.Contains(DialogBox))
            {
                PlayerCharacterInstance.ObjectsBlockingInput.Remove(DialogBox);
            }
        }

        private void HandleDialogTagShown(string tag)
        {
            dialogTagsThisFrame.Add(tag);
        }

        private void HandleBuyClicked(IWindow window)
        {
            var store = GameScreenGum.StoreInstance;
            if (store.SelectedShopItem == null)
            {
                AddNotification("Select item first, then click buy");
            }
            else if(store.SelectedShopItem.Stock == 0)
            {
                AddNotification("This item is sold out");
            }
            else
            {
                var itemToBuy = 
                    GlobalContent.ItemDefinition[store.SelectedShopItem.Item];

                if(itemToBuy.PlayerBuyingCost > PlayerDataManager.PlayerData.Money)
                {
                    AddNotification("You do not have enough money");
                }
                else
                {
                    store.ItemsBoughtFromThisStore.Add(itemToBuy.Name);

                    store.RefreshStoreItems();

                    BuyItem(itemToBuy);
                }
            }
            //if(PlayerDataManager.PlayerData.Money >= itemToBuy.)
        }

        private void BuyItem(ItemDefinition itemToBuy)
        {
            AddNotification($"Bought {itemToBuy.Name}");
            PlayerDataManager.PlayerData.Money -= itemToBuy.PlayerBuyingCost;
            PlayerDataManager.PlayerData.AwardItem(itemToBuy.Name);

            GameScreenGum.StoreInstance.PlayerMoneyText = $"${PlayerDataManager.PlayerData.Money}";
        }

        private void InventoryUiActivity()
        {
            var inventory = GameScreenGum.InventoryInstance;
            if(inventory.Visible)
            {
                inventory.CustomActivity();
            }
            else if (inventory.Visible == false && PlayerCharacterInstance.InventoryInput.WasJustPressed)
            {
                ShowInventory(InventoryRuntime.ViewOrSell.View, 1.0f, InventoryRestrictions.NoRestrictions);
            }
        }

        private static void ShowInventory(InventoryRuntime.ViewOrSell state, 
            float sellPriceMultiplier, InventoryRestrictions inventoryRestrictions)
        {
            var inventory = GameScreenGum.InventoryInstance;

            inventory.Visible = true;
            inventory.CurrentViewOrSellState = state;
            inventory.FillWithInventory(PlayerDataManager.PlayerData.ItemInventory, 
                sellPriceMultiplier, inventoryRestrictions);
            inventory.PlayerMoneyText = "$" + PlayerDataManager.PlayerData.Money.ToString();
        }

        #endregion

        #endregion

        #region Script-helping methods

        public bool HasTag(string tag) =>
            dialogTagsThisFrame.Contains(tag);

        public void AwardMoney(int amountOfMoney)
        {
            GameScreenGum.NotificationBoxInstance.AddNotification($"Earned ${amountOfMoney}");

            PlayerDataManager.PlayerData.Money += amountOfMoney;
        }

        #endregion

        #region Destroy

        void CustomDestroy()
        {
            WaterCollision.RemoveFromManagers();
            SolidCollision.RemoveFromManagers();

        }

        #endregion

        static void CustomLoadStaticContent(string contentManagerName)
        {


        }

    }
}
