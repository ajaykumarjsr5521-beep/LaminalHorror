using UnityEngine;

namespace NocturneAnnex.Controls
{
    /// <summary>Scales a group of touch buttons by the Touch control size setting. Anchor the group at a screen corner so it grows inward.</summary>
    public class TouchControlsScaler : MonoBehaviour
    {
        float _applied = -1f;

        void OnEnable() => Apply();
        void Update() => Apply();

        void Apply()
        {
            float scale = ControlsLayout.Scale;
            if (Mathf.Approximately(scale, _applied)) return;
            _applied = scale;
            transform.localScale = new Vector3(scale, scale, 1f);
        }
    }
}
