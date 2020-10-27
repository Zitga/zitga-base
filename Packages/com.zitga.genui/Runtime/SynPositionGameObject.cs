using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace GenUI.Extension
{
    public static class SynPositionGameObject
    {
        [MenuItem("GameObject/Extension/Syn Position", false, 0)]
        public static void SynPosition()
        {
            var selected = Selection.gameObjects;
            Transform childTransform = null;
            Transform parentTransform = null;
            if (selected.Length == 1)
            {
                childTransform = selected[0].transform;
                if (childTransform != null)
                {
                    parentTransform = childTransform.parent;
                }
            }
            else if (selected.Length == 2)
            {
                Transform child = selected[0].transform;
                Transform parent = selected[1].transform;
                int loop = 0;
                while (loop < 2)
                {
                    if (child.parent != null)
                    {
                        if (child.parent != parent)
                        {
                            child = child.parent;
                        }
                        else
                        {
                            if (loop == 0)
                            {
                                childTransform = selected[0].transform;
                                parentTransform = selected[1].transform;
                            }
                            else
                            {
                                childTransform = selected[1].transform;
                                parentTransform = selected[0].transform;
                            }
                            break;
                        }
                    }
                    else
                    {
                        child = selected[1].transform;
                        parent = selected[0].transform;
                        loop++;
                    }
                }
            }
            if (childTransform != null && parentTransform != null)
            {
                Vector3 dri = childTransform.position - parentTransform.position;
                Undo.RecordObject(parentTransform, "SynPositon");
                parentTransform.position += dri;
                EditorUtility.SetDirty(parentTransform);
                for (int i = 0; i < parentTransform.childCount; i++)
                {
                    Transform child = parentTransform.GetChild(i);
                    Undo.RecordObject(child, "SynPositon");
                    child.position -= dri;
                    EditorUtility.SetDirty(child);
                }
            }
        }

        [MenuItem("GameObject/Extension/Remove All Raycast")]
        public static void RemoveAllRaycast()
        {
            var selected = Selection.gameObjects;
            if (selected.Length == 1)
            {
                Graphic[] childs = selected[0].GetComponentsInChildren<Graphic>();
                for (int i = 0; i < childs.Length; i++)
                {
                    Graphic graphic = childs[i];
                    if (graphic != null)
                    {
                        Undo.RecordObject(graphic, "RemoveRaycast");
                        graphic.raycastTarget = false;
                    }
                }
            }
        }
    }
}