using SFB;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public enum BREAK_SENTENCE_MODE
{
    AUTOBREAK,
    MANUAL
}
public class Manager : MonoBehaviour
{

    public Canvas canvas;

    public RectTransform rootKeyFrames;
    public RectTransform rootTextFrames;
    public RectTransform handle;

    public GameObject autoParseModeBtn;
    public GameObject manualParseModeBtn;
    public GameObject prefabTimeKey;
    public GameObject prefabTimeWord;

    public TextMeshProUGUI audioPathDisplay;
    public TMP_InputField sentenceInput;
    public TMP_InputField breakSentenceDisplay;
    public ScrollRect scrollRect;



    public AudioSource audioSource;
    public AudioClip clip;



    public List<float> mocks;
    public Image audioImage;

    public BREAK_SENTENCE_MODE parseMode;


    public TMP_InputField scaleZoomInput;
    public string currentSentence;
    public bool isPlaying;
    public bool isPlayingAudioAndEditing;



    public List<TimeKey> keys;
    public List<TimeWord> words;
    public List<string> currentWordLists;
    public List<Color> currentSentenceColor;
    private int countIdx;
    private int idxKeyFrameSelected;
    [SerializeField]
    private int multiply = 300;
    // Start is called before the first frame update
    private void Start()
    {
        AudioClip monoaudio = clip;
        RectTransform rect = scrollRect.content;
        scaleZoomInput.text = (multiply / 300.0f).ToString();
        Debug.Log(SystemInfo.maxTextureSize);
        int textureWidth = (int)(clip.length * multiply);
        if (textureWidth > SystemInfo.maxTextureSize)
        {
            textureWidth = SystemInfo.maxTextureSize;
            Debug.Log("maxTextureSize: " + SystemInfo.maxTextureSize);
        }
        rect.sizeDelta = new Vector2(textureWidth, rect.sizeDelta.y);
        Texture2D texture = PaintWaveformSpectrum(monoaudio, 1, textureWidth, 200, Color.cyan);
        audioImage.sprite = Sprite.Create(texture, new Rect(0, 0, textureWidth, 200), new Vector2(0.5f, 0.5f));

        int i = 0;
        while (i + 1920 < rect.sizeDelta.x)
        {
            mocks.Add(i / rect.sizeDelta.x);
            i = i + 1920;
        }
        mocks.Add(1);

        Color color = Color.magenta;
        print(ColorUtility.ToHtmlStringRGBA(color));
        SetParseTextMode(BREAK_SENTENCE_MODE.AUTOBREAK);

    }

    public void OnSentenceTextEndEdit(string change)
    {
        SetSentence(sentenceInput.text);

    }

    public void SetSentence(string currentSentence)
    {
        this.currentSentence = currentSentence;
        if (parseMode == BREAK_SENTENCE_MODE.AUTOBREAK)
        {
            currentWordLists = Extension.AutoBreakText(currentSentence);
        }
        else
        {
            currentWordLists = Extension.ManualBreakText(currentSentence);
        }

        currentSentenceColor = Extension.CreateColor(currentWordLists);
        breakSentenceDisplay.text = Extension.RebuildColorText(currentWordLists, currentSentenceColor);
        Reset();
    }
    public void OnEndEditZoomScale(string text)
    {
        float.TryParse(text, out float zoomScale);
        multiply = (int)(300 * zoomScale);
        if (clip != null)
        {
            if (keys != null && keys.Count > 0 && currentWordLists != null && currentWordLists.Count > 0)
            {
                VoiceConfig data = new VoiceConfig()
                {
                    words = currentWordLists,
                    times = keys.Select(x => x.recttf.anchoredPosition.x / scrollRect.content.sizeDelta.x * clip.length).ToList()
                };
                LoadAudio(clip);
                Reset();
                LoadVoiceConfig(data);
            }
            else
            {
                LoadAudio(clip);
                Reset();
            }
        }

    }



    public void LoadVoiceConfig(VoiceConfig data)
    {
        countIdx = 0;
        for (int i = 0; i < data.times.Count; i++)
        {
            HandleAddKeyFrame(data.times[i]);

        }

    }

