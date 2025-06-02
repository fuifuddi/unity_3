using System;
using System.Collections;
using UnityEngine;
using M2MqttUnity;
using uPLibrary.Networking.M2Mqtt.Messages;
using System.Text;

public class MovementController : M2MqttUnityClient
{
    [Header("Subscription Settings")]
    [Tooltip("Topic, auf dem auf das Start-Signal gewartet wird")]
    public string subscribeTopic = "unity/test";

    [Header("Zylinder (Coin) Spawn")]
    [Tooltip("Prefab für den Zylinder, das beim Start gespawnt wird")]
    public GameObject coinPrefab;
    private Transform coin;
    private Rigidbody coinRb;

    [Header("Zu bewegende Objekte")]
    public Transform object1;
    public Transform object2;
    public Transform object3;
    public Transform object4;
    public Transform object5;

    [Header("Rotations-Pivot für Objekt 5")]
    public Transform rotationPivot5;
    public Vector3 object5Axis = Vector3.up;
    public float object5Angle = 360f;
    public float object5Duration = 1.0f;

    [Header("Unterobjekt von Objekt 4 (für Schub)")]
    public Transform object4_1;
    public float obj4_1Distance = 1.0f;
    public float obj4_1Duration = 0.8f;

    [Header("Unterobjekt von Objekt 3")]
    public Transform object3_1;
    private float subObjDistance = 0.8f;
    private float subObjDuration = 0.5f;
    private float subObjPause = 0.5f;

    [Header("Rotation Pivot für Objekt 4")]
    public Transform rotationPivot4;
    public float rotationDuration = 0.5f;
    public float rotationPause = 1.0f;
    public float brennPause = 1.0f;

    // individuelle Einstellungen
    private float obj1Distance = 1.5f;
    private float obj1Duration = 0.8f;
    private float obj2Distance = 2.0f;
    private float obj2Duration = 1.2f;
    private float obj3Distance = 6.51f;

    // verhindert Mehrfachstarts
    private bool sequenceStarted = false;

    protected override void Start()
    {
        // MQTT-Broker konfigurieren
        brokerAddress = "127.0.0.1";
        brokerPort = 1883;
        autoConnect = true;
        Debug.Log("[MovementController] Starte MQTT…");
        base.Start();
    }

    protected override void OnConnected()
    {
        Debug.Log("[MovementController] Mit Broker verbunden.");
        base.OnConnected();
    }

    protected override void SubscribeTopics()
    {
        Debug.Log($"[MovementController] Abonniere '{subscribeTopic}'…");
        client.Subscribe(
            new string[] { subscribeTopic },
            new byte[] { MqttMsgBase.QOS_LEVEL_AT_LEAST_ONCE }
        );
        Debug.Log("[MovementController] Abonniert.");
    }

    protected override void DecodeMessage(string topic, byte[] message)
    {
        string msg = Encoding.UTF8.GetString(message);
        Debug.Log($"[MovementController] Nachricht auf '{topic}': {msg}");

        if (!sequenceStarted
            && topic == subscribeTopic
            && msg.Equals("start", StringComparison.InvariantCultureIgnoreCase))
        {
            sequenceStarted = true;
            SpawnCoin();
            Debug.Log("[MovementController] Start-Signal, Sequenz startet.");
            StartCoroutine(SequenceRoutine());
        }
    }

    private void SpawnCoin()
    {
        if (coinPrefab == null)
        {
            Debug.LogError("[MovementController] Kein coinPrefab gesetzt!");
            return;
        }

        // neuen Coin instanziieren
        GameObject go = Instantiate(
            coinPrefab,
            new Vector3(1.39600003f, 1f, -7.55499983f),
            Quaternion.identity
        );
        coin = go.transform;

        // kinematischen Rigidbody setzen
        coinRb = go.GetComponent<Rigidbody>();
        if (coinRb != null) coinRb.isKinematic = true;

        // zufällige Farbe
        Color[] possible = { Color.red, Color.green, Color.blue };
        Color c = possible[UnityEngine.Random.Range(0, possible.Length)];
        Renderer rend = go.GetComponent<Renderer>() ?? go.GetComponentInChildren<Renderer>();
        if (rend != null)
        {
            rend.material.color = c;
            Debug.Log($"[MovementController] Coin-Farbe: {c}");
        }
    }

    protected override void OnConnectionFailed(string errorMessage)
        => Debug.LogError("[MovementController] Connect failed: " + errorMessage);

    protected override void OnDisconnected()
        => Debug.LogWarning("[MovementController] Disconnected.");

    protected override void OnConnectionLost()
        => Debug.LogWarning("[MovementController] Connection lost!");

