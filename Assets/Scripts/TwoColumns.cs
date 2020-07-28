using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace Quatrain
{
    public class TwoColumns : MonoBehaviour
    {
        public void Show(string helpText)
        {
            ComputeLayout();
            SplitText(helpText);
            gameObject.SetActive(true);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        public Text LeftColumn, RightColumn;
        public int Split, Border;

        public void ComputeLayout()
        {
            if (!LeftColumn || !RightColumn)
            {
                Debug.LogError("No UI text.");
                return;
            }
            var r = GetComponent<RectTransform>();
            float W = r.GetWidth(), H = r.GetHeight();
            Debug.Log($"W={W},H={H}");
            var rL = LeftColumn.rectTransform;
            var rR = RightColumn.rectTransform;
            rL.SetWidth(W * (Split / 100f) - 1.5f * Border);
            rR.SetWidth(W * (1 - Split / 100f) - 1.5f * Border);
            rL.SetLeftTopPosition(new Vector2(- W / 2 + Border, H / 2 - Border));
            rR.SetRightTopPosition(new Vector2(W / 2 - Border, H / 2 - Border));
        }

        public void SplitText(string text)
        {
            if (!LeftColumn || !RightColumn)
            {
                Debug.LogError("No UI text.");
                return;
            }
            var left = new StringBuilder();
            var right = new StringBuilder();
            using (StringReader sr = new StringReader(text)) {
                string line;
                while ((line = sr.ReadLine()) != null)
                {
                    var lr = line.Split(':');
                    if (lr.Length != 2)
                    {
                        left.AppendLine(line);
                        right.AppendLine();
                    }
                    else
                    {
                        left.AppendLine(lr[0].TrimStart('-', ' ').TrimEnd('\\', 't'));
                        right.AppendLine(lr[1].TrimStart(' '));
                    }
                }
            }
            LeftColumn.text = left.ToString();
            RightColumn.text = right.ToString();
        }
    }

    public static class RectTransformExtensions
    {
        public static void SetDefaultScale(this RectTransform trans) {
            trans.localScale = new Vector3(1, 1, 1);
        }
        public static void SetPivotAndAnchors(this RectTransform trans, Vector2 aVec) {
            trans.pivot = aVec;
            trans.anchorMin = aVec;
            trans.anchorMax = aVec;
        }

        public static Vector2 GetSize(this RectTransform trans) {
            return trans.rect.size;
        }
        public static float GetWidth(this RectTransform trans) {
            return trans.rect.width;
        }
        public static float GetHeight(this RectTransform trans) {
            return trans.rect.height;
        }

        public static void SetPositionOfPivot(this RectTransform trans, Vector2 newPos) {
            trans.localPosition = new Vector3(newPos.x, newPos.y, trans.localPosition.z);
        }

        public static void SetLeftBottomPosition(this RectTransform trans, Vector2 newPos) {
            trans.localPosition = new Vector3(newPos.x + (trans.pivot.x * trans.rect.width), newPos.y + (trans.pivot.y * trans.rect.height), trans.localPosition.z);
        }
        public static void SetLeftTopPosition(this RectTransform trans, Vector2 newPos) {
            trans.localPosition = new Vector3(newPos.x + (trans.pivot.x * trans.rect.width), newPos.y - ((1f - trans.pivot.y) * trans.rect.height), trans.localPosition.z);
        }
        public static void SetRightBottomPosition(this RectTransform trans, Vector2 newPos) {
            trans.localPosition = new Vector3(newPos.x - ((1f - trans.pivot.x) * trans.rect.width), newPos.y + (trans.pivot.y * trans.rect.height), trans.localPosition.z);
        }
        public static void SetRightTopPosition(this RectTransform trans, Vector2 newPos) {
            trans.localPosition = new Vector3(newPos.x - ((1f - trans.pivot.x) * trans.rect.width), newPos.y - ((1f - trans.pivot.y) * trans.rect.height), trans.localPosition.z);
        }

        public static void SetSize(this RectTransform trans, Vector2 newSize) {
            Vector2 oldSize = trans.rect.size;
            Vector2 deltaSize = newSize - oldSize;
            trans.offsetMin = trans.offsetMin - new Vector2(deltaSize.x * trans.pivot.x, deltaSize.y * trans.pivot.y);
            trans.offsetMax = trans.offsetMax + new Vector2(deltaSize.x * (1f - trans.pivot.x), deltaSize.y * (1f - trans.pivot.y));
        }
        public static void SetWidth(this RectTransform trans, float newSize) {
            SetSize(trans, new Vector2(newSize, trans.rect.size.y));
        }
        public static void SetHeight(this RectTransform trans, float newSize) {
            SetSize(trans, new Vector2(trans.rect.size.x, newSize));
        }
    }
}