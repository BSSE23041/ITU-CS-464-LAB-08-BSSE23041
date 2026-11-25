using UnityEngine;

namespace CometRun.UI
{
    // Keeps the UI away from the notch and the rounded corners on newer phones.
    [RequireComponent(typeof(RectTransform))]
    public class SafeArea : MonoBehaviour
    {
        RectTransform rt;
        Rect lastArea;
        Vector2Int lastSize;

        void Awake()
        {
            rt = (RectTransform)transform;
            Apply();
        }

        void Update()
        {
            if (Screen.safeArea != lastArea || lastSize.x != Screen.width || lastSize.y != Screen.height)
                Apply();
        }

        void Apply()
        {
            lastArea = Screen.safeArea;
            lastSize = new Vector2Int(Screen.width, Screen.height);
            if (Screen.width <= 0 || Screen.height <= 0) return;

            Vector2 min = lastArea.position;
            Vector2 max = lastArea.position + lastArea.size;
            min.x /= Screen.width; min.y /= Screen.height;
            max.x /= Screen.width; max.y /= Screen.height;

            rt.anchorMin = min;
            rt.anchorMax = max;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }
    }
}