    private void OnDestroy()
        => Disconnect();

    private IEnumerator SequenceRoutine()
    {
        // Basispunkte merken
        Vector3 base1 = object1.position;
        Vector3 base2 = object2.position;
        Vector3 base3 = object3.position;
        Vector3 obj3LeftTarget = base3 + Vector3.back * obj3Distance;

        // --- Objekt3 parallel nach links ---
        float obj3LeftDuration = obj2Duration + obj1Duration + brennPause;
        Coroutine obj3Left = StartCoroutine(
            MoveOverTime(object3, base3, obj3LeftTarget, obj3LeftDuration)
        );

        // --- Objekt2 & Objekt1 Abläufe ---
        if (coin != null) coin.SetParent(object2, true);
        yield return MoveOverTime(object2, base2, base2 + Vector3.left * obj2Distance, obj2Duration);
        yield return MoveOverTime(object1, base1, base1 - Vector3.up * obj1Distance, obj1Duration);
        yield return new WaitForSeconds(brennPause);
        yield return obj3Left;
        yield return MoveOverTime(object1, base1 - Vector3.up * obj1Distance, base1, obj1Duration);
        yield return MoveOverTime(object2, base2 + Vector3.left * obj2Distance, base2, obj2Duration);

        // --- object3_1-Schaukeln ---
        Vector3 startA = object3_1.position;
        Vector3 downA = startA + Vector3.down * subObjDistance;
        yield return MoveOverTime(object3_1, startA, downA, subObjDuration);
        yield return new WaitForSeconds(subObjPause);
        if (coin != null) coin.SetParent(object3_1, true);
        yield return MoveOverTime(object3_1, downA, startA, subObjDuration);

        yield return MoveOverTime(object3, obj3LeftTarget, base3, obj3LeftDuration);

        Vector3 startB = object3_1.position;
        Vector3 downB = startB + Vector3.down * subObjDistance;
        yield return MoveOverTime(object3_1, startB, downB, subObjDuration);
        yield return new WaitForSeconds(subObjPause);
        if (coin != null) coin.SetParent(null, true);
        yield return MoveOverTime(object3_1, downB, startB, subObjDuration);

        // --- Objekt4: 180° Rotation ---
        if (coin != null && object4 != null) coin.SetParent(object4, true);
        yield return RotateAroundPivot(object4, rotationPivot4.position, Vector3.up, 180f, rotationDuration * 2f);

        // --- Objekt5 rotiert ---
        if (object5 != null && rotationPivot5 != null)
        {
            yield return RotateAroundPivot(
                object5,
                rotationPivot5.position,
                object5Axis,
                object5Angle,
                object5Duration
            );
        }

        // --- Objekt4: 90° Rotation ---
        yield return RotateAroundPivot(object4, rotationPivot4.position, Vector3.up, 90f, rotationDuration);

        // --- object4_1 & Coin-Drop ---
        if (object4_1 != null)
        {
            Vector3 start4 = object4_1.position;
            Vector3 right4 = start4 + object4_1.right * obj4_1Distance;
            if (coin != null) coin.SetParent(object4_1, true);
            yield return MoveOverTime(object4_1, start4, right4, obj4_1Duration);

            if (coinRb != null) coinRb.isKinematic = false;
            if (coin != null) coin.SetParent(null, true);

            yield return MoveOverTime(object4_1, right4, start4, obj4_1Duration);
        }

        // --- Rückrotation object4 (−270°) ---
        yield return new WaitForSeconds(rotationPause);
        yield return RotateAroundPivot(object4, rotationPivot4.position, Vector3.up, -270f, rotationDuration * 3f);

        Debug.Log("Komplette Sequenz abgeschlossen.");

        // Reset für nächsten Start
        sequenceStarted = false;
        coin = null;
        coinRb = null;
        Debug.Log("[MovementController] Bereit für nächsten Start.");
    }

    private IEnumerator MoveOverTime(Transform obj, Vector3 from, Vector3 to, float duration)
    {
        if (obj == null) yield break;
        float elapsed = 0f;
        while (elapsed < duration)
        {
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
        float elapsed = 0f;
        float sign = Mathf.Sign(angle);
        float total = Mathf.Abs(angle);
        while (elapsed < duration)
        {
            float step = sign * (total / duration) * Time.deltaTime;
            obj.RotateAround(pivot, axis.normalized, step);
            elapsed += Time.deltaTime;
            yield return null;
        }
        float remaining = total - (total / duration) * duration;
        if (Mathf.Abs(remaining) > 0.0001f)
            obj.RotateAround(pivot, axis.normalized, sign * remaining);
    }
}
