using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class TorqueUIPair
{
	[Header("Joint and UI")]
	[Tooltip("The articulation body joint to read torque from")] public ArticulationBody joint;
	[Tooltip("UI widget that receives a 0..1 value via SetValue")] public ImgsFillDynamic targetUI;
	[Tooltip("Label to display on the UI widget")] public string label = "Joint";

	[Header("Normalization")] 
	[Tooltip("Use absolute torque magnitude")] public bool useAbsoluteValue = true;
	[Tooltip("Prefer normalization by xDrive.forceLimit if > 0")] public bool normalizeByForceLimit = true;
	[Tooltip("Fallback/custom max torque (N·m) when forceLimit is 0")] public float customMaxTorqueNm = 50f;
	[Tooltip("Additional multiplier applied to the normalized value")] public float customMultiplier = 1f;

	[Header("Debug")] public bool showDebug = false;

	[HideInInspector] public float currentTorqueNm;
	[HideInInspector] public float currentRatio;
}

public class JointTorqueToUIManager : MonoBehaviour
{
	[Header("Data Source")]
	[Tooltip("Provider that samples torque for all joints. If null, will search in parents.")]
	public ReadAllJointTorques torqueProvider;

	[Header("Joint/UI Pairs")] 
	public List<TorqueUIPair> jointUIPairs = new List<TorqueUIPair>();

	[Header("Update")]
	[Tooltip("UI update frequency (Hz)")] public float updatesPerSecond = 30f;
	[Tooltip("Use smoothed torque from provider; disable to use instantaneous torque")] public bool useSmoothedTorque = true;

	[Header("Accumulation (optional)")]
	public bool accumulateValues = false;
	public ImgsFillDynamic accumulatedTargetUI;
	public string accumulatedLabel = "Combined";

	[Header("Debug")] public bool showDebugInfo = false;

	private float _accum;

	void Awake()
	{
		if (!torqueProvider)
		{
			// Prefer parent provider to avoid multiple scanners
			torqueProvider = GetComponentInParent<ReadAllJointTorques>();
		}
	}

	void Start()
	{
		// Initialize UI labels
		foreach (var pair in jointUIPairs)
		{
			if (pair.targetUI != null && !string.IsNullOrEmpty(pair.label))
			{
				pair.targetUI.SetLabel(pair.label);
			}
		}

		if (accumulateValues && accumulatedTargetUI != null && !string.IsNullOrEmpty(accumulatedLabel))
		{
			accumulatedTargetUI.SetLabel(accumulatedLabel);
		}
	}

	void Update()
	{
		if (jointUIPairs.Count == 0) return;

		_accum += Time.deltaTime;
		float interval = Mathf.Max(0.01f, 1f / Mathf.Max(1f, updatesPerSecond));
		if (_accum < interval) return;
		_accum = 0f;

		float total = 0f;
		float totalWeight = 0f; // we can extend to per-pair weight later

		for (int i = 0; i < jointUIPairs.Count; i++)
		{
			var pair = jointUIPairs[i];
			if (!pair.joint || pair.targetUI == null) continue;

			float torqueNm = ReadTorqueNm(pair.joint);
			pair.currentTorqueNm = pair.useAbsoluteValue ? Mathf.Abs(torqueNm) : torqueNm;

			float denom = 0f;
			if (pair.normalizeByForceLimit)
			{
				float fl = pair.joint.xDrive.forceLimit; // for revolute: torque limit
				if (fl > 0f) denom = fl;
			}
			if (denom <= 0f)
			{
				denom = Mathf.Max(1e-3f, pair.customMaxTorqueNm);
			}

			float ratio = denom > 0f ? pair.currentTorqueNm / denom : 0f;
			ratio = Mathf.Clamp01(ratio * pair.customMultiplier);
			pair.currentRatio = ratio;

			if (!accumulateValues)
			{
				pair.targetUI.SetValue(ratio, _isDirectly: true);
			}
			else
			{
				total += ratio;
				totalWeight += 1f;
			}

			if (pair.showDebug || showDebugInfo)
			{
				Debug.Log($"{pair.label}: torque={pair.currentTorqueNm:F2} N·m, ratio={pair.currentRatio:F3}");
			}
		}

		if (accumulateValues && accumulatedTargetUI)
		{
			float avg = totalWeight > 0f ? total / totalWeight : 0f;
			accumulatedTargetUI.SetValue(Mathf.Clamp01(avg), _isDirectly: true);
		}
	}

	private float ReadTorqueNm(ArticulationBody joint)
	{
		if (!joint) return 0f;
		if (torqueProvider)
		{
			return useSmoothedTorque ? torqueProvider.GetTorqueNm(joint) : torqueProvider.ReadInstantTorqueNm(joint);
		}
		// Fallback: direct read if provider is absent
		var jf = joint.jointForce;
		return jf.dofCount > 0 ? jf[0] : 0f;
	}

	// Helpers for runtime control
	public void AddPair(ArticulationBody joint, ImgsFillDynamic ui, string label = "Joint")
	{
		jointUIPairs.Add(new TorqueUIPair { joint = joint, targetUI = ui, label = label });
		if (ui && !string.IsNullOrEmpty(label)) ui.SetLabel(label);
	}

	public void ClearPairs() => jointUIPairs.Clear();
}



