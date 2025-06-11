using System;
using System.Collections;
using UnityEngine;
using M2MqttUnity;
using uPLibrary.Networking.M2Mqtt.Messages;
using System.Text;

public class MovementController : M2MqttUnityClient
{
    [Header("UI")]
    [Tooltip("Referenz auf den UIController (Ampel)")]
    public UIController uiController;

    [Header("Subscription Settings")]
    [Tooltip("Topic, auf dem auf das Start-Signal gewartet wird")]
    private string startTopic = "multi/I9";
    [Tooltip("Topic für den neuen Checkpoint (Pusher-Auswurf stop)")]
    private string checkpointTopic = "multi/Q14";
    [Tooltip("Topic für die Coin-Farbe (weiß, rot oder blau)")]
    private string colorTopic = "color/detector";

    [Header("Zylinder (Coin) Spawn")]
    public GameObject coinPrefab;
    private Transform coin;
    private Rigidbody coinRb;

    [Header("Initial Coin Color")]
    [Tooltip("Farbe, die der Coin beim Spawn bekommt")]
    public Color initialCoinColor = Color.grey;

    [Header("Zu bewegende Objekte")]
    public Transform object1;
    public Transform object2;
    public Transform object3;
    public Transform object4;
    public Transform object5;

    [Header("Unterobjekte")]
    [Tooltip("Unterobjekt von Objekt3")]
    public Transform object3_1;
    [Tooltip("Unterobjekt von Objekt4 für Coin-Drop")]
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
    public float obj4_1Distance = 1.0f;
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

    // ─── Steuerungsflags ──────────────────────────
    private bool sequenceStarted = false;
    private bool checkpointReceived = false;
    private bool skipToCheckpoint = false;
    private bool reachedCheckpoint = false;

