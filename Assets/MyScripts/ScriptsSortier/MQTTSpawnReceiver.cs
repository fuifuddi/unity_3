using M2MqttUnity;
using System.Text;
using UnityEngine;
using uPLibrary.Networking.M2Mqtt.Messages;

public class MQTTReceiver : M2MqttUnityClient
{
    [Header("Subscription Settings")]
    [Tooltip("Das Topic, auf das Unity hören soll")]
    public string subscribeTopic = "unity/test";

    [Header("Spawner Reference")]
    [Tooltip("Hier das GameObject mit CylinderSpawner-Skript reinziehen")]
    public CylinderSpawner spawner;

    protected override void Start()
    {
        // Broker konfigurieren
        brokerAddress = "127.0.0.1";
        brokerPort = 1883;
        autoConnect = true;

        Debug.Log("[MQTTReceiver] Starting and connecting to broker...");
        base.Start();
    }

    protected override void OnConnected()
    {
        Debug.Log("[MQTTReceiver] Connected to broker.");
        base.OnConnected();  // ruft SubscribeTopics() auf
    }

    protected override void SubscribeTopics()
    {
        Debug.Log($"[MQTTReceiver] Subscribing to '{subscribeTopic}'...");
        client.Subscribe(
            new string[] { subscribeTopic },
            new byte[] { MqttMsgBase.QOS_LEVEL_AT_LEAST_ONCE }
        );
        Debug.Log($"[MQTTReceiver] Subscribed to '{subscribeTopic}'.");
    }

    /// <summary>
    /// Hier wird jede eingehende Nachricht verarbeitet.
    /// </summary>
    protected override void DecodeMessage(string topic, byte[] message)
    {
        string msg = Encoding.UTF8.GetString(message);
        Debug.Log($"[MQTTReceiver] Message received on '{topic}': {msg}");

        // Nur auf unser subscribeTopic reagieren
        if (topic == subscribeTopic)
        {
            Debug.Log("[MQTTReceiver] Trigger spawn via MQTT.");
            if (spawner != null)
            {
                spawner.SpawnCylinder();
            }
            else
            {
                Debug.LogWarning("[MQTTReceiver] Kein Spawner zugewiesen!");
            }
        }
    }

    protected override void OnConnectionFailed(string errorMessage)
    {
        Debug.LogError("[MQTTReceiver] Connection failed: " + errorMessage);
    }

    protected override void OnDisconnected()
    {
        Debug.LogWarning("[MQTTReceiver] Disconnected from broker.");
    }

    protected override void OnConnectionLost()
    {
        Debug.LogWarning("[MQTTReceiver] Connection lost!");
    }

    private void OnDestroy()
    {
        // Sauber trennen, wenn Objekt zerstört wird
        Disconnect();
    }
}
