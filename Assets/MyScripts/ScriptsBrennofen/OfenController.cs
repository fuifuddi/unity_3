using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using M2MqttUnity;
using uPLibrary.Networking.M2Mqtt.Messages;
using System.Text;
using System.Linq;

public class MovementController : M2MqttUnityClient
{
    [Header("UI")]
    [Tooltip("Referenz auf den UIController (Ampel)")]
    public UIController uiController;

    [Header("Subscription Settings")]
    private string startTopic = "multi/I9";
    private string checkpointTopic = "multi/Q14";
    private string colorTopic = "color/detector";
    private string endTopic = "color/Q2";

    [Header("Conveyor-Signal")]
    [Tooltip("Topic + Message für das ‚weiterfahren auf dem Fließband‘")]
    private string conveyorSignalTopic = "color/I2";
    private string conveyorSignalMessage = "read";

    // intern
    private bool conveyorSignalReceived = false;
    private const float conveyorZThreshold = 4.45f;
    private readonly Vector3 conveyorStopPos = new Vector3(4.45f, 0.4691761f, 2.59f);

    [Header("Zylinder (Coin) Spawn")]
    public GameObject coinPrefab;
    private Transform coin;
    private Rigidbody coinRb;

    [Header("Initial Coin Color")]
    public Color initialCoinColor = Color.grey;

    [Header("Zu bewegende Objekte")]
    public Transform object1;
    public Transform object2;
    public Transform object3;
    public Transform object4;
    public Transform object5;

    [Header("Unterobjekte")]
    public Transform object3_1;
    public Transform object4_1;

    [Header("Object1 Settings")]
    public float obj1Distance = 1.5f;
    public float obj1Duration = 0.5f;

    [Header("Object2 Settings")]
    public float obj2Distance = 2.0f;
    public float obj2Duration = 2.0f;

    [Header("Object3 Settings")]
    public float obj3Distance = 6.51f;
    public float obj3MoveDuration = 4.0f;

    [Header("Object3 Subobject Settings")]
    public float subObjDistance = 0.8f;
    public float subObjDuration = 0.2f;
    public float subObjPause = 1.0f;

    [Header("Object4 Subobject Settings")]
    public float obj4_1Distance = 1.5f;
    public float obj4_1Duration = 0.8f;

    [Header("Object4 Rotation Settings")]
    public Transform rotationPivot4;
    public float rotationDuration = 1.4f;
    public float rotationPause = 1.0f;

    [Header("Object5 Rotation Settings")]
    public Transform rotationPivot5;
    public Vector3 object5Axis = Vector3.up;
    public float object5Angle = 360f;
    public float object5Duration = 1.0f;

    [Header("Pauses")]
    public float brennPause = 2.7f;
    public float initialPause = 0.5f;

    // ─── Steuerungsflags & Checkpoint-Tracking ──────────────────────────
    private bool sequenceStarted = false;
    private bool skipToCheckpoint = false;
    private bool reachedCheckpoint = false;

    private List<string> checkpointKeys = new List<string>();
    private HashSet<string> receivedCheckpoints = new HashSet<string>();
    private string currentCheckpointKey = null;

    // ─── ACTION INTERFACE & IMPLEMENTIERUNGEN ──────────────────────────

    private interface IAction
    {
        IEnumerator Execute();
        void CompleteInstantly();
    }

    private class MoveAction : IAction
    {
        private MovementController ctrl;
        private Transform obj;
        private Vector3 direction;
        private float distance;
        private float duration;

        private bool hasStarted = false;
        private Vector3 startPos;
        private Vector3 endPos;

        public MoveAction(MovementController controller, Transform target, Vector3 dir, float dist, float dur)
        {
            ctrl = controller;
            obj = target;
            direction = dir;
            distance = dist;
            duration = dur;
        }

        public IEnumerator Execute()
        {
            if (obj == null) yield break;
            hasStarted = true;
            startPos = obj.position;
            endPos = startPos + direction.normalized * distance;
            yield return ctrl.MoveOverTime(obj, startPos, endPos, ctrl.AdjustDuration(duration));
        }

