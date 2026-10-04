using TMPro;
using UnityEngine;
using NocturneAnnex.Core;

namespace NocturneAnnex.UI
{
    /// <summary>Fills a TMP label from a Loc key whenever it is enabled, so views hold keys, not text.</summary>
    [RequireComponent(typeof(TMP_Text))]
    public class LocalizedText : MonoBehaviour
    {
        public string Key = "";

        void OnEnable() => Refresh();

        public void Refresh() => GetComponent<TMP_Text>().text = Loc.Get(Key);
    }
}