    public void HandleSave()
    {
        if (keys != null && keys.Count > 0 && currentWordLists != null && currentWordLists.Count > 0 && clip != null)
        {




            VoiceConfig data = new VoiceConfig()
            {
                words = currentWordLists,
                times = keys.Select(x => x.recttf.anchoredPosition.x / scrollRect.content.sizeDelta.x * clip.length).ToList()
            };

            string jsonString = JsonUtility.ToJson(data);
            string prevPath = PlayerPrefs.GetString("LAST_AUDIO_FILE_PATH", "");
            string dirname = "";
            string filename = "";
            if (!string.IsNullOrEmpty(prevPath))
            {
                FileInfo fileinfo = new FileInfo(prevPath);
                DirectoryInfo dir = fileinfo.Directory;
                dirname = dir.FullName;
                int lastDot = fileinfo.Name.LastIndexOf(".");
                filename = fileinfo.Name.Substring(0, lastDot);
                Debug.Log("fileName: " + filename);
            }
            string path = StandaloneFileBrowser.SaveFilePanel("Open File", dirname, filename, "json");
            if (!string.IsNullOrEmpty(path))
            {
                File.WriteAllText(path, jsonString);
            }
        }

    }



    public void OnBrowseAudio()
    {

        string[] paths = StandaloneFileBrowser.OpenFilePanel("Open File", PlayerPrefs.GetString("LAST_AUDIO_FOLDER_PATH", ""), "", false);
        if (paths != null && paths.Length > 0)
        {
            audioPathDisplay.text = paths[0];
            FileInfo fileinfo = new FileInfo(paths[0]);
            int lastDot = fileinfo.Name.LastIndexOf(".");
            string filename = fileinfo.Name.Substring(0, lastDot);
            PlayerPrefs.SetString("LAST_AUDIO_FOLDER_PATH", fileinfo.Directory.FullName);
            PlayerPrefs.SetString("LAST_AUDIO_FILE_PATH", fileinfo.FullName);
            Debug.Log("paths[0]:" + paths[0]);
            SetSentence(filename);
            AsyncLoadAudio(paths[0]);
        }
    }



    private void AsyncLoadAudio(string path)
    {

        byte[] bytes = File.ReadAllBytes(path);
        AudioClip monoaudio = NAudioPlayer.FromMp3Data(bytes);


        LoadAudio(monoaudio);


        Reset();

    }

