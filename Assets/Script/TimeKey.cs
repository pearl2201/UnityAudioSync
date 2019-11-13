using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
public class TimeKey : MonoBehaviour
{
    public RectTransform recttf;
    public int idx;
    public GameObject selectedState;
    public GameObject deselectedState;
    public Manager manager;
    // Use this for initialization
    private void Start()
    {
        recttf = GetComponent<RectTransform>();
    }

    // Update is called once per frame
    private void Update()
    {

    }

    public void SetIdx(Manager manager, int idx)
    {
        this.idx = idx;
        this.manager = manager;
    }

    public void SetSelected(bool isSelected)
    {
        selectedState.gameObject.SetActive(isSelected);
        deselectedState.gameObject.SetActive(!isSelected);
    }
    public void OnClick()
    {
        manager.HandleClickKeyFrame(idx);
    }


    public void OnBeginDrag(BaseEventData evt)
    {


    }

    public void OnEndDrag(BaseEventData evt)
    {

    }

    public void OnDrag(BaseEventData evt)
    {
        manager.HandleDragKeyFrame(this);
    }
}
