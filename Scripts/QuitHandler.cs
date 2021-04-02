using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using System.Collections;

namespace EnemyRenamerTwitch
{
    class QuitHandler : MonoBehaviour
    {
        //makes sure tp kill the listener when the game  exits, otherwise game crashes. is only needed because ETGModule.Exit does not work for whatever reason
        void OnApplicationQuit()
        {
            if (TwitchRenamerModule.listener != null)
            {
                TwitchRenamerModule.listener.ForceStopListening();
            }
        }
    }
}
