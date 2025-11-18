using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class FillRound : MonoBehaviour
{
    float baseDistance;
    Vector3 initialFillScale;
    Vector3 initialFillLossyScale;
    Vector2 startDotBaseSize;
    Vector2 endDotBaseSize;
    [Range(0.01f, 2f)] public float dotSize = 0.2f;
    public Image ImgFill, ImgStartDot, ImgEndDot;
    public ImgsFillDynamic ImgsFD;
    private void Awake()
    {
        this.initialFillScale = this.ImgFill != null ? this.ImgFill.rectTransform.localScale : Vector3.one;
        this.initialFillLossyScale = this.ImgFill != null ? this.ImgFill.rectTransform.lossyScale : Vector3.one;
        float initialScaleX = Mathf.Max(0.0001f, this.initialFillLossyScale.x);
        this.baseDistance = Vector3.Distance(this.transform.position, this.ImgStartDot.transform.position) / initialScaleX;

        if (this.ImgStartDot != null) this.startDotBaseSize = this.ImgStartDot.rectTransform.sizeDelta;
        if (this.ImgEndDot != null) this.endDotBaseSize = this.ImgEndDot.rectTransform.sizeDelta;
    }

    public void SetFill(float _amount)
    {
        this.ImgFill.fillAmount = _amount;
        this.RefreshAngle();
    }

    void RefreshAngle()
    {
        float ratio = this.ImgsFD != null ? this.ImgsFD.Factor : this.ImgFill.fillAmount;
        Vector3 fillLossyScale = this.ImgFill != null ? this.ImgFill.rectTransform.lossyScale : Vector3.one;
        float scaleFactorX = Mathf.Max(0.0001f, fillLossyScale.x) / Mathf.Max(0.0001f, this.initialFillLossyScale.x);
        float scaledDistance = this.baseDistance * Mathf.Max(0.0001f, fillLossyScale.x);

        // Ensure dots' visual size scales with the fill image without double-scaling
        if (this.ImgStartDot != null)
        {
            this.ImgStartDot.rectTransform.localScale = Vector3.one;
            this.ImgStartDot.rectTransform.sizeDelta = this.startDotBaseSize * this.dotSize * 0.75f;
        }
        if (this.ImgEndDot != null)
        {
            this.ImgEndDot.rectTransform.localScale = Vector3.one;
            this.ImgEndDot.rectTransform.sizeDelta = this.endDotBaseSize * this.dotSize * 0.75f;
        }

        this.ImgStartDot.transform.localPosition = Vector3.zero;
        this.ImgStartDot.transform.rotation = Quaternion.identity;
        this.ImgStartDot.transform.Translate(0, scaledDistance, 0);

        this.ImgEndDot.transform.localPosition = Vector3.zero;
        this.ImgEndDot.transform.rotation = Quaternion.identity;
        this.ImgEndDot.transform.Rotate(0, 0, -this.GetAngle(ratio), Space.Self);
        this.ImgEndDot.transform.Translate(0, scaledDistance, 0, Space.Self);
    }

    float GetAngle(float _amount)
    {
        return _amount * 360F;
    }
}
