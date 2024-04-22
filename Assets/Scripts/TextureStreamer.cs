using UnityEngine;
using Photon.Pun;
using System.IO;
using System.IO.Compression;
using Photon.Realtime;
using ExitGames.Client.Photon;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;

public class TextureStreamer : MonoBehaviourPunCallbacks {
    
    public class TexturePool {
        public bool useTexturePool = false;
        private readonly Queue<Texture2D> availableTextures = new Queue<Texture2D>();

        public Texture2D GetTexture(int width, int height, TextureFormat format) {
            if (!useTexturePool) {
                return new Texture2D(width, height, format, false); // Si el pooling está desactivado, siempre crea una nueva textura
            }

            foreach(var tex in availableTextures) {
                if (tex.width == width && tex.height == height && tex.format == format) {
                    return availableTextures.Dequeue();
                }
            }
            return new Texture2D(width, height, format, false);
        }

        public void ReturnTexture(Texture2D texture) {
            if (!useTexturePool || texture == null) {
                if (texture != null) {
                    Destroy(texture);
                }
            } else {
                availableTextures.Enqueue(texture);  
            }
        }
    }

    public enum RGBTextureFormat {
        RGB24,
        RGB565,
        RGBA32,
        ARGB32,
        RGB48,
        RGBA64,
    }

    private readonly TexturePool texturePool = new TexturePool();
    private readonly Dictionary<string, Texture2D> textureDictionary = new Dictionary<string, Texture2D>();
    private readonly Dictionary<Texture2D, int> referenceCount = new Dictionary<Texture2D, int>();

    public enum FrameRatePreset {_01FPS, _05FPS ,_10FPS, _15FPS, _20FPS, _24FPS, _30FPS, _60FPS, }

    [Header("Setup")]
    public Camera captureCamera;
    public int desiredResolutionHeight = 500;

    [Range(0, 100)]
    public int compression = 35;
    public FrameRatePreset frameRate = FrameRatePreset._10FPS;
    public RGBTextureFormat rgbTextureFormat = RGBTextureFormat.RGB24;

    private int textureWidth;
    private int textureHeight;
    private float waitTime;

    private const int RENDER_TEXTURE_DEPTH = 16;
    private const byte TEXTURE_STREAM_EVENT = 1;

    private RenderTexture renderTexture;
    private bool presetsApplied = false;
    private bool isInitialized = false;
    private bool isStreaming = true;
    private MemoryStream memoryStream = new MemoryStream();
    private GZipStream zipStream;
    private bool disposed = false;

    private void Awake() {
        if (!ValidateComponents() || !IsEligibleForStreaming()) {
            enabled = false;
            return;
        }

        memoryStream = new MemoryStream();
        zipStream = new GZipStream(memoryStream, CompressionMode.Compress, true);

        ApplyPresets();
        InitializeTextureStreaming();
        isInitialized = true;

        PhotonNetwork.AddCallbackTarget(this);
    }

    private void Start() {
        if (isInitialized) {
            StartCoroutine(CaptureAndSendTexture());
        }
    }

    private void OnDestroy() {
        PhotonNetwork.RemoveCallbackTarget(this);
        StopCoroutine(CaptureAndSendTexture());
        Dispose();
    }

    private void InitializeTextureStreaming() {
        if (renderTexture == null || renderTexture.width != textureWidth || renderTexture.height != textureHeight) {
            if (renderTexture != null)
                renderTexture.Release();

            renderTexture = new RenderTexture(textureWidth, textureHeight, RENDER_TEXTURE_DEPTH, RenderTextureFormat.ARGB32);
        }
        captureCamera.targetTexture = renderTexture;
    }

