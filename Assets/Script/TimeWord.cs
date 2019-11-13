using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using TMPro;
public class TimeWord : MonoBehaviour
{
    public Image image;
    public TextMeshProUGUI text;
    public RectTransform recttf;
    public Manager manager;
    private void Awake()
    {
        recttf = GetComponent<RectTransform>();
    }
    // Use this for initialization
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }

    public void Setup(Manager manager, string word, Color color)
    {
        this.manager = manager;
        text.text = word;
        image.color = color;
    }
}