        public void CompleteInstantly()
        {
            if (obj == null) return;
            if (!hasStarted)
            {
                startPos = obj.position;
                endPos = startPos + direction.normalized * distance;
                hasStarted = true;
            }
            obj.position = endPos;
        }
    }

    private class RotateAction : IAction
    {
        private MovementController ctrl;
        private Transform obj;
        private Vector3 pivot, axis;
        private float angle, duration;

        public RotateAction(MovementController c, Transform o, Vector3 p, Vector3 a, float ang, float d)
        {
            ctrl = c;
            obj = o;
            pivot = p;
            axis = a;
            angle = ang;
            duration = d;
        }

        public IEnumerator Execute()
        {
            yield return ctrl.RotateAroundPivot(obj, pivot, axis, angle, ctrl.AdjustDuration(duration));
        }

        public void CompleteInstantly()
        {
            if (obj == null) return;
            obj.RotateAround(pivot, axis.normalized, angle);
        }
    }

    private class WaitAction : IAction
    {
        private MovementController ctrl;
        private float duration;

        public WaitAction(MovementController c, float d) { ctrl = c; duration = d; }

        public IEnumerator Execute()
        {
            yield return ctrl.WaitOrSkip(ctrl.AdjustDuration(duration));
        }

        public void CompleteInstantly()
        {
            // Pause überspringen geschieht in WaitOrSkip
        }
    }

    private class CallbackAction : IAction
    {
        private Action callback;
        public CallbackAction(MovementController c, Action cb) { callback = cb; }

        public IEnumerator Execute()
        {
            callback?.Invoke();
            yield break;
        }

        public void CompleteInstantly() => callback?.Invoke();
    }

    private class ParallelAction : IAction
    {
        private MovementController ctrl;
        private List<IAction> actions;
        public ParallelAction(MovementController c, List<IAction> list)
        {
            ctrl = c;
            actions = list;
        }

        public IEnumerator Execute()
        {
            var coros = new List<Coroutine>();
            foreach (var act in actions)
                coros.Add(ctrl.StartCoroutine(act.Execute()));
            foreach (var cr in coros)
                yield return cr;
        }

        public void CompleteInstantly()
        {
            foreach (var act in actions)
                act.CompleteInstantly();
        }
    }

    private class ConveyorWaitAction : IAction
    {
        private MovementController ctrl;

        public ConveyorWaitAction(MovementController c)
        {
            ctrl = c;
        }

        public IEnumerator Execute()
        {
            // Wenn Signal schon da: sofort an Stop-Position setzen
            if (ctrl.conveyorSignalReceived)
            {
                if (ctrl.coin != null)
                    ctrl.coin.position = ctrl.conveyorStopPos;
                yield break;
            }

            // Warten bis Coin gespawnt ist
            while (ctrl.coin == null)
                yield return null;

            // Warten bis Coin über Threshold oder Signal da
            while (ctrl.coin.position.x <= conveyorZThreshold
                   && !ctrl.conveyorSignalReceived)
            {
                yield return null;
            }

            // Coin anhalten
            if (ctrl.coinRb != null)
                ctrl.coinRb.isKinematic = true;

            // Auf exakte Stop-Position teleportieren
            ctrl.coin.position = ctrl.conveyorStopPos;

            // Auf Signal warten
            while (!ctrl.conveyorSignalReceived)
                yield return null;

            ctrl.coinRb.isKinematic = false;
        }

        public void CompleteInstantly()
        {
            if (ctrl.coin != null)
                ctrl.coin.position = ctrl.conveyorStopPos;
        }
    }

    // ─── UNITY & MQTT LIFECYCLE ──────────────────────────

    protected override void Start()
    {
        brokerAddress = "127.0.0.1";
        brokerPort = 1883;
        autoConnect = true;
        Debug.Log("[MovementController] MQTT-Start…");
        base.Start();

        if (uiController == null)
            Debug.LogError("[MovementController] ⚠️ uiController ist NULL! Bitte im Inspector zuweisen.");
        else
            uiController.SetReady(true);
    }

