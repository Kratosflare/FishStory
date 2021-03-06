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
using FlatRedBall.TileEntities;
using FishStory.Managers;

namespace FishStory.Screens
{
    public partial class TitleScreen
    {

        void CustomInitialize()
        {
            MusicManager.Stop();
            InitializeEntitiesFromMap();
            
            InitializeCamera();

            //InitializeCollision();

            InitializeDarkness();

            InitializeWaterCaustics();
        }

        private void InitializeDarkness()
        {
            BaseUnderwaterDarknessColor.ColorOperation = FlatRedBall.Graphics.ColorOperation.Color;

            LightEffectsLayer.RenderTarget = DarknessRenderTarget;
            DarknessOverlaySprite.Texture = DarknessRenderTarget;
            DarknessOverlaySprite.Red = 0f;
            DarknessOverlaySprite.Green = 0.5f;
            DarknessOverlaySprite.Blue = 1.0f;
            DarknessOverlaySprite.BlendOperation = FlatRedBall.Graphics.BlendOperation.Modulate;

            DarknessOverlaySprite.Alpha = 0.65f;

            FadeOutSprite.Alpha = 1f;
        }

        private void InitializeWaterCaustics()
        {
            var waterEffectSpriteWidth = 256;
            var waterEffectSpriteHeight = 256;
            var cameraWidth = Camera.Main.OrthogonalWidth;
            var cameraHeight = Camera.Main.OrthogonalHeight;

            for (var x = 0; x < cameraWidth; x += waterEffectSpriteWidth)
            {
                for (var y = 0; y < cameraHeight+waterEffectSpriteHeight; y+= waterEffectSpriteHeight)
                {
                    var newWaterSprite = WaterEffectSprite.Clone();
                    newWaterSprite.X = WaterEffectSprite.X + x;
                    newWaterSprite.Y = WaterEffectSprite.Y - y;
                    newWaterSprite.Alpha = 0.35f;

                    SpriteManager.AddSprite(newWaterSprite);
                    SpriteManager.AddToLayer(newWaterSprite, WaterEffectLayer);
                    WaterCausticSpriteList.Add(newWaterSprite);
                }
            }

            WaterEffectSprite.Visible = false;
        }

        private void InitializeCamera()
        {
            //Camera.Main.X = PlayerCharacterInstance.X;
            //Camera.Main.Y = PlayerCharacterInstance.Y;

            Camera.Main.SetBordersAtZ(Map.X, Map.Y - Map.Height, Map.X + Map.Width, Map.Y, 0);
            Map.Z = -3;
            SpriteManager.OrderedSortType = FlatRedBall.Graphics.SortType.ZSecondaryParentY;
        }

        private void InitializeEntitiesFromMap()
        {
            TileEntityInstantiator.CreateEntitiesFrom(Map);

            foreach (var fish in FishList)
            {
                //propObject.Z = PlayerCharacterInstance.Z; // same as player so they sort
                fish.SetLayers(LightEffectsLayer);
            }


        }

        void CustomActivity(bool firstTimeCalled)
        {
            HandleInputActivity();

            if (MusicManager.IsSongPlaying == false)
            {
                MusicManager.PlaySong(GlobalContent.music_misty_woods_calling, forceRestart: true);
            }
        }

        private void HandleInputActivity()
        {
            if (fadeInHasEnded == false)
            {
                HandleFadeIn();
            }
            else if (fadeOutHasStarted)
            {
                HandleFadeOut();
            }
            else
            {
                if (InputManager.Xbox360GamePads[0].IsConnected &&
                        (InputManager.Xbox360GamePads[0].ButtonDown(Xbox360GamePad.Button.A) ||
                        InputManager.Xbox360GamePads[0].ButtonDown(Xbox360GamePad.Button.X) ||
                        InputManager.Xbox360GamePads[0].ButtonDown(Xbox360GamePad.Button.B) ||
                         InputManager.Xbox360GamePads[0].ButtonDown(Xbox360GamePad.Button.Start))
                    )
                {
                    HandleFadeOut();
                }
                else if (InputManager.Keyboard.KeyPushed(Microsoft.Xna.Framework.Input.Keys.Space) ||
                        InputManager.Keyboard.KeyPushed(Microsoft.Xna.Framework.Input.Keys.Enter) ||
                        InputManager.Keyboard.KeyPushed(Microsoft.Xna.Framework.Input.Keys.Escape) ||
                        InputManager.Keyboard.KeyPushed(Microsoft.Xna.Framework.Input.Keys.W) ||
                        InputManager.Keyboard.KeyPushed(Microsoft.Xna.Framework.Input.Keys.A) ||
                        InputManager.Keyboard.KeyPushed(Microsoft.Xna.Framework.Input.Keys.D) ||
                        InputManager.Keyboard.KeyPushed(Microsoft.Xna.Framework.Input.Keys.S))
                {
                    HandleFadeOut();
                }
            }
        }

        private bool fadeInHasEnded = false;
        private void HandleFadeIn()
        {
            if (FadeOutSprite.Alpha > 0)
            {
                FadeOutSprite.Alpha -= TimeManager.SecondDifference * (1f/(float)FadeInTimeInSeconds);
                MusicManager.MusicVolumeLevel = MusicManager.DefaultMusicLevel - (MusicManager.DefaultMusicLevel * FadeOutSprite.Alpha);
            }
            else
            {
                fadeInHasEnded = true;
                TitleScreenGum.IntroAnimationAnimation.Play();
            }
        }

        private bool fadeOutHasStarted = false;
        private void HandleFadeOut()
        {
            if (fadeOutHasStarted == false)
            {
                fadeOutHasStarted = true;
                SoundManager.Play(GlobalContent.GameStartSound);
            }

            if (FadeOutSprite.Alpha < 1)
            {
                FadeOutSprite.Alpha += TimeManager.SecondDifference * (1f / (float)FadeOutTimeInSeconds);
                MusicManager.MusicVolumeLevel = MusicManager.DefaultMusicLevel - (MusicManager.DefaultMusicLevel * FadeOutSprite.Alpha);
            }
            else
            {
                MoveToScreen(nameof(MainLevel));
            }
        }


        void CustomDestroy()
        {
            var spriteCount = WaterCausticSpriteList.Count;
            for (var i = spriteCount - 1; i >= 0; i--)
            {
                var spriteToRemove = WaterCausticSpriteList[i];
                SpriteManager.RemoveSprite(spriteToRemove);
            }

        }

        static void CustomLoadStaticContent(string contentManagerName)
        {


        }

    }
}
