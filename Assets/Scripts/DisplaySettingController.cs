using UnityEngine;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// Hand Menu의 Display Setting 드롭다운 값에 따라 UI를 전환하는 컨트롤러
/// 모든 소환된 인스턴스의 UI를 동시에 업데이트합니다.
/// </summary>
public class DisplaySettingController : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("Overlay UI on the Robot (Implant 모드)")]
    public GameObject overlayUIOnRobot;
    
    [Tooltip("Vertical UI with Line (Outside 모드)")]
    public GameObject verticalUIWithLine;
    
    [Tooltip("Horizontal UI with Fixed Place (HUD 모드)")]
    public GameObject horizontalUIWithFixedPlace;
    
    [Header("Dropdown Reference")]
    [Tooltip("Display Setting 드롭다운 (자동으로 찾을 수 있음)")]
    public TMP_Dropdown displaySettingDropdown;
    
    // 정적 리스트로 모든 인스턴스 추적
    private static List<DisplaySettingController> s_AllInstances = new List<DisplaySettingController>();
    private static TMP_Dropdown s_SharedDropdown;
    private static int s_CurrentDisplayMode = 0;
    private static float s_LastSearchTime = 0f;
    private const float SEARCH_INTERVAL = 0.5f; // 0.5초마다 한 번씩만 찾기
    
    private void Awake()
    {
        // 인스턴스 리스트에 추가
        if (!s_AllInstances.Contains(this))
        {
            s_AllInstances.Add(this);
            Debug.Log($"DisplaySettingController: 새 인스턴스 등록됨 (총 {s_AllInstances.Count}개)");
        }
    }
    
    private void Start()
    {
        // 초기 드롭다운 찾기 시도
        TryFindAndConnectDropdown();
        
        // 이미 드롭다운이 설정되어 있으면 현재 값으로 즉시 업데이트
        if (s_SharedDropdown != null)
        {
            displaySettingDropdown = s_SharedDropdown;
            OnDisplaySettingChanged(s_CurrentDisplayMode);
            Debug.Log($"DisplaySettingController: 시작 시 현재 모드({s_CurrentDisplayMode})로 UI 설정");
        }
        else
        {
            // 드롭다운이 아직 없어도 기본값으로 설정
            OnDisplaySettingChanged(0); // 기본값: Implant
        }
    }
    
    private void Update()
    {
        // 드롭다운이 아직 연결되지 않았으면 주기적으로 찾기 시도
        if (s_SharedDropdown == null && Time.time - s_LastSearchTime > SEARCH_INTERVAL)
        {
            s_LastSearchTime = Time.time;
            TryFindAndConnectDropdown();
        }
    }
    
    /// <summary>
    /// Display Setting 드롭다운을 찾고 연결하는 메서드
    /// </summary>
    private void TryFindAndConnectDropdown()
    {
        // 이미 연결되어 있으면 리턴
        if (s_SharedDropdown != null)
            return;
        
        // 드롭다운이 없으면 자동으로 찾기
        if (displaySettingDropdown == null)
        {
            // 모든 드롭다운 찾기 (비활성화된 것도 포함)
            TMP_Dropdown[] allDropdowns = Resources.FindObjectsOfTypeAll<TMP_Dropdown>();
            
            foreach (var dropdown in allDropdowns)
            {
                // 드롭다운 옵션에서 "Implant", "Outside", "HUD" 확인
                bool isDisplaySettingDropdown = false;
                
                if (dropdown.options != null && dropdown.options.Count >= 3)
                {
                    var options = dropdown.options;
                    bool hasImplant = false, hasOutside = false, hasHUD = false;
                    
                    foreach (var option in options)
                    {
                        string text = option.text.ToLower();
                        if (text.Contains("implant"))
                            hasImplant = true;
                        if (text.Contains("outside"))
                            hasOutside = true;
                        if (text.Contains("hud"))
                            hasHUD = true;
                    }
                    
                    if (hasImplant && hasOutside && hasHUD)
                    {
                        isDisplaySettingDropdown = true;
                    }
                }
                
                // 이름이나 캡션 텍스트에서도 확인
                if (!isDisplaySettingDropdown)
                {
                    if (dropdown.name.Contains("Display") || 
                        (dropdown.captionText != null && dropdown.captionText.text.Contains("Display Setting")))
                    {
                        isDisplaySettingDropdown = true;
                    }
                }
                
                if (isDisplaySettingDropdown)
                {
                    displaySettingDropdown = dropdown;
                    Debug.Log($"DisplaySettingController: Display Setting 드롭다운을 찾았습니다: {dropdown.name}");
                    break;
                }
            }
        }
        
        // 공유 드롭다운 설정 (첫 번째 인스턴스만 이벤트 연결)
        if (s_SharedDropdown == null && displaySettingDropdown != null)
        {
            s_SharedDropdown = displaySettingDropdown;
            s_SharedDropdown.onValueChanged.AddListener(OnGlobalDisplaySettingChanged);
            // 초기값 설정
            s_CurrentDisplayMode = s_SharedDropdown.value;
            Debug.Log($"DisplaySettingController: 드롭다운 연결 완료. 현재 값: {s_CurrentDisplayMode}");
            OnGlobalDisplaySettingChanged(s_CurrentDisplayMode);
        }
    }
    
    private void OnDestroy()
    {
        // 인스턴스 리스트에서 제거
        s_AllInstances.Remove(this);
        
        // 마지막 인스턴스가 파괴되면 이벤트 해제
        if (s_AllInstances.Count == 0 && s_SharedDropdown != null)
        {
            s_SharedDropdown.onValueChanged.RemoveListener(OnGlobalDisplaySettingChanged);
            s_SharedDropdown = null;
        }
    }
    
    /// <summary>
    /// 전역 드롭다운 변경 이벤트 핸들러 - 모든 인스턴스에 알림
    /// </summary>
    private static void OnGlobalDisplaySettingChanged(int value)
    {
        s_CurrentDisplayMode = value;
        Debug.Log($"DisplaySettingController: 전역 드롭다운 값 변경됨: {value} (인스턴스 수: {s_AllInstances.Count})");
        
        // 모든 인스턴스에 변경 사항 적용
        int updatedCount = 0;
        foreach (var instance in s_AllInstances)
        {
            if (instance != null)
            {
                instance.OnDisplaySettingChanged(value);
                updatedCount++;
            }
        }
        
        Debug.Log($"DisplaySettingController: {updatedCount}개의 인스턴스 업데이트 완료");
    }
    
    /// <summary>
    /// Display Setting 드롭다운 값이 변경될 때 호출됨
    /// 0: Implant (Default) -> Overlay UI on the Robot
    /// 1: Outside -> Vertical UI with Line
    /// 2: HUD -> Horizontal UI with Fixed Place
    /// </summary>
    public void OnDisplaySettingChanged(int value)
    {
        // 모든 UI 비활성화
        if (overlayUIOnRobot != null)
            overlayUIOnRobot.SetActive(false);
        
        if (verticalUIWithLine != null)
            verticalUIWithLine.SetActive(false);
        
        if (horizontalUIWithFixedPlace != null)
            horizontalUIWithFixedPlace.SetActive(false);
        
        // 선택된 UI만 활성화
        switch (value)
        {
            case 0: // Implant (Default)
                if (overlayUIOnRobot != null)
                {
                    overlayUIOnRobot.SetActive(true);
                    Debug.Log("Display Setting: Implant (Default) - Overlay UI on the Robot 활성화");
                }
                break;
                
            case 1: // Outside
                if (verticalUIWithLine != null)
                {
                    verticalUIWithLine.SetActive(true);
                    Debug.Log("Display Setting: Outside - Vertical UI with Line 활성화");
                }
                break;
                
            case 2: // HUD
                if (horizontalUIWithFixedPlace != null)
                {
                    horizontalUIWithFixedPlace.SetActive(true);
                    Debug.Log("Display Setting: HUD - Horizontal UI with Fixed Place 활성화");
                }
                break;
                
            default:
                Debug.LogWarning($"DisplaySettingController: 알 수 없는 드롭다운 값: {value}");
                break;
        }
    }
    
    /// <summary>
    /// 외부에서 직접 UI를 전환할 수 있는 메서드
    /// </summary>
    public void SetDisplayMode(int mode)
    {
        if (displaySettingDropdown != null)
        {
            displaySettingDropdown.value = mode;
        }
        else
        {
            OnDisplaySettingChanged(mode);
        }
    }
}