    protected override void OnConnected()
    {
        Debug.Log("[MovementController] Mit Broker verbunden.");
        base.OnConnected();
    }

    protected override void SubscribeTopics()
    {
        client.Subscribe(
            new string[] { startTopic, checkpointTopic, "multi/I8", conveyorSignalTopic, colorTopic, endTopic},
            new byte[] {
                MqttMsgBase.QOS_LEVEL_AT_LEAST_ONCE,
                MqttMsgBase.QOS_LEVEL_AT_LEAST_ONCE,
                MqttMsgBase.QOS_LEVEL_AT_LEAST_ONCE,
                MqttMsgBase.QOS_LEVEL_AT_LEAST_ONCE,
                MqttMsgBase.QOS_LEVEL_AT_LEAST_ONCE,
                MqttMsgBase.QOS_LEVEL_AT_LEAST_ONCE
            }
        );
        Debug.Log($"[MovementController] Topics abonniert: {startTopic}, multi/I8, {checkpointTopic}, {conveyorSignalTopic}, {colorTopic}, {endTopic}");
    }

    protected override void DecodeMessage(string topic, byte[] message)
    {
        string msg = Encoding.UTF8.GetString(message).Trim();
        string keyStr = topic + ":" + msg;
        Debug.Log($"[MovementController] Nachricht auf '{topic}': '{msg}'");

        // Farb-Topic
        if (topic == colorTopic && coin != null)
        {
            Color newColor;
            switch (msg.ToLowerInvariant())
            {
                case "white": newColor = Color.white; break;
                case "red": newColor = Color.red; break;
                case "blue": newColor = Color.blue; break;
                default:
                    Debug.LogWarning($"[MovementController] Unbekannte Farbe '{msg}' erhalten.");
                    return;
            }
            var rend = coin.GetComponent<Renderer>() ?? coin.GetComponentInChildren<Renderer>();
            if (rend != null)
            {
                rend.material.color = newColor;
                Debug.Log($"[MovementController] Coin-Farbe gesetzt auf {msg}.");
            }
            return;
        }

        // Start-Signal
        if (!sequenceStarted
            && topic == startTopic
            && msg.Equals("read", StringComparison.InvariantCultureIgnoreCase))
        {
            sequenceStarted = true;
            Debug.Log("[MovementController] Start-Signal erkannt → Sequenz startet.");
            uiController?.SetReady(false);
            SpawnCoin();
            StartCoroutine(SequenceRoutine());
            return;
        }

        // Conveyor-Signal
        if (topic == conveyorSignalTopic
            && msg.Equals(conveyorSignalMessage, StringComparison.InvariantCultureIgnoreCase))
        {
            conveyorSignalReceived = true;
            Debug.Log("[MovementController] Conveyor-Signal empfangen → Coin darf weiter.");
            return;
        }

        // Checkpoint-Signale (multi/I8:read oder Q14:stop)
        if (checkpointKeys.Contains(keyStr))
        {
            receivedCheckpoints.Add(keyStr);

            if (!reachedCheckpoint && keyStr == currentCheckpointKey)
            {
                skipToCheckpoint = true;
                Debug.Log("[MovementController] Anlage schneller → Skip aktiviert.");
            }
            else if (reachedCheckpoint && keyStr == currentCheckpointKey)
            {
                Debug.Log("[MovementController] Anlage langsamer → Weiter freigegeben.");
            }
        }
    }

    private void SpawnCoin()
    {
        if (coinPrefab == null)
        {
            Debug.LogError("[MovementController] Kein coinPrefab gesetzt!");
            return;
        }

        var go = Instantiate(
            coinPrefab,
            new Vector3(1.396f, 1f, -7.555f),
            Quaternion.identity
        );
        coin = go.transform;
        coinRb = go.GetComponent<Rigidbody>();
        if (coinRb != null) coinRb.isKinematic = true;

        var rend = go.GetComponent<Renderer>() ?? go.GetComponentInChildren<Renderer>();
        if (rend != null)
        {
            rend.material.color = initialCoinColor;
            Debug.Log("[MovementController] Coin initial in Grau gesetzt.");
        }
    }

