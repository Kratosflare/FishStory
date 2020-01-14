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
using FishStory.Managers;
using FishStory.DataTypes;
using FishStory.Factories;
using FishStory.Entities;

namespace FishStory.Screens
{
    public partial class MainLevel
    {
        private string[] FishNames => GlobalContent.ItemDefinition
            .Where(item => item.Value.IsFish).Select(item => item.Value.Name).ToArray();

        private int TotalFishIdentified
        {
            get
            {
                int total = 0;
                foreach (var item in FishNames)
                {
                    total += PlayerDataManager.PlayerData.TimesFishIdentified.Get(item);
                }
                return total;
            }
        }
        void CustomInitialize()
        {
            InitializeScript();

        }

        private void InitializeScript()
        {
            var If = script;
            var Do = script;
                

            PlayerCharacterInstance.DirectionFacing = TopDownDirection.Left;

            PlayerDataManager.PlayerData.AwardItem(ItemDefinition.Small_Brown_Fish);
            PlayerDataManager.PlayerData.AwardItem(ItemDefinition.Small_Brown_Fish);
            PlayerDataManager.PlayerData.AwardItem(ItemDefinition.Small_Brown_Fish);

            #region Day 1
            //Identifier
            If.Check(() => HasTag("HasSeenIdentifierDay1"));
            Do.Call(() =>
            {
                NPCList.FindByName("Identifier").TwineDialogId = "IdentifierDay1Brief";
            });
            // Tycoon
            // He gives you the key if you have identified 3 fish
            int numFishRequiredForKey = 3;
            If.Check(() => HasTag("HasTalkedToTycoonDay1"));
            Do.Call(() =>
            {
                NPCList.FindByName("Tycoon").TwineDialogId = "TycoonNoFishNoKey";
            });
            If.Check(() => HasTag("HasTalkedToTycoonDay1") && TotalFishIdentified >= numFishRequiredForKey);
            Do.Call(() =>
            {
                NPCList.FindByName("Tycoon").TwineDialogId = "TycoonYesFishNoKey";
            });
            If.Check(() => HasTag("GiveTrailerKey"));
            Do.Call(() =>
            {
                PlayerDataManager.PlayerData.AwardItem(ItemDefinition.Trailer_Key);
                AddNotification("Recieved: Trailer Key");
            });
            If.Check(() => PlayerDataManager.PlayerData.Has(ItemDefinition.Trailer_Key));
            Do.Call(() =>
            {
                NPCList.FindByName("Tycoon").TwineDialogId = "TycoonYesKey";
            });

            // Mayor
            // TODO: This is annoying during testing, but turn it back on eventually!
            //If.Check(() => !HasTag("HasSeenWelcomeDialog") && PlayerCharacterInstance.X < 1070 );
            //Do.Call(() =>
            //{
            //    if (DialogBox.TryShow("WelcomeDialog"))
            //    {
            //        PlayerCharacterInstance.ObjectsBlockingInput.Add(DialogBox);
            //    }
            //});
            If.Check(() => HasTag("HasSeenWelcomeDialog"));
            Do.Call(() =>
            {
                var npc = this.NPCList.FindByName("Mayor");
                npc.TwineDialogId = nameof(GlobalContent.MayorAfterWelcome);
                PlayerDataManager.PlayerData.AwardItem("Festival Badge");
                PlayerDataManager.PlayerData.AwardItem("Festival Pamphlet");
                // Magic numbers to save time here... this is referenced in dialog as well.
                PlayerDataManager.PlayerData.Money -= 5;
                AddNotification("-$5.");
                AddNotification("Recieved: Festival Badge");
                AddNotification("Recieved: Festival Pamphlet");
            });

            //If.Check(() =>
            //{
            //    return PlayerDataManager.PlayerData.NpcRelationships["Dave"].EventsTriggered
            //        .Contains(5);
            //});
            #endregion
        }

        void CustomActivity(bool firstTimeCalled)
        {
            FlatRedBall.Debugging.Debugger.Write($"Player X: {PlayerCharacterInstance.X}, Player Y: {PlayerCharacterInstance.Y}");
            //FlatRedBall.Debugging.Debugger.Write($"Fish identified: {TotalFishIdentified}");
        }

        void CustomDestroy()
        {


        }

        static void CustomLoadStaticContent(string contentManagerName)
        {


        }

    }
}
