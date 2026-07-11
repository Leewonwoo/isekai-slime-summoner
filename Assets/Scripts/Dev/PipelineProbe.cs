using UnityEngine;

/// <summary>
/// 빌드 파이프라인 검증용 프로브. 오브젝트를 회전시키고 화면에 기기 정보를 표시한다.
/// APK가 실기기에서 정상 렌더링되는지 확인한 뒤 삭제 예정.
/// </summary>
public class PipelineProbe : MonoBehaviour
{
    float _fps;

    void Update()
    {
        transform.Rotate(0f, 0f, 90f * Time.deltaTime);
        _fps = Mathf.Lerp(_fps, 1f / Mathf.Max(Time.unscaledDeltaTime, 0.0001f), 0.1f);
    }

    void OnGUI()
    {
        GUI.skin.label.fontSize = Screen.height / 40;
        GUILayout.BeginArea(new Rect(20, 20, Screen.width - 40, Screen.height - 40));
        GUILayout.Label("Cross Defense — pipeline probe");
        GUILayout.Label($"Screen: {Screen.width}x{Screen.height} ({Screen.orientation})");
        GUILayout.Label($"FPS: {_fps:F0}");
        GUILayout.Label($"Unity {Application.unityVersion} / {Application.platform}");
        GUILayout.EndArea();
    }
}
