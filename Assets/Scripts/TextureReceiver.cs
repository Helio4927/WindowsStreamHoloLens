  using UnityEngine;
using Photon.Pun;
using System.IO;
using System.IO.Compression;
using ExitGames.Client.Photon;
using Photon.Realtime; 

public class TextureReceiver : MonoBehaviourPunCallbacks, IOnEventCallback{
    public enum RGBTextureFormat {
        RGB24,
        RGB565,
        RGBA32,
        ARGB32,
        RGB48,
        RGBA64,
    }

    [SerializeField] private Material targetMaterial;
    [SerializeField] private Canvas canvas;
    [SerializeField] private TMPro.TextMeshProUGUI packetsPerSecondText;
    [SerializeField] private int packetCount = 0; 
    private float elapsedTime = 0.0f;
    [SerializeField] public RGBTextureFormat rgbTextureFormat = RGBTextureFormat.RGB24;
    private Texture2D receivedTexture;

    private const int TEXTURE_WIDTH = 756;
    private const int TEXTURE_HEIGHT = 425;
    private const byte TEXTURE_STREAM_EVENT = 1;

    private void Awake(){
        if (targetMaterial == null) {
            Debug.LogError("Target Material is not assigned in TextureReceiver.");
            enabled = false;
            return;
        }

        TextureFormat unityTextureFormat = ConvertToUnityTextureFormat(rgbTextureFormat);
        receivedTexture = new Texture2D(TEXTURE_WIDTH, TEXTURE_HEIGHT, unityTextureFormat, false);
    }

    private TextureFormat ConvertToUnityTextureFormat(RGBTextureFormat format) {
        switch (format) {
            case RGBTextureFormat.RGB24:
                return TextureFormat.RGB24;
            case RGBTextureFormat.RGB565:
                return TextureFormat.RGB565;
            case RGBTextureFormat.RGBA32:
                return TextureFormat.RGBA32;
            case RGBTextureFormat.ARGB32:
                return TextureFormat.ARGB32;
            case RGBTextureFormat.RGB48:
                return TextureFormat.RGBAFloat;
            case RGBTextureFormat.RGBA64:
                return TextureFormat.RGBAHalf;
            default:
                return TextureFormat.RGB24;
        }
    }

    private void Start(){
        PhotonNetwork.AddCallbackTarget(this);
    }

    private void Update(){
        elapsedTime += Time.deltaTime;

        if (elapsedTime >= 1.0f){
            int packetsPerSecond = packetCount;

            if (packetsPerSecondText != null)
                packetsPerSecondText.text = "Packets/sec: " + packetsPerSecond;
            
            packetCount = 0;
            elapsedTime = 0;
        }
    }


    private void OnDestroy(){
        PhotonNetwork.RemoveCallbackTarget(this);
    }

    public void OnEvent(EventData photonEvent){
        if (photonEvent.Code == TEXTURE_STREAM_EVENT){
            packetCount++;

            ReceiveTexture((byte[])photonEvent.CustomData);
        }
            
    }

    public void ReceiveTexture(byte[] compressedBytes){
        if (compressedBytes == null || compressedBytes.Length == 0){
            Debug.LogError("No texture data received, or the data is corrupt.");
            return;
        }

        byte[] decompressedBytes;
        using (var inputStream = new MemoryStream(compressedBytes)){
            using var outputStream = new MemoryStream();
            using (var zipStream = new GZipStream(inputStream, CompressionMode.Decompress))
                zipStream.CopyTo(outputStream);
            
            decompressedBytes = outputStream.ToArray();
        }

        Debug.Log("Received and decompressed bytes: " + decompressedBytes.Length + ". Original size: " + compressedBytes.Length + ".");

        if (receivedTexture.LoadImage(decompressedBytes)){
            targetMaterial.mainTexture = receivedTexture;
            Debug.Log("Texture successfully applied.");
        }
        else
            Debug.LogError("Failed to apply the texture.");
    }
}