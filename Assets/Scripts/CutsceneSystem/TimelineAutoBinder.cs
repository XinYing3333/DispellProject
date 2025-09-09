// using System.Linq;
// using UnityEngine;
// using UnityEngine.Playables;
//
// /// <summary>
// /// Naive auto-binder: tries to bind Timeline track outputs to scene objects by Track/Stream name.
// /// Strategy:
// /// 1) If a GameObject with Tag == streamName exists, bind that.
// /// 2) Else if a GameObject with Name == streamName exists, bind that.
// /// 3) (Optional) Add your common singletons or lookups below.
// /// Note: Real tracks expect specific component types (Animator, AudioSource, etc.).
// /// This helper will bind the first matching Component found on the target object,
// /// but you should extend this to match exact types for reliability.
// /// </summary>
// public static class TimelineAutoBinder
// {
//     /// <summary>Call before playing the director if you want to auto-wire exposed references.</summary>
//     public static void Bind(GameObject timelineGO)
//     {
//         var director = timelineGO.GetComponent<PlayableDirector>();
//         if (!director || director.playableAsset == null) return;
//
//         foreach (var binding in director.playableAsset.outputs)
//         {
//             // Skip if already bound
//             if (director.GetGenericBinding(binding.sourceObject) != null) continue;
//
//             string streamName = binding.streamName;
//             Object targetBinding = null;
//
//             // 1) Try by tag (first object found)
//             var tagged = GameObject.FindGameObjectsWithTagSafe(streamName).FirstOrDefault();
//             if (tagged != null) targetBinding = ResolveBindingForTrack(tagged, binding.sourceObject);
//
//             // 2) Try by exact name
//             if (targetBinding == null)
//             {
//                 var byName = GameObject.Find(streamName);
//                 if (byName != null) targetBinding = ResolveBindingForTrack(byName, binding.sourceObject);
//             }
//
//             // 3) Example: well-known names → singletons (extend per-project)
//             // if (targetBinding == null && streamName == "Player")
//             // {
//             //     var player = Object.FindFirstObjectByType<PlayerMovement>();
//             //     if (player) targetBinding = player.GetComponent<Animator>();
//             // }
//
//             if (targetBinding != null)
//                 director.SetGenericBinding(binding.sourceObject, targetBinding);
//         }
//     }
//
//     /// <summary>
//     /// Attempt to find a sensible Component on a GameObject for the given track source.
//     /// If nothing is found, fallback to binding the GameObject itself.
//     /// </summary>
//     private static Object ResolveBindingForTrack(GameObject go, Object trackSource)
//     {
//         if (go == null || trackSource == null) return null;
//
//         // Common track component guesses (extend as needed)
//         var animator = go.GetComponent<Animator>();
//         if (animator) return animator;
//
//         var audio = go.GetComponent<AudioSource>();
//         if (audio) return audio;
//
//         var cam = go.GetComponent<Camera>();
//         if (cam) return cam;
//
//         // Fallback: bind the GameObject. Some tracks accept GameObject.
//         return go;
//     }
//
//     /// <summary>Safe variant: FindGameObjectsWithTag but return empty array if tag missing.</summary>
//     private static GameObject[] FindGameObjectsWithTagSafe(this GameObject _, string tag)
//     {
//         try { return GameObject.FindGameObjectsWithTag(tag); }
//         catch { return new GameObject[0]; }
//     }
// }
