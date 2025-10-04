using UnityEngine;

[ExecuteInEditMode]
public class PrefabInitializer : MonoBehaviour
{
    [SerializeField] private int count;
    [SerializeField] private RectTransform content;
    [SerializeField] private GameObject prefab;
    [SerializeField] private bool update;
    [SerializeField] private bool delete;

    private void Update()
    {
        if (delete)
        {
            delete = false;

            foreach (Transform go in content)
            {
                DestroyImmediate(go.gameObject);
            }
        }
        if (update)
        {
            update = false;

            foreach (Transform go in content)
            {
                DestroyImmediate(go.gameObject);
            }

            for (var i = 0; i < count; i++)
            {
                var go = Instantiate(prefab, content);
                go.GetComponent<DemoCellPrototype>().Initialize(i.ToString());
            }
        }
    }
}