    public void LoadAudio(AudioClip monoaudio)
    {
        Debug.Log(monoaudio.name + " " + monoaudio.length);
        RectTransform rect = scrollRect.content;



        Debug.Log(SystemInfo.maxTextureSize);
        int textureWidth = (int)(monoaudio.length * multiply);

        if (textureWidth > SystemInfo.maxTextureSize)
        {
            textureWidth = SystemInfo.maxTextureSize;
            multiply = (int)(textureWidth / monoaudio.length);
            Debug.Log("maxTextureSize: " + SystemInfo.maxTextureSize + " - texture width: " + textureWidth);
        }
        scaleZoomInput.text = (multiply / 300.0f).ToString();
        rect.sizeDelta = new Vector2(textureWidth, rect.sizeDelta.y);
        Texture2D texture = PaintWaveformSpectrum(monoaudio, 1, textureWidth, 200, Color.cyan);
        audioImage.sprite = Sprite.Create(texture, new Rect(0, 0, textureWidth, 200), new Vector2(0.5f, 0.5f));
        clip = monoaudio;
        audioSource.clip = clip;
        int i = 0;
        while (i + 1920 < rect.sizeDelta.x)
        {
            mocks.Add(i / rect.sizeDelta.x);
            i = i + 1920;
        }
        mocks.Add(1);
    }
    // Update is called once per frame
    private void Update()
    {
        if (!isPlaying && !sentenceInput.isFocused)
        {
            if (Input.GetKey(KeyCode.Delete))
            {

                if (idxKeyFrameSelected != -1 && idxKeyFrameSelected == keys.Count - 1)
                {
                    HandleDeleteKeyFrame(idxKeyFrameSelected);
                    idxKeyFrameSelected = -1;
                }

            }
            else if ((Input.GetKey(KeyCode.RightControl) || Input.GetKey(KeyCode.LeftControl)) && Input.GetKeyDown(KeyCode.S))
            {
                // CTRL + Z

                HandleSave();
            }
            else if ((Input.GetKey(KeyCode.RightControl) || Input.GetKey(KeyCode.LeftControl)) && Input.GetKeyDown(KeyCode.O))
            {
                OnBrowseAudio();
            }
            else if (Input.GetKeyDown(KeyCode.Space))
            {
                if (isPlaying)
                {
                    HandleClickStopBtn();
                }
                else
                {
                    HandlePlayAudioOnly();
                }

            }
            else if (Input.GetKeyDown(KeyCode.RightControl) || Input.GetKeyDown(KeyCode.LeftControl))
            {
                Debug.Log("OnControl");
                if (!isPlaying)
                {
                    HandleContinue();
                }


            }
            else if ((Input.GetKey(KeyCode.RightControl) || Input.GetKey(KeyCode.LeftControl)) && Input.GetKeyDown(KeyCode.R))
            {
                if (isPlaying)
                {
                    HandleClickStopBtn();
                }
                else
                {
                    HandleStart();
                }

            }
            else if ((Input.GetKey(KeyCode.RightControl) || Input.GetKey(KeyCode.LeftControl)) && Input.GetKeyDown(KeyCode.T))
            {
                if (isPlaying)
                {
                    HandleClickStopBtn();
                }
                else
                {
                    HandleContinue();
                }

            }
            else if (!sentenceInput.isFocused && Input.GetKeyDown(KeyCode.A))
            {
                SetAutoParseTextMode();
            }
            else if (!sentenceInput.isFocused && Input.GetKeyDown(KeyCode.M))
            {
                SetManualParseTextMode();
            }
        }
        else if (isPlaying)
        {

            {
                if (Input.GetKeyDown(KeyCode.Space) || !audioSource.isPlaying)
                {
                    Debug.Log("Stop");
                    HandleClickStopBtn();
                }

                if (Input.GetKeyDown(KeyCode.RightControl) || Input.GetKeyDown(KeyCode.LeftControl))
                {
                    Debug.Log("Get A");
                    if (isPlayingAudioAndEditing)
                    {

                        HandleAddKeyFrame(audioSource.time);
                    }

                }
                handle.anchoredPosition = new Vector2(Mathf.Clamp(audioSource.time / clip.length * scrollRect.content.sizeDelta.x, 0, scrollRect.content.sizeDelta.x), 0);
                float x = audioSource.time / clip.length * scrollRect.content.sizeDelta.x;

                float multiply = (int)(x / 1920);

                if (x > 960)
                {

                    scrollRect.content.anchoredPosition = new Vector2(-(x - 960), 0);

                }


            }
        }

    }

