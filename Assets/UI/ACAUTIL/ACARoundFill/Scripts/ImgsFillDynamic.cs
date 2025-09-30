using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ImgsFillDynamic : MonoBehaviour
{
    float BeforeValue = 0F;
    float TargetValue = 0F;

    float speed = 1F;
    float nowTime = 0F;

    public Image[] ImgFacterTarget;
    public FillRound FillRound;
    float facter = 0F;
    public float Factor { get { return this.facter; } }

    public Color[] PercentByColor;

    public bool IsUsingMaxColor = false;
    public Color MaxColor;

    public Text TxtValue;
    public float MultifyText = 100F;
    public string TailText = "%";

		// Label shown above the circle
    public Text TxtLabel;
    public string LabelText = "Joint";
    public bool AutoPlaceLabelAbove = true;
    public float LabelMargin = 10f;

    void Start()
    {
        // Initialize label text on start
			this.SyncLabelText();
    }

		void OnEnable()
		{
			this.SyncLabelText();
		}

		#if UNITY_EDITOR
		void OnValidate()
		{
			// Keep label preview in sync in the editor
			this.SyncLabelText();
		}
		#endif

    /// <summary>
    /// USAGE:Set SetFillAmount
    /// </summary>
    /// <param name="_value">Target Value (0F ~ 1F)</param>
    /// <param name="_isDirectly">Is Directly Fill</param>
    /// <param name="_duringSpeed">Fill Speed 0.5(2sec), 2(0.5sec)</param>
    public void SetValue(float _value, bool _isDirectly = false, float _duringSpeed = 1F)
    {
        Mathf.Clamp01(_value);
        this.TargetValue = _value;

        if (_isDirectly)
        {
            this.nowTime = 1F;
            this.SetImageFillAmount(_value);
            this.SetImageColor();
            return;
        }
        else
        {
            this.nowTime = 0F;
            this.speed = _duringSpeed;
        }

    }

    void Update()
    {
        if (this.ImgFacterTarget != null && nowTime > -1F)
        {
            nowTime += Time.deltaTime * this.speed;
            this.facter = Mathf.Lerp(this.BeforeValue, this.TargetValue, nowTime);
            this.SetImageFillAmount(this.facter);
            this.BeforeValue = this.facter;
            if (this.FillRound != null)
                this.FillRound.SetFill(this.facter);

            if (nowTime < 0F)
            {
                nowTime = -1F;
                this.facter = this.TargetValue;
                this.SetImageFillAmount(this.facter);
                if (this.FillRound != null)
                    this.FillRound.SetFill(this.facter);
            }

            this.SetTextFactor();
            this.SetImageColor();
        }

			// Keep label text synced to LabelText
			this.SyncLabelText();

        // Keep label positioned above the circle if enabled
    }

    void SetTextFactor()
    {
        float textFactor = Mathf.Clamp01(this.facter) * this.MultifyText;
        if (this.TxtValue != null)
            this.TxtValue.text = string.Format("{0}{1}", textFactor.ToString("0"), this.TailText);
    }

    void SetImageFillAmount(float facter)
    {
        for (int i = 0; i < this.ImgFacterTarget.Length; i++)
            this.ImgFacterTarget[i].fillAmount = facter;
    }

    void SetImageColor()
    {
        if (this.PercentByColor.Length < 1) return;

        if (this.IsUsingMaxColor && this.facter > 0.99f)
        {
            for (int i = 0; i < this.ImgFacterTarget.Length; i++)
                this.ImgFacterTarget[i].color = MaxColor;
            return;
        }

        int arr = (int)Mathf.Lerp(0, this.PercentByColor.Length, this.facter);
        if (arr >= this.PercentByColor.Length)
            arr = this.PercentByColor.Length - 1;
        else if (arr < 0) arr = 0;
        for (int i = 0; i < this.ImgFacterTarget.Length; i++)
            this.ImgFacterTarget[i].color = this.PercentByColor[arr];
    }

    public void SetLabel(string label)
    {
        this.LabelText = label;
        if (this.TxtLabel != null)
            this.TxtLabel.text = this.LabelText;
    }

    void UpdateLabelPosition()
    {
        if (this.TxtLabel == null) return;
        if (this.ImgFacterTarget == null || this.ImgFacterTarget.Length == 0 || this.ImgFacterTarget[0] == null) return;

        RectTransform circle = this.ImgFacterTarget[0].rectTransform;
        RectTransform labelRT = this.TxtLabel.rectTransform;

        // Assume same parent and anchored positioning
        Vector2 newPos = labelRT.anchoredPosition;
        newPos.x = circle.anchoredPosition.x;
        newPos.y = circle.anchoredPosition.y + (circle.sizeDelta.y * 0.5f) + this.LabelMargin;
        labelRT.anchoredPosition = newPos;
    }

		void SyncLabelText()
		{
			if (this.TxtLabel == null) return;
			if (this.TxtLabel.text != this.LabelText)
				this.TxtLabel.text = this.LabelText;
		}
}
