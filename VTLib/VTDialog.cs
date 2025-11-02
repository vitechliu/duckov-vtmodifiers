

using Cysharp.Threading.Tasks;
using NodeCanvas.DialogueTrees;
using UnityEngine;
using Dialogues;
using HarmonyLib;

namespace VTModifiers.VTLib;

public static class VTDialog {

    public static async UniTask DialogFlow(string[] textFlow) {
        // List<DuckovDialogueActor> actors = 
        //     Traverse.CreateWithType(nameof(DuckovDialogueActor)).Field("_activeActors")
        //     .GetValue<List<DuckovDialogueActor>>();
        // foreach (DuckovDialogueActor a in actors) {
        //     ModBehaviour.LogStatic($"Actors:{a.ID}: {a.NameKey}");
        // }

        DuckovDialogueActor dda = DuckovDialogueActor.Get("Jeff");
        if (!dda) return;
        foreach (string text in textFlow) {
            VtStatement vt = new VtStatement(text);
            SubtitlesRequestInfo sri = new SubtitlesRequestInfo(dda, vt, NN);
            await DialogueUI.instance.DoSubtitle(sri);
        }
    }

    public static void NN() {
        ModBehaviour.LogStatic("EndSRI");
    }
    
    
    public class VtStatement : IStatement {
        public string text { get; }
        public AudioClip audio { get; }
        public string meta { get; }

        public VtStatement(string t) {
            text = t;
        }
    }
}