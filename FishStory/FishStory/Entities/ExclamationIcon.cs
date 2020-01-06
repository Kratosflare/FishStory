using System;
using System.Collections.Generic;
using System.Text;
using FlatRedBall;
using FlatRedBall.Input;
using FlatRedBall.Instructions;
using FlatRedBall.AI.Pathfinding;
using FlatRedBall.Graphics.Animation;
using FlatRedBall.Graphics.Particle;
using FlatRedBall.Math.Geometry;

namespace FishStory.Entities
{
    public partial class ExclamationIcon
    {
        /// <summary>
        /// Initialization logic which is execute only one time for this Entity (unless the Entity is pooled).
        /// This method is called when the Entity is added to managers. Entities which are instantiated but not
        /// added to managers will not have this method called.
        /// </summary>
        private void CustomInitialize()
        {


        }

        private void CustomActivity()
        {


        }

        public void BeginAnimations()
        {
            this.SpriteInstance.CurrentChainName = AppearAnimation.Name ;
            this.SpriteInstance.CurrentFrameIndex = 0;

            this.Call(() =>
            {
                this.SpriteInstance.CurrentChainName = CycleAnimation.Name;
            }).After(AppearAnimation.TotalLength);
        }

        private void CustomDestroy()
        {


        }

        private static void CustomLoadStaticContent(string contentManagerName)
        {


        }
    }
}