    protected override void OnConnectionFailed(string errorMessage)
        => Debug.LogError("[MovementController] Connect failed: " + errorMessage);

    protected override void OnDisconnected()
        => Debug.LogWarning("[MovementController] Disconnected.");

    protected override void OnConnectionLost()
        => Debug.LogWarning("[MovementController] Connection lost!");

    private void OnDestroy() => Disconnect();

    // ─── SEQUENCE ROUTINE MIT DYNAMISCHEN CHECKPOINTS ──────────────────────────

    private IEnumerator SequenceRoutine()
    {
        // Flags & Tracking initialisieren
        receivedCheckpoints.Clear();
        checkpointKeys.Clear();
        skipToCheckpoint = false;
        reachedCheckpoint = false;
        currentCheckpointKey = null;
        conveyorSignalReceived = false;

        string i8Key = "multi/I8:read";
        string q14Key = checkpointTopic + ":stop";
        string q2Key = endTopic + ":stop";
        string postKey = "__after_checkpoint";

        // 2) Aktionsgruppen definieren
        var groupA = new List<IAction>
        {
            // Schritte bis ParallelAction
            new MoveAction(this, object1, Vector3.up,   obj1Distance, obj1Duration),
            new WaitAction(this, initialPause),
            new CallbackAction(this, () => { if (coin!=null) coin.SetParent(object2, true); }),
            new MoveAction(this, object2, Vector3.left, obj2Distance, obj2Duration),
            new MoveAction(this, object1, Vector3.down, obj1Distance, obj1Duration),
            new WaitAction(this, brennPause),
            new MoveAction(this, object1, Vector3.up,   obj1Distance, obj1Duration),
            new WaitAction(this, initialPause),
            new MoveAction(this, object2, Vector3.right,obj2Distance, obj2Duration),
            new ParallelAction(this, new List<IAction>
            {
                new MoveAction(this, object1, Vector3.down, obj1Distance, obj1Duration),
                new MoveAction(this, object3, Vector3.back,  obj3Distance, obj3MoveDuration)
            })
        };

        var groupB = new List<IAction>
        {
            // Schritte nach ParallelAction bis Q14
            new MoveAction(this, object3_1, Vector3.down, subObjDistance, subObjDuration),
            new WaitAction(this, subObjPause),
            new CallbackAction(this, () => { if (coin!=null) coin.SetParent(object3_1, true); }),
            new MoveAction(this, object3_1, Vector3.up,   subObjDistance, subObjDuration),
            new MoveAction(this, object3,   Vector3.forward,obj3Distance, obj3MoveDuration),
            new MoveAction(this, object3_1, Vector3.down, subObjDistance, subObjDuration),
            new WaitAction(this, subObjPause),
            new CallbackAction(this, () => { if (coin!=null) coin.SetParent(null, true); }),
            new MoveAction(this, object3_1, Vector3.up,   subObjDistance, subObjDuration),
            new CallbackAction(this, () => { if (coin!=null && object4!=null) coin.SetParent(object4, true); }),
            new RotateAction(this, object4, rotationPivot4.position, Vector3.up, 180f, rotationDuration*2f),
            new RotateAction(this, object5, rotationPivot5.position, object5Axis, object5Angle, object5Duration),
            new RotateAction(this, object4, rotationPivot4.position, Vector3.up, 90f, rotationDuration),
            new CallbackAction(this, () => {
                if (coin != null)
                    coin.SetParent(object4_1, true);
            }),
            new MoveAction(this, object4_1, object4_1.forward, obj4_1Distance, obj4_1Duration),
            new CallbackAction(this, () => {
                if (coinRb != null) coinRb.isKinematic = false;
                if (coin != null)
                    coin.SetParent(null, true);
            }),
        };

        var groupC = new List<IAction>
        {
            // nach Q14
            new ParallelAction(this, new List<IAction>
            {
                new ConveyorWaitAction(this),
                new MoveAction(this, object4_1, -object4_1.forward, obj4_1Distance, obj4_1Duration),
            }),
        };

        var groupD = new List<IAction>
        {
            new RotateAction(this, object4, rotationPivot4.position, Vector3.up, -270f, rotationDuration*3f),
        };

        // Mapping Key → Gruppe
        var map = new Dictionary<string, List<IAction>>
        {
            { i8Key,   groupA },
            { q14Key,  groupB },
            { q2Key,   groupC },
            { postKey, groupD }
        };

        var allKeys = map.Keys.ToList();

        // checkpointKeys bekommt automatisch alle Keys außer dem letzten
        checkpointKeys = allKeys
            .Take(allKeys.Count - 1)
            .ToList();

        // 3) Dynamische Schleife
        foreach (var key in map.Keys.ToList())
        {
            Debug.Log($"Now key {key}");
            currentCheckpointKey = key;
            reachedCheckpoint = false;

            foreach (var action in map[key])
                yield return action.Execute();

            reachedCheckpoint = true;
            if (key != postKey)
            {
                if (!receivedCheckpoints.Contains(key))
                {
                    Debug.Log($"[MovementController] Warte auf {key}…");
                    while (!receivedCheckpoints.Contains(key))
                        yield return null;
                    Debug.Log($"[MovementController] {key} empfangen → weiter.");
                }
            }

            skipToCheckpoint = false;
            reachedCheckpoint = false;
        }

        uiController?.SetReady(true);

        // 4) Cleanup
        sequenceStarted = false;
        skipToCheckpoint = false;
        reachedCheckpoint = false;
        currentCheckpointKey = null;
        checkpointKeys.Clear();
        receivedCheckpoints.Clear();
        coin = null;
        coinRb = null;
    }