    // liefert 0 oder Originaldauer, je nach skip-Flag
    private float AdjustDuration(float original) =>
        (!reachedCheckpoint && skipToCheckpoint) ? 0f : original;

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
        {
            Debug.Log("[MovementController] uiController ist gesetzt.");
            uiController.SetReady(true);
        }
    }

    protected override void OnConnected()
    {
        Debug.Log("[MovementController] Mit Broker verbunden.");
        base.OnConnected();
    }

    protected override void SubscribeTopics()
    {
        // Jetzt abonnieren wir drei Topics
        client.Subscribe(
            new string[] { startTopic, checkpointTopic, colorTopic },
            new byte[] {
                MqttMsgBase.QOS_LEVEL_AT_LEAST_ONCE,
                MqttMsgBase.QOS_LEVEL_AT_LEAST_ONCE,
                MqttMsgBase.QOS_LEVEL_AT_LEAST_ONCE
            }
        );
        Debug.Log($"[MovementController] Topics abonniert: {startTopic}, {checkpointTopic}, {colorTopic}");
    }

    protected override void DecodeMessage(string topic, byte[] message)
    {
        string msg = Encoding.UTF8.GetString(message).Trim();
        Debug.Log($"[MovementController] Nachricht auf '{topic}': '{msg}'");

        // ——— Farb-Topic abfangen —————————————————————————
        if (topic == colorTopic && coin != null)
        {
            Color newColor;
            switch (msg.ToLowerInvariant())
            {
                case "white":
                    newColor = Color.white;
                    break;
                case "red":
                    newColor = Color.red;
                    break;
                case "blue":
                    newColor = Color.blue;
                    break;
                default:
                    Debug.LogWarning($"[MovementController] Unbekannte Farbe '{msg}' erhalten.");
                    return;
            }

            Renderer rend = coin.GetComponent<Renderer>()
                         ?? coin.GetComponentInChildren<Renderer>();
            if (rend != null)
            {
                rend.material.color = newColor;
                Debug.Log($"[MovementController] Coin-Farbe gesetzt auf {msg}.");
            }
            return;
        }

        // ——— Start-Signal ————————————————————————————————
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

        // ——— Checkpoint-Signal ————————————————————————————
        if (topic == checkpointTopic
            && msg.Equals("stop", StringComparison.InvariantCultureIgnoreCase))
        {
            Debug.Log("[MovementController] Checkpoint-Signal empfangen.");
            if (!reachedCheckpoint)
            {
                skipToCheckpoint = true;
                checkpointReceived = true;
                Debug.Log("[MovementController] Pre-Checkpoint: skip aktiviert.");
            }
            else
            {
                checkpointReceived = true;
                Debug.Log("[MovementController] Post-Checkpoint: weiter.");
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

        GameObject go = Instantiate(
            coinPrefab,
            new Vector3(1.39600003f, 1f, -7.55499983f),
            Quaternion.identity
        );

        coin = go.transform;
        coinRb = go.GetComponent<Rigidbody>();
        if (coinRb != null) coinRb.isKinematic = true;

        // Initial immer grau
        Renderer rend = go.GetComponent<Renderer>()
                     ?? go.GetComponentInChildren<Renderer>();
        if (rend != null)
        {
            rend.material.color = initialCoinColor;
            Debug.Log($"[MovementController] Coin initial in Grau gesetzt.");
        }
    }

    protected override void OnConnectionFailed(string errorMessage)
        => Debug.LogError("[MovementController] Connect failed: " + errorMessage);

    protected override void OnDisconnected()
        => Debug.LogWarning("[MovementController] Disconnected.");

    protected override void OnConnectionLost()
        => Debug.LogWarning("[MovementController] Connection lost!");

    private void OnDestroy() => Disconnect();

    private IEnumerator SequenceRoutine()
    {
        reachedCheckpoint = false;

        Vector3 base1 = object1.position;
        Vector3 top1 = base1 + Vector3.up * obj1Distance;
        Vector3 base2 = object2.position;
        Vector3 left2 = base2 + Vector3.left * obj2Distance;
        Vector3 base3 = object3.position;
        Vector3 left3 = base3 + Vector3.back * obj3Distance;

        yield return MoveOverTime(object1, base1, top1, AdjustDuration(obj1Duration));
        yield return WaitOrSkip(AdjustDuration(initialPause));
        if (coin != null) coin.SetParent(object2, true);
        yield return MoveOverTime(object2, base2, left2, AdjustDuration(obj2Duration));
        yield return MoveOverTime(object1, top1, base1, AdjustDuration(obj1Duration));
        yield return WaitOrSkip(AdjustDuration(brennPause));
        yield return MoveOverTime(object1, base1, top1, AdjustDuration(obj1Duration));
        yield return WaitOrSkip(AdjustDuration(initialPause));
        yield return MoveOverTime(object2, left2, base2, AdjustDuration(obj2Duration));

        Coroutine move1Down = StartCoroutine(
            MoveOverTime(object1, top1, base1, AdjustDuration(obj1Duration))
        );
        Coroutine move3Left = StartCoroutine(
            MoveOverTime(object3, base3, left3, AdjustDuration(obj3MoveDuration))
        );
        yield return move1Down;
        yield return move3Left;

        Vector3 startA = object3_1.position;
        Vector3 downA = startA + Vector3.down * subObjDistance;
        yield return MoveOverTime(object3_1, startA, downA, AdjustDuration(subObjDuration));
        yield return WaitOrSkip(AdjustDuration(subObjPause));
        if (coin != null) coin.SetParent(object3_1, true);
        yield return MoveOverTime(object3_1, downA, startA, AdjustDuration(subObjDuration));

        yield return MoveOverTime(object3, left3, base3, AdjustDuration(obj3MoveDuration));

        Vector3 startB = object3_1.position;
        Vector3 downB = startB + Vector3.down * subObjDistance;
        yield return MoveOverTime(object3_1, startB, downB, AdjustDuration(subObjDuration));
        yield return WaitOrSkip(AdjustDuration(subObjPause));
        if (coin != null) coin.SetParent(null, true);
        yield return MoveOverTime(object3_1, downB, startB, AdjustDuration(subObjDuration));

        if (coin != null && object4 != null) coin.SetParent(object4, true);
        yield return RotateAroundPivot(
            object4,
            rotationPivot4.position,
            Vector3.up,
            180f,
            AdjustDuration(rotationDuration * 2f)
        );

        if (object5 != null && rotationPivot5 != null)
        {
            yield return RotateAroundPivot(
                object5,
                rotationPivot5.position,
                object5Axis,
                object5Angle,
                AdjustDuration(object5Duration)
            );
        }

        yield return RotateAroundPivot(
            object4,
            rotationPivot4.position,
            Vector3.up,
            90f,
            AdjustDuration(rotationDuration)
        );

        if (object4_1 != null)
        {
            Vector3 start4 = object4_1.position;
            Vector3 right4 = start4 + object4_1.right * obj4_1Distance;
            if (coin != null) coin.SetParent(object4_1, true);

            yield return MoveOverTime(object4_1, start4, right4, AdjustDuration(obj4_1Duration));

            reachedCheckpoint = true;
            if (!checkpointReceived)
            {
                Debug.Log("[MovementController] zu schnell → warte auf Q14 stop…");
                while (!checkpointReceived)
                    yield return null;
                Debug.Log("[MovementController] Q14 stop empfangen → weiter.");
            }
            skipToCheckpoint = false;

            if (coinRb != null) coinRb.isKinematic = false;
            if (coin != null) coin.SetParent(null, true);
            yield return MoveOverTime(object4_1, right4, start4, AdjustDuration(obj4_1Duration));
        }

        yield return WaitOrSkip(AdjustDuration(rotationPause));
        yield return RotateAroundPivot(
            object4,
            rotationPivot4.position,
            Vector3.up,
            -270f,
            AdjustDuration(rotationDuration * 3f)
        );

        Debug.Log("[MovementController] Gesamte Sequenz abgeschlossen → Maschine bereit → Grün");
        uiController?.SetReady(true);

        sequenceStarted = false;
        checkpointReceived = false;
        skipToCheckpoint = false;
        reachedCheckpoint = false;
        coin = null;
        coinRb = null;
    }

    private IEnumerator MoveOverTime(Transform obj, Vector3 from, Vector3 to, float duration)
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

    private IEnumerator RotateAroundPivot(
        Transform obj, Vector3 pivot, Vector3 axis, float angle, float duration
    )
    {
        if (obj == null) yield break;

        if (duration <= 0f)
        {
            Debug.Log($"[MovementController] Instant RotateAroundPivot um {angle}° (duration=0).");
            obj.RotateAround(pivot, axis.normalized, angle);
            yield break;
        }

        float elapsed = 0f;
        float sign = Mathf.Sign(angle);
        float total = Mathf.Abs(angle);
        while (elapsed < duration)
        {
            if (skipToCheckpoint && !reachedCheckpoint)
                yield break;
            float step = sign * (total / duration) * Time.deltaTime;
            obj.RotateAround(pivot, axis.normalized, step);
            elapsed += Time.deltaTime;
            yield return null;
        }
        float remaining = total - (total / duration) * duration;
        if (Mathf.Abs(remaining) > 0.0001f)
            obj.RotateAround(pivot, axis.normalized, sign * remaining);
    }

    private IEnumerator WaitOrSkip(float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            if (skipToCheckpoint && !reachedCheckpoint)
                yield break;
            elapsed += Time.deltaTime;
            yield return null;
        }
    }
}
