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



namespace FishStory.Screens
{
    public partial class GameScreen
    {

        void CustomInitialize()
        {


        }

        void CustomActivity(bool firstTimeCalled)
        {


        }

        void CustomDestroy()
        {
            WaterCollision.RemoveFromManagers();
            SolidCollision.RemoveFromManagers();

        }

        static void CustomLoadStaticContent(string contentManagerName)
        {


        }

    }
}
