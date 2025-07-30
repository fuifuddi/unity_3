#if GAME4AUTOMATION_SIEMENSSIMIT
using System.Collections;
using System.Collections.Generic;
using CouplingToolbox;
using game4automation;
using UnityEngine;
using Signal = CouplingToolbox.Signal;

using CouplingToolbox;

public class SiemensSimitCoupler : SignalComponent
{
    // Start is called before the first frame update
    private game4automation.Signal[] signals;
    
    public void ConnectSignals()
    {
        signals = GetComponentsInChildren<game4automation.Signal>();
     
        foreach (var signal in signals)
        {
            var type = signal.GetType();
            if (signal.IsInput())
            {
                if (type==typeof(PLCOutputBool))
                    AddOutputSignal(signal.Name,SignalType.Binary);
                if (type==typeof(PLCOutputFloat))
                    AddOutputSignal(signal.Name,SignalType.Analog);
                if (type==typeof(PLCOutputInt))
                    AddOutputSignal(signal.Name,SignalType.Integer);
            }

            if (!signal.IsInput())
            {
                if (type==typeof(PLCOutputBool))
                    AddInputSignal(signal.Name,SignalType.Binary);
                if (type==typeof(PLCOutputFloat))
                    AddInputSignal(signal.Name,SignalType.Analog);
                if (type==typeof(PLCOutputInt))
                    AddInputSignal(signal.Name,SignalType.Integer);
            }
        }
    }

    public void UpdateSignals()
    {
        foreach (var signal in signals)
        {
            var type = signal.GetType();
            if (!signal.IsInput())
            {
                if (type==typeof(PLCOutputBool))
                    ((PLCOutputBool)signal).Value = GetInputSignal("GameObjectSwitch").BinaryValue;
                if (type==typeof(PLCOutputFloat))
                    ((PLCOutputFloat)signal).Value = (float)GetInputSignal("GameObjectSwitch").AnalogValue;
                if (type==typeof(PLCOutputInt))
                    ((PLCOutputInt)signal).Value = (int)GetInputSignal("GameObjectSwitch").IntegerValue;
            }

            if (signal.IsInput())
            {
                if (type == typeof(PLCInputBool))
                    GetOutputSignal(signal.Name).BinaryValue = ((PLCInputBool) signal).Value;
                if (type == typeof(PLCInputFloat))
                    GetOutputSignal(signal.Name).AnalogValue = ((PLCInputFloat) signal).Value;
                if (type == typeof(PLCInputInt))
                    GetOutputSignal(signal.Name).IntegerValue = ((PLCInputInt) signal).Value;
            }
        }
    }
   
    
    public override void InitializeSignals()
    {
        base.InitializeSignals();
        ConnectSignals();
    }


}
#else
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SiemensSimitCoupler : MonoBehaviour
{
   
}

#endif

