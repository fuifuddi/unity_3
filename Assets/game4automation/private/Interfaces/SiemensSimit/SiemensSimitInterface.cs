// Game4Automation (R) Framework for Automation Concept Design, Virtual Commissioning and 3D-HMI
// (c) 2019 in2Sight GmbH - Usage of this source code only allowed based on License conditions see https://game4automation.com/lizenz  

#if UNITY_STANDALONE_WIN
#pragma warning disable 0168
#pragma warning disable 0649
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Linq.Expressions;
using System.Text;
using UnityEngine;
using System.Threading;
using NaughtyAttributes;
#if GAME4AUTOMATION_SIEMENSSIMIT
using CouplingToolbox;
using CouplingToolbox.Simit;
#endif

namespace game4automation
{
    //!  Shared memory interface for an interface based on Siemens Simit shared memory structure (see Simit documentation)
    [HelpURL("https://game4automation.com/documentation/current/simit.html")]
#if GAME4AUTOMATION_SIEMENSSIMIT
    [RequireComponent(typeof(SiemensSimitCoupler))]
#endif
    public class SiemensSimitInterface : InterfaceBaseClass
    {
#if !GAME4AUTOMATION_SIEMENSSIMIT
        [InfoBox("To use the Siemens Simit Interface, you need to install the Siemens Simit SDK and add the define GAME4AUTOMATION_SIEMENSSIMIT to the Scripting Define Symbols in the Player Settings (Edit->Project Settings->Player->Other Settings->Scripting Define Symbols")]        

#endif
        public GameObject SimitConnection; //! Reference to the Simit Connection
#if GAME4AUTOMATION_SIEMENSSIMIT
        [ReadOnly] private string SimitConnectionStatus;
        private SiemensSimitCoupler simitcoupler;
        private SimitConnector simitconnector;
        private TimeController timecontroller;
#endif
        public override void OpenInterface()
        {
         
        }
#if GAME4AUTOMATION_SIEMENSSIMIT
        // Use this for initialization
        void Start()
        {

            simitcoupler = GetComponent<SiemensSimitCoupler>();
            simitconnector = SimitConnection.GetComponent<SimitConnector>();
            if (simitconnector == null)
            {
                Error("Siemens Simit Interface - No Simit Connection with included Simit Connector referenced!");
                return;
            }

            OpenInterface();

        }
        
        void FixedUpdate()
        {
            simitcoupler.UpdateSignals();
        }

        void Update()
        {
            if (simitconnector.ConnectionStatus == ConnectionStatus.Connected)
            {
                IsConnected = true;
                OnConnected();
            }

            if (IsConnected && simitconnector.ConnectionStatus != ConnectionStatus.Connected)
            {
                IsConnected = false;
                OnDisconnected();
            }
            SimitConnectionStatus = simitconnector.ConnectionStatus.ToString();
        }
#endif
        public override void CloseInterface()
        {
            OnDisconnected();
        }
    }
}
#endif