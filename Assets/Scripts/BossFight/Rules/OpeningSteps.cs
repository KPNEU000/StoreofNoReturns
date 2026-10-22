using System.Collections.Generic;

namespace BossFight.Rules
{
    public enum OpeningStep
    {
        LockPlayer,
        MagicToMonitor,
        ShatterMonitor,
        CrackScreen,
        WitchLine,
        GrabSword,
        GrabFlashlight,
        MoveLine,
        Vault,
        TutorialLight,
        TutorialSlash,
        DoorBlast
    }

    public enum OpeningCondition
    {
        Immediate,
        Seconds,
        LeftClick,
        RightClick,
        RightHeld,
        VaultDone,
        CostumeDead
    }

    // What the player and the scene did this frame; the opening only looks at what its current step waits for.
    public struct OpeningSignals
    {
        public bool LeftClick;
        public bool RightClick;
        public bool RightHeld;
        public bool VaultDone;
        public bool CostumeDead;
    }

    public readonly struct OpeningStepInfo
    {
        public readonly OpeningStep Step;
        public readonly OpeningCondition Condition;
        public readonly float Seconds;

        public OpeningStepInfo(OpeningStep step, OpeningCondition condition, float seconds = 0f)
        {
            Step = step;
            Condition = condition;
            Seconds = seconds;
        }
    }

    // The opening cinematic and tutorial as an ordered list of steps, each with the condition that ends it.
    // Advance moves at most one step per call so the caller can run each step's entry action.
    public class OpeningSteps
    {
        private readonly List<OpeningStepInfo> steps;
        private int index;
        private float elapsed;

        public IReadOnlyList<OpeningStepInfo> All => steps;
        public bool Finished => index >= steps.Count;
        public OpeningStepInfo Current => steps[index < steps.Count ? index : steps.Count - 1];
        public float Elapsed => elapsed;

        public OpeningSteps(BossFightSettings settings)
        {
            steps = new List<OpeningStepInfo>
            {
                new OpeningStepInfo(OpeningStep.LockPlayer, OpeningCondition.Immediate),
                new OpeningStepInfo(OpeningStep.MagicToMonitor, OpeningCondition.Seconds, settings.openingMagicSeconds),
                new OpeningStepInfo(OpeningStep.ShatterMonitor, OpeningCondition.Seconds, settings.openingShatterSeconds),
                new OpeningStepInfo(OpeningStep.CrackScreen, OpeningCondition.Seconds, settings.openingCrackSeconds),
                new OpeningStepInfo(OpeningStep.WitchLine, OpeningCondition.Seconds, settings.openingWitchLineSeconds),
                new OpeningStepInfo(OpeningStep.GrabSword, OpeningCondition.LeftClick),
                new OpeningStepInfo(OpeningStep.GrabFlashlight, OpeningCondition.RightClick),
                new OpeningStepInfo(OpeningStep.MoveLine, OpeningCondition.Seconds, settings.openingMoveLineSeconds),
                new OpeningStepInfo(OpeningStep.Vault, OpeningCondition.VaultDone),
                new OpeningStepInfo(OpeningStep.TutorialLight, OpeningCondition.RightHeld),
                new OpeningStepInfo(OpeningStep.TutorialSlash, OpeningCondition.CostumeDead),
                new OpeningStepInfo(OpeningStep.DoorBlast, OpeningCondition.Immediate),
            };
        }

        // Returns true when the step changed (or the last step just completed, making Finished true).
        public bool Advance(OpeningSignals signals, float deltaTime)
        {
            if (Finished) return false;
            elapsed += deltaTime;
            if (!IsMet(steps[index], signals)) return false;
            index++;
            elapsed = 0f;
            return true;
        }

        private bool IsMet(OpeningStepInfo info, OpeningSignals s)
        {
            switch (info.Condition)
            {
                case OpeningCondition.Immediate: return true;
                case OpeningCondition.Seconds: return elapsed >= info.Seconds;
                case OpeningCondition.LeftClick: return s.LeftClick;
                case OpeningCondition.RightClick: return s.RightClick;
                case OpeningCondition.RightHeld: return s.RightHeld;
                case OpeningCondition.VaultDone: return s.VaultDone;
                case OpeningCondition.CostumeDead: return s.CostumeDead;
                default: return false;
            }
        }
    }
}
