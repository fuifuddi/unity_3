using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace game4automation
{
    [RequireComponent(typeof(DrivesRecorder))]
    public class ReplayRecording : Game4AutomationBehavior,ISignalInterface
    {
        public string Sequence;
        public PLCOutputBool StartOnSignal;

        private DrivesRecorder _drivesRecorder;
        // Start is called before the first frame update
        void Start()
        {
            _drivesRecorder = GetComponent<DrivesRecorder>();
            if (StartOnSignal != null)
                StartOnSignal.EventSignalChanged.AddListener(OnSignal);
        }

        private void OnSignal(Signal signal)
        {
            if (((PLCOutputBool)signal).Value == true)
                _drivesRecorder.StartReplay(Sequence);
        }

        // Update is called once per frame
        void Update()
        {
        
        }
    }

}