    private IEnumerator CaptureAndSendTexture() {
        WaitForSeconds wait = new (waitTime);
        TextureFormat unityTextureFormat = ConvertToUnityTextureFormat(rgbTextureFormat);

        while (isStreaming) {
            yield return new WaitForEndOfFrame();

            Texture2D texture2D = texturePool.GetTexture(renderTexture.width, renderTexture.height, unityTextureFormat);

            RenderTexture prevActive = RenderTexture.active;
            RenderTexture.active = renderTexture;

            texture2D.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
            texture2D.Apply();

            RenderTexture.active = prevActive;

            yield return StartCoroutine(EncodeTextureCoroutine(texture2D, (textureBytes) => {
                memoryStream.SetLength(0);
                memoryStream.Position = 0;

                using (GZipStream localZipStream = new (memoryStream, CompressionMode.Compress, true)) {
                    localZipStream.Write(textureBytes, 0, textureBytes.Length);
                }

                byte[] compressedData = memoryStream.ToArray();

                try {
                    PhotonNetwork.RaiseEvent(TEXTURE_STREAM_EVENT, compressedData, new RaiseEventOptions { Receivers = ReceiverGroup.Others }, SendOptions.SendReliable);
                } catch (Exception ex) {
                    Debug.LogError("Error sending texture data: " + ex.Message);
                }

                Debug.Log($"Sending. Final Size: {compressedData.Length}. JPG Size: {textureBytes.Length}. GZIP Size: {memoryStream.Length}");

                texturePool.ReturnTexture(texture2D);
            }));

            yield return wait;
        }
    }

    private IEnumerator EncodeTextureCoroutine(Texture2D texture, Action<byte[]> onEncoded) {
        byte[] textureBytes = texture.EncodeToJPG(compression);
        onEncoded(textureBytes);
        yield return null;
    }


    public void Dispose() {
        if (!this.disposed) {
            if (zipStream != null) {
                zipStream.Dispose();
                zipStream = null;
            }

            if (memoryStream != null) {
                memoryStream.Dispose();
                memoryStream = null;
            }

            disposed = true;

            foreach (var texture in textureDictionary.Values) {
                if (texture != null) {
                    Destroy(texture);
                }
            }
            textureDictionary.Clear();
            referenceCount.Clear();
        }
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

    private bool ValidateComponents() {
        if (captureCamera == null) {
            Debug.LogError("Capture Camera not assigned in TextureStreamer.");
            return false;
        }

        if (photonView == null) {
            Debug.LogError("PhotonView not found on this component.");
            return false;
        }

        return true;
    }

    private bool IsEligibleForStreaming() {
        if (!PhotonNetwork.IsConnected || !PhotonNetwork.IsMasterClient) {
            Debug.LogWarning("TextureStreamer is disabled as it's not connected or not the Master Client.");
            return false;
        }
        return true;
    }

    private async Task<Texture2D> GetTextureAsync(int width, int height, TextureFormat format) {
        string key = $"{width}_{height}_{format}";
        Texture2D texture;
        if (textureDictionary.TryGetValue(key, out texture) && referenceCount[texture] == 0) {
            referenceCount[texture]++;
            return texture;
        }

        await Task.Yield();
        texture = new Texture2D(width, height, format, false);
        textureDictionary[key] = texture;
        referenceCount[texture] = 1;
        return texture;
    }

    private void ReturnTexture(Texture2D texture) {
        if (texture != null) {
            referenceCount[texture]--;
            if (referenceCount[texture] == 0) {
                StartCoroutine(GradualDeallocation(texture));
            }
        }
    }

    private IEnumerator GradualDeallocation(Texture2D texture) {
        yield return new WaitForSeconds(30);
        if (referenceCount[texture] == 0) {
            Destroy(texture);
        }
    }

    private void ApplyPresets() {
        if (presetsApplied) return;

        textureWidth = (int)(desiredResolutionHeight * 16.0f / 9.0f);
        textureHeight = desiredResolutionHeight;

        switch (frameRate) {
            case FrameRatePreset._01FPS:
                waitTime = 1.0f / 1.0f;
                break;
            case FrameRatePreset._05FPS:
                waitTime = 1.0f / 5.0f;
                break;
            case FrameRatePreset._10FPS:
                waitTime = 1.0f / 10.0f;
                break;
            case FrameRatePreset._15FPS:
                waitTime = 1.0f / 15.0f;
                break;
            case FrameRatePreset._20FPS:
                waitTime = 1.0f / 20.0f;
                break;
            case FrameRatePreset._24FPS:
                waitTime = 1.0f / 24.0f;
                break;
            case FrameRatePreset._30FPS:
                waitTime = 1.0f / 30.0f;
                break;
            case FrameRatePreset._60FPS:
                waitTime = 1.0f / 60.0f;
                break;
        }

        presetsApplied = true;
    }
}