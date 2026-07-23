using UnityEditor;
using UnityEngine;

namespace DarkTonic.MasterAudio.EditorScripts
{
    [InitializeOnLoad]
    // ReSharper disable once CheckNamespace
    public class MasterAudioHierIcon : MonoBehaviour
    {
        static readonly Texture2D MAicon;
        static readonly Texture2D PCicon;

        static MasterAudioHierIcon()
        {
            MAicon = AssetDatabase.LoadAssetAtPath("Assets/Gizmos/MasterAudio/MasterAudio Icon.png", typeof(Texture2D)) as Texture2D;
            PCicon = AssetDatabase.LoadAssetAtPath("Assets/Gizmos/MasterAudio/PlaylistController Icon.png", typeof(Texture2D)) as Texture2D;

            if (MAicon == null)
            {
                return;
            }

#if UNITY_6000_4_OR_NEWER
            EditorApplication.hierarchyWindowItemByEntityIdOnGUI += HierarchyItemCB;
#else
            EditorApplication.hierarchyWindowItemOnGUI += HierarchyItemCB;
#endif
            EditorApplication.RepaintHierarchyWindow();
        }

        // ReSharper disable once InconsistentNaming
        static void HierarchyItemCB(
#if UNITY_6000_4_OR_NEWER
            EntityId instanceId,
#else
            int instanceId, 
#endif
            Rect selectionRect)
        {
#if UNITY_6000_4_OR_NEWER
            var masterAudioGameObject = EditorUtility.EntityIdToObject(instanceId) as GameObject;
#else
            var masterAudioGameObject = EditorUtility.InstanceIDToObject(instanceId) as GameObject;
#endif

            if (masterAudioGameObject == null)
            {
                return;
            }

            if (MAicon != null && masterAudioGameObject.GetComponent<MasterAudio>() != null)
            {
                var iconRect = new Rect(selectionRect);
                // Always position the hierarchy icon on the right no matter how deep the GameObject is within the hierarchy
                iconRect.x = iconRect.width + (selectionRect.x - 16);
                iconRect.width = 16;
                iconRect.height = 16;
                GUI.DrawTexture(iconRect, MAicon);
            }
            else if (PCicon != null && masterAudioGameObject.GetComponent<PlaylistController>() != null)
            {
                var iconRect = new Rect(selectionRect);
                // Always position the hierarchy icon on the right no matter how deep the GameObject is within the hierarchy
                iconRect.x = iconRect.width + (selectionRect.x - 16);
                iconRect.width = 16;
                iconRect.height = 16;
                GUI.DrawTexture(iconRect, PCicon);
            }
        }
    }
}