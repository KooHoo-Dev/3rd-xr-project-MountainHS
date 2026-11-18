using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Simulation;

namespace StudyXR.Strike
{
    /// <summary>
    /// 플레이 시작 때 XR Interaction Simulator가 몸(FPS)만 조종하게 한다.
    /// 시뮬레이터는 Awake에서 시작 대상을 FPS + 컨트롤러로 고정하므로, 그 뒤에 도는 Start에서 FPS만 남긴다.
    /// </summary>
    /// <remarks>
    /// 시뮬레이터는 AfterSceneLoad 로더가 만든다. 씬 오브젝트의 Start는 그 이후에 돌기 때문에 instance가 이미 있다.
    /// 컨트롤러 전환은 시뮬레이터의 기본 키 그대로 동작한다.
    /// </remarks>
    public class SimulatorBodyStart : MonoBehaviour
    {
        /// <summary>
        /// 시뮬레이터의 조종 대상을 몸(FPS)만으로 바꾼다. Unity가 첫 프레임 전에 한 번 호출한다.
        /// </summary>
        private void Start()
        {
            XRInteractionSimulator.instance.targetedDeviceInput = TargetedDevices.FPS;
        }
    }
}