    public void HandleDragKeyFrame(TimeKey key)
    {
        Vector2 localPos;
        if (scrollRect.content.sizeDelta.x > 1920)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(scrollRect.content, Input.mousePosition, canvas.worldCamera, out localPos);
        }
        else
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(scrollRect.viewport, Input.mousePosition, canvas.worldCamera, out localPos);
        }


        float lastX = key.recttf.anchoredPosition.x;
        localPos.y = 0;
        float minX = 0;
        float maxX = scrollRect.content.sizeDelta.x;
        if (key.idx > 0)
        {
            minX = keys[key.idx - 1].recttf.anchoredPosition.x;
        }
        if (key.idx < keys.Count - 1)
        {
            maxX = keys[key.idx + 1].recttf.anchoredPosition.x;
        }

        localPos.x = Mathf.Clamp(localPos.x, minX, maxX);
        key.recttf.anchoredPosition = localPos;
        if (key.idx < keys.Count - 1)
        {

            TimeWord nextWord = words[key.idx + 1];
            Debug.Log(string.Format("key.idx: {0} - text: {1} - predict: {2}", key.idx, nextWord.text.text, nextWord.recttf.anchoredPosition.x + nextWord.recttf.sizeDelta.x - localPos.x));
            nextWord.recttf.sizeDelta = new Vector2(nextWord.recttf.anchoredPosition.x + nextWord.recttf.sizeDelta.x - localPos.x, nextWord.recttf.sizeDelta.y);
            nextWord.recttf.anchoredPosition = new Vector2(localPos.x, nextWord.recttf.anchoredPosition.y);

        }


        TimeWord currentWord = words[key.idx];
        Debug.Log(string.Format("key.idx: {0} - text: {1}", key.idx, currentWord.text.text));
        currentWord.recttf.sizeDelta = new Vector2(localPos.x - minX, currentWord.recttf.sizeDelta.y);
        currentWord.recttf.anchoredPosition = new Vector2(minX, currentWord.recttf.anchoredPosition.y);

    }

    public void HandleAddKeyFrame(float time)
    {
        GameObject go = null;
        TimeKey timekey = null;
        if (countIdx < currentWordLists.Count)
        {
            int idx = countIdx;
            if (keys.Count > idx && keys[idx] != null && keys[idx].gameObject != null)
            {

                timekey = keys[idx];
                go = timekey.gameObject;
            }
            else
            {
                go = Instantiate(prefabTimeKey);
                timekey = go.GetComponent<TimeKey>();
                keys.Add(timekey);
            }
            go.transform.SetParent(rootKeyFrames);
            go.GetComponent<RectTransform>().anchoredPosition = new Vector2(time / clip.length * scrollRect.content.sizeDelta.x, 0);

            timekey = go.GetComponent<TimeKey>();
            timekey.SetIdx(this, keys.Count - 1);
            AddWord(idx, time);

            Debug.Log("Add Key: " + keys.Count);
            countIdx += 1;
        }

    }

    public void AddWord(int idx, float time)
    {
        TimeWord word = null;
        GameObject go = null;
        if (words.Count > idx && words[idx] != null && words[idx].gameObject != null)
        {

            word = words[idx];
            go = word.gameObject;
        }
        else
        {
            go = Instantiate(prefabTimeWord);
            word = go.GetComponent<TimeWord>();
            words.Add(word);
        }

        go.transform.SetParent(rootTextFrames);
        RectTransform recttf = go.GetComponent<RectTransform>();
        if (idx == 0)
        {
            recttf.anchoredPosition = new Vector2(0, 0);

            recttf.sizeDelta = new Vector2(time / clip.length * scrollRect.content.sizeDelta.x, rootTextFrames.sizeDelta.y);
        }

        else
        {
            RectTransform prevRectTf = words[idx - 1].GetComponent<RectTransform>();

            go.GetComponent<RectTransform>().anchoredPosition = new Vector2(prevRectTf.anchoredPosition.x + prevRectTf.sizeDelta.x, 0);

            recttf.sizeDelta = new Vector2(time / clip.length * scrollRect.content.sizeDelta.x - (prevRectTf.anchoredPosition.x + prevRectTf.sizeDelta.x), rootTextFrames.sizeDelta.y);

        }
        go.transform.localScale = new Vector3(1, 1, 1);



        word.Setup(this, currentWordLists[idx], currentSentenceColor[idx]);

    }

    public void HandleDeleteKeyFrame(int idxKeyFrame)
    {

        {
            TimeWord timeWord = words[words.Count - 1];
            words.Remove(timeWord);
            Destroy(timeWord.gameObject);
        }
        {
            TimeKey temp = keys[idxKeyFrame];
            keys.Remove(temp);
            Destroy(temp.gameObject);

        }

    }

    public void Reset()
    {
        for (int i = 0; i < keys.Count; i++)
        {
            Destroy(keys[i].gameObject);
        }

        for (int i = 0; i < words.Count; i++)
        {
            Destroy(words[i].gameObject);
        }
        keys.Clear();
        words.Clear();
        handle.anchoredPosition = new Vector2(0, handle.anchoredPosition.y);
        idxKeyFrameSelected = -1;
        audioSource.Stop();
    }

    public void HandleClickKeyFrame(int idxKey)
    {
        if (idxKeyFrameSelected == -1)
        {
            idxKeyFrameSelected = idxKey;
            keys[idxKey].SetSelected(true);

        }
        else if (idxKeyFrameSelected == idxKey)
        {
            idxKeyFrameSelected = -1;
            keys[idxKey].SetSelected(false);
        }
        else
        {
            keys[idxKeyFrameSelected].SetSelected(false);
            idxKeyFrameSelected = idxKey;
            keys[idxKey].SetSelected(true);
        }
    }


    public void HandleStart()
    {
        Reset();
        countIdx = 0;
        audioSource.Stop();
        audioSource.Play();
        isPlaying = true;
        isPlayingAudioAndEditing = true;
        Debug.Log("Replay");
    }

    public void HandleContinue()
    {
        float currentTime = handle.anchoredPosition.x;
        countIdx = 0;
        for (int i = 0; i < keys.Count; i++)
        {
            if (keys[i].recttf.anchoredPosition.x < handle.anchoredPosition.x)
            {
                countIdx = i + 1;
            }
        }
        if (countIdx == keys.Count)
        {
            countIdx = 0;
        }
        if (audioSource.time > 0)
        {
            audioSource.Play();
        }
        else
        {
            audioSource.Play();
        }




        isPlaying = true;
        isPlayingAudioAndEditing = true;
        Debug.Log("Replay");
    }

    public void HandlePlayAudioOnly()
    {
        if (Mathf.Approximately(handle.anchoredPosition.x, scrollRect.content.sizeDelta.x))
        {
            audioSource.time = 0;
        }
        else
        {
            audioSource.time = Mathf.Clamp(handle.anchoredPosition.x * audioSource.clip.length / scrollRect.content.sizeDelta.x, 0, audioSource.clip.length);
        }

        audioSource.Play();
        isPlaying = true;
        isPlayingAudioAndEditing = false;
    }
    public void HandleClickStopBtn()
    {
        audioSource.Pause();
        isPlaying = false;
        isPlayingAudioAndEditing = false;
    }

    public AudioClip CloneAudioClip(AudioClip audioClip, string name)
    {
        AudioClip newAudioClip = AudioClip.Create(name, audioClip.samples, audioClip.channels, audioClip.frequency, false);
        float[] copyData = new float[audioClip.samples * audioClip.channels];
        audioClip.GetData(copyData, 0);

        List<float> monoData = new List<float>();
        for (int i = 0; i < copyData.Length; i += 2)
        {
            monoData.Add(copyData[i]);
        }
        newAudioClip.SetData(monoData.ToArray(), 0);

        return newAudioClip;
    }

    public Texture2D PaintWaveformSpectrum(AudioClip audio, float saturation, int width, int height, Color col)
    {
        Debug.Log("Texture Width: " + width);
        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        float[] samples = new float[audio.samples];
        float[] waveform = new float[width];
        audio.GetData(samples, 0);
        Debug.Log("Sample length: " + samples.Length);
        int packSize = (audio.samples / width) + 1;
        int s = 0;
        for (int i = 0; i < audio.samples; i += packSize)
        {
            waveform[s] = Mathf.Abs(samples[i]);
            s++;
        }

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                tex.SetPixel(x, y, Color.black);
            }
        }

        for (int x = 0; x < waveform.Length; x++)
        {
            for (int y = 0; y <= waveform[x] * (height * .75f); y++)
            {
                tex.SetPixel(x, (height / 2) + y, col);
                tex.SetPixel(x, (height / 2) - y, col);
            }
        }
        tex.Apply();

        return tex;
    }

    public void OnClickScrollView()
    {

        if (clip != null)
        {
            Vector2 localPos;
            if (scrollRect.content.sizeDelta.x > 1920)
            {
                RectTransformUtility.ScreenPointToLocalPointInRectangle(scrollRect.content, Input.mousePosition, canvas.worldCamera, out localPos);
            }
            else
            {
                RectTransformUtility.ScreenPointToLocalPointInRectangle(scrollRect.viewport, Input.mousePosition, canvas.worldCamera, out localPos);
            }
            localPos.y = handle.anchoredPosition.y;
            localPos.x = Mathf.Clamp(localPos.x, 0, scrollRect.content.sizeDelta.x);
            handle.anchoredPosition = localPos;
            audioSource.time = handle.anchoredPosition.x / scrollRect.content.sizeDelta.x * audioSource.clip.length;
        }


    }

    public void SetAutoParseTextMode()
    {
        SetParseTextMode(BREAK_SENTENCE_MODE.AUTOBREAK);
    }

    public void SetManualParseTextMode()
    {
        SetParseTextMode(BREAK_SENTENCE_MODE.MANUAL);
    }

    public void SetParseTextMode(BREAK_SENTENCE_MODE mode)
    {
        autoParseModeBtn.gameObject.SetActive(mode == BREAK_SENTENCE_MODE.AUTOBREAK);
        parseMode = mode;
        manualParseModeBtn.gameObject.SetActive(mode != BREAK_SENTENCE_MODE.AUTOBREAK);
        OnSentenceTextEndEdit("");
    }
}

[System.Serializable]
public class VoiceConfig
{
    public List<string> words;
    public List<float> times;
}