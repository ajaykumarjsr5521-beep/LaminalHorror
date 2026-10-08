using UnityEngine;

namespace NocturneAnnex.Entity
{
    /// <summary>Greybox arm swing: both shoulders pivot from hanging (0) to raised overhead (1). Driven by StrikeModel.Raise.</summary>
    public class StalkerArms : MonoBehaviour
    {
        public Transform ShoulderL, ShoulderR;
        public float MaxAngle = 165f;

        public float Raise { get; private set; }

        public void Pose(float raise)
        {
            Raise = Mathf.Clamp01(raise);
            var q = Quaternion.Euler(-MaxAngle * Raise, 0f, 0f);   // negative X swings the hanging arm forward and up
            if (ShoulderL != null) ShoulderL.localRotation = q;
            if (ShoulderR != null) ShoulderR.localRotation = q;
        }
    }
}