    // ─── HELPER COROUTINES ──────────────────────────

    private float AdjustDuration(float original) =>
        (!reachedCheckpoint && skipToCheckpoint) ? 0f : original;

    public IEnumerator MoveOverTime(
        Transform obj, Vector3 from, Vector3 to, float duration
    )
    {
        if (obj == null) yield break;
        if (duration <= 0f)
        {
            obj.position = to;
            yield break;
        }
        float elapsed = 0f;
        while (elapsed < duration)
        {
            if (skipToCheckpoint && !reachedCheckpoint)
            {
                obj.position = to;
                yield break;
            }
            obj.position = Vector3.Lerp(from, to, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }
        obj.position = to;
    }

    public IEnumerator RotateAroundPivot(
        Transform obj, Vector3 pivot, Vector3 axis, float angle, float duration
    )
    {
        if (obj == null) yield break;
        if (duration <= 0f)
        {
            obj.RotateAround(pivot, axis.normalized, angle);
            yield break;
        }
        float elapsed = 0f;
        float totalAngle = angle;
        while (elapsed < duration)
        {
            if (skipToCheckpoint && !reachedCheckpoint && currentCheckpointKey != null)
            {
                float rotatedSoFar = (totalAngle / duration) * elapsed;
                float remainingAngle = totalAngle - rotatedSoFar;
                obj.RotateAround(pivot, axis.normalized, remainingAngle);
                yield break;
            }
            float delta = (totalAngle / duration) * Time.deltaTime;
            obj.RotateAround(pivot, axis.normalized, delta);
            elapsed += Time.deltaTime;
            yield return null;
        }
        float finalRotated = (totalAngle / duration) * elapsed;
        float leftover = totalAngle - finalRotated;
        if (Mathf.Abs(leftover) > 0.0001f)
            obj.RotateAround(pivot, axis.normalized, leftover);
    }

    public IEnumerator WaitOrSkip(float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            if (skipToCheckpoint && !reachedCheckpoint && currentCheckpointKey != null)
                yield break;
            elapsed += Time.deltaTime;
            yield return null;
        }
    }
}
