using System;
using System.ComponentModel;
using BovineLabs.Timeline.Authoring;
using UnityEngine.Timeline;

namespace BovineLabs.Timeline.Combat.Authoring
{
    [Serializable]
    [TrackClipType(typeof(FleeClip))]
    [TrackClipType(typeof(SeekClip))]
    [TrackClipType(typeof(StopClip))]
    [TrackColor(0.2f, 0.8f, 0.3f)]
    [DisplayName("BovineLabs/Combat/Locomotion")]
    public class LocomotionTrack : DOTSTrack
    {
    }
}
